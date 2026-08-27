function findTargetFile(target, results) {
            // --- STEP 1: FINDING TARGET FILE ---
            const selectors = ['tr', '[role="row"]', '[role="listitem"]'];
            let rows = [];
            for (const sel of selectors) {
                rows = Array.from(document.querySelectorAll(sel));
                if (rows.length > 0) break;
            }
            results.push('\n--- STEP 1: CHECKING TARGET FILE ---');
            results.push('Found ' + rows.length + ' rows');
            let targetRow = null;
            for (const row of rows) {
                const text = row.textContent ? row.textContent.trim().toLowerCase() : '';
                if (text.includes(target)) {
                    results.push('[+] FOUND TARGET: ' + target);
                    targetRow = row;
                    let cb = row.querySelector('input[type="checkbox"]');
                    if (cb && !cb.checked) cb.click();
                    break;
                }
            }
            if (!targetRow) {
                results.push('[!] TARGET NOT FOUND');
                return null;
            }
    return targetRow;
}
