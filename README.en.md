# StS2 Mod Manager

[中文](./README.md) | [English](./README.en.md)

An opinionated desktop manager for **Slay the Spire 2** mods, with mod source management, enable/disable workflows, save-slot copy/restore, and one-click launch options.

---

## 1. What This Tool Does

- Detects game paths automatically and supports manual path selection.
- Manages mods between tool-side and game-side directories.
- Supports save copy between modded and non-modded profiles, with backups.
- Launches the game directly or via Steam (with optional `--nomods`).

## 2. Core Features

### 2.1 Path Management

- Auto-detect common install paths.
- Manual browse for game directory.
- Quick-open actions for game folder and mod folders.

### 2.2 Mod Management

- Tool mods directory: `<tool-dir>/Mods`
- Game mods directory: `<game-dir>/mods` (lowercase)
- Enable/disable per mod, enable all, disable all.
- Pull mods back from game directory to tool directory.
- Filter mods by keyword, tag, and author.
- Unified metadata editing (name, tags, author, notes, links).

### 2.3 Save Management

- Base save path: `%AppData%/SlayTheSpire2/steam`
- Works on the latest detected Steam numeric ID directory.
- Copy between:
  - `modded/profileX`
  - `profileX`
- Supports slot-based copy and full-copy workflows.
- Creates timestamped backups before destructive operations.

### 2.4 Launch Modes

- Direct launch: `SlayTheSpire2.exe`
- Direct launch without mods: `SlayTheSpire2.exe --nomods`
- Steam launch: `steam://rungameid/2868840`
- Steam launch without mods: `steam://run/2868840//--nomods`

## 3. Download

- GitHub Releases: <https://github.com/liwenhao0427/StS2ModManager/releases>

## 4. Quick Start

1. Start the app and verify the detected game path.
2. Select mods you want to enable.
3. Use keyword/tag/author filters to find mods quickly.
4. Launch the game with your preferred mode.
5. For save copy, confirm direction and slots before applying.

## 5. Development

### 5.1 Stack

- .NET 9 (WPF)
- CommunityToolkit.Mvvm

### 5.2 Build

```bash
dotnet restore
dotnet build
```

### 5.3 Release Outputs

The project ships two single-file Windows executables:

- Framework-dependent: `StS2ModManager.依赖.Net环境版本.exe`
- Self-contained: `StS2ModManager.exe`

Release output directory:

`C:\Users\temp\项目\杀戮尖塔2Mod\Slay the Spire 2\StS2ModManager\ReleaseSingle`

## 6. Troubleshooting

- Launch issues: verify `SlayTheSpire2.exe` exists under the selected game path.
- Steam launch issues: ensure Steam is installed and logged in.
- URL open issues: use a fully qualified URL (`http://` or `https://`).
- Save copy issues: verify source slot exists and backup path is writable.

---

## License

This repository currently has no explicit open-source license file. If you plan to redistribute or modify it publicly, please add a `LICENSE` file first.
