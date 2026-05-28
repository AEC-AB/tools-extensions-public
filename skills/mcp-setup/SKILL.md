## name: mcp-setup
description: Use this skill when the `extension-docs` assistant docs MCP tool is missing or when the agent runtime does not have the assistant MCP server configured


# MCP setup

Use this skill when the `extension-docs` assistant docs MCP tool is missing or when the agent runtime does not have the assistant MCP server configured.

## Restore the assistant MCP server

1. Find the MCP client configuration used by your agent framework or editor.
2. Ensure it defines an `assistant` server entry.
3. If the server entry is missing, recreate it from this repo's `.vscode/mcp.json` or copy equivalent values into your framework's MCP configuration format.
4. Confirm the server launches `assistant` with args `[`"mcp"`]`.
5. Reload or restart the agent runtime after updating MCP configuration if required.

## Required server values

- Server name: `assistant`
- Transport: `stdio`
- Command: `assistant`
- Args: `mcp`

## Why this matters

The assistant MCP server exposes the `extension-docs` tool, which gives current guidance for building Assistant extensions.

