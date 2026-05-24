# Dock Commander Desktop

C# WPF UI/UX mockup for a modern dock operations dashboard.

## Requirements
- Windows
- .NET 8 SDK or newer
- Visual Studio Community recommended

## Run
```bash
cd DockCommander.Desktop
dotnet run
```

## Build
```bash
dotnet build
```

This is a frontend-only UI shell with sample data.

## Splash startup screen

This version starts with `SplashWindow.xaml`.

Splash features:
- dark grey minimal/technical background
- orange/black "Pyle Century" branding
- orange lightning bolt between Pyle and Century
- tagline: "Built on Legacy, Designed for the future."
- black outlined loading bar with orange fill
- Continue button appears after loading completes
- bottom text: "over 100 years of Pyle delivering service!"

Startup flow:

```text
App.xaml -> SplashWindow.xaml -> Continue button -> MainWindow.xaml
```

## Functional dummy data update

This build includes a clickable dummy data panel for Door 5.

Door 5 data:
- Route: New London Wave
- Trailer: 5851L
- Total Bills: 15
- Loaded: 5%
- Hazmat: Flammable, Corrosive
- Weight: 989 lbs

Dashboard layout update:
- Top/North dock doors are displayed 60 through 32.
- Bottom/South dock doors are displayed 1 through 30, plus Door 31 as a dead dumpster door with no number.
- Rack layout is A through S.
- Each rack has 10 paired rows: 1|2, 3|4, 5|6, etc. through 19|20.
- A larger gap appears between Rack I and Rack J for the crossover/dock desk area.
- Appointments were removed from the main dashboard.

## Debug-friendly startup update

This version launches the splash screen manually from `App.xaml.cs` and shows any startup/runtime exceptions in a MessageBox instead of silently closing.

Run:

```bash
dotnet run
```

If the app does not appear, also try:

```bash
dotnet build
bin\Debug\net8.0-windows\DockCommander.Desktop.exe
```

## Hazmat placard update

Door 5 no longer shows hazmat as simple text only. It now renders placard-style shipment icons:
- FLAMMABLE / Class 3
- CORROSIVE / Class 8

These are dummy UI placards for concept/demo use.
