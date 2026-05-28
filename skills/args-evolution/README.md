# Args evolution

Use this skill when editing `*Args.cs` in any integration under this repository.

## Load these docs first

Use the `extension-docs` MCP tool:

1. `operation=content` with document id `args-developer-guide`
2. `operation=search` with query `args versioning upgrades`
3. `operation=content` using the returned document id

## Upgrade rules

Before changing the `*Args.cs` structure, ask whether the Args class may already be used in production workflows.

If the answer is yes:

1. Add or bump `[ArgsVersion(N)]` on the current Args class.
2. Implement `IArgsUpgrade<TOldArgs, TNewArgs>` to map the old structure into the new one.
3. Preserve existing user data when fields are renamed, moved, split, or removed.
4. Use defaults only for truly new values, not as a replacement for migrated data.

## Collectors and field guidance

- Use async collector interfaces when values should come from host APIs or external systems.
- Keep field labels and descriptions user-focused and platform-accurate.
- Add validation close to field definitions so failures are actionable in UI.
