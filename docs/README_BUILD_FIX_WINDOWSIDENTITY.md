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
