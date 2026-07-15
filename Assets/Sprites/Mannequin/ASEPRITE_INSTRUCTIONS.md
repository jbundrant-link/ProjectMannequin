Aseprite cleanup and export checklist

1. Open the generated `mannequin_sheet.png` in Aseprite.
2. If PixelLab produced a single-image sheet with correct grid, set `View -> Grid -> Grid Settings` to match frame size.
3. Use `Sprite -> Sheet -> Slice` or `File -> Export Sprite Sheet` with `Fixed Grid`:
   - Columns: 10
   - Rows: 6
   - Frame size: match the grid (e.g., 96x96)
4. Clean frames:
   - Align feet: ensure the character's feet rest on the same baseline across frames.
   - Remove stray pixels and anti-aliasing artifacts.
   - Fix silhouettes: keep the silhouette consistent across frames and poses.
   - Check outlines: ensure black outlines are consistent and not broken.
5. Optional: run a pixel-perfect check by toggling magnification and stepping through frames.
6. Export:
   - File -> Export Sprite Sheet
   - Sheet type: Fixed Grid
   - Columns: 10, Rows: 6
   - Include: PNG and optional JSON data
   - Output path: `Assets/Sprites/Mannequin/mannequin_sheet.png`

Tips:
- Use `Layer Transparency` and onion skinning to compare frames.
- Use `Edit -> Copy` / `Paste` to correct a single frame from a better frame.
- If frames are different sizes, expand canvas for each frame to the same size before export.

PixelLab source reuse:
- Put concept/source sheets in `Assets/Sprites/Concepts/`.
- Install the image crop dependency once with `python -m pip install -r Scripts/Tools/requirements.txt`.
- Optional exact crop override:
  - PowerShell: `$env:PIXELLAB_REFERENCE_CROP="x,y,w,h"`
  - Example: `$env:PIXELLAB_REFERENCE_CROP="575,100,555,735"`
- Optional clean-sheet cell override:
  - PowerShell: `$env:PIXELLAB_REFERENCE_CELL="0,1,10,6"`
  - Format is `column,row,total_columns,total_rows`.
