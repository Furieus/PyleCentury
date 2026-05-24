# Admin Area + Employee Manager Fix

## Admin area

The accidental RPS card was removed from the Admin area.

Admin area now contains:

```text
- Employee Manager
- Terminal Admin placeholder
- Route Grid Admin placeholder
```

RPS remains only under the Applications area.

## Employee Manager

Employee Manager still launches through the Pyle Menu with an Admin launch session.

Direct launching is intentionally blocked:

```text
C:\PyleCentury\apps\Admin\PyleCentury.Admin.exe
```

The correct path is:

```text
C:\PyleCentury\PyleMenu.exe
→ Admin
→ Open Employee Manager
```

The menu now tries both possible admin executable names:

```text
PyleCentury.Admin.exe
PyleCentury.Admin.Desktop.exe
```
