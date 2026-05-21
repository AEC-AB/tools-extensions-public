# Extension Development Documentation

Welcome to the comprehensive guide for building Assistant extensions. This documentation covers everything from your first extension to advanced patterns and platform-specific integration.

## Quick Navigation

**New to Extension Development?**
→ Start with [Quick Start Guide](./dotnet/QUICK_START.md) (5-10 minutes)

**Building Your First Extension?**
→ See [Cookbook: Common Patterns](./dotnet/COOKBOOK.md) for copy-paste ready examples

**Need Deep Technical Reference?**
→ Read [Args Developer Guide](./dotnet/ARGS_DEVELOPER_GUIDE.md) for complete lifecycle, validation, features

**Looking Up Field Types or DSL Syntax?**
→ Check [Reference](./dotnet/REFERENCE.md) for quick lookup tables and detailed narratives

**Working with a Specific Platform?**
- [Assistant Extensions](./dotnet/PLATFORM_GUIDES/ASSISTANT.md)
- [Revit Extensions](./dotnet/PLATFORM_GUIDES/REVIT.md)
- [AutoCAD Extensions](./dotnet/PLATFORM_GUIDES/AUTOCAD.md)
- [Tekla Extensions](./dotnet/PLATFORM_GUIDES/TEKLA.md)
- [Navisworks Extensions](./dotnet/PLATFORM_GUIDES/NAVISWORKS.md)

## Choose Extension Type First

Use this quick rule before you pick a template:

- **Assistant Extension**: Use when your extension does not need Revit/AutoCAD/Tekla/Navisworks host APIs.
	Typical examples: integrations with external services/tools such as StreamBIM, INFRA, Excel, REST APIs, file processing, and orchestration logic.
- **Host-specific Extension**: Use Revit/AutoCAD/Tekla/Navisworks extension templates when the extension must access that host application's model, document, selection, or UI APIs.

For Revit and Tekla specifically, see the platform guides for when to choose **Automation Extension** vs **App Extension** templates.

---

## Documentation Structure

### `.NET Extension Documentation`

| Document | Audience | Purpose |
|----------|----------|---------|
| [Quick Start](./dotnet/QUICK_START.md) | All developers | Get a working extension in 5-10 minutes |
| [Args Developer Guide](./dotnet/ARGS_DEVELOPER_GUIDE.md) | All developers | Complete reference on configuration classes and UI binding |
| [Cookbook](./dotnet/COOKBOOK.md) | All developers | Real-world patterns ready to copy-paste |
| [Reference](./dotnet/REFERENCE.md) | All developers | Quick lookup tables, DSL syntax, validation rules |
| [Platform Guides](./dotnet/PLATFORM_GUIDES/) | Integration-specific developers | Host API context, transaction patterns, platform-specific behaviors |

### Platform-Specific Guides

- **[ASSISTANT.md](./dotnet/PLATFORM_GUIDES/ASSISTANT.md)** — Assistant-specific extensions, variable binding, execution context
- **[REVIT.md](./dotnet/PLATFORM_GUIDES/REVIT.md)** — Revit transactions, API context, document access patterns
- **[AUTOCAD.md](./dotnet/PLATFORM_GUIDES/AUTOCAD.md)** — AutoCAD database transactions, document state, integration patterns
- **[TEKLA.md](./dotnet/PLATFORM_GUIDES/TEKLA.md)** — Tekla Model API, selector patterns, transaction behavior
- **[NAVISWORKS.md](./dotnet/PLATFORM_GUIDES/NAVISWORKS.md)** — Navisworks application context, model navigation, selection patterns

---

## Core Concepts at a Glance

### Args Class = Configuration UI
Your `*Args.cs` class defines what users configure in the Assistant UI. Decorate properties with field attributes (`[TextField]`, `[IntegerField]`, etc.) and validation attributes (`[Required]`, `[Range]`, etc.). When the extension runs, the Args instance is hydrated with user input and passed to your `*Command.cs`.

### Command Class = Execution Logic
Your `*Command.cs` implements `IAssistantExtension` (or platform-specific interface) and receives the hydrated Args. This is where you read/write model data, trigger operations, and return results.

### UI Rendering
The Args attributes are rendered by the Assistant Forms library into a configuration form. Field visibility and enable state can be controlled with conditional DSL expressions. Groups organize related fields. Validation runs on user input.

### Versioning & Upgrades
As your Args evolve, use `[ArgsVersion]` and implement `IArgsUpgrade<TFrom, TTo>` to migrate persisted configurations automatically. The framework chains upgrades and handles version tracking.

---

## Learning Path

1. **Understand the Pattern** → Read "Core Concepts at a Glance" above
2. **See It Working** → Follow [Quick Start](./dotnet/QUICK_START.md) with a working example
3. **Explore Patterns** → Browse [Cookbook](./dotnet/COOKBOOK.md) for your use case
4. **Deep Dive** → Study [Args Developer Guide](./dotnet/ARGS_DEVELOPER_GUIDE.md) as needed
5. **Reference** → Use [Reference](./dotnet/REFERENCE.md) to look up syntax or behavior
6. **Platform-Specific** → Check relevant [Platform Guide](./dotnet/PLATFORM_GUIDES/) for host API details

---

## Key Resources

- **Living Reference**: All examples in this documentation are drawn from or traceable to [`AssistantDemoExtension`](../src/Assistant/dotnet/AssistantDemoExtension/AssistantDemoExtensionArgs.cs)
- **Authoritative Source**: Field attribute contracts are defined in [`CW.Assistant.Extensions.Contracts.Fields`](../../tools/src/platform/Assistant/CW.Assistant.Extensions.Contracts/Fields)
- **Rendering Pipeline**: Forms are rendered by `Assistant by AEC`
- **Instruction Files**: For Copilot agent scoping and IDE support, see [`.github/instructions/`](./.github/instructions/)

---

## Contributing to This Documentation

These docs are living guides. When you:
- **Discover a bug or unclear section** → Open an issue with the page name and section
- **Find an example pattern that should be in the Cookbook** → Contribute via PR
- **Add a new feature to Args** → Update the relevant guide and add an example to the Cookbook or Quick Start

Keep examples working and linked to `AssistantDemoExtension`. See [Governance](#governance) below.

---

## Governance: Documentation Updates

**When to update these docs:**

| Event | Update | Section |
|-------|--------|---------|
| New field attribute added to `CW.Assistant.Extensions.Contracts.Fields` | Add row to Reference; optionally add Cookbook pattern | REFERENCE.md, COOKBOOK.md |
| New feature (visibility DSL, grouping, versioning) ships | Add feature section to Args Developer Guide; add example | ARGS_DEVELOPER_GUIDE.md, COOKBOOK.md |
| `AssistantDemoExtension` gets new field examples | Update QUICK_START and Cookbook examples to match | QUICK_START.md, COOKBOOK.md |
| Platform-specific behavior changes or additions | Update relevant Platform Guide | PLATFORM_GUIDES/*.md |
| Common developer questions emerge | Add Q&A section or Cookbook entry | COOKBOOK.md or ARGS_DEVELOPER_GUIDE.md |

**Maintenance cadence:**
- Quarterly review of Quick Start and Cookbook examples against actual working extensions
- Update Platform Guides when integration-specific APIs or patterns change
- Backlog developer questions and batch them into Cookbook or Q&A sections

---

**Next Step:** Ready to start? → [Quick Start Guide](./dotnet/QUICK_START.md)
