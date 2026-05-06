---
name: tia-plc-engineering
description: Work with TIA Portal PLC projects using Openness exports, SCL/LAD/FBD/DB/UDT artifacts, compile diagnostics, and safe change workflows. Use when the user asks Codex to inspect, generate, edit, review, document, compile, or troubleshoot PLC blocks, tags, UDTs, DBs, OBs, FBs, FCs, or technology objects in TIA Portal.
---

# TIA PLC Engineering

Default to read-only inspection. Before modifying a project, export the affected block/type/tag table and propose a concise change plan.

Preferred workflow:

1. Inspect project/environment with MCP status tools.
2. Export relevant PLC artifacts.
3. Analyze generated XML/SCL and produce a change plan.
4. Ask for explicit approval before import or project modification.
5. Compile after changes and report diagnostics.

For SCL generation, prefer deterministic, documented code with clear interfaces, explicit types, and defensive range handling. Do not invent hardware addresses or safety behavior.
