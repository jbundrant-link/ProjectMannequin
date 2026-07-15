"""Post-process a Higgsfield enemy restyle into a clean transparent 10x9 sheet.

The enemy sheets are produced by restyling the normalized mannequin pose
template (see enemy_sprite_pipeline notes). Because the template is already a
clean 10x9 grid, the restyle output stays close to that grid; this script
chroma-keys the flat background to transparency and re-normalizes every cell
into a 256px frame that is bottom-anchored to the mannequin ground baseline so
the result drops straight into the existing "mannequin" animation profile.

Usage:
    python Scripts/Tools/process_enemy_sheet.py <input.png> <output.png> [green|magenta]

Use a magenta background/key for green-skinned characters (e.g. Saibaman) so
the chroma key does not eat the character; use green for everyone else.
"""
from __future__ import annotations

import sys

FRAME = 256
COLS = 10
ROWS = 9
BASELINE = 248  # feet sit here inside each 256px cell (matches mannequin sheet)
PAD = 6
MAX_UPSCALE = 1.7


def _threshold(channel, cutoff):
    return channel.point(lambda v, c=cutoff: 255 if v > c else 0)


def _and(*masks):
    from PIL import ImageChops

    result = masks[0]
    for mask in masks[1:]:
        result = ImageChops.multiply(result, mask)  # 0/255 masks -> logical AND
    return result


def build_alpha(rgb, bg):
    from PIL import ImageChops

    r, g, b = rgb.split()
    if bg == "magenta":
        # background = high red + high blue + low green
        is_bg = _and(
            _threshold(r, 100),
            _threshold(b, 100),
            _threshold(ImageChops.subtract(r, g), 25),
            _threshold(ImageChops.subtract(b, g), 25),
        )
    else:
        # background = green dominates both red and blue
        is_bg = _and(
            _threshold(g, 100),
            _threshold(ImageChops.subtract(g, r), 25),
            _threshold(ImageChops.subtract(g, b), 25),
        )
    from PIL import ImageChops as _ic

    return _ic.invert(is_bg)


def despill(rgba, bg):
    """Pull residual background fringe out of kept pixels."""
    from PIL import Image, ImageChops

    r, g, b, a = rgba.split()
    if bg == "magenta":
        # reduce red and blue fringe where they exceed green
        g10 = g.point(lambda v: min(255, v + 10))
        r2 = ImageChops.darker(r, g10)  # min(r, g+10)
        b2 = ImageChops.darker(b, g10)  # min(b, g+10)
        return Image.merge("RGBA", (r2, g, b2, a))
    # green: reduce green only where it exceeds both red and blue (halo),
    # leaving teal/cyan (green ~= blue) intact.
    max_rb = ImageChops.lighter(r, b)  # max(r, b)
    max_rb8 = max_rb.point(lambda v: min(255, v + 8))
    g2 = ImageChops.darker(g, max_rb8)  # min(g, max(r,b)+8)
    return Image.merge("RGBA", (r, g2, b, a))


def main() -> int:
    from PIL import Image

    if len(sys.argv) < 3:
        print("usage: process_enemy_sheet.py <input.png> <output.png> [green|magenta]")
        return 2
    src, dst = sys.argv[1], sys.argv[2]
    bg = sys.argv[3] if len(sys.argv) > 3 else "green"

    im = Image.open(src).convert("RGB")
    width, height = im.size
    alpha = build_alpha(im, bg)
    rgba = im.convert("RGBA")
    rgba.putalpha(alpha)
    rgba = despill(rgba, bg)

    cell_w = width / COLS
    cell_h = height / ROWS
    sheet = Image.new("RGBA", (FRAME * COLS, FRAME * ROWS), (0, 0, 0, 0))

    INSET = 4   # shrink each cell crop by this many px on each side to avoid
                # picking up neighbor-bleed pixels before chroma-keying
    SKIP_CLIP_FRAC = 0.65  # if the figure bbox covers > this fraction of the
                            # inset-cell, the figure is probably a bleed from a
                            # neighboring cell that was cut at the boundary;
                            # skip only those cells.
    filled = 0
    skipped_clip = 0
    for row in range(ROWS):
        for col in range(COLS):
            x0 = int(col * cell_w)
            y0 = int(row * cell_h)
            x1 = int((col + 1) * cell_w)
            y1 = int((row + 1) * cell_h)
            # Inset to avoid border bleed from neighboring cells
            crop_box = (x0 + INSET, y0 + INSET, x1 - INSET, y1 - INSET)
            if crop_box[2] <= crop_box[0] or crop_box[3] <= crop_box[1]:
                continue
            cell = rgba.crop(crop_box)
            cw = cell.width
            ch = cell.height
            bbox = cell.getbbox()
            if not bbox:
                continue
            fig_w = bbox[2] - bbox[0]
            fig_h = bbox[3] - bbox[1]
            if fig_w < 8 or fig_h < 8:
                continue
            # Detect bleed: figure fills nearly the full inset cell on the side
            # that touches the original cell boundary.
            edge_fill_x = fig_w / cw
            edge_fill_y = fig_h / ch
            if edge_fill_x > SKIP_CLIP_FRAC and edge_fill_y > SKIP_CLIP_FRAC:
                skipped_clip += 1
                continue
            fig = cell.crop(bbox)
            scale = min((FRAME - 2 * PAD) / fig_w, (BASELINE - PAD) / fig_h, MAX_UPSCALE)
            new_w = max(1, int(fig_w * scale))
            new_h = max(1, int(fig_h * scale))
            fig = fig.resize((new_w, new_h), Image.LANCZOS)
            ox = col * FRAME + (FRAME - new_w) // 2
            oy = row * FRAME + BASELINE - new_h
            sheet.alpha_composite(fig, (ox, oy))
            filled += 1

    sheet.save(dst)
    print(f"SAVED {dst} {sheet.size} cells={filled}/{COLS * ROWS} skipped_edge_clip={skipped_clip} bg={bg}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
