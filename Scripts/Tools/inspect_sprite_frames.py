from __future__ import annotations

import re
import struct
import sys
from pathlib import Path


DEFAULT_DIR = Path("Assets/Sprites/Mannequin")
MIN_FRAME_SIZE = 32


def natural_key(path: Path) -> list[object]:
    return [int(part) if part.isdigit() else part.lower() for part in re.split(r"(\d+)", path.name)]


def png_dimensions(path: Path) -> tuple[int, int]:
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG file")

    return struct.unpack(">II", data[16:24])


def main() -> int:
    root = Path(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_DIR
    files = sorted(root.glob("row*_frame*.png"), key=natural_key)

    if not files:
        print(f"No row*_frame*.png files found in {root}")
        return 1

    bad_files: list[Path] = []
    for path in files:
        try:
            width, height = png_dimensions(path)
            size_label = f"{width}x{height}"
            byte_count = path.stat().st_size
            status = "OK" if width >= MIN_FRAME_SIZE and height >= MIN_FRAME_SIZE else "BAD"
            print(f"{status:3} {path.name:28} {size_label:10} {byte_count:8} bytes")
            if status == "BAD":
                bad_files.append(path)
        except Exception as exc:
            print(f"ERR {path.name:28} {exc}")
            bad_files.append(path)

    if bad_files:
        print()
        print(f"{len(bad_files)} file(s) are too small or invalid. These are not usable animation frames.")
        return 2

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
