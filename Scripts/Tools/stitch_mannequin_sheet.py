from __future__ import annotations

import re
import sys
from pathlib import Path


ROOT = Path("Assets/Sprites/Mannequin")
OUT_SHEET = ROOT / "mannequin_sheet.png"
COLUMNS = 10
ROWS = 6
MIN_VALID_SIZE = 32

ROW_SOURCES = {
    0: ("idle", "row0_idle_frame*.png"),
    1: ("walk", "row1_walk_frame*.png"),
    2: ("dash", "row2_dash_frame*.png"),
    3: ("jump", "row3_jump_frame*.png"),
    4: ("attacks", "row4_attacks_frame*.png"),
    5: ("misc", "row5_misc_frame*.png"),
}

FALLBACK_ROWS = {
    4: 0,  # use idle as a placeholder for attacks until attack row generation succeeds
    5: 0,  # use idle as a placeholder for hit/form/death until misc row generation succeeds
}


def main() -> int:
    try:
        from PIL import Image
    except ImportError:
        print("ERROR: Pillow is required.")
        print("Install it with: python -m pip install -r Scripts/Tools/requirements.txt")
        return 1

    rows: dict[int, list[Path]] = {}
    for row_index, (row_name, pattern) in ROW_SOURCES.items():
        frames = valid_frames(ROOT.glob(pattern), Image)
        if not frames and row_index in FALLBACK_ROWS:
            fallback_index = FALLBACK_ROWS[row_index]
            fallback_name, fallback_pattern = ROW_SOURCES[fallback_index]
            frames = valid_frames(ROOT.glob(fallback_pattern), Image)
            print(f"Row {row_index} ({row_name}) missing; using row {fallback_index} ({fallback_name}) as placeholder.")

        if not frames:
            print(f"ERROR: Row {row_index} ({row_name}) has no valid frames.")
            return 2

        rows[row_index] = frames

    frame_width, frame_height = frame_size(rows)
    sheet = Image.new("RGBA", (frame_width * COLUMNS, frame_height * ROWS), (0, 0, 0, 0))

    for row_index in range(ROWS):
        frames = rows[row_index]
        for column in range(COLUMNS):
            frame_path = frames[min(column, len(frames) - 1)]
            with Image.open(frame_path) as image:
                frame = image.convert("RGBA")
                if frame.size != (frame_width, frame_height):
                    frame = fit_frame(frame, frame_width, frame_height, Image)

                sheet.alpha_composite(frame, (column * frame_width, row_index * frame_height))

    sheet.save(OUT_SHEET)
    print(f"Saved {OUT_SHEET} ({sheet.width}x{sheet.height})")
    print("Godot will load this automatically through CharacterVisualComponent.")
    return 0


def valid_frames(paths, image_module) -> list[Path]:
    result: list[Path] = []
    for path in sorted(paths, key=natural_key):
        try:
            with image_module.open(path) as image:
                width, height = image.size
            if width >= MIN_VALID_SIZE and height >= MIN_VALID_SIZE:
                result.append(path)
        except Exception:
            continue

    return result


def frame_size(rows: dict[int, list[Path]]) -> tuple[int, int]:
    from PIL import Image

    sizes: dict[tuple[int, int], int] = {}
    for frames in rows.values():
        for path in frames:
            with Image.open(path) as image:
                sizes[image.size] = sizes.get(image.size, 0) + 1

    return max(sizes, key=sizes.get)


def fit_frame(frame, width: int, height: int, image_module):
    frame.thumbnail((width, height), image_module.Resampling.LANCZOS)
    canvas = image_module.new("RGBA", (width, height), (0, 0, 0, 0))
    x = (width - frame.width) // 2
    y = height - frame.height
    canvas.alpha_composite(frame, (x, y))
    return canvas


def natural_key(path: Path) -> list[object]:
    return [int(part) if part.isdigit() else part.lower() for part in re.split(r"(\d+)", path.name)]


if __name__ == "__main__":
    raise SystemExit(main())
