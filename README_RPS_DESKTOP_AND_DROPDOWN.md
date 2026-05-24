# RPS Desktop + Styled Terminal Dropdown

## Dock Commander terminal dropdown

The Dock Commander terminal dropdown is now styled like a small menu button:

```text
- Dark button background
- Orange border/accent
- Hover/open states
- Dropdown uses the same dark/orange color flow
```

## RPS on Pyle Menu

RPS is now added as a Pyle Menu card.

New project:

```text
PyleCentury.RPS.Desktop
```

This is a first desktop shell, not the full drag/drop RPS yet.

Included now:

```text
- RPS launches from Pyle Menu
- Reads terminal from launch context
- Loads route buckets from backend endpoint
- Has UI placeholders for freight bucket, active run, clipped runs, and locks
```

Next RPS build:

```text
- Bucket lock icons in dropdown
- Read-only mode if someone else owns the lock
- Drag/drop freight stops
- Active/clipped/saved run behavior
- WebSocket live updates
```
