# Pyle Century Menu - Admin Folder

This build adds a Level 4 Admin area inside the Pyle Century launcher.

## Admin visibility

Only Access Level 4 users see/open Admin.

```text
Level 4 = admin / ultimate access
```

## Admin tools added

```text
Admin
- User Management
- Terminal Management placeholder
- Module Access placeholder
- Route Grid Admin placeholder
```

## User Management

The User Management screen includes:

```text
- Search users
- Filter by access level
- Create new user
- Edit Windows identity
- Edit Windows username
- Edit Employee ID
- Edit display name
- Edit home terminal
- Set access level 1-4
- Activate/deactivate user
- Delete button uses deactivate behavior by default
```

## Backend endpoints needed

Add this file to the Render FastAPI backend:

```text
backend-render-update/main_py_admin_users_patch.txt
```

Endpoints:

```text
GET  /admin/users
POST /admin/users
```

## Database

Uses existing tables:

```text
pc_employees
pc_employee_terminal_access
pc_employee_module_access
pc_modules
pc_terminals
```

The admin save endpoint rebuilds module access from access level:

```text
Level 1: DockCommander only
Level 2: DockCommander only
Level 3: DockCommander + Billing
Level 4: DockCommander + Billing + RPS + Dispatch + RouteGrid + Admin
```

## Recommended production behavior

Use deactivate instead of hard-delete to keep audit history.
