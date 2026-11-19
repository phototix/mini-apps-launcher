[CmdletBinding()]
param(
    [switch]$RemoveConfig
)

$ErrorActionPreference = 'Stop'

Get-Process MiniAppsLauncher -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if (Get-ItemProperty -Path $runKey -Name 'MiniAppsLauncher' -ErrorAction SilentlyContinue) {
    Remove-ItemProperty -Path $runKey -Name 'MiniAppsLauncher' -ErrorAction SilentlyContinue
    Write-Host 'Removed autostart entry.'
}

$shortcutPath = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\Mini Apps Launcher.lnk'
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
    Write-Host 'Removed Start Menu shortcut.'
}

$installDir = Join-Path $env:LOCALAPPDATA 'Programs\MiniAppsLauncher'
if (Test-Path $installDir) {
    Remove-Item $installDir -Recurse -Force
    Write-Host "Removed $installDir"
}

if ($RemoveConfig) {
    $configDir = Join-Path $env:APPDATA 'MiniAppsLauncher'
    if (Test-Path $configDir) {
        Remove-Item $configDir -Recurse -Force
        Write-Host "Removed $configDir"
    }
}

Write-Host 'Uninstalled.'
