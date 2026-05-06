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

## Analyze With Codex

Use these MCP tools:

- `tia_analyze_export_folder`: summarize supported files.
- `tia_parse_block_xml`: inspect a specific XML artifact.
- `tia_summarize_scl`: inspect SCL/AWL source.
- `tia_generate_export_documentation`: create a Markdown report draft.
- `tia_prepare_manual_import_checklist`: create manual import steps.

## Manual Import

The toolkit should not pretend it imported anything when Openness is unavailable. It should prepare artifacts/checklists and tell the user to import manually in TIA Portal.

Safety rules:

- Always work on a project copy.
- Compile after each import group.
- Test offline/simulation before hardware.
- Keep PLC download as a separate manual decision.
