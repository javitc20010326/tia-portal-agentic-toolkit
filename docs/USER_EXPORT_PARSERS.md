# User Export Parsers

These parsers are for private user artifacts. Do not commit real student/lab/project exports to this repository.

The repo ignores common personal/export formats such as `.ap16`, `.zap16`, `.rar`, `.zip`, `.xlsx`, `.pdf`, and `.download`.

## MCP Tools

- `tia_analyze_project_texts_xlsx`: reads a TIA Project Texts workbook and extracts HMI screens, object types, text categories, PLC block references, and sample labels.
- `tia_analyze_webserver_bindings`: scans TIA web-server HTML/TXT files for PLC tag bindings such as `:="DB".Tag:`.
- `tia_analyze_db_source`: reads generated/exported DB source files and summarizes block name, sections, optimized access, and variables.
- `tia_analyze_pdf_printout_text`: reads text extracted from a TIA Portal PDF printout and extracts object/event/variable hints.

## Recommended Private Files

Best:

- `.scl`
- `.db`
- exported block `.xml`
- exported tag `.csv` or `.xml`
- `TIAProjectTexts.xlsx`
- TIA PDF printout converted to text
- TIA web-server `.html` or `.txt`

Useful for context:

- `.ap16`
- `.zap16`
- full project folders
- screenshots
- PDFs

Do not publish files containing usernames, passwords, real names, DNI/NIF, project credentials, PLC IPs, or lab network details.

## PDF Printouts

Binary PDF parsing is intentionally not part of the MCP server. Convert the PDF to text first with a local tool, then pass the `.txt` file to `tia_analyze_pdf_printout_text`.

If text conversion is unavailable, screenshots or the original PDF can still be reviewed manually by Codex in a private session, but should not be committed.
