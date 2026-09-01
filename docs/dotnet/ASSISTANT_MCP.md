# Assistant MCP for extension development

The Assistant MCP server provides project creation, inspection, execution, registration, publishing, and operation-discovery tools. It complements the offline extension docs. It does not replace `ExtensionDocsRoot` for versioned guidance.

## Configure the server

Add this server to the active agent framework's MCP configuration:

```json
{
  "servers": {
    "assistant": {
      "type": "stdio",
      "command": "assistant",
      "args": ["mcp"]
    }
  }
}
```

Use these tools when their named task applies. Inspect metadata or operation schemas before executing or publishing.

## Project creation

| Tool | Use |
| --- | --- |
| `mcp_cw_assistant__extensions-get-available-templates` | List extension templates before choosing a scaffold. |
| `mcp_cw_assistant__extensions-create-from-template` | Create an extension project from a selected template. |

## Project understanding and local testing

| Tool | Use |
| --- | --- |
| `mcp_cw_assistant__extensions-get-project-metadata` | Read a local project's metadata, schema, and expected arguments. |
| `mcp_cw_assistant__extensions-run-project` | Run a local extension project with structured inputs. |
| `mcp_cw_assistant__run-csharp-script` | Run a C# script in Revit on the Revit UI thread against the active document. |

## Registered extensions

| Tool | Use |
| --- | --- |
| `mcp_cw_assistant__extensions-search` | Search available extension packages for reuse or examples. |
| `mcp_cw_assistant__extensions-get-metadata` | Read metadata for a registered extension by ID. |
| `mcp_cw_assistant__extensions-run` | Run a registered extension by ID. |
| `mcp_cw_assistant__extension-register-from-project` | Register a private extension from a local project for testing. |
| `mcp_cw_assistant__extension-publish-from-project` | Build, version, and publish a local extension project. |

## Typed operations

| Tool | Use |
| --- | --- |
| `mcp_cw_assistant__search_operations` | Find typed operations for a capability. |
| `mcp_cw_assistant__get_operations` | Read selected operation schemas, inputs, outputs, and relationships. |
| `mcp_cw_assistant__run_operation` | Run an inspected operation with a JSON configuration payload. |

## Safe workflow

1. Use `extensions-get-available-templates` before creating a project.
2. Use `extensions-get-project-metadata` before running a local project.
3. Use `extensions-run-project` for local validation.
4. Use `extension-register-from-project` before exercising a private registered extension.
5. Use `extension-publish-from-project` only when the extension is ready to distribute.

For Revit API signatures and other host integration APIs, use `dotnet-inspect` against the resolved assembly. The Assistant MCP is for Assistant extension lifecycle and live Revit script execution, not a replacement for host API reference.
