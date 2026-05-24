# Distribution + Visible RPS Buckets Patch

## RPS dropdown visibility fix

The backend/Supabase returns route bucket fields as snake_case, for example:

```json
route_area_name
display_order
terminal_code
```

The native RPS app was reading PascalCase property names, so bucket names could render blank.

This patch adds `JsonPropertyName` attributes and a fallback display name.

## Distributing Pyle Century

Publish to a folder such as:

```text
C:\PyleCentury
```

The runtime folder should look like this:

```text
C:\PyleCentury\
  PyleMenu.exe
  PyleCentury.Menu.dll
  PyleCentury.Menu.deps.json
  PyleCentury.Menu.runtimeconfig.json
  appsettings.json
  *.dll

  apps\
    DockCommander\
      DockCommander.exe
      DockCommander.Desktop.dll
      DockCommander.Desktop.deps.json
      DockCommander.Desktop.runtimeconfig.json
      appsettings.json
      *.dll

    Billing\
      PyleCentury.Billing.exe
      PyleCentury.Billing.Desktop.dll
      PyleCentury.Billing.Desktop.deps.json
      PyleCentury.Billing.Desktop.runtimeconfig.json
      appsettings.json
      *.dll

    RPS\
      PyleCentury.RPS.exe
      PyleCentury.RPS.dll
      PyleCentury.RPS.deps.json
      PyleCentury.RPS.runtimeconfig.json
      appsettings.json
      *.dll

    Admin\
      PyleCentury.Admin.exe
      PyleCentury.Admin.Desktop.dll
      PyleCentury.Admin.Desktop.deps.json
      PyleCentury.Admin.Desktop.runtimeconfig.json
      appsettings.json
      *.dll
```

Do not copy only the `.exe` files. Copy each full publish folder.

## Requirements on the other PC

Because the publish scripts use:

```text
--self-contained false
```

the other PC must have the matching .NET Desktop Runtime installed.

If you want to avoid installing .NET on each PC, publish self-contained later.
