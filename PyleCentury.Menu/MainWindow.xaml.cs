using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Extensions.Configuration;
using PyleCentury.Shared;

namespace PyleCentury.Menu;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient = new();
    private readonly string _apiBaseUrl;
    private readonly string _installRoot;
    private readonly string _appsRoot;
    private EmployeeProfile? _profile;

    public MainWindow()
    {
        InitializeComponent();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        _apiBaseUrl = config["ApiBaseUrl"] ?? "https://PyleCentury.onrender.com";
        _installRoot = config["InstallRoot"] ?? @"C:\PyleCentury";
        _appsRoot = config["AppsRoot"] ?? Path.Combine(_installRoot, "apps");

        UserText.Text = WindowsIdentityHelper.WindowsUserName;
        Loaded += async (_, _) => await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            SetStatus("Loading employee profile...");
            SetModuleButtons(false);

            var url = $"{_apiBaseUrl.TrimEnd(\'/\')}/auth/windows-profile?windows_username={Uri.EscapeDataString(WindowsIdentityHelper.WindowsUserName)}";
            _profile = await _httpClient.GetFromJsonAsync<EmployeeProfile>(url);

            if (_profile is null)
            {
                UserText.Text = WindowsIdentityHelper.WindowsUserName;
                EmployeeIdText.Text = "-";
                HomeTerminalText.Text = "-";
                ProfileMessageText.Text = "No profile found. Use Employee Manager or contact an admin.";
                AdminButton.Visibility = Visibility.Visible;
                SetStatus("No employee profile found for this Windows username.", true);
                return;
            }

            UserText.Text = !string.IsNullOrWhiteSpace(_profile.DisplayName)
                ? _profile.DisplayName
                : WindowsIdentityHelper.WindowsUserName;

            EmployeeIdText.Text = _profile.EmployeeNumber;
            HomeTerminalText.Text = _profile.HomeTerminalCode;
            ProfileMessageText.Text = "";

            ApplyPermissions();
            AdminButton.Visibility = _profile.AccessLevel == 4 ? Visibility.Visible : Visibility.Collapsed;
            SetStatus($"Profile loaded. User: {UserText.Text} · ID: {_profile.EmployeeNumber} · Terminal: {_profile.HomeTerminalCode}");
        }
        catch (Exception ex)
        {
            UserText.Text = WindowsIdentityHelper.WindowsUserName;
            EmployeeIdText.Text = "-";
            HomeTerminalText.Text = "-";
            ProfileMessageText.Text = "Backend unavailable";
            AdminButton.Visibility = Visibility.Visible;
            SetModuleButtons(false);
            SetStatus($"Profile lookup failed: {ex.Message}", true);
        }
    }

    private async void RefreshProfile_Click(object sender, RoutedEventArgs e)
    {
        await LoadProfileAsync();
    }

    private void ApplyPermissions()
    {
        SetModuleButtons(true);
    }

    private void SetModuleButtons(bool enabled)
    {
        if (_profile is null)
        {
            DockCommanderButton.IsEnabled = false;
            BillingButton.IsEnabled = false;
            return;
        }

        DockCommanderButton.IsEnabled = enabled && _profile.AccessLevel is >= 1 and <= 4;
        BillingButton.IsEnabled = enabled && (_profile.AccessLevel == 3 || _profile.AccessLevel == 4);
    }

    private void Admin_Click(object sender, RoutedEventArgs e)
    {
        ShowAdminFront();
    }

    private void BackToApps_Click(object sender, RoutedEventArgs e)
    {
        ShowProgramsFront();
    }

    private void ShowAdminFront()
    {
        AdminFront.Visibility = Visibility.Visible;

        var outAnim = new DoubleAnimation(0, -1140, TimeSpan.FromMilliseconds(220));
        var inAnim = new DoubleAnimation(1140, 0, TimeSpan.FromMilliseconds(220));

        outAnim.Completed += (_, _) => ProgramsFront.Visibility = Visibility.Collapsed;

        ProgramsTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, outAnim);
        AdminTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, inAnim);

        SetStatus("Admin area opened.");
    }

    private void ShowProgramsFront()
    {
        ProgramsFront.Visibility = Visibility.Visible;

        var inAnim = new DoubleAnimation(-1140, 0, TimeSpan.FromMilliseconds(220));
        var outAnim = new DoubleAnimation(0, 1140, TimeSpan.FromMilliseconds(220));

        outAnim.Completed += (_, _) => AdminFront.Visibility = Visibility.Collapsed;

        ProgramsTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, inAnim);
        AdminTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, outAnim);

        SetStatus("Application area opened.");
    }

    private async void OpenDockCommander_Click(object sender, RoutedEventArgs e)
    {
        await LaunchAppAsync(
            "Dock Commander",
            "DockCommander",
            "DockCommander.Desktop",
            "DockCommander.exe",
            "DockCommander.Desktop.csproj");
    }

    private async void OpenBilling_Click(object sender, RoutedEventArgs e)
    {
        await LaunchAppAsync(
            "Billing",
            "Billing",
            "PyleCentury.Billing.Desktop",
            "PyleCentury.Billing.exe",
            "PyleCentury.Billing.Desktop.csproj");
    }

    private void OpenEmployeeManager_Click(object sender, RoutedEventArgs e)
    {
        var pathToLaunch = FindAppExecutable(
            "PyleCentury.Admin.Desktop",
            "PyleCentury.Admin.exe");

        if (string.IsNullOrWhiteSpace(pathToLaunch))
        {
            MessageBox.Show(
                "Employee Manager was not found.\n\nBuild/publish PyleCentury.Admin.Desktop, then try again.",
                "Application not found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = pathToLaunch,
            WorkingDirectory = Path.GetDirectoryName(pathToLaunch)!,
            UseShellExecute = true
        });

        SetStatus("Opened Employee Manager.");
    }

    private async Task LaunchAppAsync(string appName, string moduleCode, string projectFolderName, string executableName, string projectFileName)
    {
        if (_profile is null)
        {
            MessageBox.Show("No employee profile is loaded for this Windows username.",
                "Employee profile required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!CanOpenModule(moduleCode))
        {
            MessageBox.Show($"You are not authorized for {appName}.", "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var token = await CreateLaunchSessionAsync(moduleCode);
        if (string.IsNullOrWhiteSpace(token))
        {
            MessageBox.Show($"Could not create a launch session for {appName}.", "Launch session failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pathToLaunch = FindAppExecutable(projectFolderName, executableName);

        if (string.IsNullOrWhiteSpace(pathToLaunch))
        {
            SetStatus($"{appName} executable not found. Build/publish that project first.", true);

            MessageBox.Show(
                $"{appName} was not found.\n\nRun publish-all-windows-release.bat from the suite root, then try again.",
                "Application not found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        var args = BuildLaunchArgs(token, moduleCode);

        Process.Start(new ProcessStartInfo
        {
            FileName = pathToLaunch,
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(pathToLaunch)!,
            UseShellExecute = true
        });

        SetStatus($"Launched {appName} for {UserText.Text} · terminal {_profile.HomeTerminalCode}.");
    }

    private bool CanOpenModule(string moduleCode)
    {
        if (_profile is null) return false;

        return _profile.AccessLevel switch
        {
            1 => moduleCode.Equals("DockCommander", StringComparison.OrdinalIgnoreCase),
            2 => moduleCode.Equals("DockCommander", StringComparison.OrdinalIgnoreCase),
            3 => moduleCode.Equals("DockCommander", StringComparison.OrdinalIgnoreCase) ||
                 moduleCode.Equals("Billing", StringComparison.OrdinalIgnoreCase),
            4 => true,
            _ => false
        };
    }

    private string DockPermissionForTerminal(string requestedTerminal)
    {
        if (_profile is null) return "none";

        var sameHome = string.Equals(_profile.HomeTerminalCode, requestedTerminal, StringComparison.OrdinalIgnoreCase);

        return _profile.AccessLevel switch
        {
            1 => sameHome ? "read_write" : "read_only",
            2 => "read_write",
            3 => "read_only",
            4 => "admin",
            _ => "none"
        };
    }

    private string BillingPermission()
    {
        if (_profile is null) return "none";

        return _profile.AccessLevel switch
        {
            3 => "read_write",
            4 => "admin",
            _ => "none"
        };
    }

    private async Task<string?> CreateLaunchSessionAsync(string moduleCode)
    {
        try
        {
            var request = new CreateLaunchSessionRequest(
                WindowsIdentityHelper.WindowsIdentity,
                _profile!.EmployeeNumber,
                moduleCode,
                _profile.HomeTerminalCode);

            var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl.TrimEnd('/')}/auth/menu-session", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"{response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<CreateLaunchSessionResponse>();
            return result?.SessionToken;
        }
        catch (Exception ex)
        {
            SetStatus($"Launch session failed: {ex.Message}", true);
            return null;
        }
    }

    private string BuildLaunchArgs(string token, string moduleCode)
    {
        var parts = new[]
        {
            ("pyle-session-token", token),
            ("employee-id", _profile!.EmployeeNumber),
            ("display-name", _profile.DisplayName),
            ("terminal", _profile.HomeTerminalCode),
            ("module", moduleCode),
            ("api-base-url", _apiBaseUrl),
            ("windows-identity", WindowsIdentityHelper.WindowsIdentity),
            ("permission-level", _profile.PermissionLevel),
            ("access-level", _profile.AccessLevel.ToString()),
            ("dock-permission", DockPermissionForTerminal(_profile.HomeTerminalCode)),
            ("billing-permission", BillingPermission()),
            ("admin-permission", _profile.AccessLevel == 4 ? "admin" : "none")
        };

        return string.Join(" ", parts.Select(p => $"--{p.Item1} {Quote(p.Item2)}"));
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private string? FindAppExecutable(string projectFolderName, string executableName)
    {
        var suiteRoot = FindSuiteRoot();

        var candidates = new[]
        {
            Path.Combine(_appsRoot, projectFolderName, executableName),
            Path.Combine(_appsRoot, AppFolderName(projectFolderName), executableName),
            Path.Combine(_installRoot, "apps", projectFolderName, executableName),
            Path.Combine(_installRoot, "apps", AppFolderName(projectFolderName), executableName),

            Path.Combine(suiteRoot, projectFolderName, "release", "win-x64", executableName),
            Path.Combine(suiteRoot, projectFolderName, "bin", "Debug", "net10.0-windows", executableName),
            Path.Combine(suiteRoot, projectFolderName, "bin", "Release", "net10.0-windows", "win-x64", executableName),
            Path.Combine(AppContext.BaseDirectory, "Apps", projectFolderName, executableName),
            Path.Combine(AppContext.BaseDirectory, projectFolderName, executableName)
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string AppFolderName(string projectFolderName)
    {
        return projectFolderName switch
        {
            "DockCommander.Desktop" => "DockCommander",
            "PyleCentury.Billing.Desktop" => "Billing",
            "PyleCentury.Admin.Desktop" => "Admin",
            _ => projectFolderName
        };
    }

    private static string FindSuiteRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "DockCommander.Desktop")) &&
                Directory.Exists(Path.Combine(dir.FullName, "PyleCentury.Billing.Desktop")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return AppContext.BaseDirectory;
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? System.Windows.Media.Brushes.OrangeRed
            : System.Windows.Media.Brushes.LightSlateGray;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private sealed record CreateLaunchSessionRequest(
        string WindowsIdentity,
        string EmployeeNumber,
        string ModuleCode,
        string TerminalCode);

    private sealed record CreateLaunchSessionResponse(string SessionToken);
}
