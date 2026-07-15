#!/usr/bin/env python3
"""Build a 12-form verification contact sheet from the regenerated runtime
Goku atlases: one idle pose and one Kamehameha special per form, so per-form
hair/aura and the movement/specials frame mapping can be checked at a glance.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

CELL_W, CELL_H, COLUMNS = 384, 320, 8
REVIEW = Path("Assets/Sprites/Goku/Diagnostics/Review")

FORMS = [
    ("base", ""),
    ("kaioken", "_kaioken"),
    ("false_super", "_false_super"),
    ("ss1", "_ss1"),
    ("ss2", "_ss2"),
    ("ss3", "_ss3"),
    ("ss4", "_ss4"),
    ("god", "_god"),
    ("blue", "_blue"),
    ("blue_kaioken", "_blue_kaioken"),
    ("ui_sign", "_ui_sign"),
    ("instinct", "_instinct"),
]

IDLE_FRAME = 2          # movement atlas: early idle pose
KAMEHAMEHA_FRAME = 24   # specials atlas: mid Kamehameha


def grab(atlas_path: Path, frame: int) -> Image.Image:
    atlas = Image.open(atlas_path).convert("RGBA")
    col = frame % COLUMNS
    row = frame // COLUMNS
    return atlas.crop((col * CELL_W, row * CELL_H, (col + 1) * CELL_W, (row + 1) * CELL_H))


def checker(step: int = 16) -> Image.Image:
    base = Image.new("RGBA", (CELL_W, CELL_H), (64, 64, 72, 255))
    draw = ImageDraw.Draw(base)
    for y in range(0, CELL_H, step):
        for x in range(0, CELL_W, step):
            if (x // step + y // step) % 2 == 0:
                draw.rectangle((x, y, x + step, y + step), fill=(84, 84, 94, 255))
    return base


def main() -> None:
    scale = 0.5
    cw, ch = int(CELL_W * scale), int(CELL_H * scale)
    pad, label = 8, 26
    cols = 6
    rows = (len(FORMS) + cols - 1) // cols
    # Two stacked cells (idle + kamehameha) per form.
    block_h = label + ch + ch + pad
    canvas_w = pad + cols * (cw + pad)
    canvas_h = pad + rows * (block_h + pad)
    canvas = Image.new("RGBA", (canvas_w, canvas_h), (20, 20, 26, 255))
    draw = ImageDraw.Draw(canvas)
    for index, (name, suffix) in enumerate(FORMS):
        gx = pad + (index % cols) * (cw + pad)
        gy = pad + (index // cols) * (block_h + pad)
        move = REVIEW.parent.parent / f"goku_astral{suffix}_higgsfield_v1_sheet.png"
        spec = REVIEW.parent.parent / f"goku_astral_{name}_specials_higgsfield_v1_sheet.png"
        draw.text((gx, gy), name, fill=(230, 230, 240, 255))
        idle = checker(); idle.alpha_composite(grab(move, IDLE_FRAME).resize((cw, ch), Image.LANCZOS))
        kame = checker(); kame.alpha_composite(grab(spec, KAMEHAMEHA_FRAME).resize((cw, ch), Image.LANCZOS))
        canvas.alpha_composite(idle, (gx, gy + label))
        canvas.alpha_composite(kame, (gx, gy + label + ch))
    out = REVIEW / "VERIFY_all_forms.png"
    canvas.convert("RGB").save(out)
    print("Wrote", out, canvas.size)


if __name__ == "__main__":
    main()
