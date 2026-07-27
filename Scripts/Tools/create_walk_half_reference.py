#!/usr/bin/env python3
"""Rearrange one four-frame row of a 4x2 walk sheet into an undistorted 2x2 reference."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--source-row", type=int, choices=(0, 1), default=0)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    source = Image.open(args.source).convert("RGB")
    if source.width % 4 or source.height % 2:
        raise ValueError(f"{args.source} is not evenly divisible into a 4x2 grid")

    cell_width = source.width // 4
    cell_height = source.height // 2
    quadrant_width = source.width // 2
    quadrant_height = source.height // 2
    background = source.getpixel((0, 0))
    output = Image.new("RGB", source.size, background)

    for index in range(4):
        cell = source.crop(
            (
                index * cell_width,
                args.source_row * cell_height,
                (index + 1) * cell_width,
                (args.source_row + 1) * cell_height,
            )
        )
        target_column = index % 2
        target_row = index // 2
        target_x = target_column * quadrant_width + (quadrant_width - cell_width) // 2
        target_y = target_row * quadrant_height + (quadrant_height - cell_height) // 2
        output.paste(cell, (target_x, target_y))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    output.save(args.output)
    print(f"SAVED {args.output} ({output.width}x{output.height})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())