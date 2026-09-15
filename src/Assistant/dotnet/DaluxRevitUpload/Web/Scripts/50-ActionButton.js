async function runActionButton(actionButtonText, results) {
            // --- STEP 3: ACTION BUTTON ---
            if (actionButtonText) {
                results.push('\n--- STEP 3: ACTION BUTTON ---');
                results.push('[*] Locating action button: ' + actionButtonText);
                let actionBtn = null;
                let allButtons = Array.from(document.querySelectorAll('button, [role="button"], input[type="button"], input[type="submit"], a[class*="button"]'));
                for (let btn of allButtons) {
                    let btnText = (btn.innerText || btn.textContent || btn.getAttribute('aria-label') || btn.getAttribute('title') || '').trim().toLowerCase();
                    if (btnText.includes(actionButtonText.toLowerCase())) {
                        actionBtn = btn;
                        break;
                    }
                }
                if (actionBtn) {
                    results.push('[+] Found action button, clicking...');
                    actionBtn.scrollIntoView({behavior: 'smooth', block: 'center', inline: 'center'});
                    await new Promise(r => setTimeout(r, 300));
                    actionBtn.focus();
                    await new Promise(r => setTimeout(r, 100));
                    actionBtn.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));
                    actionBtn.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));
                    actionBtn.click();
                    await new Promise(r => setTimeout(r, 1000));
                    results.push('[+] Action button clicked');
                    if (actionButtonText.toLowerCase().includes('upload')) {
                        results.push('[*] Upload initiated. Waiting for completion...');
                        const waitForDoneButton = async () => {
                            const maxWait = 12 * 60 * 60 * 1000;
                            const checkInterval = 5000;
                            const startTime = Date.now();
                            while (Date.now() - startTime < maxWait) {
                                let allElems = Array.from(document.querySelectorAll('button, [role="button"], input[type="button"], a[class*="button"]'));
                                let doneBtn = allElems.find(el => {
                                    let text = (el.innerText || el.textContent || el.getAttribute('aria-label') || el.getAttribute('title') || '').trim().toLowerCase();
                                    return text === 'done' || text === 'ok' || text.includes('done') && text.length < 20;
                                });
                                if (doneBtn) {
                                    results.push('[+] Done button found after ' + Math.round((Date.now() - startTime) / 1000) + 's');
                                    doneBtn.scrollIntoView({behavior: 'smooth', block: 'center', inline: 'center'});
                                    await new Promise(r => setTimeout(r, 300));
                                    doneBtn.focus();
                                    await new Promise(r => setTimeout(r, 100));
                                    doneBtn.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));
                                    doneBtn.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));
                                    doneBtn.click();
                                    results.push('[+] Done button clicked - Upload complete');
                                    return true;
                                }
                                await new Promise(r => setTimeout(r, checkInterval));
                            }
                            results.push('[!] Timeout: Done button not found within 12 hours');
                            return false;
                        };
                        await waitForDoneButton();
                    }
                } else {
                    results.push('[!] Action button not found: ' + actionButtonText);
                }
            }
}
