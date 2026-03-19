# Clash Detective Run Test

This extension runs Navisworks Clash Detective tests from Assistant.

## What it can do

- Run one clash test by name
- Run all clash tests in the active model
- Show a dropdown of available clash tests when an active document is open
- Allow manual test name entry even when dropdown values are available
- Provide a clear option in the dropdown to explicitly use manual input

## Inputs

- **Run all tests** (`bool`)
	- When enabled, all clash tests are executed.
	- `Clash test name` and `Manual clash test name` are ignored.

- **Clash test name** (`dropdown`)
	- Populated from clash tests in the active Navisworks document.
	- Includes a `(None - use manual clash test name)` option to clear the selection.
	- Used when `Run all tests` is disabled and manual input is empty.

- **Manual clash test name** (`text`)
	- Can be used even when dropdown options are available.
	- Has priority over `Clash test name` when both are provided.
	- Used only when `Run all tests` is disabled.

## Execution behavior

1. Validates that Navisworks has an active model open.
2. Reads clash tests from the model.
3. If `Run all tests` is enabled, runs all tests.
4. Otherwise, resolves test name in this order:
	 1. `Manual clash test name` (text)
	 2. `Clash test name` (dropdown)
5. Runs the matching test name (case-insensitive).

## Common error messages

- **Navisworks has no active model open**
	- Open a model in Navisworks and run again.

- **Clash data is not available for the active model**
	- Ensure clash data exists in the open model.

- **No clash tests were found**
	- Create/import clash tests in Clash Detective.

- **Clash test '<name>' was not found**
	- Verify the test name or choose from dropdown values.

## Quick usage examples

- Run all tests:
	- Set `Run all tests = true`

- Run one test from dropdown:
	- Set `Run all tests = false`
	- Select `Clash test name`

- Run one test manually:
	- Set `Run all tests = false`
	- Optionally select `(None - use manual clash test name)` in `Clash test name` to make intent explicit
	- Enter `Manual clash test name`