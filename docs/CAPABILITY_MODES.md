# Capability Modes

The toolkit must support several user environments. Not every student or engineer has administrator rights, Openness access, or even TIA Portal installed locally.

## Modes

### Full Agentic

Requirements:

- Windows.
- TIA Portal installed.
- TIA Portal Openness installed.
- User belongs to `Siemens TIA Openness`.
- Siemens.Engineering assemblies detected.
- TIA Portal/project available for the requested operation.

Capabilities:

- attach to TIA Portal,
- inspect project structure,
- enumerate devices/PLCs/HMIs,
- export blocks/tags/UDTs,
- compile and return diagnostics,
- prepare write operations with explicit approval gates.

### Semi-Agentic

Requirements:

- Repo installed.
- User can provide exported artifacts manually.

Capabilities:

- analyze exported XML/SCL/CSV/Excel,
- generate SCL and documentation,
- review naming and structure,
- prepare import-ready artifacts for manual import,
- explain TIA workflows step by step.

Core tools:

- `tia_analyze_export_folder`
- `tia_parse_block_xml`
- `tia_summarize_scl`
- `tia_generate_export_documentation`
- `tia_prepare_manual_import_checklist`

### Advisory

Requirements:

- Repo installed, no TIA Portal required.

Capabilities:

- PLC/HMI design guidance,
- code generation from specifications,
- control sequence design,
- safety and commissioning checklists.

## Capability Tool

The canonical first tool should be `tia_capabilities`. It should summarize environment state and choose a mode:

```json
{
  "mode": "semi_agentic",
  "tiaPortalVersions": ["V16"],
  "recommendedVersion": "V16",
  "opennessInstalled": true,
  "userInOpennessGroup": false,
  "canUseExports": true,
  "canUseOpenness": false,
  "nextAction": "Ask an administrator to add the user to Siemens TIA Openness, or continue with exported files."
}
```

## Version Policy

The repo must not hard-code one TIA Portal version. Every Openness bridge should:

1. discover installed versions from the registry,
2. locate matching Siemens.Engineering assemblies,
3. choose the newest compatible version by default,
4. allow a user override such as V16, V17, V18, V19, V20, or V21,
5. degrade to semi-agentic mode when the selected version is unavailable.
