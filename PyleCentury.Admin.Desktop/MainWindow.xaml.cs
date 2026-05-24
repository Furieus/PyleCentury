using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using Microsoft.Extensions.Configuration;
using PyleCentury.Shared;

namespace PyleCentury.Admin.Desktop;

public partial class MainWindow : Window
{
    private readonly HttpClient _http = new();
    private readonly LaunchContext? _launchContext;
    private readonly string _apiBaseUrl;
    private readonly ObservableCollection<AdminUserRow> _users = new();
    private AdminUserRow? _selectedUser;

    public MainWindow()
    {
        _launchContext = ModuleLaunchGuard.RequireMenuLaunch("Admin");
        if (_launchContext is null) return;

        if (!string.Equals(_launchContext.AdminPermission, "admin", StringComparison.OrdinalIgnoreCase) && _launchContext.AccessLevel != 4)
        {
            MessageBox.Show("Your access level does not allow Employee Manager access.", "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            Application.Current.Shutdown();
            return;
        }

        InitializeComponent();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        _apiBaseUrl = config["ApiBaseUrl"] ?? "https://PyleCentury.onrender.com";
        ApiText.Text = _apiBaseUrl;

        UsersList.ItemsSource = _users;
        Loaded += async (_, _) => await LoadUsersAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadUsersAsync();
    private async void Search_Click(object sender, RoutedEventArgs e) => await LoadUsersAsync();

    private async Task LoadUsersAsync()
    {
        try
        {
            SetStatus("Loading employees...");
            var query = Uri.EscapeDataString(SearchBox.Text.Trim());
            var levelIndex = AccessFilterBox.SelectedIndex;
            var levelQuery = levelIndex > 0 ? $"&access_level={levelIndex}" : "";

            var users = await _http.GetFromJsonAsync<List<AdminUserRow>>(
                $"{_apiBaseUrl.TrimEnd('/')}/admin/users?query={query}{levelQuery}");

            _users.Clear();
            foreach (var user in users ?? new List<AdminUserRow>())
            {
                _users.Add(user);
            }

            SetStatus($"Loaded {_users.Count} employee(s).");
        }
        catch (Exception ex)
        {
            SetStatus($"Load failed: {ex.Message}", true);
        }
    }

    private void UsersList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _selectedUser = UsersList.SelectedItem as AdminUserRow;
        if (_selectedUser is null) return;

        WindowsIdentityBox.Text = _selectedUser.WindowsIdentity;
        WindowsUsernameBox.Text = _selectedUser.WindowsUsername;
        EmployeeNumberBox.Text = _selectedUser.EmployeeNumber;
        DisplayNameBox.Text = _selectedUser.DisplayName;
        HomeTerminalBox.Text = _selectedUser.HomeTerminalCode;
        AccessLevelBox.SelectedIndex = Math.Clamp(_selectedUser.AccessLevel - 1, 0, 3);
        IsActiveBox.IsChecked = _selectedUser.IsActive;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        _selectedUser = null;
        UsersList.SelectedItem = null;

        WindowsIdentityBox.Text = "";
        WindowsUsernameBox.Text = "";
        EmployeeNumberBox.Text = "";
        DisplayNameBox.Text = "";
        HomeTerminalBox.Text = "CON";
        AccessLevelBox.SelectedIndex = 0;
        IsActiveBox.IsChecked = true;

        SetStatus("Ready to create a new employee.");
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await SaveCurrentAsync();
    }

    private async Task SaveCurrentAsync()
    {
        try
        {
            var request = new AdminUpsertUserRequest(
                _selectedUser?.Id,
                WindowsIdentityBox.Text.Trim(),
                WindowsUsernameBox.Text.Trim(),
                EmployeeNumberBox.Text.Trim(),
                DisplayNameBox.Text.Trim(),
                HomeTerminalBox.Text.Trim().ToUpperInvariant(),
                AccessLevelBox.SelectedIndex + 1,
                IsActiveBox.IsChecked == true);

            if (string.IsNullOrWhiteSpace(request.WindowsUsername) ||
                string.IsNullOrWhiteSpace(request.EmployeeNumber) ||
                string.IsNullOrWhiteSpace(request.DisplayName) ||
                string.IsNullOrWhiteSpace(request.HomeTerminalCode))
            {
                MessageBox.Show("Windows username, employee ID, display name, and home terminal are required.",
                    "Missing employee data",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SetStatus("Saving employee...");
            var response = await _http.PostAsJsonAsync($"{_apiBaseUrl.TrimEnd('/')}/admin/users", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"{response.StatusCode}: {body}");
            }

            await LoadUsersAsync();
            SetStatus($"Saved {request.DisplayName}.");
        }
        catch (Exception ex)
        {
            SetStatus($"Save failed: {ex.Message}", true);
        }
    }

    private async void Deactivate_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedUser is null)
        {
            MessageBox.Show("Select an employee first.", "No employee selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsActiveBox.IsChecked = false;
        await SaveCurrentAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedUser is null)
        {
            MessageBox.Show("Select an employee first.", "No employee selected", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Delete employee {_selectedUser.DisplayName}?\n\nThis should only be used for test records. Production should usually deactivate instead.",
            "Delete employee",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            SetStatus("Deleting employee...");
            var response = await _http.DeleteAsync($"{_apiBaseUrl.TrimEnd('/')}/admin/users/{Uri.EscapeDataString(_selectedUser.Id)}");

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"{response.StatusCode}: {body}");
            }

            New_Click(sender, e);
            await LoadUsersAsync();
            SetStatus("Employee deleted.");
        }
        catch (Exception ex)
        {
            SetStatus($"Delete failed: {ex.Message}", true);
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? System.Windows.Media.Brushes.OrangeRed
            : System.Windows.Media.Brushes.LightSlateGray;
    }

    private sealed record AdminUserRow(
        string Id,
        string WindowsIdentity,
        string WindowsUsername,
        string? WindowsDomain,
        string DisplayName,
        string EmployeeNumber,
        string HomeTerminalCode,
        int AccessLevel,
        bool IsActive);

    private sealed record AdminUpsertUserRequest(
        string? Id,
        string WindowsIdentity,
        string WindowsUsername,
        string EmployeeNumber,
        string DisplayName,
        string HomeTerminalCode,
        int AccessLevel,
        bool IsActive);
}
