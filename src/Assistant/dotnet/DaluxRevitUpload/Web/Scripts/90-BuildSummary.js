function buildAutomationSummary(state, results) {
    const {
        target,
        incrementValue,
        actionButtonText,
        revisionUpdated,
        revisionOldVal,
        revisionNewVal,
        columnsUpdated,
        allDetectedHeaders,
        columnConfig
    } = state;

    results.push('\n=== AUTOMATION SUMMARY ===');
    results.push('File : ' + target);
    results.push('Headers detected across all scroll positions: ' + Array.from(allDetectedHeaders).join(' | '));
    results.push('');
    results.push('Metadata updates:');
    if (incrementValue > 0) {
        if (revisionUpdated)
            results.push('  [+] Revision          : ' + revisionOldVal + ' → ' + revisionNewVal);
        else
            results.push('  [!] Revision          : NOT updated (column not found or no value)');
    }
    for (let key of Object.keys(columnConfig)) {
        if (columnsUpdated[key])
            results.push('  [+] ' + key + ' : ' + columnConfig[key]);
        else
            results.push('  [!] ' + key + ' : NOT updated (column not found)');
    }
    results.push('');
    results.push('Steps completed:');
    results.push('  [+] Popup loaded & file found');
    results.push('  [+] Metadata updated');
    if (actionButtonText)
        results.push('  [+] Action button clicked: ' + actionButtonText);
    results.push('\n[+] Automation completed successfully.');

    return results.join('\n');
}
