param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_score_pickup_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_score_pickup_pilot_v1_job.json'
$healthPickup = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_health_pickup_style_pilot_v1.png'
$meterPickup = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_meter_pickup_style_pilot_v1.png'
$grandTournament = 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_backdrop_style_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR JUDGE'S LAUREL FAN, a small original score-value pickup viewed in a clean right-facing three-quarter angle, centered and fully visible.

The pickup must read instantly as tournament judgment, earned honor, and arcade score value at gameplay size without using an Archive data prism, currency text or symbol, generic coin, medal, trophy, crown, gem, score number, or floating points effect. Build one compact ceremonial JUDGE'S DISPLAY FAN with this exact large-form hierarchy:
- one broad open semicircular folding-fan silhouette, wider than tall, with a gently scalloped BLUNT outer edge and no sharp weapon tips;
- five large vermilion lacquer-cloth fan panels separated by thick warm-ivory ribs radiating from one deep-indigo round pivot;
- one continuous saffron-gold LAUREL SPRIG arcing across the upper fan edge, simplified into exactly seven broad paired leaves and one central bud, attached to the fan rather than floating;
- three chunky warm-ivory judge's tally tabs rising behind the fan at three different heights, each completely blank and marked only by one thick saffron edge stripe, with no letters, numbers, symbols, or writing;
- one deep-indigo lacquered U-shaped display rest integrated beneath the pivot so the pickup is grounded and clearly ceremonial rather than hand-held combat equipment;
- one short plum braided honor tassel tied to the right side of the pivot, with one saffron bead and two attached blunt cloth tails.

Function must come from unmistakable tournament-judging and earned-honor cues: the open judge fan, blank ranking tabs, attached laurel sprig, and formal display rest. Use only a few large components with thick contours. It must remain legible when reduced to roughly 64, 80, and 96 pixels tall. The broad fan arc, seven-leaf laurel, three stepped tabs, and indigo rest must survive at that size. Keep the object pristine, collectible, and bottom-weighted. This is a ceremonial display fan, NOT a weapon: no blade edge, spikes, sharpened ribs, throwing-fan silhouette, hand grip, or combat pose.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior Vitality Gourd. Use only its pickup-scale contour weight, large-form simplification, grounded collectible presentation, and warm-ivory/vermilion/deep-indigo/saffron hierarchy. Do not copy its calabash body, leaves, cork, cord knot, cradle, health function, or silhouette.
2. Reference 2 is the approved World Warrior Focus Drum. Use only its pickup scale, bold tournament-equipment finish, thick ivory contours, indigo lacquer, vermilion tension accents, and attached tassel treatment. Do not copy its hourglass body, drumheads, resonance rings, cords, beads, grip, meter function, or silhouette.
3. Reference 3 is the approved Grand Tournament environment. Use only its judging-canopy ceremony, empty championship architecture, warm ivory/vermilion/deep-indigo/saffron hierarchy, and formal tournament mood. Do not reproduce architecture, stairs, walls, mountains, braziers, banners, judge silhouette, trophy silhouette, or scene.
4-6. References 4-6 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered pickup on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including above all three tally tabs and the laurel, outside the full fan arc and tassel, and below the complete display rest. No Archive data prism, crystal, gemstone, shard, cube, chip, card, scroll, document, book, currency, coin, medal, trophy, cup, crown, belt, championship plate, star icon, number, tally mark, score text, plus sign, letters, kanji, logo, face, eyes, mouth, person, judge, creature, weapon, blade, spike, sharp fan, crate, chest, potion, gourd, drum, floating orb, aura, detached ring, rays, sparks, detached leaves, detached tabs, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square pickup pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_score_pickup_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @(
        $healthPickup,
        $meterPickup,
        $grandTournament,
        $mannequin,
        $ryu,
        $goku
    )
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

    Write-Host 'GENERATE world_warrior_score_pickup_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the score pickup pilot after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for the score pickup pilot.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}