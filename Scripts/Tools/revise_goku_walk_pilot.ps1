param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/goku_base_walk_sheet_pilot_v2.png'
$metadata = 'Artifacts/style_calibration_goku_base_walk_sheet_pilot_v2_job.json'
$references = @(
    'Assets/Sprites/Concepts/StyleCalibration/goku_base_walk_sheet_pilot_v1.png',
    'Artifacts/StyleCalibration/GokuWalk/goku_base_identity_1.png',
    'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v2.png',
    'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png',
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
)

$prompt = @'
Revise the first supplied BASE GOKU WALK-CYCLE SHEET for production containment while preserving its animation exactly.

Reference 1 is the exact pose, identity, camera, frame-order, and rendering authority. Preserve the same eight Goku figures, same body design, same orange-and-blue costume, same black hair, same left-to-right top-to-bottom order, and the same distinct gait geometry in every corresponding cell. Do not replace any pose with idle and do not change which physical leg leads. Reference 2 reinforces exact base Goku identity. Reference 3 reinforces the intended contact/down/passing/up then opposite-leg contact/down/passing/up mechanics. References 4 and 5 supply only canonical Project Mannequin rendering finish.

The only required corrections are layout and background:
- exactly 4 columns x 2 rows in one square 2K image;
- shrink every figure uniformly to approximately 78 percent of its current height;
- center each complete full-body figure inside its implied cell;
- leave at least 12 percent of each cell height as empty background above the hair and below both boots;
- keep generous horizontal separation so no hair, hand, elbow, knee, or boot touches another cell;
- use one identical foot baseline per row;
- replace the white background with perfectly uniform pure chroma-green RGB 0,255,0.

Every hair spike, head, hand, leg, and boot must be fully visible with clear green margin. No crop, no touching figures, no duplicated pose, no extra figure, no missing limb, no grid, divider, border, floor, shadow, text, labels, numbers, arrows, aura, energy, projectile, or motion effect. Do not change to photorealism, PBR, painterly blur, flat vector art, or pixel art. Preserve polished modern 2.5D anime fighting-game contours and cel shading.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP goku_base_walk_sheet_pilot_v2'
        return
    }
    if (-not (Test-Path $cli)) {
        throw "Higgsfield CLI not found: $cli"
    }
    foreach ($reference in $references) {
        if (-not (Test-Path $reference)) {
            throw "Missing generation reference: $reference"
        }
    }

    $arguments = @('generate', 'create', 'nano_banana_pro', '--prompt', $prompt)
    foreach ($reference in $references) {
        $arguments += @('--image', (Resolve-Path $reference).Path)
    }
    $arguments += @(
        '--aspect_ratio', '1:1',
        '--resolution', '2k',
        '--wait',
        '--wait-timeout', '20m',
        '--json'
    )
    $raw = & $cli @arguments
    if ($LASTEXITCODE -ne 0) {
        throw 'Higgsfield failed to revise goku_base_walk_sheet_pilot_v2.'
    }
    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for goku_base_walk_sheet_pilot_v2.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}