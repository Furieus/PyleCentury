# Native RPS Lockout + Security UI Patch

## RPS UI

This keeps RPS as native WPF, but restyles it closer to the uploaded HTML mockup:

```text
- dark Pyle Century topbar
- orange/green accents
- readable button-style route bucket dropdown
- freight bucket left panel
- internal map center panel
- saved/clipped/locks right panel
- read-only pill and disabled action buttons when locked
```

The old unreadable WPF ComboBox route bucket dropdown was replaced with a custom button + popup list.

## Lockout test

Before testing, run this once in Supabase if not already run:

```text
database/rps_bucket_locks.sql
```

Then test:

```text
1. Open RPS from Pyle Menu.
2. Select a route bucket.
3. It should lock for you and show EDIT mode.
4. Another user opening the same bucket should see 👁 and READ ONLY mode.
```

## Security patch

Changed behavior:

```text
- If a Windows user has no Supabase employee profile, app buttons remain disabled.
- Admin button is hidden when profile lookup fails or backend is unavailable.
- Employee Manager now launches through Pyle Menu with an Admin session.
- Direct launching PyleCentury.Admin.exe is blocked.
```

Dock Commander, Billing, and RPS already require Pyle Menu launch context.
