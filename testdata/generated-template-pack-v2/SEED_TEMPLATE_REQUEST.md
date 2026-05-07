# Seed Template Request

To convert neutral templates into real TIA Portal XML, provide exports from the same TIA Portal major version: `V16`.

Needed seed exports:

- One tiny LAD FB exported as XML with one normally-open contact, one normally-closed contact, one parallel branch, and one coil.
- One tiny FBD FB exported as XML with AND, OR, and NOT blocks.
- One PLC tag table exported as CSV or XML.
- One exported HMI screen containing a text, button, numeric input/output, indicator, and alarm if your TIA/WinCC setup allows exporting it.

Recommended names:

- `Seed_LAD_Contacts.xml`
- `Seed_FBD_Blocks.xml`
- `Seed_TagTable.csv`
- `Seed_HMI_Screen.xml`

Once these are available, the toolkit can map neutral template nodes to the exact Siemens XML shape used by this TIA Portal version.