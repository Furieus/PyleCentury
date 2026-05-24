# Single Menu Executable Fix

The menu now builds directly as:

```text
PyleMenu.exe
```

The old duplicate:

```text
PyleCentury.Menu.exe
```

is removed during publish.

## Distribution root should have one launcher

```text
C:\PyleCentury\
  PyleMenu.exe
  PyleMenu.dll
  PyleMenu.deps.json
  PyleMenu.runtimeconfig.json
  appsettings.json
  apps\
    DockCommander\
    Billing\
    RPS\
    Admin\
```

Use `PyleMenu.exe` as the only launcher.
