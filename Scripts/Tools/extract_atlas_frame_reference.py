#!/usr/bin/env python3
"""Extract one atlas frame into a large square chroma-green identity reference."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--columns", type=int, required=True)
    parser.add_argument("--rows", type=int, required=True)
    parser.add_argument("--frame", type=int, default=0)
    parser.add_argument("--size", type=int, default=2048)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    atlas = Image.open(args.source).convert("RGBA")
    if atlas.width % args.columns or atlas.height % args.rows:
        raise ValueError(f"{args.source} is not divisible by {args.columns}x{args.rows}")
    if not 0 <= args.frame < args.columns * args.rows:
        raise ValueError("frame is outside the atlas")

    frame_width = atlas.width // args.columns
    frame_height = atlas.height // args.rows
    column = args.frame % args.columns
    row = args.frame // args.columns
    frame = atlas.crop(
        (
            column * frame_width,
            row * frame_height,
            (column + 1) * frame_width,
            (row + 1) * frame_height,
        )
    )
    bounds = frame.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"frame {args.frame} is empty")

    figure = frame.crop(bounds)
    scale = min(
        (args.size * 0.72) / figure.width,
        (args.size * 0.82) / figure.height,
    )
    figure = figure.resize(
        (
            max(1, round(figure.width * scale)),
            max(1, round(figure.height * scale)),
        ),
        Image.Resampling.LANCZOS,
    )
    canvas = Image.new("RGBA", (args.size, args.size), (0, 255, 0, 255))
    canvas.alpha_composite(
        figure,
        (
            (args.size - figure.width) // 2,
            round(args.size * 0.91) - figure.height,
        ),
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(args.output)
    print(f"SAVED {args.output} ({args.size}x{args.size})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())