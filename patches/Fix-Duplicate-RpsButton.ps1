param(
    [string]$RepoRoot = "C:\Users\Dan\Documents\GitHub\PyleCentury"
)

$ErrorActionPreference = "Stop"

$file = Join-Path $RepoRoot "PyleCentury.Menu\MainWindow.xaml"

if (!(Test-Path $file)) {
    throw "Could not find $file"
}

Copy-Item $file "$file.bak-rpsbutton" -Force

$text = Get-Content $file -Raw
$target = 'x:Name="RpsButton"'

$matches = [regex]::Matches($text, [regex]::Escape($target))

if ($matches.Count -le 1) {
    Write-Host "No duplicate RpsButton names found." -ForegroundColor Yellow
} else {
    $result = New-Object System.Text.StringBuilder
    $last = 0
    $count = 0

    foreach ($m in $matches) {
        [void]$result.Append($text.Substring($last, $m.Index - $last))
        $count++

        if ($count -eq 1) {
            [void]$result.Append($target)
        } else {
            [void]$result.Append("x:Name=`"RpsButtonDuplicate$count`"")
        }

        $last = $m.Index + $m.Length
    }

    [void]$result.Append($text.Substring($last))
    Set-Content $file $result.ToString() -Encoding UTF8

    Write-Host "Fixed duplicate RpsButton names. Kept first as RpsButton." -ForegroundColor Green
}

dotnet build (Join-Path $RepoRoot "PyleCentury.Menu\PyleCentury.Menu.csproj")
