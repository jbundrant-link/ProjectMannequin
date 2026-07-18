param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = 'C:\Users\Joseph Bundrant\AppData\Local\Programs\higgsfield\higgsfield.exe'
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$identityLock = @'
PROJECT MANNEQUIN STYLE LOCK. The first supplied image is the exact approved WORLD WARRIOR DOJO ROOKIE identity. Preserve the same original young adult fighter in every pose: lean compact rushdown build, short wavy black hair gathered at the nape by one bronze clasp, sleeveless deep-indigo crossover vest, asymmetrical saffron placket, brick-red forearm wraps, charcoal tapered trousers with one muted vermilion side panel, short saffron sash tab at the left hip, round lacquer waist token, and low ivory split-sole shoes. Keep the same face, body proportions, costume geometry, color hierarchy, camera, and light. The second image is choreography/layout reference only. The final three images are rendering-finish references only: controlled dark contours, broad two-to-four-band cel shading, simplified cloth materials, designed highlights, and arcade-readable anatomy. Do not copy any reference character's face, hair, costume, or identity.

Every figure must be complete and separated on uniform pure chroma-green RGB 0,255,0. No white karate gi, red headband, bare feet, tracksuit, military or tactical gear, armor, cargo pockets, photorealism, PBR, painterly blur, flat vector art, pixel art, weapon, aura, impact effect, target, shadow, floor, scene, text, labels, grid lines, border, touching figures, extra limbs, missing costume pieces, duplicate pose, or crop.
'@

$jobs = @(
    [pscustomobject]@{
        Name = 'idle'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_idle_sheet_v1.png'
        Prompt = @'
Produce exactly FOUR full-body IDLE poses in a clean 2 columns by 2 rows. All face right and use one consistent foot baseline. Ordered left-to-right, top then bottom: 1 forward-coiled open-palm guard with weight on rear leg; 2 subtle inhale with lead palm raised; 3 slight front-foot weight shift and alert head turn; 4 subtle exhale returning to the original guard. Keep the short sash tab visible but restrained. Large green margins around every figure. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'walk'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v1.png'
        Prompt = @'
Produce exactly EIGHT full-body quick WALK poses in a clean 4 columns by 2 rows. All face right. Sequence left-to-right, top then bottom: 1 left-foot contact; 2 compression; 3 passing; 4 high point; 5 right-foot contact; 6 compression; 7 passing; 8 high point. Use a light forward lean, compact arm swing, open lead hand, and a short sash tab trailing opposite the step. Same scale and baseline in all poses with large green separation. No run or attack. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'dash'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_dash_sheet_v1.png'
        Prompt = @'
Produce exactly SIX full-body forward DASH poses in a clean 3 columns by 2 rows. All face right. Ordered sequence: 1 low coil; 2 explosive first step; 3 forward acceleration; 4 longest low sprint stride; 5 braking step; 6 quick return toward open-palm guard. Preserve both shoes, both hands, waist token, and short sash in every pose. Same scale with large green separation. No speed effects or motion blur. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'jump'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_jump_sheet_v1.png'
        Prompt = @'
Produce exactly FOUR full-body agile JUMP poses in a clean 2 columns by 2 rows. All face right. Ordered sequence: 1 compact takeoff crouch; 2 rising pose with lead knee lifted; 3 controlled apex with limbs tucked and open lead hand; 4 soft landing compression. Each pose must include the complete head, hands, sash, token, and shoes with generous green around it. Same scale and identity. No aura, dust, or floor. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'attack_startup'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_attacks_sheet_v1.png'
        Prompt = @'
Produce exactly FIVE full-body QUICK PALM startup/active poses arranged as 3 figures on the top row and 2 on the bottom row. All face right. Ordered poses: 1 coiled open-palm guard; 2 lead shoulder rotates; 3 quick front-foot step and palm chamber; 4 lead palm nearly extended; 5 strongest fully extended straight open-palm strike active pose. Keep the rear fist compact at the ribs, both shoes grounded, and the active hand clearly open. Large green separation and one consistent scale. No punch, kick, recovery pose, impact, target, or speed line. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'attack_recovery'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_attacks_sheet_v1.png'
        Prompt = @'
Produce exactly FIVE full-body QUICK PALM recovery poses arranged as 3 figures on the top row and 2 on the bottom row. All face right. Continue after a fully extended straight open-palm strike: 1 palm begins to snap back; 2 rear foot catches balance; 3 torso unwinds and lead elbow tucks; 4 open-palm guard returns; 5 neutral forward-coiled guard. Keep both shoes, both hands, token, and sash visible with one consistent scale and large green separation. No startup, new attack, impact, target, or effect. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'misc'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_misc_sheet_v1.png'
        Prompt = @'
Produce exactly EIGHT fully colored DEFENSE / HIT / DEFEAT poses in a clean 4 columns by 2 rows. All preserve the exact Rookie identity. Top row: 1 compact crossed-forearm guard; 2 guard compression; 3 chest-hit recoil; 4 off-balance backward stagger. Bottom row: 5 backward fall fully visible; 6 one-knee recovery; 7 side-prone knockdown with face and sash visible; 8 final defeated side-prone pose. Large green separation and consistent scale. No attacker, blood, injury detail, dust, impact effect, black silhouette, placeholder, or cropped prone body. 2K square animation source.
'@
    }
)

Push-Location $projectRoot
try {
    foreach ($job in $jobs) {
        $output = "Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_$($job.Name)_sheet_style_v1.png"
        $metadata = "Artifacts/style_calibration_world_warrior_rookie_$($job.Name)_sheet_v1_job.json"
        if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
            Write-Host "SKIP $($job.Name)"
            continue
        }

        Write-Host "GENERATE $($job.Name)"
        $prompt = $identityLock + "`n" + $job.Prompt
        $raw = & $cli generate create nano_banana_pro `
            --prompt $prompt `
            --image $identity `
            --image $job.Choreography `
            --image $mannequin `
            --image $ryu `
            --image $goku `
            --aspect_ratio '1:1' `
            --resolution '2k' `
            --wait `
            --wait-timeout '20m' `
            --json
        if ($LASTEXITCODE -ne 0) {
            throw "Higgsfield failed for $($job.Name)."
        }

        $result = ($raw -join "`n") | ConvertFrom-Json
        $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding Unicode
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