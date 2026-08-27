async function waitForDaluxPopup(results) {
            // --- WAIT FOR POPUP TO FULLY LOAD ---
            results.push('\n--- WAITING FOR DALUX POPUP TO LOAD ---');
            try {
                let loadAttempts = 0;
                const maxLoadAttempts = 1200; // 10 minutes (1200 × 500ms)
                const countRows = () => {
                    let n = document.querySelectorAll('tr, [role="row"]').length;
                    if (n === 0) {
                        for (const f of document.querySelectorAll('iframe')) {
                            try { n += f.contentDocument.querySelectorAll('tr, [role="row"]').length; } catch(e) {}
                        }
                    }
                    return n;
                };
                const countCbs = () => {
                    let n = document.querySelectorAll('input[type="checkbox"], input[data-cy="checkbox-input-field"]').length;
                    if (n === 0) {
                        for (const f of document.querySelectorAll('iframe')) {
                            try { n += f.contentDocument.querySelectorAll('input[type="checkbox"], input[data-cy="checkbox-input-field"]').length; } catch(e) {}
                        }
                    }
                    return n;
                };
                while (loadAttempts < maxLoadAttempts) {
                    const rows = countRows();
                    const checkboxes = countCbs();
                    if (rows > 1 || checkboxes > 0) {
                        results.push('[+] Popup fully loaded (' + rows + ' rows, ' + checkboxes + ' checkboxes)');
                        break;
                    }
                    await new Promise(r => setTimeout(r, 500));
                    loadAttempts++;
                    // Every 2 seconds: fire resize + force virtual scroll viewports to non-zero height
                    if (loadAttempts % 4 === 0) {
                        window.dispatchEvent(new Event('resize'));
                        document.querySelectorAll('cdk-virtual-scroll-viewport, [class*="virtual-scroll"]').forEach(vp => {
                            if (!vp.getBoundingClientRect().height)
                                vp.style.setProperty('height', '600px', 'important');
                        });
                    }
                }
                if (loadAttempts >= maxLoadAttempts) {
                    const diagTitle   = document.title;
                    const diagUrl     = document.URL.substring(0, 100);
                    const diagReady   = document.readyState;
                    const diagElems   = document.querySelectorAll('*').length;
                    const diagIframes = document.querySelectorAll('iframe').length;
                    results.push('[DEBUG] Title: ' + diagTitle);
                    results.push('[DEBUG] URL: ' + diagUrl);
                    results.push('[DEBUG] ReadyState: ' + diagReady + ' | Elements: ' + diagElems + ' | iframes: ' + diagIframes);
                    if (diagIframes > 0) results.push('[DEBUG] rows in iframes: ' + countRows() + ' | checkboxes in iframes: ' + countCbs());
                    results.push('[!] Timed out (10 min) waiting for popup to load — no rows or checkboxes found. Aborting.');
                    return results.join('\n');
                }
            } catch(e) {
                results.push('[!] Error waiting for popup load: ' + e.message);
                return false;
            }
    
    return true;
}
