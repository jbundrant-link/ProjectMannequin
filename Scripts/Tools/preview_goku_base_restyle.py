#!/usr/bin/env python3
"""Preview the restyled Goku BASE form without touching runtime atlases.

Processes the regenerated base source sheets through the shared normalization
pipeline into preview atlases, then builds an old-vs-new comparison at the
real runtime cell size (384x320) so the quality change is visible in context.
"""

from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
import process_goku_higgsfield_sprites as g  # noqa: E402

REVIEW = Path("Assets/Sprites/Goku/Diagnostics/Review")
REVIEW.mkdir(parents=True, exist_ok=True)

NEW_CORE = Path("Assets/Sprites/Goku/goku_core_actions_higgsfield_v2_TEST.png")
NEW_POSTURE = Path("Assets/Sprites/Goku/goku_posture_normals_higgsfield_v2_TEST.png")
NEW_SPECIALS = Path("Assets/Sprites/Goku/goku_base_specials_higgsfield_v3_TEST.png")

PREVIEW_MOVEMENT = REVIEW / "PREVIEW_astral_movement_v2.png"
PREVIEW_SPECIALS = REVIEW / "PREVIEW_astral_specials_v2.png"

CURRENT_MOVEMENT = Path("Assets/Sprites/Goku/goku_astral_higgsfield_v1_sheet.png")


def build_previews() -> None:
    g.write_movement_atlas(NEW_CORE, NEW_POSTURE, PREVIEW_MOVEMENT)
    g.write_single_atlas(
        NEW_SPECIALS,
        PREVIEW_SPECIALS,
        rows=8,
        occupied_frames=64,
        blank_frames=g.INTENTIONALLY_BLANK_SPECIAL_FRAMES,
    )


def cell(atlas: Image.Image, frame_index: int) -> Image.Image:
    column = frame_index % g.COLUMNS
    row = frame_index // g.COLUMNS
    box = (
        column * g.CELL_WIDTH,
        row * g.CELL_HEIGHT,
        (column + 1) * g.CELL_WIDTH,
        (row + 1) * g.CELL_HEIGHT,
    )
    return atlas.crop(box)


def checker(size: tuple[int, int], step: int = 16) -> Image.Image:
    base = Image.new("RGBA", size, (70, 70, 78, 255))
    draw = ImageDraw.Draw(base)
    for y in range(0, size[1], step):
        for x in range(0, size[0], step):
            if (x // step + y // step) % 2 == 0:
                draw.rectangle((x, y, x + step, y + step), fill=(90, 90, 100, 255))
    return base


def comparison() -> None:
    old = Image.open(CURRENT_MOVEMENT).convert("RGBA")
    new = Image.open(PREVIEW_MOVEMENT).convert("RGBA")
    # Representative movement frames: idle, walk-ish, standing normals, air.
    frames = [0, 2, 30, 38, 50, 58, 91, 111]
    scale = 0.62
    cw = int(g.CELL_WIDTH * scale)
    ch = int(g.CELL_HEIGHT * scale)
    pad = 10
    label = 34
    cols = len(frames)
    canvas_w = pad + cols * (cw + pad)
    canvas_h = label + pad + ch + pad + label + pad + ch + pad
    canvas = Image.new("RGBA", (canvas_w, canvas_h), (24, 24, 30, 255))
    draw = ImageDraw.Draw(canvas)
    draw.text((pad, 8), "CURRENT runtime (v1)", fill=(255, 170, 170, 255))
    draw.text((pad, label + pad + ch + pad), "NEW restyle (preview)", fill=(170, 255, 170, 255))
    for index, frame in enumerate(frames):
        x = pad + index * (cw + pad)
        old_cell = cell(old, frame).resize((cw, ch), Image.LANCZOS)
        new_cell = cell(new, frame).resize((cw, ch), Image.LANCZOS)
        bg_old = checker((cw, ch)); bg_old.alpha_composite(old_cell)
        bg_new = checker((cw, ch)); bg_new.alpha_composite(new_cell)
        canvas.alpha_composite(bg_old, (x, label))
        canvas.alpha_composite(bg_new, (x, label + pad + ch + pad + label))
    canvas.convert("RGB").save(REVIEW / "COMPARISON_runtime_old_vs_new.png")
    print("Wrote", REVIEW / "COMPARISON_runtime_old_vs_new.png", canvas.size)


if __name__ == "__main__":
    build_previews()
    comparison()
