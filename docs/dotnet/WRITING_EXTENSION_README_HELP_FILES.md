# Writing Extension README Help Files

When developing an extension, the `README.md` in the extension root should work as a user guide for engineers and architects, not as internal developer notes.

Use this guide to document what the extension does, how it is configured, and how users can troubleshoot common issues.

---

## Purpose of the Root README

Your extension root `README.md` should help users answer:

1. What does this extension do?
2. When should I use it?
3. How do I configure each field?
4. What happens when I run it?
5. What can go wrong and how do I fix it?

---

## Required Sections

Use this structure in the extension root `README.md`:

1. `# Extension Title`
2. `## Description`
3. `## Configuration`
4. `## Functionality`
5. `## Troubleshooting`
6. `## FAQ`
7. `## Resources`
8. `## Version History`

---

## What to Include

### Description

- Plain-language summary of business value.
- Host context (Assistant, Revit, Tekla, AutoCAD, Navisworks).
- Typical use cases and expected outcomes.

### Configuration

For each Args property, explain:

- Display label users see in the UI.
- Expected input format and valid ranges.
- Default value (if relevant).
- Units (mm, m, ft, degrees, percent, etc.) when applicable.
- Effect on execution.

### Functionality

- Step-by-step behavior of `Run` or `RunAsync` in user language.
- Preconditions (for example: active model, selected objects, required files).
- Output or side effects (created files, updated model data, reports).

### Troubleshooting

For each common issue:

- Symptoms users will notice.
- Likely causes.
- Resolution steps.
- Any known limitations.

### FAQ

- Questions about when to use the extension.
- Questions about configuration choices and limits.

### Version History

- Start with initial version.
- Add short, user-facing change notes per release.

---

## Template

```md
# Extension Title

## Description
[Brief overview of what the extension does and why users should use it]

## Configuration

- **Setting 1**: [What it controls, accepted values, default, units]
- **Setting 2**: [What changes when this value changes]

## Functionality

### Description
[Explain execution flow in plain language]

### How to Use
1. [Prerequisites]
2. [Configure fields]
3. [Run extension]
4. [Verify results]

### Visual Aids
[Add screenshots or GIFs that show configuration and result]

## Troubleshooting

### Issue 1: [Short problem name]
- **Causes**: [Likely causes]
- **Solution**: [Resolution steps]

### Issue 2: [Short problem name]
- **Causes**: [Likely causes]
- **Solution**: [Resolution steps]

## FAQ

- **Q: [Common user question]**
  - **A:** [Clear answer with practical context]

## Resources

- [Related internal docs]
- [Platform-specific guide]

## Version History

- **Version 0.0.1 - [YYYY-MM-DD]**
  - Initial release
```

---

## Quality Checklist

Before publishing your `README.md`, verify:

1. Language is user-focused (minimal implementation jargon).
2. Every visible Args field is documented.
3. Units and limits are stated where relevant.
4. Troubleshooting includes concrete, actionable fixes.
5. Content matches current extension behavior.
