using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PyleCentury.Shared;

namespace PyleCentury.RPS.Desktop;

public partial class MainWindow : Window
{
    private readonly HttpClient _http = new();
    private readonly LaunchContext? _launchContext;
    private readonly DispatcherTimer _heartbeatTimer = new();
    private readonly DispatcherTimer _lockRefreshTimer = new();

    private bool _loadingBuckets;
    private bool _loadingLock;
    private bool _readOnly = true;

    private readonly List<RpsBucketDisplay> _bucketDisplays = new();
    private RpsBucketRow? _selectedBucket;

    public MainWindow()
    {
        _launchContext = ModuleLaunchGuard.RequireMenuLaunch("RPS");
        if (_launchContext is null) return;

        InitializeComponent();

        if (!_launchContext.ModuleCode.Equals("RPS", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Invalid module launch context.", "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            Application.Current.Shutdown();
            return;
        }

        UserText.Text = $"{_launchContext.DisplayName} · {_launchContext.EmployeeNumber} · {_launchContext.HomeTerminalCode}";
        TerminalButtonText.Text = _launchContext.HomeTerminalCode;

        _heartbeatTimer.Interval = TimeSpan.FromSeconds(30);
        _heartbeatTimer.Tick += async (_, _) => await HeartbeatLockAsync();

        _lockRefreshTimer.Interval = TimeSpan.FromSeconds(20);
        _lockRefreshTimer.Tick += async (_, _) =>
        {
            await LoadLocksForTerminalAsync();
            await LoadBucketsAsync(preserveSelected: true);
        };

        Loaded += async (_, _) =>
        {
            ApplyReadOnlyMode(true, null, "Select a bucket");
            await LoadBucketsAsync();
            await LoadLocksForTerminalAsync();
            _lockRefreshTimer.Start();
        };

        Closing += async (_, _) => await ReleaseLockQuietlyAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadBucketsAsync(preserveSelected: true);
        await LoadLocksForTerminalAsync();
    }

    private void TerminalButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Only your home terminal is enabled in this lockout test build.", "Terminal locked", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BucketButton_Click(object sender, RoutedEventArgs e)
    {
        BucketPopup.IsOpen = true;
    }

    private async void BucketDropdownList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingBuckets || _loadingLock) return;

        if (BucketDropdownList.SelectedItem is not RpsBucketDisplay item)
        {
            return;
        }

        BucketPopup.IsOpen = false;
        _selectedBucket = item.Bucket;
        BucketButtonText.Text = item.DisplayText;

        await AcquireOrViewLockAsync(item.Bucket);
    }

    private async void ReleaseLock_Click(object sender, RoutedEventArgs e)
    {
        await ReleaseLockQuietlyAsync();
        await LoadLocksForTerminalAsync();
        await LoadBucketsAsync(preserveSelected: true);
    }

    private async Task LoadBucketsAsync(bool preserveSelected = false)
    {
        if (_launchContext is null) return;

        try
        {
            _loadingBuckets = true;
            StatusText.Text = "Loading route buckets...";

            var terminal = _launchContext.HomeTerminalCode;
            var locks = await GetLocksAsync(terminal);
            var url = $"{_launchContext.ApiBaseUrl.TrimEnd('/')}/rps/terminals/{terminal}/buckets";
            var buckets = await _http.GetFromJsonAsync<List<RpsBucketRow>>(url) ?? new();

            var selectedId = preserveSelected ? _selectedBucket?.Id : null;
            _bucketDisplays.Clear();
            BucketDropdownList.Items.Clear();

            foreach (var bucket in buckets.OrderBy(b => b.DisplayOrder).ThenBy(b => b.DisplayName))
            {
                var lockRow = locks.FirstOrDefault(l => string.Equals(l.RouteAreaId, bucket.Id, StringComparison.OrdinalIgnoreCase));
                var display = new RpsBucketDisplay(bucket, lockRow);
                _bucketDisplays.Add(display);
                BucketDropdownList.Items.Add(BuildBucketDropdownItem(display));

                if (!string.IsNullOrWhiteSpace(selectedId) && string.Equals(selectedId, bucket.Id, StringComparison.OrdinalIgnoreCase))
                {
                    BucketButtonText.Text = display.DisplayText;
                }
            }

            StopsMetric.Text = buckets.Count.ToString();
            WeightMetric.Text = "--";
            CubeMetric.Text = "--";

            if (buckets.Count == 0)
            {
                BucketButtonText.Text = "No route buckets found";
            }

            StatusText.Text = $"Loaded {buckets.Count} bucket(s).";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Bucket load failed: {ex.Message}";
        }
        finally
        {
            _loadingBuckets = false;
        }
    }

    private ListBoxItem BuildBucketDropdownItem(RpsBucketDisplay display)
    {
        var item = new ListBoxItem
        {
            Tag = display,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = Brushes.White
        };

        var border = new Border
        {
            Background = display.IsLocked ? new SolidColorBrush(Color.FromRgb(36, 17, 6)) : new SolidColorBrush(Color.FromRgb(7, 16, 24)),
            BorderBrush = display.IsLocked ? new SolidColorBrush(Color.FromRgb(249, 115, 22)) : new SolidColorBrush(Color.FromRgb(38, 50, 65)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 3, 0, 3)
        };

        var dock = new DockPanel();

        var right = new TextBlock
        {
            Text = display.IsLocked ? $"👁 {display.Lock!.LockedByDisplayName}" : "Available",
            Foreground = display.IsLocked ? new SolidColorBrush(Color.FromRgb(253, 186, 116)) : new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        DockPanel.SetDock(right, Dock.Right);
        dock.Children.Add(right);

        var left = new TextBlock
        {
            Text = display.Bucket.DisplayName,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };

        dock.Children.Add(left);
        border.Child = dock;
        item.Content = border;

        return item;
    }

    private async Task LoadLocksForTerminalAsync()
    {
        if (_launchContext is null) return;

        try
        {
            var terminal = _launchContext.HomeTerminalCode;
            var locks = await GetLocksAsync(terminal);

            LocksList.Items.Clear();

            if (locks.Count == 0)
            {
                LocksList.Items.Add("No active bucket locks.");
                return;
            }

            foreach (var l in locks)
            {
                LocksList.Items.Add($"👁 {l.DisplayRouteName} — {l.LockedByDisplayName}");
            }
        }
        catch
        {
        }
    }

    private async Task<List<RpsActiveLockRow>> GetLocksAsync(string terminal)
    {
        if (_launchContext is null) return new();

        try
        {
            var url = $"{_launchContext.ApiBaseUrl.TrimEnd('/')}/rps/buckets/{terminal}/locks";
            return await _http.GetFromJsonAsync<List<RpsActiveLockRow>>(url) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private async Task AcquireOrViewLockAsync(RpsBucketRow bucket)
    {
        if (_launchContext is null) return;

        try
        {
            _loadingLock = true;
            StatusText.Text = $"Checking lock for {bucket.DisplayName}...";

            var terminal = _launchContext.HomeTerminalCode;
            var url = $"{_launchContext.ApiBaseUrl.TrimEnd('/')}/rps/buckets/{terminal}/{bucket.Id}/lock";

            var response = await _http.PostAsJsonAsync(url, new RpsLockRequest(
                _launchContext.EmployeeNumber,
                Environment.MachineName,
                false));

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"{response.StatusCode}: {body}");
            }

            var lockInfo = await response.Content.ReadFromJsonAsync<RpsBucketLockResponse>();
            var readOnly = lockInfo?.ReadOnly ?? true;

            ApplyReadOnlyMode(readOnly, lockInfo, lockInfo?.Message);
            LoadSampleBucketData(bucket, readOnly);

            if (!readOnly)
            {
                _heartbeatTimer.Start();
            }
            else
            {
                _heartbeatTimer.Stop();
            }

            await LoadLocksForTerminalAsync();
            await LoadBucketsAsync(preserveSelected: true);

            StatusText.Text = lockInfo?.Message ?? "Bucket lock checked.";
        }
        catch (Exception ex)
        {
            _heartbeatTimer.Stop();
            ApplyReadOnlyMode(true, null, "Lock check failed");
            StatusText.Text = $"Lock check failed: {ex.Message}";
        }
        finally
        {
            _loadingLock = false;
        }
    }

    private void ApplyReadOnlyMode(bool readOnly, RpsBucketLockResponse? lockInfo, string? message)
    {
        _readOnly = readOnly;

        MoveToRunButton.IsEnabled = !readOnly;
        RemoveStopButton.IsEnabled = !readOnly;
        ClipButton.IsEnabled = !readOnly;
        UnclipButton.IsEnabled = !readOnly;
        SaveRunButton.IsEnabled = !readOnly;

        ReadOnlyTopPill.Visibility = readOnly && _selectedBucket is not null ? Visibility.Visible : Visibility.Collapsed;

        if (readOnly)
        {
            var owner = lockInfo?.LockOwnerName ?? "another user";
            LockBannerText.Text = message ?? $"READ ONLY — routed by {owner}";
            ModeMetric.Text = _selectedBucket is null ? "--" : "READ ONLY";
            ModeMetric.Foreground = Brushes.OrangeRed;
            ReadOnlyBadgeLeft.Text = _selectedBucket is null ? "" : "VIEW ONLY";
            ReadOnlyBadgeCenter.Text = _selectedBucket is null ? "" : "VIEW ONLY";
        }
        else
        {
            LockBannerText.Text = message ?? "EDIT MODE — you own this bucket lock";
            ModeMetric.Text = "EDIT";
            ModeMetric.Foreground = Brushes.LightGreen;
            ReadOnlyBadgeLeft.Text = "";
            ReadOnlyBadgeCenter.Text = "";
        }
    }

    private void LoadSampleBucketData(RpsBucketRow bucket, bool readOnly)
    {
        BucketStopsList.Items.Clear();
        ActiveRunList.Items.Clear();

        BucketStopsList.Items.Add($"{bucket.DisplayName} · sample stop 1 · 3 bills · 1,250 lb");
        BucketStopsList.Items.Add($"{bucket.DisplayName} · sample stop 2 · 1 bill · 420 lb");
        BucketStopsList.Items.Add($"{bucket.DisplayName} · sample stop 3 · 5 bills · 2,800 lb");

        ActiveRunList.Items.Add(readOnly
            ? "Read-only: routing actions disabled because someone else owns the bucket."
            : "Editable: move freight into this run.");

        ActiveRunList.Items.Add("Real freight stop data wires in after lockout test.");
    }

    private async Task HeartbeatLockAsync()
    {
        if (_launchContext is null || _selectedBucket is null || _readOnly) return;

        try
        {
            var terminal = _launchContext.HomeTerminalCode;
            var url = $"{_launchContext.ApiBaseUrl.TrimEnd('/')}/rps/buckets/{terminal}/{_selectedBucket.Id}/lock/heartbeat";
            _ = await _http.PostAsJsonAsync(url, new RpsLockRequest(
                _launchContext.EmployeeNumber,
                Environment.MachineName,
                false));

            StatusText.Text = $"Lock heartbeat OK: {_selectedBucket.DisplayName}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Lock heartbeat failed: {ex.Message}";
        }
    }

    private async Task ReleaseLockQuietlyAsync()
    {
        if (_launchContext is null || _selectedBucket is null || _readOnly) return;

        try
        {
            _heartbeatTimer.Stop();

            var terminal = _launchContext.HomeTerminalCode;
            var url = $"{_launchContext.ApiBaseUrl.TrimEnd('/')}/rps/buckets/{terminal}/{_selectedBucket.Id}/lock?employee_number={Uri.EscapeDataString(_launchContext.EmployeeNumber)}";
            await _http.DeleteAsync(url);

            LockBannerText.Text = "Lock released.";
            StatusText.Text = "Bucket lock released.";
            ApplyReadOnlyMode(true, null, "Select a bucket");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Release lock failed: {ex.Message}";
        }
    }

    private sealed record RpsBucketRow(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("terminal_code")] string TerminalCode,
        [property: JsonPropertyName("route_area_name")] string? RouteAreaName,
        [property: JsonPropertyName("display_order")] int DisplayOrder,
        [property: JsonPropertyName("is_active")] bool IsActive)
    {
        public string DisplayName => !string.IsNullOrWhiteSpace(RouteAreaName) ? RouteAreaName! : Id;
    }

    private sealed record RpsActiveLockRow(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("terminal_code")] string TerminalCode,
        [property: JsonPropertyName("route_area_id")] string RouteAreaId,
        [property: JsonPropertyName("route_area_name")] string? RouteAreaName,
        [property: JsonPropertyName("locked_by_employee_id")] string LockedByEmployeeId,
        [property: JsonPropertyName("locked_by_display_name")] string LockedByDisplayName,
        [property: JsonPropertyName("locked_by_windows_username")] string? LockedByWindowsUsername,
        [property: JsonPropertyName("lock_mode")] string LockMode,
        [property: JsonPropertyName("acquired_at")] string AcquiredAt,
        [property: JsonPropertyName("heartbeat_at")] string HeartbeatAt,
        [property: JsonPropertyName("expires_at")] string ExpiresAt,
        [property: JsonPropertyName("client_id")] string? ClientId)
    {
        public string DisplayRouteName => !string.IsNullOrWhiteSpace(RouteAreaName) ? RouteAreaName! : RouteAreaId;
    }

    private sealed record RpsBucketLockResponse(
        [property: JsonPropertyName("routeAreaId")] string RouteAreaId,
        [property: JsonPropertyName("terminalCode")] string TerminalCode,
        [property: JsonPropertyName("locked")] bool Locked,
        [property: JsonPropertyName("readOnly")] bool ReadOnly,
        [property: JsonPropertyName("lockOwnerName")] string? LockOwnerName,
        [property: JsonPropertyName("lockOwnerEmployeeNumber")] string? LockOwnerEmployeeNumber,
        [property: JsonPropertyName("lockOwnerWindowsUsername")] string? LockOwnerWindowsUsername,
        [property: JsonPropertyName("expiresAt")] string? ExpiresAt,
        [property: JsonPropertyName("lockId")] string? LockId,
        [property: JsonPropertyName("message")] string? Message);

    private sealed record RpsLockRequest(
        [property: JsonPropertyName("employeeNumber")] string EmployeeNumber,
        [property: JsonPropertyName("clientId")] string? ClientId,
        [property: JsonPropertyName("force")] bool Force);

    private sealed class RpsBucketDisplay
    {
        public RpsBucketRow Bucket { get; }
        public RpsActiveLockRow? Lock { get; }
        public bool IsLocked => Lock is not null;
        public string DisplayText => Lock is null ? Bucket.DisplayName : $"👁 {Bucket.DisplayName} — {Lock.LockedByDisplayName}";

        public RpsBucketDisplay(RpsBucketRow bucket, RpsActiveLockRow? lockRow)
        {
            Bucket = bucket;
            Lock = lockRow;
        }

        public override string ToString() => DisplayText;
    }
}
