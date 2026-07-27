$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$capture = Join-Path $PSScriptRoot 'capture_stage_only_frame.ps1'

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
    foreach ($stage in $stages) {
        $output = "$($stage.Name)_stage_only_1280x720.png"
        $result = & $capture -WorldId $stage.World -StageIndex $stage.Stage -OutputName $output
        if (@($result)[-1] -notmatch 'exists=True') {
            throw "Stage-only capture failed for $($stage.Name)"
        }
        "OK $output"
    }
}
finally {
    Pop-Location
}
