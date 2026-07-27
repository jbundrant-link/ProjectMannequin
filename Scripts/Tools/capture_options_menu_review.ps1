param(
    [string]$Resolution = '1280x720'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$godot = Join-Path $env:LOCALAPPDATA 'Programs\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$capture = Join-Path $projectRoot 'Artifacts\StyleCalibration\options_menu_pause.png'

Get-ChildItem Env: |
    Where-Object { $_.Name -like 'PROJECT_MANNEQUIN_*' } |
    ForEach-Object { Remove-Item "Env:$($_.Name)" -ErrorAction SilentlyContinue }

if (Test-Path $capture -PathType Leaf) {
    Remove-Item $capture -Force
}

$env:DOTNET_ROOT = Join-Path $env:USERPROFILE '.dotnet'
$env:PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE = '1'
$env:PROJECT_MANNEQUIN_OPTIONS_UI_SMOKE_TEST = '1'
$env:PROJECT_MANNEQUIN_OPTIONS_CAPTURE = $capture
$eraseCapture = Join-Path $projectRoot 'Artifacts\StyleCalibration\options_menu_erase_confirm.png'
if (Test-Path $eraseCapture -PathType Leaf) { Remove-Item $eraseCapture -Force }
$env:PROJECT_MANNEQUIN_OPTIONS_ERASE_CAPTURE = $eraseCapture

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

$lines | Where-Object { $_ -match 'OptionsSmoke|SCRIPT ERROR' } | Select-Object -First 12
"capture=$capture exists=$(Test-Path $capture -PathType Leaf)"
