## 2024-11-20 - Windows Forms Accessibility Standards
**Learning:** WinForms does not use web-based ARIA attributes (like `aria-label`). Instead, screen readers and accessibility tools rely on the `AccessibleName` property, while sighted users benefit from the `ToolTip` component for icon-only interactive elements.
**Action:** Always set both `AccessibleName` and add a `ToolTip` (using `_toolTip.SetToolTip()`) for any icon-only `Button` or control in Windows Forms applications to ensure both full accessibility and a good UX.

## 2024-05-18 - Icon-Only Button Accessibility in Windows Forms
**Learning:** In Windows Forms applications, unlike web-based applications that use ARIA labels, you must explicitly set the `AccessibleName` property (and ideally `ToolTip`) for icon-only buttons (like `✕` for close) so that screen readers can convey their purpose correctly.
**Action:** Always assign both `AccessibleName` and `ToolTipText` (or use a helper extension method) for UI components that only display an icon.
