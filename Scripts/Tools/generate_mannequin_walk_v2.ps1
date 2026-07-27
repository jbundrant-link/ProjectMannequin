param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_sheet_v2.png'
$metadata = 'Artifacts/style_calibration_mannequin_walk_sheet_v2_job.json'
$references = @(
    'Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png',
    'Assets/Sprites/Concepts/StyleCalibration/archive_knight_walk_sheet_v3.png',
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png',
    'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png',
    'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v1.png'
)

$prompt = @'
Create a corrected production 4x2 WALK-CYCLE SOURCE SHEET for the blank PROJECT MANNEQUIN.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the exact approved mannequin identity authority. Preserve its faceless warm-ivory porcelain head and armor plates, narrow dark neck, plum-brown ball joints and understructure, lean athletic proportions, clean segmented anatomy, dark contour weight, palette, and lighting in every frame.
2. Reference 2 supplies ONLY the exact eight-pose walking cadence, clear leg alternation, 4-columns-by-2-rows spacing, camera, and baseline. Do not copy its armor, weapon, asymmetry, colors, or character identity.
3 and 4 are canonical Project Mannequin rendering-finish anchors only: controlled dark contours, broad two-to-four-step anime cel shading, clean graphic highlights, and arcade-readable fighting-game anatomy. Do not copy either character's face, hair, clothing, or identity.
5. Reference 5 is a FAILURE EXAMPLE ONLY. It incorrectly keeps the same light foreground leg leading through both rows. Do not reproduce its repeated leg identity, repeated stride poses, uneven spacing, or cropped edge figure.

Produce EXACTLY EIGHT separate, complete, full-body, screen-right-facing WALK poses in a clean square 4 columns x 2 rows, read left-to-right across the top row and then left-to-right across the bottom row. This is one seamless eight-frame walk loop, not eight unrelated poses. Keep one camera angle, one body proportion set, one figure scale, one light direction, and one identical foot baseline.

LEG IDENTITY IS THE HIGHEST-PRIORITY REQUIREMENT. Treat the two legs as persistent labeled limbs throughout the sequence:
- LEG A is the camera-near/foreground leg and is rendered mostly warm light ivory.
- LEG B is the camera-far/background leg and is rendered visibly darker in plum-gray shadow.
- Never recolor one physical leg to impersonate the other. Never keep Leg A in front for both half-cycles.

TOP ROW, FRAMES 1-4, LEG A HALF-CYCLE:
1. A-CONTACT: light Leg A reaches forward toward screen-right with heel contacting; dark Leg B extends behind on its toe.
2. A-DOWN: body compresses slightly over planted light Leg A; dark Leg B begins lifting from behind.
3. A-PASSING: light Leg A is planted under the hips while bent dark Leg B passes it moving forward. Feet and knees must visibly differ from frame 2.
4. A-UP: light Leg A rises onto its toe behind the hips while dark Leg B's knee advances forward toward the next contact.

BOTTOM ROW, FRAMES 5-8, EXACT OPPOSITE LEG HALF-CYCLE:
5. B-CONTACT: dark Leg B is now clearly the forward leading leg, reaching farthest toward screen-right with heel contacting; light Leg A extends behind on its toe. This must be the limb-identity reverse of frame 1.
6. B-DOWN: body compresses slightly over planted dark Leg B; light Leg A begins lifting from behind.
7. B-PASSING: dark Leg B is planted under the hips while bent light Leg A passes it moving forward. This must be the limb-identity reverse of frame 3.
8. B-UP: dark Leg B rises onto its toe behind the hips while light Leg A's knee advances forward, flowing naturally back into frame 1.

Arms swing naturally opposite the legs: when light Leg A leads, the opposite arm leads; when dark Leg B leads, the arm relationship reverses. Keep both arms, both legs, both feet, all joints, and the full head visible in every frame. Use a natural grounded walk, not a run, march, fighting attack, shuffle, or idle pose.

Every figure must remain independently contained inside its implied cell with generous green clearance on all four sides. Uniform pure chroma-green RGB 0,255,0 background. No floor, cast shadow, scenery, grid, dividers, border, text, labels, numbers, arrows, motion effects, extra figures, extra limbs, missing limbs, merged legs, touching figures, or cropped anatomy. PROJECT MANNEQUIN STYLE LOCK: polished modern 2.5D fighting-game sprite illustration, confident dark ink contours, broad cel-shaded value planes, designed highlights, simplified porcelain material, and a clean readable silhouette; no photorealism, PBR, gritty texture, painterly blur, flat vector art, or pixel art. 2K square animation source.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host "SKIP mannequin_walk_v2"
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
        $arguments += @('--image', $reference)
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
        throw 'Higgsfield failed to generate mannequin_walk_v2.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for mannequin_walk_v2.'
    }

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}