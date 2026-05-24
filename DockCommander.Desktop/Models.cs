using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace DockCommander.Desktop;

public enum DoorStatus
{
    Available,
    InUse,
    Delayed,
    Offline,
    Dead
}

public sealed class DoorViewModel : INotifyPropertyChanged
{
    private DoorStatus _status;
    private DoorDetailsViewModel? _details;

    public int DoorNumber { get; init; }
    public bool IsDeadDoor { get; init; }
    public bool IsSpacer { get; init; }

    public string DisplayNumber => IsDeadDoor || IsSpacer ? "" : DoorNumber.ToString();
    public Visibility DoorVisibility => IsSpacer ? Visibility.Collapsed : Visibility.Visible;
    public Visibility SpacerVisibility => IsSpacer ? Visibility.Visible : Visibility.Collapsed;

    public DoorStatus Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusBrush));
            OnPropertyChanged(nameof(BorderBrush));
            OnPropertyChanged(nameof(ToolTipText));
        }
    }

    public DoorDetailsViewModel? Details
    {
        get => _details;
        set
        {
            if (_details == value) return;
            _details = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasDetails));
            OnPropertyChanged(nameof(ToolTipText));
        }
    }

    public bool HasDetails => Details is not null;

    public Brush StatusBrush => Status switch
    {
        DoorStatus.Available => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
        DoorStatus.InUse => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
        DoorStatus.Delayed => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
        DoorStatus.Offline => new SolidColorBrush(Color.FromRgb(63, 63, 70)),
        DoorStatus.Dead => new SolidColorBrush(Color.FromRgb(24, 24, 27)),
        _ => new SolidColorBrush(Color.FromRgb(63, 63, 70))
    };

    public Brush BorderBrush => HasDetails
        ? new SolidColorBrush(Color.FromRgb(249, 115, 22))
        : new SolidColorBrush(Color.FromRgb(82, 82, 91));

    public string ToolTipText => IsSpacer
        ? "Open space in dock layout"
        : IsDeadDoor
            ? "Dead door / blocked position"
            : HasDetails
                ? $"Door {DoorNumber}: {Status} - click for trailer details"
                : $"Door {DoorNumber}: {Status}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class DoorDetailsViewModel
{
    public int DoorNumber { get; init; }
    public string Route { get; init; } = "";
    public string Trailer { get; init; } = "";
    public int TotalBills { get; init; }
    public double LoadedPercent { get; init; }
    public ObservableCollection<HazmatPlacardViewModel> HazmatPlacards { get; init; } = new();
    public int WeightPounds { get; init; }

    public string LoadedDisplay => $"{LoadedPercent:0}%";
    public string WeightDisplay => $"{WeightPounds:N0} lbs";
}

public sealed class HazmatPlacardViewModel
{
    public string Name { get; init; } = "";
    public string ClassCode { get; init; } = "";
    public string Symbol { get; init; } = "◆";
    public Brush PlacardBrush { get; init; } = new SolidColorBrush(Color.FromRgb(249, 115, 22));
    public Brush TextBrush { get; init; } = Brushes.Black;
}

public sealed class RackViewModel
{
    public string Name { get; init; } = "";
    public ObservableCollection<RackPairViewModel> Pairs { get; init; } = new();
}

public sealed class RackPairViewModel
{
    public RackSlotViewModel BottomSlot { get; init; } = new();
    public RackSlotViewModel TopSlot { get; init; } = new();

    public string PairLabel => $"{BottomSlot.SlotNumber} | {TopSlot.SlotNumber}";
}

public sealed class RackSlotViewModel
{
    public int SlotNumber { get; init; }
    public DoorStatus Status { get; init; } = DoorStatus.Offline;

    public Brush StatusBrush => Status switch
    {
        DoorStatus.Available => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
        DoorStatus.InUse => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
        DoorStatus.Delayed => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
        _ => new SolidColorBrush(Color.FromRgb(39, 39, 42))
    };
}

public sealed class ActivityViewModel
{
    public string Door { get; init; } = "";
    public string Message { get; init; } = "";
    public string Time { get; init; } = "";
    public Brush AccentBrush { get; init; } = Brushes.RoyalBlue;
}

public sealed class AppointmentViewModel
{
    public string Time { get; init; } = "";
    public string Carrier { get; init; } = "";
    public string Door { get; init; } = "";
    public string Type { get; init; } = "";
}
