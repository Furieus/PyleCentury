using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using PyleCentury.Shared;

namespace PyleCentury.RPS.Desktop;

public partial class MainWindow : Window
{
    private readonly HttpClient _http = new();
    private readonly LaunchContext? _launchContext;

    public MainWindow()
    {
        _launchContext = ModuleLaunchGuard.RequireMenuLaunch("RPS");
        if (_launchContext is null) return;

        InitializeComponent();

        UserText.Text = $"{_launchContext.DisplayName} · {_launchContext.EmployeeId} · {_launchContext.TerminalCode}";
        TerminalBox.Items.Add(_launchContext.TerminalCode);
        TerminalBox.SelectedIndex = 0;

        StatusText.Text = "RPS desktop shell loaded. Next build wires bucket data, locks, and drag/drop.";
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Refresh requested. Bucket API wiring comes next.";
    }

    private async void TerminalBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        await LoadBucketsAsync();
    }

    private void BucketBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        LockBannerText.Text = "Bucket selected. Lock check wiring comes next.";
    }

    private async Task LoadBucketsAsync()
    {
        if (_launchContext is null || TerminalBox.SelectedItem is null) return;

        try
        {
            var terminal = TerminalBox.SelectedItem.ToString();
            var url = $"{_launchContext.ApiBaseUrl.TrimEnd('/')}/rps/terminals/{terminal}/buckets";
            var buckets = await _http.GetFromJsonAsync<List<RpsBucketRow>>(url) ?? new();

            BucketBox.Items.Clear();

            foreach (var bucket in buckets)
            {
                BucketBox.Items.Add(bucket.RouteAreaName);
            }

            StatusText.Text = $"Loaded {buckets.Count} RPS bucket(s).";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Bucket load failed: {ex.Message}";
        }
    }

    private sealed record RpsBucketRow(
        string Id,
        string TerminalCode,
        string RouteAreaName,
        int DisplayOrder,
        bool IsActive);
}
