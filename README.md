# Centauri Carbon Downloader

[![Version](https://img.shields.io/badge/version-0.5.4-blue.svg)](CHANGELOG.md)
[![License](https://img.shields.io/badge/license-Non--Commercial--Attribution-red.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%2B-512bd4.svg)](https://dotnet.microsoft.com)
[![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-0078d4.svg)](#)

**Created by ashemka** - A portable Windows application for managing Elegoo Centauri Carbon 3D printer files.

> 🇫🇷 [Documentation en français](README_FR.md) | 🇬🇧 [Full English version](README_EN.md)

## ✨ Quick Features

- 📋 List and manage printer files via WebSocket
- ⬇️ Download G-code files to your PC
- 🗑️ Delete files directly from printer
- 🎬 Create MP4 timelapses locally with FFmpeg
- 🌙 Light & Dark themes
- 🌍 8 languages supported
- ⚙️ Portable - no installation required

## 🚀 Getting Started

This is the **source code repository**. To use the application:

### Quick Build

**Requirements:**
- Windows 10/11 (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later

**Build:**
```bash
# Windows batch script (recommended)
build_release_win64.bat

# Or manual build
dotnet build -c Release --self-contained -r win-x64
```

**Output:** `bin/Release/net8.0-windows/win-x64/Centauri Carbon Downloader.exe`

📖 **Detailed instructions:** [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md) | [BUILD_INSTRUCTIONS_FR.md](BUILD_INSTRUCTIONS_FR.md)

### First Launch

1. Select export folder (or use default)
2. FFmpeg auto-downloads if needed
3. Enter printer IP address
4. Click Connect → Manage files! 

## 🎯 Design Goal

Keep the end-user workflow **simple and intuitive**:

1. Launch the app
2. Enter printer IP
3. Click **Connect**
4. Select files or timelapses
5. Click **Download** or **Create videos**

No PowerShell. No CMD. No raw G-code. No manual slicing exports.

## 🎨 Main Features

| Feature | Details |
|---------|---------|
| **File Management** | List, download, delete local printer files |
| **Timelapse Videos** | Create MP4 videos from image frames locally |
| **Smart Connection** | WebSocket + fallback HTTP export modes |
| **Themes** | Light and dark mode support |
| **Settings Persistence** | Remember folder location & UI preferences |
| **Logging** | Detailed logs saved to export folder |
| **Fast Performance** | Parallel downloads, optimized file scanning |

## 🌐 Supported Languages

| Language | Code | Status |
|----------|------|--------|
| English | en | ✅ Supported |
| Français | fr | ✅ Supported |
| Italiano | it | ✅ Supported |
| Español | es | ✅ Supported |
| Deutsch | de | ✅ Supported |
| 日本語 | ja | ✅ Supported |
| 中文 (简体) | zh-Hans | ✅ Supported |
| 中文 (繁體) | zh-Hant | ✅ Supported |

## ❓ Troubleshooting & FAQ

### "dotnet is not recognized"
- Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Restart your terminal
- Verify: `dotnet --version`

### "FFmpeg download fails"
- Download manually: https://ffmpeg.org/download.html
- Place `ffmpeg.exe` in `tools/` folder
- Restart the application

### "Cannot connect to printer"
- Verify printer IP address
- Ensure printer and PC are on same network
- Check printer's WebSocket port (usually 80)
- Check firewall settings

### "Video generation is slow"
- This is normal for large image counts
- FFmpeg processes frames sequentially
- Close other applications to free memory

### "Build fails with SDK error"
- Update .NET SDK: `dotnet sdk update`
- Or install latest: https://dotnet.microsoft.com/download

📖 **More help:** Check [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md) or open a [GitHub Issue](../../issues)

## 🤝 Support & Contributing

### Get Help
- 🐛 **Report bugs**: [Open an issue](../../issues)
- 💡 **Suggest features**: [GitHub Discussions](../../discussions)
- ❓ **Ask questions**: [GitHub Discussions](../../discussions)

### Want to Contribute?
We welcome contributions! Please read [CONTRIBUTING.md](CONTRIBUTING.md) first.

Quick start:
1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Commit changes: `git commit -m "Add my feature"`
4. Push: `git push origin feature/my-feature`
5. Open a Pull Request

## 📄 Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history and updates.

## 📋 Version History

- **v0.5.4** - Dark mode improvements, folder selection
- **v0.5.2** - Build fixes, SDK compatibility
- **v0.5.1** - English README & translations
- **v0.5.0** - Initial release with multi-language support

## ⚖️ License

This project is licensed under **Non-Commercial Attribution License**.

**You can:**
- Use for personal/educational purposes
- Modify the code
- Distribute non-commercial versions

**You must:**
- Cite **ashemka** as original author
- Include license with distributions
- Not use commercially without permission

See [LICENSE](LICENSE) for full details.

---

## 📞 Contact

**Created by ashemka**
- 📧 Report issues: [GitHub Issues](../../issues)
- 💬 Discuss: [GitHub Discussions](../../discussions)
- 🔗 Repository: https://github.com/Ashemka/Centauri-Carbon-File-Manager-by-Ashemka

## Detailed Documentation

### For Users
- [Getting Started Guide](BUILD_INSTRUCTIONS.md)
- [French Guide](BUILD_INSTRUCTIONS_FR.md)
- [Changelog](CHANGELOG.md)

### For Developers
- [Contributing Guidelines](CONTRIBUTING.md)
- [License](LICENSE)
- [Code Style (.editorconfig)](.editorconfig)

---

*Centauri Carbon Downloader - Making 3D printing simpler, one file at a time.* 🖨️

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
