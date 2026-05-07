# TIA Portal Agentic Toolkit

Experimental Codex toolkit for Siemens TIA Portal automation through TIA Portal Openness, generated import packs, and desktop UI automation.

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
- Semi-agentic import-pack generation for PLC/HMI engineering artifacts.
- Axis-control starter pack generation: SCL UDTs, DB, FB, OB1 call example, suggested tag CSV, HMI plan, report, manifest, and manual import checklist.
- UI Agent Mode plan generation for machines with TIA Portal but without Openness permissions.
- Experimental PowerShell UI runner for TIA Portal desktop detection, project opening, window focus, import-pack preparation, and state capture.
- Neutral LAD/FBD/HMI template packs plus built-in experimental robot templates for fallback no-Openness workflows.
- Private export parsers for TIA Project Texts XLSX, TIA web-server HTML bindings, DB source files, and extracted PDF printout text.
- Codex plugin metadata and skills catalog.
- Multi-mode strategy: full agentic with Openness, UI-agent without Openness, semi-agentic with exports/import packs, advisory without TIA Portal.

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

If TIA Portal is installed but Openness is unavailable, the toolkit can use UI Agent Mode. This is desktop automation, not a Siemens engineering API. It can open/focus TIA Portal, prepare import packs, and provide a runner for guided/aggressive UI automation, but it is more brittle than Openness.

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

To generate a useful pack without Openness, ask:

```text
Generate a TIA Portal V16 axis-control import pack for Axis1 in this output folder.
```

This produces SCL, CSV, HMI planning, and checklist files. It does not modify `.ap16`/`.zap16` project internals and does not download to hardware.

To prepare no-Openness desktop automation, ask:

```text
Generate a TIA UI-agent plan for my V16 project and this import pack.
```

To generate LAD/FBD/HMI template specifications, ask:

```text
Generate a LAD/FBD/HMI template pack for Axis1 in TIA Portal V16.
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
- `tia_generate_axis_control_pack`
- `tia_generate_plc_tag_table_csv`
- `tia_generate_hmi_plan`
- `tia_generate_logic_template_pack`
- `tia_generate_ui_agent_plan`
- `tia_analyze_project_texts_xlsx`
- `tia_analyze_webserver_bindings`
- `tia_analyze_db_source`
- `tia_analyze_pdf_printout_text`
- `tia_attach_running_portal` (stub in v0.1)

## No-Openness Automation Strategy

There are two no-Openness paths:

- `Import Pack`: generate SCL/CSV/HMI documentation that can be imported or copied into TIA.
- `UI Agent Mode`: run a desktop automation plan against the visible TIA Portal application.

LAD, FBD, and HMI generation uses a template system. The repo can generate neutral logic/template files now. For reliable real TIA XML, provide seed exports from the same TIA Portal version: one tiny LAD block, one tiny FBD block, one tag table, and one HMI screen if available.

The repo also includes built-in experimental templates in:

```text
templates/tia-portal/base-v16-experimental/
```

These are fallback recipes for the toolkit and UI robot. They are not guaranteed Siemens import XML. Real exported TIA files are still the best way to improve reliability.

## Best Files To Give Codex

Preferred formats:

- `.scl` generated sources,
- `.xml` exported PLC blocks, LAD/FBD blocks, DBs, UDTs, and tag tables,
- `.csv` exported PLC tag tables,
- HMI screen/template exports if TIA/WinCC allows export,
- copied compiler diagnostics as `.txt`.

Useful but weaker:

- `.zap16` archives and `.ap16` project files for context,
- screenshots of LAD/FBD/HMI screens,
- PDFs or written control requirements.

Avoid expecting the toolkit to edit `.ap16` internals directly. The safe paths are Openness, TIA exports/imports, or UI Agent Mode on a visible copied project.

Private exports should stay private. The repository `.gitignore` excludes common TIA/project/archive/report files so exercises, credentials, and personal data are not accidentally published.

## References

- Siemens TIA Portal Openness API documentation: https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows
- TIA Openness user group requirement: https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows/basics/installation/adding-users-to-the-siemens-tia-openness-user-group
- TIA Openness firewall: https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows/tia-portal-openness-api/general-functions/tia-portal-openness-firewall
