# VM Handoff: TIA Portal V16 Lab Machine

Use this document when opening Codex inside the VM/lab PC that has TIA Portal V16 installed.

## Goal

Validate and continue implementing the TIA Portal Agentic Toolkit against a real TIA Portal V16 + Openness installation.

## First Checks In PowerShell

Run these commands in the VM and paste the output into Codex:

```powershell
whoami
```

```powershell
Get-ChildItem 'C:\Program Files\Siemens\Automation' -Recurse -Filter Siemens.Engineering.dll -ErrorAction SilentlyContinue | Select-Object FullName
```

```powershell
Get-ChildItem 'HKLM:\SOFTWARE\Siemens\Automation\Openness' -Recurse -ErrorAction SilentlyContinue
```

```powershell
net localgroup "Siemens TIA Openness"
```

```powershell
Get-Process | Where-Object { $_.ProcessName -like '*Portal*' -or $_.ProcessName -like '*Siemens*' } | Select-Object Id,ProcessName,MainWindowTitle
```

## If The Repo Is Available In The VM

From the repository root:

```powershell
dotnet build .\TiaPortalAgenticToolkit.sln
```

Then run the V16 bridge:

```powershell
.\src\TiaPortalAgenticToolkit.OpennessBridge.V16\bin\Debug\net48\TiaPortalAgenticToolkit.OpennessBridge.V16.exe status
```

Install for Codex inside the VM:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-codex.ps1
```

Restart Codex in the VM and ask:

```text
Check my TIA Portal Openness environment using the tia_portal MCP server.
```

## If Openness Is Not Installed

Open the TIA Portal V16 installer or maintenance setup and enable the `TIA Portal Openness` option. After installation, add the Windows user to:

```text
Siemens TIA Openness
```

Then sign out and sign back in.

## Codex Continuation Prompt

Paste this into Codex in the VM:

```text
We are continuing the TIA Portal Agentic Toolkit project. The local repo should contain a .NET MCP server, Codex skills, and a V16 Openness bridge. First run docs/VM_HANDOFF.md checks, then build the solution, run the V16 bridge status command, inspect Siemens.Engineering.dll paths and Openness registry keys, and implement the next bridge step for TIA Portal V16. Keep all write-capable TIA actions behind explicit approval.
```

## Expected Next Implementation Step

After the bridge detects `Siemens.Engineering.dll`, implement a read-only command that loads the V16 assembly and reports available public types. Then implement attach/open project only after the assembly path and user group are validated.
