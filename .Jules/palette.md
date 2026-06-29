## 2024-11-20 - Windows Forms Accessibility Standards
**Learning:** WinForms does not use web-based ARIA attributes (like `aria-label`). Instead, screen readers and accessibility tools rely on the `AccessibleName` property, while sighted users benefit from the `ToolTip` component for icon-only interactive elements.
**Action:** Always set both `AccessibleName` and add a `ToolTip` (using `_toolTip.SetToolTip()`) for any icon-only `Button` or control in Windows Forms applications to ensure both full accessibility and a good UX.
## 2026-06-29 - [WinForms Icon-Only Button Accessibility]
**Learning:** In Windows Forms, setting just a ToolTip text doesn't always automatically provide an ARIA-equivalent accessible name for screen readers. Icon-only buttons (like a simple '✕' for closing) must have their `AccessibleName` property explicitly set to be accessible.
**Action:** Update the `ToolTipText` extension method to automatically set the `AccessibleName` property if it's empty, ensuring all icon-only buttons get both visual and screen-reader context simultaneously.
