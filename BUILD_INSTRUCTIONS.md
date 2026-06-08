# Building Centauri Carbon Downloader from Source

## Prerequisites

1. **Windows 10/11** (x64)
2. **.NET 8.0 SDK** or later
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

3. **FFmpeg** (optional - downloaded automatically on first run)
   - Or manually place `ffmpeg.exe` in the `tools/` folder

## Build Methods

### Option 1: Quick Build (Recommended)

**Windows:**
```batch
build_release_win64.bat
```

This script will:
- Clean previous builds
- Restore NuGet packages
- Build the Release configuration for win-x64
- Output: `bin/Release/net8.0-windows/win-x64/Centauri Carbon Downloader.exe`

### Option 2: Manual Build with .NET CLI

```powershell
# Restore dependencies
dotnet restore

# Build release version
dotnet build -c Release --self-contained -r win-x64

# The executable will be at:
# bin/Release/net8.0-windows/win-x64/Centauri Carbon Downloader.exe
```

### Option 3: Debug Build (for developers)

```powershell
dotnet build -c Debug
# Executable: bin/Debug/net8.0-windows/Centauri Carbon Downloader.exe
```

## Post-Build

After building:

1. **First Run Setup:**
   - The app will automatically create: `%LOCALAPPDATA%\CentauriCarbonDownloader\`
   - FFmpeg will be downloaded automatically if not found
   - Settings saved to: `settings.json` in that folder

2. **Manual FFmpeg Setup** (optional):
   - Download from: https://ffmpeg.org/download.html
   - Extract `ffmpeg.exe`
   - Place in: `tools/ffmpeg.exe` before building

## Troubleshooting

**Error: "dotnet: The term 'dotnet' is not recognized"**
- Install .NET SDK: https://dotnet.microsoft.com/download
- Restart your terminal/IDE
- Verify: `dotnet --version`

**Error: "No required supplemental .NET Runtime version"**
- Update .NET: `dotnet sdk update`

**FFmpeg download fails**
- Manually download from: https://ffmpeg.org/download.html
- Place `ffmpeg.exe` in `tools/` folder

## Project Info

- **Language:** C# (.NET 8.0)
- **Framework:** WinForms
- **Target:** net8.0-windows, win-x64
- **Version:** 0.5.4

## Additional Resources

- Full documentation: [README.md](README.md)
- French documentation: [README_FR.md](README_FR.md)
- License: Check LICENSE file (if applicable)

## Development

For developers wanting to modify the source:

```powershell
# Open in Visual Studio or VS Code
code .

# Or with Visual Studio IDE
start CentauriCarbonDownloader.csproj
```

---

**Created by ashemka** - Report issues on GitHub
