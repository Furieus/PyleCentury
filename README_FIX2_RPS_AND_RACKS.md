# Fix 2: RPS LaunchContext + Rack Centering

## RPS build fix

The shared `LaunchContext` record uses:

```text
EmployeeNumber
HomeTerminalCode
```

The first RPS desktop shell accidentally referenced:

```text
EmployeeId
TerminalCode
```

This patch corrects those references.

## Dock Commander rack centering

This patch attempts to nudge the rack/door field down and center it by applying:

```text
Margin="0,32,0,0"
HorizontalAlignment="Center"
VerticalAlignment="Center"
```

to the first matching dock/rack layout host found in `DockCommander.Desktop/MainWindow.xaml`.

If your local XAML uses a different element name, the patch script leaves a backup so the rack host can be adjusted manually.
