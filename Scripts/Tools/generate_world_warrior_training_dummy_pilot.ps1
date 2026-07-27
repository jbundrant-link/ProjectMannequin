param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_training_dummy_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_training_dummy_pilot_v1_job.json'
$dojo = 'Assets/Stages/WorldWarrior/world_warrior_dojo_backdrop_style_v1.png'
$pavilion = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of a WORLD WARRIOR BREAKABLE TRAINING DUMMY: a single freestanding nonhumanoid martial-arts striking post viewed in a clean right-facing three-quarter angle, centered and fully visible.

The prop must read instantly as BOTH a PRACTICE TARGET and a BREAKABLE GAMEPLAY OBJECT at arcade gameplay size. Build one broad, stable, waist-to-chest-high training apparatus with this exact large-form hierarchy:
- one thick vertical charcoal-brown lacquered hardwood spine, visibly constructed from three sacrificial stacked timber segments;
- one broad warm-ivory rectangular torso strike pad wrapped in canvas, centered on the upper half, with a simple vermilion diamond-shaped impact patch and no text or symbol;
- one smaller saffron-gold circular upper strike pad above it, clearly a padded target rather than a face or head;
- two short horizontal side striking arms at different heights, made from dark timber and capped by compact deep-indigo padded blocks; they are equipment bars, not human arms;
- one wide low octagonal weighted base in deep indigo lacquer with vermilion corner braces and warm-ivory wear strips;
- four large exposed bronze break pegs and two bold diagonal split seams where the post would snap under attacks.

Breakability must come from large readable construction cues: segmented sacrificial wood, exposed pegs, wedge joints, broad split seams, and slightly chipped pad edges. Keep the object intact and usable in this pilot; no destroyed state, loose debris, dust, impact burst, or attacker. The silhouette must remain compact and bottom-heavy, with the base wider than the upper post. Use only a few large components so it remains legible when reduced to roughly 128-192 pixels tall.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior Dojo Approach environment. Use only its charcoal ink contours, warm lacquered wood, worn ivory plaster/canvas, muted vermilion banners, and dusk-lit martial-arts identity. Do not reproduce its buildings, roofs, trees, mountains, floor, or scene.
2. Reference 2 is the approved Pavilion Circuit environment. Use only its cleaner vermilion/deep-indigo/saffron tournament hierarchy and refined champion-equipment finish. Do not reproduce its architecture, lanterns, platform, drums, mountains, or scene.
3-5. References 3-5 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered object on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including above the upper target, outside both side arms, and below the entire base. No human figure, mannequin body, torso anatomy, face, eyes, mouth, hair, hands, feet, clothing, creature, opponent, person-shaped silhouette, punching bag, hanging bag, crate, treasure chest, pickup icon, health cross, meter symbol, currency symbol, explosive warning, barrel, metal sci-fi container, armor, weapon, text, letters, numbers, kanji, logo, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, detached parts, photorealism, PBR, gritty realism, painterly blur, flat vector art, pixel art, or crop. 2K square prop pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_training_dummy_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @($dojo, $pavilion, $mannequin, $ryu, $goku)
    foreach ($required in $references) {
        if (-not (Test-Path $required -PathType Leaf)) {
            throw "Missing generation reference: $required"
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

    Write-Host 'GENERATE world_warrior_training_dummy_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the training dummy pilot after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for the training dummy pilot.'
    }

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}