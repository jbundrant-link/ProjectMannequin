param(
    [switch]$SkipExisting,
    [switch]$GenerateSources
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
$pilot = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_dojo_prodigy_kenzo_style_pilot_v1.png'
$rookie = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_style_pilot_v1.png'
$striker = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$identityCore = @'
The WORLD WARRIOR DOJO PRODIGY KENZO is the decorated elite version of the Dojo Rookie: a poised, upright, taller young open-palm master. Preserve this exact identity in every pose: lean athletic build with confident straightened posture; swept-back black hair held by one bright saffron-gold champion circlet; layered ceremonial sleeveless vest in deep indigo with gold-embroidered edge trim over a warm-ivory underwrap; a long vermilion champion half-sash that hangs from the left hip down to the knee as a distinctive trailing silhouette; wide warm-ivory forearm guards; charcoal wide pleated hakama-style trousers gathered by a saffron cord; a bronze champion medallion centered on the chest; and low ivory split-sole shoes. Kenzo is clearly a rank above the Rookie: more ceremonial cloth, an upright master's bearing, and the trailing vermilion half-sash. Keep an open-palm karate-master fighting identity, not a wrestler or striker.
'@
$forbid = @'
Every figure must be complete and separated on uniform pure chroma-green RGB 0,255,0. No white karate gi, no red headband, no bare feet, no tracksuit, no short Rookie sash tab, no military or tactical gear, no armor, no cargo pockets, no photorealism, no PBR, no painterly blur, no flat vector art, no pixel art, no weapon, no aura, no impact effect, no target, no shadow, no floor, no scene, no text, no labels, no grid lines, no border, no touching figures, no extra limbs, no missing costume pieces, no duplicate pose, and no crop.
'@

$pilotPrompt = @"
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-body IDENTITY PILOT of the World Warrior Dojo Prodigy Kenzo, a single figure standing in a calm confident three-quarter guard facing right, both feet grounded, lead palm open and slightly forward. $identityCore

The first supplied image is the approved World Warrior Dojo Rookie: use it only for shared World Warrior material and color grammar (deep indigo, saffron, vermilion, warm ivory), never to copy its lean build, short sash tab, face, or costume geometry. The second image is the approved World Warrior Striker: family reference only. The final three images are the canonical mannequin, Ryu, and Goku rendering-finish anchors: match their controlled dark contours, broad two-to-four-band cel shading, simplified cloth materials, designed highlights, and arcade-readable anatomy. Do not copy any reference character's face, hair, costume, or identity.

One complete centered figure on uniform pure chroma-green RGB 0,255,0 with a large green margin on all sides. $forbid 2K square identity pilot.
"@

function Invoke-KenzoImage {
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

function Invoke-KenzoSources {
    $identityLock = @"
PROJECT MANNEQUIN STYLE LOCK. The first supplied image is the exact approved WORLD WARRIOR DOJO PRODIGY KENZO identity. Preserve it exactly in every pose. $identityCore The second image is choreography/layout reference only; copy its timing and body flow, not its identity. The third image is the approved Dojo Rookie for shared World Warrior material grammar only. The final three images are rendering-finish references only. Do not copy any reference character's face, hair, costume, or identity.

$forbid
"@

    $jobs = @(
        [pscustomobject]@{
            Name         = 'idle'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_idle_sheet_v1.png'
            Prompt       = @'
Produce exactly FOUR full-body IDLE poses in a clean 2 columns by 2 rows. All face right and use one consistent foot baseline. Ordered left-to-right, top then bottom: 1 upright open-palm master guard with weight balanced; 2 subtle inhale with lead palm raised; 3 slight front-foot weight shift and composed head turn; 4 subtle exhale returning to the original guard. Keep the long vermilion half-sash trailing naturally. Large green margins around every figure. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name         = 'walk'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v1.png'
            Prompt       = @'
Produce exactly EIGHT full-body composed WALK poses in a clean 4 columns by 2 rows. All face right. Sequence left-to-right, top then bottom: 1 left-foot contact; 2 compression; 3 passing; 4 high point; 5 right-foot contact; 6 compression; 7 passing; 8 high point. Use a measured upright master's gait, controlled arm carriage, open lead hand, and the long vermilion half-sash swaying opposite the step. Same scale and baseline with large green separation. No run or attack. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name         = 'dash'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_dash_sheet_v1.png'
            Prompt       = @'
Produce exactly SIX full-body forward DASH poses in a clean 3 columns by 2 rows. All face right. Ordered sequence: 1 low coil; 2 explosive first step; 3 forward acceleration; 4 longest gliding sprint stride; 5 braking step; 6 quick return toward open-palm guard. Preserve both shoes, both hands, chest medallion, circlet, and the long half-sash in every pose. Same scale with large green separation. No speed effects or motion blur. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name         = 'jump'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_jump_sheet_v1.png'
            Prompt       = @'
Produce exactly FOUR full-body agile JUMP poses in a clean 2 columns by 2 rows. All face right. Ordered sequence: 1 compact takeoff crouch; 2 rising pose with lead knee lifted; 3 controlled apex with limbs tucked and open lead hand; 4 poised landing compression. Each pose must include the complete head, hands, half-sash, medallion, and shoes with generous green around it. Same scale and identity. No aura, dust, or floor. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name         = 'attack_startup'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_attacks_sheet_v1.png'
            Prompt       = @'
Produce exactly FIVE full-body MASTER PALM startup/active poses arranged as 3 figures on the top row and 2 on the bottom row. All face right. Ordered poses: 1 upright open-palm guard; 2 lead shoulder rotates; 3 precise front-foot step and palm chamber; 4 lead palm nearly extended; 5 strongest fully extended straight open-palm master strike active pose. Keep the rear hand poised at the ribs, both shoes grounded, and the active hand clearly open. Large green separation and one consistent scale. No punch, kick, recovery pose, impact, target, or speed line. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name         = 'attack_recovery'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_attacks_sheet_v1.png'
            Prompt       = @'
Produce exactly FIVE full-body MASTER PALM recovery poses arranged as 3 figures on the top row and 2 on the bottom row. All face right. Continue after a fully extended straight open-palm strike: 1 palm begins to snap back; 2 rear foot settles balance; 3 torso unwinds and lead elbow tucks; 4 open-palm guard returns; 5 neutral upright guard. Keep both shoes, both hands, medallion, and half-sash visible with one consistent scale and large green separation. No startup, new attack, impact, target, or effect. 2K square animation source.
'@
        },
        [pscustomobject]@{
            Name         = 'misc'
            Choreography = 'Assets/Sprites/Mannequin/higgsfield_misc_sheet_v1.png'
            Prompt       = @'
Produce exactly EIGHT fully colored DEFENSE / HIT / DEFEAT poses in a clean 4 columns by 2 rows. All preserve the exact Kenzo identity. Top row: 1 composed crossed-forearm guard; 2 guard compression; 3 chest-hit recoil; 4 off-balance backward stagger. Bottom row: 5 backward fall fully visible; 6 one-knee recovery; 7 side-prone knockdown with face, circlet, and half-sash visible; 8 final defeated side-prone pose. Large green separation and consistent scale. No attacker, blood, injury detail, dust, impact effect, black silhouette, placeholder, or cropped prone body. 2K square animation source.
'@
        }
    )

    foreach ($job in $jobs) {
        $output = "Assets/Sprites/Concepts/StyleCalibration/world_warrior_dojo_prodigy_kenzo_$($job.Name)_sheet_style_v1.png"
        $metadata = "Artifacts/style_calibration_world_warrior_dojo_prodigy_kenzo_$($job.Name)_sheet_v1_job.json"
        Invoke-KenzoImage `
            -Name $job.Name `
            -Prompt ($identityLock + "`n" + $job.Prompt) `
            -Output $output `
            -Metadata $metadata `
            -References @($pilot, $job.Choreography, $rookie, $mannequin, $ryu, $goku)
    }
}

Push-Location $projectRoot
try {
    if ($GenerateSources) {
        Invoke-KenzoSources
        return
    }

    Invoke-KenzoImage `
        -Name 'kenzo_pilot_v1' `
        -Prompt $pilotPrompt `
        -Output $pilot `
        -Metadata 'Artifacts/style_calibration_world_warrior_dojo_prodigy_kenzo_pilot_v1_job.json' `
        -References @($rookie, $striker, $mannequin, $ryu, $goku)
}
finally {
    Pop-Location
}
