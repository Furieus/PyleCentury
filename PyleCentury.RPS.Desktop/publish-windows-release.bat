@echo off
setlocal

echo Publishing Pyle Century RPS release...

if exist release\win-x64 rmdir /s /q release\win-x64
mkdir release\win-x64

dotnet publish PyleCentury.RPS.Desktop.csproj -c Release -r win-x64 --self-contained false -o release\win-x64

if errorlevel 1 (
    echo.
    echo Build failed.
    pause
    exit /b 1
)

echo.
echo Release output:
echo %cd%\release\win-x64
echo.
pause
