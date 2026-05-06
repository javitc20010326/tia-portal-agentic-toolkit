# Issue: Implement Openness attach

Load `Siemens.Engineering.dll` dynamically from the detected Openness installation and attach to a running TIA Portal process.

Acceptance criteria:

- `tia_attach_running_portal` returns attached process metadata.
- TIA Portal Openness firewall prompt behavior is documented.
- Failure modes are clear: missing group, missing Openness, no running Portal, multiple Portal processes.
