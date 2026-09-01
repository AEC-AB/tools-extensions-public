## name: args-evolution
description: Use this skill when editing `*Args.cs` in any integration under this repository


# Args evolution

Use this skill when editing `*Args.cs` in any integration under this repository.

## Load these docs first

Use `../docs-routing/SKILL.md` to resolve `ExtensionDocsRoot`, then read `ARGS_DEVELOPER_GUIDE.md`. Search that file for `version` and `upgrade` when changing a persisted shape.

## Upgrade rules

Newly generated, unshipped Args classes are exempt from the production-use
question and upgrade ceremony: make structural changes directly without a
version bump or upgrade mapping.

For an existing or production Args class, ask the user whether the Args class
is already used in production workflows before proceeding.

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

