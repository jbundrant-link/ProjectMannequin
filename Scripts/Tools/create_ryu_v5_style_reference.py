#!/usr/bin/env python3
"""Build a compact Ryu V4 design reference for the MUGEN V5 art pass."""

from pathlib import Path

from PIL import Image


SOURCE = Path("Assets/Sprites/Ryu/ryu_higgsfield_v4_sheet.png")
OUTPUT = Path("Assets/Sprites/Ryu/Higgsfield/V5References/ryu_v4_style_board.png")
DESIGN_ANCHOR_OUTPUT = Path(
    "Assets/Sprites/Ryu/Higgsfield/V5References/ryu_v4_design_anchor.png"
)
SOURCE_COLUMNS = 16
SOURCE_CELL_SIZE = 320
BOARD_COLUMNS = 4
BOARD_ROWS = 2
FRAME_INDICES = (0, 66, 84, 100, 132, 177, 194, 212)
GREEN = (0, 255, 0, 255)


def main() -> int:
    if not SOURCE.exists():
        print(f"ERROR: Missing {SOURCE}")
        return 1

    with Image.open(SOURCE) as source_image:
        source = source_image.convert("RGBA")

    board = Image.new(
        "RGBA",
        (BOARD_COLUMNS * SOURCE_CELL_SIZE, BOARD_ROWS * SOURCE_CELL_SIZE),
        GREEN,
    )
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    for output_index, frame_index in enumerate(FRAME_INDICES):
        source_column = frame_index % SOURCE_COLUMNS
        source_row = frame_index // SOURCE_COLUMNS
        frame = source.crop(
            (
                source_column * SOURCE_CELL_SIZE,
                source_row * SOURCE_CELL_SIZE,
                (source_column + 1) * SOURCE_CELL_SIZE,
                (source_row + 1) * SOURCE_CELL_SIZE,
            )
        )
        destination = (
            (output_index % BOARD_COLUMNS) * SOURCE_CELL_SIZE,
            (output_index // BOARD_COLUMNS) * SOURCE_CELL_SIZE,
        )
        board.alpha_composite(frame, destination)
        if output_index == 0:
            anchor = Image.new("RGBA", (SOURCE_CELL_SIZE, SOURCE_CELL_SIZE), GREEN)
            anchor.alpha_composite(frame)
            anchor.save(DESIGN_ANCHOR_OUTPUT)

    board.save(OUTPUT)
    print(f"Saved {OUTPUT}")
    print(f"Saved {DESIGN_ANCHOR_OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
