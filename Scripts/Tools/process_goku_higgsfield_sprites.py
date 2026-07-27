#!/usr/bin/env python3
"""Normalize the complete Higgsfield Goku animation set for Godot."""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path
from statistics import median

from PIL import Image, ImageChops, ImageDraw, ImageFilter


COLUMNS = 8
CELL_WIDTH = 384
CELL_HEIGHT = 320
GROUND_BASELINE = 312
TARGET_BODY_HEIGHT = 230
WALK_RUNTIME_START = 9
WALK_SOURCE_SEQUENCE = (0, 0, 1, 2, 3, 4, 4, 5, 6, 7)
MOVEMENT_FORM_KEYS = (
    "base",
    "kaioken",
    "false_super",
    "ss1",
    "ss2",
    "ss3",
    "ss4",
    "god",
    "blue",
    "blue_kaioken",
    "ui_sign",
    "instinct",
)


MOVEMENT_SETS = (
    (
        Path("Assets/Sprites/Goku/goku_restyle_base_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_base_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_kaioken_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_kaioken_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_kaioken_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_false_super_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_false_super_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_false_super_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ss1_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_ss1_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_ss1_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ss2_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_ss2_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_ss2_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ss3_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_ss3_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_ss3_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ss4_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_ss4_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_ss4_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_god_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_god_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_god_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_blue_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_blue_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_blue_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_blue_kaioken_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_blue_kaioken_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_blue_kaioken_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ui_sign_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_ui_sign_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_ui_sign_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_instinct_core.png"),
        Path("Assets/Sprites/Goku/goku_restyle_instinct_posture.png"),
        Path("Assets/Sprites/Goku/goku_astral_instinct_higgsfield_v1_sheet.png"),
    ),
)

SPECIAL_SETS = (
    (
        Path("Assets/Sprites/Goku/goku_restyle_base_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_base_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_kaioken_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_kaioken_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_false_super_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_false_super_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ss1_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_ss1_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ss2_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_ss2_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ss3_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_ss3_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ss4_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_ss4_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_god_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_god_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_blue_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_blue_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_blue_kaioken_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_blue_kaioken_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_ui_sign_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_ui_sign_specials_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_instinct_specials.png"),
        Path("Assets/Sprites/Goku/goku_astral_instinct_specials_higgsfield_v1_sheet.png"),
    ),
)

EFFECT_SETS = (
    (
        Path("Assets/Sprites/Goku/goku_ki_blast_fx_higgsfield_v1.png"),
        Path("Assets/Sprites/Goku/goku_astral_ki_blast_fx_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_kamehameha_fx_higgsfield_v2.png"),
        Path("Assets/Sprites/Goku/goku_astral_kamehameha_fx_higgsfield_v1_sheet.png"),
    ),
    (
        Path("Assets/Sprites/Goku/goku_spirit_bomb_fx_higgsfield_v1.png"),
        Path("Assets/Sprites/Goku/goku_astral_spirit_bomb_fx_higgsfield_v1_sheet.png"),
    ),
)

FULL_LANE_BEAM_SOURCE = Path(
    "Assets/Sprites/Goku/goku_kamehameha_full_lane_higgsfield_v3.png"
)
FULL_LANE_BEAM_OUTPUT = Path(
    "Assets/Sprites/Goku/goku_astral_kamehameha_full_lane_higgsfield_v3_sheet.png"
)

TRANSFORMATION_SETS = (
    (
        Path("Assets/Sprites/Goku/goku_restyle_transform_blue.png"),
        Path("Assets/Sprites/Goku/goku_astral_transform_blue_higgsfield_v1_sheet.png"),
        3,
    ),
    (
        Path("Assets/Sprites/Goku/goku_restyle_transform_instinct.png"),
        Path("Assets/Sprites/Goku/goku_astral_transform_instinct_higgsfield_v1_sheet.png"),
        8,
    ),
)

INTENTIONALLY_BLANK_SPECIAL_FRAMES = (42, 46)
WALK_SOURCES = {
    Path("Assets/Sprites/Goku/goku_astral_higgsfield_v1_sheet.png"): Path(
        "Assets/Sprites/Concepts/StyleCalibration/goku_base_walk_sheet_pilot_v2.png"
    ),
}


def normalize_sheet(
    path: Path,
    rows: int,
    occupied_frames: int,
    fixed_scale: float,
) -> Image.Image:
    image = Image.open(path).convert("RGBA")
    image = chroma_key_green(image)
    image = clear_cell_boundaries(image, COLUMNS, rows)
    source_cell_width = image.width // COLUMNS
    source_cell_height = image.height // rows
    normalized = Image.new(
        "RGBA",
        (COLUMNS * CELL_WIDTH, rows * CELL_HEIGHT),
        (0, 0, 0, 0),
    )
    for frame_index in range(occupied_frames):
        column = frame_index % COLUMNS
        row = frame_index // COLUMNS
        margin_x = source_cell_width // 2
        margin_y = source_cell_height // 3
        source_left = column * source_cell_width
        source_top = row * source_cell_height
        region_left = max(0, source_left - margin_x)
        region_top = max(0, source_top - margin_y)
        region_right = min(image.width, source_left + source_cell_width + margin_x)
        region_bottom = min(image.height, source_top + source_cell_height + margin_y)
        source_region = image.crop(
            (
                region_left,
                region_top,
                region_right,
                region_bottom,
            )
        )
        expected_center = (
            source_left + source_cell_width / 2 - region_left,
            source_top + source_cell_height / 2 - region_top,
        )
        component = extract_component_near(source_region, expected_center)
        if component is None:
            continue

        target_size = (
            max(1, round(component.width * fixed_scale)),
            max(1, round(component.height * fixed_scale)),
        )
        target_component = component.resize(target_size, Image.Resampling.LANCZOS)
        target_cell = Image.new(
            "RGBA",
            (CELL_WIDTH, CELL_HEIGHT),
            (0, 0, 0, 0),
        )
        paste_x = (CELL_WIDTH - target_component.width) // 2
        paste_y = GROUND_BASELINE - target_component.height
        alpha_composite_clipped(
            target_cell,
            target_component,
            paste_x,
            paste_y,
        )
        normalized.alpha_composite(
            target_cell,
            (column * CELL_WIDTH, row * CELL_HEIGHT),
        )
    return normalized


def derive_sheet_scale(
    path: Path,
    rows: int,
    occupied_frames: int,
    reference_frames: tuple[int, ...] = (),
) -> float:
    image = chroma_key_green(Image.open(path).convert("RGBA"))
    image = clear_cell_boundaries(image, COLUMNS, rows)
    source_cell_width = image.width // COLUMNS
    source_cell_height = image.height // rows
    frame_indices = (
        reference_frames
        if reference_frames
        else tuple(range(occupied_frames))
    )
    body_heights: list[int] = []
    for frame_index in frame_indices:
        component = extract_frame_component(
            image,
            frame_index,
            source_cell_width,
            source_cell_height,
        )
        if component is None:
            continue
        # Use the extracted body silhouette height rather than a luminance
        # ("ink") measurement. Ink height miscounts dark red/blue auras and
        # skips bright gold/silver hair, which scaled forms inconsistently.
        # The silhouette height is color-independent, so every form normalizes
        # to the same body size (auras are kept as thin rims at generation).
        body_height = component.height
        if body_height >= 24:
            body_heights.append(body_height)

    if not body_heights:
        return 1.0

    if reference_frames:
        reference_height = median(body_heights)
    else:
        ordered = sorted(body_heights)
        reference_height = ordered[round((len(ordered) - 1) * 0.78)]
    return max(0.72, min(1.35, TARGET_BODY_HEIGHT / reference_height))


def extract_frame_component(
    image: Image.Image,
    frame_index: int,
    source_cell_width: int,
    source_cell_height: int,
) -> Image.Image | None:
    column = frame_index % COLUMNS
    row = frame_index // COLUMNS
    margin_x = source_cell_width // 2
    margin_y = source_cell_height // 3
    source_left = column * source_cell_width
    source_top = row * source_cell_height
    region_left = max(0, source_left - margin_x)
    region_top = max(0, source_top - margin_y)
    region_right = min(image.width, source_left + source_cell_width + margin_x)
    region_bottom = min(image.height, source_top + source_cell_height + margin_y)
    source_region = image.crop(
        (region_left, region_top, region_right, region_bottom)
    )
    expected_center = (
        source_left + source_cell_width / 2 - region_left,
        source_top + source_cell_height / 2 - region_top,
    )
    return extract_component_near(source_region, expected_center)


def measure_ink_height(image: Image.Image) -> int:
    min_y = image.height
    max_y = -1
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha <= 24:
                continue
            luminance = (red * 54 + green * 183 + blue * 19) // 256
            if luminance < 145:
                min_y = min(min_y, y)
                max_y = max(max_y, y)
    return 0 if max_y < min_y else max_y - min_y + 1


def alpha_composite_clipped(
    destination: Image.Image,
    source: Image.Image,
    x: int,
    y: int,
) -> None:
    source_left = max(0, -x)
    source_top = max(0, -y)
    source_right = min(source.width, destination.width - x)
    source_bottom = min(source.height, destination.height - y)
    if source_right <= source_left or source_bottom <= source_top:
        return
    destination.alpha_composite(
        source.crop((source_left, source_top, source_right, source_bottom)),
        (max(0, x), max(0, y)),
    )


def clear_cell_boundaries(
    image: Image.Image,
    columns: int,
    rows: int,
    half_band: int = 7,
) -> Image.Image:
    alpha = image.getchannel("A")
    draw = ImageDraw.Draw(alpha)
    cell_width = image.width // columns
    cell_height = image.height // rows
    for column in range(1, columns):
        boundary = column * cell_width
        draw.rectangle(
            (boundary - half_band, 0, boundary + half_band, image.height),
            fill=0,
        )
    for row in range(1, rows):
        boundary = row * cell_height
        draw.rectangle(
            (0, boundary - half_band, image.width, boundary + half_band),
            fill=0,
        )
    image.putalpha(alpha)
    return image


def chroma_key_green(image: Image.Image) -> Image.Image:
    # `get_flattened_data` exists only on a custom Pillow build; fall back to the
    # standard `getdata` so the pipeline runs on a stock interpreter too.
    pixel_reader = getattr(image, "get_flattened_data", image.getdata)
    pixels = []
    for red, green, blue, alpha in pixel_reader():
        green_dominance = green - max(red, blue)
        # Fully key out the solid green screen.
        if green > 90 and green_dominance > 30:
            alpha = 0
        # Feather the anti-aliased green halo ring so silhouettes lose their
        # green fringe instead of baking it into the runtime atlas.
        elif green > 70 and green_dominance > 14:
            alpha = min(alpha, 96)
        pixels.append((red, green, blue, alpha))
    keyed = Image.new("RGBA", image.size)
    keyed.putdata(pixels)
    return _despill_green(keyed)


def _despill_green(image: Image.Image) -> Image.Image:
    """Neutralize green spill on edge pixels.

    Goku (base and every transformed form) contains no green, so clamping the
    green channel to at most ``max(red, blue)`` removes chroma-screen bleed on
    anti-aliased silhouette edges without altering legitimate colors.
    """
    red, green, blue, alpha = image.split()
    red_blue_ceiling = ImageChops.lighter(red, blue)
    corrected_green = ImageChops.darker(green, red_blue_ceiling)
    return Image.merge("RGBA", (red, corrected_green, blue, alpha))


def extract_component_near(
    image: Image.Image,
    expected_center: tuple[float, float],
) -> Image.Image | None:
    width, height = image.size
    alpha = image.getchannel("A")
    alpha_pixels = alpha.load()
    visited = bytearray(width * height)
    candidates: list[tuple[float, list[int]]] = []

    for y in range(height):
        for x in range(width):
            start = y * width + x
            if visited[start] or alpha_pixels[x, y] <= 16:
                continue

            visited[start] = 1
            queue: deque[tuple[int, int]] = deque([(x, y)])
            component: list[int] = []
            while queue:
                current_x, current_y = queue.popleft()
                component.append(current_y * width + current_x)
                for neighbor_x, neighbor_y in (
                    (current_x - 1, current_y),
                    (current_x + 1, current_y),
                    (current_x, current_y - 1),
                    (current_x, current_y + 1),
                ):
                    if (
                        neighbor_x < 0
                        or neighbor_y < 0
                        or neighbor_x >= width
                        or neighbor_y >= height
                    ):
                        continue
                    neighbor = neighbor_y * width + neighbor_x
                    if visited[neighbor] or alpha_pixels[neighbor_x, neighbor_y] <= 16:
                        continue
                    visited[neighbor] = 1
                    queue.append((neighbor_x, neighbor_y))

            if len(component) < 24:
                continue

            centroid_x = sum(pixel_index % width for pixel_index in component) / len(component)
            centroid_y = sum(pixel_index // width for pixel_index in component) / len(component)
            distance = (
                ((centroid_x - expected_center[0]) / max(1, width)) ** 2
                + ((centroid_y - expected_center[1]) / max(1, height)) ** 2
            )
            area_bias = min(0.2, len(component) / max(1, width * height))
            candidates.append((distance - area_bias, component))

    if not candidates:
        return None

    component = min(candidates, key=lambda candidate: candidate[0])[1]
    min_x = min(pixel_index % width for pixel_index in component)
    max_x = max(pixel_index % width for pixel_index in component)
    min_y = min(pixel_index // width for pixel_index in component)
    max_y = max(pixel_index // width for pixel_index in component)
    mask_data = bytearray(width * height)
    for pixel_index in component:
        pixel_x = pixel_index % width
        pixel_y = pixel_index // width
        mask_data[pixel_index] = alpha_pixels[pixel_x, pixel_y]
    mask = Image.frombytes("L", image.size, bytes(mask_data))
    result = image.copy()
    result.putalpha(mask)
    return result.crop((min_x, min_y, max_x + 1, max_y + 1))


def clear_frames(image: Image.Image, frame_indices: tuple[int, ...]) -> None:
    transparent = Image.new("RGBA", (CELL_WIDTH, CELL_HEIGHT), (0, 0, 0, 0))
    for frame_index in frame_indices:
        column = frame_index % COLUMNS
        row = frame_index // COLUMNS
        image.paste(
            transparent,
            (column * CELL_WIDTH, row * CELL_HEIGHT),
        )


def normalize_walk_frames(path: Path) -> list[Image.Image]:
    columns = 4
    rows = 2
    source = chroma_key_green(Image.open(path).convert("RGBA"))
    source = clear_cell_boundaries(source, columns, rows)
    source_cell_width = source.width // columns
    source_cell_height = source.height // rows
    components: list[Image.Image] = []

    for frame_index in range(columns * rows):
        column = frame_index % columns
        row = frame_index // columns
        cell = source.crop(
            (
                column * source_cell_width,
                row * source_cell_height,
                (column + 1) * source_cell_width,
                (row + 1) * source_cell_height,
            )
        )
        component = extract_component_near(
            cell,
            (source_cell_width * 0.5, source_cell_height * 0.5),
        )
        if component is None:
            raise ValueError(f"{path}: walk frame {frame_index + 1} is empty")
        components.append(component)

    reference_height = median(component.height for component in components)
    maximum_width = max(component.width for component in components)
    maximum_height = max(component.height for component in components)
    scale = min(
        TARGET_BODY_HEIGHT / reference_height,
        (CELL_WIDTH - 12) / maximum_width,
        (CELL_HEIGHT - 12) / maximum_height,
    )

    frames: list[Image.Image] = []
    for component in components:
        resized = component.resize(
            (
                max(1, round(component.width * scale)),
                max(1, round(component.height * scale)),
            ),
            Image.Resampling.LANCZOS,
        )
        frame = Image.new("RGBA", (CELL_WIDTH, CELL_HEIGHT), (0, 0, 0, 0))
        alpha_composite_clipped(
            frame,
            resized,
            (CELL_WIDTH - resized.width) // 2,
            GROUND_BASELINE - resized.height,
        )
        frames.append(frame)
    return frames


def patch_walk_frames(atlas: Image.Image, source_path: Path) -> None:
    source_frames = normalize_walk_frames(source_path)
    transparent = Image.new("RGBA", (CELL_WIDTH, CELL_HEIGHT), (0, 0, 0, 0))
    for offset, source_index in enumerate(WALK_SOURCE_SEQUENCE):
        frame_index = WALK_RUNTIME_START + offset
        column = frame_index % COLUMNS
        row = frame_index // COLUMNS
        position = (column * CELL_WIDTH, row * CELL_HEIGHT)
        atlas.paste(transparent, position)
        atlas.alpha_composite(source_frames[source_index], position)


def write_movement_atlas(
    core_path: Path,
    posture_path: Path,
    output_path: Path,
    apply_walk_patch: bool = True,
) -> None:
    rows_per_source = 9
    core_scale = derive_sheet_scale(
        core_path,
        rows_per_source,
        occupied_frames=65,
        reference_frames=(0, 1, 2, 3, 4, 5),
    )
    posture_scale = derive_sheet_scale(
        posture_path,
        rows_per_source,
        occupied_frames=66,
    )
    core = normalize_sheet(
        core_path,
        rows_per_source,
        occupied_frames=65,
        fixed_scale=core_scale,
    )
    postures = normalize_sheet(
        posture_path,
        rows_per_source,
        occupied_frames=66,
        fixed_scale=posture_scale,
    )
    atlas = Image.new(
        "RGBA",
        (COLUMNS * CELL_WIDTH, rows_per_source * CELL_HEIGHT * 2),
        (0, 0, 0, 0),
    )
    atlas.alpha_composite(core, (0, 0))
    atlas.alpha_composite(postures, (0, rows_per_source * CELL_HEIGHT))
    if apply_walk_patch and (walk_source := WALK_SOURCES.get(output_path)):
        patch_walk_frames(atlas, walk_source)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(output_path)
    print(f"Wrote {output_path} at {atlas.width}x{atlas.height} (8x18 frames)")


def write_single_atlas(
    source_path: Path,
    output_path: Path,
    rows: int,
    occupied_frames: int,
    blank_frames: tuple[int, ...] = (),
) -> None:
    fixed_scale = derive_sheet_scale(source_path, rows, occupied_frames)
    atlas = normalize_sheet(
        source_path,
        rows,
        occupied_frames,
        fixed_scale=fixed_scale,
    )
    clear_frames(atlas, blank_frames)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(output_path)
    print(f"Wrote {output_path} at {atlas.width}x{atlas.height} (8x{rows} frames)")


def write_effect_atlas(
    source_path: Path,
    output_path: Path,
    columns: int = 4,
    rows: int = 2,
) -> None:
    border_trim = 5
    source = chroma_key_green(Image.open(source_path).convert("RGBA"))
    cell_width = source.width // columns
    cell_height = source.height // rows
    atlas = Image.new(
        "RGBA",
        (cell_width * columns, cell_height * rows),
        (0, 0, 0, 0),
    )

    for frame_index in range(columns * rows):
        column = frame_index % columns
        row = frame_index // columns
        source_left = column * cell_width
        source_top = row * cell_height
        cell = source.crop(
            (
                source_left + border_trim,
                source_top + border_trim,
                source_left + cell_width - border_trim,
                source_top + cell_height - border_trim,
            )
        )
        atlas.alpha_composite(
            cell,
            (
                source_left + border_trim,
                source_top + border_trim,
            ),
        )

    output_path.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(output_path)
    print(f"Wrote {output_path} at {atlas.width}x{atlas.height} (4x2 frames)")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--movement-form", choices=MOVEMENT_FORM_KEYS)
    parser.add_argument("--skip-walk-patch", action="store_true")
    args = parser.parse_args()

    movement_sets = MOVEMENT_SETS
    if args.movement_form:
        movement_sets = (
            MOVEMENT_SETS[MOVEMENT_FORM_KEYS.index(args.movement_form)],
        )

    for core_path, posture_path, output_path in movement_sets:
        write_movement_atlas(
            core_path,
            posture_path,
            output_path,
            apply_walk_patch=not args.skip_walk_patch,
        )

    if args.movement_form:
        return

    for source_path, output_path in SPECIAL_SETS:
        write_single_atlas(
            source_path,
            output_path,
            rows=8,
            occupied_frames=64,
            blank_frames=INTENTIONALLY_BLANK_SPECIAL_FRAMES,
        )

    for source_path, output_path, rows in TRANSFORMATION_SETS:
        write_single_atlas(
            source_path,
            output_path,
            rows,
            occupied_frames=17 if rows == 3 else 64,
        )

    for source_path, output_path in EFFECT_SETS:
        write_effect_atlas(source_path, output_path)
    write_effect_atlas(
        FULL_LANE_BEAM_SOURCE,
        FULL_LANE_BEAM_OUTPUT,
        columns=2,
        rows=2,
    )


if __name__ == "__main__":
    main()
