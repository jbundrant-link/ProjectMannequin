#!/usr/bin/env python3
"""Normalize one reviewed identity pilot onto a contained chroma-green canvas."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

from process_higgsfield_sprites import remove_background


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--source-background", choices=("green", "white"), required=True)
    parser.add_argument("--size", type=int, default=2048)
    parser.add_argument("--margin", type=int, default=192)
    parser.add_argument("--report", type=Path)
    return parser.parse_args()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    args = parse_args()
    if args.size <= 0:
        raise ValueError("size must be positive")
    if args.margin <= 0 or args.margin * 2 >= args.size:
        raise ValueError("margin must leave positive canvas space")

    source = Image.open(args.source).convert("RGBA")
    keyed = remove_background(
        source,
        Image,
        key_white_background=args.source_background == "white",
    )
    source_bounds = keyed.getchannel("A").getbbox()
    if source_bounds is None:
        raise ValueError(f"{args.source}: no figure remains after background removal")

    figure = keyed.crop(source_bounds)
    available = args.size - args.margin * 2
    scale = min(available / figure.width, available / figure.height, 1.0)
    figure = figure.resize(
        (
            max(1, round(figure.width * scale)),
            max(1, round(figure.height * scale)),
        ),
        Image.Resampling.LANCZOS,
    )

    x = (args.size - figure.width) // 2
    y = (args.size - figure.height) // 2
    canvas = Image.new("RGBA", (args.size, args.size), (0, 255, 0, 255))
    canvas.alpha_composite(figure, (x, y))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(args.output)
    normalized_bounds = [x, y, x + figure.width, y + figure.height]

    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(
            json.dumps(
                {
                    "source": args.source.as_posix(),
                    "source_sha256": sha256(args.source),
                    "output": args.output.as_posix(),
                    "output_sha256": sha256(args.output),
                    "source_background": args.source_background,
                    "canvas_size": [args.size, args.size],
                    "minimum_margin": args.margin,
                    "source_bounds": list(source_bounds),
                    "normalized_bounds": normalized_bounds,
                    "scale": round(scale, 6),
                },
                indent=2,
            )
            + "\n",
            encoding="utf-8",
        )

    print(
        f"SAVED {args.output} scale={scale:.4f} "
        f"bounds={normalized_bounds}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())