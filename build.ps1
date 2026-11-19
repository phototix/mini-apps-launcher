[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$OutputDir
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if (-not $OutputDir) {
    $OutputDir = Join-Path $root 'dist'
}

$frameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path (Join-Path $frameworkDir 'csc.exe'))) {
    $frameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
}

$csc = Join-Path $frameworkDir 'csc.exe'
if (-not (Test-Path $csc)) {
    throw 'Could not find the .NET Framework C# compiler (csc.exe).'
}

$icon = Join-Path $root 'assets\launcher.ico'
if (-not (Test-Path $icon)) {
    throw "Missing $icon. Run: python tools\make-icon.py"
}

$sources = @(Get-ChildItem -Path (Join-Path $root 'src') -Filter *.cs | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) {
    throw 'No source files were found in src\.'
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$exe = Join-Path $OutputDir 'MiniAppsLauncher.exe'

$references = @(
    'System.dll'
    'System.Core.dll'
    'System.Drawing.dll'
    'System.Windows.Forms.dll'
    'System.Web.Extensions.dll'
) | ForEach-Object { '/reference:' + (Join-Path $frameworkDir $_) }

$compilerArgs = @(
    '/nologo'
    '/target:winexe'
    '/platform:anycpu'
    ('/win32icon:' + $icon)
    ('/out:' + $exe)
    ('/resource:' + (Join-Path $root 'config\apps.example.json') + ',apps.example.json')
)
$compilerArgs += $references

if ($Configuration -eq 'Release') {
    $compilerArgs += '/optimize+'
}
else {
    $compilerArgs += '/debug+'
}

$compilerArgs += $sources

& $csc $compilerArgs
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

Copy-Item (Join-Path $root 'config\apps.example.json') (Join-Path $OutputDir 'apps.example.json') -Force

$built = Get-Item $exe
Write-Host ("Built {0} ({1:N0} bytes)" -f $built.FullName, $built.Length)
