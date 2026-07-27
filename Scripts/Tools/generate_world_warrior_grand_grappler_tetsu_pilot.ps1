param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$pilot = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_style_pilot_v3.png'
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_style_pilot_v2.png'
$grappler = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_grand_grappler_tetsu_pilot_v3_job.json'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. REFRAME the exact WORLD WARRIOR GRAND GRAPPLER TETSU from the first supplied image as one approved-quality full-body identity pilot on pure chroma green. This is a composition and background correction only, not a redesign. Preserve the first image's exact face, bald head, twin beard braids, deep warm-brown skin, massive rectangular anatomy, upright right-facing three-quarter clinch stance, open hands, boxy vermilion vest, ivory side panels, indigo waist wrap, charcoal shorts, plum forearm wraps, calf wraps, saffron ankle cords, ivory-toed shoes, square bronze left-hip seal, colors, contours, cel shading, and light direction. Do not alter, simplify, embellish, or reinterpret the character.

Tetsu is a monolithic veteran grappling champion: very tall, extremely broad rectangular torso, thick waist, massive rounded shoulders, huge forearms, pillar-like thighs and calves, deep warm-brown skin, stern calm square face, broad nose, smooth shaved head, and one enormous dense charcoal beard divided into TWO short blunt square braids capped with restrained saffron bands. No scalp hair, forelock, moustache-only face, scars, or angry screaming expression. The bald crown and twin square beard braids are mandatory head-and-silhouette anchors.

His costume is ceremonial tournament cloth, never armor: a boxy sleeveless vermilion champion vest with a high deep-indigo stand collar, closed front, broad warm-ivory vertical side panels, and narrow saffron edge piping; a very wide deep-indigo ribbed waist wrap; charcoal knee-length grappling shorts with separate warm-ivory rectangular side blocks and clear space between both legs; wide plum forearm wraps leaving both hands bare; narrow saffron ankle cords over charcoal calf wraps; low deep-indigo split-sole wrestling shoes with warm-ivory toe caps; and one large flat BRONZE SQUARE champion seal fixed off-center at the LEFT hip with a simple vermilion diamond inset. The boxy vest, twin beard braids, pillar shorts silhouette, ivory toe caps, and offset square seal are mandatory identity anchors.

Tetsu is the elite counterpart to the Tournament Grappler, but must be a separate champion rather than a recolor, older version, or costume variant. His fighting identity is upright immovable clinch control, not the base Grappler's low forward-pitched shoulder-entry pose. Keep his torso rectangular rather than tapered, his head bald rather than curly, and his hands open rather than clenched.

The first supplied image is the absolute Tetsu identity and pose authority. The second supplied image is the approved Tournament Grappler; use it ONLY to retain shared World Warrior cloth material grammar. Do not copy his short black curls, silver forelock, clean-shaven face, diagonal plum wrap tunic, centered round belt token, brick-red loose trousers, low forward lean, face, or exact proportions. The final three supplied images are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Retain their confident dark contours, broad two-to-four-band cel shading, simplified cloth materials, designed highlights, saturated controlled accents, and arcade-readable fighting-game anatomy. Do not copy any reference character's face, hair, costume, body, or identity.

CAMERA FRAMING IS MANDATORY: zoom out and make the complete figure occupy only about 72 to 78 percent of the square canvas height and no more than 70 percent of its width. Preserve at least 160 pixels of uninterrupted pure green above the bald head, below both shoes, and outside the leftmost and rightmost body points. Do not enlarge the body to fill the frame.

One complete centered figure on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side. No white karate gi, red headband, bare feet, diagonal wrap tunic, centered round belt token, brick-red baggy trousers, long coat, cape, mantle, long sash, hakama, skirt, singlet, championship belt, tactical or military gear, armor, cargo pockets, knee pads, combat boots, photorealism, PBR, painterly blur, flat vector art, pixel art, weapon, chain, aura, impact effect, target, shadow, floor, scene, text, labels, grid lines, border, extra limbs, missing beard braid, missing square seal, closed fist, duplicate figure, tight framing, or crop. 2K square identity pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $pilot) -and (Test-Path $metadata)) {
        Write-Host 'SKIP tetsu_pilot_v3'
        return
    }

    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    foreach ($required in @($identity, $grappler, $mannequin, $ryu, $goku)) {
        if (-not (Test-Path $required -PathType Leaf)) {
            throw "Missing generation reference: $required"
        }
    }

    Write-Host 'GENERATE tetsu_pilot_v3'
    $raw = & $cli generate create nano_banana_pro `
        --prompt $prompt `
        --image $identity `
        --image $grappler `
        --image $mannequin `
        --image $ryu `
        --image $goku `
        --aspect_ratio '1:1' `
        --resolution '2k' `
        --wait `
        --wait-timeout '20m' `
        --json
    if ($LASTEXITCODE -ne 0) {
        throw 'Higgsfield failed for tetsu_pilot_v3.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for tetsu_pilot_v3.'
    }

    Invoke-WebRequest -Uri $url -OutFile $pilot
    Write-Host "SAVED $pilot"
}
finally {
    Pop-Location
}