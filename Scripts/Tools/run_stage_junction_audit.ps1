param(
    [string]$Resolution = '1280x720'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$godot = Join-Path $env:LOCALAPPDATA 'Programs\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$outputDirectory = Join-Path $projectRoot 'Artifacts\StyleCalibration\Junction'
$report = Join-Path $projectRoot 'Artifacts\stage_backdrop_junction_report.json'

# Every layered stage. Full-frame plate stages draw one complete painting, so
# they have no panel seams to leak through.
$stages = @(
    @{ World = 'world_warrior_sector'; Index = 1; Name = 'dojo_approach' },
    @{ World = 'world_warrior_sector'; Index = 2; Name = 'pavilion_circuit' },
    @{ World = 'world_warrior_sector'; Index = 3; Name = 'grand_tournament' },
    @{ World = 'archive_nexus'; Index = 1; Name = 'intake_boulevard' },
    @{ World = 'archive_nexus'; Index = 2; Name = 'index_vaults' },
    @{ World = 'archive_nexus'; Index = 3; Name = 'corruption_repository' },
    @{ World = 'archive_nexus'; Index = 4; Name = 'knights_reliquary' }
)

# Two clear colours far apart in every channel, so a real gap always swings past
# the audit's delta and painted art never does.
$clearColours = @(
    @{ Suffix = 'clear_a'; Value = '#ff00ff' },
    @{ Suffix = 'clear_b'; Value = '#00ff00' }
)

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$pairArguments = @()

foreach ($stage in $stages) {
    $capturedPair = @()
    foreach ($colour in $clearColours) {
        $capturePath = Join-Path $outputDirectory "$($stage.Name)_$($colour.Suffix).png"
        if (Test-Path $capturePath -PathType Leaf) { Remove-Item $capturePath -Force }

        Get-ChildItem Env: |
            Where-Object { $_.Name -like 'PROJECT_MANNEQUIN_*' } |
            ForEach-Object { Remove-Item "Env:$($_.Name)" -ErrorAction SilentlyContinue }

        $env:DOTNET_ROOT = Join-Path $env:USERPROFILE '.dotnet'
        $env:PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE = '1'
        $env:PROJECT_MANNEQUIN_LADDER_SMOKE_TEST = '1'
        $env:PROJECT_MANNEQUIN_WORLD_ID = $stage.World
        $env:PROJECT_MANNEQUIN_STAGE_INDEX = "$($stage.Index)"
        $env:PROJECT_MANNEQUIN_STAGE_PLATE_CAPTURE_PATH = $capturePath
        $env:PROJECT_MANNEQUIN_FORCE_CLEAR_COLOR = $colour.Value

        Push-Location $projectRoot
        try {
            $previous = $ErrorActionPreference
            $ErrorActionPreference = 'Continue'
            & $godot --path $projectRoot --audio-driver Dummy --resolution $Resolution 'res://Scenes/Main.tscn' 2>&1 | Out-Null
            $ErrorActionPreference = $previous
        }
        finally {
            Pop-Location
        }

        if (Test-Path $capturePath -PathType Leaf) {
            $capturedPair += $capturePath
        }
    }

    if ($capturedPair.Count -eq 2) {
        $pairArguments += '--pair'
        $pairArguments += $capturedPair[0]
        $pairArguments += $capturedPair[1]
    }
    else {
        Write-Warning "capture pair incomplete for $($stage.Name)"
    }
}

Get-ChildItem Env: |
    Where-Object { $_.Name -like 'PROJECT_MANNEQUIN_*' } |
    ForEach-Object { Remove-Item "Env:$($_.Name)" -ErrorAction SilentlyContinue }

Push-Location $projectRoot
try {
    & (Join-Path $projectRoot '.venv\Scripts\python.exe') `
        'Scripts/Tools/audit_stage_backdrop_junction.py' `
        @pairArguments --report $report
    $auditExit = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $auditExit
