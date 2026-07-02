## 2024-11-20 - Windows Forms Accessibility Standards
**Learning:** WinForms does not use web-based ARIA attributes (like `aria-label`). Instead, screen readers and accessibility tools rely on the `AccessibleName` property, while sighted users benefit from the `ToolTip` component for icon-only interactive elements.
**Action:** Always set both `AccessibleName` and add a `ToolTip` (using `_toolTip.SetToolTip()`) for any icon-only `Button` or control in Windows Forms applications to ensure both full accessibility and a good UX.
## 2024-05-18 - Set AccessibleName on custom-painted controls
**Learning:** When creating custom-painted Windows Forms controls (like icon-only buttons or buttons that draw their labels via graphics instead of using the standard `Text` property), standard accessibility tools and screen readers cannot read the text. It is necessary to explicitly set the `AccessibleName` property.
**Action:** When creating custom buttons in `ThemeManager` or extending standard buttons (e.g., using `ToolTipText` on icon buttons), always ensure that `btn.AccessibleName` is also set to provide proper context for screen readers.
