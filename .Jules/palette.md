## 2024-05-24 - [Add Tooltips to Icon-Only Buttons]
**Learning:** Found an accessibility/UX issue where icon-only buttons in the Logistics Route Manager (✏, ⏸, ✕) had no tooltips. This made their actions ambiguous to users.
**Action:** Used standard WinForms `ToolTip.SetToolTip()` to add descriptive text to these buttons, improving clarity and accessibility.
