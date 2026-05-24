param(
    [string]$RepoRoot = "C:\Users\Dan\Documents\GitHub\PyleCentury"
)

$ErrorActionPreference = "Stop"

$file = Join-Path $RepoRoot "PyleCentury.RPS.Desktop\MainWindow.xaml.cs"
if (!(Test-Path $file)) { throw "Could not find $file" }

Copy-Item $file "$file.bak-bucketclick" -Force

$text = Get-Content $file -Raw

if ($text -notmatch "using System\.Windows\.Controls;") {
    $text = $text.Replace("using System.Windows;", "using System.Windows;`r`nusing System.Windows.Controls;")
}

$pattern = 'private async void BucketDropdownList_SelectionChanged\(object sender, SelectionChangedEventArgs e\)\s*\{[\s\S]*?\r?\n    \}\r?\n\r?\n    private async Task LoadBucketsAsync'
$replacement = @'
private async void BucketDropdownList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingBuckets || _loadingLock) return;

        RpsBucketDisplay? item = null;

        if (BucketDropdownList.SelectedItem is RpsBucketDisplay directItem)
        {
            item = directItem;
        }
        else if (BucketDropdownList.SelectedItem is ListBoxItem listBoxItem &&
                 listBoxItem.Tag is RpsBucketDisplay tagItem)
        {
            item = tagItem;
        }

        if (item is null)
        {
            StatusText.Text = "Bucket selection could not be read.";
            return;
        }

        BucketPopup.IsOpen = false;
        _selectedBucket = item.Bucket;
        BucketButtonText.Text = item.DisplayText;

        await AcquireOrViewLockAsync(item.Bucket);
    }

    private async Task LoadBucketsAsync
'@

$text = [regex]::Replace($text, $pattern, $replacement, 1)
Set-Content $file $text -Encoding UTF8

dotnet build (Join-Path $RepoRoot "PyleCentury.RPS.Desktop\PyleCentury.RPS.Desktop.csproj")
