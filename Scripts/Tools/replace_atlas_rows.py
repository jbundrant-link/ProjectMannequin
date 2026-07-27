#!/usr/bin/env python3
"""Replace selected rows in an atlas while preserving every untouched pixel."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("base", type=Path)
    parser.add_argument("patch", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--rows", type=int, nargs="+", required=True)
    parser.add_argument("--row-count", type=int, default=9)
    parser.add_argument("--report", type=Path)
    return parser.parse_args()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    args = parse_args()
    base = Image.open(args.base).convert("RGBA")
    patch = Image.open(args.patch).convert("RGBA")
    if base.size != patch.size:
        raise ValueError(f"atlas sizes differ: base={base.size}, patch={patch.size}")
    if base.height % args.row_count:
        raise ValueError(f"atlas height is not divisible by {args.row_count}")

    rows = sorted(set(args.rows))
    if not rows or rows[0] < 0 or rows[-1] >= args.row_count:
        raise ValueError(f"rows must be within 0..{args.row_count - 1}")

    row_height = base.height // args.row_count
    output = base.copy()
    for row in rows:
        box = (0, row * row_height, base.width, (row + 1) * row_height)
        output.paste(patch.crop(box), box)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    output.save(args.output)

    base_pixels = np.asarray(base)
    output_pixels = np.asarray(output)
    unchanged_rows = []
    changed_rows = []
    for row in range(args.row_count):
        row_slice = slice(row * row_height, (row + 1) * row_height)
        unchanged = bool(np.array_equal(base_pixels[row_slice], output_pixels[row_slice]))
        (unchanged_rows if unchanged else changed_rows).append(row)

    report = {
        "base": str(args.base).replace("\\", "/"),
        "patch": str(args.patch).replace("\\", "/"),
        "output": str(args.output).replace("\\", "/"),
        "base_sha256": sha256(args.base),
        "patch_sha256": sha256(args.patch),
        "output_sha256": sha256(args.output),
        "requested_rows": rows,
        "changed_rows": changed_rows,
        "unchanged_rows": unchanged_rows,
        "passed": changed_rows == rows,
    }
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())