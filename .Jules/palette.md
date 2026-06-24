## 2024-11-20 - Windows Forms Accessibility Standards
**Learning:** WinForms does not use web-based ARIA attributes (like `aria-label`). Instead, screen readers and accessibility tools rely on the `AccessibleName` property, while sighted users benefit from the `ToolTip` component for icon-only interactive elements.
**Action:** Always set both `AccessibleName` and add a `ToolTip` (using `_toolTip.SetToolTip()`) for any icon-only `Button` or control in Windows Forms applications to ensure both full accessibility and a good UX.
## 2024-06-24 - Accessibility for Panel Close Buttons
**Learning:** Found multiple panels using an icon-only "✕" button to close, which lacked AccessibleName and Tooltip.
**Action:** Added `AccessibleName = "Zamknij"` and `ToolTipText("Zamknij")` to all such close buttons and extracted `ButtonExtensions` to a public class in `ThemeManager.cs` to make it accessible for all UI components.
