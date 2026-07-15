# generate_enemy_sprites.ps1
# Generates all 6 animation sub-sheets for one enemy via Higgsfield gpt_image_2,
# then composites them into a final 10x9 sprite sheet.
#
# Usage:
#   .\Scripts\Tools\generate_enemy_sprites.ps1 `
#       -Id "archive_scout" `
#       -Desc "lean agile humanoid guardian construct clad in sleek dark-teal segmented armor ..." `
#       [-Bg green]          # use 'magenta' for green-skinned characters (e.g. Saibaman)
#       [-SkipExisting]      # skip API call if <id>_src_<anim>.png already exists
#       [-ComposeOnly]       # skip all generation, just re-run the compositor
#
# Requires: Higgsfield CLI + Pillow
param(
    [Parameter(Mandatory=$true)]  [string]$Id,
    [Parameter(Mandatory=$true)]  [string]$Desc,
    [string]$Bg = "green",
    [switch]$SkipExisting,
    [switch]$ComposeOnly
)

$ErrorActionPreference = 'Stop'
$cliCommand = Get-Command higgsfield -ErrorAction Stop
$cli  = $cliCommand.Source
$root = 'Assets/Sprites/Enemies'
$mannequin = 'Assets/Sprites/Mannequin'
$style = (Resolve-Path "$mannequin/mannequin_master_higgsfield_v1.png").Path
$bgHex = if ($Bg -eq 'magenta') { 'FF00FF' } else { '00FF00' }

# Animation groups: name, source sheet, grid (cols x rows), aspect ratio for gpt_image_2
$anims = @(
    [pscustomobject]@{Name='idle';    Src="$mannequin/higgsfield_idle_sheet_v1.png";    Cols=2; Rows=2; Aspect='1:1'   },
    [pscustomobject]@{Name='walk';    Src="$mannequin/higgsfield_walk_sheet_v1.png";    Cols=4; Rows=2; Aspect='16:9'  },
    [pscustomobject]@{Name='dash';    Src="$mannequin/higgsfield_dash_sheet_v1.png";    Cols=3; Rows=2; Aspect='16:9'  },
    [pscustomobject]@{Name='jump';    Src="$mannequin/higgsfield_jump_sheet_v1.png";    Cols=2; Rows=2; Aspect='1:1'   },
    [pscustomobject]@{Name='attacks'; Src="$mannequin/higgsfield_attacks_sheet_v1.png"; Cols=5; Rows=2; Aspect='16:9'  },
    [pscustomobject]@{Name='misc';    Src="$mannequin/higgsfield_misc_sheet_v1.png";    Cols=4; Rows=2; Aspect='16:9'  }
)

$bgName = if ($Bg -eq 'magenta') { 'magenta background hex FF00FF' } else { 'pure green background hex 00FF00' }

if (-not $ComposeOnly) {
    foreach ($a in $anims) {
        $out = "$root/${Id}_src_$($a.Name).png"
        if ($SkipExisting -and (Test-Path $out)) {
            Write-Host "SKIP $($a.Name) (already exists)"
            continue
        }

        $tpl = (Resolve-Path $a.Src).Path
        $rows = $a.Rows; $cols = $a.Cols

        $prompt = "The first image is the EXACT choreography reference: a $cols-column by $rows-row sprite animation sheet. " +
            "Redraw the posed figure in EVERY cell as: $Desc. " +
            "CRITICAL RULES: (1) Each figure must be COMPLETELY contained inside its own cell — ZERO parts of any " +
            "figure crossing into any neighboring cell. (2) Leave a VISIBLE $bgName margin on ALL FOUR sides of " +
            "every figure — at least 12 percent of cell width. (3) The grid must have exactly $cols columns and " +
            "$rows rows with the IDENTICAL pose in each cell and identical left-to-right top-to-bottom order. " +
            "(4) Match ONLY the rendering finish of the second image: bold clean black outlines, smooth cel-shaded " +
            "coloring with crisp highlight and shadow banding, polished modern 2.5D fighting-game sprite look. " +
            "(5) Each figure must have a clearly rendered head/face and complete, correctly connected limbs. " +
            "(6) Use a uniform $bgName everywhere. No motion blur, no duplicated limbs, no text, no projectiles."

        Write-Host "Generating $($a.Name) ($cols x $rows, $($a.Aspect))..."
        $raw = & $cli generate create gpt_image_2 `
            --prompt $prompt `
            --image $tpl `
            --image $style `
            --aspect_ratio $a.Aspect `
            --resolution "2k" `
            --quality "high" `
            --wait --wait-timeout "20m" `
            --json

        $jobs = ($raw -join "`n") | ConvertFrom-Json
        $url  = @($jobs)[0].result_url
        Write-Host "  URL: $url"
        Invoke-WebRequest -Uri $url -OutFile $out
        Write-Host "  SAVED $out"
    }
}

Write-Host "`nCompositing $Id..."
python Scripts/Tools/compose_enemy_sheet.py $Id $Bg

Write-Host "`nGenerating preview..."
python -c "
from PIL import Image, ImageDraw
im=Image.open('Assets/Sprites/Enemies/${Id}_higgsfield_v1.png').convert('RGBA'); W,H=im.size
sc=900/W; prev=im.resize((900,int(H*sc)),Image.LANCZOS)
bg=Image.new('RGBA',prev.size,(58,58,66,255)); d=ImageDraw.Draw(bg); st=14
[d.rectangle((x,y,x+st,y+st),fill=(84,84,92,255)) for y in range(0,prev.size[1],st) for x in range(0,prev.size[0],st) if (x//st+y//st)%2==0]
bg.alpha_composite(prev); bg.convert('RGB').save('Assets/Sprites/Enemies/_preview_${Id}.png'); print('PREVIEW Assets/Sprites/Enemies/_preview_${Id}.png')
"
