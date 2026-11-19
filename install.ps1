[CmdletBinding()]
param(
    [switch]$NoAutostart,
    [switch]$NoLaunch
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$builtExe = Join-Path $root 'dist\MiniAppsLauncher.exe'

if (-not (Test-Path $builtExe)) {
    & (Join-Path $root 'build.ps1')
}

$installDir = Join-Path $env:LOCALAPPDATA 'Programs\MiniAppsLauncher'
$installedExe = Join-Path $installDir 'MiniAppsLauncher.exe'

New-Item -ItemType Directory -Force -Path $installDir | Out-Null

Get-Process MiniAppsLauncher -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

Copy-Item $builtExe $installedExe -Force

$configDir = Join-Path $env:APPDATA 'MiniAppsLauncher'
$configPath = Join-Path $configDir 'apps.json'
if (-not (Test-Path $configPath)) {
    New-Item -ItemType Directory -Force -Path $configDir | Out-Null
    Copy-Item (Join-Path $root 'config\apps.example.json') $configPath
    Write-Host "Created config at $configPath"
}

$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$shortcutPath = Join-Path $startMenu 'Mini Apps Launcher.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installedExe
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = "$installedExe,0"
$shortcut.Description = 'Mini Apps Launcher'
$shortcut.Save()

if (-not $NoAutostart) {
    $runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
    Set-ItemProperty -Path $runKey -Name 'MiniAppsLauncher' -Value ('"' + $installedExe + '" --tray')
}

if (-not $NoLaunch) {
    Start-Process $installedExe
}

Write-Host "Installed: $installedExe"
Write-Host "Config:    $configPath"
Write-Host "Shortcut:  $shortcutPath"
if (-not $NoAutostart) {
    Write-Host 'Autostart: registered (HKCU Run)'
}
