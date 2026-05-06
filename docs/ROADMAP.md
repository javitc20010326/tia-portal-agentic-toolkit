# Roadmap

## Milestone 0.1

- MCP server compiles.
- Codex can list tools.
- Environment status works without TIA Portal installed.
- Skills and plugin metadata exist.

## Milestone 0.2

- Load Siemens.Engineering dynamically from detected Openness installation.
- Attach to a running TIA Portal process.
- Report opened project metadata.
- Enumerate devices and PLC software containers.
- Validate V16 bridge on a TIA Portal V16 VM/lab PC.
- Add version override and capability mode selection for V16-V21.

## Milestone 0.2b

- Implement semi-agentic export analysis tools.
- Parse exported XML/SCL artifacts without requiring Openness.
- Generate documentation and review reports from exported files.

Status: initial implementation added in v0.1 for XML/SCL/AWL/CSV/Excel folder analysis and Markdown draft generation.

## Milestone 0.3

- Export PLC blocks, UDTs, DBs, and tag tables to a workspace folder.
- Summarize exported artifacts for Codex.
- Compile PLC software and parse diagnostics.

## Milestone 0.4

- Import XML with approval gates.
- Generate SCL blocks and import them.
- Preserve backups and produce change reports.

## Milestone 0.5

- HMI/WinCC Unified export helpers.
- JavaScript review/generation workflows.
- Alarm/tag/screen inspection.

## Milestone 0.6

- Optional PLCSIM workflow.
- Offline simulation/test helpers.
- Safety checklist generation.
