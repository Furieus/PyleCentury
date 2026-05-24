@echo off
setlocal

echo Publishing Pyle Century Menu release...
echo.

dotnet publish PyleCentury.Menu.csproj ^
  -c Release ^
  -f net10.0-windows ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:DebugType=None ^
  -p:DebugSymbols=false ^
  -o ".\release\win-x64"

echo.
if exist ".\release\win-x64\PyleCentury.Menu.exe" del /q ".\release\win-x64\PyleCentury.Menu.exe"

echo Release output:
echo %cd%\release\win-x64
echo.
pause
