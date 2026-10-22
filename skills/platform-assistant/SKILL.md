## name: platform-assistant
description: Use this skill when editing `*Command.cs`, collector code, or Assistant-specific runtime logic


# Platform guide

Use this skill when editing `*Command.cs`, collector code, or Assistant-specific runtime logic.

## Load these docs first

Use `../docs-routing/SKILL.md` to resolve `ExtensionDocsRoot`, then read `PLATFORM_GUIDES/ASSISTANT.md`. Search that file for topics such as `variables` or `dry run` when you need a narrower example.

## Platform scope

Use this platform for:

- desktop automation
- file transformation workflows
- REST and cloud API orchestration
- integration logic that reads or writes Assistant variables

If the implementation must access a host CAD/BIM model, selection, or UI API, switch to a host-specific platform skill.

## Platform rules

- Implement commands through `IAssistantExtension<TArgs>.RunAsync(IAssistantExtensionContext, TArgs, CancellationToken)`.
- Validate file paths, URLs, and external identifiers before execution.
- Keep logic host-agnostic and testable.
- Use Assistant variables only for small workflow state, not as a substitute for durable storage.
- Return actionable failure messages that tell the user what to check next.

