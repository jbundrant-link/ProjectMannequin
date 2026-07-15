from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


ROOT = Path("Assets/Sprites/Ryu")
SOURCE = ROOT / "ryu_higgsfield_v4_sheet.png"
OUTPUT_DIR = ROOT / "Diagnostics" / "V4"
FRAME_SIZE = 320
SHEET_COLUMNS = 16
LABEL_HEIGHT = 28
BACKGROUND = (22, 25, 31, 255)


@dataclass(frozen=True)
class Preview:
    name: str
    row: int
    frame_count: int
    duration_ms: int


PREVIEWS = (
    Preview("idle_8_frames", 0, 8, 190),
    Preview("walk_11_frames", 1, 11, 80),
    Preview("dash_9_frames", 2, 9, 55),
    Preview("jump_12_frames", 3, 12, 75),
    Preview("standing_punches_10_frames", 4, 10, 90),
    Preview("standing_kicks_8_frames", 5, 8, 95),
    Preview("hadouken_3_frames", 11, 3, 217),
    Preview("shoryuken_6_frames", 12, 6, 117),
    Preview("tatsumaki_12_frames", 13, 12, 67),
)


def main() -> int:
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        print("ERROR: Pillow is required.")
        return 1

    if not SOURCE.exists():
        print(f"ERROR: Missing V4 sprite sheet: {SOURCE}")
        return 2

    atlas = Image.open(SOURCE).convert("RGBA")
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for preview in PREVIEWS:
        frames = [
            make_preview_frame(atlas, preview, column, Image, ImageDraw)
            for column in range(preview.frame_count)
        ]
        output = OUTPUT_DIR / f"{preview.name}.gif"
        frames[0].save(
            output,
            save_all=True,
            append_images=frames[1:],
            duration=preview.duration_ms,
            loop=0,
            disposal=2,
        )
        print(
            f"Saved {output} "
            f"({preview.frame_count} frames at {preview.duration_ms} ms)"
        )

    return 0


def make_preview_frame(atlas, preview, column, image_module, draw_module):
    left = column * FRAME_SIZE
    top = preview.row * FRAME_SIZE
    sprite = atlas.crop(
        (left, top, left + FRAME_SIZE, top + FRAME_SIZE)
    )
    canvas = image_module.new(
        "RGBA",
        (FRAME_SIZE, FRAME_SIZE + LABEL_HEIGHT),
        BACKGROUND,
    )
    canvas.alpha_composite(sprite, (0, LABEL_HEIGHT))
    draw = draw_module.Draw(canvas)
    draw.text(
        (8, 8),
        f"{preview.name.replace('_', ' ')} | {column + 1}/{preview.frame_count}",
        fill=(245, 247, 250, 255),
    )
    draw.line(
        (0, LABEL_HEIGHT + 312, FRAME_SIZE, LABEL_HEIGHT + 312),
        fill=(91, 213, 255, 180),
        width=1,
    )
    return canvas.convert("P", palette=image_module.Palette.ADAPTIVE)


if __name__ == "__main__":
    raise SystemExit(main())
