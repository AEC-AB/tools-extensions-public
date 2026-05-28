## name: args-evolution
description: Replace with description of the skill and when Claude should use it.

# Insert skill instructions below

# Args evolution

Use this skill when editing `*Args.cs` in any integration under this repository.

## Load these docs first

Use the `extension-docs` MCP tool:

1. `operation=content` with document id `args-developer-guide`
2. `operation=search` with query `args versioning upgrades`
3. `operation=content` using the returned document id

If the `extension-docs` MCP tool is unavailable or returns no results, proceed using only the rules in this document and inform the user that docs could not be loaded.

## Upgrade rules

Before changing the `*Args.cs` structure, ask the user whether the Args class is already used in production workflows before proceeding.

If the answer is yes:

1. Add or bump `[ArgsVersion(N)]` on the current Args class.
2. Implement `IArgsUpgrade<TOldArgs, TNewArgs>` to map the old structure into the new one.
3. Preserve existing user data when fields are renamed, moved, split, or removed.
4. Use defaults only for truly new values, not as a replacement for migrated data.

If the answer is no: make the structural change directly without adding a version bump or upgrade mapping.

## Collectors and field guidance

- Use async collector interfaces when values should come from host APIs or external systems.
- Keep field labels and descriptions user-focused and platform-accurate.
- Add validation close to field definitions so failures are actionable in UI.
