# Centauri Carbon Downloader v0.5.4

**Created by ashemka.**

Portable Windows application for the Elegoo Centauri Carbon. It can list local printer files, download or delete G-code files, and create timelapse videos on the PC from the image frames stored in `/local/aic_tlp/`.

> French documentation is available in `README_FR.md`.

## 🚀 Getting Started

This is the **source code repository**. To use the application:

### Quick Start (Build from Source)

**Requirements:**
- Windows 10/11 (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later

**Build:**
```bash
# Option 1: Windows batch script
build_release_win64.bat

# Option 2: .NET CLI
dotnet build -c Release --self-contained -r win-x64
```

**Output:** `bin/Release/net8.0-windows/win-x64/Centauri Carbon Downloader.exe`

**Detailed build instructions:** See [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md)

### What happens on first run?

1. Select an export folder (or use default)
2. FFmpeg is automatically downloaded if needed
3. Enter your printer's IP address
4. Connect and start managing your printer files!

## Goal

The goal is to keep the end-user workflow as simple as possible:

1. launch the application;
2. enter the printer IP address;
3. click **Connect**;
4. select G-code files or timelapses;
5. click **Download selected** or **Create videos on PC**.

No PowerShell, no CMD, no browser tabs showing raw G-code, and no manual export from the slicer.

## Main features

- direct WebSocket connection to the printer;
- local file listing through `Cmd 258`;
- deletion of selected files through `Cmd 259`;
- sequential G-code download to the selected export folder under `GCode`;
- fast scan of the `/local/aic_tlp/` root folder;
- detection of extensionless timelapse frames, including `tlp_layer_*`;
- local MP4 generation with FFmpeg;
- parallel download of source images;
- fallback **Printer export** mode through `Cmd 323`;
- log files in the selected export folder;
- light and dark theme;
- multilingual interface: French, English, Italian, Spanish, German, Japanese, Chinese, Korean.

## What's new in v0.5.4

- Reworked dark mode colors for tabs, grids, buttons, inputs, and disabled controls.
- Added a **Choose folder** button to select the export directory manually.
- The selected export folder is persisted in `%LOCALAPPDATA%\CentauriCarbonDownloader\settings.json`.
- The UI now displays the active export folder.

## What's new in v0.5.2

- Build fix for the localized UI selection toggle.
- Version metadata updated to 0.5.2.
- Project SDK declaration adjusted for cleaner builds with recent .NET SDK versions.

## What's new in v0.5.1

- English `README.md` for GitHub;
- French documentation moved to `README_FR.md`;
- added `README_EN.md` as an explicit English copy;
- kept the original French text in `README_FR.txt` for compatibility with the previous package.

## What's new in v0.5.0

- added **Created by ashemka** branding in the UI, code, project metadata, and documentation;
- added language selector;
- added dark mode;
- adjusted the interface to better support longer labels;
- UI settings are persisted in `%LOCALAPPDATA%\CentauriCarbonDownloader\settings.json`;
- WebSocket handshake timeout set to 8 seconds;
- short WebSocket response timeout set to 6 seconds;
- HTTP probe timeout set to 15 seconds;
- improved GitHub documentation.

## Timelapses: how it works

The Centauri Carbon often exposes timelapse image frames in `/local/aic_tlp/`, but not necessarily a ready-to-download MP4 file. The application therefore provides two modes.

### Create videos on PC

Recommended mode. The application downloads the source frames and creates the MP4 locally with FFmpeg. This is usually faster than waiting for the printer to export the video.

### Printer export

Fallback mode. The application asks the printer to generate the MP4, then downloads it when it becomes available. This mode can be much slower.

## Settings

- **Export folder**: can be changed from the UI with **Choose folder**. G-code files are stored in `GCode`, timelapses in `Timelapses`.
- **FPS**: frames per second for the final video. Recommended value: `30`.
- **Streams**: number of parallel image downloads. Recommended value: `6`.
- **Keep images**: keeps the source frames after encoding, useful for diagnostics.

## FFmpeg

The fast PC mode requires FFmpeg. The `build_release_win64.bat` script tries to download `ffmpeg.exe` automatically into `tools\ffmpeg.exe`.

If `tools\ffmpeg.exe` is present at build time, it is embedded into the final EXE. At runtime, the application extracts it to:

```txt
%LOCALAPPDATA%\CentauriCarbonDownloader\tools\ffmpeg.exe
```

The end user normally does not need to do anything.

## Windows build

Requirement: .NET 8 SDK for Windows.

1. Download or clone the project.
2. Double-click `build_release_win64.bat`.
3. Wait for the build to complete.
4. Get the application from:

```txt
bin\Release\net8.0-windows\win-x64\publish\
```

Recommended files to distribute:

```txt
Centauri Carbon Downloader.exe
FFMPEG_NOTICE.txt
```

## Network notes and timeouts

The application does not perform an account login. It opens a local WebSocket connection to:

```txt
ws://PRINTER_IP/websocket
```

Timeouts defined in v0.5.0:

| Step | Duration |
|---|---:|
| WebSocket handshake | 8 s |
| Short WebSocket response | 6 s |
| HTTP folder/file probe | 15 s |
| Long transfer for G-code / images / video | 45 min |
| Printer timelapse export | 10 min max |

These values avoid leaving the UI stuck for too long while still allowing large files to complete.

## Troubleshooting

### The timelapse scan finds MP4 files but not source images

Check the folder in a browser:

```txt
http://PRINTER_IP/local/aic_tlp/
```

Some firmware versions expose images as extensionless files such as `tlp_layer_1`, `tlp_layer_2`, and so on. v0.5.0 and later treat them as video frames.

### FFmpeg not found

Run `build_release_win64.bat` again. The script tries to download FFmpeg automatically. If the network blocks the download, manually place `ffmpeg.exe` here:

```txt
tools\ffmpeg.exe
```

then rebuild.

### Connection failed

- make sure the PC and printer are on the same local network;
- check the printer IP address;
- test browser access to `http://PRINTER_IP/`;
- restart the application.

## Credits

Created by ashemka.

Development assisted by Hex Kernel / ChatGPT.

FFmpeg is subject to its own licenses and notices. See `FFMPEG_NOTICE.txt`.
