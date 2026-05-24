# Pyle Century Suite

This package contains the current Pyle Century desktop suite:

```text
PyleCentury.Menu
DockCommander.Desktop
PyleCentury.Billing.Desktop
```

## What changed

### Pyle Menu

This is the new entry point for the suite.

It starts with a Pyle Century loading screen, then shows app cards for:

- Dock Commander
- Billing

Each card launches the matching executable.

### Dock Commander

Dock Commander was changed to launch directly into the dashboard.

The old splash/startup loader is removed because the Pyle Menu is now the suite loader.

### Billing

Billing launches as its own app and includes:

- locked dummy shipper
- consignee lookup
- freight details
- hazmat details
- foodstuffs/liftgate/straight truck flags
- 9-digit PRO generation
- local JSON save/load by PRO

## Do we need to build each as release?

Yes.

The Pyle Menu launches the release executables:

```text
DockCommander.Desktop\release\win-x64\DockCommander.exe
PyleCentury.Billing.Desktop\release\win-x64\PyleCentury.Billing.exe
```

The menu itself also gets published as:

```text
PyleCentury.Menu\release\win-x64\PyleCentury.Menu.exe
```

## Build everything

From this folder:

```powershell
.\publish-all-windows-release.bat
```

Then launch:

```text
PyleCentury.Menu\release\win-x64\PyleCentury.Menu.exe
```

## Development run

You can also run individual apps during development:

```powershell
cd PyleCentury.Menu
dotnet run
```

```powershell
cd DockCommander.Desktop
dotnet run
```

```powershell
cd PyleCentury.Billing.Desktop
dotnet run
```

If you run the menu before publishing, it will try to launch debug builds if release EXEs are not found.

## Update: centered racks and launcher path fix

Dock Commander rack layout was adjusted so the rack block centers between the north and south door rows when the window is maximized.

The Pyle Menu launcher now searches from the suite root for each app executable, including:

```text
DockCommander.Desktop\release\win-x64\DockCommander.exe
PyleCentury.Billing.Desktop\release\win-x64\PyleCentury.Billing.exe
```

That means you can keep each app's release build in its own project folder. The menu does not require the app EXEs to be copied beside the menu EXE.

Recommended launch flow:

```text
1. Run publish-all-windows-release.bat from the suite root.
2. Start PyleCentury.Menu\release\win-x64\PyleCentury.Menu.exe.
3. Use the menu cards to open Dock Commander or Billing.
```

## Render/Supabase backed Billing update

Billing now uses the Render API for shared shipment storage instead of a local JSON file.

Setup:

1. Run this SQL in Supabase:

```text
backend-render-update/supabase_billing_shipments.sql
```

2. Replace your Render backend `main.py` with:

```text
backend-render-update/main.py
```

3. Commit and push to GitHub.

4. Let Render redeploy.

5. Set Billing's `appsettings.json` to your Render URL:

```json
{
  "ApiBaseUrl": "https://YOUR-RENDER-SERVICE.onrender.com"
}
```

After that, PRO generation, shipment saving, and PRO lookup are shared across systems.

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

# Pyle Century Custom Install + Supabase Identity Schema

## Supabase schema

This build includes a ready-to-run Supabase schema folder:

```text
supabase-schema/pyle_century_identity_schema.sql
supabase-schema/seed_level4_user_template.sql
```

Run this on your PC to get your Windows login:

```bat
whoami
```

Example:

```text
desktop-abc123\dan
```

Put that exact value into:

```text
seed_level4_user_template.sql
```

For Dan:

```text
Display Name: Dan Rosengrant
Employee ID: 17713
Home Terminal: CON
Access Level: 4
```

## Custom build/install output

This build includes two publish scripts:

```text
publish-to-c-pylecentury.bat
publish-to-c-pylecentury.ps1
```

Default output:

```text
C:\PyleCentury\PyleMenu.exe
C:\PyleCentury\apps\DockCommander\DockCommander.exe
C:\PyleCentury\apps\Billing\PyleCentury.Billing.exe
C:\PyleCentury\config\install.json
```

Run from the suite root:

```bat
publish-to-c-pylecentury.bat
```

Or choose a different install folder:

```bat
publish-to-c-pylecentury.bat "D:\PyleCenturyTest"
```

PowerShell version:

```powershell
.\publish-to-c-pylecentury.ps1
.\publish-to-c-pylecentury.ps1 -InstallRoot "D:\PyleCenturyTest"
```

## Menu launcher behavior

The menu looks for child apps in:

```text
C:\PyleCentury\apps
```

Configured in:

```text
PyleCentury.Menu\appsettings.json
```

Relevant settings:

```json
{
  "InstallRoot": "C:\\PyleCentury",
  "AppsRoot": "C:\\PyleCentury\\apps",
  "ApiBaseUrl": "https://PyleCentury.onrender.com"
}
```

## Menu-only modules

Dock Commander and Billing should not be opened directly. They require the menu launch/session context.

If a child app is double-clicked directly, it should refuse and tell the user to open it from the Pyle Century Menu.

## Terminal note

```text
CON = Connecticut terminal

```

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

# Build Fix - WindowsIdentity name collision

This build fixes:

```text
error CS1061: 'string' does not contain a definition for 'GetCurrent'
```

Cause:

```csharp
WindowsIdentity.GetCurrent()
```

was being interpreted as the string property/parameter named `WindowsIdentity`, not the .NET class.

Fix:

```csharp
System.Security.Principal.WindowsIdentity.GetCurrent()
```

Patched files:

```text
PyleCentury.Billing.Desktop/Shared/LaunchContext.cs
DockCommander.Desktop/Shared/LaunchContext.cs
PyleCentury.Menu/Shared/LaunchContext.cs
```

# Build Fix - Pyle Menu Microsoft.Extensions.Configuration

This build fixes:

```text
PyleCentury.Menu\MainWindow.xaml.cs(9,17): error CS0234:
The type or namespace name 'Extensions' does not exist in the namespace 'Microsoft'
```

Cause:

The Menu project uses:

```csharp
using Microsoft.Extensions.Configuration;
```

but the `.csproj` did not include the required NuGet package references.

Added to:

```text
PyleCentury.Menu/PyleCentury.Menu.csproj
```

Package references:

```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.FileExtensions" Version="10.0.0" />
```

Also ensured:

```text
appsettings.json
```

copies to the build output.

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


# Username-Only Login

For now, Pyle Century uses only the username part of `whoami`.

Example from your PC:

```text
gaming\dan
```

The app looks up:

```text
dan
```

not:

```text
gaming\dan
```

## Database

Use:

```text
pc_employees.windows_username = dan
```

`windows_identity` is optional/background info and does not need to be the login key.

## Migration for existing DB

Run:

```text
backend-render-update/supabase_username_only_login_migration.sql
```

## Seed Dan as Level 4

Run:

```text
backend-render-update/seed_dan_username_only_level4.sql
```

## Backend

Add/update the Render endpoint using:

```text
backend-render-update/main_py_username_only_login_patch.txt
```

The endpoint should resolve:

```text
GET /auth/windows-profile?windows_username=dan
```

## Employee Manager

When creating users, the important field is:

```text
Windows Username
```

For `gaming\dan`, enter:

```text
dan
```

The full Windows Identity field can be left blank for now.
