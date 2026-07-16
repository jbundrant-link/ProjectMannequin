#!/usr/bin/env python3
"""Pack Higgsfield-restyled MUGEN actions into Ryu's secondary V5 atlas."""

from __future__ import annotations

import gc
import json
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

from audit_mugen_character import (
    AirFrame,
    parse_air,
    unique_frames_with_sequence,
)
from process_higgsfield_sprites import remove_green_background


ROOT = Path("Assets/Sprites/Ryu")
GENERATED_ROOT = ROOT / "Higgsfield/V5Generated"
PROCESSED_ROOT = ROOT / "Higgsfield/ProcessedV5"
MUGEN_AIR = Path(r"C:/Users/Joseph Bundrant/Downloads/sf3_ryu/sf3_ryu.air")
V4_ATLAS = ROOT / "ryu_higgsfield_v4_sheet.png"
OUTPUT_ATLAS = ROOT / "ryu_higgsfield_v5_actions.png"
OUTPUT_MAP = ROOT / "ryu_higgsfield_v5_animation_map.json"

FRAME_SIZE = 256
FRAME_PADDING = 6
GROUND_BASELINE = 248
ATLAS_COLUMNS = 16
ATLAS_ROWS = 13
V4_COLUMNS = 16
V4_FRAME_SIZE = 320
V4_TO_V5_SCALE = FRAME_SIZE / V4_FRAME_SIZE


@dataclass(frozen=True)
class ActionSheet:
    name: str
    source_name: str
    action: int
    atlas_row: int
    row_lengths: tuple[int, ...]
    target_v4_frame: int

    @property
    def frame_count(self) -> int:
        return sum(self.row_lengths)

    @property
    def source(self) -> Path:
        return GENERATED_ROOT / self.source_name


SHEETS = (
    ActionSheet(
        "shoulder_throw",
        "shoulder_throw_action_810.png",
        810,
        0,
        (8, 8),
        0,
    ),
    ActionSheet(
        "back_throw",
        "back_throw_action_820_v2.png",
        820,
        1,
        (6, 6),
        0,
    ),
    ActionSheet(
        "collarbone_breaker",
        "collarbone_breaker_action_900.png",
        900,
        2,
        (8, 3),
        0,
    ),
    ActionSheet(
        "solar_plexus",
        "solar_plexus_action_910.png",
        910,
        3,
        (7, 7),
        0,
    ),
    ActionSheet(
        "hadouken",
        "hadouken_action_1000_v2.png",
        1000,
        4,
        (8, 8),
        0,
    ),
    ActionSheet(
        "shoryuken",
        "shoryuken_action_1120_v2.png",
        1120,
        5,
        (6, 5),
        192,
    ),
    ActionSheet(
        "tatsumaki",
        "tatsumaki_action_1220.png",
        1220,
        6,
        (7, 6),
        0,
    ),
    ActionSheet(
        "air_tatsumaki",
        "air_tatsumaki_action_1320.png",
        1320,
        7,
        (8, 7),
        149,
    ),
    ActionSheet(
        "joudan",
        "joudan_action_1420.png",
        1420,
        8,
        (8, 6),
        80,
    ),
    ActionSheet(
        "shin_shoryuken_start",
        "shin_shoryuken_start_action_3000_v2.png",
        3000,
        9,
        (7, 6),
        192,
    ),
    ActionSheet(
        "shin_shoryuken_finish",
        "shin_shoryuken_finish_action_3010.png",
        3010,
        10,
        (8, 1),
        193,
    ),
    ActionSheet(
        "shinku_hadouken_a",
        "shinku_hadouken_action_3100_a.png",
        3100,
        11,
        (6, 6),
        0,
    ),
    ActionSheet(
        "shinku_hadouken_b",
        "shinku_hadouken_action_3100_b.png",
        3100,
        12,
        (6, 6),
        0,
    ),
)


MOVE_ACTIONS = {
    "ryu_collarbone_breaker": (900, 900),
    "ryu_solar_plexus": (910, 910),
    "ryu_shoulder_throw": (810, 810),
    "ryu_back_throw": (820, 820),
    "ryu_hadouken_light": (1000, 1000),
    "ryu_hadouken_medium": (1000, 1000),
    "ryu_hadouken_heavy": (1000, 1000),
    "ryu_hadouken_ex": (1030, 1000),
    "ryu_shoryuken_light": (1100, 1120),
    "ryu_shoryuken_medium": (1110, 1120),
    "ryu_shoryuken_heavy": (1120, 1120),
    "ryu_shoryuken_ex": (1130, 1120),
    "ryu_tatsumaki_light": (1200, 1220),
    "ryu_tatsumaki_medium": (1210, 1220),
    "ryu_tatsumaki_heavy": (1220, 1220),
    "ryu_tatsumaki_ex": (1230, 1220),
    "ryu_air_tatsumaki_light": (1300, 1320),
    "ryu_air_tatsumaki_medium": (1310, 1320),
    "ryu_air_tatsumaki_heavy": (1320, 1320),
    "ryu_air_tatsumaki_ex": (1330, 1320),
    "ryu_joudan_light": (1400, 1420),
    "ryu_joudan_medium": (1410, 1420),
    "ryu_joudan_heavy": (1420, 1420),
    "ryu_joudan_ex": (1430, 1420),
    "ryu_shinku_hadouken": (3100, 3100),
}


def main() -> int:
    missing = [str(sheet.source) for sheet in SHEETS if not sheet.source.exists()]
    for required in (MUGEN_AIR, V4_ATLAS):
        if not required.exists():
            missing.append(str(required))
    if missing:
        print("ERROR: Missing V5 inputs:")
        for path in missing:
            print(f"  {path}")
        return 1

    actions = parse_air(MUGEN_AIR)
    with Image.open(V4_ATLAS) as v4_source:
        v4_atlas = v4_source.convert("RGBA")

    atlas = Image.new(
        "RGBA",
        (ATLAS_COLUMNS * FRAME_SIZE, ATLAS_ROWS * FRAME_SIZE),
        (0, 0, 0, 0),
    )
    PROCESSED_ROOT.mkdir(parents=True, exist_ok=True)
    action_atlas_frames: dict[int, list[int]] = {}

    for sheet in SHEETS:
        with Image.open(sheet.source) as source_image:
            source = source_image.convert("RGBA")
        components = segment_components(source)
        poses = order_components(components, sheet.row_lengths)
        if len(poses) != sheet.frame_count:
            raise RuntimeError(
                f"{sheet.name}: selected {len(poses)} poses, expected {sheet.frame_count}"
            )

        target_height = target_height_for_frame(v4_atlas, sheet.target_v4_frame)
        frames = normalize_sheet_poses(poses, target_height)
        if sheet.action == 3100:
            action_atlas_frames.setdefault(sheet.action, [])
        else:
            action_atlas_frames[sheet.action] = []

        for column, frame in enumerate(frames):
            atlas_index = sheet.atlas_row * ATLAS_COLUMNS + column
            action_atlas_frames[sheet.action].append(atlas_index)
            frame.save(
                PROCESSED_ROOT
                / f"row{sheet.atlas_row:02d}_{sheet.name}_frame{column:02d}.png"
            )
            atlas.alpha_composite(
                frame,
                (column * FRAME_SIZE, sheet.atlas_row * FRAME_SIZE),
            )
        print(
            f"Packed row {sheet.atlas_row:02d}: {sheet.name} "
            f"({len(frames)} poses, target height {target_height}px)"
        )
        del source
        del components
        del poses
        del frames
        gc.collect()

    atlas.save(OUTPUT_ATLAS)
    animation_map = build_animation_map(actions, action_atlas_frames)
    OUTPUT_MAP.write_text(
        json.dumps(animation_map, indent=2, ensure_ascii=True),
        encoding="utf-8",
    )
    print(f"Saved {OUTPUT_ATLAS} ({atlas.width}x{atlas.height})")
    print(f"Saved {OUTPUT_MAP}")
    return 0


def segment_components(source: Image.Image) -> list[tuple[Image.Image, tuple[int, ...], int]]:
    keyed = remove_green_background(source, Image)
    alpha = keyed.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > 32 else 0)
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

        seed = (left + seed_index % cropped.width, top + seed_index // cropped.width)
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

        isolated = keyed.crop(component_bounds)
        cropped_alpha = alpha.crop(component_bounds)
        cropped_mask = component_mask.crop(component_bounds)
        isolated.putalpha(ImageChops.multiply(cropped_alpha, cropped_mask))
        components.append(
            (isolated, component_bounds, component_area)
        )
    return components


def order_components(
    components: list[tuple[Image.Image, tuple[int, ...], int]],
    row_lengths: tuple[int, ...],
) -> list[Image.Image]:
    expected = sum(row_lengths)
    if len(components) < expected:
        raise RuntimeError(f"Found {len(components)} poses, expected at least {expected}")

    ordered_by_y = sorted(
        components,
        key=lambda item: (item[1][1] + item[1][3]) * 0.5,
    )
    selected = []
    start = 0
    for row_length in row_lengths:
        available_in_row = len(ordered_by_y) - start
        if available_in_row < row_length:
            raise RuntimeError("Not enough components remain for the declared row layout")
        row = ordered_by_y[start : start + row_length]
        selected.extend(sorted(row, key=lambda item: item[1][0]))
        start += row_length
    return [item[0] for item in selected]


def target_height_for_frame(v4_atlas: Image.Image, frame_index: int) -> int:
    column = frame_index % V4_COLUMNS
    row = frame_index // V4_COLUMNS
    frame = v4_atlas.crop(
        (
            column * V4_FRAME_SIZE,
            row * V4_FRAME_SIZE,
            (column + 1) * V4_FRAME_SIZE,
            (row + 1) * V4_FRAME_SIZE,
        )
    )
    bounds = frame.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError(f"V4 frame {frame_index} is empty")
    return max(1, round((bounds[3] - bounds[1]) * V4_TO_V5_SCALE))


def normalize_sheet_poses(
    poses: list[Image.Image],
    target_first_pose_height: int,
) -> list[Image.Image]:
    source_bounds = poses[0].getchannel("A").getbbox()
    if source_bounds is None:
        raise RuntimeError("First pose is empty")
    source_height = source_bounds[3] - source_bounds[1]
    scale = target_first_pose_height / max(1, source_height)

    content_bounds = [pose.getchannel("A").getbbox() for pose in poses]
    max_width = max(bounds[2] - bounds[0] for bounds in content_bounds if bounds)
    max_height = max(bounds[3] - bounds[1] for bounds in content_bounds if bounds)
    scale = min(
        scale,
        (FRAME_SIZE - FRAME_PADDING * 2) / max_width,
        (FRAME_SIZE - FRAME_PADDING * 2) / max_height,
    )

    normalized = []
    for pose, bounds in zip(poses, content_bounds):
        if bounds is None:
            normalized.append(Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE)))
            continue
        content = pose.crop(bounds)
        resized = content.resize(
            (
                max(1, round(content.width * scale)),
                max(1, round(content.height * scale)),
            ),
            Image.Resampling.LANCZOS,
        )
        canvas = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
        x = (FRAME_SIZE - resized.width) // 2
        y = max(FRAME_PADDING, GROUND_BASELINE - resized.height)
        canvas.alpha_composite(resized, (x, y))
        normalized.append(canvas)
    return normalized


def frame_key(frame: AirFrame) -> tuple[int, int, str]:
    return frame.group, frame.image, frame.flags


def map_action(
    actions: dict[int, list[AirFrame]],
    action_atlas_frames: dict[int, list[int]],
    action: int,
    base_action: int,
) -> dict[str, object]:
    base_unique, _ = unique_frames_with_sequence(actions[base_action])
    atlas_frames = action_atlas_frames[base_action]
    if len(base_unique) != len(atlas_frames):
        raise RuntimeError(
            f"Action {base_action}: {len(base_unique)} unique AIR poses but "
            f"{len(atlas_frames)} atlas poses"
        )
    key_to_atlas = {
        frame_key(frame): atlas_frames[index]
        for index, frame in enumerate(base_unique)
    }
    sequence = []
    for frame in actions[action]:
        key = frame_key(frame)
        if key not in key_to_atlas:
            raise RuntimeError(
                f"Action {action}: sprite {key} missing from base action {base_action}"
            )
        sequence.append(key_to_atlas[key])
    return {
        "source_action": action,
        "base_action": base_action,
        "frames": sequence,
        "durations": [max(1, frame.duration) for frame in actions[action]],
    }


def combine_entries(*entries: dict[str, object]) -> dict[str, object]:
    return {
        "source_action": "+".join(str(entry["source_action"]) for entry in entries),
        "base_action": "+".join(str(entry["base_action"]) for entry in entries),
        "frames": [
            frame
            for entry in entries
            for frame in entry["frames"]
        ],
        "durations": [
            duration
            for entry in entries
            for duration in entry["durations"]
        ],
    }


def build_animation_map(
    actions: dict[int, list[AirFrame]],
    action_atlas_frames: dict[int, list[int]],
) -> dict[str, object]:
    moves = {
        move_id: map_action(actions, action_atlas_frames, action, base_action)
        for move_id, (action, base_action) in MOVE_ACTIONS.items()
    }
    moves["ryu_shin_shoryuken"] = combine_entries(
        map_action(actions, action_atlas_frames, 3000, 3000),
        map_action(actions, action_atlas_frames, 3010, 3010),
    )
    moves["ryu_denjin_hadouken"] = combine_entries(
        map_action(actions, action_atlas_frames, 3200, 1000),
        map_action(actions, action_atlas_frames, 3220, 1000),
    )
    return {
        "atlas": {
            "path": "res://Assets/Sprites/Ryu/ryu_higgsfield_v5_actions.png",
            "columns": ATLAS_COLUMNS,
            "rows": ATLAS_ROWS,
            "pixel_size": 0.018,
            # Sprite3D is centre-anchored, so the ground offset is measured from
            # the cell centre to the feet baseline, not the raw baseline pixel.
            "ground_offset_pixels": GROUND_BASELINE - FRAME_SIZE // 2,
        },
        "moves": moves,
    }


if __name__ == "__main__":
    raise SystemExit(main())
