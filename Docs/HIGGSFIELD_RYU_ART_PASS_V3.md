# World Warrior Art Pass V3

Historical note: V4 supersedes this atlas with exact source-frame counts and
full-sequence runtime playback. See `Docs/HIGGSFIELD_RYU_ART_PASS_V4.md`.

Reviewed: July 1, 2026

The previous V2 atlas used ten representative poses and repeated them across
multiple gameplay states. V3 audits the supplied 441-frame source directory,
creates numbered references for frames 61-389, and provides dedicated
Higgsfield generations for each implemented motion family.

## Final Atlas

- Runtime asset: `Assets/Sprites/Ryu/ryu_higgsfield_v3_sheet.png`
- Layout: 10 columns by 12 rows
- Cell size: 320 by 320 pixels
- Shared pose scale: 0.39
- Ground baseline: pixel 312
- Runtime pixel size: 0.0144

Rows:

1. Idle
2. Walk
3. Dash
4. Jump
5. Six-button standing normals
6. Reactions and knockdown
7. Crouch guard, punches, low kick, and sweep
8. Aerial punches and kicks
9. Projectile technique
10. Rising uppercut
11. Spinning kick
12. Super projectile technique

## Generated Sources

- Movement:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_203106_39c387db-7e73-48cd-b688-146dcc3ce014.png
- Standing normals:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_204143_79a67dd6-7d61-4c90-9f37-cfb03408c36a.png
- Reactions:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_211159_65ac603e-47e6-4595-a128-6426ce6397dd.png
- Aerial attacks:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_213441_06c68548-8b8e-4888-8d65-3ddcb3aedd09.png
- Projectile technique:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_213713_78f1ced7-e69f-43b6-8964-bb9f41800e93.png
- Rising uppercut:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_213834_55efc945-f1eb-4b93-83e9-a2d97088f52d.png
- Spinning kick:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_214009_0f507fb5-2529-4f1a-a291-52d36252ef15.png
- Corrected crouch attacks:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260701_220015_7a5e1362-6704-4aa9-b867-7db58dd0ddd9.png

## Runtime Coverage

The Ryu boss and unlockable form now expose light, medium, and heavy punches;
light, medium, and heavy kicks; crouching jab, crouching medium kick, sweep;
three aerial normals; projectile, rising uppercut, spinning kick; and projectile
super. The atlas also contains movement and reaction states used by the CPU
fighter.

The source directory still contains alternate intros, taunts, duplicate
strength variants, and effects that are not separate MVP moves. They remain in
the numbered reference sheets and are not falsely presented as implemented.

## Rebuild And Verify

```powershell
python Scripts/Tools/create_ryu_source_contact_sheets.py
python Scripts/Tools/create_ryu_move_references.py
python Scripts/Tools/process_world_warrior_sprites.py
python Scripts/Tools/create_ryu_scale_comparison.py
dotnet build
```

The clean no-enemy comparison is:

`Assets/Sprites/Ryu/Diagnostics/ryu_move_scale_comparison_v3.png`
