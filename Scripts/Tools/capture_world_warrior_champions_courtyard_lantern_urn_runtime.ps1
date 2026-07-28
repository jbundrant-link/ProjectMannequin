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
$intact = Join-Path $projectRoot "Artifacts\StyleCalibration\world_warrior_champions_courtyard_lantern_urn_intact_runtime_$suffix.png"
$drop = Join-Path $projectRoot "Artifacts\StyleCalibration\world_warrior_champions_courtyard_lantern_urn_drop_runtime_$suffix.png"
$collection = Join-Path $projectRoot "Artifacts\StyleCalibration\world_warrior_champions_courtyard_lantern_urn_collection_runtime_$suffix.png"
$log = Join-Path $projectRoot "Artifacts\StyleCalibration\world_warrior_champions_courtyard_lantern_urn_runtime_$suffix.log"
$environment = [ordered]@{
    DOTNET_ROOT = $dotnetRoot
    PATH = "$dotnetRoot;$env:PATH"
    PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE = '1'
    PROJECT_MANNEQUIN_LADDER_SMOKE_TEST = '1'
    PROJECT_MANNEQUIN_WORLD_ID = 'world_warrior_sector'
    PROJECT_MANNEQUIN_STAGE_INDEX = '4'
    PROJECT_MANNEQUIN_LADDER_CACHE_CAPTURE = $intact
    PROJECT_MANNEQUIN_LADDER_PICKUP_CAPTURE = $drop
    PROJECT_MANNEQUIN_LADDER_PICKUP_COLLECTION_CAPTURE = $collection
    PROJECT_MANNEQUIN_LADDER_PROP_SPRITE_SUFFIX = 'world_warrior_champions_courtyard_lantern_urn_style_v1.png'
    PROJECT_MANNEQUIN_LADDER_PICKUP_SPRITE_SUFFIX = 'world_warrior_meter_pickup_style_v1.png'
}
$previous = @{}

Push-Location $projectRoot
try {
    if (-not (Test-Path $godot -PathType Leaf)) {
        throw "Missing Godot executable: $godot"
    }
    foreach ($path in @($intact, $drop, $collection, $log)) {
        if (Test-Path $path -PathType Leaf) {
            Remove-Item $path -Force
        }
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
    $intactLines = [regex]::Matches(
        $text,
        '\[LadderSmoke\] Styled cache capture saved:[^\r\n]*')
    $dropLines = [regex]::Matches(
        $text,
        '\[LadderSmoke\] Styled pickup capture saved:[^\r\n]*')
    $collectionLines = [regex]::Matches(
        $text,
        '\[LadderSmoke\] Pickup collection capture saved:[^\r\n]*')
    $errors = [regex]::Matches($text, '(?m)^ERROR:[^\r\n]*')
    $warnings = [regex]::Matches($text, '(?m)^WARNING:[^\r\n]*')

    Add-Type -AssemblyName System.Drawing
    $intactImage = if (Test-Path $intact -PathType Leaf) {
        [System.Drawing.Image]::FromFile($intact)
    }
    $dropImage = if (Test-Path $drop -PathType Leaf) {
        [System.Drawing.Image]::FromFile($drop)
    }
    $collectionImage = if (Test-Path $collection -PathType Leaf) {
        [System.Drawing.Image]::FromFile($collection)
    }
    try {
        $passed = $exitCode -eq 0 `
            -and $summary.Success `
            -and $summary.Value -match 'passed=True' `
            -and $summary.Value -match 'stage=4' `
            -and $summary.Value -match 'pickupCollection=True' `
            -and $intactLines.Count -eq 1 `
            -and $dropLines.Count -eq 1 `
            -and $collectionLines.Count -eq 1 `
            -and $intactImage `
            -and $intactImage.Width -eq $Width `
            -and $intactImage.Height -eq $Height `
            -and $dropImage `
            -and $dropImage.Width -eq $Width `
            -and $dropImage.Height -eq $Height `
            -and $collectionImage `
            -and $collectionImage.Width -eq $Width `
            -and $collectionImage.Height -eq $Height `
            -and $errors.Count -eq 0 `
            -and $warnings.Count -eq 0

        [PSCustomObject]@{
            Resolution = $suffix
            Passed = $passed
            IntactCaptures = $intactLines.Count
            DropCaptures = $dropLines.Count
            CollectionCaptures = $collectionLines.Count
            IntactSize = if ($intactImage) {
                "$($intactImage.Width)x$($intactImage.Height)"
            } else {
                'missing'
            }
            DropSize = if ($dropImage) {
                "$($dropImage.Width)x$($dropImage.Height)"
            } else {
                'missing'
            }
            CollectionSize = if ($collectionImage) {
                "$($collectionImage.Width)x$($collectionImage.Height)"
            } else {
                'missing'
            }
            Errors = $errors.Count
            Warnings = $warnings.Count
        }
        if ($summary.Success) {
            $summary.Value
        }
        $intactLines | ForEach-Object { $_.Value }
        $dropLines | ForEach-Object { $_.Value }
        $collectionLines | ForEach-Object { $_.Value }
        if (-not $passed) {
            throw "Champion's Courtyard lantern urn runtime capture failed validation at $suffix."
        }
    }
    finally {
        if ($intactImage) {
            $intactImage.Dispose()
        }
        if ($dropImage) {
            $dropImage.Dispose()
        }
        if ($collectionImage) {
            $collectionImage.Dispose()
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
