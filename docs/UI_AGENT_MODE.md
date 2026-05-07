# UI Agent Mode

UI Agent Mode is the experimental no-Openness automation path.

Use it when the machine has TIA Portal installed but the Windows user cannot use TIA Portal Openness.

## Capability

UI Agent Mode can automate the visible desktop:

- detect TIA Portal executables and running Portal processes,
- open a TIA project file through Windows file association,
- bring the TIA Portal window to the foreground,
- prepare generated SCL/CSV/XML/template files for import,
- capture process/window state before and after automation phases,
- send controlled keystrokes in advanced profiles.

It is intended to become the bridge for:

- importing generated SCL/CSV artifacts,
- compiling through the visible TIA UI,
- collecting diagnostics,
- iterating generated code after compile errors.

## Why It Is Not Full Agentic

Without Openness, the toolkit does not have a stable Siemens engineering API. UI automation depends on:

- TIA Portal version,
- Windows language,
- screen layout,
- menu focus,
- open dialogs,
- project tree selection,
- whether diagnostics can be copied as text.

This is more automatic than manual import, but less reliable than Openness.

## Profiles

- `dry-run`: generate plans and inspect state without UI mutation.
- `guided`: open/focus/prepare and stop at safe checkpoints.
- `aggressive`: may send keystrokes after the flow has been proven on that machine.

Default profile is `guided`.

## Runner

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\ui-agent\tia-ui-agent.ps1 -Action status
```

With a generated plan:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\ui-agent\tia-ui-agent.ps1 -PlanPath C:\path\to\ui-agent-run.json
```

## Safety

Use a copied/offline project. Do not let UI Agent Mode download to real PLC hardware. Hardware downloads remain a human-reviewed action.
