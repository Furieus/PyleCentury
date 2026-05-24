param(
    [string]$InstallRoot = "C:\PyleCentury"
)

$ErrorActionPreference = "Stop"

$AppsRoot = Join-Path $InstallRoot "apps"
$ConfigRoot = Join-Path $InstallRoot "config"
$MenuTemp = Join-Path $InstallRoot "_menu_publish"

Write-Host ""
Write-Host "Publishing Pyle Century Suite" -ForegroundColor Cyan
Write-Host "Install root: $InstallRoot"
Write-Host "Apps root:    $AppsRoot"
Write-Host ""

New-Item -ItemType Directory -Force -Path $InstallRoot, $AppsRoot, $ConfigRoot | Out-Null

if (Test-Path $MenuTemp) { Remove-Item $MenuTemp -Recurse -Force }
if (Test-Path (Join-Path $AppsRoot "DockCommander")) { Remove-Item (Join-Path $AppsRoot "DockCommander") -Recurse -Force }
if (Test-Path (Join-Path $AppsRoot "Billing")) { Remove-Item (Join-Path $AppsRoot "Billing") -Recurse -Force }
if (Test-Path (Join-Path $AppsRoot "Admin")) { Remove-Item (Join-Path $AppsRoot "Admin") -Recurse -Force }

Write-Host "Publishing Menu..." -ForegroundColor Yellow
dotnet publish ".\PyleCentury.Menu\PyleCentury.Menu.csproj" -c Release -r win-x64 --self-contained false -o $MenuTemp

Copy-Item "$MenuTemp\*" $InstallRoot -Recurse -Force

$menuExe = Join-Path $InstallRoot "PyleCentury.Menu.exe"
$pyleMenuExe = Join-Path $InstallRoot "PyleMenu.exe"
if (Test-Path $menuExe) {
    Copy-Item $menuExe $pyleMenuExe -Force
}

Write-Host "Publishing Dock Commander..." -ForegroundColor Yellow
dotnet publish ".\DockCommander.Desktop\DockCommander.Desktop.csproj" -c Release -r win-x64 --self-contained false -o (Join-Path $AppsRoot "DockCommander")

Write-Host "Publishing Billing..." -ForegroundColor Yellow
dotnet publish ".\PyleCentury.Billing.Desktop\PyleCentury.Billing.Desktop.csproj" -c Release -r win-x64 --self-contained false -o (Join-Path $AppsRoot "Billing")

Write-Host "Publishing Employee Manager..." -ForegroundColor Yellow
dotnet publish ".\PyleCentury.Admin.Desktop\PyleCentury.Admin.Desktop.csproj" -c Release -r win-x64 --self-contained false -o (Join-Path $AppsRoot "Admin")

$config = @{
    InstallRoot = $InstallRoot
    AppsRoot = $AppsRoot
    ApiBaseUrl = "https://PyleCentury.onrender.com"
}

$config | ConvertTo-Json -Depth 3 | Set-Content -Path (Join-Path $ConfigRoot "install.json") -Encoding UTF8

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "Main menu: $pyleMenuExe"
Write-Host "Apps:      $AppsRoot"
