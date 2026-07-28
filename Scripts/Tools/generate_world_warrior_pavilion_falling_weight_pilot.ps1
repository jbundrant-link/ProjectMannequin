param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

# CORRECTED PLACEMENT: World Warrior's second hazard identity, for Pavilion
# Circuit (Stage 2), paired with the Rolling Log. See
# generate_world_warrior_pavilion_rolling_log_pilot.ps1 for the full
# correction rationale (MASTER_IMPLEMENTATION_PLAN.md assigns both hazards to
# Pavilion Circuit, not Dojo Approach).
#
# Motifs come from Pavilion Circuit and the already-approved Pavilion Rack
# Chest: vermilion lacquer, indigo-and-saffron cloth, and the red medallion
# motif, applied to a hanging practice weight instead of Dojo's stone weight.

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_falling_weight_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_pavilion_falling_weight_pilot_v1_job.json'

$rackChest = 'Assets/Sprites/Props/WorldWarrior/world_warrior_pavilion_rack_chest_style_v1.png'
$pavilionBackdrop = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png'
$archiveFallingShelf = 'Assets/Sprites/Hazards/Archive/archive_repository_falling_shelf_style_v1.png'
$rollingLogPilot = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_rolling_log_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR PAVILION FALLING TRAINING WEIGHT, an original stage HAZARD viewed in a clean right-facing three-quarter angle, centered and fully visible, hanging as if suspended a moment before it drops.

The prop must read instantly as a dangerous falling pavilion training weight at gameplay size, clearly a lacquered wooden practice weight that drops from pavilion rafters to slam the deck, and NOT a bell, gong, lantern, boulder, wrecking ball, anchor, or decorative rock. Build one compact, heavy, rounded structure with this exact large-form hierarchy:
- one large rounded vermilion-lacquered wooden training weight, wider at the bottom than the top, bound in a crisscrossing harness of thick indigo cloth rope;
- one central rope loop at the top where the harness gathers into a single suspension cord, cut short with a frayed cord end trailing above it;
- one bold horizontal saffron stripe wrapping the harness knot, matching pavilion banner cloth, and a small red circular medallion emblem stamped on the weight's front face;
- a shallow carved practice-strike ring worn smooth at the weight's front center from repeated training impact;
- a clear sense of mass and imminent drop: the weight reads as dense and heavy, hanging taut on its cord rather than resting on the ground.

Function must come from unmistakable falling-hazard cues: the taut suspension cord, the frayed cut end above, and the worn strike-target face. Use only a few large components with thick contours. It must remain legible when reduced to roughly 96, 128, and 160 pixels tall. The rope harness and strike-target mark must survive at that size. Keep the weight hanging centered, as if caught mid-suspension a moment before falling.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior Pavilion Rack Chest. Use ONLY its vermilion-lacquer finish, indigo-and-saffron cloth material, and red medallion motif for family consistency with its host stage. Do NOT copy its tall standing-cabinet silhouette, tiled pent roof, exposed practice staves, or chest structure.
2. Reference 2 is the approved Pavilion Circuit environment. Use only its warm dusk pavilion mood, lacquered post material language, indigo banner cloth, red medallion motif, and ordered value hierarchy. Do not reproduce architecture, buildings, decks, railings, lanterns, banners in place, mountains, or scene.
3. Reference 3 is the approved Archive Repository Falling Shelf hazard. Use ONLY its stage-hazard contour weight, large-form simplification, and readable about-to-fall presentation appropriate for a FallingStrike hazard. Do NOT copy its shelf shape, metal framework, sci-fi paneling, or Archive color identity.
4. Reference 4 is the approved World Warrior Pavilion Rolling Log hazard pilot. Use ONLY its vermilion-lacquer and cloth-wrap material treatment and hazard contour weight for family consistency. Do NOT copy its cylindrical log shape; this new hazard must be a rounded hanging weight, not a log.
5-7. References 5-7 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered hazard prop on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including above the frayed cord end and below the full rounded base. No training dummy, breakable crate, standing cabinet, stepped podium, ceremonial urn, rolling log, bell, gong, lantern, boulder, wrecking ball, anchor, tree, fence, barrel, cannon, weapon, spear, blade, treasure chest, gold coins, gems, lock, hinge padlock, keyhole, Archive crystal, shard, cube, chip, data cache, sci-fi panel, glowing seam, potion, gourd, drum, fan, letters, numbers, kanji, logo, label text, star icon, warning sign, arrow, face, eyes, person, creature, floating orb, aura, detached ring, rays, sparks, active flame/fire effect, debris cloud, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square stage-hazard pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_pavilion_falling_weight_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @(
        $rackChest,
        $pavilionBackdrop,
        $archiveFallingShelf,
        $rollingLogPilot,
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

    Write-Host 'GENERATE world_warrior_pavilion_falling_weight_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the pavilion falling weight pilot after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $payload = $raw | Out-String
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $metadata) | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $projectRoot $metadata),
        $payload,
        (New-Object System.Text.UTF8Encoding($false)))

    $job = $payload | ConvertFrom-Json
    $record = if ($job -is [System.Array]) { $job[0] } else { $job }
    $url = $null
    foreach ($candidate in @(
            $record.result_url,
            $record.results.raw.url,
            $record.results.min.url,
            $record.min_result_url)) {
        if ($candidate) { $url = $candidate; break }
    }
    if (-not $url) {
        throw "Higgsfield returned no image URL. Job metadata: $metadata"
    }
    if ($record.status -and $record.status -ne 'completed') {
        throw "Higgsfield job did not complete (status=$($record.status))."
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $output) | Out-Null
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "WROTE $output"
}
finally {
    Pop-Location
}
