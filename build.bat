@echo off
setlocal

set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%"

dotnet clean "src\src.sln" -c Release -r win-x64
dotnet clean "src\Installer\XhairInstaller.csproj" -c Release -r win-x64
if exist "src\Installer\obj" rd /s /q "src\Installer\obj"
if exist "src\Installer\bin" rd /s /q "src\Installer\bin"
dotnet publish "src\Xhair.csproj" -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "publish\app"
dotnet publish "src\Installer\XhairInstaller.csproj" -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o "publish\installer"

endlocal
