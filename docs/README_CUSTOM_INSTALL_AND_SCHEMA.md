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
