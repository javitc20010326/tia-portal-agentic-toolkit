# TIA Portal UI Agent Runbook

Target TIA version: `V16`
Automation profile: `guided`
Project path: `not set`
Import pack folder: `C:\Users\javit\Documents\Codex\2026-05-06\instala-esto-revisa-todo-el-enlace\tia-portal-agentic-toolkit\testdata\generated-axis-pack`

This runbook is for machines with TIA Portal installed but without usable Openness permissions.

## Command

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\ui-agent\tia-ui-agent.ps1 -PlanPath "C:\Users\javit\Documents\Codex\2026-05-06\instala-esto-revisa-todo-el-enlace\tia-portal-agentic-toolkit\testdata\generated-ui-agent-plan\ui-agent-run.json"
```

## What The UI Agent Can Automate

- Locate installed TIA Portal executables.
- Open a TIA project file if a project path is provided.
- Bring TIA Portal to the foreground.
- Prepare generated SCL/CSV/template files for import.
- Capture process/window state and diagnostics text that is visible or copyable.

## What Needs Template Training

For reliable automatic LAD/FBD/HMI creation without Openness, provide seed exports from `V16`:

- one tiny LAD XML export,
- one tiny FBD XML export,
- one tag table export,
- one HMI screen export if available.

The toolkit can then map neutral template files to the exact XML shape used by this TIA version.