"""Composite per-animation sub-sheets into a final 10x9 enemy sprite sheet.

Usage:
    python Scripts/Tools/compose_enemy_sheet.py <enemy_id> [green|magenta]

Expects sub-sheets at:
    Assets/Sprites/Enemies/<enemy_id>_src_idle.png      (2 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_walk.png      (4 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_dash.png      (3 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_jump.png      (2 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_attacks.png   (5 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_misc.png      (4 cols x 2 rows)

Sub-sheets are composited into a 10-col x 9-row 2560x2304 sheet at:
    Assets/Sprites/Enemies/<enemy_id>_higgsfield_v1.png

Rows 6-8 (crouch attacks, air attacks, uppercut) are left transparent;
basic enemies never animate those frames.
"""
from __future__ import annotations

import sys
from pathlib import Path

FRAME = 256
SHEET_COLS = 10
SHEET_ROWS = 9
BASELINE = 248
PAD = 6
MAX_UPSCALE = 1.7
ENEMY_DIR = Path("Assets/Sprites/Enemies")

# Each entry: (animation_name, src_cols, src_rows, target_sheet_row)
ANIMATIONS = [
    ("idle",    2, 2, 0),
    ("walk",    4, 2, 1),
    ("dash",    3, 2, 2),
    ("jump",    2, 2, 3),
    ("attacks", 5, 2, 4),
    ("misc",    4, 2, 5),
]


def _threshold(channel, cutoff):
    return channel.point(lambda v, c=cutoff: 255 if v > c else 0)


def _and(*masks):
    from PIL import ImageChops
    result = masks[0]
    for m in masks[1:]:
        result = ImageChops.multiply(result, m)
    return result


def build_alpha(rgb, bg):
    from PIL import ImageChops
    r, g, b = rgb.split()
    if bg == "magenta":
        is_bg = _and(
            _threshold(r, 100), _threshold(b, 100),
            _threshold(ImageChops.subtract(r, g), 25),
            _threshold(ImageChops.subtract(b, g), 25),
        )
    else:
        is_bg = _and(
            _threshold(g, 100),
            _threshold(ImageChops.subtract(g, r), 25),
            _threshold(ImageChops.subtract(g, b), 25),
        )
    from PIL import ImageChops as ic
    return ic.invert(is_bg)


def despill(rgba, bg):
    from PIL import Image, ImageChops
    r, g, b, a = rgba.split()
    if bg == "magenta":
        g10 = g.point(lambda v: min(255, v + 10))
        return Image.merge("RGBA", (ImageChops.darker(r, g10), g, ImageChops.darker(b, g10), a))
    max_rb = ImageChops.lighter(r, b)
    max_rb8 = max_rb.point(lambda v: min(255, v + 8))
    return Image.merge("RGBA", (r, ImageChops.darker(g, max_rb8), b, a))


def extract_frames(path: Path, src_cols: int, src_rows: int, bg: str):
    """Open a sub-sheet, chroma-key it, return list of normalized 256x256 RGBA frames."""
    from PIL import Image
    im = Image.open(path).convert("RGB")
    alpha = build_alpha(im, bg)
    rgba = im.convert("RGBA")
    rgba.putalpha(alpha)
    rgba = despill(rgba, bg)

    W, H = rgba.size
    cell_w = W / src_cols
    cell_h = H / src_rows
    inset = 4

    frames = []
    for row in range(src_rows):
        for col in range(src_cols):
            box = (
                int(col * cell_w) + inset,
                int(row * cell_h) + inset,
                int((col + 1) * cell_w) - inset,
                int((row + 1) * cell_h) - inset,
            )
            if box[2] <= box[0] or box[3] <= box[1]:
                frames.append(Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0)))
                continue
            cell = rgba.crop(box)
            bbox = cell.getbbox()
            out = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
            if bbox:
                fw, fh = bbox[2] - bbox[0], bbox[3] - bbox[1]
                if fw >= 8 and fh >= 8:
                    scale = min((FRAME - 2 * PAD) / fw, (BASELINE - PAD) / fh, MAX_UPSCALE)
                    nw, nh = max(1, int(fw * scale)), max(1, int(fh * scale))
                    fig = cell.crop(bbox).resize((nw, nh), Image.LANCZOS)
                    ox = (FRAME - nw) // 2
                    oy = BASELINE - nh
                    out.alpha_composite(fig, (ox, oy))
            frames.append(out)
    return frames


def main() -> int:
    from PIL import Image

    if len(sys.argv) < 2:
        print("usage: compose_enemy_sheet.py <enemy_id> [green|magenta]")
        return 2

    enemy_id = sys.argv[1]
    bg = sys.argv[2] if len(sys.argv) > 2 else "green"

    sheet = Image.new("RGBA", (FRAME * SHEET_COLS, FRAME * SHEET_ROWS), (0, 0, 0, 0))
    filled_total = 0

    for anim_name, src_cols, src_rows, target_row in ANIMATIONS:
        src_path = ENEMY_DIR / f"{enemy_id}_src_{anim_name}.png"
        if not src_path.exists():
            print(f"  SKIP {anim_name} (not found: {src_path})")
            continue

        frames = extract_frames(src_path, src_cols, src_rows, bg)
        filled = sum(1 for f in frames if f.getbbox() is not None)

        for col in range(SHEET_COLS):
            frame = frames[min(col, len(frames) - 1)].copy()
            sheet.alpha_composite(frame, (col * FRAME, target_row * FRAME))

        filled_total += filled
        total_cells = src_cols * src_rows
        print(f"  row {target_row} ({anim_name}): {filled}/{total_cells} frames")

    out_path = ENEMY_DIR / f"{enemy_id}_higgsfield_v1.png"
    sheet.save(out_path)
    print(f"SAVED {out_path} ({sheet.width}x{sheet.height}) filled={filled_total}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
