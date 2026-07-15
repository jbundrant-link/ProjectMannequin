"""Process a Higgsfield intro pose sheet into a clean Godot atlas.

Pure-PIL (no numpy). Chroma-keys the green background, slices the grid,
uniformly scales every frame by ONE global factor (so the character never
grows/shrinks between frames), and bottom-anchors + horizontally-centers each
pose into a square cell. Paths/layout are CLI args (defaults = player mannequin).

Temp dev tool (safe to delete after the atlas is confirmed).
"""
import argparse

from PIL import Image, ImageChops, ImageOps

_parser = argparse.ArgumentParser()
_parser.add_argument("--raw", default="Assets/Sprites/Mannequin/Diagnostics/_intro_raw.png")
_parser.add_argument("--out", default="Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png")
_parser.add_argument("--preview", default="Assets/Sprites/Mannequin/Diagnostics/_intro_atlas_preview.png")
_parser.add_argument("--cols", type=int, default=4)
_parser.add_argument("--rows", type=int, default=2)
_parser.add_argument("--cell", type=int, default=256)
_parser.add_argument("--baseline", type=int, default=250, help="feet y within each cell")
_parser.add_argument("--maxh", type=int, default=244, help="tallest pose scaled to this")
_args = _parser.parse_args()

RAW = _args.raw
OUT = _args.out
PREVIEW = _args.preview
COLS, ROWS, CELL = _args.cols, _args.rows, _args.cell
BASELINE = _args.baseline
MAX_FRAME_HEIGHT = _args.maxh

im = Image.open(RAW).convert("RGBA")
r, g, b, _ = im.split()

# Chroma key: background is bright green (~#00E000); mannequin is tan/burgundy.
gr = ImageChops.subtract(g, r)
gb = ImageChops.subtract(g, b)
mask_gr = gr.point(lambda v: 255 if v > 40 else 0)
mask_gb = gb.point(lambda v: 255 if v > 40 else 0)
mask_g = g.point(lambda v: 255 if v > 100 else 0)
green = ImageChops.multiply(ImageChops.multiply(mask_gr, mask_gb), mask_g)
alpha = ImageOps.invert(green)  # 0 where green, 255 elsewhere

# Green despill so anti-aliased edges don't leave a halo: clamp g <= (r+b)/2+24.
half = ImageChops.add(r.point(lambda v: v // 2), b.point(lambda v: v // 2))
limit = half.point(lambda v: min(255, v + 24))
g2 = ImageChops.darker(g, limit)

keyed = Image.merge("RGBA", (r, g2, b, alpha))
# Hard-cut RGB to transparent where keyed out so downscaling can't bleed green.
keyed = Image.composite(keyed, Image.new("RGBA", im.size, (0, 0, 0, 0)), alpha)

W, H = im.size
cw, ch = W // COLS, H // ROWS

frames = []
for row in range(ROWS):
    for col in range(COLS):
        box = (col * cw, row * ch, (col + 1) * cw, (row + 1) * ch)
        cell = keyed.crop(box)
        bbox = alpha.crop(box).getbbox()
        frames.append(cell.crop(bbox) if bbox else None)

valid = [f for f in frames if f is not None]
scale = MAX_FRAME_HEIGHT / max(f.height for f in valid)

atlas = Image.new("RGBA", (COLS * CELL, ROWS * CELL), (0, 0, 0, 0))
report = []
for i, f in enumerate(frames):
    if f is None:
        report.append(0)
        continue
    nw, nh = max(1, round(f.width * scale)), max(1, round(f.height * scale))
    scaled = f.resize((nw, nh), Image.LANCZOS)
    col, row = i % COLS, i // COLS
    x = col * CELL + (CELL - nw) // 2
    y = row * CELL + BASELINE - nh
    atlas.alpha_composite(scaled, (x, y))
    report.append(nh)

atlas.save(OUT)

# Checkerboard preview so transparency is visible.
bg = Image.new("RGBA", atlas.size, (60, 60, 68, 255))
d = 16
px = bg.load()
for yy in range(atlas.height):
    for xx in range(atlas.width):
        if (xx // d + yy // d) % 2 == 0:
            px[xx, yy] = (86, 86, 94, 255)
bg.alpha_composite(atlas)
bg.convert("RGB").save(PREVIEW)

print("saved", OUT, atlas.size, "scale", round(scale, 3), "frame_heights", report)
