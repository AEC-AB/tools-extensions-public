## name: docs-routing
description: Replace with description of the skill and when Claude should use it.

# Insert skill instructions below

# Docs routing

Use this skill when you need extension framework guidance before changing code in this repository.

If the `extension-docs` assistant docs MCP tool is unavailable, use `../mcp-setup/SKILL.md` first to restore the assistant MCP server in the active agent framework.

## MCP entry points

Use the `extension-docs` MCP tool:

- `operation=index` to list available docs
- `operation=search` with focused queries such as `args versioning`, `autofill`, `transaction`, `document lock`, `current selection`, or `model connection`
- `operation=content` with document ids such as `quick-start`, `args-developer-guide`, `cookbook`, `reference`, `assistant`, `autocad`, `revit`, `navisworks`, or `tekla`

## Suggested reading order

1. `quick-start` for extension shape and execution model
2. Platform doc (`assistant`, `autocad`, `revit`, `navisworks`, or `tekla`) for runtime behavior
3. `args-developer-guide` when changing configuration classes
4. `cookbook` for reusable implementation patterns
5. `reference` for exact field syntax and validation rules
