# Changelog

All notable changes to Centauri Carbon Downloader are documented here.

Format based on [Keep a Changelog](https://keepachangelog.com/) and [Semantic Versioning](https://semver.org/).

## [0.5.4] - June 2026

### ✨ Added
- **Documentation**: BUILD_INSTRUCTIONS.md (English & French)
- **.editorconfig**: Code style consistency
- **CONTRIBUTING.md**: Contributor guidelines
- **LICENSE**: Non-commercial attribution license
- **README improvements**: Features table, languages table, FAQ, troubleshooting section
- **Badges**: Version, license, platform badges

### 🔧 Changed
- Dark mode colors refined (tabs, grids, buttons, inputs, controls)
- build_release_win64.bat now includes error checking and progress feedback
- .gitignore expanded for cleaner repository
- CHANGELOG reformatted to standard format

### ✅ Fixed
- Build script verifies .NET SDK before compilation

## [0.5.2]

### ✅ Fixed
- Build failure with `ToggleAllVisible` static method + instance `Tr(...)`
- SDK declaration: Microsoft.NET.Sdk.WindowsDesktop → Microsoft.NET.Sdk

### 🔧 Changed
- Version metadata → 0.5.2
- Resolved NETSDK1137 warning

## [0.5.1]

### ✨ Added
- README.md (English)
- README_EN.md (explicit English)
- README_FR.md (French)
- Backward compatibility with README_FR.txt

## [0.5.0] - Initial Release

### ✨ Added

**Core Functionality:**
- WebSocket connection to Elegoo Centauri Carbon
- List files: `Cmd 258`
- Delete files: `Cmd 259`
- Download G-code files
- Create MP4 timelapse videos (FFmpeg)
- Parallel image downloads
- Fallback printer export: `Cmd 323`
- Automatic log generation

**User Interface:**
- Light & dark themes
- 8 languages: EN, FR, IT, ES, DE, JA, ZH-Hans, ZH-Hant
- Language selector
- Persistent settings in %LOCALAPPDATA%
- Active folder display

**Technical:**
- C# / .NET 8.0 WinForms
- Self-contained Windows x64
- Portable (no installation)
- Created by ashemka branding

### ⚙️ Technical Details
- WebSocket handshake timeout: 8s
- Response timeout: 6s
- HTTP probe timeout: 15s

## [0.4.5]

### ✅ Fixed
- Timelapse frame detection (`tlp_layer_*` files)
- Optimized recursive scanning

### ✨ Added
- Progress indication during scan/download/encode

## [0.4.x - 0.3.x]

- FFmpeg local encoding
- Printer export fallback
- Initial timelapse support

---

**Latest Release:** [0.5.4 on GitHub](https://github.com/Ashemka/Centauri-Carbon-File-Manager-by-Ashemka/releases)

For more info, see [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md)
