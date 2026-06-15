## 2024-05-18 - Added ToolTips and AccessibleNames to Icon-Only Buttons
**Learning:** Icon-only buttons in Windows Forms require explicit AccessibleName and ToolTip properties for screen reader accessibility and visual context. The AccessibleName property is not automatically derived from ToolTip text, so both must be managed, especially when button text changes dynamically (like the play/pause button).

**Action:** Whenever adding or modifying an icon-only control (like buttons using Unicode symbols), ensure both `AccessibleName` and `ToolTip` are explicitly set and updated simultaneously if the button's state changes.
