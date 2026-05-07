---
name: tia-plc-engineering
description: Work with TIA Portal PLC projects using Openness exports, SCL/LAD/FBD/DB/UDT artifacts, compile diagnostics, and safe change workflows. Use when the user asks Codex to inspect, generate, edit, review, document, compile, or troubleshoot PLC blocks, tags, UDTs, DBs, OBs, FBs, FCs, or technology objects in TIA Portal.
---

# TIA PLC Engineering

Default to read-only inspection. Before modifying a project, export the affected block/type/tag table and propose a concise change plan.

If Openness is unavailable, switch to semi-agentic mode: ask the user for exported XML/SCL/CSV/Excel artifacts, analyze them, and prepare manual import instructions.

Use semi-agentic tools when available:

- `tia_analyze_export_folder` for the whole export folder.
- `tia_parse_block_xml` for individual XML artifacts.
- `tia_summarize_scl` for SCL/AWL source.
- `tia_generate_export_documentation` for report drafts.
- `tia_prepare_manual_import_checklist` before the user imports anything manually.
- `tia_generate_axis_control_pack` to create SCL UDT/DB/FB artifacts, tag CSV, HMI plan, report, and checklist for manual import.
- `tia_generate_plc_tag_table_csv` to create suggested tag tables with hardware addresses intentionally left for human assignment.
- `tia_generate_logic_template_pack` for neutral LAD/FBD/HMI template artifacts and seed-template requests.
- `tia_generate_ui_agent_plan` when TIA Portal is installed but Openness is unavailable and the user wants desktop-level automation.

Preferred workflow:

1. Inspect project/environment with MCP status tools.
2. Export relevant PLC artifacts.
3. Analyze generated XML/SCL and produce a change plan.
4. Ask for explicit approval before import or project modification.
5. Compile after changes and report diagnostics.

For SCL generation, prefer deterministic, documented code with clear interfaces, explicit types, and defensive range handling. Do not invent hardware addresses or safety behavior.

For LAD/FBD/HMI without Openness, do not pretend neutral templates are real TIA XML until seed exports from the same TIA Portal version are available. Use the template system to request seed LAD, FBD, tag-table, and HMI exports.
