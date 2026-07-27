#!/usr/bin/env python3
"""Extract individual frames from a 4x2 walk source as square pose references."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output_directory", type=Path)
    parser.add_argument("--prefix", default="walk_pose")
    parser.add_argument("--start-frame", type=int, default=0)
    parser.add_argument("--frame-count", type=int, default=8)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    source = Image.open(args.source).convert("RGBA")
    if source.width % 4 or source.height % 2:
        raise ValueError(f"{args.source} is not evenly divisible into a 4x2 grid")
    if args.start_frame < 0 or args.start_frame + args.frame_count > 8:
        raise ValueError("requested frames must fit within the 4x2 source")

    cell_width = source.width // 4
    cell_height = source.height // 2
    output_size = max(source.width, source.height)
    args.output_directory.mkdir(parents=True, exist_ok=True)

    for frame_index in range(args.start_frame, args.start_frame + args.frame_count):
        column = frame_index % 4
        row = frame_index // 4
        cell = source.crop(
            (
                column * cell_width,
                row * cell_height,
                (column + 1) * cell_width,
                (row + 1) * cell_height,
            )
        )
        bounds = cell.getchannel("A").getbbox()
        if bounds is None:
            raise ValueError(f"frame {frame_index + 1} is empty")

        figure = cell.crop(bounds)
        scale = min(
            (output_size * 0.72) / figure.width,
            (output_size * 0.82) / figure.height,
        )
        figure = figure.resize(
            (
                max(1, round(figure.width * scale)),
                max(1, round(figure.height * scale)),
            ),
            Image.Resampling.LANCZOS,
        )
        canvas = Image.new("RGBA", (output_size, output_size), (0, 255, 0, 255))
        canvas.alpha_composite(
            figure,
            (
                (output_size - figure.width) // 2,
                round(output_size * 0.91) - figure.height,
            ),
        )
        output = args.output_directory / f"{args.prefix}_{frame_index + 1}.png"
        canvas.convert("RGB").save(output)
        print(f"SAVED {output}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())