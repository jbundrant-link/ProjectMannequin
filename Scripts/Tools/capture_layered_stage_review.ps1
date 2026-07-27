$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$probe = Join-Path $PSScriptRoot 'capture_stage_ground_probe.ps1'

$stages = @(
    @{ World = 'world_warrior_sector'; Stage = 1; Name = 'world_warrior_dojo_approach' },
    @{ World = 'world_warrior_sector'; Stage = 2; Name = 'world_warrior_pavilion_circuit' },
    @{ World = 'world_warrior_sector'; Stage = 3; Name = 'world_warrior_grand_tournament' },
    @{ World = 'archive_nexus'; Stage = 1; Name = 'archive_intake_boulevard' },
    @{ World = 'archive_nexus'; Stage = 2; Name = 'archive_index_vaults' },
    @{ World = 'archive_nexus'; Stage = 3; Name = 'archive_corruption_repository' },
    @{ World = 'archive_nexus'; Stage = 4; Name = 'archive_knights_reliquary' }
)

Push-Location $projectRoot
try {
    foreach ($resolution in @('1280x720', '1920x1080')) {
        foreach ($stage in $stages) {
            $output = "$($stage.Name)_layered_calibrated_runtime_$resolution.png"
            $result = & $probe `
                -WorldId $stage.World `
                -StageIndex $stage.Stage `
                -EncounterNumber 1 `
                -OutputName $output `
                -Resolution $resolution
            $tail = @($result)[-1]
            if ($tail -notmatch 'exists=True') {
                throw "Capture failed for $($stage.Name) at $resolution"
            }
            "OK $output"
        }
    }
}
finally {
    Pop-Location
}
