# Pyle Century Suite - Windows Login + Postgres Menu Gate

This build changes the suite architecture:

```text
Windows Login
↓
Pyle Century Menu
↓
Postgres employee profile lookup
↓
Authorized modules shown
↓
Menu creates launch session
↓
Module opens only if launched from menu
```

## Important terminology

```text
CON = Connecticut terminal

```

## Dan test profile

```text
Employee: Dan Rosengrant
Employee ID: 17713
Home Terminal: CON
```

## No-domain PC testing

A non-domain PC still has a Windows identity. The menu displays the detected value at the top.

It will usually look like:

```text
PCNAME\YourWindowsUser
```

If the profile is not found in Postgres, use the first-time setup panel:

```text
Employee ID: 17713
Display Name: Dan Rosengrant
Home Terminal: CON
```

Then click:

```text
Create / Update Employee Profile
```

That writes the employee profile into Postgres through the Render API.

## Postgres / Supabase setup

Run this SQL in Supabase:

```text
backend-render-update/supabase_pyle_century_identity.sql
```

It creates:

```text
pc_terminals
pc_employees
pc_employee_terminal_access
pc_modules
pc_employee_module_access
pc_menu_sessions
```

## Backend setup

Add the identity endpoints from:

```text
backend-render-update/main_py_identity_patch.txt
```

Or compare with:

```text
backend-render-update/main_with_identity.py
```

The C# menu expects:

```text
GET  /auth/windows-profile
POST /auth/employee-profile
POST /auth/menu-session
POST /auth/validate-launch
```

## Menu-only module launching

Dock Commander and Billing now require menu launch arguments.

If someone double-clicks a module directly, they see:

```text
This program must be opened from the Pyle Century Menu.
```

The menu launches modules with context:

```text
--pyle-session-token
--employee-id
--display-name
--terminal
--module
--api-base-url
--windows-identity
--permission-level
```

## API base URL

Default:

```text
https://PyleCentury.onrender.com
```

Config file:

```text
PyleCentury.Menu/appsettings.json
```

## Build

From the suite root:

```bat
publish-all-windows-release.bat
```

Or build each project from Visual Studio / dotnet.

## Next step

After this tests correctly, RPS and Dispatch should be added as menu-gated modules using the same launch guard.
