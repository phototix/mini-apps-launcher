# Changelog

## 0.1.0 - 2026-11-19

First working version.

- Tray icon with a dynamic app menu and per-app icons
- JSON config at `%APPDATA%\MiniAppsLauncher\apps.json`, seeded on first run
- Portable mode when `apps.json` sits next to the executable
- Automatic config reload plus a manual **Reload apps** entry
- Start with Windows toggle via `HKCU\...\Run`
- Single-instance launcher and per-app focus-existing behaviour
- Optional per-app elevation, arguments, working directory, and icon
- Missing executables are greyed out instead of failing
- Rotating log at `%LOCALAPPDATA%\MiniAppsLauncher\launcher.log`
- `--selftest` diagnostic mode
- Build, install, and uninstall PowerShell scripts
- GitHub Actions build and tagged release workflow
