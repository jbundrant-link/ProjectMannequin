"""Audit top-down stage floor materials for directional banding.

A stage floor is projected onto a plane the camera views at a shallow angle, so
a material whose detail runs in horizontal courses collapses into screen-wide
stripes instead of reading as ground. The tell is anisotropy: variation across
the texture's rows collapses while variation down its columns stays high.

Reports the ratio of horizontal to vertical variation and fails any floor whose
detail is too directional to survive the stage camera.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("floors", nargs="+", type=Path)
    parser.add_argument("--report", type=Path)
    parser.add_argument(
        "--min-anisotropy-ratio",
        type=float,
        default=0.55,
        help="Lowest tolerated horizontal/vertical variation ratio.",
    )
    return parser.parse_args()


def measure(path: Path) -> dict[str, object]:
    with Image.open(path).convert("RGB") as image:
        pixels = np.asarray(image, dtype=np.float32)
        size = [image.width, image.height]

    horizontal = float(pixels.std(axis=1).mean())
    vertical = float(pixels.std(axis=0).mean())
    ratio = horizontal / vertical if vertical > 0.01 else 0.0
    return {
        "floor": path.as_posix(),
        "size": size,
        "horizontal_variation": round(horizontal, 3),
        "vertical_variation": round(vertical, 3),
        "anisotropy_ratio": round(ratio, 4),
    }


def main() -> int:
    args = parse_args()
    floors = []
    failed = False
    for path in args.floors:
        entry = measure(path)
        entry["passed"] = entry["anisotropy_ratio"] >= args.min_anisotropy_ratio
        failed |= not entry["passed"]
        floors.append(entry)

    report = {
        "min_anisotropy_ratio": args.min_anisotropy_ratio,
        "floors": floors,
        "passed": not failed,
    }
    text = json.dumps(report, indent=2)
    print(text)
    if args.report is not None:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(text + "\n", encoding="utf-8")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
