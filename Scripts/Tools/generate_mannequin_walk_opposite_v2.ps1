param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_opposite_sheet_v2.png'
$metadata = 'Artifacts/style_calibration_mannequin_walk_opposite_v2_job.json'
$references = @(
    'Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png',
    'Assets/Sprites/Concepts/StyleCalibration/archive_knight_walk_opposite_sheet_v1.png',
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png',
    'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png',
    'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v1.png'
)

$prompt = @'
Create ONLY the missing opposite-leg half of the blank PROJECT MANNEQUIN walk cycle as EXACTLY FOUR complete full-body poses in a clean 2 columns x 2 rows source sheet, ordered left-to-right then top-to-bottom.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the exact approved mannequin identity authority. Preserve the faceless warm-ivory porcelain head and plates, plum-brown joints and understructure, lean athletic proportions, simplified segmented anatomy, palette, contour weight, and lighting.
2. Reference 2 supplies ONLY the four walking mechanics, spacing, camera, and baseline. Do not copy the Knight's helmet, armor, weapon, costume, or asymmetry.
3 and 4 supply only canonical Project Mannequin cel-shaded fighting-game finish. Do not copy their identities.
5. Reference 5 is a FAILURE EXAMPLE ONLY. Its light foreground leg incorrectly leads in almost every pose. Do not reproduce that limb error, pose repetition, spacing, or crop.

This sheet is specifically the shaded LEG B support phase. The two mannequin legs are persistent physical limbs, not interchangeable colors:
- LEG A is the camera-near limb, always rendered mostly LIGHT WARM IVORY.
- LEG B is the camera-far limb, always rendered DARK PLUM-GRAY IN SHADOW.
- Keep those colors attached to the same physical limbs through all four poses. Never turn the forward dark leg light. Never place the light leg in the frame-1 contact position.

POSE 1, B-CONTACT: dark shaded Leg B reaches farthest toward screen-right with its heel contacting the baseline. Light Leg A extends clearly behind the body on its toe. Dark leg in front, light leg behind.
POSE 2, B-DOWN: body lowers slightly over planted dark Leg B. The dark foot is flat ahead/under the body while light Leg A lifts from behind. Dark leg remains the weight-bearing support.
POSE 3, B-PASSING: dark Leg B remains planted vertically under the hips while bent light Leg A passes beside it moving forward. Show clear knee bend and toe clearance; this cannot resemble contact or idle.
POSE 4, B-UP: dark Leg B is the support leg rising onto its toe just behind the hips while light Leg A's bent knee advances forward toward the next heel contact. This flows into the light-leg contact half-cycle.

All four figures face screen-right in the same three-quarter side camera. Use natural opposite arm swing and a relaxed grounded walk, not a march, run, shuffle, attack, or idle. Preserve exactly two arms, two legs, two feet, and all joints. Same height, scale, head size, light direction, and foot baseline in every pose. Every complete figure fits independently inside its implied quadrant with generous clearance.

Uniform pure chroma-green RGB 0,255,0 background. No floor, cast shadow, scenery, grid, dividers, border, text, labels, numbers, arrows, motion effects, extra figures, extra limbs, missing limbs, merged legs, touching figures, or crop. PROJECT MANNEQUIN STYLE LOCK: polished modern 2.5D fighting-game sprite illustration, controlled dark contours, broad two-to-four-step cel shading, clean graphic highlights, simplified porcelain materials, and arcade-readable silhouettes; no photorealism, PBR, grit, painterly blur, flat vector art, or pixel art. 2K square animation source.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP mannequin_walk_opposite_v2'
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
        throw 'Higgsfield failed to generate mannequin_walk_opposite_v2.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for mannequin_walk_opposite_v2.'
    }

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}