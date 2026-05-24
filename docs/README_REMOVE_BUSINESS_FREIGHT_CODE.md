# Removed removed_terminal_legacy_code

This build removes `removed_terminal_legacy_code` from the Pyle Century terminal/profile database and UI.

## Why

That value is not needed for the employee/menu/access system.

Terminal records should stay clean:

```text
terminal_code
terminal_name
state
is_active
```

## Migration

If the Supabase database already has the old column, run:

```text
backend-render-update/supabase_drop_removed_terminal_legacy_code.sql
```

## Cleaned areas

```text
- Supabase schema files
- Terminal seed SQL files
- Dock Commander terminal display
- README references
- Backend update notes
```
