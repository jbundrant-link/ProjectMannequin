param(
    [ValidateSet('Keyboard', 'Xbox', 'PlayStation', 'Nintendo', 'GenericGamepad')]
    [string]$Family = 'PlayStation',
    [string]$Resolution = '1280x720'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$godot = Join-Path $env:LOCALAPPDATA 'Programs\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$capture = Join-Path $projectRoot "Artifacts\StyleCalibration\move_list_glyphs_$($Family.ToLowerInvariant()).png"

Get-ChildItem Env: |
    Where-Object { $_.Name -like 'PROJECT_MANNEQUIN_*' } |
    ForEach-Object { Remove-Item "Env:$($_.Name)" -ErrorAction SilentlyContinue }

if (Test-Path $capture -PathType Leaf) {
    Remove-Item $capture -Force
}
New-Item -ItemType Directory -Force -Path (Split-Path $capture -Parent) | Out-Null

$env:DOTNET_ROOT = Join-Path $env:USERPROFILE '.dotnet'
$env:PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE = '1'
$env:PROJECT_MANNEQUIN_MOVE_LIST_SMOKE_TEST = '1'
$env:PROJECT_MANNEQUIN_MOVE_LIST_CAPTURE = $capture
$env:PROJECT_MANNEQUIN_FORCE_GLYPH_FAMILY = $Family

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

$lines | Where-Object { $_ -match 'SCRIPT ERROR' } | Select-Object -First 3
"family=$Family capture=$capture exists=$(Test-Path $capture -PathType Leaf)"
