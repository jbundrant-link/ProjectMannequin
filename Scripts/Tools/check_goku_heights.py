#!/usr/bin/env python3
"""Feet-aligned idle strip across all 12 Goku forms to verify equal heights,
plus a specials strip to verify no baked-in energy remains.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

CW, CH, COLS = 384, 320, 8
REVIEW = Path("Assets/Sprites/Goku/Diagnostics/Review")
FORMS = [
    ("base", ""), ("kaioken", "_kaioken"), ("false_super", "_false_super"),
    ("ss1", "_ss1"), ("ss2", "_ss2"), ("ss3", "_ss3"), ("ss4", "_ss4"),
    ("god", "_god"), ("blue", "_blue"), ("blue_kaioken", "_blue_kaioken"),
    ("ui_sign", "_ui_sign"), ("instinct", "_instinct"),
]


def frame(atlas: Image.Image, idx: int) -> Image.Image:
    c, r = idx % COLS, idx // COLS
    return atlas.crop((c * CW, r * CH, (c + 1) * CW, (r + 1) * CH))


def checker(step: int = 16) -> Image.Image:
    b = Image.new("RGBA", (CW, CH), (60, 60, 68, 255))
    d = ImageDraw.Draw(b)
    for y in range(0, CH, step):
        for x in range(0, CW, step):
            if (x // step + y // step) % 2 == 0:
                d.rectangle((x, y, x + step, y + step), fill=(80, 80, 90, 255))
    return b


def strip(get_cell, out_name: str, title: str) -> None:
    scale = 0.6
    cw, ch = int(CW * scale), int(CH * scale)
    pad, label = 6, 20
    canvas = Image.new("RGBA", (pad + len(FORMS) * (cw + pad), label + ch + pad), (20, 20, 26, 255))
    d = ImageDraw.Draw(canvas)
    d.text((pad, 5), title, fill=(230, 230, 240, 255))
    for i, (name, suf) in enumerate(FORMS):
        cell = get_cell(name, suf)
        bg = checker(); bg.alpha_composite(cell)
        gd = ImageDraw.Draw(bg)
        gd.line((0, 312, CW, 312), fill=(255, 80, 80, 220), width=2)  # ground baseline
        gd.text((5, 5), name, fill=(255, 255, 120, 255))
        canvas.alpha_composite(bg.resize((cw, ch), Image.LANCZOS), (pad + i * (cw + pad), label))
    canvas.convert("RGB").save(REVIEW / out_name)
    print("Wrote", REVIEW / out_name, canvas.size)


def movement_cell(name, suf):
    at = Image.open(f"Assets/Sprites/Goku/goku_astral{suf}_higgsfield_v1_sheet.png").convert("RGBA")
    return frame(at, 2)


def specials_cell(name, suf):
    at = Image.open(f"Assets/Sprites/Goku/goku_astral_{name}_specials_higgsfield_v1_sheet.png").convert("RGBA")
    return frame(at, 24)  # Kamehameha firing pose


if __name__ == "__main__":
    strip(movement_cell, "HEIGHT_check_all_idles.png", "IDLE (feet-aligned, red=baseline) - heights should match")
    strip(specials_cell, "ENERGY_check_all_kamehameha.png", "KAMEHAMEHA pose - hands should be EMPTY (no beam/orb)")
