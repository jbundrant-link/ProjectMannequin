#!/usr/bin/env python3
"""Key one green-screen sprite to real alpha and report runtime geometry."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

from compose_enemy_sheet import load_source_rgba


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--target-world-height", type=float, required=True)
    return parser.parse_args()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    args = parse_args()
    if args.target_world_height <= 0.0:
        raise ValueError("target world height must be positive")

    source = Image.open(args.source)
    keyed = load_source_rgba(args.source, "green")
    bounds = keyed.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"{args.source}: no keyed subject")

    visible_width = bounds[2] - bounds[0]
    visible_height = bounds[3] - bounds[1]
    pixel_size = args.target_world_height / visible_height
    ground_offset = bounds[3] - keyed.height * 0.5

    args.output.parent.mkdir(parents=True, exist_ok=True)
    keyed.save(args.output)
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(
        json.dumps(
            {
                "source": args.source.as_posix(),
                "source_sha256": sha256(args.source),
                "source_mode": source.mode,
                "output": args.output.as_posix(),
                "output_sha256": sha256(args.output),
                "output_mode": keyed.mode,
                "canvas_size": [keyed.width, keyed.height],
                "alpha_bounds": list(bounds),
                "visible_size": [visible_width, visible_height],
                "target_world_height": args.target_world_height,
                "suggested_pixel_size": round(pixel_size, 8),
                "suggested_ground_offset_pixels": round(ground_offset, 3),
            },
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    print(
        f"SAVED {args.output} bounds={bounds} "
        f"pixel_size={pixel_size:.8f} ground_offset={ground_offset:.3f}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())