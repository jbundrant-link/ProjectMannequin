param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_style_pilot_v3_normalized.png'
$choreography = 'Assets/Sprites/Mannequin/higgsfield_misc_sheet_v1.png'
$failedMisc = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_misc_sheet_style_v1.png'
$grappler = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
$makoto = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_style_pilot_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_misc_sheet_style_v2.png'
$metadata = 'Artifacts/style_calibration_world_warrior_grand_grappler_tetsu_misc_sheet_v2_job.json'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Create a corrected production DEFENSE / HIT / DEFEAT source sheet for WORLD WARRIOR GRAND GRAPPLER TETSU.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the exact approved Tetsu identity authority. Preserve his smooth bald head, stern square face, two blunt charcoal beard braids with saffron bands, deep warm-brown skin, massive rectangular anatomy, vermilion and warm-ivory high-collar vest, deep-indigo waist wrap, charcoal shorts with ivory side blocks, plum forearm wraps, charcoal calf wraps, saffron ankle cords, ivory-toed indigo shoes, and square bronze left-hip seal in every pose.
2. Reference 2 is the exact 4-columns-by-2-rows choreography/layout authority. Copy its eight defense, hit, fall, recovery, knockdown, and defeat pose roles, never its mannequin identity.
3. Reference 3 is the rejected Tetsu v1 misc sheet and supplies rendering continuity plus the surviving pose ideas only. It has only SEVEN figures because its top row omits the fourth backward-stagger beat. Do not copy its missing slot, oversized spacing, or seven-figure layout.
4. Reference 4 is Tournament Grappler for shared heavyweight World Warrior cloth grammar only. Do not copy his hair, face, diagonal tunic, trousers, round token, or identity.
5. Reference 5 is Pavilion Ace Makoto for named-elite finish quality only. Do not copy her body, braid, costume, or identity.
6-7. References 6-7 are canonical rendering-finish anchors only. Match controlled dark contours, broad two-to-four-band cel shading, clean designed highlights, and arcade-readable anatomy without copying either identity.

Produce EXACTLY EIGHT complete, separate, fully colored Tetsu figures in one square 4 columns x 2 rows sheet, ordered left-to-right across the top row then left-to-right across the bottom row.

TOP ROW MUST CONTAIN FOUR FIGURES:
1. Broad crossed-forearm standing guard, feet wide and both shoes planted.
2. Deep guard compression, elbows and knees visibly yielding while remaining upright.
3. Clear chest-hit recoil, torso jolting back, arms separating, both feet still visible.
4. Distinct off-balance backward stagger one beat later, torso farther back, one foot sliding or lifting, arms spread for balance. Pose 4 must not duplicate pose 3 and must remain fully visible.

BOTTOM ROW MUST CONTAIN FOUR FIGURES:
5. Broad backward fall in progress, complete body and all limbs visible.
6. One-knee recovery with one open hand planted and head raised.
7. Complete side-prone knockdown with face, both beard braids, square hip seal, both legs, and both shoes visible.
8. Final defeated side-prone pose, lower and more settled than pose 7, with the complete body visible.

LAYOUT IS ABSOLUTE. Use one consistent smaller figure scale. Every figure must fit independently inside its implied quarter-width cell with pure green between figures. Preserve at least 48 pixels of green around each complete standing/falling figure and at least 24 pixels around each complete prone figure. No body part may touch a canvas edge. The top row must visibly contain four separate figures, not three.

Uniform pure chroma-green RGB 0,255,0 background. No attacker, blood, wound, injury detail, impact effect, dust, floor, shadow, scene, text, labels, grid, divider, black silhouette, placeholder, missing beard braid, missing square seal, extra limb, touching figures, duplicate pose, or crop. 2K square animation source.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP tetsu_misc_v2'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @($identity, $choreography, $failedMisc, $grappler, $makoto, $ryu, $goku)
    foreach ($required in $references) {
        if (-not (Test-Path $required -PathType Leaf)) {
            throw "Missing generation reference: $required"
        }
    }

    $arguments = @('generate', 'create', 'nano_banana_pro', '--prompt', $prompt)
    foreach ($reference in $references) {
        $arguments += @('--image', $reference)
    }
    $arguments += @('--aspect_ratio', '1:1', '--resolution', '2k', '--wait', '--wait-timeout', '20m', '--json')

    Write-Host 'GENERATE tetsu_misc_v2'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for tetsu_misc_v2 after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed for tetsu_misc_v2; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for tetsu_misc_v2.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}