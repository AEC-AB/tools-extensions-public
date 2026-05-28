# Extension Templates

Use `dotnet new` templates to scaffold Assistant extensions and automation scripts with the expected structure, configuration files, and build setup.

---

## Install Templates

```bash
dotnet new install CW.Assistant.Extensions.Templates
```

---

## List Available Templates

```bash
dotnet new list cw
```

---

## Create a Project

Example:

```bash
dotnet new cwrvtae -n MyRevitAutomationExtension
```

This creates a new Revit automation extension project named `MyRevitAutomationExtension`.

---

## Included Template Short Names

- `cwae` - Assistant Automation Extension
- `cwacadae` - AutoCAD Automation Extension for Assistant
- `cwnwae` - Navisworks Automation Extension for Assistant
- `cwrvtapp` - Revit App Extension for Assistant
- `cwrvtae` - Revit Automation Extension for Assistant
- `cwtapp` - Tekla App Extension for Assistant
- `cwtae` - Tekla Automation Extension for Assistant
- `cwpyas` - Assistant Python Automation Script
- `cwpyrvtas` - Revit Python Automation Script

---

## Template Selection Guide

- Choose `cwae` when no host API (Revit/AutoCAD/Tekla/Navisworks) is needed.
- Choose host-specific automation templates (`cwacadae`, `cwnwae`, `cwrvtae`, `cwtae`) when you need host model/document APIs.
- Choose app templates (`cwrvtapp`, `cwtapp`) for interactive app-style extensions.
- Choose script templates (`cwpyas`, `cwpyrvtas`) for Python-based workflows.

---

## After Scaffolding

1. Update Args fields and labels for your use case.
2. Implement command/app behavior.
3. Add user-facing extension root README using [Writing Extension README Help Files](../WRITING_EXTENSION_README_HELP_FILES.md).
4. Validate against the relevant platform guide in `PLATFORM_GUIDES`.
