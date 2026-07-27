param(
    [string]$WorldId = 'archive_nexus',
    [int]$StageIndex = 2,
    [switch]$HighContrast,
    [switch]$ReducedFlash,
    [string]$OutputName,
    [string]$Resolution = '1280x720'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$godot = Join-Path $env:LOCALAPPDATA 'Programs\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$suffix = if ($HighContrast) { 'high_contrast' } elseif ($ReducedFlash) { 'reduced_flash' } else { 'default' }
$name = if ($OutputName) { $OutputName } else { "hazard_telegraph_$suffix.png" }
$capture = Join-Path $projectRoot "Artifacts\StyleCalibration\$name"

Get-ChildItem Env: |
    Where-Object { $_.Name -like 'PROJECT_MANNEQUIN_*' } |
    ForEach-Object { Remove-Item "Env:$($_.Name)" -ErrorAction SilentlyContinue }

if (Test-Path $capture -PathType Leaf) {
    Remove-Item $capture -Force
}
New-Item -ItemType Directory -Force -Path (Split-Path $capture -Parent) | Out-Null

$env:DOTNET_ROOT = Join-Path $env:USERPROFILE '.dotnet'
$env:PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE = '1'
$env:PROJECT_MANNEQUIN_LADDER_SMOKE_TEST = '1'
$env:PROJECT_MANNEQUIN_WORLD_ID = $WorldId
$env:PROJECT_MANNEQUIN_STAGE_INDEX = "$StageIndex"
$env:PROJECT_MANNEQUIN_HAZARD_TELEGRAPH_CAPTURE = $capture
if ($HighContrast) {
    $env:PROJECT_MANNEQUIN_FORCE_HIGH_CONTRAST_TELEGRAPHS = '1'
}
if ($ReducedFlash) {
    $env:PROJECT_MANNEQUIN_FORCE_REDUCED_FLASH = '1'
}

Push-Location $projectRoot
try {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $lines = @(
        & $godot --path $projectRoot --audio-driver Dummy --resolution $Resolution 'res://Scenes/Main.tscn' 2>&1 |
            ForEach-Object { "$_" }
    )
    $ErrorActionPreference = $previous
}
finally {
    Pop-Location
}

$lines | Where-Object { $_ -match 'Telegraph capture|SCRIPT ERROR' } | Select-Object -First 5
"capture=$capture exists=$(Test-Path $capture -PathType Leaf)"
