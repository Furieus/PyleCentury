@echo off
setlocal

echo ========================================
echo Pyle Century Suite Release Publisher
echo ========================================
echo.

echo Publishing Dock Commander...
pushd DockCommander.Desktop
call publish-windows-release.bat
popd

echo.
echo Publishing Billing...
pushd PyleCentury.Billing.Desktop
call publish-windows-release.bat
popd

echo.
echo Publishing Employee Manager...
pushd PyleCentury.Admin.Desktop
call publish-windows-release.bat
popd

echo.
echo Publishing Pyle Menu...
pushd PyleCentury.Menu
call publish-windows-release.bat
popd

echo.
echo ========================================
echo All releases attempted.
echo.
echo Start the suite from:
echo PyleCentury.Menu\release\win-x64\PyleCentury.Menu.exe
echo.
echo The menu will look for apps here:
echo DockCommander.Desktop\release\win-x64\DockCommander.exe
echo PyleCentury.Billing.Desktop\release\win-x64\PyleCentury.Billing.exe
echo PyleCentury.Admin.Desktop\release\win-x64\PyleCentury.Admin.exe
echo ========================================
echo.
pause
