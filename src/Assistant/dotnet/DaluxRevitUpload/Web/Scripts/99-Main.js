(async function() {
    const results = [];
    try {
        const target = __TARGET_JSON__.toLowerCase();
        const incrementValue = parseFloat("__REVISION_INCREMENT__");
        const columnConfig = __COLUMN_CONFIG_JSON__;
        const actionButtonText = __ACTION_BUTTON_JSON__;

        const popupLoaded = await waitForDaluxPopup(results);
        if (!popupLoaded) return results.join('\n');

        await resetSelections(results);

        const targetRow = findTargetFile(target, results);
        if (!targetRow) return results.join('\n');

        const metadata = await updateMetadata(targetRow, target, incrementValue, columnConfig, results);
        await runActionButton(actionButtonText, results);

        return buildAutomationSummary({
            target,
            incrementValue,
            actionButtonText,
            columnConfig,
            ...metadata
        }, results);
    } catch (error) {
        results.push('\n[!] FATAL ERROR: ' + error.message);
        return results.join('\n');
    }
})();
