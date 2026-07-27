#!/usr/bin/env python3
"""Split a transparent two-edge stage source into trimmed left/right assets."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("left_output", type=Path)
    parser.add_argument("right_output", type=Path)
    parser.add_argument("--margin", type=int, default=22)
    return parser.parse_args()


def trim_half(image: Image.Image, box: tuple[int, int, int, int], margin: int) -> Image.Image:
    half = image.crop(box)
    alpha_bounds = half.getchannel("A").getbbox()
    if alpha_bounds is None:
        raise ValueError(f"No visible pixels found in source half {box}.")
    left = max(0, alpha_bounds[0] - margin)
    top = max(0, alpha_bounds[1] - margin)
    right = min(half.width, alpha_bounds[2] + margin)
    bottom = min(half.height, alpha_bounds[3] + margin)
    return half.crop((left, top, right, bottom))


def main() -> int:
    args = parse_args()
    image = Image.open(args.source).convert("RGBA")
    midpoint = image.width // 2
    left = trim_half(image, (0, 0, midpoint, image.height), args.margin)
    right = trim_half(
        image,
        (midpoint, 0, image.width, image.height),
        args.margin,
    )
    args.left_output.parent.mkdir(parents=True, exist_ok=True)
    args.right_output.parent.mkdir(parents=True, exist_ok=True)
    left.save(args.left_output)
    right.save(args.right_output)
    print(f"SAVED {args.left_output} {left.size} alpha={left.getchannel('A').getbbox()}")
    print(f"SAVED {args.right_output} {right.size} alpha={right.getchannel('A').getbbox()}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())