@echo off
setlocal enabledelayedexpansion

REM ============================================================
REM Pyle Century Custom Publish
REM ============================================================
REM Default output:
REM   C:\PyleCentury\PyleMenu.exe
REM   C:\PyleCentury\apps\DockCommander\DockCommander.exe
REM   C:\PyleCentury\apps\Billing\PyleCentury.Billing.exe
REM
REM Optional:
REM   publish-to-c-pylecentury.bat "D:\SomeOtherFolder"
REM ============================================================

set "INSTALL_ROOT=%~1"
if "%INSTALL_ROOT%"=="" set "INSTALL_ROOT=C:\PyleCentury"

set "APPS_ROOT=%INSTALL_ROOT%\apps"
set "CONFIG_ROOT=%INSTALL_ROOT%\config"

echo.
echo Publishing Pyle Century Suite
echo Install root: %INSTALL_ROOT%
echo Apps root:    %APPS_ROOT%
echo.

if not exist "%INSTALL_ROOT%" mkdir "%INSTALL_ROOT%"
if not exist "%APPS_ROOT%" mkdir "%APPS_ROOT%"
if not exist "%CONFIG_ROOT%" mkdir "%CONFIG_ROOT%"

echo Cleaning old publish output...
if exist "%INSTALL_ROOT%\_menu_publish" rmdir /s /q "%INSTALL_ROOT%\_menu_publish"
if exist "%APPS_ROOT%\DockCommander" rmdir /s /q "%APPS_ROOT%\DockCommander"
if exist "%APPS_ROOT%\Billing" rmdir /s /q "%APPS_ROOT%\Billing"
if exist "%APPS_ROOT%\Admin" rmdir /s /q "%APPS_ROOT%\Admin"
if exist "%APPS_ROOT%\RPS" rmdir /s /q "%APPS_ROOT%\RPS"

echo.
echo Publishing Menu...
dotnet publish ".\PyleCentury.Menu\PyleCentury.Menu.csproj" -c Release -r win-x64 --self-contained false -o "%INSTALL_ROOT%\_menu_publish"
if errorlevel 1 goto failed

echo Copying Menu files to root...
xcopy "%INSTALL_ROOT%\_menu_publish\*" "%INSTALL_ROOT%\" /E /Y /I >nul

REM Rename menu exe to PyleMenu.exe for a clean launcher entry.
if exist "%INSTALL_ROOT%\PyleCentury.Menu.exe" (
    copy /Y "%INSTALL_ROOT%\PyleCentury.Menu.exe" "%INSTALL_ROOT%\PyleMenu.exe" >nul
)

echo.
echo Publishing Dock Commander...
dotnet publish ".\DockCommander.Desktop\DockCommander.Desktop.csproj" -c Release -r win-x64 --self-contained false -o "%APPS_ROOT%\DockCommander"
if errorlevel 1 goto failed

echo.
echo Publishing Billing...
dotnet publish ".\PyleCentury.Billing.Desktop\PyleCentury.Billing.Desktop.csproj" -c Release -r win-x64 --self-contained false -o "%APPS_ROOT%\Billing"
if errorlevel 1 goto failed

echo.
echo Publishing Employee Manager...
dotnet publish ".\PyleCentury.Admin.Desktop\PyleCentury.Admin.Desktop.csproj" -c Release -r win-x64 --self-contained false -o "%APPS_ROOT%\Admin"
if errorlevel 1 goto failed

echo.
echo Publishing RPS...
dotnet publish ".\PyleCentury.RPS.Desktop\PyleCentury.RPS.Desktop.csproj" -c Release -r win-x64 --self-contained false -o "%APPS_ROOT%\RPS"
if errorlevel 1 goto failed

echo.
echo Writing install config...
(
echo {
echo   "InstallRoot": "%INSTALL_ROOT:\=\\%",
echo   "AppsRoot": "%APPS_ROOT:\=\\%",
echo   "ApiBaseUrl": "https://PyleCentury.onrender.com"
echo }
) > "%CONFIG_ROOT%\install.json"

echo.
echo Done.
echo Main menu:
echo   %INSTALL_ROOT%\PyleMenu.exe
echo.
echo Child apps:
echo   %APPS_ROOT%\DockCommander
echo   %APPS_ROOT%\Billing
echo   %APPS_ROOT%\Admin
echo.
pause
exit /b 0

:failed
echo.
echo Publish failed. Check errors above.
pause
exit /b 1
