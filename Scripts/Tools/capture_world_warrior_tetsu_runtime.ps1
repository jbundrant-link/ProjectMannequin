param(
    [ValidateSet(1280, 1920)]
    [int]$Width,

    [ValidateSet(720, 1080)]
    [int]$Height
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$godot = Join-Path $env:LOCALAPPDATA 'Programs\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$dotnetRoot = Join-Path $env:USERPROFILE '.dotnet'
$suffix = "${Width}x${Height}"
$idle = Join-Path $projectRoot "Artifacts\StyleCalibration\world_warrior_grand_grappler_tetsu_idle_runtime_$suffix.png"
$attack = Join-Path $projectRoot "Artifacts\StyleCalibration\world_warrior_grand_grappler_tetsu_iron_gate_clinch_runtime_$suffix.png"
$log = Join-Path $projectRoot "Artifacts\StyleCalibration\world_warrior_grand_grappler_tetsu_runtime_$suffix.log"
$environment = [ordered]@{
    DOTNET_ROOT = $dotnetRoot
    PATH = "$dotnetRoot;$env:PATH"
    PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE = '1'
    PROJECT_MANNEQUIN_LADDER_SMOKE_TEST = '1'
    PROJECT_MANNEQUIN_WORLD_ID = 'world_warrior_sector'
    PROJECT_MANNEQUIN_STAGE_INDEX = '3'
    PROJECT_MANNEQUIN_LADDER_ENEMY_FORM_ID = 'world_warrior_grand_grappler_tetsu'
    PROJECT_MANNEQUIN_LADDER_ENEMY_MOVE_ID = 'world_warrior_grand_grappler_tetsu_attack'
    PROJECT_MANNEQUIN_LADDER_ENEMY_MOVE_FRAME = '20'
    PROJECT_MANNEQUIN_LADDER_TETSU_IDLE_CAPTURE = $idle
    PROJECT_MANNEQUIN_LADDER_TETSU_ATTACK_CAPTURE = $attack
}
$previous = @{}

Push-Location $projectRoot
try {
    if (-not (Test-Path $godot -PathType Leaf)) {
        throw "Missing Godot executable: $godot"
    }
    foreach ($entry in $environment.GetEnumerator()) {
        $previous[$entry.Key] = [System.Environment]::GetEnvironmentVariable(
            $entry.Key,
            [System.EnvironmentVariableTarget]::Process)
        [System.Environment]::SetEnvironmentVariable(
            $entry.Key,
            $entry.Value,
            [System.EnvironmentVariableTarget]::Process)
    }

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $lines = @(
            & $godot `
                --path $projectRoot `
                --audio-driver 'Dummy' `
                --resolution $suffix `
                'res://Scenes/Main.tscn' 2>&1 |
                ForEach-Object { "$_" }
        )
        $exitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    $lines | Set-Content $log -Encoding utf8

    $text = $lines -join [Environment]::NewLine
    $summary = [regex]::Match($text, '\[LadderSmoke\] SUMMARY[^\r\n]*')
    $captureLines = [regex]::Matches(
        $text,
        '\[CharacterVisual\] Grand Grappler Tetsu[^\r\n]*capture saved[^\r\n]*')
    $errors = [regex]::Matches($text, '(?m)^ERROR:[^\r\n]*')
    $warnings = [regex]::Matches($text, '(?m)^WARNING:[^\r\n]*')
    $unexpectedWarnings = @(
        $warnings | Where-Object {
            $_.Value -notmatch 'ObjectDB instances were leaked at exit'
        }
    )

    Add-Type -AssemblyName System.Drawing
    $idleImage = if (Test-Path $idle -PathType Leaf) {
        [System.Drawing.Image]::FromFile($idle)
    }
    $attackImage = if (Test-Path $attack -PathType Leaf) {
        [System.Drawing.Image]::FromFile($attack)
    }
    try {
        $passed = $exitCode -eq 0 `
            -and $summary.Success `
            -and $summary.Value -match 'passed=True' `
            -and $summary.Value -match 'stage=3' `
            -and $captureLines.Count -eq 2 `
            -and $idleImage `
            -and $idleImage.Width -eq $Width `
            -and $idleImage.Height -eq $Height `
            -and $attackImage `
            -and $attackImage.Width -eq $Width `
            -and $attackImage.Height -eq $Height `
            -and $errors.Count -eq 0 `
            -and $unexpectedWarnings.Count -eq 0

        [PSCustomObject]@{
            Resolution = $suffix
            Passed = $passed
            CaptureLines = $captureLines.Count
            IdleSize = if ($idleImage) {
                "$($idleImage.Width)x$($idleImage.Height)"
            } else {
                'missing'
            }
            AttackSize = if ($attackImage) {
                "$($attackImage.Width)x$($attackImage.Height)"
            } else {
                'missing'
            }
            Errors = $errors.Count
            UnexpectedWarnings = $unexpectedWarnings.Count
            KnownShutdownWarnings = $warnings.Count - $unexpectedWarnings.Count
        }
        if ($summary.Success) {
            $summary.Value
        }
        $captureLines | ForEach-Object { $_.Value }
        if (-not $passed) {
            throw "Tetsu runtime capture failed validation at $suffix."
        }
    }
    finally {
        if ($idleImage) {
            $idleImage.Dispose()
        }
        if ($attackImage) {
            $attackImage.Dispose()
        }
    }
}
finally {
    foreach ($entry in $environment.GetEnumerator()) {
        [System.Environment]::SetEnvironmentVariable(
            $entry.Key,
            $previous[$entry.Key],
            [System.EnvironmentVariableTarget]::Process)
    }
    Pop-Location
}