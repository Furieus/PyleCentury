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
