# Add Shared Parameters

## Description
The Add Shared Parameters extension automates the process of adding and managing shared parameters in Revit models. Shared parameters are custom data fields that maintain consistency across multiple projects and can be used in schedules, tags, and filters. This extension allows you to add multiple shared parameters at once, update existing parameters, merge duplicates, and automatically create schedules with the selected parameters.

This extension is particularly useful when standardizing project parameters across teams, implementing company standards, or migrating parameter definitions to new projects.

## Configuration

- **Shared Parameter file**: Path to the shared parameter text file (.txt) that contains the parameter definitions. You can browse to select the file. The extension supports environment variables in the path (e.g., `%USERPROFILE%\Documents\SharedParams.txt`).

- **Parameters to insert**: List of parameter names from the shared parameter file that you want to add to the model. Select one or more parameters from the list. The list is automatically populated from the shared parameter file you specified.

- **Parameter group**: The group under which the parameters will be organized in Revit's parameter dialogs (e.g., Identity Data, Dimensions, Construction). This helps organize parameters logically. Default is "Identity Data".

- **Change Parameter group**: When enabled (default: Yes), the extension will update the parameter group for parameters that already exist in the model to match your selected group. If disabled, existing parameters keep their current group assignment.

- **Binding type**: Determines whether parameters are attached to type or instance level:
  - **Instance**: Parameters vary per individual element (e.g., each door can have different values)
  - **Type**: Parameters are shared across all elements of the same type (e.g., all doors of type "A" share the same value)
  
  Default is "Instance".

- **Change binding type**: When enabled (default: No), the extension will change the binding type of existing parameters to match your selection. This requires deleting and reinserting the parameter, which will preserve existing values for elements in the specified categories.

- **Groups**: Controls how parameter values behave across group instances:
  - **Vary**: Each group instance can have different parameter values (default)
  - **Aligned**: All instances of a group share the same parameter value

- **Categories**: List of Revit categories (e.g., Walls, Doors, Windows) that the parameters should be added to. Selected categories will have access to these parameters. If not specified when adding new parameters, the extension will fail.

- **Reset categories**: When enabled (default: No), replaces all category assignments on existing parameters with only the categories you specified. Use with caution as this removes the parameter from any categories not in your list.

- **Remove categories**: List of specific categories to remove from existing parameter bindings. This allows you to selectively remove parameters from certain categories while keeping them on others.

- **Replace parameter**: Options to handle parameter definition changes in the shared parameter file:
  - **Name**: Updates the parameter name if it was changed in the shared parameter file
  - **Type**: Updates the parameter data type (e.g., Text to Number) if it was changed in the shared parameter file
  
  Selecting either option triggers deletion and reinsertion of the parameter with value preservation for elements in the specified categories.

- **Merge parameters**: When enabled (default: No), automatically merges duplicate parameters with the same name into a single parameter. Values from all duplicates are preserved by transferring them to the merged parameter. This is useful for cleaning up models with parameter conflicts.

- **Reinsert parameter**: When enabled (default: No), forces deletion and reinsertion of the parameter even if no changes are detected. Use this when you've modified other aspects of the parameter definition in the shared parameter file (like GUID) that aren't automatically detected.

- **Schedule name**: Optional. If provided, the extension will automatically create a new schedule containing all the parameters you added. The schedule will include all categories specified in the Categories setting. Leave blank to skip schedule creation.

## Functionality

### Description
The Add Shared Parameters extension performs the following operations:

1. **Validation**: Verifies the shared parameter file exists and that at least one parameter is selected
2. **Parameter Loading**: Opens the shared parameter file and locates the definitions for selected parameters
3. **Parameter Processing**: For each selected parameter:
   - **New Parameters**: Creates a new binding to the specified categories with the configured binding type and parameter group
   - **Existing Parameters**: Updates the parameter based on your configuration:
     - Adds/removes categories as specified
     - Changes parameter group if enabled
     - Changes binding type if enabled
     - Replaces parameter name/type if the definition changed in the file
     - Preserves parameter values for elements in the resulting category list during updates
     - Skips parameters bound only at the family level to avoid conflicts
4. **Duplicate Merging**: If enabled, identifies and merges duplicate parameters with the same name, consolidating all values
5. **Schedule Creation**: If a schedule name is provided, creates a new schedule with all selected parameters and specified categories
6. **Value Restoration**: After any deletion and reinsertion operations, restores original parameter values only to elements that belong to the resulting category list, maintaining data integrity while preventing incorrect value assignments to elements outside the scope

### How to Use

1. **Prerequisites**:
   - Have an active Revit model open
   - Have a shared parameter file (.txt) prepared with the parameters you want to add
   - Know which categories need these parameters (Walls, Doors, etc.)

2. **Configuration**:
   - Browse to select your shared parameter file path
   - From the auto-populated list, select the parameter names you want to add
   - Choose the parameter group for organization
   - Select Instance or Type binding based on your needs
   - Choose the categories that should have these parameters
   - (Optional) Configure advanced options:
     - Enable "Change parameter group" if updating existing parameters
     - Enable "Merge parameters" if you have duplicates to clean up
     - Enter a "Schedule name" if you want an automatic schedule created
     - Select "Replace parameter" options if definitions changed in your file

3. **Execution**:
   - Run the extension through Assistant
   - The extension will process each parameter sequentially
   - A transaction is created to ensure all changes can be rolled back if errors occur

4. **Verification**:
   - Review the results summary showing how many parameters were added/updated
   - Check for any warnings about parameters not found or issues during processing
   - If a schedule was created, open it to verify the parameters appear correctly
   - Select an element from one of the configured categories and verify the parameters appear in the Properties panel

## Troubleshooting

### Issue 1: "Could not find shared parameter file"
- **Causes**: The file path is incorrect, the file was moved/deleted, or environment variables aren't resolving correctly
- **Solution**: 
  - Use the Browse button to select the file directly
  - Verify the file exists at the specified location
  - If using environment variables like %USERPROFILE%, ensure they're correctly formatted
  - Check file permissions to ensure Revit can access the file
- **Resources**: Verify your shared parameter file is a valid .txt file exported from Revit

### Issue 2: "No parameters to insert is found in the shared parameter file"
- **Causes**: The parameter names selected don't match any definitions in the shared parameter file
- **Solution**:
  - Open the shared parameter file in a text editor and verify the parameter names
  - Parameter names are case-sensitive and must match exactly
  - Refresh the parameter list by reselecting the shared parameter file
- **Resources**: Review Revit's shared parameter file format documentation

### Issue 3: "Trying to add a new shared parameter without any categories"
- **Causes**: Attempting to create a new parameter binding without specifying which categories should have the parameter
- **Solution**: 
  - Select at least one category in the "Categories" configuration field
  - If updating an existing parameter, you can leave categories empty (they'll stay as-is)
- **Resources**: N/A

### Issue 4: Parameters added but some values are missing
- **Causes**: Values are only restored to elements that belong to the categories specified in the resulting category list
- **Solution**:
  - This is expected behavior to prevent incorrect value assignments
  - Ensure all necessary categories are included in your category list
  - If you've changed parameter names, values will only restore to elements in the specified categories, not to families or elements outside the scope
- **Resources**: Review which categories are included in your configuration

### Issue 5: "Failed to merge parameters, duplicate parameters found"
- **Causes**: The merge operation encountered parameters that couldn't be automatically consolidated
- **Solution**:
  - Review the duplicate parameters manually in Revit
  - Check if GUIDs match between the duplicates
  - Try removing one duplicate parameter manually before running the extension
- **Resources**: Consult Autodesk documentation on shared parameter GUIDs

### Issue 6: Parameter skipped with "bound only at the family level" warning
- **Causes**: The parameter exists only in family documents and not at the project level
- **Solution**:
  - This is expected behavior to avoid conflicts with family parameters
  - Family-level parameters must be managed within the family editor
  - If you need the parameter at project level, add it with a different name or remove it from families first
- **Resources**: Review Autodesk documentation on family parameters vs. project parameters

## FAQ

- **Q: When should I use Instance vs Type binding?**
  - **A: Use Instance binding when individual elements need unique values (e.g., "Installation Date" might differ per door). Use Type binding when all elements of the same type should share values (e.g., "Fire Rating" is the same for all doors of type "Fire Door A"). Instance parameters are more flexible but Type parameters help maintain consistency.**

- **Q: What happens to existing parameter values when I change the binding type or parameter name?**
  - **A: The extension preserves values during deletion and reinsertion, but only for elements that belong to the categories specified in your configuration. This prevents incorrect value assignments to elements outside the intended scope. For example, if you rename a parameter and only include "Walls" in your category list, values will be restored to walls but not to other element types that may have had the old parameter.**

- **Q: Can I add parameters to schedules in other categories beyond the ones I selected?**
  - **A: No, the automatically created schedule will only include the categories specified in the "Categories" field. However, you can manually edit the schedule after creation to add additional categories if supported by Revit.**

- **Q: What does "Varies across groups" mean?**
  - **A: This setting controls parameter behavior in Revit groups. "Vary" allows each group instance to have different parameter values (useful for location-specific data). "Aligned" means all group instances share the same value (useful for consistent data like part numbers).**

- **Q: What happens if I select "Replace parameter" for both Name and Type?**
  - **A: The extension will delete the old parameter and create a new one with both the updated name and data type from the shared parameter file. Values will be preserved for elements in the specified categories if the data types are compatible (e.g., Text to Text). If data types are incompatible (e.g., Number to Text), values will be converted where possible or cleared if conversion isn't possible.**

- **Q: Can I run this extension on multiple models at once?**
  - **A: The extension operates on the active Revit document. To process multiple models, you would need to run it separately for each model or use Assistant's batch processing capabilities if available.**

- **Q: Why are some of my values not being restored after a parameter update?**
  - **A: Values are only restored to elements that belong to the categories in your resulting category list. This is by design to prevent incorrect value assignments. For instance, if you had a parameter on both Walls and Doors, but your update only includes Walls in the category list, only wall values will be restored.**

## Support

For assistance or to report issues:
- **Team Support**: Contact the Tools development team

## Version History

- **Version 0.1.3 - Current**
  - Fixed value restoration bug: Values are now only restored to elements within the resulting category list, preventing incorrect assignments to elements outside the intended scope (e.g., families with old shared parameter names)
  - Added support for skipping parameters bound only at family level to avoid conflicts
  - Improved warning messages for better user feedback

- **Version 0.1.2**
  - Enhanced error handling and validation
  - Comprehensive parameter management capabilities
  - Support for parameter merging and duplicate resolution
  - Automatic schedule creation
  - Category management (add/remove/reset)
  - Binding type and parameter group modification
  - Support for parameter definition changes (name/type replacement)

---

*This documentation was generated based on the extension's code structure.*