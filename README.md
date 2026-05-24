# Pyle Century

Pyle Century is a Windows desktop application suite backed by a Render FastAPI service and Supabase Postgres.

## Current apps

```text
PyleCentury.Menu              Main launcher
DockCommander.Desktop         Dock Commander
PyleCentury.Billing.Desktop   Billing
PyleCentury.Admin.Desktop     Employee Manager
```

## Backend

Render should point to:

```text
Root Directory: backend
Build Command:  pip install -r requirements.txt
Start Command:  uvicorn main:app --host 0.0.0.0 --port $PORT
```

Required Render environment variables:

```text
SUPABASE_URL
SUPABASE_SERVICE_ROLE_KEY
```

## Database

Run the schema files in:

```text
database/
```

Recommended order:

```text
1. optional_drop_pyle_century_tables.sql
2. clean_rebuild_schema.sql
3. seed_dan_username_only_level4.sql
```

Current login approach:

```text
whoami = gaming\dan
lookup = dan
```

So the employee record should use:

```text
windows_username = dan
```

## Publish locally

Default custom install:

```powershell
.\publish-to-c-pylecentury.ps1
```

or:

```bat
publish-to-c-pylecentury.bat
```

Default output:

```text
C:\PyleCentury\PyleMenu.exe
C:\PyleCentury\apps\DockCommander\
C:\PyleCentury\apps\Billing\
C:\PyleCentury\apps\Admin\
```

## Notes

Production data is not stored in local JSON. The apps use local config only for settings like:

```json
{
  "ApiBaseUrl": "https://PyleCentury.onrender.com"
}
```

All employees, terminals, access levels, dock layouts, and RPS data should live in Supabase Postgres through the Render backend.
