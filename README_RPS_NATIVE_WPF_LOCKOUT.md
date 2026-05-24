# RPS Native WPF Lockout Test

This replaces the WebView/HTML RPS with a native WPF RPS screen.

## Testable now

```text
- Launch RPS from Pyle Menu
- Load route buckets from Render/Supabase
- Show active bucket locks in the right panel
- Show 👁 in the bucket dropdown when a bucket is locked
- Acquire a bucket lock when selecting an available bucket
- Enter read-only mode when someone else owns the bucket lock
- Disable routing buttons in read-only mode
- Heartbeat owned lock every 30 seconds
- Release owned lock
```

## Internal map

The center panel is a native placeholder now. A real map renderer can be added inside that panel next without making the whole RPS app HTML.
