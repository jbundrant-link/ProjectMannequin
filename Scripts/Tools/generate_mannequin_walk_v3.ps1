param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_sheet_v3.png'
$metadata = 'Artifacts/style_calibration_mannequin_walk_sheet_v3_job.json'
$references = @(
    'Artifacts/mannequin_walk_choreography_guide.png',
    'Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png',
    'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png',
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png',
    'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
)

$prompt = @'
Create one production-ready 4 columns x 2 rows WALK-CYCLE SOURCE SHEET for the blank PROJECT MANNEQUIN.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the absolute CHOREOGRAPHY AND LIMB-IDENTITY MAP. Reproduce its eight poses cell-for-cell and in the same row-major order: A-contact, A-down, A-passing, A-up, B-contact, B-down, B-passing, B-up. Preserve which colored guide limb is planted, trailing, passing, airborne, or contacting in every cell. Do not reproduce its labels, colored circles, guide lines, stick-figure geometry, or white background.
2. Reference 2 is the exact full-body blank mannequin identity authority. Preserve its faceless warm-ivory porcelain head and plates, plum-brown joints and understructure, lean athletic proportions, segmented anatomy, contour weight, palette, and lighting in every frame.
3. Reference 3 reinforces the same approved mannequin identity and rendering finish. Do not copy its intro pose sequence.
4 and 5 supply only canonical Project Mannequin fighting-game contour, cel-shading, anatomy, and finish. Do not copy their faces, hair, costumes, or identities.

MAP THE GUIDE LIMBS EXACTLY:
- Every GOLD A leg segment in reference 1 becomes the same persistent camera-near LIGHT WARM-IVORY mannequin leg from hip through thigh, knee, shin, ankle, and foot.
- Every CYAN B leg segment in reference 1 becomes the same persistent camera-far DARK PLUM-GRAY SHADED mannequin leg from hip through foot.
- Keep those identities attached to their original hips for all eight frames. Never recolor one leg halfway down. Never keep the light leg leading in the second row.
- Apply the guide's opposite arm swing while rendering both mannequin arms in their normal approved materials.

Exactly eight separate complete full-body figures, all facing screen-right in the same three-quarter side camera, arranged 4x2 and read left-to-right across the top row then the bottom row. Top row: light A contact, down, planted passing while dark B swings forward, then light A toe support/high point while dark B advances. Bottom row: dark B contact, down, planted passing while light A swings forward, then dark B toe support/high point while light A advances. One seamless natural grounded walk loop, not a run, march, shuffle, attack, idle, or eight unrelated poses.

Same body proportions, height, scale, head size, light direction, and identical foot baseline in every cell. Every figure must fit independently within its implied cell with generous clearance. Exactly two arms, two legs, two feet, and all joints in every figure.

Uniform pure chroma-green RGB 0,255,0 background. No floor, cast shadow, scenery, grid, dividers, border, text, labels, numbers, arrows, colored guide marks, motion effects, extra figure, extra limb, missing limb, merged legs, touching figures, or crop.

PROJECT MANNEQUIN STYLE LOCK: polished modern 2.5D fighting-game sprite illustration, controlled dark contours, broad two-to-four-step cel shading, clean graphic highlights, simplified porcelain material, and arcade-readable silhouettes. No photorealism, PBR, grit, microtexture, painterly blur, flat vector art, or pixel art. 2K square animation source.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP mannequin_walk_v3'
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

    $arguments = @('generate', 'create', 'gpt_image_2', '--prompt', $prompt)
    foreach ($reference in $references) {
        $arguments += @('--image', $reference)
    }
    $arguments += @(
        '--aspect_ratio', '1:1',
        '--resolution', '2k',
        '--quality', 'high',
        '--wait',
        '--wait-timeout', '20m',
        '--json'
    )

    $raw = & $cli @arguments
    if ($LASTEXITCODE -ne 0) {
        throw 'Higgsfield failed to generate mannequin_walk_v3.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for mannequin_walk_v3.'
    }

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}