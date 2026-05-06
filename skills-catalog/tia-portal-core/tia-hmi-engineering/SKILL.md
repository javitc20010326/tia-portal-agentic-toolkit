---
name: tia-hmi-engineering
description: Work with TIA Portal HMI and WinCC Unified artifacts, screens, tags, scripts, alarms, faceplates, and UI engineering workflows. Use when the user asks Codex to inspect, generate, document, refactor, or troubleshoot HMI/WinCC Unified objects in TIA Portal.
---

# TIA HMI Engineering

Keep HMI changes operator-safe:

1. Identify screen, tag, alarm, faceplate, or script scope.
2. Export or inspect the relevant artifact first.
3. Preserve naming, navigation, alarm semantics, and units.
4. Do not change safety-related operator flows without explicit confirmation.
5. For JavaScript, use small functions, clear names, and avoid hidden global state.

When Openness is unavailable, use `tia_generate_hmi_plan` or `tia_generate_axis_control_pack` to produce a manual HMI implementation plan with tags, alarms, operator behavior, and validation steps. Do not claim the HMI screen was created inside TIA Portal unless Openness or a human import actually performed it.
