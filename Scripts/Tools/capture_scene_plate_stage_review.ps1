$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$probe = Join-Path $PSScriptRoot 'capture_stage_ground_probe.ps1'

$stages = @(
    @{ World = 'astral_battlefront'; Stage = 1; Name = 'astral_st1_skyfall_breach' },
    @{ World = 'astral_battlefront'; Stage = 2; Name = 'astral_st2_capsule_causeway' },
    @{ World = 'astral_battlefront'; Stage = 3; Name = 'astral_st3_energy_rail' },
    @{ World = 'astral_battlefront'; Stage = 4; Name = 'astral_st4_tournament_summit' },
    @{ World = 'world_warrior_sector'; Stage = 4; Name = 'world_warrior_st4_champions_courtyard' }
)

Push-Location $projectRoot
try {
    foreach ($stage in $stages) {
        $output = "$($stage.Name)_final_runtime_1280x720.png"
        $result = & $probe `
            -WorldId $stage.World `
            -StageIndex $stage.Stage `
            -EncounterNumber 1 `
            -OutputName $output `
            -Resolution '1280x720'
        $tail = @($result)[-1]
        if ($tail -notmatch 'exists=True') {
            throw "Capture failed for $($stage.Name)"
        }
        "OK $output"
    }
}
finally {
    Pop-Location
}
