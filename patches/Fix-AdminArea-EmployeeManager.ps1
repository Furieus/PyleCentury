param(
    [string]$RepoRoot = "C:\Users\Dan\Documents\GitHub\PyleCentury"
)

$ErrorActionPreference = "Stop"

Write-Host "This patch is easiest to apply by replacing the repo with the ZIP version." -ForegroundColor Yellow
Write-Host "Manual quick fix:" -ForegroundColor Yellow
Write-Host "1. In PyleCentury.Menu\MainWindow.xaml remove the RPS card inside AdminFront." -ForegroundColor Cyan
Write-Host "2. Keep only Employee Manager, Terminal Admin, and Route Grid Admin inside AdminFront." -ForegroundColor Cyan
Write-Host "3. Rebuild PyleCentury.Menu." -ForegroundColor Cyan

dotnet build (Join-Path $RepoRoot "PyleCentury.Menu\PyleCentury.Menu.csproj")
