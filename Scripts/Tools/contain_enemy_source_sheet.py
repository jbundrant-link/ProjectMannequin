#!/usr/bin/env python3
"""Contain separated source figures on a pure-green canvas without redrawing."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

from compose_enemy_sheet import extract_connected_components


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--background", choices=("green", "magenta"), default="green")
    parser.add_argument("--minimum-area", type=int, default=50_000)
    parser.add_argument("--expected-count", type=int, required=True)
    parser.add_argument("--margin", type=int, default=8)
    parser.add_argument("--report", type=Path)
    return parser.parse_args()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    args = parse_args()
    source = Image.open(args.source)
    components = extract_connected_components(
        args.source,
        args.background,
        args.minimum_area,
    )
    if len(components) != args.expected_count:
        raise ValueError(
            f"{args.source}: found {len(components)} components, "
            f"expected {args.expected_count}"
        )

    left = min(int(component["bbox"][0]) for component in components)
    top = min(int(component["bbox"][1]) for component in components)
    right = max(int(component["bbox"][2]) for component in components)
    bottom = max(int(component["bbox"][3]) for component in components)
    available_width = source.width - 2 * args.margin
    available_height = source.height - 2 * args.margin
    scale = min(
        available_width / (right - left),
        available_height / (bottom - top),
        1.0,
    )
    crop = source.convert("RGBA").crop((left, top, right, bottom))
    crop = crop.resize(
        (
            max(1, round(crop.width * scale)),
            max(1, round(crop.height * scale)),
        ),
        Image.Resampling.LANCZOS,
    )
    color = (0, 255, 0, 255) if args.background == "green" else (255, 0, 255, 255)
    canvas = Image.new("RGBA", source.size, color)
    x = (source.width - crop.width) // 2
    y = (source.height - crop.height) // 2
    canvas.alpha_composite(crop, (x, y))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(args.output)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(
            json.dumps(
                {
                    "source": args.source.as_posix(),
                    "source_sha256": sha256(args.source),
                    "output": args.output.as_posix(),
                    "output_sha256": sha256(args.output),
                    "background": args.background,
                    "expected_count": args.expected_count,
                    "source_bounds": [left, top, right, bottom],
                    "normalized_bounds": [x, y, x + crop.width, y + crop.height],
                    "scale": round(scale, 6),
                    "minimum_margin": args.margin,
                },
                indent=2,
            )
            + "\n",
            encoding="utf-8",
        )
    print(f"SAVED {args.output} scale={scale:.4f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())