param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
$rookie = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_style_pilot_v1.png'
$striker = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$identityLock = @'
PROJECT MANNEQUIN STYLE LOCK. The first supplied image is the exact approved WORLD WARRIOR TOURNAMENT GRAPPLER identity. Preserve the same original massive heavyweight in every pose: broad trapezoid torso, thick neck, huge forearms and thighs, short dense black curls with one silver forelock, clean-shaven square jaw, sleeveless deep-plum wrap tunic with one indigo diagonal inner panel, wide woven saffron belt with compact side knot, loose brick-red knee-length wrestling trousers with indigo gussets, charcoal shin wraps, low warm-ivory soft wrestling shoes, dark-indigo wrist wraps, and one bronze-rimmed vermilion lacquer token centered on the belt. Keep the same face, heavyweight proportions, costume geometry, color hierarchy, camera, and light. The second image is choreography/layout reference only. The next two images are World Warrior family-material references only. The final three images are rendering-finish references only: controlled dark contours, broad two-to-four-band cel shading, simplified cloth materials, designed highlights, and arcade-readable anatomy. Do not copy any reference character's face, hair, costume, or identity.

Every figure must be complete and separated on uniform pure chroma-green RGB 0,255,0. No olive drab, camouflage, tactical vest, suspenders, cargo trousers, utility pockets, knee pads, combat boots, military gear, armor, wrestling singlet, championship text, photorealism, PBR, painterly blur, flat vector art, pixel art, weapon, aura, impact effect, target, shadow, floor, scene, text, labels, grid lines, border, touching figures, extra limbs, missing belt/token, duplicate pose, or crop.
'@

$jobs = @(
    [pscustomobject]@{
        Name = 'idle'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_idle_sheet_v1.png'
        Prompt = @'
Produce exactly FOUR full-body IDLE poses in a clean 2 columns by 2 rows. All face right and keep a low wide wrestling baseline. Ordered left-to-right, top then bottom: 1 forward-pitched two-hand grappling guard; 2 subtle inhale with lead hand reaching; 3 weight shifts lower over the lead shoulder; 4 subtle exhale returning to guard. Both hands stay open, knees bent, head up, belt token centered, and shoes planted. Large green margins around every figure. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'walk'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v1.png'
        Prompt = @'
Produce exactly EIGHT full-body heavy stalking WALK poses in a clean 4 columns by 2 rows. All face right. Sequence left-to-right, top then bottom: 1 left-foot contact; 2 low compression; 3 passing; 4 high point; 5 right-foot contact; 6 low compression; 7 passing; 8 high point. Use short powerful steps, bent knees, forward torso, open grappling hands, and restrained belt/tunic movement. Same scale and baseline with large green separation. No run, punch, kick, or attack. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'dash'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_dash_sheet_v1.png'
        Prompt = @'
Produce exactly SIX full-body shoulder-led DASH poses in a clean 3 columns by 2 rows. All face right. Ordered sequence: 1 deep preload with hands open; 2 explosive first step; 3 low forward acceleration; 4 longest shoulder-first driving stride; 5 broad braking step; 6 return to low grappling guard. Preserve both shoes, both hands, belt, token, tunic panels, and heavyweight mass in every pose. Same scale with large green separation. No strike, tackle contact, target, speed effect, or motion blur. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'jump'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_jump_sheet_v1.png'
        Prompt = @'
Produce exactly FOUR full-body compact HEAVY JUMP poses in a clean 2 columns by 2 rows. All face right. Ordered sequence: 1 deep takeoff crouch; 2 powerful rise with knees bent separately; 3 compact apex with open hands guarding and heavyweight body tucked; 4 broad controlled landing compression. Each pose includes complete head, hands, belt token, tunic, trousers, wraps, and shoes with generous green around it. Same scale and identity. No aerial attack, aura, dust, or floor. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'attack_startup'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_attacks_sheet_v1.png'
        Prompt = @'
Produce exactly FIVE full-body SHOULDER DRIVE startup/active poses arranged as 3 figures on the top row and 2 on the bottom row. All face right. Ordered poses: 1 low open-hand wrestling guard; 2 hips drop into a level change; 3 both arms open for a body lock as lead shoulder advances; 4 rear leg drives and hands begin to wrap around an imaginary opponent's waist; 5 strongest active pose: very low long shoulder-first lunge to the right, head safely beside the imaginary torso, both arms visibly curved inward in a body-lock shape, rear leg fully driving, front knee deeply bent. This must read as a grappling entry, not a punch. Every body complete with large green separation and one consistent scale. No target figure, closed striking fist, kick, recovery pose, impact, dust, or speed line. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'attack_recovery'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_attacks_sheet_v1.png'
        Prompt = @'
Produce exactly FIVE full-body SHOULDER DRIVE recovery poses arranged as 3 figures on the top row and 2 on the bottom row. All face right. Continue after a low body-lock drive: 1 forward momentum settles with arms still curved inward; 2 hands release and rear foot catches balance; 3 torso rises from the level change; 4 feet widen as open-hand grappling guard returns; 5 neutral low forward-pitched guard restored. Keep both shoes, both open hands, belt, token, tunic panels, and heavyweight proportions visible with one consistent scale and large green separation. No new attack, punch, target, throw victim, impact, or effect. 2K square animation source.
'@
    },
    [pscustomobject]@{
        Name = 'misc'
        Choreography = 'Assets/Sprites/Mannequin/higgsfield_misc_sheet_v1.png'
        Prompt = @'
Produce exactly EIGHT fully colored DEFENSE / HIT / DEFEAT poses in a clean 4 columns by 2 rows. Preserve the exact Grappler identity. Top row: 1 broad crossed-forearm guard; 2 deep guard compression; 3 torso-hit recoil; 4 heavyweight backward stagger. Bottom row: 5 broad backward fall fully visible; 6 one-knee recovery with one hand planted; 7 side-prone knockdown with belt token and silver forelock visible; 8 final defeated side-prone pose. Large green separation and consistent scale. No attacker, blood, injury detail, dust, impact effect, black silhouette, placeholder, or cropped prone body. 2K square animation source.
'@
    }
)

Push-Location $projectRoot
try {
    foreach ($job in $jobs) {
        $output = "Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_$($job.Name)_sheet_style_v1.png"
        $metadata = "Artifacts/style_calibration_world_warrior_grappler_$($job.Name)_sheet_v1_job.json"
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
            --image $rookie `
            --image $striker `
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