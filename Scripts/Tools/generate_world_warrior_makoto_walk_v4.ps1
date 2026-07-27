param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_style_pilot_v1.png'
$choreography = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_walk_sheet_style_v2.png'
$guide = 'Artifacts/mannequin_walk_choreography_guide.png'
$failedV2 = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_walk_sheet_style_v2.png'
$striker = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_style_pilot_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_walk_sheet_style_v4.png'
$metadata = 'Artifacts/style_calibration_world_warrior_pavilion_ace_makoto_walk_sheet_v4_job.json'

$prompt = @'
Create one production 4-columns-by-2-rows WALK-CYCLE SOURCE SHEET for WORLD WARRIOR PAVILION ACE MAKOTO.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the exact Makoto identity authority. Preserve this exact adult woman in all eight poses: warm medium skin, sharp composed face, huge high looped black braided crest with saffron bands, short asymmetrical vermilion pavilion mantle, fitted deep-indigo wrap top with warm-ivory diagonal chest panel, charcoal kick trousers, long split indigo pavilion panels, plum hand wraps, warm-ivory shin guards with vermilion chevrons, ivory split-sole shoes, and bronze crescent token at the right hip. Preserve her female proportions, costume asymmetry, face, braid, palette, materials, contour weight, and light.
2. Reference 2 is the exact WALK CHOREOGRAPHY and SHEET-LAYOUT authority. Reproduce its eight row-major lower-body silhouettes, alternating contacts, down poses, passing poses, stylized high points, opposing arm swings, spacing, right-facing camera, and shared baseline. Replace the male Striker identity completely; do not copy his hair, face, male anatomy, vest, token, wraps, or costume.
3. Reference 3 is an anatomical timing map. Follow its A-contact, A-down, A-passing, A-up, B-contact, B-down, B-passing, B-up order. Do not render guide colors, labels, circles, lines, or white background.
4. Reference 4 is a rejected Makoto sheet used only for rendering continuity. Do not copy its duplicated bottom row, same-leg lead, idle-like poses, or weak gait.
5 is the Striker identity only so it can be explicitly excluded. 6 and 7 supply only canonical Project Mannequin rendering finish.

Produce EXACTLY EIGHT separate complete full-body Makoto figures in one square 4x2 sheet, read left-to-right across the top row then the bottom row. Match reference 2 pose-for-pose. The bottom row must not duplicate the top row: it must visibly use the physically opposite lead/support leg and reversed arm swing. At least six materially different lower-body silhouettes.

Keep camera-near physical leg A one cel-value band lighter than camera-far physical leg B through the entire hip-to-shoe chain, and keep those values attached to the same limbs across all eight frames. Preserve Makoto's crescent token on the right hip and mantle on the same shoulder; never mirror her identity. Long split panels may flow but cannot hide the knees, ankles, or lead-leg swap.

One consistent body scale, head size, light direction, and foot baseline. Every figure independently contained with generous green margin. Exactly two arms, two legs, two feet, complete braid, mantle, token, shin guards, and shoes in every frame. Natural grounded walk only: no idle, shuffle, run, jump, attack, guard, or repeated raised-knee pose.

Uniform pure chroma-green RGB 0,255,0 background. No floor, cast shadow, scenery, grid, dividers, border, text, labels, numbers, arrows, guide marks, motion effects, extra figure, extra limb, missing limb, merged legs, touching figures, or crop. PROJECT MANNEQUIN STYLE LOCK: controlled dark contours, broad two-to-four-step cel shading, clean graphic highlights, arcade-readable fighting-game anatomy, and polished modern 2.5D anime finish. No photorealism, PBR, grit, painterly blur, flat vector art, or pixel art. 2K square source.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP makoto_walk_v4'
        return
    }

    foreach ($required in @($identity, $choreography, $guide, $failedV2, $striker, $ryu, $goku)) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
        }
    }

    $arguments = @('generate', 'create', 'nano_banana_pro', '--prompt', $prompt)
    foreach ($reference in @($identity, $choreography, $guide, $failedV2, $striker, $ryu, $goku)) {
        $arguments += @('--image', (Resolve-Path $reference).Path)
    }
    $arguments += @('--aspect_ratio', '1:1', '--resolution', '2k', '--wait', '--wait-timeout', '20m', '--json')
    $raw = & $cli @arguments
    if ($LASTEXITCODE -ne 0) {
        throw 'Higgsfield failed for Makoto walk v4.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for Makoto walk v4.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}