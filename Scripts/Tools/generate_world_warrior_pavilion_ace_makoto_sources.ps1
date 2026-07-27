param(
    [switch]$SkipExisting,
    [switch]$GenerateSources
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
$pilot = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_style_pilot_v1.png'
$striker = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_style_pilot_v1.png'
$kenzo = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_dojo_prodigy_kenzo_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_pavilion_ace_makoto_pilot_v1_job.json'

$identityCore = @'
The WORLD WARRIOR PAVILION ACE MAKOTO is one exact original adult woman: tall lean athletic precision-kick build with powerful calves and compact shoulders; warm medium skin; sharp composed face; black hair braided into one huge high looped crest arcing backward with restrained saffron bands; one short asymmetrical vermilion pavilion mantle fastened across the upper left shoulder; fitted sleeveless deep-indigo wrap top with one warm-ivory diagonal chest panel and narrow saffron piping; charcoal fitted kick trousers; long narrow deep-indigo split pavilion panels hanging at the front and sides while leaving both legs clearly visible; wide plum hand wraps; warm-ivory shin guards with one vermilion chevron each; low ivory split-sole kicking shoes; and one bronze crescent champion token fixed at the right hip. Preserve her exact face, braid geometry, female proportions, costume geometry, colors, light direction, and right-facing three-quarter camera in every pose.
'@
$forbid = @'
Every figure must be complete and separated on uniform pure chroma-green RGB 0,255,0. No rust hair, undercut, male body, raised-knee copy of the Striker pilot, white karate gi, red headband, bare feet, tracksuit, long coat, long sash, hakama, military or tactical gear, armor, cargo pockets, dress that fuses the legs, photorealism, PBR, painterly blur, flat vector art, pixel art, weapon, aura, impact effect, target, shadow, floor, scene, text, labels, grid lines, border, touching figures, extra limbs, missing braid, missing mantle, missing crescent token, missing shin guards, duplicate pose, or crop.
'@

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-body IDENTITY PILOT of WORLD WARRIOR PAVILION ACE MAKOTO, a single original adult woman standing in a calm right-facing three-quarter precision-kick guard with both feet grounded, rear heel slightly raised, hands poised but relaxed, and her full body visible.

Makoto is the decorated elite counterpart to the Pavilion Striker, but she must be a distinct champion rather than a recolor or gender swap. Preserve this identity: tall lean athletic kick-specialist build with powerful calves and compact shoulders; warm medium skin; sharp composed face; black hair swept into one high braided crest that arcs backward and ends above the shoulder blades, threaded with two restrained saffron bands; short asymmetrical vermilion pavilion mantle fastened only over the left shoulder and ending above the waist; fitted sleeveless deep-indigo wrap top with one warm-ivory diagonal chest panel and narrow saffron piping; charcoal fitted kick trousers beneath a two-panel deep-indigo split overskirt that ends at mid-thigh and opens clearly around both legs; wide plum hand wraps; warm-ivory shin guards with one simple vermilion chevron each; low ivory split-sole kicking shoes; and one bronze crescent champion token fixed at the right hip. The high braid, one-shoulder short mantle, split overskirt, ivory shin guards, and crescent hip token are mandatory silhouette anchors.

Makoto's fighting identity is exact, economical precision kicking: upright balanced posture, long clean leg lines, no open-palm master pose, no wrestling crouch, no lifted-knee copy of the Striker pilot, and no exaggerated dancer pose. She is visibly senior to the base Striker through ceremonial asymmetry and controlled bearing while remaining a practical fast fighter.

The first supplied image is the approved Pavilion Striker. Use it only for shared World Warrior material grammar and the vermilion/deep-indigo/saffron/warm-ivory family; do not copy his rust hair, undercut, sleeveless long jacket, male body, raised-knee pose, round hanging token, trousers, or costume geometry. The second supplied image is the approved Dojo Prodigy Kenzo. Use it only as the named-elite quality bar; do not copy his circlet, medallion, long half-sash, hakama, open-palm pose, face, or identity. The final three images are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, simplified cloth materials, designed highlights, saturated controlled accents, and arcade-readable fighting-game anatomy. Do not copy any reference character's face, hair, costume, or identity.

One complete centered figure on uniform pure chroma-green RGB 0,255,0 with generous green margin on every side, including above the braid and below both shoes. No white karate gi, red headband, bare feet, tracksuit, long coat, long sash, hakama, military or tactical gear, armor, cargo pockets, skirt that hides either leg, photorealism, PBR, painterly blur, flat vector art, pixel art, weapon, aura, impact effect, target, shadow, floor, scene, text, labels, grid lines, border, extra limbs, missing costume anchors, or crop. 2K square identity pilot.
'@

function Invoke-MakotoImage {
    param(
        [string]$Name,
        [string]$Prompt,
        [string]$Output,
        [string]$Metadata,
        [string[]]$References
    )

    if ($SkipExisting -and (Test-Path $Output) -and (Test-Path $Metadata)) {
        Write-Host "SKIP $Name"
        return
    }

    foreach ($required in $References) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
        }
    }

    Write-Host "GENERATE $Name"
    $arguments = @('generate', 'create', 'nano_banana_pro', '--prompt', $Prompt)
    foreach ($reference in $References) {
        $arguments += @('--image', $reference)
    }
    $arguments += @('--aspect_ratio', '1:1', '--resolution', '2k', '--wait', '--wait-timeout', '20m', '--json')
    $raw = & $cli @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Higgsfield failed for $Name."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $Metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw "Higgsfield returned no result URL for $Name."
    }

    Invoke-WebRequest -Uri $url -OutFile $Output
    Write-Host "SAVED $Output"
}

function Invoke-MakotoSources {
    $identityLock = @"
PROJECT MANNEQUIN STYLE LOCK. The first supplied image is the exact approved WORLD WARRIOR PAVILION ACE MAKOTO identity. $identityCore The second image is choreography/layout reference only; copy its timing and body flow, not its identity. The third image is the approved Pavilion Striker for shared World Warrior material grammar only. The fourth image is approved Dojo Prodigy Kenzo for named-elite finish only. The final three images are rendering-finish references only. Do not copy any reference character's face, hair, costume, body, or identity.

$forbid
"@

    $jobs = @(
        [pscustomobject]@{
            Name = 'idle'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_idle_sheet_v1.png'
            Prompt = @'
Produce exactly FOUR full-body IDLE poses in a clean 2 columns by 2 rows. All face right and use one consistent foot baseline. Ordered left-to-right, top then bottom: 1 upright narrow precision-kick guard with rear heel slightly raised; 2 subtle inhale with shoulders settling; 3 small weight transfer onto the front foot while hands remain compact; 4 controlled return to the original guard. The high braided crest and split pavilion panels sway only slightly. Large green margins around every figure. No lifted-knee pose or attack. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name = 'walk'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v1.png'
            Prompt = @'
Produce exactly EIGHT full-body measured WALK poses in a clean 4 columns by 2 rows. All face right. Ordered left-to-right, top then bottom: 1 left-foot contact; 2 compression; 3 passing; 4 high point; 5 right-foot contact; 6 compression; 7 passing; 8 high point. Use an upright economical champion gait, compact arm carriage, and clearly alternating planted feet. The braided crest and split panels counter-swing naturally without hiding either leg. Same scale and baseline with large green separation. No run, glide, attack, or duplicate pose. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name = 'dash'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_dash_sheet_v1.png'
            Prompt = @'
Produce exactly SIX full-body forward DASH poses in a clean 3 columns by 2 rows. All face right. Ordered sequence: 1 compact upright coil; 2 sharp first step; 3 forward acceleration; 4 longest low sprint stride; 5 precise braking step; 6 return toward narrow kick guard. Preserve the complete braid, mantle, crescent token, shin guards, both shoes, and separated legs in every pose. Same scale with large green separation. No speed effects, motion blur, or attack. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name = 'jump'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_jump_sheet_v1.png'
            Prompt = @'
Produce exactly FOUR full-body agile JUMP poses in a clean 2 columns by 2 rows. All face right. Ordered sequence: 1 compact takeoff crouch; 2 rising pose with knees controlled; 3 balanced apex with both legs tucked separately; 4 precise landing compression. Every pose must include the entire high braid, hands, mantle, split panels, crescent token, shin guards, and shoes with generous green around it. Same scale and identity. No kick, aura, dust, floor, or crop. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name = 'attack_startup'
            Choreography = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_attack_startup_sheet_style_v1.png'
            Prompt = @'
Produce exactly FIVE distinct full-body CRESCENT HEEL startup/active poses arranged as three figures on the top row and two on the bottom row, ordered left-to-right then top-to-bottom. All remain in the same right-facing three-quarter side camera: 1 narrow precision-kick guard; 2 weight settles onto the left support leg and right knee chambers inward; 3 right knee rises diagonally across the body while torso stays tall; 4 right lower leg begins opening upward and outward in a steep crescent arc; 5 strongest active silhouette, a very high right outside-crescent/hook heel kick sweeping down toward the right at head height, support foot planted, kicking leg long but slightly curved, heel leading, toes pulled back, torso upright, and both hands protecting the chest. Pose 5 must read instantly as a high arcing heel kick, not a straight horizontal side kick. The braid and split panels follow the arc without covering either leg. Maintain one consistent character scale and generous green margin. No Striker Turning Kick copy, no horizontal straight kick, no lifted-knee-only final pose, no back-facing turn, no spin with hidden face, no recovery pose, target, impact, speed line, or motion blur. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name = 'attack_recovery'
            Choreography = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_attack_recovery_sheet_style_v1.png'
            Prompt = @'
Produce exactly FIVE distinct full-body CRESCENT HEEL recovery poses arranged as three figures on the top row and two on the bottom row, ordered left-to-right then top-to-bottom. Continue from Makoto's high right outside-crescent heel kick in one right-facing three-quarter camera: 1 heel has just passed the high apex and the right knee begins folding; 2 right leg retracts across the body while torso remains upright; 3 right foot descends beside the support leg; 4 controlled staggered landing with hands returning to guard; 5 original narrow precision-kick guard restored. Preserve the full braid, mantle, crescent token, split panels, shin guards, and both shoes in every pose. Maintain one consistent scale and generous green margin. No new attack, straight horizontal kick, back-facing turn, target, impact, effect, or crop. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name = 'misc'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_misc_sheet_v1.png'
            Prompt = @'
Produce exactly EIGHT fully colored DEFENSE / HIT / DEFEAT poses in a clean 4 columns by 2 rows. All preserve the exact Makoto identity. Top row: 1 compact crossed-forearm guard; 2 guard compression; 3 chest-hit recoil; 4 off-balance backward stagger. Bottom row: 5 backward fall fully visible; 6 one-knee recovery; 7 complete side-prone knockdown with face, braid, mantle, crescent token, and both legs visible; 8 final defeated side-prone pose. Large green separation and consistent scale. No attacker, blood, injury detail, dust, impact effect, black silhouette, placeholder, or cropped prone body. 2K square animation source.
'@
        }
    )

    foreach ($job in $jobs) {
        $output = "Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_$($job.Name)_sheet_style_v1.png"
        $jobMetadata = "Artifacts/style_calibration_world_warrior_pavilion_ace_makoto_$($job.Name)_sheet_v1_job.json"
        Invoke-MakotoImage `
            -Name $job.Name `
            -Prompt ($identityLock + "`n" + $job.Prompt) `
            -Output $output `
            -Metadata $jobMetadata `
            -References @($pilot, $job.Choreography, $striker, $kenzo, $mannequin, $ryu, $goku)
    }
}

Push-Location $projectRoot
try {
    if ($GenerateSources) {
        Invoke-MakotoSources
        return
    }

    if ($SkipExisting -and (Test-Path $pilot) -and (Test-Path $metadata)) {
        Write-Host 'SKIP makoto_pilot_v1'
        return
    }

    foreach ($required in @($striker, $kenzo, $mannequin, $ryu, $goku)) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
        }
    }

    Write-Host 'GENERATE makoto_pilot_v1'
    $raw = & $cli generate create nano_banana_pro `
        --prompt $prompt `
        --image $striker `
        --image $kenzo `
        --image $mannequin `
        --image $ryu `
        --image $goku `
        --aspect_ratio '1:1' `
        --resolution '2k' `
        --wait `
        --wait-timeout '20m' `
        --json
    if ($LASTEXITCODE -ne 0) {
        throw 'Higgsfield failed for makoto_pilot_v1.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for makoto_pilot_v1.'
    }

    Invoke-WebRequest -Uri $url -OutFile $pilot
    Write-Host "SAVED $pilot"
}
finally {
    Pop-Location
}