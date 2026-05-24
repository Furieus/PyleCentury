# Pyle Century Access Levels

The employee database now has a numeric access level.

## Level 1

```text
Dock Commander:
- Read/write at home terminal
- Read-only at all other terminals

Billing:
- No access
```

Example:

```text
Home terminal: CON
CON Dock Commander = can move trailers
NYX Dock Commander = view only
Billing = blocked
```

## Level 2

```text
Dock Commander:
- Read/write at home terminal
- Read/write at other terminals

Billing:
- No access
```

Example:

```text
Home terminal: CON
CON Dock Commander = can move trailers
NYX Dock Commander = can move trailers
Billing = blocked
```

## Level 3

```text
Dock Commander:
- Read-only at every terminal

Billing:
- Allowed
```

Example:

```text
CON Dock Commander = view only
NYX Dock Commander = view only
Billing = allowed
```

## Level 4

```text
Admin / ultimate level
- Access pretty much everything
- Read/write everywhere
- Billing allowed
- Admin tools allowed
```

## Database

Run:

```text
backend-render-update/supabase_pyle_century_identity.sql
```

It now adds:

```text
pc_employees.access_level
pc_access_level_definitions
```

## Menu behavior

The Pyle Century Menu now shows the user's access level and enables modules based on that level:

```text
Level 1: Dock Commander only
Level 2: Dock Commander only
Level 3: Dock Commander + Billing
Level 4: All / Admin
```

## Module behavior

The menu passes these launch fields to child programs:

```text
--access-level
--dock-permission
--billing-permission
--admin-permission
```

Dock Commander should enforce read-only/read-write using `DockPermission`.

Billing refuses to open if `BillingPermission` is `none`.

## Terminal terminology

```text
CON = Connecticut terminal

```
