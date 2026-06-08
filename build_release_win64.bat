@echo off
REM Centauri Carbon Downloader - Release Build Script for Windows x64
REM Created by ashemka

echo.
echo ========================================
echo Centauri Carbon Downloader Build Script
echo ========================================
echo.

REM Check if dotnet is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found!
    echo.
    echo Please install .NET 8.0 SDK or later from:
    echo https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo [1/4] Cleaning previous builds...
if exist bin rmdir /s /q bin >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1
echo OK

echo.
echo [2/4] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Package restore failed
    pause
    exit /b 1
)
echo OK

echo.
echo [3/4] Building Release configuration...
dotnet build -c Release --self-contained -r win-x64
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b 1
)
echo OK

echo.
echo [4/4] Build complete!
echo.
echo ========================================
echo Output executable:
echo bin\Release\net8.0-windows\win-x64\Centauri Carbon Downloader.exe
echo ========================================
echo.

REM Check if the executable was created
if exist "bin\Release\net8.0-windows\win-x64\Centauri Carbon Downloader.exe" (
    echo.
    echo SUCCESS: Build completed successfully!
    echo You can now run the application from the output folder.
    echo.
    pause
) else (
    echo.
    echo ERROR: Build completed but executable not found
    echo.
    pause
    exit /b 1
)
setlocal
cd /d "%~dp0"

echo ======================================================
echo  Centauri Carbon Downloader v0.5.4 - build Windows x64
echo  Cree par ashemka
echo ======================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERREUR : le SDK .NET 8 n'est pas installe.
    echo Telechargement : https://dotnet.microsoft.com/download/dotnet/8.0
    echo Installer le SDK, puis relancer ce fichier.
    echo.
    pause
    exit /b 1
)

echo Nettoyage...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

if not exist "%~dp0tools\ffmpeg.exe" (
    echo.
    echo FFmpeg absent : tentative de recuperation automatique...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\get_ffmpeg_win64.ps1"
)

if exist "%~dp0tools\ffmpeg.exe" (
    echo FFmpeg trouve : il sera integre directement dans l'EXE final.
) else (
    echo.
    echo ATTENTION : tools\ffmpeg.exe introuvable.
    echo L'EXE sera compile, mais le mode rapide PC demandera FFmpeg.
)

echo Compilation de l'exe portable...
dotnet publish "CentauriCarbonDownloader.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:DebugType=None ^
  /p:DebugSymbols=false

if errorlevel 1 (
    echo.
    echo ECHEC DE LA COMPILATION.
    pause
    exit /b 1
)

set PUBLISH_DIR=%~dp0bin\Release\net8.0-windows\win-x64\publish

if exist "%~dp0FFMPEG_NOTICE.txt" (
    copy /Y "%~dp0FFMPEG_NOTICE.txt" "%PUBLISH_DIR%\FFMPEG_NOTICE.txt" >nul
)

echo.
echo ======================================================
echo  Termine.
echo  Dossier final :
echo  %PUBLISH_DIR%
echo ======================================================
echo.
explorer "%PUBLISH_DIR%"
pause
