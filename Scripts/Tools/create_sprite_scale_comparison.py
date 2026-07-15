from __future__ import annotations

from pathlib import Path


ROOT = Path("Assets/Sprites/Mannequin")
SOURCE = ROOT / "mannequin_sheet_higgsfield_v1.png"
OUTPUT_DIR = ROOT / "Diagnostics"
OUTPUT = OUTPUT_DIR / "mannequin_scale_comparison.png"
FRAME_SIZE = 256
FRAMES = (
    ("Idle", 0),
    ("Held crouch", 60),
    ("Crouch jab", 61),
    ("Crouch sweep", 66),
    ("Air jab", 71),
    ("Air kick", 74),
)


def main() -> int:
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        print("ERROR: Pillow is required.")
        return 1

    if not SOURCE.exists():
        print(f"ERROR: Missing sprite sheet: {SOURCE}")
        return 2

    sheet = Image.open(SOURCE).convert("RGBA")
    panel_width = FRAME_SIZE
    label_height = 44
    canvas = Image.new(
        "RGBA",
        (panel_width * len(FRAMES), FRAME_SIZE + label_height),
        (22, 25, 31, 255),
    )
    draw = ImageDraw.Draw(canvas)

    for index, (label, frame_index) in enumerate(FRAMES):
        column = frame_index % 10
        row = frame_index // 10
        frame = sheet.crop(
            (
                column * FRAME_SIZE,
                row * FRAME_SIZE,
                (column + 1) * FRAME_SIZE,
                (row + 1) * FRAME_SIZE,
            )
        )
        x = index * panel_width
        canvas.alpha_composite(frame, (x, label_height))
        draw.line(
            (x, label_height + 248, x + FRAME_SIZE, label_height + 248),
            fill=(91, 213, 255, 180),
            width=1,
        )
        bounds = frame.getchannel("A").getbbox()
        dimensions = "empty" if bounds is None else f"{bounds[2] - bounds[0]}x{bounds[3] - bounds[1]}"
        draw.text((x + 8, 7), label, fill=(245, 247, 250, 255))
        draw.text((x + 8, 23), dimensions, fill=(155, 184, 205, 255))

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    canvas.save(OUTPUT)
    print(f"Saved {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
