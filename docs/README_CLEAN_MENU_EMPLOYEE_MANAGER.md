# Clean Pyle Menu + Standalone Employee Manager

## Menu changes

The Pyle Century Menu no longer shows:

```text
- Network Application Suite / Windows Login / Postgres Employee Profile
- Access Level
- Profile Source
- First-Time Employee Setup
```

The subtitle now says:

```text
Built on legacy, designed for the future.
```

The profile strip now shows only:

```text
User: <display name from backend, fallback Windows username>
ID: <employee ID>
Terminal: <home terminal from admin profile>
```

## Admin button

The Admin button switches the launcher front to an Admin front/options screen.

Current Admin option:

```text
Employee Manager
```

Other admin cards are placeholders for later.

## Standalone Employee Manager

New project:

```text
PyleCentury.Admin.Desktop
```

It can be launched outside the Pyle Menu for now.

It connects to:

```text
https://PyleCentury.onrender.com
```

and uses:

```text
GET    /admin/users
POST   /admin/users
DELETE /admin/users/{employee_id}
```

Features:

```text
- Search users
- Filter by access level
- Create employee
- Edit employee
- Set Windows Identity
- Set Windows Username
- Set Employee ID
- Set Display Name
- Set Home Terminal
- Set Access Level 1-4
- Activate/deactivate
- Delete test users
```

Backend patch:

```text
backend-render-update/main_py_employee_manager_admin_patch.txt
```
