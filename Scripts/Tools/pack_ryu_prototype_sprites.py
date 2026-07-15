from __future__ import annotations

from pathlib import Path


ROOT = Path("Assets/Sprites")
SOURCE_DIR = ROOT / "Concepts" / "Ryu"
OUTPUT_DIR = ROOT / "Ryu"
OUTPUT_SHEET = OUTPUT_DIR / "ryu_mvc2_prototype_sheet.png"
FRAME_SIZE = 256
FRAME_PADDING = 4
GROUND_BASELINE = 248
SOURCE_SCALE = 2.25

# The extracted archive contains raw SFF frames and effects without action metadata.
# These explicit selections make the personal-use prototype conversion reproducible.
ROWS = (
    (61, 62, 63, 64, 65, 66, 67, 68, 67, 66),  # idle
    (70, 71, 72, 73, 74, 75, 76, 77, 78, 79),  # walk
    (81, 82, 83, 84, 85, 86, 87, 88, 89, 89),  # dash
    (90, 91, 92, 93, 94, 95, 96, 100, 101, 96),  # jump
    (61, 200, 203, 206, 207, 210, 212, 215, 229, 307),  # normals
    (340, 341, 342, 160, 161, 162, 363, 370, 373, 374),  # reactions
    (218, 219, 220, 221, 222, 223, 227, 228, 229, 230),  # crouch
    (238, 239, 240, 241, 242, 243, 247, 248, 249, 250),  # air
    (300, 301, 307, 308, 309, 313, 315, 317, 319, 320),  # projectile / uppercut
)


def main() -> int:
    try:
        from PIL import Image
    except ImportError:
        print("ERROR: Pillow is required. Install it with: python -m pip install pillow")
        return 1

    required_indices = sorted({index for row in ROWS for index in row})
    missing = [
        index
        for index in required_indices
        if not source_path(index).exists()
    ]
    if missing:
        print(f"ERROR: Missing source frames: {missing}")
        return 2

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    atlas = Image.new(
        "RGBA",
        (FRAME_SIZE * 10, FRAME_SIZE * len(ROWS)),
        (0, 0, 0, 0),
    )

    for row_index, source_indices in enumerate(ROWS):
        for column, source_index in enumerate(source_indices):
            frame = prepare_frame(source_path(source_index), Image)
            atlas.alpha_composite(
                frame,
                (column * FRAME_SIZE, row_index * FRAME_SIZE),
            )

    atlas.save(OUTPUT_SHEET)
    print(f"Saved {OUTPUT_SHEET} ({atlas.width}x{atlas.height})")
    return 0


def source_path(index: int) -> Path:
    return SOURCE_DIR / f"MVC2_Ryu_{index}.png"


def prepare_frame(path: Path, image_module):
    with image_module.open(path) as source_image:
        source = source_image.convert("RGBA")

    pixels = []
    pixel_reader = getattr(source, "get_flattened_data", source.getdata)
    for red, green, blue, alpha in pixel_reader():
        is_magenta_key = red >= 205 and blue >= 165 and green <= 105
        pixels.append((red, green, blue, 0 if is_magenta_key else alpha))
    source.putdata(pixels)

    alpha_box = source.getchannel("A").getbbox()
    canvas = image_module.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
    if alpha_box is None:
        return canvas

    content = source.crop(alpha_box)
    scale = min(
        SOURCE_SCALE,
        (FRAME_SIZE - FRAME_PADDING * 2) / content.width,
        (FRAME_SIZE - FRAME_PADDING * 2) / content.height,
    )
    resized = content.resize(
        (
            max(1, round(content.width * scale)),
            max(1, round(content.height * scale)),
        ),
        image_module.Resampling.NEAREST,
    )
    x = (FRAME_SIZE - resized.width) // 2
    y = GROUND_BASELINE - resized.height
    canvas.alpha_composite(resized, (x, y))
    return canvas


if __name__ == "__main__":
    raise SystemExit(main())
