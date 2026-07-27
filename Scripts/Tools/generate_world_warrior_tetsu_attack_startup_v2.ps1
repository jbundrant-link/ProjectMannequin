param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_style_pilot_v3_normalized.png'
$failedStartup = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_attack_startup_sheet_style_v1.png'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_attack_startup_sheet_style_v2.png'
$metadata = 'Artifacts/style_calibration_world_warrior_grand_grappler_tetsu_attack_startup_sheet_v2_job.json'
$grappler = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
$makoto = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Create a corrected production IRON GATE CLINCH startup/active source sheet for WORLD WARRIOR GRAND GRAPPLER TETSU.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the exact approved Tetsu identity authority. Preserve his exact bald head, stern face, two blunt square beard braids, massive rectangular body, boxy vermilion and warm-ivory vest, indigo waist wrap, charcoal shorts, plum forearm wraps, saffron ankle cords, ivory-toed shoes, square bronze left-hip seal, colors, right-facing three-quarter camera, cel shading, and light in all five poses.
2. Reference 2 is the rejected v1 startup and supplies choreography order only. Its top-right wide-arm figure is clipped at the right canvas edge and MUST NOT be copied at its failed scale or framing. Preserve the progression from guard to open gates to inward clinch, but redraw every figure complete.
3. Reference 3 is Tournament Grappler for shared World Warrior heavyweight cloth grammar only. Do not copy his curls, silver forelock, diagonal tunic, loose trousers, round token, low shoulder drive, face, or identity.
4. Reference 4 is Pavilion Ace Makoto for named-elite finish quality only. Do not copy her body, braid, costume, stance, or identity.
5-7. References 5-7 are canonical rendering-finish anchors only. Match their confident dark contours, broad two-to-four-band cel shading, simplified cloth materials, designed highlights, and arcade-readable fighting-game anatomy. Do not copy any reference identity.

Produce exactly FIVE distinct complete full-body IRON GATE CLINCH startup/active poses arranged as three figures on the top row and two figures on the bottom row, ordered left-to-right then top-to-bottom:
1. Upright extra-wide open-hand clinch guard, both feet planted.
2. Lead foot makes one broad forward step while both elbows open and both hands remain visibly open.
3. Both arms spread chest-high like two heavy gates around an imaginary opponent, torso tall, palms facing inward, every fingertip and elbow fully visible.
4. Hips settle as both forearms sweep inward in a square body-lock arc, hands still separate and open.
5. Strongest active silhouette: an upright crushing two-arm body-lock entry at chest and rib height, both open arms clearly curved inward around an imaginary torso, elbows behind hands, chest advanced, knees flexed, both feet planted wide, head above the imaginary shoulder line.

FRAMING IS ABSOLUTE. Use one consistent smaller character scale across all five figures. Each figure must fit entirely inside its own implied cell. In the three-figure top row, each complete figure must remain within one third of the canvas width with at least 64 pixels of green between neighboring figures. Preserve at least 96 pixels of pure green outside the leftmost and rightmost body points, above every head and raised hand, below every shoe, and around the bottom-row figures. Pose 3 must be scaled down enough that both wide open hands, fingertips, elbows, and shoulders fit with clear green on both sides. No body part may touch any canvas edge.

Every figure faces right in the same three-quarter camera and remains on uniform pure chroma-green RGB 0,255,0. No low shoulder tackle, Tournament Grappler Shoulder Drive, punch, closed striking fist, clasped hands, crossed wrists, target figure, contact, throw victim, impact, speed line, motion blur, back-facing turn, recovery pose, scalp hair, missing beard braid, missing square seal, extra limb, touching figures, border, text, floor, shadow, scene, or crop. 2K square animation source.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP tetsu_attack_startup_v2'
        return
    }

    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @(
        $identity,
        $failedStartup,
        $grappler,
        $makoto,
        $mannequin,
        $ryu,
        $goku
    )
    foreach ($required in $references) {
        if (-not (Test-Path $required -PathType Leaf)) {
            throw "Missing generation reference: $required"
        }
    }

    Write-Host 'GENERATE tetsu_attack_startup_v2'
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
        throw 'Higgsfield failed for tetsu_attack_startup_v2.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for tetsu_attack_startup_v2.'
    }

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}