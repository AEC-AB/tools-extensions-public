async function updateMetadata(targetRow, target, incrementValue, columnConfig, results) {
            // --- STEP 2: EXTRACTING & UPDATING METADATA ---
            results.push('\n--- STEP 2: EXTRACTING & UPDATING METADATA ---');
            const extractedData = {};
            let revisionUpdated = false;
            let revisionOldVal = '';
            let revisionNewVal = '';
            let columnsUpdated = {};
            for (let key of Object.keys(columnConfig)) columnsUpdated[key] = false;
            let allDetectedHeaders = new Set();
    
            const getHeaderText = (el) => {
                if (!el) return '';
                let title = el.getAttribute('title') || el.getAttribute('data-tooltip') || el.getAttribute('mat-tooltip');
                if (title && title.trim().length > 0) return title.trim();
                let text = String(el.innerText || el.textContent || '');
                text = text.replace(String.fromCharCode(10), ' ').replace(String.fromCharCode(13), ' ');
                return text.trim();
            };
    
            const typeText = async (el, text) => {
                el.scrollIntoView({behavior: 'instant', block: 'center', inline: 'center'});
                el.focus();
                if (el.select) el.select();
                await new Promise(r => setTimeout(r, 50));
                let nativeSetter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value');
                if (!nativeSetter) nativeSetter = Object.getOwnPropertyDescriptor(HTMLTextAreaElement.prototype, 'value');
                if (nativeSetter && nativeSetter.set) nativeSetter.set.call(el, '');
                else el.value = '';
                el.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
                await new Promise(r => setTimeout(r, 50));
                for (let i = 0; i < text.length; i++) {
                    let char = text[i];
                    if (nativeSetter && nativeSetter.set) nativeSetter.set.call(el, el.value + char);
                    else el.value += char;
                    el.dispatchEvent(new KeyboardEvent('keydown', { key: char, bubbles: true, composed: true }));
                    el.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
                    el.dispatchEvent(new KeyboardEvent('keyup', { key: char, bubbles: true, composed: true }));
                    await new Promise(r => setTimeout(r, 30));
                }
                await new Promise(r => setTimeout(r, 400));
                el.dispatchEvent(new Event('change', { bubbles: true, composed: true }));
                el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true, composed: true }));
                el.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true, composed: true }));
                el.blur();
                el.dispatchEvent(new Event('focusout', { bubbles: true, composed: true }));
                await new Promise(r => setTimeout(r, 100));
                let safeSpot = document.querySelector('.dlx-datagrid-header-cell') || document.body;
                safeSpot.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));
                safeSpot.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));
                safeSpot.click();
                await new Promise(r => setTimeout(r, 200));
            };
    
            const handleDropdown = async (cell, targetValue, headerName) => {
                results.push('[*] Processing Dropdown: ' + headerName + ' -> Target: ' + targetValue);
                cell.scrollIntoView({behavior: 'instant', block: 'center', inline: 'center'});
                cell.click();
                cell.dispatchEvent(new MouseEvent('dblclick', {bubbles: true, composed: true}));
                await new Promise(r => setTimeout(r, 400));
                let cRect = cell.getBoundingClientRect();
                let hitX = cRect.right - 15;
                let hitY = cRect.top + (cRect.height / 2);
                results.push('[*] Dropdown pointer: cellRect={left:' + Math.round(cRect.left) + ',top:' + Math.round(cRect.top) + ',w:' + Math.round(cRect.width) + ',h:' + Math.round(cRect.height) + '} hitX=' + Math.round(hitX) + ' hitY=' + Math.round(hitY));
                let arrowTarget = document.elementFromPoint(hitX, hitY) || cell;
                results.push('[*] arrowTarget: ' + (arrowTarget === cell ? 'cell (elementFromPoint returned null)' : arrowTarget.tagName + '.' + (arrowTarget.className||'').split(' ').slice(0,2).join('.')));
                ['pointerdown', 'mousedown', 'pointerup', 'mouseup', 'click'].forEach(evt => {
                    arrowTarget.dispatchEvent(new MouseEvent(evt, {
                        bubbles: true, composed: true, view: window, clientX: hitX, clientY: hitY
                    }));
                });
                let focusTarget = document.activeElement || cell;
                focusTarget.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', altKey: true, bubbles: true, composed: true }));
                await new Promise(r => setTimeout(r, 1000));
                let success = false;
                const optionSelectors = '[role="option"], .mat-option, .ng-option, .dropdown-item, li.dlx-list-item, div.option, .dlx-dropdown-item, li span';
                const options = Array.from(document.querySelectorAll(optionSelectors));
                results.push('[*] Options in DOM after Alt+Down: ' + options.length + (options.length > 0 ? ' | first: "' + (options[0].innerText||options[0].textContent||'').trim().substring(0,30) + '"' : ''));
                results.push('[*] Searching ' + options.length + ' options for: "' + targetValue + '" | available: ' + options.slice(0,8).map(o => '"' + (o.innerText||o.textContent||'').trim().substring(0,20) + '"').join(', '));
                let targetOption = options.find(opt => {
                    let text = (opt.innerText || opt.textContent || '').trim().toLowerCase();
                    return text === targetValue.toLowerCase() || text.includes(targetValue.toLowerCase()) || targetValue.toLowerCase().includes(text) && text.length > 2;
                });
                if (targetOption && targetOption.getBoundingClientRect().width > 0) {
                    results.push('[*] Target option \'' + targetValue + '\' found directly. Clicking it...');
                    targetOption.scrollIntoView({behavior: 'instant', block: 'center'});
                    targetOption.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));
                    targetOption.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));
                    targetOption.click();
                    success = true;
                }
                if (!success) {
                    let searchInput = null;
                    let activeEl = document.activeElement;
                    if (activeEl && activeEl.tagName === 'INPUT' && activeEl.type !== 'checkbox' && !cell.contains(activeEl)) {
                        searchInput = activeEl;
                    } else {
                        let overlayInputs = Array.from(document.querySelectorAll('body > *:last-child input, .cdk-overlay-container input, .mat-select-panel input, .dropdown-menu input, .dlx-dropdown input, input[placeholder*="earch"]'))
                            .filter(i => i.getBoundingClientRect().width > 0 && !cell.contains(i));
                        if (overlayInputs.length > 0) searchInput = overlayInputs[0];
                    }
                    if (searchInput) {
                        results.push('[*] Typing \'' + targetValue + '\' into search bar...');
                        await typeText(searchInput, targetValue);
                        success = true;
                    } else {
                        results.push('[!] Could NOT safely identify an overlay for ' + headerName);
                    }
                }
                let safeSpot = document.querySelector('.dlx-datagrid-header-cell') || document.body;
                safeSpot.dispatchEvent(new MouseEvent('mousedown', {bubbles: true, composed: true}));
                safeSpot.dispatchEvent(new MouseEvent('mouseup', {bubbles: true, composed: true}));
                safeSpot.click();
                await new Promise(r => setTimeout(r, 300));
                return success;
            };
    
            const handleDatePicker = async (cell, targetDateStr, headerName) => {
                try {
                    // STEP 1: TRIGGER THE DATE PICKER
                    cell.scrollIntoView({behavior: 'instant', block: 'center', inline: 'center'});
                    let triggerEl = cell.querySelector('[data-cy="datepicker-input-box"], [class*="calendar"], [class*="chevron"]');
                    if (!triggerEl) triggerEl = cell.querySelector('button, [role="button"]');
                    if (!triggerEl) triggerEl = cell;
                    
                    // Dispatch full pointer event sequence for complex UI frameworks
                    triggerEl.dispatchEvent(new PointerEvent('pointerdown', {bubbles: true, composed: true}));
                    triggerEl.dispatchEvent(new MouseEvent('mousedown', {bubbles: true}));
                    triggerEl.dispatchEvent(new PointerEvent('pointerup', {bubbles: true, composed: true}));
                    triggerEl.dispatchEvent(new MouseEvent('mouseup', {bubbles: true}));
                    triggerEl.dispatchEvent(new MouseEvent('click', {bubbles: true}));
                    await new Promise(r => setTimeout(r, 500));
                    
                    // STEP 2: LOCATE THE CALENDAR OVERLAY
                    let calendar = document.querySelector('dlx-date-calender, [data-cy="calendar-header"], .dlx-date-picker');
                    if (!calendar) {
                        calendar = document.querySelector('.cdk-overlay-pane [data-cy="calendar-header"]');
                    }
                    if (!calendar) {
                        for (let overlay of document.querySelectorAll('.cdk-overlay-pane')) {
                            if (overlay.textContent.includes('Sun') || overlay.textContent.includes('Mon')) {
                                calendar = overlay;
                                break;
                            }
                        }
                    }
                    if (!calendar) {
                        results.push('[!] Could not update ' + headerName + ' - date picker not found');
                        return false;
                    }
                    
                    // STEP 3: PARSE TARGET DATE
                    const monthNames = {jan:1,feb:2,mar:3,apr:4,may:5,jun:6,jul:7,aug:8,sep:9,oct:10,nov:11,dec:12};
                    let day, month, year;
                    let parts = targetDateStr.split('-');
                    if (parts.length < 3) parts = targetDateStr.split('/');
                    if (parts.length < 3) {
                        let textMatch = targetDateStr.match(/(\d{1,2})\s+([A-Za-z]+)\s+(\d{4})/);
                        if (textMatch) {
                            day = parseInt(textMatch[1]);
                            month = monthNames[textMatch[2].toLowerCase().substring(0,3)] || parseInt(textMatch[2]);
                            year = parseInt(textMatch[3]);
                        } else {
                            results.push('[!] Invalid date format: ' + targetDateStr);
                            return false;
                        }
                    } else {
                        day = parseInt(parts[0]);
                        let monthPart = parts[1].toLowerCase();
                        month = monthNames[monthPart.substring(0,3)] || parseInt(monthPart);
                        year = parseInt(parts[2]);
                    }
                    if (year < 100) year += 2000;
                    const targetTotalMonths = (year * 12) + month;
                    
                    // STEP 4: NAVIGATE TO TARGET MONTH/YEAR
                    let maxNav = 24;
                    let currentNav = 0;
                    while (currentNav < maxNav) {
                        let headerEl = calendar.querySelector('[data-cy="calendar-header"], [class*="header"]');
                        let currentText = '';
                        for (let wait = 0; wait < 10; wait++) {
                            currentText = headerEl ? (headerEl.innerText || headerEl.textContent || '') : '';
                            if (currentText.match(/\d{4}/) || currentText.includes('2026') || currentText.includes('2025')) break;
                            await new Promise(r => setTimeout(r, 200));
                        }
                        
                        let match = currentText.match(/(\d{4})|(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)/gi);
                        let currentMonth = 1, currentYear = 2026;
                        if (match) {
                            for (let m of match) {
                                if (/\d{4}/.test(m)) currentYear = parseInt(m);
                                else currentMonth = Math.max(currentMonth, Object.keys(monthNames).indexOf(m.toLowerCase().substring(0,3)) + 1);
                            }
                        }
                        const currentTotalMonths = (currentYear * 12) + currentMonth;
                        
                        if (currentTotalMonths === targetTotalMonths) break;
                        
                        let navBtn = null;
                        if (currentTotalMonths < targetTotalMonths) {
                            navBtn = calendar.querySelector('[data-cy="date-next-month-btn"]');
                        } else {
                            navBtn = calendar.querySelector('[data-cy="date-prev-month-btn"]');
                        }
                        if (!navBtn) navBtn = calendar.querySelectorAll('button')[currentTotalMonths < targetTotalMonths ? 1 : 0];
                        if (!navBtn) break;
                        navBtn.click();
                        await new Promise(r => setTimeout(r, 300));
                        currentNav++;
                    }
                    
                    // STEP 5: SELECT THE CORRECT DAY
                    let dayElements = Array.from(calendar.querySelectorAll('button, [role="button"], td, div, span'));
                    dayElements = dayElements.filter(el => {
                        if (el.offsetHeight === 0) return false;
                        let text = el.textContent.trim();
                        if (text !== String(day)) return false;
                        let style = window.getComputedStyle(el);
                        if (parseFloat(style.opacity || '1') !== 1) return false;
                        if (el.className.includes('muted') || el.className.includes('disabled')) return false;
                        return true;
                    });
                    
                    if (dayElements.length > 1) {
                        let minLeft = Math.min(...dayElements.map(el => el.getBoundingClientRect().left));
                        let tolerance = 5;
                        dayElements = dayElements.filter(el => el.getBoundingClientRect().left > minLeft + tolerance);
                    }
                    
                    if (dayElements.length === 0) {
                        let allElements = calendar.querySelectorAll('*');
                        for (let el of allElements) {
                            if (el.childNodes.length === 1 && el.childNodes[0].nodeType === 3) {
                                let text = el.textContent.trim();
                                if (text === String(day) && el.offsetHeight > 0) {
                                    let style = window.getComputedStyle(el);
                                    if (parseFloat(style.opacity || '1') === 1) {
                                        dayElements = [el];
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    let dayBtn = dayElements[0];
                    if (dayBtn) {
                        dayBtn.scrollIntoView({behavior: 'instant', block: 'nearest'});
                        await new Promise(r => setTimeout(r, 100));
                        dayBtn.dispatchEvent(new MouseEvent('mousedown', {bubbles: true}));
                        dayBtn.dispatchEvent(new MouseEvent('mouseup', {bubbles: true}));
                        dayBtn.dispatchEvent(new MouseEvent('click', {bubbles: true}));
                        await new Promise(r => setTimeout(r, 500));
                        results.push('[+] Updated ' + headerName + ' to ' + targetDateStr);
                        return true;
                    } else {
                        results.push('[!] Could not find day ' + day + ' in calendar for ' + headerName);
                        return false;
                    }
                } catch(e) {
                    results.push('[!] Date picker error for ' + headerName + ': ' + e.message);
                    return false;
                }
            };
    
            // Helper: get visible non-checkbox cells from the target row.
            // Uses textContent (not innerText) so the row is found even when off-screen or vertically clipped.
            // Does NOT filter the row by width — the row may be below the grid's visible area after moving to last position.
            const getVisibleCells = () => {
                let cells = Array.from(targetRow.querySelectorAll('td, th, [role="gridcell"], [class*="cell"]'));
                if (cells.length === 0) cells = Array.from(targetRow.children);
                let visible = cells.filter(c => c && c.nodeType === 1 && c.getBoundingClientRect().width > 0 && !c.querySelector('input[type="checkbox"]'));
                // Exclude frozen/pinned cells that live outside the scroll container —
                // they have fixed viewport positions and corrupt horizontal position matching.
                if (scrollerContainer) visible = visible.filter(c => scrollerContainer.contains(c));
                return visible;
            };
            // Helper: detect cell type and update it
            const updateCell = async (cell, targetValue, key) => {
                let cR = cell.getBoundingClientRect();
                let cTxt = (cell.innerText || cell.textContent || '').trim().replace(/\s+/g, ' ').substring(0, 40);
                let isDatePicker = !!cell.querySelector('[data-cy="datepicker-input-box"], dlx-date-calender, dlx-date-picker, [class*="date-picker"], [class*="datepicker"], [class*="calendar"]');
                let isLikelyDropdown = !isDatePicker && !!cell.querySelector('[role="combobox"], [role="listbox"], mat-select, dlx-dropdown, dlx-select, select, [class*="dropdown"], [class*="dlx-select"], [class*="mat-select"]');
                results.push('[*] updateCell: ' + key + ' | cellRect={left:' + Math.round(cR.left) + ',top:' + Math.round(cR.top) + ',w:' + Math.round(cR.width) + '} | existingText="' + cTxt + '" | type=' + (isDatePicker ? 'datePicker' : isLikelyDropdown ? 'dropdown' : 'unknown'));
                if (isDatePicker) {
                    results.push('[*] Date picker detected for: ' + key);
                    await handleDatePicker(cell, targetValue, key);
                    results.push('[+] Updated ' + key + ' to ' + targetValue + ' (date)');
                    return true;
                } else if (isLikelyDropdown) {
                    results.push('[*] Dropdown detected for: ' + key);
                    let ok = await handleDropdown(cell, targetValue, key);
                    if (ok) results.push('[+] Updated ' + key + ' to ' + targetValue + ' (dropdown)');
                    else results.push('[!] Dropdown failed for: ' + key);
                    return ok;
                } else {
                    let inputEl = cell.querySelector('input:not([type="checkbox"]), textarea');
                    if (inputEl && inputEl.getBoundingClientRect().width > 0) {
                        await typeText(inputEl, targetValue);
                        results.push('[+] Updated ' + key + ' to ' + targetValue + ' (text)');
                        return true;
                    } else {
                        cell.scrollIntoView({behavior: 'instant', block: 'center', inline: 'center'});
                        cell.click();
                        await new Promise(r => setTimeout(r, 300));
                        cell.dispatchEvent(new MouseEvent('dblclick', {bubbles: true}));
                        await new Promise(r => setTimeout(r, 400));
                        inputEl = cell.querySelector('input:not([type="checkbox"]), textarea') || document.activeElement;
                        if (inputEl && (inputEl.tagName === 'INPUT' || inputEl.tagName === 'TEXTAREA') && inputEl.getBoundingClientRect().width > 0) {
                            await typeText(inputEl, targetValue);
                            results.push('[+] Updated ' + key + ' to ' + targetValue + ' (text)');
                            return true;
                        } else {
                            let ok = await handleDropdown(cell, targetValue, key);
                            if (ok) results.push('[+] Updated ' + key + ' to ' + targetValue + ' (dropdown)');
                            else results.push('[!] No text input or dropdown found for: ' + key);
                            return ok;
                        }
                    }
                }
            };
            // ── SCROLL CONTAINER DETECTION ──
            let scrollerContainer = targetRow.closest('.cdk-virtual-scroll-viewport, [class*="datagrid-body"], [class*="grid-viewport"], [class*="scroll-container"]');
            if (!scrollerContainer || scrollerContainer.scrollWidth <= scrollerContainer.clientWidth) {
                let p = targetRow.parentElement;
                while (p && p !== document.body) {
                    if (p.scrollWidth > p.clientWidth + 10) { scrollerContainer = p; break; }
                    p = p.parentElement;
                }
            }
            let scrollerContainerRect = scrollerContainer ? scrollerContainer.getBoundingClientRect() : { left: 0, width: 800 };
            let maxScrollLeft = scrollerContainer ? scrollerContainer.scrollWidth - scrollerContainer.clientWidth : 0;
            const doScroll = (el, pos) => {
                el.scrollLeft = pos;
                if (el.scrollTo) el.scrollTo({ left: pos, behavior: 'instant' });
                el.dispatchEvent(new Event('scroll', { bubbles: true }));
                window.dispatchEvent(new Event('scroll'));
            };
            // ── COLLECT ALL COLUMN HEADERS (sticky — always in DOM) ──
            let allGridHeaders = Array.from(document.querySelectorAll('th, [role="columnheader"], [class*="header-cell"], .dlx-datagrid-header-cell'));
            allGridHeaders.filter(h => h.getBoundingClientRect().width > 0).forEach(h => { let t = getHeaderText(h); if (t) allDetectedHeaders.add(t); });
            results.push('[*] Scroll container: ' + (scrollerContainer ? 'found (maxScroll=' + maxScrollLeft + 'px)' : 'none'));
            // ── KEY-CENTRIC HELPER: scroll a column into view, return its data cell ──
            const scrollToColumn = async (headerEl) => {
                // Headers are NOT sticky — they scroll with content. Add currentScrollLeft to get the
                // header's original (scroll=0) position, which is what naturalOffset must be based on.
                let hLeft = headerEl.getBoundingClientRect().left + (scrollerContainer ? scrollerContainer.scrollLeft : 0);
                if (!scrollerContainer) {
                    return getVisibleCells().find(c => Math.abs(c.getBoundingClientRect().left - hLeft) < 30) || null;
                }
                // Inner helper: scroll to bottom vertically, find fresh row, center it
                const reAcquireRow = async () => {
                    results.push('[*] Re-acquiring target row (vertically scrolling to bottom)...');
                    doScroll(scrollerContainer, 0);
                    let vertCandidates = [];
                    for (let el of Array.from(document.querySelectorAll('.cdk-virtual-scroll-viewport, [class*="datagrid"], [class*="grid-body"], [class*="scroll"], [class*="table"]'))) {
                        if (el.scrollHeight > el.clientHeight + 5) vertCandidates.push(el);
                    }
                    let vsP = scrollerContainer ? scrollerContainer.parentElement : null;
                    while (vsP && vsP !== document.body) {
                        if (vsP.scrollHeight > vsP.clientHeight + 5 && !vertCandidates.includes(vsP)) vertCandidates.push(vsP);
                        vsP = vsP.parentElement;
                    }
                    results.push('[*] Vert containers found: ' + vertCandidates.map(vc => vc.tagName + '(scrollTop=' + vc.scrollTop + ',scrollH=' + vc.scrollHeight + ')').join(', '));
                    for (let vc of vertCandidates) { vc.scrollTop = vc.scrollHeight; }
                    window.scrollTo(0, document.body.scrollHeight);
                    await new Promise(r => setTimeout(r, 1000));
                    for (let vc of vertCandidates) results.push('[*] After vertScroll: ' + vc.tagName + ' scrollTop=' + vc.scrollTop);
                    let freshRow = Array.from(document.querySelectorAll('tr, [role="row"], [role="listitem"]'))
                        .find(r => r.textContent.toLowerCase().includes(target));
                    if (freshRow) {
                        targetRow = freshRow;
                        freshRow.scrollIntoView({ behavior: 'instant', block: 'center' });
                        await new Promise(r => setTimeout(r, 300));
                        let fr = freshRow.getBoundingClientRect();
                        // Layout reflows after vertical scroll (scrollbar appearing shifts positions).
                        // Recompute scrollerContainerRect so all subsequent naturalOffset calculations are accurate.
                        let prevLeft = scrollerContainerRect.left;
                        scrollerContainerRect = scrollerContainer.getBoundingClientRect();
                        results.push('[*] Row re-acquired and centered: rect={top:' + Math.round(fr.top) + ',h:' + Math.round(fr.height) + '} | scrollerContainer.left: ' + Math.round(prevLeft) + ' → ' + Math.round(scrollerContainerRect.left));
                        // ONE-TIME DIAGNOSTIC: dump all cells in the row at scroll=0 to understand the DOM structure
                        doScroll(scrollerContainer, 0);
                        await new Promise(r => setTimeout(r, 500));
                        let diagCells = Array.from(freshRow.querySelectorAll('*')).filter(c => c.getBoundingClientRect().width > 0 && c.getBoundingClientRect().height > 0 && !c.querySelector('[role="gridcell"], td, th'));
                        let seen = new Set();
                        diagCells = diagCells.filter(c => { let k = Math.round(c.getBoundingClientRect().left); if (seen.has(k)) return false; seen.add(k); return true; });
                        results.push('[DIAG] Row cells at scroll=0 (one per unique left):');
                        diagCells.slice(0, 30).forEach(c => {
                            let r = c.getBoundingClientRect();
                            let cls = (typeof c.className === 'string' ? c.className : (c.getAttribute && c.getAttribute('class') || '')).split(' ').slice(0,2).join('.');
                            let txt = (c.innerText||c.textContent||'').trim().replace(/\s+/g,' ').substring(0,25);
                            let inSc = scrollerContainer.contains(c);
                            results.push('[DIAG]  ' + c.tagName + '.' + cls + ' left=' + Math.round(r.left) + ' w=' + Math.round(r.width) + ' inScroller=' + inSc + ' text="' + txt + '"');
                        });
                        results.push('[DIAG] Header positions:');
                        allGridHeaders.filter(h => h.getBoundingClientRect().width > 0).forEach(h => {
                            let r = h.getBoundingClientRect();
                            results.push('[DIAG]  header "' + getHeaderText(h).substring(0,20) + '" left=' + Math.round(r.left) + ' w=' + Math.round(r.width));
                        });
                    } else {
                        results.push('[!] Row still not found after vertical scroll');
                    }
                };
                // Pre-scroll: re-acquire if already detached
                if (!document.body.contains(targetRow)) await reAcquireRow();
                let naturalOffset = hLeft - scrollerContainerRect.left;
                let targetPos = Math.max(0, Math.min(Math.round(naturalOffset - scrollerContainer.clientWidth / 2), maxScrollLeft));
                doScroll(scrollerContainer, targetPos);
                await new Promise(r => setTimeout(r, 700));
                let cells = getVisibleCells();
                // Post-scroll: re-acquire if row became detached DURING the wait (Angular re-render)
                if (cells.length === 0 && !document.body.contains(targetRow)) {
                    await reAcquireRow();
                    doScroll(scrollerContainer, targetPos);
                    await new Promise(r => setTimeout(r, 700));
                    cells = getVisibleCells();
                }
                let actualScroll = scrollerContainer.scrollLeft;
                let expectedLeft = scrollerContainerRect.left + naturalOffset - actualScroll;
                let rowY = targetRow.getBoundingClientRect().top + targetRow.getBoundingClientRect().height / 2;
                // PRIMARY: elementFromPoint — finds the actual rendered element regardless of CSS class.
                // This handles frozen/sticky cells that don't match our querySelectorAll selectors.
                let efpCell = null;
                {
                    let el = document.elementFromPoint(expectedLeft, rowY);
                    let walker = el;
                    while (walker && walker !== document.body) {
                        if (walker.matches && walker.matches('td, th, [role="gridcell"], [class*="cell"]') && targetRow.contains(walker)) { efpCell = walker; break; }
                        walker = walker.parentElement;
                    }
                    if (!efpCell && el && targetRow.contains(el)) efpCell = el;
                    let efpTxt = efpCell ? (efpCell.innerText || efpCell.textContent || '').trim().replace(/\s+/g, ' ').substring(0, 30) : 'null';
                    results.push('[*] scrollToColumn: ' + getHeaderText(headerEl) + ' targetPos=' + targetPos + ' actualScroll=' + Math.round(actualScroll) + ' expectedX=' + Math.round(expectedLeft) + ' rowY=' + Math.round(rowY) + ' | EFP: ' + (el ? el.tagName + '.' + (el.className||'').split(' ')[0] : 'null') + ' inRow=' + !!efpCell + ' text="' + efpTxt + '"');
                }
                // FALLBACK: position matching among querySelectorAll cells
                let best = null, bestDist = Infinity;
                for (let c of cells) { let d = Math.abs(c.getBoundingClientRect().left - expectedLeft); if (d < bestDist) { bestDist = d; best = c; } }
                if (best && bestDist < 50) {
                    let bestTxt = (best.innerText || best.textContent || '').trim().replace(/\s+/g, ' ').substring(0, 30);
                    results.push('[*]   POS fallback: bestDist=' + Math.round(bestDist) + ' text="' + bestTxt + '"');
                }
                // Use EFP result if valid, otherwise pos-match if close enough
                let finalCell = efpCell || (bestDist < scrollerContainer.clientWidth / 2 + 50 ? best : null);
                return finalCell;
            };
            // ── UPDATE REVISION ──
            if (incrementValue > 0) {
                let revisionHeader = allGridHeaders.find(h => { let t = getHeaderText(h).toLowerCase().trim(); return (t.includes('revision') || t.startsWith('rev')) && !t.includes('date'); });
                if (revisionHeader) {
                    let revCell = await scrollToColumn(revisionHeader);
                    if (revCell) {
                        let existingInput = revCell.querySelector('input:not([type="checkbox"]), select, textarea');
                        let cellValue = existingInput ? (existingInput.value || '').trim() : (revCell.innerText || revCell.textContent || '').trim();
                        if (cellValue.toLowerCase() === 'select date') cellValue = '';
                        if (cellValue) {
                            let numericStr = cellValue.replace(/[^0-9.]/g, '');
                            if (numericStr) {
                                let floatVal = parseFloat(numericStr);
                                let calculatedVal = floatVal + incrementValue;
                                let finalStringVal = calculatedVal.toFixed(2).toString();
                                let newVal = cellValue.replace(numericStr, finalStringVal);
                                if (!existingInput || existingInput.getBoundingClientRect().width === 0) {
                                    revCell.scrollIntoView({ behavior: 'instant', block: 'center', inline: 'center' });
                                    revCell.click();
                                    await new Promise(r => setTimeout(r, 300));
                                    existingInput = revCell.querySelector('input:not([type="checkbox"]), textarea') || document.activeElement;
                                }
                                if (existingInput && (existingInput.tagName === 'INPUT' || existingInput.tagName === 'TEXTAREA')) {
                                    await typeText(existingInput, newVal);
                                    revisionUpdated = true; revisionOldVal = numericStr; revisionNewVal = finalStringVal;
                                } else { results.push('[!] Revision input not accessible after click'); }
                            }
                        }
                    } else { results.push('[!] Revision cell not visible after scroll'); }
                } else { results.push('[!] Revision header not found in grid'); }
            }
            // ── UPDATE COLUMN FIELDS (key-centric) ──
            for (let key of Object.keys(columnConfig)) {
                let matchingHeader = allGridHeaders.find(h => { let t = getHeaderText(h).toLowerCase().trim(); return t === key.toLowerCase() || t.includes(key.toLowerCase()); });
                if (!matchingHeader) { results.push('[!] Header not found for: ' + key); continue; }
                let cell = await scrollToColumn(matchingHeader);
                if (!cell) { results.push('[!] Cell not found in viewport for: ' + key); continue; }
                let updated = await updateCell(cell, columnConfig[key], key);
                if (updated) columnsUpdated[key] = true;
            }
            if (scrollerContainer) doScroll(scrollerContainer, 0);
            results.push('[+] Step 2 completed: Metadata analysis & updates done');
    return { revisionUpdated, revisionOldVal, revisionNewVal, columnsUpdated, allDetectedHeaders };
}
