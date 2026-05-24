using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PyleCentury.Shared;

namespace DockCommander.Desktop;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private LaunchContext? _launchContext;
    private DockTerminal? _selectedTerminal;
    private bool _isApplyingTerminal;
    public ObservableCollection<DockTerminal> Terminals { get; } = new(DockTerminalCatalog.All);
    private DoorDetailsViewModel? _selectedDoorDetails;

    public ObservableCollection<DoorViewModel> TopDoors { get; } = new();
    public ObservableCollection<DoorViewModel> BottomDoors { get; } = new();
    public ObservableCollection<RackViewModel> LeftRacks { get; } = new();
    public ObservableCollection<RackViewModel> RightRacks { get; } = new();
    public ObservableCollection<ActivityViewModel> Activities { get; } = new();

    public DoorDetailsViewModel? SelectedDoorDetails
    {
        get => _selectedDoorDetails;
        set
        {
            if (_selectedDoorDetails == value) return;
            _selectedDoorDetails = value;
            OnPropertyChanged();
        }
    }

    private string _topDockLabel = "NORTH DOCK (DRIVE-IN)";
    private string _bottomDockLabel = "SOUTH DOCK (DRIVE-IN)";
    private string _doorCountDisplay = "/60";

    public string TopDockLabel
    {
        get => _topDockLabel;
        set
        {
            if (_topDockLabel == value) return;
            _topDockLabel = value;
            OnPropertyChanged();
        }
    }

    public string BottomDockLabel
    {
        get => _bottomDockLabel;
        set
        {
            if (_bottomDockLabel == value) return;
            _bottomDockLabel = value;
            OnPropertyChanged();
        }
    }

    public string DoorCountDisplay
    {
        get => _doorCountDisplay;
        set
        {
            if (_doorCountDisplay == value) return;
            _doorCountDisplay = value;
            OnPropertyChanged();
        }
    }

    public MainWindow()
    {
        _launchContext = ModuleLaunchGuard.RequireMenuLaunch("DockCommander");
        if (_launchContext is null) return;

        InitializeComponent();
        DataContext = this;
        InitializeTerminalSelector();
        LoadSampleData();

        var terminal = _selectedTerminal?.Code ?? _launchContext.HomeTerminalCode;
        Title = $"Dock Commander · {terminal} · {_launchContext.DisplayName}";
    }

    private void LoadSampleData()
    {
        LoadDoors();
        LoadRacks();
        LoadActivities();
    }

    private void LoadDoors()
    {
        TopDoors.Clear();
        BottomDoors.Clear();

        var terminalCode = _selectedTerminal?.Code ?? _launchContext?.HomeTerminalCode ?? "CON";

        if (string.Equals(terminalCode, "NYN", StringComparison.OrdinalIgnoreCase))
        {
            LoadNynDoors();
            return;
        }

        LoadConDefaultDoors();
    }

    private void LoadConDefaultDoors()
    {
        TopDockLabel = "NORTH DOCK (DRIVE-IN)";
        BottomDockLabel = "SOUTH DOCK (DRIVE-IN)";
        DoorCountDisplay = "/60";

        var doorFiveDetails = BuildSampleDoorDetails();

        var inUse = new HashSet<int> { 5, 7, 37, 60 };
        var delayed = new HashSet<int> { 9, 20, 43, 53 };
        var available = new HashSet<int> { 1, 2, 12, 16, 24, 30, 33, 39, 49, 50, 58, 59 };

        // 30 equal slots across the top.
        for (var door = 60; door >= 31; door--)
        {
            TopDoors.Add(CreateDoor(door, available, inUse, delayed, doorFiveDetails));
        }

        // 30 equal slots across the bottom.
        for (var door = 1; door <= 30; door++)
        {
            BottomDoors.Add(CreateDoor(door, available, inUse, delayed, doorFiveDetails));
        }
    }

    private void LoadNynDoors()
    {
        TopDockLabel = "NYN UPPER DOCK";
        BottomDockLabel = "NYN LOWER DOCK";
        DoorCountDisplay = "/56";

        var sampleDetails = BuildSampleDoorDetails();

        // Demo statuses only. These are not copied from the screenshots.
        var inUse = new HashSet<int> { 5, 7, 21, 37, 55 };
        var delayed = new HashSet<int> { 9, 22, 40, 53 };
        var available = new HashSet<int> { 1, 2, 12, 16, 24, 29, 30, 33, 39, 49, 50, 56 };

        // NYN geometry:
        // Top row is 30 equal slots:
        // 12..1, four open spacer slots, 56..43.
        // This makes 12 line up with 13, 56 line up with 29, and 43 line up with 42.
        for (var door = 12; door >= 1; door--)
        {
            TopDoors.Add(CreateDoor(door, available, inUse, delayed, sampleDetails));
        }

        for (var i = 0; i < 4; i++)
        {
            TopDoors.Add(CreateSpacerDoor());
        }

        for (var door = 56; door >= 43; door--)
        {
            TopDoors.Add(CreateDoor(door, available, inUse, delayed, sampleDetails));
        }

        // Bottom row is continuous 13..42 with no blank gap between 24 and 25.
        for (var door = 13; door <= 42; door++)
        {
            BottomDoors.Add(CreateDoor(door, available, inUse, delayed, sampleDetails));
        }
    }

    private DoorDetailsViewModel BuildSampleDoorDetails()
    {
        return new DoorDetailsViewModel
        {
            DoorNumber = 5,
            Route = "Live terminal data",
            Trailer = "N/A",
            TotalBills = 0,
            LoadedPercent = 0,
            WeightPounds = 0,
            HazmatPlacards = new ObservableCollection<HazmatPlacardViewModel>()
        };
    }

    private DoorViewModel CreateSpacerDoor()
    {
        return new DoorViewModel
        {
            IsSpacer = true,
            Status = DoorStatus.Dead
        };
    }

    private DoorViewModel CreateDoor(
        int door,
        HashSet<int> available,
        HashSet<int> inUse,
        HashSet<int> delayed,
        DoorDetailsViewModel doorFiveDetails)
    {
        var status =
            delayed.Contains(door) ? DoorStatus.Delayed :
            inUse.Contains(door) ? DoorStatus.InUse :
            available.Contains(door) ? DoorStatus.Available :
            DoorStatus.Offline;

        return new DoorViewModel
        {
            DoorNumber = door,
            Status = status,
            Details = door == 5 ? doorFiveDetails : null
        };
    }

    private void LoadRacks()
    {
        var rackNames = Enumerable.Range('A', 19).Select(c => ((char)c).ToString()).ToList();

        foreach (var rackName in rackNames)
        {
            var rack = new RackViewModel { Name = rackName };

            for (var bottom = 1; bottom <= 19; bottom += 2)
            {
                var top = bottom + 1;

                rack.Pairs.Add(new RackPairViewModel
                {
                    BottomSlot = new RackSlotViewModel
                    {
                        SlotNumber = bottom,
                        Status = GetSampleRackStatus(rackName, bottom)
                    },
                    TopSlot = new RackSlotViewModel
                    {
                        SlotNumber = top,
                        Status = GetSampleRackStatus(rackName, top)
                    }
                });
            }

            if (rackName[0] <= 'I')
            {
                LeftRacks.Add(rack);
            }
            else
            {
                RightRacks.Add(rack);
            }
        }
    }

    private DoorStatus GetSampleRackStatus(string rackName, int slot)
    {
        var key = $"{rackName}{slot}";

        return key switch
        {
            "A3" or "A4" or "E3" or "G5" or "H9" or "P11" or "R17" => DoorStatus.Available,
            "E9" or "F10" or "K11" or "M9" or "P16" or "R7" => DoorStatus.InUse,
            "C5" or "C6" or "G17" or "H11" or "H12" or "N13" or "S17" or "S18" => DoorStatus.Delayed,
            _ => DoorStatus.Offline
        };
    }

    private void LoadActivities()
    {
        Activities.Add(new ActivityViewModel { Door = "Door 14", Message = "Strip completed", Time = "9:40 AM", AccentBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)) });
        Activities.Add(new ActivityViewModel { Door = "Door 22", Message = "Load delayed", Time = "9:35 AM", AccentBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)) });
        Activities.Add(new ActivityViewModel { Door = "Trailer 53124", Message = "Checked in", Time = "9:31 AM", AccentBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)) });
        Activities.Add(new ActivityViewModel { Door = "Door 3", Message = "Strip completed", Time = "9:28 AM", AccentBrush = new SolidColorBrush(Color.FromRgb(34, 197, 94)) });
        Activities.Add(new ActivityViewModel { Door = "Door 54", Message = "Load delayed", Time = "9:22 AM", AccentBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)) });
        Activities.Add(new ActivityViewModel { Door = "Door 5", Message = "New London Wave loading", Time = "9:18 AM", AccentBrush = new SolidColorBrush(Color.FromRgb(249, 115, 22)) });
    }

    private void DoorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not DoorViewModel door)
        {
            return;
        }

        if (door.IsSpacer || door.IsDeadDoor)
        {
            SelectedDoorDetails = null;
            HideDoorDetailsPanel();
            return;
        }

        if (door.Details is null)
        {
            SelectedDoorDetails = new DoorDetailsViewModel
            {
                DoorNumber = door.DoorNumber,
                Route = "No active route",
                Trailer = "N/A",
                TotalBills = 0,
                LoadedPercent = 0,
                WeightPounds = 0,
                HazmatPlacards = new ObservableCollection<HazmatPlacardViewModel>()
            };
        }
        else
        {
            SelectedDoorDetails = door.Details;
        }

        ShowDoorDetailsPanel();
    }

    private void CloseDoorDetails_Click(object sender, RoutedEventArgs e)
    {
        HideDoorDetailsPanel();
    }

    private void ShowDoorDetailsPanel()
    {
        DoorDetailsPanel.Visibility = Visibility.Visible;

        var animation = new DoubleAnimation
        {
            From = 320,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(260),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        DoorDetailsTransform.BeginAnimation(TranslateTransform.YProperty, animation);
    }

    private void HideDoorDetailsPanel()
    {
        var animation = new DoubleAnimation
        {
            From = DoorDetailsTransform.Y,
            To = 320,
            Duration = TimeSpan.FromMilliseconds(210),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        animation.Completed += (_, _) => DoorDetailsPanel.Visibility = Visibility.Collapsed;
        DoorDetailsTransform.BeginAnimation(TranslateTransform.YProperty, animation);
    }

private void InitializeTerminalSelector()
    {
        TerminalSelector.ItemsSource = Terminals;

        var homeTerminal = _launchContext?.HomeTerminalCode ?? "CON";
        var terminal = DockTerminalCatalog.Find(homeTerminal);

        if (terminal is null || !terminal.HasDockLayout)
        {
            terminal = DockTerminalCatalog.Find("CON") ?? DockTerminalCatalog.All.First(t => t.HasDockLayout);
        }

        _isApplyingTerminal = true;
        TerminalSelector.SelectedItem = terminal;
        _isApplyingTerminal = false;

        ApplyTerminal(terminal, reloadDoors: false);
    }

    private void ApplyTerminal(DockTerminal terminal, bool reloadDoors = true)
    {
        if (!terminal.HasDockLayout)
        {
            MessageBox.Show(
                $"{terminal.DisplayName} does not have a Dock Commander map yet, so it cannot be selected.",
                "No dock map",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            _isApplyingTerminal = true;
            TerminalSelector.SelectedItem = _selectedTerminal;
            _isApplyingTerminal = false;
            return;
        }

        _selectedTerminal = terminal;

        var permission = DockPermissionForTerminal(terminal.Code);
        TerminalPermissionText.Text = $"Permission: {permission.Replace("_", " ").ToUpperInvariant()}";
        TerminalShipCodeText.Text = "Mapped dock layout";

        SidebarTerminalText.Text = terminal.DisplayName;
        Title = $"Dock Commander · {terminal.Code} · {terminal.Name} · {permission}";

        if (reloadDoors)
        {
            LoadDoors();
        }
    }

    private string DockPermissionForTerminal(string terminalCode)
    {
        if (_launchContext is null) return "none";

        var sameHome = string.Equals(_launchContext.HomeTerminalCode, terminalCode, StringComparison.OrdinalIgnoreCase);

        return _launchContext.AccessLevel switch
        {
            1 => sameHome ? "read_write" : "read_only",
            2 => "read_write",
            3 => "read_only",
            4 => "admin",
            _ => "none"
        };
    }

    private void TerminalSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isApplyingTerminal) return;

        if (TerminalSelector.SelectedItem is DockTerminal terminal)
        {
            ApplyTerminal(terminal);
        }
    }

    private void RefreshTerminal_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTerminal is null) return;

        ApplyTerminal(_selectedTerminal);
        MessageBox.Show(
            $"Refreshed terminal {_selectedTerminal.Code}.\n\nCurrent build reloads the mapped XAML door layout. Production will reload terminal layout/state from Postgres.",
            "Terminal Refresh",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private bool CanWriteDock()
    {
        if (_selectedTerminal is null) return false;

        var permission = DockPermissionForTerminal(_selectedTerminal.Code);
        return permission is "read_write" or "admin";
    }

    private bool RequireDockWrite()
    {
        if (CanWriteDock()) return true;

        MessageBox.Show(
            "Your access level is read-only for this terminal. You can view Dock Commander, but cannot move trailers or change doors.",
            "Read-only terminal access",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        return false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
