# RefreshReferenceModels Extension

## Overview
The RefreshReferenceModels extension automatically refreshes all reference models in the current Tekla Structures model. This is useful when the source files of reference models have been updated and you need to reload them into your project.

## Functionality
This extension:
- Scans the current Tekla model for all reference models
- Attempts to refresh each reference model from its source file
- Reports the success or failure status for each reference model

## How to use this Extension
1. Open your Tekla Structures model
2. Run the RefreshReferenceModels extension
3. The extension will automatically refresh all reference models
4. Review the results to see which models were successfully updated

## Results
The extension provides detailed feedback:
- **Success**: Lists each reference model that was successfully refreshed
- **Partial Success**: 
  - If no reference models are found in the model
  - If reference models exist but none could be updated
- **Updates per model**: Shows the filename and update status for each reference model

## Example Output
```
ModelA.ifc was updated
ModelB.ifc was updated
ModelC.ifc was not updated
```
