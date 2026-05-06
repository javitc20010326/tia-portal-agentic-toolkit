# Semi-Agentic Workflow

Use this workflow when the user does not have Openness permissions or cannot run Codex on the TIA Portal machine.

## Export From TIA Portal

In TIA Portal:

1. Open the project copy.
2. Right-click relevant PLC blocks, UDTs, DBs, tag tables, or HMI artifacts.
3. Choose export where available.
4. Prefer XML/SCL/CSV formats.
5. Save all files into one folder, for example:

```text
C:\Users\<user>\Downloads\tia_exports
```

Important: a full TIA Portal project folder or `.ap16`/`.zap16` archive is useful to identify project metadata, but it is not enough for deep semi-agentic code analysis. For PLC/HMI review, export the actual blocks, tags, UDTs, screens, or sources as XML/SCL/CSV where TIA Portal allows it.

## Analyze With Codex

Use these MCP tools:

- `tia_analyze_export_folder`: summarize supported files.
- `tia_parse_block_xml`: inspect a specific XML artifact.
- `tia_summarize_scl`: inspect SCL/AWL source.
- `tia_generate_export_documentation`: create a Markdown report draft.
- `tia_prepare_manual_import_checklist`: create manual import steps.

## Generate Importable Starter Artifacts

Semi-agentic mode is not limited to reading exported files. It can also generate a reviewable engineering pack that a user imports manually into TIA Portal:

- `tia_generate_axis_control_pack`: creates SCL UDTs, a DB, an FB, an OB1 call example, suggested tag CSV, HMI plan, report, manifest, and import checklist.
- `tia_generate_plc_tag_table_csv`: creates only suggested tag-table artifacts.
- `tia_generate_hmi_plan`: creates only the HMI/WinCC screen plan.

These outputs are intended for manual import/copy into a project copy. They should compile before hardware testing and may need small syntax adjustments depending on the exact TIA Portal version and project settings.

## Manual Import

The toolkit should not pretend it imported anything when Openness is unavailable. It should prepare artifacts/checklists and tell the user to import manually in TIA Portal.

Safety rules:

- Always work on a project copy.
- Compile after each import group.
- Test offline/simulation before hardware.
- Keep PLC download as a separate manual decision.
