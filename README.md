# TIA Portal Agentic Toolkit

Experimental Codex toolkit for Siemens TIA Portal automation through TIA Portal Openness.

The goal is to give Codex an agentic interface similar in spirit to MATLAB/Simulink agentic toolkits:

- an MCP server that exposes safe engineering tools,
- Codex skills for PLC, HMI, safety, and setup workflows,
- global installation instructions for Codex,
- a clear security model for human-in-the-loop industrial automation.

This project is not affiliated with Siemens. TIA Portal, SIMATIC, WinCC, and related product names are trademarks of Siemens or their respective owners.

## Current Status

This is an initial scaffold.

Implemented:

- MCP stdio server in .NET 8.
- Environment detection for TIA Portal Openness registry keys.
- Windows group membership check for `Siemens TIA Openness`.
- MCP tools/list and tools/call support.
- Semi-agentic export analysis for XML/SCL/AWL/CSV/Excel folders.
- Codex plugin metadata and skills catalog.
- Multi-mode strategy: full agentic with Openness, semi-agentic with exports, advisory without TIA Portal.

Planned:

- Attach to running TIA Portal processes.
- Open project / project overview.
- Export PLC blocks, UDTs, DBs, tag tables, and HMI objects.
- Import XML/SimaticML with approval gates.
- Compile PLC software.
- Optional PLCSIM and WinCC Unified helpers.

## Requirements

- Windows for full agentic mode.
- TIA Portal with TIA Portal Openness installed for full agentic mode.
- User added to the local Windows group `Siemens TIA Openness` for full agentic mode.
- .NET 8 SDK or runtime.
- OpenAI Codex with MCP support.

Without Openness permissions, the toolkit should still work in semi-agentic mode over exported XML/SCL/CSV/Excel artifacts.

Siemens documents that TIA Portal Openness is an API for automating engineering workflows, and that access requires the local `Siemens TIA Openness` Windows group plus the TIA Portal Openness firewall prompt.

## Quick Start

Build the MCP server:

```powershell
dotnet build .\src\TiaPortalAgenticToolkit.McpServer\TiaPortalAgenticToolkit.McpServer.csproj
```

Add this MCP server to `C:\Users\<you>\.codex\config.toml`:

```toml
[mcp_servers.tia_portal]
command = 'C:\path\to\tia-portal-agentic-toolkit\src\TiaPortalAgenticToolkit.McpServer\bin\Debug\net8.0-windows\TiaPortalAgenticToolkit.McpServer.exe'
tool_timeout_sec = 600
env_vars = ['WINDIR', 'ProgramFiles', 'ProgramFiles(x86)', 'USERNAME', 'USERDOMAIN']
```

Restart Codex and ask:

```text
Check my TIA Portal Openness environment.
```

For semi-agentic mode, manually export TIA Portal blocks/tags/UDTs to a folder and ask:

```text
Analyze this TIA Portal export folder and document the project.
```

## Safety Model

The MCP server should default to read-only exploration. Any tool that can modify a TIA Portal project, compile, download, start/stop simulation, or touch hardware must require explicit user approval at the Codex layer and should create backups/export artifacts first.

Do not connect this toolkit to production PLCs without a reviewed change-control process.

## Repository Layout

```text
.codex-plugin/                         Codex plugin metadata
.agents/plugins/marketplace.json       Local marketplace metadata
skills-catalog/                        Codex skills
src/TiaPortalAgenticToolkit.McpServer  MCP stdio server
src/TiaPortalAgenticToolkit.Openness   Openness adapter layer
templates/                             Codex config examples
```

## MCP Tools

- `tia_capabilities`
- `tia_environment_status`
- `tia_analyze_export_folder`
- `tia_parse_block_xml`
- `tia_summarize_scl`
- `tia_generate_export_documentation`
- `tia_prepare_manual_import_checklist`
- `tia_attach_running_portal` (stub in v0.1)

## References

- Siemens TIA Portal Openness API documentation: https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows
- TIA Openness user group requirement: https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows/basics/installation/adding-users-to-the-siemens-tia-openness-user-group
- TIA Openness firewall: https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows/tia-portal-openness-api/general-functions/tia-portal-openness-firewall
