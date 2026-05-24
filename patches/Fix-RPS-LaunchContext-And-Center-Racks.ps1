param(
    [string]$RepoRoot = "C:\Users\Dan\Documents\GitHub\PyleCentury"
)

$ErrorActionPreference = "Stop"

$rpsFile = Join-Path $RepoRoot "PyleCentury.RPS.Desktop\MainWindow.xaml.cs"
$menuFile = Join-Path $RepoRoot "PyleCentury.Menu\MainWindow.xaml.cs"
$dockXaml = Join-Path $RepoRoot "DockCommander.Desktop\MainWindow.xaml"

if (Test-Path $rpsFile) {
    Copy-Item $rpsFile "$rpsFile.bak" -Force
    $t = Get-Content $rpsFile -Raw
    $t = $t.Replace("_launchContext.EmployeeId", "_launchContext.EmployeeNumber")
    $t = $t.Replace("_launchContext.TerminalCode", "_launchContext.HomeTerminalCode")
    Set-Content $rpsFile $t -Encoding UTF8
    Write-Host "Fixed RPS LaunchContext property names." -ForegroundColor Green
}

if (Test-Path $menuFile) {
    Copy-Item $menuFile "$menuFile.bak2" -Force
    $t = Get-Content $menuFile -Raw
    $t = $t.Replace("\'", "'")
    Set-Content $menuFile $t -Encoding UTF8
    Write-Host "Cleaned menu bad escaped single quotes." -ForegroundColor Green
}

if (Test-Path $dockXaml) {
    Copy-Item $dockXaml "$dockXaml.bak" -Force
    $t = Get-Content $dockXaml -Raw

    $names = @("DockLayoutHost","DoorCanvas","DockCanvas","RackCanvas","RackGrid","DoorsGrid")
    foreach ($n in $names) {
        $plain = "x:Name=`"$n`""
        $patched = "x:Name=`"$n`" Margin=`"0,32,0,0`" HorizontalAlignment=`"Center`" VerticalAlignment=`"Center`""
        if ($t.Contains($plain) -and !$t.Contains($patched)) {
            $t = $t.Replace($plain, $patched)
            Write-Host "Centered rack host: $n" -ForegroundColor Green
            break
        }
    }

    Set-Content $dockXaml $t -Encoding UTF8
}

Write-Host ""
Write-Host "Testing RPS build..." -ForegroundColor Yellow
dotnet build (Join-Path $RepoRoot "PyleCentury.RPS.Desktop\PyleCentury.RPS.Desktop.csproj")
