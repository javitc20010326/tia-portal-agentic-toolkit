# Manual Import Checklist

Use this when Openness is unavailable or when you want to review every action manually.

## Before Import

- Work on a copy of the TIA Portal project, not the original.
- Confirm the target project opens correctly in `V16`.
- Keep real PLC/drive hardware offline unless a responsible engineer approves testing.
- If possible, export an empty tag table from your TIA Portal version and compare its CSV/XML columns with `PLC_Tags_Suggested.csv`.

## Import Order

1. Import or create `01_UDT_AxisCommand.scl`.
2. Import or create `02_UDT_AxisStatus.scl`.
3. Import or create `03_DB_AxisData.scl`.
4. Import or create `04_FB_AxisControl.scl`.
5. Add the call from `05_OB1_Call_Example.scl` to OB1 or a cyclic block.
6. Import or manually create tags from `PLC_Tags_Suggested.csv`.
7. Build the HMI screen from `HMI_Screen_Plan.md`.
8. Compile PLC software and resolve diagnostics.

## Tests

- Enable false: outputs must remain off.
- Drive fault true: block must enter fault state.
- Jog positive with positive limit true: block must block movement and set fault.
- Jog negative with negative limit true: block must block movement and set fault.
- Stop command: movement outputs must turn off.
- Move absolute: state should return to enabled when actual position reaches target tolerance.

## Human Approval

Do not download to a real PLC or connect a real drive until the generated artifacts compile and have been reviewed against the lab hardware.