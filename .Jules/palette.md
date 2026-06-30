## 2024-11-20 - Windows Forms Accessibility Standards
**Learning:** WinForms does not use web-based ARIA attributes (like `aria-label`). Instead, screen readers and accessibility tools rely on the `AccessibleName` property, while sighted users benefit from the `ToolTip` component for icon-only interactive elements.
**Action:** Always set both `AccessibleName` and add a `ToolTip` (using `_toolTip.SetToolTip()`) for any icon-only `Button` or control in Windows Forms applications to ensure both full accessibility and a good UX.
## 2026-06-30 - Windows Forms Screen Reader Accessibility for Icon/Custom Buttons
**Learning:** In Windows Forms, buttons that use the `Paint` event for custom rendering (like icon-only buttons or buttons with manual text drawing) often leave the standard `Text` property empty. This causes screen readers to announce them as blank or unlabelled.
**Action:** Always ensure that `btn.AccessibleName` is explicitly set to a descriptive string for any custom-painted or icon-only buttons (e.g., in helper methods like `ToolTipText` or `CreateHudNavButton`).
