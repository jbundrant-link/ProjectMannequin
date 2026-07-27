param(
    [switch]$SkipExisting,
    [ValidateSet('idle', 'walk', 'dash', 'jump', 'attack_startup', 'attack_recovery', 'misc')]
    [string[]]$Family,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_style_pilot_v3_normalized.png'
$grappler = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
$makoto = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$identityCore = @'
The WORLD WARRIOR GRAND GRAPPLER TETSU is one exact original older adult man: very tall monolithic heavyweight with a broad rectangular torso, thick waist, massive rounded shoulders, huge forearms, pillar-like thighs and calves, and deep warm-brown skin; stern calm square face with broad nose; smooth shaved head; enormous dense charcoal beard divided into two short blunt square braids capped with restrained saffron bands; boxy sleeveless vermilion champion vest with high deep-indigo stand collar, closed front, broad warm-ivory vertical side panels, and narrow saffron piping; very wide deep-indigo ribbed waist wrap; charcoal knee-length grappling shorts with separate warm-ivory rectangular side blocks; wide plum forearm wraps with bare hands; charcoal calf wraps with narrow saffron ankle cords; low deep-indigo split-sole wrestling shoes with warm-ivory toe caps; and one large flat bronze square champion seal fixed off-center at the left hip with a vermilion diamond inset. Preserve his exact face, bald head, twin beard geometry, massive rectangular proportions, costume geometry, colors, light direction, and right-facing three-quarter camera in every pose.
'@
$forbid = @'
Every figure must be complete and separated on uniform pure chroma-green RGB 0,255,0. No scalp hair, curls, silver forelock, clean-shaven face, diagonal plum wrap tunic, centered round token, brick-red loose trousers, low shoulder-entry copy of Tournament Grappler, white karate gi, red headband, bare feet, long coat, cape, mantle, long sash, hakama, skirt, wrestling singlet, championship belt, tactical or military gear, armor, cargo pockets, knee pads, combat boots, photorealism, PBR, painterly blur, flat vector art, pixel art, weapon, chain, aura, impact effect, target, shadow, floor, scene, text, labels, grid lines, border, touching figures, extra limbs, missing beard braid, missing square seal, duplicate pose, or crop.
'@
$identityLock = @"
PROJECT MANNEQUIN STYLE LOCK. The first supplied image is the exact approved WORLD WARRIOR GRAND GRAPPLER TETSU identity and absolute authority. $identityCore The second supplied image is choreography/layout reference only; copy its requested timing and body flow, never its identity. The third image is the approved Tournament Grappler for shared World Warrior heavyweight cloth/material grammar only; do not copy his face, hair, diagonal tunic, trousers, low shoulder posture, centered round token, or identity. The fourth image is approved Pavilion Ace Makoto for named-elite finish quality only; do not copy her face, braid, mantle, panels, body, stance, or identity. The final three images are canonical rendering-finish anchors only. Match controlled dark contours, broad two-to-four-band cel shading, simplified cloth materials, designed highlights, saturated controlled accents, and arcade-readable fighting-game anatomy. Do not copy any reference character's face, hair, costume, body, or identity.

$forbid
"@

$jobs = @(
    [pscustomobject]@{
        Name = 'idle'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_idle_sheet_v1.png'
        Prompt = @'
Produce exactly FOUR full-body IDLE poses in a clean 2 columns by 2 rows. All face right and use one consistent foot baseline. Ordered left-to-right, top then bottom: 1 upright extra-wide open-hand clinch guard; 2 subtle inhale as the chest rises and both beard braids lift slightly; 3 small weight transfer onto the front foot while elbows remain broad; 4 controlled exhale returning to the original guard. Keep both feet grounded, torso tall, hands open, and square hip seal visible. Large green margins around every figure. No attack, low shoulder lunge, or closed fist. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'walk'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v1.png'
        Prompt = @'
Produce exactly EIGHT full-body heavy WALK poses in a clean 4 columns by 2 rows. All face right. Ordered left-to-right, top then bottom: 1 left-foot heel contact; 2 left-leg down/compression; 3 left leg planted while right leg passes; 4 left-leg high point and right leg advances; 5 right-foot heel contact; 6 right-leg down/compression; 7 right leg planted while left leg passes; 8 right-leg high point and left leg advances. Use slow powerful alternating steps, upright rectangular torso, broad open-hand carriage, clearly different planted feet, and visible separation between both legs. Twin beard braids and square hip seal counter-swing slightly without changing sides. Same scale and baseline with large green separation. No shuffle, glide, march, run, attack, repeated lead leg, or duplicate pose. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'dash'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_dash_sheet_v1.png'
        Prompt = @'
Produce exactly SIX full-body forward DASH poses in a clean 3 columns by 2 rows. All face right. Ordered sequence: 1 upright wide preload with open hands; 2 powerful first step; 3 heavy forward acceleration while torso stays tall; 4 longest driving stride with both arms ready to clinch; 5 broad braking step; 6 return to upright extra-wide guard. Preserve the bald head, two beard braids, both open hands, square seal, vest panels, shorts, calf wraps, and both shoes in every pose. Same scale with large green separation. No low shoulder tackle, strike, target, speed effect, or motion blur. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'jump'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_jump_sheet_v1.png'
        Prompt = @'
Produce exactly FOUR full-body HEAVY JUMP poses in a clean 2 columns by 2 rows. All face right. Ordered sequence: 1 deep but upright takeoff crouch; 2 powerful rise with knees bent separately; 3 compact apex with massive body tucked and both hands open; 4 broad controlled landing compression. Every pose includes the complete bald head, twin beard braids, hands, square hip seal, vest, shorts, calf wraps, and shoes with generous green around it. Same scale and identity. No aerial attack, aura, dust, floor, or crop. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'attack_startup'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_attacks_sheet_v1.png'
        Prompt = @'
Produce exactly FIVE distinct full-body IRON GATE CLINCH startup/active poses arranged as three figures on the top row and two on the bottom row, ordered left-to-right then top-to-bottom. All remain in the same right-facing three-quarter camera: 1 upright extra-wide open-hand guard; 2 lead foot makes one broad forward step as both elbows open; 3 both arms spread chest-high like two heavy gates around an imaginary opponent while torso remains tall; 4 hips settle and both forearms begin sweeping inward in a square body-lock arc; 5 strongest active silhouette, an upright crushing clinch entry with both open arms visibly curved inward around an imaginary torso at chest/rib height, elbows behind the hands, chest advanced, knees flexed, both feet planted wide, and head above the imaginary opponent's shoulder line. Pose 5 must read instantly as a two-arm standing body lock, not a punch, bear roar, low shoulder tackle, or one-arm grab. Keep both hands visible and open, both beard braids separate, and the square hip seal unobscured. Maintain one consistent scale and generous green margin. No Tournament Grappler Shoulder Drive copy, low lunge, closed fist, target figure, contact, impact, speed line, motion blur, recovery pose, or back-facing turn. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'attack_recovery'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_attacks_sheet_v1.png'
        Prompt = @'
Produce exactly FIVE distinct full-body IRON GATE CLINCH recovery poses arranged as three figures on the top row and two on the bottom row, ordered left-to-right then top-to-bottom. Continue from Tetsu's upright two-arm body-lock entry in one right-facing three-quarter camera: 1 the inward clinch pressure ends with both forearms still curved; 2 both open hands release outward; 3 lead foot slides back as elbows lower; 4 torso settles upright and hands return to broad guard; 5 original extra-wide open-hand clinch guard restored. Preserve the exact bald head, twin beard braids, vest, square seal, shorts, wraps, and both shoes in every pose. Maintain one consistent scale and generous green margin. No new attack, low shoulder tackle, fist, target, throw victim, impact, effect, or crop. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'misc'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_misc_sheet_v1.png'
        Prompt = @'
Produce exactly EIGHT fully colored DEFENSE / HIT / DEFEAT poses in a clean 4 columns by 2 rows. All preserve the exact Tetsu identity. Top row: 1 broad crossed-forearm guard; 2 deep guard compression; 3 torso-hit recoil; 4 heavyweight backward stagger. Bottom row: 5 broad backward fall fully visible; 6 one-knee recovery with one open hand planted; 7 complete side-prone knockdown with bald head, twin beard braids, square seal, and both legs visible; 8 final defeated side-prone pose. Large green separation and consistent scale. No attacker, blood, injury detail, dust, impact effect, black silhouette, placeholder, or cropped prone body. 2K square animation source.
'@
    }
)

Push-Location $projectRoot
try {
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    foreach ($job in $jobs) {
        if ($Family.Count -gt 0 -and $job.Name -notin $Family) {
            Write-Host "SKIP_UNSELECTED $($job.Name)"
            continue
        }

        $output = "Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_grappler_tetsu_$($job.Name)_sheet_style_v1.png"
        $metadata = "Artifacts/style_calibration_world_warrior_grand_grappler_tetsu_$($job.Name)_sheet_v1_job.json"
        if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
            Write-Host "SKIP $($job.Name)"
            continue
        }

        $references = @($identity, $job.Choreography, $grappler, $makoto, $mannequin, $ryu, $goku)
        foreach ($required in $references) {
            if (-not (Test-Path $required -PathType Leaf)) {
                throw "Missing generation reference: $required"
            }
        }

        Write-Host "GENERATE $($job.Name)"
        $arguments = @(
            'generate', 'create', 'nano_banana_pro',
            '--prompt', ($identityLock + "`n" + $job.Prompt)
        )
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

        $raw = $null
        for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
            $raw = & $cli @arguments
            if ($LASTEXITCODE -eq 0) {
                break
            }

            if ($attempt -eq $RequestAttempts) {
                throw "Higgsfield failed for $($job.Name) after $RequestAttempts attempts."
            }
            Write-Warning "Higgsfield transport failed for $($job.Name); retrying attempt $($attempt + 1)/$RequestAttempts."
        }

        $result = ($raw -join "`n") | ConvertFrom-Json
        $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
        $url = @($result)[0].result_url
        if ([string]::IsNullOrWhiteSpace($url)) {
            throw "Higgsfield returned no result URL for $($job.Name)."
        }

        Invoke-WebRequest -Uri $url -OutFile $output
        Write-Host "SAVED $output"
    }
}
finally {
    Pop-Location
}