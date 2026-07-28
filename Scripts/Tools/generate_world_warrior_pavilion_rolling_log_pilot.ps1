param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

# CORRECTED PLACEMENT: World Warrior's first hazard identity, for Pavilion
# Circuit (Stage 2) -- not Dojo Approach. MASTER_IMPLEMENTATION_PLAN.md >
# Phase 5 > item 16 "World Warrior concepts" explicitly assigns "telegraphed
# rolling training-log sweeper/falling practice props that can hit either
# side" to Pavilion Circuit's "lane-width funnels", while Dojo Approach is
# specified as "minimal hazard pressure, breakable training props" (props
# only, no hazards). An earlier session wired these into Dojo Approach by
# mistake, using Dojo's own rope/timber/stone motifs; this generator
# replaces that with Pavilion Circuit's own established motifs instead, so
# the hazard reads as native to its actual host stage.
#
# Motifs come from the Pavilion Circuit environment and the already-approved
# Pavilion Rack Chest rather than being invented: vermilion lacquered posts,
# slate tiled pent roofs, indigo banners with a saffron stripe, and the red
# circular medallion that repeats across that stage.

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_rolling_log_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_pavilion_rolling_log_pilot_v1_job.json'

$rackChest = 'Assets/Sprites/Props/WorldWarrior/world_warrior_pavilion_rack_chest_style_v1.png'
$pavilionBackdrop = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png'
$archiveIntakeTram = 'Assets/Sprites/Hazards/Archive/archive_intake_tram_style_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR PAVILION ROLLING TRAINING LOG, an original stage HAZARD viewed in a clean right-facing three-quarter angle, centered and fully visible, lying on its side as if resting on a pavilion deck ready to roll.

The prop must read instantly as a dangerous rolling training-hall obstacle at gameplay size, clearly a lacquered ceremonial practice log that could sweep across a pavilion deck and knock a fighter down, and NOT a fence post, tree trunk, barrel, cannon, weapon, crate, or standing pillar. Build one horizontal CYLINDRICAL structure with this exact large-form hierarchy:
- one thick, long cylindrical log lying on its side, finished in glossy vermilion lacquer with visible wood-grain rings at both cut ends;
- two thick indigo cloth rope-wraps encircling the log at intervals, each with one bold horizontal saffron stripe, matching pavilion banner cloth;
- one small red circular medallion emblem stamped near one end, with a simple four-spoke ring inside, echoing the pavilion's repeating medallion motif;
- one polished bronze end-cap ring on each cut end of the log, matching the rivet studs on pavilion lacquered posts;
- a clear sense of weight and roll-readiness: the log reads as perfectly round in cross-section and unmistakably mobile, not a fixed static pillar.

Function must come from unmistakable rolling-hazard cues: the perfectly round cross-section, the cloth rope wraps, and the bronze end-caps. Use only a few large components with thick contours. It must remain legible when reduced to roughly 96, 128, and 160 pixels tall. The cloth wraps and round log silhouette must survive at that size. Keep the log resting flat and grounded, as if sitting on the pavilion deck a moment before it starts to roll.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior Pavilion Rack Chest. Use ONLY its vermilion-lacquer finish, indigo-and-saffron cloth material, red medallion motif, and bronze rivet/stud material language for family consistency with its host stage. Do NOT copy its tall standing-cabinet silhouette, tiled pent roof, exposed practice staves, or chest structure.
2. Reference 2 is the approved Pavilion Circuit environment. Use only its warm dusk pavilion mood, lacquered post material language, indigo banner cloth, red medallion motif, and ordered value hierarchy. Do not reproduce architecture, buildings, decks, railings, lanterns, banners in place, mountains, or scene.
3. Reference 3 is the approved Archive Intake Tram hazard. Use ONLY its stage-hazard contour weight, large-form simplification, and readable moving-danger presentation appropriate for a horizontally sweeping hazard. Do NOT copy its vehicle shape, sci-fi paneling, rail, warning stripes, or Archive color identity.
4-6. References 4-6 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered hazard prop on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including beyond both cut ends and below the full round underside. No training dummy, breakable crate, standing cabinet, stepped podium, ceremonial urn, tree, fence, barrel, cannon, weapon, spear, blade, treasure chest, gold coins, gems, lock, hinge padlock, keyhole, Archive crystal, shard, cube, chip, data cache, sci-fi panel, glowing seam, hanging lantern, potion, gourd, drum, fan, letters, numbers, kanji, logo, label text, star icon, warning sign, arrow, face, eyes, person, creature, floating orb, aura, detached ring, rays, sparks, active flame/fire effect, debris cloud, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square stage-hazard pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_pavilion_rolling_log_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @(
        $rackChest,
        $pavilionBackdrop,
        $archiveIntakeTram,
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

    Write-Host 'GENERATE world_warrior_pavilion_rolling_log_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the pavilion rolling log pilot after $RequestAttempts attempts."
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
