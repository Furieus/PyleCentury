using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using PyleCentury.Shared;

namespace PyleCentury.RPS.Desktop;

public partial class MainWindow : Window
{
    private readonly LaunchContext? _launchContext;

    public MainWindow()
    {
        _launchContext = ModuleLaunchGuard.RequireMenuLaunch("RPS");
        if (_launchContext is null) return;

        InitializeComponent();

        Loaded += async (_, _) => await LoadMockupAsync();
    }

    private async Task LoadMockupAsync()
    {
        try
        {
            await RpsWebView.EnsureCoreWebView2Async();

            RpsWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            RpsWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            RpsWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            var htmlPath = Path.Combine(AppContext.BaseDirectory, "Assets", "rps_mockup.html");

            if (!File.Exists(htmlPath))
            {
                MessageBox.Show(
                    $"RPS mockup file was not found:\n\n{htmlPath}",
                    "RPS file missing",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            RpsWebView.Source = new Uri(htmlPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not load RPS mockup UI.\n\n{ex.Message}",
                "RPS startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
