## 2024-11-20 - Windows Forms Accessibility Standards
**Learning:** WinForms does not use web-based ARIA attributes (like `aria-label`). Instead, screen readers and accessibility tools rely on the `AccessibleName` property, while sighted users benefit from the `ToolTip` component for icon-only interactive elements.
**Action:** Always set both `AccessibleName` and add a `ToolTip` (using `_toolTip.SetToolTip()`) for any icon-only `Button` or control in Windows Forms applications to ensure both full accessibility and a good UX.

## 2024-11-20 - Adding Accessibility in Helper Methods
**Learning:** For factory methods that create reusable icon-based buttons (like `ThemeManager.CreateHudNavButton`), it is cleaner to set both `AccessibleName` and attach tooltips (e.g., using an extension method like `ButtonExtensions.ToolTipText`) directly within the factory method instead of manually updating each instanced button.
**Action:** When adding UX improvements such as tooltips and accessible names to multiple identical components, target the factory method where possible to avoid repetitive code and improve maintainability.
