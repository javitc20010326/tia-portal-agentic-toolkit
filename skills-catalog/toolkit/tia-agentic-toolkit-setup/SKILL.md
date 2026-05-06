---
name: tia-agentic-toolkit-setup
description: Install and configure the TIA Portal Agentic Toolkit for Codex, including MCP server build, global config, skills links, and Openness validation. Use when the user asks to set up, update, repair, or validate this toolkit.
---

# TIA Agentic Toolkit Setup

Follow this order:

1. Confirm Windows, .NET 8, TIA Portal, and Openness prerequisites.
2. Build `src/TiaPortalAgenticToolkit.McpServer`.
3. Add `[mcp_servers.tia_portal]` to `~/.codex/config.toml`.
4. Link skills under `skills-catalog` into `~/.agents/skills`.
5. Restart Codex.
6. Run `tia_environment_status`.

If TIA Portal is not installed on the current machine, still build the server and explain that Openness tools will activate on the TIA Portal engineering workstation.
