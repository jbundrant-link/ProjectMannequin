param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$generator = Join-Path $PSScriptRoot 'generate_walk_cycle_repair.ps1'
$repairs = @(
    [pscustomobject]@{
        Name = 'Archive Knight'
        Identity = 'Assets/Sprites/Concepts/StyleCalibration/archive_knight_style_pilot_v1.png'
        Current = 'Assets/Sprites/Concepts/StyleCalibration/archive_knight_walk_sheet_v3.png'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/archive_knight_walk_sheet_v4.png'
        Metadata = 'Artifacts/style_calibration_archive_knight_walk_v4_job.json'
        Description = 'faceless warm-ivory crested archive knight; cyan visor; enormous open broken-archive-arch left pauldron; small right shoulder plate; violet sternum core; deep-plum joints; short split tabards; heavy boots; one cyan-edged blade integrated along the right forearm; never mirror or swap asymmetrical features'
    },
    [pscustomobject]@{
        Name = 'Archive Raider'
        Identity = 'Assets/Sprites/Concepts/StyleCalibration/archive_raider_style_pilot_v1.png'
        Current = 'Assets/Sprites/Concepts/StyleCalibration/archive_raider_walk_sheet_style_v2.png'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/archive_raider_walk_sheet_style_v3.png'
        Metadata = 'Artifacts/style_calibration_archive_raider_walk_sheet_v3_job.json'
        Description = 'sleek warm-ivory porcelain archive raider with dark indigo-plum understructure, smooth black visor with cyan light, cyan-violet chest core, paired tapered back fins, matched forearm blade guards, athletic proportions, and exact approved armor seams'
    },
    [pscustomobject]@{
        Name = 'Archive Bruiser'
        Identity = 'Assets/Sprites/Concepts/StyleCalibration/archive_bruiser_style_pilot_v1.png'
        Current = 'Assets/Sprites/Concepts/StyleCalibration/archive_bruiser_walk_sheet_style_v1.png'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/archive_bruiser_walk_sheet_style_v2.png'
        Metadata = 'Artifacts/style_calibration_archive_bruiser_walk_sheet_v2_job.json'
        Description = 'massive heavy archive bruiser with broad ivory armored torso, indigo-plum joints, magenta fractured-hex sternum core, amber visor eyes, horned helmet silhouette, huge hammer-like forearms, and short powerful legs; preserve exact armor and heavyweight proportions'
    },
    [pscustomobject]@{
        Name = 'Overseer Basalt'
        Identity = 'Assets/Sprites/Concepts/StyleCalibration/overseer_basalt_style_pilot_v1.png'
        Current = 'Assets/Sprites/Concepts/StyleCalibration/overseer_basalt_walk_sheet_v2.png'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/overseer_basalt_walk_sheet_v3.png'
        Metadata = 'Artifacts/style_calibration_overseer_basalt_walk_sheet_v3_job.json'
        Description = 'mostly matte obsidian-indigo heavyweight construct with ivory fracture plates, wedge helmet prongs, violet core seams, one stepped seismic gauntlet and one stabilizer hand; preserve exact asymmetry, low commanding posture, and distinct Basalt identity'
    },
    [pscustomobject]@{
        Name = 'World Warrior Dojo Rookie'
        Identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_style_pilot_v1.png'
        Current = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_walk_sheet_style_v1.png'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_walk_sheet_style_v2.png'
        Metadata = 'Artifacts/style_calibration_world_warrior_rookie_walk_sheet_v2_job.json'
        Description = 'original young quick open-palm fighter with black swept hair and bronze clasp, deep-indigo crossover vest, saffron placket and short sash tab, vermilion trouser panel and wraps, charcoal trousers, ivory shoes, and lacquer waist token; preserve exact lean identity'
    },
    [pscustomobject]@{
        Name = 'World Warrior Tournament Grappler'
        Identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
        Current = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_walk_sheet_style_v1.png'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_walk_sheet_style_v2.png'
        Metadata = 'Artifacts/style_calibration_world_warrior_grappler_walk_sheet_v2_job.json'
        Description = 'massive tournament wrestler with broad trapezoid torso, thick neck, huge forearms and thighs, short black curls with one silver forelock, deep-plum wrap tunic, indigo diagonal panel, saffron belt, brick-red wrestling trousers, charcoal wraps, and ivory shoes'
    },
    [pscustomobject]@{
        Name = 'Astral Saibaman'
        Identity = 'Artifacts/StyleCalibration/WalkIdentityReferences/astral_saibaman_identity.png'
        Current = 'Assets/Sprites/Enemies/Source/astral_saibaman_src_walk.png'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/astral_saibaman_walk_sheet_v2.png'
        Metadata = 'Artifacts/style_calibration_astral_saibaman_walk_sheet_v2_job.json'
        Description = 'small lean green Saibaman creature with bulbous ridged head, pointed ears, red eyes, clawed three-finger hands and feet, compact hunched alien body, pale belly plates, and exact approved anime cel-shaded identity; no clothing or armor'
    },
    [pscustomobject]@{
        Name = 'Astral Frieza Force Heavy'
        Identity = 'Artifacts/StyleCalibration/WalkIdentityReferences/astral_frieza_heavy_identity.png'
        Current = 'Assets/Sprites/Enemies/Source/astral_frieza_heavy_src_walk.png'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/astral_frieza_heavy_walk_sheet_v2.png'
        Metadata = 'Artifacts/style_calibration_astral_frieza_heavy_walk_sheet_v2_job.json'
        Description = 'large broad Frieza Force heavy soldier with exact approved alien face and skin, white-and-brown segmented battle armor, dark bodysuit, shoulder guards, scouter, heavy boots, and muscular heavyweight proportions; preserve costume and species identity'
    }
)

foreach ($repair in $repairs) {
    $arguments = @{
        Name = $repair.Name
        IdentityPath = $repair.Identity
        CurrentWalkPath = $repair.Current
        OutputPath = $repair.Output
        MetadataPath = $repair.Metadata
        IdentityDescription = $repair.Description
    }
    if ($SkipExisting) {
        $arguments.SkipExisting = $true
    }
    & $generator @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate $($repair.Name) walk repair."
    }
}