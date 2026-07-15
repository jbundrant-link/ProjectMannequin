# World Warrior Art Pass V4

Reviewed: July 2, 2026

V4 replaces the uniform ten-pose V3 animation scheme. The supplied full sprite
sheet and the extracted files in `Assets/Sprites/Concepts/Ryu/` are now used as
frame-by-frame choreography references. Higgsfield changes the rendering style,
but the source sequence count and order remain authoritative.

## Active Atlas

- Runtime asset: `Assets/Sprites/Ryu/ryu_higgsfield_v4_sheet.png`
- Layout: 16 columns by 15 rows
- Cell size: 320 by 320 pixels
- Shared pose scale: 0.39
- Ground baseline: pixel 312
- Runtime pixel size: 0.0144

## Exact Sequence Map

| Row | Runtime sequence | Source frames | Count |
| --- | --- | --- | ---: |
| 0 | Idle | 61-68 | 8 |
| 1 | Walk | 70-80 | 11 |
| 2 | Dash | 81-89 | 9 |
| 3 | Jump | 90-101 | 12 |
| 4 | Standing punches | 200-209 | 10 |
| 5 | Standing kicks | 210-217 | 8 |
| 6 | Crouching punches | 218-226 | 9 |
| 7 | Crouching kicks and sweep | 227-239 | 13 |
| 8 | Air attacks A | 240-249 | 10 |
| 9 | Air attacks B | 250-259 | 10 |
| 10 | Reactions | V3 reaction source | 10 |
| 11 | Projectile technique | 277-279 | 3 |
| 12 | Rising uppercut | 280-285 | 6 |
| 13 | Spinning kick | 286-297 | 12 |
| 14 | Projectile super | 277-279 | 3 |

## Runtime Playback Correction

`CharacterVisualComponent.ResolveAttackFrame()` previously held the first image
for all startup frames, displayed only one middle image during the active
window, and then jumped to the final image for all recovery frames. V4 maps the
complete ordered image sequence across the move's full simulation duration.

Movement also uses its real row lengths. The jump row advances from vertical
velocity so takeoff, ascent, apex, descent, and landing poses are all visible.

## Higgsfield Sources

- Walk 70-75:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_010026_64e11688-bf3b-4c9c-af80-cfb3c8710821.png
- Walk 76-80:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012403_20f0554f-bd20-4041-9fd7-3ee5b22c28f4.png
- Idle:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012508_4999ed90-410c-46dc-a067-88b2865d1c59.png
- Dash:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012511_99ce4769-58f3-4ffa-9581-67ffe0aac572.png
- Jump 90-95:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012515_7e188613-5d23-4c0a-a3e5-054f17f9f989.png
- Jump 96-101:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012519_2c142640-92c9-43ac-a136-01151d8b836d.png
- Standing punches:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012524_86a28e59-9479-47bd-b9c9-0621f58cab2e.png
- Standing kicks:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012528_b42a56c8-2d25-4619-8e8a-80ce77561e7f.png
- Crouching punches:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012532_4b19a1bc-b968-4330-a621-691c6e2fa8d9.png
- Crouching kicks 227-233:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012947_b0a549e1-4a3c-4caf-af1b-38d3d1ae232e.png
- Crouching kicks 234-239:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012952_a24c42fb-3188-40c7-8d31-489408aee943.png
- Air attacks 240-249:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012956_e1109ac8-8f89-4ebb-b4aa-f8e8c28db976.png
- Air attacks 250-259:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_013000_c0bf4c38-4ece-472c-ac93-9758bcc81017.png
- Projectile technique:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_012023_cd256e46-f299-4132-af25-ce75adeb8be4.png
- Rising uppercut:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_013003_7c960b9f-ecf9-4b12-a7ea-528f5441612d.png
- Spinning kick 286-291:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_013008_a5264b40-1452-4bc8-8b40-3db534b60ad2.png
- Spinning kick 292-297:
  https://d8j0ntlcm91z4.cloudfront.net/user_3FrYOtkTM6XEt7QUfhm0X2eWEf2/hf_20260702_013011_bc310d95-1ca7-4345-bfaf-dd84ea8a324f.png

## Rebuild

```powershell
python Scripts/Tools/create_ryu_v4_references.py
python Scripts/Tools/process_world_warrior_sprites_v4.py
python Scripts/Tools/create_ryu_v4_animation_previews.py
dotnet build
```

Clean no-enemy previews are in:

`Assets/Sprites/Ryu/Diagnostics/V4/`
