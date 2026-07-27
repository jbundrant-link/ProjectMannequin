param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_health_pickup_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_health_pickup_pilot_v1_job.json'
$trainingDummy = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_training_dummy_style_pilot_v1.png'
$dojo = 'Assets/Stages/WorldWarrior/world_warrior_dojo_backdrop_style_v1.png'
$pavilion = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR VITALITY GOURD, a small original health-recovery pickup viewed in a clean right-facing three-quarter angle, centered and fully visible.

The pickup must read instantly as restorative martial-arts provision at arcade gameplay size without using a medical cross, heart icon, crystal, text, or generic potion bottle. Build one compact, bottom-heavy, hand-carried CALABASH GOURD with this exact large-form hierarchy:
- a distinctive double-lobed warm-ivory ceramic body, smaller round upper bulb and broader lower bulb joined by a narrow waist;
- one chunky saffron-gold cork and collar at the top;
- one asymmetrical fresh jade-green medicinal leaf sprig with exactly three broad leaves emerging beside the cork;
- one wide vermilion braided tournament cord wrapped around the narrow waist, tied into a large simple side knot with two short cloth tails;
- one deep-indigo lacquered protective foot cradle under the lower bulb, shaped as three broad upward petals rather than a bottle base;
- one simple jade-green leaf-shaped ceramic inlay centered on the lower bulb, large enough to survive at 64 pixels, with no surrounding symbol or text.

Function must come from unmistakable food/herbal cues: the calabash silhouette, fresh medicinal leaves, sealed restorative vessel, and clean nourishing color hierarchy. Keep the object pristine, desirable, and easy to collect. Use only a few large components with thick contours. It must remain legible when reduced to roughly 64, 80, and 96 pixels tall. The lower bulb and indigo cradle should form a stable grounded silhouette; do not make it float or emit a large aura.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior training dummy. Use only its confident contour weight, warm ivory/vermilion/deep-indigo/saffron material hierarchy, simplified cel planes, and practical tournament-equipment finish. Do not copy its post, targets, side arms, base, break seams, proportions, or silhouette.
2. Reference 2 is the approved Dojo Approach environment. Use only its worn lacquered wood, warm ivory cloth/ceramic feeling, muted vermilion accents, and grounded martial-arts identity. Do not reproduce any architecture, floor, trees, or scene.
3. Reference 3 is the approved Pavilion Circuit environment. Use only its refined champion-equipment finish and clean vermilion/deep-indigo/saffron hierarchy. Do not reproduce architecture, lanterns, drums, platform, mountains, or scene.
4-6. References 4-6 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered pickup on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including above the cork and leaves, outside the cord tails, and below the complete indigo cradle. No medical cross, plus sign, heart shape, heart container, Archive crystal, gemstone, shard, floating orb, glowing cube, syringe, pill, bandage roll, potion bottle, glass flask, branded product, food package, text, letters, numbers, kanji, logo, face, eyes, mouth, person, creature, weapon, crate, chest, explosive warning, detached pieces, large aura, rays, sparkles, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square pickup pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_health_pickup_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @($trainingDummy, $dojo, $pavilion, $mannequin, $ryu, $goku)
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

    Write-Host 'GENERATE world_warrior_health_pickup_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the health pickup pilot after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for the health pickup pilot.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}