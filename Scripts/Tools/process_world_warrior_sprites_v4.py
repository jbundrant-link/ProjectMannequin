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
GENERATED_ROOT = SOURCE_ROOT / "V4Generated"
OUTPUT = ROOT / "ryu_higgsfield_v4_sheet.png"
OUTPUT_FRAMES = SOURCE_ROOT / "ProcessedV4"

FRAME_SIZE = 320
FRAME_PADDING = 8
GROUND_BASELINE = 312
SOURCE_SCALE = 0.39
SHEET_COLUMNS = 16
SHEET_ROWS = 15


@dataclass(frozen=True)
class Chunk:
    name: str
    frame_count: int
    rows: int = 2
    scale_multiplier: float = 1.0

    @property
    def source(self) -> Path:
        return GENERATED_ROOT / f"{self.name}.png"


@dataclass(frozen=True)
class AnimationRow:
    name: str
    chunks: tuple[Chunk, ...]

    @property
    def frame_count(self) -> int:
        return sum(chunk.frame_count for chunk in self.chunks)


ROWS = (
    AnimationRow("idle", (Chunk("idle_61_68", 8),)),
    AnimationRow(
        "walk",
        (
            Chunk("walk_a_70_75", 6, scale_multiplier=0.89),
            Chunk("walk_b_76_80", 5, scale_multiplier=0.89),
        ),
    ),
    AnimationRow("dash", (Chunk("dash_81_89", 9),)),
    AnimationRow("jump", (Chunk("jump_a_90_95", 6), Chunk("jump_b_96_101", 6))),
    AnimationRow("standing_punches", (Chunk("standing_punches_200_209", 10),)),
    AnimationRow("standing_kicks", (Chunk("standing_kicks_210_217", 8),)),
    AnimationRow("crouch_punches", (Chunk("crouch_punches_218_226", 9),)),
    AnimationRow(
        "crouch_kicks",
        (Chunk("crouch_kicks_a_227_233", 7), Chunk("crouch_kicks_b_234_239", 6)),
    ),
    AnimationRow("air_attacks_a", (Chunk("air_attacks_a_240_249", 10),)),
    AnimationRow("air_attacks_b", (Chunk("air_attacks_b_250_259", 10),)),
    AnimationRow(
        "reactions",
        (Chunk("../world_warrior_reactions_v3", 10),),
    ),
    AnimationRow(
        "hadouken",
        (Chunk("hadouken_277_279", 3, rows=1, scale_multiplier=0.59),),
    ),
    AnimationRow(
        "shoryuken",
        (Chunk("shoryuken_280_285", 6, scale_multiplier=0.94),),
    ),
    AnimationRow(
        "tatsumaki",
        (
            Chunk("tatsumaki_a_286_291", 6, scale_multiplier=0.95),
            Chunk("tatsumaki_b_292_297", 6, scale_multiplier=0.95),
        ),
    ),
    AnimationRow(
        "super",
        (Chunk("hadouken_277_279", 3, rows=1, scale_multiplier=0.59),),
    ),
)


def main() -> int:
    try:
        from PIL import Image
    except ImportError:
        print("ERROR: Pillow is required.")
        return 1

    chunks = {
        chunk
        for row in ROWS
        for chunk in row.chunks
    }
    missing = [str(chunk.source) for chunk in chunks if not chunk.source.exists()]
    if missing:
        print("ERROR: Missing V4 generated sheets:")
        for path in missing:
            print(f"  {path}")
        return 2

    extracted: dict[str, list] = {}
    for chunk in chunks:
        with Image.open(chunk.source) as source_image:
            source = source_image.convert("RGBA")

        poses = extract_segmented_poses(
            source,
            SheetSpec(
                row=0,
                name=chunk.name,
                source=chunk.source,
                columns=min(5, chunk.frame_count),
                rows=chunk.rows,
                frame_count=chunk.frame_count,
                segment_poses=True,
            ),
            Image,
        )
        extracted[chunk.name] = [
            normalize_pose(pose, Image, chunk.scale_multiplier)
            for pose in poses
        ]
        del source
        del poses
        gc.collect()

    OUTPUT_FRAMES.mkdir(parents=True, exist_ok=True)
    atlas = Image.new(
        "RGBA",
        (FRAME_SIZE * SHEET_COLUMNS, FRAME_SIZE * SHEET_ROWS),
        (0, 0, 0, 0),
    )

    for row_index, animation in enumerate(ROWS):
        frames = [
            frame
            for chunk in animation.chunks
            for frame in extracted[chunk.name]
        ]
        if len(frames) != animation.frame_count:
            raise RuntimeError(
                f"{animation.name}: packed {len(frames)} frames, "
                f"expected {animation.frame_count}"
            )

        for column, frame in enumerate(frames):
            frame.save(
                OUTPUT_FRAMES
                / f"row{row_index:02d}_{animation.name}_frame{column:02d}.png"
            )
            atlas.alpha_composite(
                frame,
                (column * FRAME_SIZE, row_index * FRAME_SIZE),
            )

        print(
            f"Packed row {row_index:02d}: {animation.name} "
            f"({animation.frame_count} exact frames)"
        )

    atlas.save(OUTPUT)
    print(f"Saved {OUTPUT} ({atlas.width}x{atlas.height})")
    return 0


def normalize_pose(frame, image_module, scale_multiplier: float):
    alpha_box = frame.getchannel("A").getbbox()
    canvas = image_module.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
    if alpha_box is None:
        return canvas

    content = frame.crop(alpha_box)
    fit_scale = min(
        SOURCE_SCALE * scale_multiplier,
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
