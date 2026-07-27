"""Detects real gaps where a stage's background shows through its geometry.

The defect this catches is the one a player reports as "the background is
showing through": stacked stage panels that do not quite meet, leaving the
environment clear colour visible across the frame.

Method, and why it is this one. Each stage is rendered twice with a different
forced clear colour. A pixel that changes between the two renders is showing the
background and nothing else, so it is a genuine gap. A pixel that does not change
is covered by geometry.

This replaced a single-render heuristic that matched pixels against the declared
clear colour. That approach produced false positives on every stage, because
these paintings contain flat dark horizontal lines - shadow gaps between deck
levels, mortar courses - that are near-identical to the clear colour and just as
uniform across the frame. Neither colour distance nor run uniformity could
separate painted art from a real hole. Two renders can, exactly.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

# A channel difference above this counts as "this pixel showed the background".
# The two forced colours are far apart, so any real gap swings well past it.
CHANNEL_DELTA = 30


def longest_run(mask_row: np.ndarray) -> int:
    best = current = 0
    for value in mask_row:
        current = current + 1 if value else 0
        if current > best:
            best = current
    return best


def measure(first: Path, second: Path) -> dict:
    a = np.asarray(Image.open(first).convert("RGB"), dtype=np.int16)
    b = np.asarray(Image.open(second).convert("RGB"), dtype=np.int16)
    if a.shape != b.shape:
        raise ValueError(f"capture pair differs in size: {first} vs {second}")

    height, width = a.shape[:2]
    gap = np.abs(a - b).max(axis=2) > CHANNEL_DELTA
    runs = np.array([longest_run(gap[row]) for row in range(height)])
    worst_row = int(runs.argmax())
    return {
        "stage": first.stem.replace("_clear_a", ""),
        "width": width,
        "height": height,
        "gap_pixels": int(gap.sum()),
        "gap_fraction": round(float(gap.mean()), 6),
        "worst_row": worst_row,
        "worst_run_pixels": int(runs[worst_row]),
        "worst_run_fraction": round(float(runs[worst_row]) / width, 4),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--pair",
        nargs=2,
        action="append",
        metavar=("FIRST", "SECOND"),
        required=True,
        help="Two captures of one stage rendered with different clear colours.")
    parser.add_argument(
        "--max-run-fraction",
        type=float,
        default=0.01,
        help="A contiguous run of background wider than this fraction is a gap.")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    results, failures = [], []
    for first, second in args.pair:
        first, second = Path(first), Path(second)
        for path in (first, second):
            if not path.exists():
                print(f"MISSING {path}")
                return 2

        result = measure(first, second)
        failed = result["worst_run_fraction"] > args.max_run_fraction
        result["status"] = "GAP" if failed else "clean"
        if failed:
            failures.append(result)
        results.append(result)
        print(
            f"{result['stage']:32s} gapPx={result['gap_pixels']:7d} "
            f"worstRun={result['worst_run_fraction'] * 100:6.2f}% "
            f"row={result['worst_row']:4d}  {result['status']}")

    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(
            json.dumps(
                {
                    "method": "two-clear-colour differential",
                    "channel_delta": CHANNEL_DELTA,
                    "max_run_fraction": args.max_run_fraction,
                    "stages": results,
                    "gap_count": len(failures),
                },
                indent=2),
            encoding="utf-8")
        print(f"report={args.report}")

    if failures:
        print(f"FAIL {len(failures)} stage(s) show the background through geometry.")
        return 1

    print(f"OK {len(results)} stage(s) checked, no background visible through geometry.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
