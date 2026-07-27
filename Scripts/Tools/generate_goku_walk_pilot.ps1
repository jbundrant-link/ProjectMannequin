param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/goku_base_walk_sheet_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_goku_base_walk_sheet_pilot_v1_job.json'
$references = @(
    'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v2.png',
    'Artifacts/mannequin_walk_choreography_guide.png',
    'Artifacts/StyleCalibration/GokuWalk/goku_base_identity_1.png',
    'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png',
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
)

$prompt = @'
Create a production-ready BASE GOKU WALK-CYCLE SOURCE SHEET with exactly EIGHT separate complete full-body figures arranged in a clean square 4 columns x 2 rows, read left-to-right across the top row then left-to-right across the bottom row.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the exact accepted walk choreography authority. Reproduce its eight sequential contact/down/passing/up gait beats, full opposite-leg alternation, opposing arm swing, screen-right three-quarter side camera, spacing, and common foot baseline. Do not copy the mannequin body, porcelain materials, colors, or identity.
2. Reference 2 is the controlling anatomical timing map. GOLD A and CYAN B remain the same physical limbs through all eight frames. Follow exactly: A-contact, A-down, A-passing, A-up, B-contact, B-down, B-passing, B-up. Do not render guide colors, labels, circles, lines, stick geometry, or white background.
3. Reference 3 is the exact BASE GOKU identity authority: adult muscular anime martial artist; tall black upswept spiky hair; orange sleeveless gi with dark-blue undershirt, belt, wristbands, and boots; warm skin; clean anime face. Preserve this exact hair, face, outfit geometry, proportions, palette, and boot design in every frame.
4 and 5 are canonical Project Mannequin rendering-finish anchors only: controlled dark contours, broad two-to-four-step cel shading, clean designed highlights, strong fighting-game anatomy, and polished modern 2.5D anime presentation. Do not copy their identity or pose.

TOP ROW, FRAMES 1-4, A-LEG HALF:
1. A heel contact forward toward screen-right while B extends behind on its toe.
2. Body compresses over planted A while B lifts from behind.
3. A remains planted under the hips while bent B passes forward with visible toe clearance.
4. A rises onto its toe behind the hips while B advances toward heel contact.

BOTTOM ROW, FRAMES 5-8, EXACT OPPOSITE B-LEG HALF:
5. B heel contact forward toward screen-right while A extends behind on its toe.
6. Body compresses over planted B while A lifts from behind.
7. B remains planted under the hips while bent A passes forward with visible toe clearance.
8. B rises onto its toe behind the hips while A advances toward frame 1.

The forward leg in frame 5 must be physically opposite the forward leg in frame 1. Reverse the arms as well. Use restrained natural walking arm swing, not fists-up combat guard. Keep the camera-near leg slightly brighter orange and the camera-far leg one cel-shadow band darker so limb ownership remains readable, but never recolor the gi or swap colors at a knee. At least six materially different silhouettes; no repeated idle pose.

All figures face screen-right with identical head size, body scale, light direction, and foot baseline. Every figure fits independently inside its implied cell with generous green clearance on all sides. Exactly two arms, two legs, two boots, one head, and all costume pieces in every frame. Natural grounded walk only: no crouch attack, dash, run, flight, jump, high-knee march, kick, punch, aura, energy, projectile, target, or motion effect.

Uniform pure chroma-green RGB 0,255,0 background. No floor, cast shadow, scenery, grid, dividers, border, text, labels, numbers, arrows, extra figure, extra limb, missing limb, merged legs, touching figures, or crop.

PROJECT MANNEQUIN STYLE LOCK: cohesive modern 2.5D fighting-game production, confident dark ink contours, broad cel-shaded value planes, clean graphic highlights, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game finish. No photorealism, PBR, grit, microtexture, painterly blur, flat vector art, or pixel art. 2K square animation source.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP goku_base_walk_sheet_pilot_v1'
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
        throw 'Higgsfield failed to generate goku_base_walk_sheet_pilot_v1.'
    }
    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for goku_base_walk_sheet_pilot_v1.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}