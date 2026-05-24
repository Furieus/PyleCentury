# RPS Mockup UI in Desktop App

This build changes `PyleCentury.RPS.Desktop` from a placeholder WPF shell into a desktop-hosted version of the RPS mockup UI.

## What changed

```text
PyleCentury.RPS.Desktop
- Uses Microsoft WebView2
- Loads Assets/rps_mockup.html
- Preserves the mockup layout:
  - Pyle Century topbar
  - Bucket header metrics
  - Freight Bucket left panel
  - Live Active Run Map center panel
  - Saved Runs right panel
  - Clip / Unclip / Save buttons
  - Bottom hotkey bar
```

## Why

The previous RPS desktop shell was only a placeholder and did not match the mockup.

This version restores the mockup look first. The next step is wiring the UI to Render/Supabase:

```text
- Bucket dropdown from backend
- Lock/eyes indicator on locked buckets
- Read-only mode for locked buckets
- Freight stops from Postgres
- Save/clip run endpoints
- WebSocket live updates
```

## Requirement

The PC needs Microsoft Edge WebView2 Runtime. Most Windows 10/11 machines already have it.
