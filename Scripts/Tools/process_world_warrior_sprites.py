from __future__ import annotations

import gc
from dataclasses import dataclass
from pathlib import Path

from process_higgsfield_sprites import (
    SheetSpec,
    extract_segmented_poses,
    remove_green_background,
)


ROOT = Path("Assets/Sprites/Ryu")
SOURCE_ROOT = ROOT / "Higgsfield"
OUTPUT = ROOT / "ryu_higgsfield_v3_sheet.png"
OUTPUT_FRAMES = SOURCE_ROOT / "ProcessedV3"

FRAME_SIZE = 320
FRAME_PADDING = 8
GROUND_BASELINE = 312
SOURCE_SCALE = 0.39
SHEET_COLUMNS = 10
SHEET_ROWS = 12


@dataclass(frozen=True)
class PoseFamily:
    name: str
    source: Path


FAMILIES = (
    PoseFamily("movement", SOURCE_ROOT / "world_warrior_movement_v3.png"),
    PoseFamily("normals", SOURCE_ROOT / "world_warrior_normals_v3.png"),
    PoseFamily("reactions", SOURCE_ROOT / "world_warrior_reactions_v3.png"),
    PoseFamily("crouch", SOURCE_ROOT / "world_warrior_crouch_v4.png"),
    PoseFamily("air", SOURCE_ROOT / "world_warrior_air_v3.png"),
    PoseFamily("hadouken", SOURCE_ROOT / "world_warrior_hadouken_v3.png"),
    PoseFamily("shoryuken", SOURCE_ROOT / "world_warrior_shoryuken_v3.png"),
    PoseFamily("tatsumaki", SOURCE_ROOT / "world_warrior_tatsumaki_v3.png"),
)


# Each output row contains ten animation frames. Repetition is intentional only
# for looping state animations; attack rows retain all ten generated phases.
ROW_LAYOUT = (
    ("idle", "movement", (0, 1, 2, 1, 0, 1, 2, 1, 0, 1)),
    ("walk", "movement", (3, 4, 5, 6, 5, 4, 3, 4, 5, 6)),
    ("dash", "movement", (6, 7, 7, 6, 7, 7, 6, 7, 7, 6)),
    ("jump", "movement", (8, 8, 9, 9, 8, 8, 9, 9, 8, 9)),
    ("normals", "normals", tuple(range(10))),
    ("reactions", "reactions", tuple(range(10))),
    ("crouch", "crouch", tuple(range(10))),
    ("air", "air", tuple(range(10))),
    ("hadouken", "hadouken", tuple(range(10))),
    ("shoryuken", "shoryuken", tuple(range(10))),
    ("tatsumaki", "tatsumaki", tuple(range(10))),
    ("super", "hadouken", (0, 1, 2, 3, 4, 5, 6, 7, 8, 9)),
)


def main() -> int:
    try:
        from PIL import Image
    except ImportError:
        print("ERROR: Pillow is required.")
        return 1

    missing = [str(family.source) for family in FAMILIES if not family.source.exists()]
    if missing:
        print("ERROR: Missing Higgsfield source sheets:")
        for path in missing:
            print(f"  {path}")
        return 2

    families: dict[str, list] = {}
    for family in FAMILIES:
        with Image.open(family.source) as source_image:
            source = source_image.convert("RGBA")

        raw_poses = extract_family_poses(family, source, Image)
        families[family.name] = [
            normalize_pose(pose, Image)
            for pose in raw_poses
        ]
        print(f"Extracted {len(raw_poses)} poses from {family.source.name}")
        del source
        del raw_poses
        gc.collect()

    OUTPUT_FRAMES.mkdir(parents=True, exist_ok=True)
    atlas = Image.new(
        "RGBA",
        (FRAME_SIZE * SHEET_COLUMNS, FRAME_SIZE * SHEET_ROWS),
        (0, 0, 0, 0),
    )

    for row, (row_name, family_name, pose_indices) in enumerate(ROW_LAYOUT):
        poses = families[family_name]
        for column, pose_index in enumerate(pose_indices):
            frame = poses[pose_index].copy()
            frame.save(OUTPUT_FRAMES / f"row{row:02d}_{row_name}_frame{column:02d}.png")
            atlas.alpha_composite(frame, (column * FRAME_SIZE, row * FRAME_SIZE))

        print(f"Packed row {row:02d}: {row_name} <- {family_name}")

    atlas.save(OUTPUT)
    print(f"Saved {OUTPUT} ({atlas.width}x{atlas.height})")
    return 0


def extract_family_poses(family: PoseFamily, source, image_module) -> list:
    if family.name == "normals":
        return extract_normals_poses(family, source, image_module)

    return extract_segmented_poses(
        source,
        SheetSpec(
            row=0,
            name=family.name,
            source=family.source,
            columns=5,
            rows=2,
            frame_count=10,
            segment_poses=True,
        ),
        image_module,
    )


def extract_normals_poses(family: PoseFamily, source, image_module) -> list:
    midpoint = source.height // 2
    top = source.crop((0, 0, source.width, midpoint))
    bottom = source.crop((0, midpoint, source.width, source.height))
    top_poses = extract_segmented_poses(
        top,
        SheetSpec(0, "normals_top", family.source, 5, 1, 5, segment_poses=True),
        image_module,
    )
    bottom_components = extract_segmented_poses(
        bottom,
        SheetSpec(0, "normals_bottom", family.source, 5, 1, 4, segment_poses=True),
        image_module,
    )

    merged = max(bottom_components, key=lambda pose: pose.width)
    split = find_alpha_valley(merged)
    left = largest_pose(merged.crop((0, 0, split, merged.height)), family, image_module)
    right = largest_pose(
        merged.crop((split, 0, merged.width, merged.height)),
        family,
        image_module,
    )
    remaining = [pose for pose in bottom_components if pose is not merged]
    bottom_poses = [left, right, *remaining]
    return [*top_poses, *bottom_poses]


def find_alpha_valley(frame) -> int:
    alpha = frame.getchannel("A")
    start = round(frame.width * 0.42)
    end = round(frame.width * 0.58)
    return min(
        range(start, end),
        key=lambda x: sum(1 for y in range(alpha.height) if alpha.getpixel((x, y)) > 32),
    )


def largest_pose(frame, family: PoseFamily, image_module):
    return extract_segmented_poses(
        frame,
        SheetSpec(0, "normals_split", family.source, 1, 1, 1, segment_poses=True),
        image_module,
    )[0]


def normalize_pose(frame, image_module):
    alpha_box = frame.getchannel("A").getbbox()
    canvas = image_module.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
    if alpha_box is None:
        return canvas

    content = frame.crop(alpha_box)
    fit_scale = min(
        SOURCE_SCALE,
        (FRAME_SIZE - FRAME_PADDING * 2) / content.width,
        (FRAME_SIZE - FRAME_PADDING * 2) / content.height,
    )
    resized = content.resize(
        (
            max(1, round(content.width * fit_scale)),
            max(1, round(content.height * fit_scale)),
        ),
        image_module.Resampling.LANCZOS,
    )
    x = (FRAME_SIZE - resized.width) // 2
    y = GROUND_BASELINE - resized.height
    y = max(FRAME_PADDING, min(y, FRAME_SIZE - resized.height))
    canvas.alpha_composite(resized, (x, y))
    return remove_green_background(canvas, image_module)


if __name__ == "__main__":
    raise SystemExit(main())
