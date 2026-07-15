# Higgsfield Art Pass V2

This pass aligns the mannequin combat poses, World Warrior fighter, and Tournament District with one modern cel-shaded 2.5D fighting-game direction.

## Generated Sources

- Mannequin crouch poses: `Assets/Sprites/Mannequin/Higgsfield/higgsfield_crouch_sheet_v2.png`
  - https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_183412_c492b4ef-46a1-48d9-9690-0c5a22381946.png
- Mannequin air poses: `Assets/Sprites/Mannequin/Higgsfield/higgsfield_air_sheet_v2.png`
  - https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_184019_609dd08a-1c8e-4968-a2b7-9f9eb3abbe94.png
- World Warrior fighter poses: `Assets/Sprites/Ryu/Higgsfield/world_warrior_fighter_sheet_v2.png`
  - https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_184427_6a97ea86-5e31-4c17-9a3a-54a601771ecd.png
- Tournament District: `Assets/Stages/WorldWarrior/world_warrior_tournament_district_higgsfield_v2.png`
  - https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_184809_217a4bd6-9122-4b99-acb7-31e3b5b71fa6.png

## Rebuild

```powershell
python Scripts/Tools/process_higgsfield_sprites.py
python Scripts/Tools/process_world_warrior_sprites.py
python Scripts/Tools/create_sprite_scale_comparison.py
```

The mannequin and fighter processors use one scale per generated pose set and a shared ground baseline. Runtime row-specific scaling is intentionally disabled.

The clean no-enemy comparison is written to:

`Assets/Sprites/Mannequin/Diagnostics/mannequin_scale_comparison.png`

Measured idle cells after processing:

- Mannequin: `85x232`
- World Warrior fighter: `126x227`

The broader fighter silhouette comes from authored shoulder and costume proportions rather than a different character-height scale.
