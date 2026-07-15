# World Warrior Fighter Atlas

`ryu_higgsfield_v4_sheet.png` is the active `16x15` atlas. It preserves the exact source-frame counts for movement, normal, crouching, aerial, and signature-attack sequences. It is built by `Scripts/Tools/process_world_warrior_sprites_v4.py`.

The Higgsfield source was style-matched to the mannequin using the locally supplied prototype frames as a pose and costume reference. Those original reference frames do not contain license or redistribution metadata. Keep this derivative asset personal-use only and replace it with wholly original or explicitly licensed character art before any public release.

Rebuild the active atlas:

```powershell
python Scripts/Tools/create_ryu_v4_references.py
python Scripts/Tools/process_world_warrior_sprites_v4.py
python Scripts/Tools/create_ryu_v4_animation_previews.py
```

The packer removes the green background, normalizes all poses with one shared anatomical scale and ground baseline, and leaves unused row cells transparent. Runtime mappings play every authored frame across the move duration.

See `Docs/HIGGSFIELD_RYU_ART_PASS_V4.md` for the exact frame map, generation URLs, and playback correction.

`ryu_mvc2_prototype_sheet.png` and `pack_ryu_prototype_sprites.py` remain as legacy local references and are no longer loaded by the game.
