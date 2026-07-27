$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$python = Join-Path $projectRoot '.venv\Scripts\python.exe'
$auditSpec = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_source_audit_spec.json'
$sourceAudit = 'Artifacts/StyleCalibration/world_warrior_grand_grappler_tetsu_source_audit.json'
$compositionManifest = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_animation_sources_v1.json'
$atlas = 'Assets/Sprites/Enemies/world_warrior_grand_grappler_tetsu_style_v1.png'
$atlasPreview = 'Artifacts/StyleCalibration/world_warrior_grand_grappler_tetsu_style_v1_atlas_preview.png'
$compositionReport = 'Artifacts/StyleCalibration/world_warrior_grand_grappler_tetsu_style_v1_composition_report.json'
$atlasAudit = 'Artifacts/StyleCalibration/world_warrior_grand_grappler_tetsu_style_v1_atlas_audit.json'
$walkAudit = 'Artifacts/StyleCalibration/world_warrior_grand_grappler_tetsu_walk_v1_audit.json'
$walkPreview = 'Artifacts/StyleCalibration/world_warrior_grand_grappler_tetsu_walk_v1_preview.png'

function Invoke-CheckedPython {
    param([string[]]$Arguments)

    & $python @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Python command failed: $($Arguments -join ' ')"
    }
}

Push-Location $projectRoot
try {
    if (-not (Test-Path $python -PathType Leaf)) {
        throw "Missing project Python environment: $python"
    }

    Invoke-CheckedPython @(
        'Scripts/Tools/audit_enemy_source_batch.py',
        $auditSpec
    )
    Invoke-CheckedPython @(
        'Scripts/Tools/build_enemy_composition_manifest.py',
        $sourceAudit,
        $compositionManifest,
        '--atlas-output', $atlas,
        '--preview-output', $atlasPreview,
        '--composition-report', $compositionReport,
        '--attack-name', 'iron_gate_clinch'
    )
    Invoke-CheckedPython @(
        'Scripts/Tools/compose_enemy_sheet.py',
        '--manifest', $compositionManifest
    )
    Invoke-CheckedPython @(
        'Scripts/Tools/audit_enemy_atlas.py',
        $atlas,
        '--output', $atlasAudit,
        '--wide-frame', '5:6',
        '--wide-frame', '5:7',
        '--wide-frame', '5:8',
        '--wide-frame', '5:9',
        '--minimum-wide-frame-width', '200',
        '--maximum-green-dominant-pixels', '0'
    )
    Invoke-CheckedPython @(
        'Scripts/Tools/audit_walk_cycle.py',
        $atlas,
        '--columns', '10',
        '--rows', '9',
        '--walk-row', '1',
        '--frame-count', '8',
        '--minimum-distinct-poses', '6',
        '--pose-distance', '0.2',
        '--output', $walkAudit,
        '--preview', $walkPreview
    )

    $source = Get-Content $sourceAudit -Raw | ConvertFrom-Json
    $atlasResult = Get-Content $atlasAudit -Raw | ConvertFrom-Json
    $walk = Get-Content $walkAudit -Raw | ConvertFrom-Json
    [PSCustomObject]@{
        SourceFamilies = "$($source.passed_count)/$($source.family_count)"
        UsedFrames = $atlasResult.nonempty_used_frames
        ReserveFrames = $atlasResult.transparent_reserve_frames
        GreenSpill = $atlasResult.green_dominant_pixels
        DistinctWalkPoses = $walk.distinct_pose_count
        AtlasSha256 = $atlasResult.sha256
    }
}
finally {
    Pop-Location
}