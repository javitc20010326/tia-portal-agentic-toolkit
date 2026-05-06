---
name: tia-openness-setup
description: Configure and validate Siemens TIA Portal Openness for Codex, including Windows group membership, Openness registry discovery, MCP configuration, firewall prompts, and safe first-run checks. Use when the user asks to install, validate, troubleshoot, or connect Codex to TIA Portal.
---

# TIA Openness Setup

Use the `tia_environment_status` MCP tool first. It reports whether the MCP server can see Siemens Openness registry keys, whether the user appears to be in the `Siemens TIA Openness` Windows group, and which Siemens.Engineering assemblies may be available.

If Openness access fails:

1. Confirm TIA Portal is installed with Openness.
2. Confirm the Windows user is in `Siemens TIA Openness`.
3. Ask the user to sign out and sign back in after group changes.
4. Ask the user to open TIA Portal before attach-mode workflows.
5. Explain the TIA Portal Openness firewall prompt and verify the executable path before approval.

For Windows TOML paths in Codex config, use single-quoted strings.
