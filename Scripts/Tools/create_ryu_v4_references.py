from __future__ import annotations

from dataclasses import dataclass
from math import ceil
from pathlib import Path

from pack_ryu_prototype_sprites import prepare_frame


SOURCE_DIR = Path("Assets/Sprites/Concepts/Ryu")
OUTPUT_DIR = Path("Assets/Sprites/Ryu/Higgsfield/V4References")
PANEL_SIZE = 256
MAX_COLUMNS = 5
GREEN = (0, 255, 0, 255)


@dataclass(frozen=True)
class ReferenceChunk:
    name: str
    frames: tuple[int, ...]


CHUNKS = (
    ReferenceChunk("idle_61_68", tuple(range(61, 69))),
    ReferenceChunk("walk_a_70_75", tuple(range(70, 76))),
    ReferenceChunk("walk_b_76_80", tuple(range(76, 81))),
    ReferenceChunk("dash_81_89", tuple(range(81, 90))),
    ReferenceChunk("jump_a_90_95", tuple(range(90, 96))),
    ReferenceChunk("jump_b_96_101", tuple(range(96, 102))),
    ReferenceChunk("standing_punches_200_209", tuple(range(200, 210))),
    ReferenceChunk("standing_kicks_210_217", tuple(range(210, 218))),
    ReferenceChunk("crouch_punches_218_226", tuple(range(218, 227))),
    ReferenceChunk("crouch_kicks_a_227_233", tuple(range(227, 234))),
    ReferenceChunk("crouch_kicks_b_234_239", tuple(range(234, 240))),
    ReferenceChunk("air_attacks_a_240_249", tuple(range(240, 250))),
    ReferenceChunk("air_attacks_b_250_259", tuple(range(250, 260))),
    ReferenceChunk("hadouken_277_279", tuple(range(277, 280))),
    ReferenceChunk("shoryuken_280_285", tuple(range(280, 286))),
    ReferenceChunk("tatsumaki_a_286_291", tuple(range(286, 292))),
    ReferenceChunk("tatsumaki_b_292_297", tuple(range(292, 298))),
)


def main() -> int:
    try:
        from PIL import Image
    except ImportError:
        print("ERROR: Pillow is required.")
        return 1

    missing = [
        source_path(frame)
        for chunk in CHUNKS
        for frame in chunk.frames
        if not source_path(frame).exists()
    ]
    if missing:
        print("ERROR: Missing source frames:")
        for path in missing:
            print(f"  {path}")
        return 2

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    for chunk in CHUNKS:
        columns = reference_columns(len(chunk.frames))
        rows = max(2, ceil(len(chunk.frames) / columns))
        board = Image.new(
            "RGBA",
            (columns * PANEL_SIZE, rows * PANEL_SIZE),
            GREEN,
        )
        for index, source_index in enumerate(chunk.frames):
            frame = prepare_frame(source_path(source_index), Image)
            column = index % columns
            row = index // columns
            board.alpha_composite(frame, (column * PANEL_SIZE, row * PANEL_SIZE))

        output = OUTPUT_DIR / f"{chunk.name}.png"
        board.save(output)
        print(f"Saved {output} ({len(chunk.frames)} poses)")

    return 0


def source_path(index: int) -> Path:
    return SOURCE_DIR / f"MVC2_Ryu_{index}.png"


def reference_columns(frame_count: int) -> int:
    if frame_count <= 3:
        return frame_count
    if frame_count <= 6:
        return ceil(frame_count / 2)
    if frame_count <= 8:
        return 4
    return MAX_COLUMNS


if __name__ == "__main__":
    raise SystemExit(main())
