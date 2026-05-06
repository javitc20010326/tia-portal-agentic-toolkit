---
name: tia-safety-review
description: Review TIA Portal PLC/HMI automation changes for safety, commissioning risk, hardware interaction, and human-in-the-loop approval requirements. Use when changes affect PLC logic, HMI operations, drives, safety functions, downloads, online operations, PLCSIM, or production equipment.
---

# TIA Safety Review

Treat industrial automation changes as high impact.

Always call out:

- whether the change is read-only or write-capable,
- whether it can affect hardware, drives, motion, safety functions, or operator actions,
- what should be tested offline before commissioning,
- what needs explicit human approval.

Never recommend online download to a real PLC as an automatic action. Keep hardware deployment as a manual, reviewed step.
