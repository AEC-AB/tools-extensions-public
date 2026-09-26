---
name: repo
type: repo
agent: CodeActAgent
---

# Repository instructions for OpenHands

This repository carries generated, agent-maintained folders (source of the agents: AEC.Nvidia.LLM repo, `services/aec.agents`):

- `Ai.Documentation/` - wiki-style documentation, maintained by the documentation agent.
- `Ai.Security/` - security review findings, maintained by the security review agent. Report only, never fixes.

These instructions apply to every OpenHands session in this repo, interactive or headless.

## Rules

- Agent output goes in its own folder only and is delivered as pull requests, never pushed to the default branch.
- Never read or copy secrets: `.env`, `*.env`, `local.settings.json`, `appsettings.*.json`, `*.bacpac`, certificates, exports (`*.xlsx`, `*.csv`). Write `[redacted]` if one is seen.
- Work items are written `WI 1234`, never `#1234`.
- ASCII filenames with hyphens, one `#` title as the first line of every page, `.order` files control navigation.
- Each folder's `.agent/learnings.md` holds standing instructions from reviewers. Read it before writing. Never delete a human-written line.
- Do not build, test, install or run services when documenting or reviewing. Read the code.

## Repository facts

(Filled in by the documentation agent on its first run. Keep to stable facts, at most 15 lines.)
