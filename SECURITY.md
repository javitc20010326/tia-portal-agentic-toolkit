# Security

This toolkit can eventually automate engineering actions in TIA Portal. Those actions can affect PLC and HMI projects, generated code, simulations, and potentially deployed industrial control systems.

## Principles

- Default to read-only tools.
- Require explicit approval for writes, imports, compiles, downloads, simulations, and project saves.
- Prefer export-before-edit and backup-before-import workflows.
- Never download to physical PLC hardware without a separate human-controlled procedure.
- Treat generated PLC/HMI code as untrusted until reviewed and tested.

## Recommended Controls

- Use a non-production project copy.
- Keep projects under version control or export snapshots before modifications.
- Use PLCSIM or an offline test rig before hardware deployment.
- Restrict Windows group membership for `Siemens TIA Openness`.
- Review the TIA Portal Openness firewall prompt and executable path.

## Reporting

Do not include proprietary PLC programs, credentials, or plant details in public issues.
