# Mini Apps Launcher

A tiny Windows tray launcher for portable apps. It sits in the notification area, starts with Windows, and gives you a one-click menu for the tools you use every day.

Built as a single ~46 KB native WinForms executable with no installer, no runtime install, and no dependencies beyond .NET Framework 4.8 (included in Windows 10/11).

## Features

- **Tray menu** with one entry per app, real icons, and a working-directory aware launch
- **Config driven** — add an app by adding one JSON block; no rebuild
- **Hot reload** — edits to `apps.json` are picked up automatically, or via **Reload apps**
- **Start with Windows** toggle in the menu (per-user `HKCU\...\Run`, no admin rights)
- **Single instance** — a second launch just opens the existing menu
- **Focus instead of duplicate** — clicking an app that is already running brings its window forward (per-app `singleInstance`)
- **Elevation support** — optional `runAsAdmin` per app
- **Graceful failures** — missing executables are greyed out and errors go to a balloon tip plus a log
- **Self-test mode** for diagnostics and CI

## Requirements

- Windows 10 or 11
- .NET Framework 4.8 (preinstalled on supported Windows versions)
- PowerShell 5.1+ to build and install
- Optional: Python 3 + Pillow, only to regenerate the icon

## Build

```powershell
git clone https://github.com/<you>/mini-apps-launcher.git
cd mini-apps-launcher
.\build.ps1
```

The executable is written to `dist\MiniAppsLauncher.exe`.

## Install

```powershell
.\install.ps1
```

This copies the exe to `%LOCALAPPDATA%\Programs\MiniAppsLauncher`, creates a Start Menu shortcut, seeds `%APPDATA%\MiniAppsLauncher\apps.json`, registers autostart, and launches the app.

Useful switches:

```powershell
.\install.ps1 -NoAutostart -NoLaunch
```

Uninstall:

```powershell
.\uninstall.ps1              # keeps your apps.json
.\uninstall.ps1 -RemoveConfig
```

## Adding an app

Open **Open config folder** from the tray menu and edit `apps.json`:

```json
{
  "version": 1,
  "apps": [
    {
      "id": "image-enhancer",
      "name": "Image Enhancer",
      "path": "%USERPROFILE%\\Tools\\Real-ESRGAN\\ImageEnhancer.exe",
      "arguments": "",
      "workingDirectory": "%USERPROFILE%\\Tools\\Real-ESRGAN",
      "icon": "%USERPROFILE%\\Tools\\Real-ESRGAN\\ImageEnhancer.ico",
      "category": "Media",
      "runAsAdmin": false,
      "enabled": true,
      "singleInstance": true
    }
  ]
}
```

Save the file. The launcher reloads it within a second, or choose **Reload apps**.

### Fields

| Field | Required | Description |
| --- | --- | --- |
| `id` | no | Stable identifier; generated from the name when missing |
| `name` | yes | Menu label |
| `path` | yes | Executable, shortcut, or document to launch |
| `arguments` | no | Command-line arguments |
| `workingDirectory` | no | Defaults to the executable's folder. Set this when the app resolves files relative to its working directory |
| `icon` | no | `.ico` or image path. Falls back to the executable's embedded icon |
| `category` | no | Reserved for grouping |
| `runAsAdmin` | no | Launch with the `runas` verb (UAC prompt) |
| `enabled` | no | `false` hides the entry without deleting it |
| `singleInstance` | no | Defaults to `true`; focus the running window instead of launching again |

Paths may use environment variables such as `%USERPROFILE%`, `%APPDATA%`, and `%ProgramFiles%`; they are expanded when the menu is built.

### Config locations

1. `apps.json` next to `MiniAppsLauncher.exe` (portable mode, checked first)
2. `%APPDATA%\MiniAppsLauncher\apps.json` (normal mode)

On first run the launcher seeds the config from the bundled example. The repo's `config/apps.example.json` is embedded into the executable at build time.

## Autostart: why not a Windows service?

Windows services run in Session 0, isolated from the interactive desktop, so they cannot show a tray icon or launch GUI apps into your session. A tray launcher must be a per-user background process started at login. This project uses the standard `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry, which:

- needs no administrator rights
- can be toggled from the tray menu
- is easy to inspect and remove

The process stays running until you choose **Exit**.

## Command line

```powershell
MiniAppsLauncher.exe --launch <id>
MiniAppsLauncher.exe --selftest
```

`--launch` starts a configured app directly, using the same path and working-directory rules as the menu. It is handy for shortcuts, Stream Deck buttons, or scripts. Exit codes: `0` started, `1` unexpected error, `2` launch failed, `3` unknown id. All attempts are recorded in the log.

## Diagnostics

```powershell
.\dist\MiniAppsLauncher.exe --selftest
Get-Content "$env:TEMP\MiniAppsLauncher.selftest.txt"
```

The report lists every configured app, whether its path and working directory exist, and whether its icon loads.

Logs: `%LOCALAPPDATA%\MiniAppsLauncher\launcher.log` (rotated at 1 MB).

## Project layout

```
src/                        C# sources (compiled with the .NET Framework csc.exe)
config/apps.example.json    Seed config, embedded into the exe
assets/launcher.ico         App and tray icon
tools/make-icon.py          Regenerates the icon (Pillow)
build.ps1                   Compiles dist/MiniAppsLauncher.exe
install.ps1 / uninstall.ps1 Per-user install and cleanup
.github/workflows/build.yml CI build, artifact, and tagged releases
```

## Notes

- Builds are unsigned. SmartScreen or antivirus may warn on first run; that is expected for a locally built executable.
- The launcher never modifies the apps it starts; it only runs them with the configured working directory.
- Tested on Windows 11 26200 with .NET Framework 4.8.

## License

MIT. See [LICENSE](LICENSE).
