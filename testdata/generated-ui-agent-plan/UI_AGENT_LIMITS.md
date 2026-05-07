# UI Agent Limits

UI Agent Mode exists because some machines have TIA Portal but do not grant Openness access.

It is useful for:

- desktop-level automation,
- repeated import/compile workflows,
- capturing diagnostics,
- training template mappings for LAD/FBD/HMI exports.

It is not equivalent to Openness:

- window titles, menus, language, and layout can change,
- hidden dialogs can block automation,
- HMI editors are graphical and harder to automate,
- compile diagnostics may require OCR or user-visible text capture,
- project mutation happens through the visible TIA UI, not a stable engineering API.

Safe default: use `AutomationProfile = guided`. Use `aggressive` only on copied/offline projects after the guided flow is proven on that machine.