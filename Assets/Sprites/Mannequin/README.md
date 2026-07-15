# Mannequin Sprite Sheets

The active MVP sheet is:

```text
Assets/Sprites/Mannequin/mannequin_sheet_higgsfield_v1.png
```

It was generated from the clean Higgsfield master and pose sheets, then processed into a transparent `10 x 9` sheet with `256 x 256` cells.

Rebuild it after replacing any `higgsfield_*_sheet_v1.png` source:

```powershell
python Scripts/Tools/process_higgsfield_sprites.py
```

Processed individual frames are written to:

```text
Assets/Sprites/Mannequin/Higgsfield/
```

The older PixelLab-compatible sheet remains available here:

```text
Assets/Sprites/Mannequin/mannequin_sheet.png
```

The current prototype expects a clean sprite sheet, not a concept-board screenshot with logos, labels, and multiple differently sized poses.

Default layout expected by `CharacterVisualComponent`:

- 10 columns
- 9 rows
- row 0: idle
- row 1: walk
- row 2: dash
- row 3: jump
- row 4: attacks
- row 5: hit, form swap, death
- row 6: crouch attacks
- row 7: air attacks
- row 8: launcher and `623HP` uppercut

Rows 6-8 use green-screen connected-pose segmentation. Higgsfield output is not
assumed to be an exact mathematical grid: each complete mannequin silhouette is
isolated first, then bottom-anchored and normalized as a group. This prevents
neighboring hands or limbs from being sliced into the wrong frame.

If the real sheet uses a different layout, update `SheetColumns`, `SheetRows`, and the frame mapping in `CharacterVisualComponent`.
