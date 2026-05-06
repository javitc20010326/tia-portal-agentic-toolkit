# Getting Started

This guide installs the TIA Portal Agentic Toolkit for Codex.

## 1. Confirm TIA Portal Openness

On the TIA Portal machine:

1. Install TIA Portal with Openness.
2. Add your Windows user to the local group `Siemens TIA Openness`.
3. Sign out and sign back in.
4. Open TIA Portal once so the Openness firewall can prompt when the MCP server connects.

## 2. Build the MCP Server

From this repository:

```powershell
dotnet build .\src\TiaPortalAgenticToolkit.McpServer\TiaPortalAgenticToolkit.McpServer.csproj
```

## 3. Configure Codex

Add this block to `C:\Users\<you>\.codex\config.toml`:

```toml
[mcp_servers.tia_portal]
command = 'C:\absolute\path\to\TiaPortalAgenticToolkit.McpServer.exe'
tool_timeout_sec = 600
env_vars = ['WINDIR', 'ProgramFiles', 'ProgramFiles(x86)', 'USERNAME', 'USERDOMAIN']
```

Use single-quoted TOML strings on Windows so backslashes are interpreted literally.

## 4. Install Skills

Create junctions or symbolic links from `C:\Users\<you>\.agents\skills` to each skill under `skills-catalog`.

At minimum:

- `tia-openness-setup`
- `tia-plc-engineering`
- `tia-hmi-engineering`
- `tia-safety-review`
- `tia-agentic-toolkit-setup`

## 5. Verify

Restart Codex and ask:

```text
Check my TIA Portal Openness environment.
```

The server should report:

- toolkit version,
- whether Siemens Openness registry keys exist,
- whether the user appears to be in the `Siemens TIA Openness` group,
- detected Siemens.Engineering assembly candidates.

## 6. Typical Workflow

1. Open TIA Portal.
2. Open the project.
3. Ask Codex to inspect the environment.
4. Ask Codex to read/export project artifacts.
5. Review every proposed change.
6. Let Codex import or modify only after an explicit approval.

## Notes

The first time an Openness application connects, TIA Portal may show a firewall prompt. Approve it only if the executable path is the MCP server you built.
