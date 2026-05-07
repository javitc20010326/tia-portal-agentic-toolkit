# Experimental Built-In Base Templates

These files exist so the toolkit can work without asking every user for seed templates first.

They are intentionally marked experimental:

- They are understood by the toolkit and UI robot.
- They are not guaranteed to be directly importable by TIA Portal.
- They are safe as construction recipes and documentation.
- Real TIA exports from `V16` can replace or calibrate them later.

Generated built-in templates:

- `EXPERIMENTAL_LAD_AxisInterlock.robot.xml`: LAD construction recipe.
- `EXPERIMENTAL_FBD_AxisMode.robot.xml`: FBD construction recipe.
- `EXPERIMENTAL_HMI_AxisOverview.robot.json`: HMI screen construction recipe.
- `EXPERIMENTAL_IMPORT_MAP.json`: tells the UI robot how to treat the generated files.

Best practical path:

1. Generate SCL/CSV import packs for immediate use.
2. Use robot templates for guided UI construction.
3. When available, feed exported TIA XML/CSV files back into the toolkit to create a real renderer for this TIA version.