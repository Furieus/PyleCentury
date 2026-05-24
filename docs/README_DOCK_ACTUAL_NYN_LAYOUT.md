# Dock Commander Actual NYN Layout

This build patches the actual Dock Commander program, not a separate preview.

## Terminal selector

The Dock Commander terminal dropdown now shows all terminals, but only mapped terminals are selectable.

Mapped terminals in this build:

```text
CON - SOUTHINGTON, CT
NYN - NEWBURGH, NY
```

All other terminals are greyed out with `(no dock map)` and cannot be selected.

## NYN layout

NYN uses the same default Dock Commander XAML style as CON.

Only the door arrangement changes.

NYN rules applied:

```text
- All door slots are equal size
- Top row has 30 equal slots
- Bottom row has 30 equal slots
- Top row: 12,11,10,9,8,7,6,5,4,3,2,1, blank, blank, blank, blank, 56,55,54,53,52,51,50,49,48,47,46,45,44,43
- Bottom row: 13 through 42 continuous
- No blank gap between 24 and 25
- 12 lines up with 13
- 56 lines up with 29
- 43 lines up with 42
```

## Ignored from the screenshots

```text
- Bills
- HUIDs
- Weight
- Icons
- Freight status
- Highlight colors
- Labels/staging text
```

## Files added/updated

```text
DockCommander.Desktop/DockTerminalCatalog.cs
DockCommander.Desktop/DockLayouts/NYN.default.layout.json
backend-render-update/supabase_nyn_actual_dock_layout.sql
```
