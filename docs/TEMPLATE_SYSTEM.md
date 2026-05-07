# Template System

The template system exists to make LAD, FBD, and HMI generation possible without hard-coding Siemens XML blindly.

## Layers

1. Neutral IR: toolkit-owned JSON describing contacts, coils, FBD blocks, HMI objects, bindings, alarms, and layout.
2. Human-readable plans: Markdown descriptions of the same networks/screens.
3. Seed templates: tiny exported TIA Portal XML/CSV artifacts from the target TIA version.
4. Renderers: future mapping code that converts neutral IR into real TIA XML using the seed template shape.
5. Built-in experimental robot templates: fallback XML/JSON recipes bundled with the repo so the toolkit can start even when no user exports are available.

## Why Seed Templates Matter

SCL is text and can be generated directly.

LAD, FBD, and HMI artifacts are usually represented through Siemens export formats that can vary by TIA Portal version and object type. Guessing those XML structures risks import failures or corrupted engineering data.

The repo includes built-in fallback templates under:

```text
templates/tia-portal/base-v16-experimental/
```

These are not certified Siemens XML files. They are toolkit/robot recipes. The UI agent can understand them, and future renderers can transform them, but direct TIA import may fail until validated on a real TIA Portal version.

The correct path is:

1. Export one tiny LAD block from TIA Portal.
2. Export one tiny FBD block from TIA Portal.
3. Export one PLC tag table.
4. Export one HMI screen/template if available.
5. Let the toolkit map generated logic to those known-good structures.

## Current Generator

`tia_generate_logic_template_pack` creates:

- `logic-ir.json`
- `LAD_AxisInterlock.template.json`
- `FBD_AxisMode.template.json`
- `LAD_Networks.md`
- `FBD_Networks.md`
- `EXPERIMENTAL_LAD_AxisInterlock.robot.xml`
- `EXPERIMENTAL_FBD_AxisMode.robot.xml`
- `EXPERIMENTAL_IMPORT_MAP.json`
- `HMI_AxisOverview.template.json`
- `EXPERIMENTAL_HMI_AxisOverview.robot.json`
- `EXPERIMENTAL_BASE_TEMPLATES.md`
- `SEED_TEMPLATE_REQUEST.md`
- `TEMPLATE_PACK_MANIFEST.json`

These files are useful immediately as specifications and will become the input for version-specific TIA XML renderers.

## Best User Files To Provide

Most useful:

- exported PLC blocks as `.xml`,
- generated/exported SCL sources as `.scl`,
- exported FBD/LAD blocks as `.xml`,
- exported tag tables as `.csv` or `.xml`,
- exported UDT/DB files as `.xml` or `.scl`,
- HMI screen exports/templates if TIA/WinCC allows it,
- compiler diagnostics copied as text or screenshots if text copy is impossible.

Less useful but still helpful:

- `.zap16` archives,
- `.ap16` project metadata files,
- screenshots of LAD/FBD/HMI screens,
- PDFs or class statements describing the expected behavior.

Full TIA project archives are useful for context, but deep generation needs exported blocks/sources/tags because the toolkit should not edit `.ap16` internals directly.
