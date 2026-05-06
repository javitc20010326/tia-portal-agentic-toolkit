# Next Steps

## What works now

- The MCP server builds on Windows with .NET 8.
- Codex can list MCP tools.
- Codex can call `tia_environment_status`.
- The server detects whether TIA Portal Openness appears to be installed.
- The server checks whether the current Windows user is in `Siemens TIA Openness`.
- Codex skills are available globally after running `scripts/install-codex.ps1`.
- The architecture supports future full/semi/advisory modes.

## What needs a TIA Portal machine

The next implementation steps require a PC with:

- TIA Portal installed.
- TIA Portal Openness installed.
- A user in the `Siemens TIA Openness` group.
- A sample `.ap*` project that can be shared for testing.
- .NET Framework 4.8 developer/runtime support for the real Siemens.Engineering bridge.

Without that environment, this repo can compile and validate MCP plumbing, but it cannot test real Siemens.Engineering calls.

## Next engineering milestone

Implement and test:

1. `tia_capabilities` mode detection.
2. Dynamic load of `Siemens.Engineering.dll`.
3. Attach to a running TIA Portal process.
4. Open/read current project metadata.
5. Enumerate devices, PLC software, blocks, and tag tables.
6. Export selected blocks/UDTs/tag tables to a folder.
7. Compile PLC software and return diagnostics.
8. Semi-agentic export parser for users without Openness permissions.

Important: Siemens documents Openness programming against .NET Framework 4.8 / Siemens.Engineering assemblies. The current MCP server validates Codex/MCP plumbing; the next real implementation step is a Windows-only Openness bridge that runs on the TIA Portal machine.

## Information needed from the user

- TIA Portal version: V17/V18/V19/V20/V21.
- Whether Openness is installed.
- Whether the PC can run unsigned local .NET executables.
- A non-confidential sample project for testing.
- GitHub repository full name for publishing, for example `javi/tia-portal-agentic-toolkit`.

For TIA Portal V16 specifically, run the V16 bridge on the lab PC:

```powershell
dotnet build .\src\TiaPortalAgenticToolkit.OpennessBridge.V16\TiaPortalAgenticToolkit.OpennessBridge.V16.csproj
.\src\TiaPortalAgenticToolkit.OpennessBridge.V16\bin\Debug\net48\TiaPortalAgenticToolkit.OpennessBridge.V16.exe status
```
