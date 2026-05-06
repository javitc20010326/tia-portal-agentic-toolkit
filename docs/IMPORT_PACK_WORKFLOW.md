# Import Pack Workflow

Use this workflow when Openness is unavailable or when the user wants to review generated PLC/HMI artifacts before importing them into TIA Portal.

## Audience

The toolkit is useful for several profiles:

- `self`: one user iterating on a private or lab project.
- `student`: a class/lab user who needs extra review and explanation.
- `plc_engineer`: an industrial automation user who needs compact engineering artifacts and explicit safety boundaries.

The profile changes generated wording and warnings. It does not grant or remove access to TIA Portal.

## Generated Pack

`tia_generate_axis_control_pack` creates:

- SCL UDT for axis commands.
- SCL UDT for axis status.
- SCL DB for command/status data.
- SCL FB for a conservative axis-control state machine.
- SCL OB1 call example.
- Suggested PLC/HMI tag CSV.
- HMI screen plan.
- Engineering report.
- Manual import checklist.
- JSON manifest.

## What It Can Do Without Openness

- Produce real files that can be reviewed, copied, or imported manually.
- Standardize naming and structure.
- Give Codex a concrete artifact set to iterate on after compiler diagnostics.
- Support different TIA Portal versions through generated naming/documentation parameters.

## What It Cannot Do Without Openness

- Open TIA Portal automatically.
- Export existing project blocks automatically.
- Import generated files automatically.
- Edit `.ap16` or `.zap16` internals safely.
- Compile or download to PLC hardware.

## Practical Loop

1. Generate the import pack into a folder.
2. Open a copy of the TIA Portal project.
3. Import or copy the generated artifacts in checklist order.
4. Compile in TIA Portal.
5. Send compiler diagnostics back to Codex.
6. Regenerate or patch the pack.
7. Test offline or in simulation before hardware.
