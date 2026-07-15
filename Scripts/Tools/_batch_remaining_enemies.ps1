# _batch_remaining_enemies.ps1 — generates the 7 remaining enemy sheets.
# Robust driver: run as a file (avoids inline multi-line interleaving).
# Each enemy: 6 animation sub-sheets via gpt_image_2, then composite + preview.
$ErrorActionPreference = 'Continue'
$cliCommand = Get-Command higgsfield -ErrorAction Stop
$cli   = $cliCommand.Source
$style = (Resolve-Path "Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1.png").Path
$base  = "CRITICAL RULES: (1) Each figure COMPLETELY contained in its own cell, ZERO parts crossing into neighboring cells. (2) VISIBLE background margin of at least 12 percent of cell width on ALL FOUR sides of every figure. (3) Preserve the identical grid, identical pose per cell, identical left-to-right top-to-bottom order. (4) Match ONLY the rendering finish of the second image: bold clean black outlines, smooth cel-shaded coloring with crisp highlight and shadow banding, polished modern 2.5D fighting-game sprite look. (5) Each figure has a clear head/face and complete correctly connected limbs. No motion blur, no duplicated limbs, no text, no projectiles."

$anims = @(
    @{name='idle';    src='higgsfield_idle_sheet_v1.png';    cols=2; rows=2; aspect='1:1'},
    @{name='walk';    src='higgsfield_walk_sheet_v1.png';    cols=4; rows=2; aspect='16:9'},
    @{name='dash';    src='higgsfield_dash_sheet_v1.png';    cols=3; rows=2; aspect='16:9'},
    @{name='jump';    src='higgsfield_jump_sheet_v1.png';    cols=2; rows=2; aspect='1:1'},
    @{name='attacks'; src='higgsfield_attacks_sheet_v1.png'; cols=5; rows=2; aspect='16:9'},
    @{name='misc';    src='higgsfield_misc_sheet_v1.png';    cols=4; rows=2; aspect='16:9'}
)

$enemies = @(
    @{id='world_warrior_rookie';   bg='green';   hex='00FF00'; desc='young street fighter in a worn white karate gi with red trim and a red cloth headband, bare feet, athletic lean build, short dark hair, determined expression, fingerless gloves'},
    @{id='world_warrior_striker';  bg='green';   hex='00FF00'; desc='agile street fighter in a sleek navy-blue athletic tracksuit with white side stripes, white sneakers, short spiky hair, fingerless gloves, lean muscular build, confident streetwise stance'},
    @{id='world_warrior_grappler'; bg='green';   hex='00FF00'; desc='huge powerfully built wrestler in a sleeveless olive muscle shirt with suspenders over the shoulders, heavy cargo pants, shaved head with beard stubble, thick forearms, wrestling knee pads and heavy boots'},
    @{id='astral_saibaman';        bg='magenta'; hex='FF00FF'; desc='a small wiry green-skinned humanoid alien creature with a hairless round head, pointed ears, large glowing red eyes, sharp fangs, thin muscular limbs, clawed hands and feet, wearing simple black shorts'},
    @{id='astral_frieza_scout';    bg='green';   hex='00FF00'; desc='a lean armored alien space soldier with pinkish skin wearing white and purple bio-armor with rounded shoulder guards and a chest plate, a green glowing scouter eyepiece over the left eye, sleek and agile build'},
    @{id='astral_frieza_heavy';    bg='green';   hex='00FF00'; desc='a bulky brutish armored alien space soldier in heavy white and dark-purple battle armor with large shoulder plates, thick muscular build, small horns on the head, a scowling face, and a green scouter eyepiece'},
    @{id='astral_ki_captain';      bg='green';   hex='00FF00'; desc='an elite alien officer in sleek blue and white command battle armor with gold trim and shoulder plates, a stern commanding face, short slicked hair, glowing cyan energy aura accents at the fists'}
)

foreach ($e in $enemies) {
    Write-Host "===== $($e.id) ($($e.bg)) ====="
    foreach ($a in $anims) {
        $out = "Assets/Sprites/Enemies/$($e.id)_src_$($a.name).png"
        if (Test-Path $out) { Write-Host "  SKIP $($a.name) (exists)"; continue }
        $tpl = (Resolve-Path "Assets/Sprites/Mannequin/$($a.src)").Path
        $prompt = "The first image is the EXACT choreography reference: a $($a.cols)-column by $($a.rows)-row sprite animation sheet. Redraw the posed figure in EVERY cell as: $($e.desc). $base (6) Use a uniform pure $($e.bg) background hex $($e.hex) everywhere including all margins."
        Write-Host "  >> $($a.name) ($($a.cols)x$($a.rows) $($a.aspect))"
        $raw = & $cli generate create gpt_image_2 --prompt $prompt --image $tpl --image $style --aspect_ratio $a.aspect --resolution "2k" --quality "high" --wait --wait-timeout "20m" --json
        try {
            $url = @(($raw -join "`n") | ConvertFrom-Json)[0].result_url
            Invoke-WebRequest -Uri $url -OutFile $out
            Write-Host "     SAVED $out"
        } catch {
            Write-Host "     FAILED $($a.name): $($_.Exception.Message)"
        }
    }
    python Scripts/Tools/compose_enemy_sheet.py $($e.id) $($e.bg)
    Write-Host "  COMPOSED $($e.id)`n"
}
Write-Host "ALL REMAINING ENEMIES DONE"
