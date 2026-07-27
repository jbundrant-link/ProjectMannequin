"""Locate where a layered stage backdrop's painted content ends.

These backdrops are floorless elevations. Some finish with rows of flat filler
below the architecture, which must sit under the gameplay floor rather than
banding across the frame; others run their architecture to the image edge.

Anchoring the image's bottom edge to the floor exposes the filler. Anchoring by
brightness instead buries real architecture and cuts the bases off columns and
doorways. The boundary that matters is therefore the last row of painted
content: everything below it is filler and belongs beneath the floor.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("backdrops", nargs="+", type=Path)
    parser.add_argument("--report", type=Path)
    parser.add_argument(
        "--filler-flatness",
        type=float,
        default=6.0,
        help="Highest horizontal variation still treated as flat filler.",
    )
    return parser.parse_args()


def measure(path: Path, filler_flatness: float) -> dict[str, object]:
    with Image.open(path).convert("RGB") as image:
        pixels = np.asarray(image, dtype=np.float32)

    height = pixels.shape[0]
    # Filler is flat, not necessarily black: these backdrops pad with a uniform
    # dark grey around luminance 20, which a brightness threshold walks straight
    # past and then keeps eating into real architecture.
    filler_rows = pixels.std(axis=1).mean(axis=1) <= filler_flatness

    # Only a filler run that reaches the bottom edge belongs under the floor. A
    # dark band higher up is painted content, such as a shadowed interior.
    row = height - 1
    if not filler_rows[row]:
        ground_line = 1.0
        filler_height = 0
    else:
        while row >= 0 and filler_rows[row]:
            row -= 1
        ground_line = (row + 1) / height
        filler_height = height - (row + 1)

    return {
        "backdrop": path.as_posix(),
        "size": [pixels.shape[1], height],
        "ground_line_fraction": round(ground_line, 4),
        "filler_rows": filler_height,
        "filler_percent": round(100.0 * filler_height / height, 2),
    }


def main() -> int:
    args = parse_args()
    report = {
        "filler_flatness": args.filler_flatness,
        "backdrops": [
            measure(path, args.filler_flatness) for path in args.backdrops
        ],
    }
    text = json.dumps(report, indent=2)
    print(text)
    if args.report is not None:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(text + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
