from __future__ import annotations

from pathlib import Path


SOURCE_DIR = Path("Assets/Sprites/Concepts/Ryu")
OUTPUT_DIR = Path("Assets/Sprites/Ryu/Higgsfield/References")
RANGES = (
    (61, 119),
    (120, 179),
    (180, 239),
    (240, 299),
    (300, 359),
    (360, 389),
)
TILE_SIZE = 180
LABEL_HEIGHT = 22
COLUMNS = 10
PADDING = 8


def main() -> int:
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        print("ERROR: Pillow is required.")
        return 1

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    for start, end in RANGES:
        frames = []
        for index in range(start, end + 1):
            path = SOURCE_DIR / f"MVC2_Ryu_{index}.png"
            if not path.exists():
                print(f"ERROR: Missing source frame: {path}")
                return 2
            frames.append((index, prepare_frame(path, Image)))

        rows = (len(frames) + COLUMNS - 1) // COLUMNS
        sheet = Image.new(
            "RGBA",
            (COLUMNS * TILE_SIZE, rows * (TILE_SIZE + LABEL_HEIGHT)),
            (24, 27, 33, 255),
        )
        draw = ImageDraw.Draw(sheet)
        for position, (index, frame) in enumerate(frames):
            column = position % COLUMNS
            row = position // COLUMNS
            x = column * TILE_SIZE
            y = row * (TILE_SIZE + LABEL_HEIGHT)
            sheet.alpha_composite(frame, (x, y + LABEL_HEIGHT))
            draw.text((x + 7, y + 4), str(index), fill=(228, 235, 242, 255))

        output = OUTPUT_DIR / f"ryu_frames_{start:03d}_{end:03d}.png"
        sheet.save(output)
        print(f"Saved {output}")

    return 0


def prepare_frame(path: Path, image_module):
    with image_module.open(path) as source_image:
        source = source_image.convert("RGBA")

    pixel_reader = getattr(source, "get_flattened_data", source.getdata)
    pixels = []
    for red, green, blue, alpha in pixel_reader():
        is_magenta_key = red >= 205 and blue >= 165 and green <= 105
        pixels.append((red, green, blue, 0 if is_magenta_key else alpha))
    source.putdata(pixels)

    alpha_box = source.getchannel("A").getbbox()
    tile = image_module.new("RGBA", (TILE_SIZE, TILE_SIZE), (0, 0, 0, 0))
    if alpha_box is None:
        return tile

    content = source.crop(alpha_box)
    scale = min(
        (TILE_SIZE - PADDING * 2) / content.width,
        (TILE_SIZE - PADDING * 2) / content.height,
    )
    resized = content.resize(
        (
            max(1, round(content.width * scale)),
            max(1, round(content.height * scale)),
        ),
        image_module.Resampling.NEAREST,
    )
    x = (TILE_SIZE - resized.width) // 2
    y = TILE_SIZE - PADDING - resized.height
    tile.alpha_composite(resized, (x, y))
    return tile


if __name__ == "__main__":
    raise SystemExit(main())
