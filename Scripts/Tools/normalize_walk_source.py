#!/usr/bin/env python3
"""Normalize eight separated walk figures into a contained 4x2 source grid."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from PIL import Image

from compose_enemy_sheet import extract_connected_components


COLUMNS = 4
ROWS = 2


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--background", choices=("green", "magenta"), default="green")
    parser.add_argument("--minimum-area", type=int, default=50_000)
    parser.add_argument("--cell-margin", type=int, default=12)
    parser.add_argument("--report", type=Path)
    return parser.parse_args()


def row_major(components: list[dict[str, object]]) -> list[dict[str, object]]:
    ordered = sorted(components, key=lambda item: float(item["center_y"]))
    top = sorted(ordered[:COLUMNS], key=lambda item: float(item["center_x"]))
    bottom = sorted(ordered[COLUMNS:], key=lambda item: float(item["center_x"]))
    return top + bottom


def main() -> int:
    args = parse_args()
    source = Image.open(args.source)
    if source.width % COLUMNS or source.height % ROWS:
        raise ValueError(f"{args.source} is not divisible by {COLUMNS}x{ROWS}")

    components = extract_connected_components(
        args.source,
        args.background,
        args.minimum_area,
    )
    if len(components) != COLUMNS * ROWS:
        raise ValueError(
            f"{args.source}: found {len(components)} complete figures, expected 8"
        )
    components = row_major(components)

    cell_width = source.width // COLUMNS
    cell_height = source.height // ROWS
    maximum_width = max(component["image"].width for component in components)
    maximum_height = max(component["image"].height for component in components)
    scale = min(
        (cell_width - 2 * args.cell_margin) / maximum_width,
        (cell_height - 2 * args.cell_margin) / maximum_height,
        1.0,
    )
    background_color = (0, 255, 0, 255) if args.background == "green" else (255, 0, 255, 255)
    output = Image.new("RGBA", source.size, background_color)
    frames = []

    for index, component in enumerate(components):
        figure = component["image"]
        figure = figure.resize(
            (
                max(1, round(figure.width * scale)),
                max(1, round(figure.height * scale)),
            ),
            Image.Resampling.LANCZOS,
        )
        column = index % COLUMNS
        row = index // COLUMNS
        x = column * cell_width + (cell_width - figure.width) // 2
        y = (row + 1) * cell_height - args.cell_margin - figure.height
        output.alpha_composite(figure, (x, y))
        frames.append(
            {
                "frame": index + 1,
                "source_bounds": component["bbox"],
                "normalized_bounds": [x, y, x + figure.width, y + figure.height],
            }
        )

    args.output.parent.mkdir(parents=True, exist_ok=True)
    output.convert("RGB").save(args.output)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(
            json.dumps(
                {
                    "source": str(args.source).replace("\\", "/"),
                    "output": str(args.output).replace("\\", "/"),
                    "layout": [COLUMNS, ROWS],
                    "scale": round(scale, 6),
                    "cell_margin": args.cell_margin,
                    "frames": frames,
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