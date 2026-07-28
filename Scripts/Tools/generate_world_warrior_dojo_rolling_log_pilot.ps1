param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

# First World Warrior HAZARD identity (not a breakable prop): a rolling
# training log for Dojo Approach. World Warrior currently has zero hazard
# zones -- every stage so far only has static breakable props. This is the
# first of two new dojo training hazards (paired with the falling practice
# weight) that give the opening stage its own obstacle-course identity.
#
# Motifs come directly from the Dojo Approach midground art itself: the two
# rope-bound wooden training posts inside the open training pavilion. This
# hazard reimagines that exact material language -- rope-wrapped aged timber
# -- as a large rolling log obstacle lying on its side, a classic martial-arts
# training-hall obstacle, sweeping across the training floor (LinearSweep
# hazard behavior) rather than standing upright like the posts.

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_dojo_rolling_log_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_dojo_rolling_log_pilot_v1_job.json'

$dojoMidground = 'Assets/Stages/WorldWarrior/world_warrior_dojo_midground_style_v1.png'
$archiveIntakeTram = 'Assets/Sprites/Hazards/Archive/archive_intake_tram_style_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR DOJO ROLLING TRAINING LOG, an original stage HAZARD viewed in a clean right-facing three-quarter angle, centered and fully visible, lying on its side as if resting on a training-hall floor ready to roll.

The prop must read instantly as a dangerous rolling training-hall obstacle at gameplay size, clearly a large heavy wooden log bound with training rope that could sweep across a floor and knock a fighter down, and NOT a fence post, tree trunk, barrel, cannon, weapon, crate, or decorative log pile. Build one horizontal CYLINDRICAL structure with this exact large-form hierarchy:
- one thick, long cylindrical log lying on its side, aged warm-brown timber with visible wood-grain rings at both cut ends;
- two or three thick aged cream-rope wrappings encircling the log at intervals, matching dojo training-post binding, with one loose rope tail hanging free;
- subtle scuffed and worn patches along the log's underside where it has rolled before, rendered as simplified graphic wear rather than photoreal texture;
- one small brass end-cap ring on each cut end of the log, echoing dojo training-hall metal fittings;
- a clear sense of weight and roll-readiness: the log reads as perfectly round in cross-section and unmistakably mobile, not a fixed static beam.

Function must come from unmistakable rolling-hazard cues: the perfectly round cross-section, the rope bindings, and the worn rolling scuffs. Use only a few large components with thick contours. It must remain legible when reduced to roughly 96, 128, and 160 pixels tall. The rope bindings and round log silhouette must survive at that size. Keep the log resting flat and grounded, as if sitting on the training floor a moment before it starts to roll.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved Dojo Approach midground stage art. Use ONLY its aged warm-brown timber material, rope-binding material language from its two training posts, and overall dojo color palette. Do NOT reproduce the training pavilion structure, roof, stone lantern, bonsai tree, torii gate, gong, or bell; use only the timber and rope material treatment.
2. Reference 2 is the approved Archive Intake Tram hazard. Use ONLY its stage-hazard contour weight, large-form simplification, and readable moving-danger presentation appropriate for a horizontally sweeping hazard. Do NOT copy its vehicle shape, sci-fi paneling, rail, warning stripes, or Archive color identity.
3-5. References 3-5 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered hazard prop on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including beyond both cut ends and below the full round underside. No training dummy, breakable crate, standing cabinet, stepped podium, ceremonial urn, tree, fence, barrel, cannon, weapon, spear, blade, treasure chest, gold coins, gems, lock, hinge padlock, keyhole, Archive crystal, shard, cube, chip, data cache, sci-fi panel, glowing seam, hanging lantern, potion, gourd, drum, fan, letters, numbers, kanji, logo, label text, star icon, warning sign, arrow, face, eyes, person, creature, floating orb, aura, detached ring, rays, sparks, active flame/fire effect, debris cloud, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square stage-hazard pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_dojo_rolling_log_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @(
        $dojoMidground,
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

    Write-Host 'GENERATE world_warrior_dojo_rolling_log_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the dojo rolling log pilot after $RequestAttempts attempts."
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
