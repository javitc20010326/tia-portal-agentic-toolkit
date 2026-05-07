# Built-In TIA Portal Base Templates, V16 Experimental

These templates are bundled so the toolkit can start without user-provided seed exports.

They are not guaranteed Siemens import XML. They are toolkit/robot recipes:

- use them to generate SCL/LAD/FBD/HMI plans,
- use them to drive UI Agent Mode,
- use them as fallback structures when no real TIA export exists,
- replace or calibrate them with real exported TIA Portal V16 XML/CSV when available.

Recommended confidence order:

1. Real TIA Portal V16 exports from the user's project or a tiny seed project.
2. Generated SCL/CSV import packs.
3. Built-in robot recipes from this folder.
4. Direct editing of `.ap16`/`.zap16` internals: not supported.

Files:

- `lad-axis-interlock.robot.xml`
- `fbd-axis-mode.robot.xml`
- `hmi-axis-overview.robot.json`
- `experimental-import-map.json`
- `plc-tags-suggested.csv`
