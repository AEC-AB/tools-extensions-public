namespace DaluxRevitUpload;

/// <summary>
/// Configuration for the Dalux headless automation tool.
/// This class contains all parameters needed for automation workflows.
/// </summary>
public class DaluxAutomationConfig
{
    /// <summary>
    /// The exact filename of the target file to be processed in Dalux.
    /// Example: "I90_BBH_A6_B72_K07_M00_F2_N001"
    /// </summary>
    public string TargetFilename { get; set; } = string.Empty;

    /// <summary>
    /// The amount to increment the revision number by.
    /// Example: 0.01 would increment "1.00" to "1.01"
    /// </summary>
    public double RevisionIncrement { get; set; } = 0.01;

    /// <summary>
    /// Column field configurations (dropdown and text fields combined).
    /// Key: Column header name (case-insensitive matching), Value: Target value.
    /// Field type (dropdown vs. text input) is detected automatically at runtime.
    /// </summary>
    public Dictionary<string, string> ColumnFields { get; set; } = new();

/// <summary>
    /// The text of the final action button to click.
    /// If set to "Upload", the script will automatically wait for the "Done" button
    /// for up to 12 hours before completing.
    /// Leave empty to skip this step.
    /// </summary>
    public string ActionButtonText { get; set; } = string.Empty;

    /// <summary>
    /// The process ID of the target Revit instance.
    /// When set, the automation targets this specific process instead of the first Revit process found.
    /// </summary>
    public int RevitProcessId { get; set; } = 0;

    /// <summary>
    /// The Chrome DevTools Protocol debugging port.
    /// 0 (default) means let each WebView2 browser process pick its own ephemeral port
    /// and write it to {UDF}/DevToolsActivePort — avoiding bind collisions between
    /// sibling WebView2 processes (Revit internal, Dalux popup, Teams, etc.).
    /// Set a fixed port only when something must share it.
    /// </summary>
    public int DebuggingPort { get; set; } = 0;

    /// <summary>
    /// Timeout in milliseconds for the WebSocket connection handshake.
    /// 44000 = 44 seconds. Applied per connect attempt; does not limit total script
    /// execution time (the JS automation itself can run for hours during upload waits).
    /// </summary>
    public int WebSocketTimeout { get; set; } = 44000;

    /// <summary>
    /// When true, if the CDP debugging port is held by a foreign process (not Revit,
    /// not a system-critical process), that process is terminated before proceeding.
    /// </summary>
    public bool AutoFreeCdpPort { get; set; } = true;

    /// <summary>
    /// Number of times to retry the JavaScript execution step when a transient CDP
    /// context error occurs (code -32000: popup navigated mid-connect). Each retry
    /// re-fetches the WebSocket URL and reconnects before re-running the script.
    /// </summary>
    public int RetryCount { get; set; } = 3;
}
