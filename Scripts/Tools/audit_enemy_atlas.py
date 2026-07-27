#!/usr/bin/env python3
"""Audit a composed 10x9 enemy atlas for Project Mannequin runtime use."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image


FRAME_SIZE = 256
SHEET_COLUMNS = 10
SHEET_ROWS = 9
USED_ROWS = 6
GROUND_BASELINE = 248
CELL_PADDING = 6


def parse_frame(value: str) -> tuple[int, int]:
    try:
        row, column = (int(part) for part in value.split(":", maxsplit=1))
    except (TypeError, ValueError) as exception:
        raise argparse.ArgumentTypeError(
            f"expected ROW:COLUMN, received {value!r}"
        ) from exception
    if not 0 <= row < SHEET_ROWS or not 0 <= column < SHEET_COLUMNS:
        raise argparse.ArgumentTypeError(f"frame {value!r} is outside the 10x9 atlas")
    return row, column


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("atlas", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument(
        "--wide-frame",
        action="append",
        type=parse_frame,
        default=[],
        help="ROW:COLUMN frame that must satisfy --minimum-wide-frame-width",
    )
    parser.add_argument("--minimum-wide-frame-width", type=int, default=200)
    parser.add_argument("--maximum-green-dominant-pixels", type=int, default=0)
    return parser.parse_args()


def frame_report(pixels: np.ndarray, row: int, column: int) -> dict[str, object]:
    cell = pixels[
        row * FRAME_SIZE : (row + 1) * FRAME_SIZE,
        column * FRAME_SIZE : (column + 1) * FRAME_SIZE,
    ]
    alpha = cell[..., 3]
    y_values, x_values = np.nonzero(alpha > 8)
    if len(x_values) == 0:
        return {
            "row": row,
            "column": column,
            "bbox": None,
            "green_dominant_pixels": 0,
        }

    visible_rgb = cell[..., :3][alpha > 32].astype(np.int16)
    green_dominant = (
        (visible_rgb[:, 1] - visible_rgb[:, 0] > 25)
        & (visible_rgb[:, 1] - visible_rgb[:, 2] > 25)
    )
    return {
        "row": row,
        "column": column,
        "bbox": [
            int(x_values.min()),
            int(y_values.min()),
            int(x_values.max()) + 1,
            int(y_values.max()) + 1,
        ],
        "green_dominant_pixels": int(np.count_nonzero(green_dominant)),
    }


def main() -> int:
    args = parse_args()
    source = Image.open(args.atlas)
    source_mode = source.mode
    image = source.convert("RGBA")
    pixels = np.asarray(image)
    frames = [
        frame_report(pixels, row, column)
        for row in range(SHEET_ROWS)
        for column in range(SHEET_COLUMNS)
    ]
    used_frames = [frame for frame in frames if frame["row"] < USED_ROWS]
    reserve_frames = [frame for frame in frames if frame["row"] >= USED_ROWS]
    used_bounds = [frame["bbox"] for frame in used_frames if frame["bbox"]]
    indexed_frames = {
        (int(frame["row"]), int(frame["column"])): frame for frame in frames
    }
    wide_frame_widths = {
        f"{row}:{column}": (
            indexed_frames[(row, column)]["bbox"][2]
            - indexed_frames[(row, column)]["bbox"][0]
            if indexed_frames[(row, column)]["bbox"]
            else 0
        )
        for row, column in args.wide_frame
    }

    report = {
        "path": str(args.atlas).replace("\\", "/"),
        "sha256": hashlib.sha256(args.atlas.read_bytes()).hexdigest(),
        "mode": source_mode,
        "size": list(image.size),
        "nonempty_used_frames": sum(frame["bbox"] is not None for frame in used_frames),
        "transparent_reserve_frames": sum(
            frame["bbox"] is None for frame in reserve_frames
        ),
        "all_used_cells_within_padding": all(
            bounds[0] >= CELL_PADDING
            and bounds[2] <= FRAME_SIZE - CELL_PADDING
            and bounds[1] >= 0
            and bounds[3] <= GROUND_BASELINE
            for bounds in used_bounds
        ),
        "all_used_cells_grounded_at_248": all(
            bounds[3] == GROUND_BASELINE for bounds in used_bounds
        ),
        "green_dominant_pixels": sum(
            int(frame["green_dominant_pixels"]) for frame in used_frames
        ),
        "wide_frame_widths": wide_frame_widths,
        "frames": frames,
    }
    report["passed"] = (
        report["mode"] == "RGBA"
        and report["size"] == [FRAME_SIZE * SHEET_COLUMNS, FRAME_SIZE * SHEET_ROWS]
        and report["nonempty_used_frames"] == USED_ROWS * SHEET_COLUMNS
        and report["transparent_reserve_frames"]
        == (SHEET_ROWS - USED_ROWS) * SHEET_COLUMNS
        and report["all_used_cells_within_padding"]
        and report["all_used_cells_grounded_at_248"]
        and report["green_dominant_pixels"]
        <= args.maximum_green_dominant_pixels
        and all(
            width >= args.minimum_wide_frame_width
            for width in wide_frame_widths.values()
        )
    )

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    summary = {key: value for key, value in report.items() if key != "frames"}
    print(json.dumps(summary, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())