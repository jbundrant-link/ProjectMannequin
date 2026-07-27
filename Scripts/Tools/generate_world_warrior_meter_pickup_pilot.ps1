param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_meter_pickup_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_meter_pickup_pilot_v1_job.json'
$healthPickup = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_health_pickup_style_pilot_v1.png'
$trainingDummy = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_training_dummy_style_pilot_v1.png'
$pavilion = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR FOCUS DRUM, a small original combat-meter pickup viewed in a clean right-facing three-quarter angle, centered and fully visible.

The pickup must read instantly as stored martial focus, fighting rhythm, and reusable combat energy at arcade gameplay size without using an Archive crystal, shard, lightning icon, text, potion, coin, or health cue. Build one compact hand-carried HOURGLASS DRUM with this exact large-form hierarchy:
- a distinctive squat hourglass silhouette, wider at both ends and deeply pinched at the center;
- two broad warm-ivory leather drumheads, one front-left and one rear-right, each marked only by three thick concentric saffron-gold resonance rings with no letters or symbols;
- one deep-indigo lacquered central waist with a wide plum grip band;
- six thick vermilion braided tension cords spanning diagonally between the two drum rims in large readable X-shaped bands;
- four chunky saffron tuning beads placed symmetrically on the cords;
- two short vermilion cloth streamers tied at the central grip, angled backward and fully attached;
- one small warm-ivory base rest integrated under the lower rim so the pickup feels grounded, never floating.

Function must come from unmistakable rhythm/focus cues: taut drumheads, visible tension cords, tuning beads, and bold resonance rings. Keep the object pristine, energized, and desirable. Use only a few large components with thick contours. It must remain legible when reduced to roughly 64, 80, and 96 pixels tall. The hourglass body and integrated base rest must form a stable grounded silhouette. No aura, floating rings, sound-wave effects, sparks, or detached energy.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior Vitality Gourd. Use only its pickup-scale contour weight, large-form simplification, grounded collectible presentation, and warm-ivory/vermilion/deep-indigo/saffron hierarchy. Do not copy its calabash body, leaves, cork, cord knot, cradle, health function, or silhouette.
2. Reference 2 is the approved World Warrior training dummy. Use only its practical tournament-equipment materials, broad cel planes, indigo lacquer, vermilion braces, and sturdy object readability. Do not copy its post, pads, arms, base, pegs, break seams, or silhouette.
3. Reference 3 is the approved Pavilion Circuit environment. Use only its ceremonial drum/tournament motif, vermilion/deep-indigo/saffron hierarchy, and refined champion-equipment finish. Do not reproduce architecture, lanterns, platform, mountains, or scene.
4-6. References 4-6 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered pickup on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including outside both drum rims and attached streamers and below the complete base rest. No Archive crystal, gemstone, shard, prism, blade, spear, lightning bolt, battery, meter bar, floating orb, aura, detached ring, rays, sparks, health cross, heart, leaf, food, gourd, potion bottle, flask, coin, medal, trophy, currency symbol, text, letters, numbers, kanji, logo, face, eyes, mouth, person, creature, weapon, crate, chest, explosive warning, detached pieces, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square pickup pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_meter_pickup_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @($healthPickup, $trainingDummy, $pavilion, $mannequin, $ryu, $goku)
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

    Write-Host 'GENERATE world_warrior_meter_pickup_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the meter pickup pilot after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for the meter pickup pilot.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}