from __future__ import annotations

from pathlib import Path


ROOT = Path("Assets/Sprites/Ryu")
SOURCE = ROOT / "ryu_higgsfield_v3_sheet.png"
OUTPUT_DIR = ROOT / "Diagnostics"
OUTPUT = OUTPUT_DIR / "ryu_move_scale_comparison_v3.png"
FRAME_SIZE = 320
SHEET_COLUMNS = 10
GROUND_BASELINE = 312
FRAMES = (
    ("Idle", 0),
    ("Light punch", 41),
    ("Heavy kick", 49),
    ("Crouch", 60),
    ("Low kick", 66),
    ("Sweep", 68),
    ("Air guard", 70),
    ("Air kick", 76),
    ("Projectile", 84),
    ("Uppercut", 94),
    ("Spin kick", 103),
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
    label_height = 48
    canvas = Image.new(
        "RGBA",
        (FRAME_SIZE * len(FRAMES), FRAME_SIZE + label_height),
        (22, 25, 31, 255),
    )
    draw = ImageDraw.Draw(canvas)

    for index, (label, frame_index) in enumerate(FRAMES):
        column = frame_index % SHEET_COLUMNS
        row = frame_index // SHEET_COLUMNS
        frame = sheet.crop(
            (
                column * FRAME_SIZE,
                row * FRAME_SIZE,
                (column + 1) * FRAME_SIZE,
                (row + 1) * FRAME_SIZE,
            )
        )
        x = index * FRAME_SIZE
        canvas.alpha_composite(frame, (x, label_height))
        draw.line(
            (
                x,
                label_height + GROUND_BASELINE,
                x + FRAME_SIZE,
                label_height + GROUND_BASELINE,
            ),
            fill=(91, 213, 255, 190),
            width=1,
        )
        bounds = frame.getchannel("A").getbbox()
        dimensions = "empty" if bounds is None else (
            f"{bounds[2] - bounds[0]}x{bounds[3] - bounds[1]}"
        )
        draw.text((x + 8, 8), label, fill=(245, 247, 250, 255))
        draw.text((x + 8, 26), dimensions, fill=(155, 184, 205, 255))

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    canvas.save(OUTPUT)
    print(f"Saved {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
