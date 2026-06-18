## 2024-11-20 - Windows Forms Accessibility Standards
**Learning:** WinForms does not use web-based ARIA attributes (like `aria-label`). Instead, screen readers and accessibility tools rely on the `AccessibleName` property, while sighted users benefit from the `ToolTip` component for icon-only interactive elements.
**Action:** Always set both `AccessibleName` and add a `ToolTip` (using `_toolTip.SetToolTip()`) for any icon-only `Button` or control in Windows Forms applications to ensure both full accessibility and a good UX.

## 2024-11-20 - Tooltips on Dynamic Controls
**Learning:** When generating dynamic rows with multiple icon-only actions (like edit, toggle, remove), it's easy to miss setting `_toolTip.SetToolTip()` on the last or destructive element. Sighted users rely on tooltips for "✕" buttons since their exact function (e.g. "Usuń trasę" vs "Zamknij") can be ambiguous out of context.
**Action:** Always verify that every dynamically created icon-only button in a row has a corresponding `_toolTip.SetToolTip()` call alongside its `AccessibleName`.
