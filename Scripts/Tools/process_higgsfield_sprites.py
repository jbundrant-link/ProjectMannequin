from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path


ROOT = Path("Assets/Sprites/Mannequin")
OUTPUT_DIR = ROOT / "Higgsfield"
OUTPUT_SHEET = ROOT / "mannequin_sheet_higgsfield_v1.png"
FRAME_SIZE = 256
SHEET_COLUMNS = 10
SHEET_ROWS = 9
FRAME_PADDING = 4
GROUND_BASELINE = 248


@dataclass(frozen=True)
class SheetSpec:
    row: int
    name: str
    source: Path
    columns: int
    rows: int
    frame_count: int
    crop_margin: int = 8
    remove_guides: bool = False
    trim_to_content: bool = False
    segment_poses: bool = False
    strict_pose_count: bool = False
    key_white_background: bool = False
    output_scale: float = 1.0


SHEETS = (
    SheetSpec(0, "idle", ROOT / "higgsfield_idle_sheet_v1.png", 2, 2, 4, crop_margin=18),
    SheetSpec(1, "walk", ROOT / "higgsfield_walk_sheet_v1.png", 4, 2, 8),
    SheetSpec(2, "dash", ROOT / "higgsfield_dash_sheet_v1.png", 3, 2, 6),
    SheetSpec(3, "jump", ROOT / "higgsfield_jump_sheet_v1.png", 2, 2, 4, remove_guides=True),
    SheetSpec(4, "attacks", ROOT / "higgsfield_attacks_sheet_v1.png", 5, 2, 10, crop_margin=12),
    SheetSpec(5, "misc", ROOT / "higgsfield_misc_sheet_v1.png", 4, 2, 8),
    SheetSpec(
        6,
        "crouch_attacks",
        OUTPUT_DIR / "higgsfield_crouch_sheet_v2.png",
        4,
        2,
        8,
        crop_margin=10,
        trim_to_content=True,
        segment_poses=True,
        output_scale=0.84,
    ),
    SheetSpec(
        7,
        "air_attacks",
        OUTPUT_DIR / "higgsfield_air_sheet_v2.png",
        6,
        1,
        6,
        crop_margin=12,
        trim_to_content=True,
        segment_poses=True,
        key_white_background=True,
        output_scale=0.93,
    ),
    SheetSpec(
        8,
        "uppercut",
        OUTPUT_DIR / "higgsfield_special_sheet_v1.png",
        6,
        1,
        6,
        crop_margin=10,
        trim_to_content=True,
        segment_poses=True,
    ),
)


def main() -> int:
    try:
        from PIL import Image
    except ImportError:
        print("ERROR: Pillow is required.")
        print("Install it with: python -m pip install pillow")
        return 1

    missing = [str(spec.source) for spec in SHEETS if not spec.source.exists()]
    if missing:
        print("ERROR: Missing Higgsfield source sheets:")
        for path in missing:
            print(f"  {path}")
        return 2

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    final_sheet = Image.new(
        "RGBA",
        (FRAME_SIZE * SHEET_COLUMNS, FRAME_SIZE * SHEET_ROWS),
        (0, 0, 0, 0),
    )

    for spec in SHEETS:
        frames = process_sheet(spec, Image)
        for column in range(SHEET_COLUMNS):
            source_index = min(column, len(frames) - 1)
            frame = frames[source_index].copy()
            output_path = OUTPUT_DIR / f"row{spec.row}_{spec.name}_frame{column}.png"
            frame.save(output_path)
            final_sheet.alpha_composite(frame, (column * FRAME_SIZE, spec.row * FRAME_SIZE))

        print(f"Processed row {spec.row} ({spec.name}): {len(frames)} source frames")

    final_sheet.save(OUTPUT_SHEET)
    print(f"Saved {OUTPUT_SHEET} ({final_sheet.width}x{final_sheet.height})")
    return 0


def process_sheet(spec: SheetSpec, image_module) -> list:
    with image_module.open(spec.source) as source_image:
        source = source_image.convert("RGBA")

    if spec.segment_poses:
        frames = extract_segmented_poses(source, spec, image_module)
    else:
        frames = []
        for index in range(spec.frame_count):
            column = index % spec.columns
            row = index // spec.columns
            cell = crop_cell(source, column, row, spec)
            keyed = remove_background(cell, image_module, spec.key_white_background)
            if spec.remove_guides:
                keyed = remove_long_dark_guides(keyed, image_module)
            frames.append(keyed)

    if spec.trim_to_content:
        return normalize_trimmed_frames(frames, image_module, spec.output_scale)

    return [normalize_frame(frame, image_module) for frame in frames]


def extract_segmented_poses(source, spec: SheetSpec, image_module) -> list:
    from PIL import ImageChops, ImageDraw, ImageFilter

    keyed = remove_background(source, image_module, spec.key_white_background)
    alpha = keyed.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > 32 else 0)

    # Join tiny anti-aliased outline gaps without joining neighboring poses.
    working = mask.filter(ImageFilter.MaxFilter(9))
    components = []

    while True:
        bounds = working.getbbox()
        if bounds is None:
            break

        left, top, _, _ = bounds
        cropped = working.crop(bounds)
        pixel_reader = getattr(cropped, "get_flattened_data", cropped.getdata)
        pixels = bytes(pixel_reader())
        seed_index = next((index for index, value in enumerate(pixels) if value), None)
        if seed_index is None:
            break

        seed = (
            left + seed_index % cropped.width,
            top + seed_index // cropped.width,
        )
        labeled = working.copy()
        ImageDraw.floodfill(labeled, seed, 128, thresh=0)
        component_mask = labeled.point(lambda value: 255 if value == 128 else 0)
        ImageDraw.floodfill(working, seed, 0, thresh=0)

        component_bounds = component_mask.getbbox()
        if component_bounds is None:
            continue

        component_area = component_mask.histogram()[255]
        component_width = component_bounds[2] - component_bounds[0]
        component_height = component_bounds[3] - component_bounds[1]
        if component_area < 4_000 or component_width < 80 or component_height < 120:
            continue

        isolated = keyed.copy()
        isolated.putalpha(ImageChops.multiply(alpha, component_mask))
        components.append((isolated.crop(component_bounds), component_bounds, component_area))

    if len(components) < spec.frame_count:
        raise RuntimeError(
            f"{spec.name}: found {len(components)} complete poses, expected {spec.frame_count}"
        )
    if spec.strict_pose_count and len(components) != spec.frame_count:
        raise RuntimeError(
            f"{spec.name}: found {len(components)} complete poses, expected exactly "
            f"{spec.frame_count}"
        )

    # Motion accents can form extra components. Complete character poses dominate by area.
    detected_count = len(components)
    components = sorted(components, key=lambda item: item[2], reverse=True)[:spec.frame_count]
    components = sort_components_into_rows(components, spec.rows)
    print(
        f"  Segmented {len(components)} of {detected_count} complete poses "
        f"from {spec.source.name}; "
        "equal-grid slicing disabled"
    )
    return [component[0] for component in components]


def sort_components_into_rows(components: list, row_count: int) -> list:
    if row_count <= 1:
        return sorted(components, key=lambda item: item[1][0])

    ordered_by_height = sorted(
        components,
        key=lambda item: (item[1][1] + item[1][3]) * 0.5,
    )
    rows = []
    base_row_size = len(components) // row_count
    extra_items = len(components) % row_count
    start = 0
    for row in range(row_count):
        row_size = base_row_size + (1 if row < extra_items else 0)
        end = start + row_size
        rows.extend(sorted(ordered_by_height[start:end], key=lambda item: item[1][0]))
        start = end
    return rows


def crop_cell(source, column: int, row: int, spec: SheetSpec):
    left = round(column * source.width / spec.columns)
    right = round((column + 1) * source.width / spec.columns)
    top = round(row * source.height / spec.rows)
    bottom = round((row + 1) * source.height / spec.rows)

    margin = spec.crop_margin
    left += margin
    right -= margin
    top += margin
    bottom -= margin
    return source.crop((left, top, right, bottom))


def remove_green_background(frame, image_module):
    keyed = frame.convert("RGBA")
    pixel_reader = getattr(keyed, "get_flattened_data", keyed.getdata)
    source_pixels = list(pixel_reader())
    output_pixels = []

    for red, green, blue, alpha in source_pixels:
        green_excess = green - max(red, blue)
        is_cyan_effect = blue > red + 35 and green - blue < 70
        if green > 20 and green_excess > 14 and not is_cyan_effect:
            if green_excess >= 72:
                output_alpha = 0
            else:
                output_alpha = round(alpha * (72 - green_excess) / 58)

            edge_green = min(green, max(red, blue))
            output_pixels.append((red, edge_green, blue, max(0, output_alpha)))
        else:
            output_pixels.append((red, green, blue, alpha))

    keyed.putdata(output_pixels)
    return keyed


def remove_background(frame, image_module, key_white_background: bool = False):
    keyed = remove_green_background(frame, image_module)
    if not key_white_background:
        return keyed

    pixel_reader = getattr(keyed, "get_flattened_data", keyed.getdata)
    output_pixels = []
    for red, green, blue, alpha in pixel_reader():
        minimum = min(red, green, blue)
        maximum = max(red, green, blue)
        chroma = maximum - minimum
        if minimum >= 242 and chroma <= 18:
            output_pixels.append((red, green, blue, 0))
        elif minimum >= 220 and chroma <= 22:
            fade = round(alpha * (242 - minimum) / 22)
            output_pixels.append((red, green, blue, max(0, fade)))
        else:
            output_pixels.append((red, green, blue, alpha))

    keyed.putdata(output_pixels)
    return keyed


def remove_long_dark_guides(frame, image_module):
    cleaned = frame.copy()
    pixels = cleaned.load()
    rows_to_clear: set[int] = set()
    columns_to_clear: set[int] = set()

    for y in range(cleaned.height):
        dark_count = sum(
            1
            for x in range(cleaned.width)
            if pixels[x, y][3] > 48 and max(pixels[x, y][:3]) < 72
        )
        if dark_count > cleaned.width * 0.25:
            rows_to_clear.update(range(max(0, y - 2), min(cleaned.height, y + 3)))

    for x in range(cleaned.width):
        dark_count = sum(
            1
            for y in range(cleaned.height)
            if pixels[x, y][3] > 48 and max(pixels[x, y][:3]) < 72
        )
        if dark_count > cleaned.height * 0.70:
            columns_to_clear.update(range(max(0, x - 2), min(cleaned.width, x + 3)))

    for y in rows_to_clear:
        for x in range(cleaned.width):
            red, green, blue, _ = pixels[x, y]
            pixels[x, y] = (red, green, blue, 0)

    for x in columns_to_clear:
        for y in range(cleaned.height):
            red, green, blue, _ = pixels[x, y]
            pixels[x, y] = (red, green, blue, 0)

    return cleaned


def normalize_frame(frame, image_module):
    scale = min(
        (FRAME_SIZE - FRAME_PADDING * 2) / frame.width,
        (FRAME_SIZE - FRAME_PADDING * 2) / frame.height,
    )
    resized = frame.resize(
        (
            max(1, round(frame.width * scale)),
            max(1, round(frame.height * scale)),
        ),
        image_module.Resampling.LANCZOS,
    )

    alpha_box = resized.getchannel("A").getbbox()
    canvas = image_module.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
    if alpha_box is None:
        return canvas

    x = (FRAME_SIZE - resized.width) // 2
    y = GROUND_BASELINE - alpha_box[3]
    y = max(FRAME_PADDING - alpha_box[1], min(y, FRAME_SIZE - alpha_box[3]))
    canvas.alpha_composite(resized, (x, y))
    return remove_green_background(canvas, image_module)


def normalize_trimmed_frames(frames, image_module, output_scale: float = 1.0):
    content_frames = []
    max_width = 1
    max_height = 1

    for frame in frames:
        alpha_box = frame.getchannel("A").getbbox()
        if alpha_box is None:
            content_frames.append((None, None))
            continue

        left, top, right, bottom = alpha_box
        content = frame.crop((left, top, right, bottom))
        content_frames.append((content, alpha_box))
        max_width = max(max_width, content.width)
        max_height = max(max_height, content.height)

    scale = min(
        (FRAME_SIZE - FRAME_PADDING * 2) / max_width,
        (FRAME_SIZE - FRAME_PADDING * 2) / max_height,
    ) * output_scale
    print(
        f"  Pose normalization scale: {scale:.4f} "
        f"(largest pose {max_width}x{max_height})"
    )
    normalized = []
    for content, alpha_box in content_frames:
        canvas = image_module.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
        if content is None or alpha_box is None:
            normalized.append(canvas)
            continue

        resized = content.resize(
            (
                max(1, round(content.width * scale)),
                max(1, round(content.height * scale)),
            ),
            image_module.Resampling.LANCZOS,
        )
        x = (FRAME_SIZE - resized.width) // 2
        y = GROUND_BASELINE - resized.height
        y = max(FRAME_PADDING, min(y, FRAME_SIZE - resized.height))
        canvas.alpha_composite(resized, (x, y))
        normalized.append(remove_green_background(canvas, image_module))

    return normalized


if __name__ == "__main__":
    raise SystemExit(main())
