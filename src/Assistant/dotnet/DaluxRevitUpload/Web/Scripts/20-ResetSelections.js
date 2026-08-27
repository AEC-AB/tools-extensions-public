async function resetSelections(results) {
            // --- STEP 0: RESETTING ALL SELECTIONS ---
            results.push('\n--- STEP 0: RESETTING SELECTIONS ---');
            try {
                let allCbs = Array.from(document.querySelectorAll('input[data-cy="checkbox-input-field"]'));
                if (allCbs.length === 0) {
                    allCbs = Array.from(document.querySelectorAll('input[type="checkbox"]')).filter(cb => {
                        if (cb.closest('mat-slide-toggle, .toggle, .switch, dlx-toggle')) return false;
                        let rect = cb.getBoundingClientRect();
                        if (rect.left > window.innerWidth * 0.7 && rect.left > 0) return false;
                        return true;
                    });
                }
                let counterEls = Array.from(document.querySelectorAll('*')).filter(el => {
                    let txt = (el.innerText || '').trim();
                    let r = el.getBoundingClientRect();
                    return /^[0-9]+\s*\/\s*[0-9]+$/.test(txt) && r.width > 0 && el.children.length === 0;
                });
                if (counterEls.length > 0 && allCbs.length > 0) {
                    let counterEl = counterEls[0];
                    let masterCb = allCbs[0];
                    let clickTarget = masterCb.closest('.checkbox')  || masterCb.closest('div') || masterCb;
                    let getSelectedCount = () => parseInt((counterEl.innerText || '0').split('/')[0].trim());
                    results.push('[*] Initial selection state: ' + counterEl.innerText);
                    let toggleCount = 0;
                    while (getSelectedCount() > 0 && toggleCount < 4) {
                        results.push('[*] Clearing selections... (Click ' + (toggleCount + 1) + ')');
                        ['pointerdown','mousedown','pointerup','mouseup','click'].forEach(evt => {
                            clickTarget.dispatchEvent(new MouseEvent(evt, {bubbles: true, composed: true}));
                        });
                        masterCb.click();
                        await new Promise(r => setTimeout(r, 600));
                        toggleCount++;
                    }
                    results.push('[+] Master selection reset. Current state: ' + counterEl.innerText);
                } else {
                    results.push('[*] Master counter not found. Proceeding.');
                }
            } catch (e) {
                results.push('[!] Step 0 error: ' + e.message);
            }
}
