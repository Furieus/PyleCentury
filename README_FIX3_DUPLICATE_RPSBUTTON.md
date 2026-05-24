# Fix 3: Duplicate RpsButton

WPF error fixed:

```text
MainWindow already contains a definition for RpsButton
```

Cause:

```text
PyleCentury.Menu/MainWindow.xaml had more than one x:Name="RpsButton".
```

Fix:

```text
The first Open RPS button remains x:Name="RpsButton".
Any duplicate named RPS buttons are renamed to RpsButtonDuplicate2, etc.
```

Local patch script:

```powershell
.\patches\Fix-Duplicate-RpsButton.ps1 -RepoRoot "C:\Users\Dan\Documents\GitHub\PyleCentury"
```
