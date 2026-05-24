using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace DockCommander.Desktop;

public partial class SplashWindow : Window, INotifyPropertyChanged
{
    private int _loadProgress;
    private string _loadingMessage = "Preparing dock operations dashboard...";
    private Visibility _continueVisibility = Visibility.Collapsed;

    public int LoadProgress
    {
        get => _loadProgress;
        set
        {
            if (_loadProgress == value) return;
            _loadProgress = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public string ProgressText => $"{LoadProgress}%";

    public string LoadingMessage
    {
        get => _loadingMessage;
        set
        {
            if (_loadingMessage == value) return;
            _loadingMessage = value;
            OnPropertyChanged();
        }
    }

    public Visibility ContinueVisibility
    {
        get => _continueVisibility;
        set
        {
            if (_continueVisibility == value) return;
            _continueVisibility = value;
            OnPropertyChanged();
        }
    }

    public SplashWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += async (_, _) => await RunStartupSequenceAsync();
    }

    private async Task RunStartupSequenceAsync()
    {
        var steps = new[]
        {
            "Checking dock door control map...",
            "Loading rack layout...",
            "Connecting activity feed...",
            "Preparing KPI panels...",
            "Syncing operations view...",
            "Finalizing Dock Commander..."
        };

        foreach (var step in steps)
        {
            LoadingMessage = step;

            for (var i = 0; i < 16; i++)
            {
                if (LoadProgress < 100)
                {
                    LoadProgress++;
                }

                await Task.Delay(22);
            }
        }

        while (LoadProgress < 100)
        {
            LoadProgress++;
            await Task.Delay(12);
        }

        LoadingMessage = "Startup complete. Ready to open Dock Commander.";
        ContinueVisibility = Visibility.Visible;
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = new MainWindow();
        Application.Current.MainWindow = mainWindow;
        Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
        Close();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
