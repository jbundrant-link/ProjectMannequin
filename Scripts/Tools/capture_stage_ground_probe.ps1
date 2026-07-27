param(
    [string]$WorldId = 'astral_battlefront',
    [int]$StageIndex = 2,
    [int]$EncounterNumber = 1,
    [string]$OutputName = 'astral_stage3_gameplay_probe_1280x720.png',
    [string]$OutputDirectory = 'Artifacts\StyleCalibration',
    [string]$Resolution = '1280x720'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$godot = Join-Path $env:LOCALAPPDATA 'Programs\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$capture = Join-Path $projectRoot (Join-Path $OutputDirectory $OutputName)

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
$env:PROJECT_MANNEQUIN_LADDER_CAPTURE = $capture
$env:PROJECT_MANNEQUIN_LADDER_CAPTURE_ENCOUNTER_NUMBER = "$EncounterNumber"

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

$lines | Where-Object { $_ -match 'LadderSmoke|StagePlate|SCRIPT ERROR' } | Select-Object -First 20
"capture=$capture exists=$(Test-Path $capture -PathType Leaf)"
