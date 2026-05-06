# Engineering Report: AxisCarla Position-Control Starter Pack

## Purpose

This pack creates a conservative starter architecture for a single axis in TIA Portal. It separates operator commands, axis status, cyclic control logic, HMI planning, and manual import checks.

## Generated Artifacts

- Two UDTs define command and status structures.
- One DB stores shared command/status data.
- One FB implements a defensive state machine for enable, jog, absolute move, stop, and fault handling.
- One OB1 call example shows how to wire the block to tags.
- One tag-table CSV proposes names and data types.
- One HMI plan defines screens, tags, alarms, and validation.

## Design Notes

- Hardware addresses are intentionally blank in the CSV. A human must assign them according to the real PLC wiring.
- The FB does not download to hardware automatically.
- The generated code should be compiled in a project copy before it is used with real drives or motion hardware.
- This pack is intended for `V16`, but generated SCL must still be checked by TIA Portal because Siemens import syntax can vary by version and project settings.