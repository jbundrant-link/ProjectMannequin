$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$godot = Join-Path $env:LOCALAPPDATA 'Programs\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'

Get-ChildItem Env: |
    Where-Object { $_.Name -like 'PROJECT_MANNEQUIN_*' } |
    ForEach-Object { Remove-Item "Env:$($_.Name)" -ErrorAction SilentlyContinue }

$env:DOTNET_ROOT = Join-Path $env:USERPROFILE '.dotnet'
$env:PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE = '1'
$env:PROJECT_MANNEQUIN_WORLD_RUN_TEST = '1'
$env:PROJECT_MANNEQUIN_STAGE_HAZARD_TEST = '1'

Push-Location $projectRoot
try {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $lines = @(
        & $godot --headless --path $projectRoot 'res://Scenes/Main.tscn' --quit-after 30 2>&1 |
            ForEach-Object { "$_" }
    )
    $ErrorActionPreference = $previous
}
finally {
    Pop-Location
}

$lines | Where-Object { $_ -match '^\s*FAIL|=== \d+ passed' }
