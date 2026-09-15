namespace DaluxRevitUpload;

internal sealed class RevitDaluxRibbonAutomation
{
    // P/Invoke for locked-screen-compatible window interaction
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT pt);
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }
    private const uint WM_LBUTTONDOWN = 0x0201;
    private const uint WM_LBUTTONUP   = 0x0202;

    private readonly DaluxAutomationConfig _config;
    private readonly Action<string> _log;

    public RevitDaluxRibbonAutomation(DaluxAutomationConfig config, Action<string> log)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _log = log ?? throw new ArgumentNullException(nameof(log));
    }

    public async Task<bool> ClickUploadAsync(CancellationToken cancellationToken)
    {
        _log("[*] Looking for Revit process...");
        Process revitProc;
        try
        {
            revitProc = Process.GetProcessById(_config.RevitProcessId);
        }
        catch
        {
            _log($"[!] Revit process with ID {_config.RevitProcessId} not found");
            return false;
        }

        _log("[*] Finding Revit main window via UI Automation...");
        var mainHwnd = revitProc.MainWindowHandle;
        if (mainHwnd == IntPtr.Zero)
        {
            _log("[!] Revit main window handle not found");
            return false;
        }

        // FromHandle bypasses the desktop root — works even when the screen is locked
        var revitWindow = AutomationElement.FromHandle(mainHwnd);
        if (revitWindow == null)
        {
            _log("[!] Could not create AutomationElement from Revit window");
            return false;
        }

        // Find and click the Dalux ribbon tab
        _log("[*] Looking for Dalux tab in ribbon...");
        var daluxTab = revitWindow.FindFirst(
            TreeScope.Descendants,
            new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.TabItem),
                new PropertyCondition(AutomationElement.NameProperty, "Dalux", PropertyConditionFlags.IgnoreCase)));

        if (daluxTab == null)
        {
            // Broader search: any element whose name is exactly "Dalux"
            daluxTab = revitWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, "Dalux", PropertyConditionFlags.IgnoreCase));
        }

        if (daluxTab == null)
        {
            _log("[!] Dalux tab not found in Revit ribbon");
            return false;
        }

        _log("[+] Found Dalux tab, clicking...");
        if (daluxTab.TryGetCurrentPattern(InvokePattern.Pattern, out var tabPattern))
            ((InvokePattern)tabPattern).Invoke();
        else
            PostMessageClick(mainHwnd, daluxTab);

        _log("[*] Waiting for Dalux ribbon panel to load...");
        await Task.Delay(1500, cancellationToken);

        // Find and click the Upload button in the Dalux ribbon panel.
        // We search broadly because the Dalux plugin's exact label/control-type varies
        // across versions and Revit locales:
        //   - Some versions ship a Button literally named "Upload".
        //   - Others ship a SplitButton named "Upload Model" / "Upload to Dalux".
        //   - Localized Revit installs may translate the label.
        // Strategy: match any element whose Name contains "upload" (case-insensitive),
        // regardless of ControlType. Retry for several seconds while the ribbon paints.
        _log("[*] Looking for Upload button in Dalux ribbon...");
        AutomationElement? uploadBtn = null;
        int attempts = 0;
        const int maxAttempts = 10;
        while (uploadBtn == null && attempts < maxAttempts)
        {
            // Pass 1: exact "Upload" Button (the original happy path — fastest match).
            uploadBtn = revitWindow.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Button),
                    new PropertyCondition(AutomationElement.NameProperty, "Upload", PropertyConditionFlags.IgnoreCase)));

            // Pass 2: any control whose Name contains "upload" (substring, case-insensitive),
            // restricted to clickable types so we don't grab a static text label.
            if (uploadBtn == null)
            {
                var candidates = revitWindow.FindAll(TreeScope.Descendants,
                    new OrCondition(
                        new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Button),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.SplitButton),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.MenuItem),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.ListItem)));
                foreach (AutomationElement c in candidates)
                {
                    var name = c.Current.Name ?? string.Empty;
                    if (name.IndexOf("upload", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        uploadBtn = c;
                        if (attempts > 0)
                            _log($"[*] Matched broader candidate: ControlType={c.Current.ControlType.ProgrammaticName}, Name=\"{name}\"");
                        break;
                    }
                }
            }

            if (uploadBtn == null)
            {
                attempts++;
                await Task.Delay(500, cancellationToken);
            }
        }

        if (uploadBtn == null)
        {
            _log("[!] Upload button not found in Dalux ribbon panel");
            DumpRibbonCandidatesForDiagnostics(revitWindow);
            return false;
        }

        _log("[+] Found Upload button, clicking to open Dalux popup...");
        if (uploadBtn.TryGetCurrentPattern(InvokePattern.Pattern, out var btnPattern))
            ((InvokePattern)btnPattern).Invoke();
        else
            PostMessageClick(mainHwnd, uploadBtn);

        return true;
    }

    /// <summary>
    /// Sends WM_LBUTTONDOWN/UP directly to the window's message queue using client-area
    /// coordinates calculated from the element's screen bounding rect.
    /// Works even when the screen is locked because PostMessage bypasses the active desktop.
    /// </summary>
    private void PostMessageClick(IntPtr hwnd, AutomationElement element)
    {
        var rect = element.Current.BoundingRectangle;
        var pt = new POINT
        {
            X = (int)(rect.Left + rect.Width  / 2),
            Y = (int)(rect.Top  + rect.Height / 2)
        };
        ScreenToClient(hwnd, ref pt);
        var lParam = new IntPtr((pt.Y << 16) | (pt.X & 0xFFFF));
        PostMessage(hwnd, WM_LBUTTONDOWN, new IntPtr(0x0001), lParam);
        PostMessage(hwnd, WM_LBUTTONUP,   IntPtr.Zero,        lParam);
    }


    /// <summary>
    /// Dumps a snapshot of the Dalux ribbon's clickable controls so we can diagnose
    /// "Upload button not found" failures remotely. The exact label varies across
    /// Dalux plugin versions and Revit locales; the dump lets a remote user paste the
    /// log back and we can add the correct match without another round-trip.
    /// </summary>
    private void DumpRibbonCandidatesForDiagnostics(AutomationElement revitWindow)
    {
        try
        {
            _log("[DEBUG] Dumping clickable ribbon controls so we can see what's actually labelled.");
            _log("[DEBUG] Send these lines back so the Upload-button matcher can be updated for your Revit/Dalux version.");

            var clickable = revitWindow.FindAll(TreeScope.Descendants,
                new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.Button),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.SplitButton),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.MenuItem),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, System.Windows.Automation.ControlType.ListItem)));

            int total = clickable.Count;
            int shown = 0;
            const int maxShown = 60;
            foreach (AutomationElement c in clickable)
            {
                var name = c.Current.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name)) continue;

                var lower = name.ToLowerInvariant();
                bool relevant =
                    lower.Contains("upload") ||
                    lower.Contains("dalux")  ||
                    lower.Contains("send")   ||
                    lower.Contains("publish");

                if (!relevant) continue;

                _log($"[DEBUG]   ControlType={c.Current.ControlType.ProgrammaticName} Name=\"{name}\" AutomationId=\"{c.Current.AutomationId}\"");
                if (++shown >= maxShown) break;
            }

            if (shown == 0)
                _log($"[DEBUG]   No controls matched upload/dalux/send/publish (scanned {total} clickable elements). Dalux ribbon panel may not be loaded \u2014 try opening the Dalux tab manually and rerunning.");
            else
                _log($"[DEBUG]   (showed {shown} of {total} clickable elements; only those whose Name contains upload/dalux/send/publish)");
        }
        catch (Exception ex)
        {
            _log($"[DEBUG] Failed to enumerate ribbon controls: {ex.Message}");
        }
    }
}
