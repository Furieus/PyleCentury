using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Principal;
using System.Text.Json;
using System.Windows;

namespace PyleCentury.Shared;

public sealed record EmployeeProfile(
    string WindowsIdentity,
    string WindowsUserName,
    string? WindowsDomain,
    string DisplayName,
    string EmployeeNumber,
    string HomeTerminalCode,
    string[] AllowedModules,
    string[] AllowedTerminals,
    string PermissionLevel,
    int AccessLevel,
    string DockHomePermission,
    string DockOtherPermission,
    string BillingPermission,
    string AdminPermission);

public sealed record LaunchContext(
    string SessionToken,
    string EmployeeNumber,
    string DisplayName,
    string HomeTerminalCode,
    string ModuleCode,
    string ApiBaseUrl,
    string WindowsIdentity,
    string PermissionLevel,
    int AccessLevel,
    string DockPermission,
    string BillingPermission,
    string AdminPermission);

public static class WindowsIdentityHelper
{
    public static string WindowsUserName => Environment.UserName;
    public static string MachineOrDomain => string.IsNullOrWhiteSpace(Environment.UserDomainName)
        ? Environment.MachineName
        : Environment.UserDomainName;

    public static string WindowsIdentity
    {
        get
        {
            try
            {
                var wi = System.Security.Principal.WindowsIdentity.GetCurrent();
                if (wi is not null && !string.IsNullOrWhiteSpace(wi.Name))
                {
                    return wi.Name;
                }
            }
            catch
            {
                // Local/non-domain test machines can still use Environment values.
            }

            return $"{MachineOrDomain}\\{WindowsUserName}";
        }
    }
}

public static class LaunchContextReader
{
    public static LaunchContext? ReadFromCommandLine(string expectedModuleCode)
    {
        var args = Environment.GetCommandLineArgs();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];

            if (!arg.StartsWith("--", StringComparison.Ordinal)) continue;

            var key = arg[2..];

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[key] = args[i + 1];
                i++;
            }
            else
            {
                values[key] = "true";
            }
        }

        if (!values.TryGetValue("pyle-session-token", out var token) || string.IsNullOrWhiteSpace(token))
            return null;

        values.TryGetValue("employee-id", out var employeeNumber);
        values.TryGetValue("display-name", out var displayName);
        values.TryGetValue("terminal", out var terminal);
        values.TryGetValue("module", out var module);
        values.TryGetValue("api-base-url", out var apiBaseUrl);
        values.TryGetValue("windows-identity", out var windowsIdentity);
        values.TryGetValue("permission-level", out var permissionLevel);
        values.TryGetValue("access-level", out var accessLevelText);
        values.TryGetValue("dock-permission", out var dockPermission);
        values.TryGetValue("billing-permission", out var billingPermission);
        values.TryGetValue("admin-permission", out var adminPermission);
        _ = int.TryParse(accessLevelText, out var accessLevel);

        if (!string.Equals(module, expectedModuleCode, StringComparison.OrdinalIgnoreCase))
            return null;

        return new LaunchContext(
            token,
            employeeNumber ?? "",
            displayName ?? "",
            terminal ?? "",
            module ?? expectedModuleCode,
            apiBaseUrl ?? "",
            windowsIdentity ?? "",
            permissionLevel ?? "",
            accessLevel,
            dockPermission ?? "none",
            billingPermission ?? "none",
            adminPermission ?? "none");
    }
}

public static class ModuleLaunchGuard
{
    public static LaunchContext? RequireMenuLaunch(string expectedModuleCode)
    {
        var ctx = LaunchContextReader.ReadFromCommandLine(expectedModuleCode);

        if (ctx is null)
        {
            MessageBox.Show(
                "This program must be opened from the Pyle Century Menu.\n\nPlease launch Pyle Century and select this module from your authorized menu.",
                "Open from Pyle Century Menu",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            Application.Current.Shutdown();
            return null;
        }

        return ctx;
    }
}
