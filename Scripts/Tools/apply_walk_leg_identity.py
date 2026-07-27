#!/usr/bin/env python3
"""Reinforce persistent far-leg identity across an eight-frame walk row."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image


B_LEG_SIDES = ("left", "left", "right", "right", "right", "right", "left", "left")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--columns", type=int, default=10)
    parser.add_argument("--rows", type=int, default=9)
    parser.add_argument("--start-frame", type=int, default=10)
    parser.add_argument("--frame-count", type=int, default=8)
    parser.add_argument("--shadow-strength", type=float, default=0.24)
    parser.add_argument("--report", type=Path)
    return parser.parse_args()


def reinforce_frame(frame: Image.Image, side: str, shadow_strength: float) -> tuple[Image.Image, int]:
    rgba = np.asarray(frame.convert("RGBA")).copy()
    alpha = rgba[..., 3]
    y_values, x_values = np.nonzero(alpha > 16)
    if len(x_values) == 0:
        return frame.copy(), 0

    left = int(x_values.min())
    right = int(x_values.max()) + 1
    top = int(y_values.min())
    bottom = int(y_values.max()) + 1
    center_x = (left + right) * 0.5
    lower_body_y = round(top + (bottom - top) * 0.43)

    rgb = rgba[..., :3].astype(np.int16)
    maximum = rgb.max(axis=2)
    minimum = rgb.min(axis=2)
    chroma = maximum - minimum
    y_grid, x_grid = np.indices(alpha.shape)

    # Restrict the pass to neutral dark cloth below the waist. This preserves
    # skin, ivory shoes, sash colors, and upper-body identity details.
    cloth = (
        (alpha > 32)
        & (y_grid >= lower_body_y)
        & (maximum >= 24)
        & (maximum <= 170)
        & (chroma <= 58)
    )
    if side == "left":
        side_mask = x_grid <= center_x
    else:
        side_mask = x_grid > center_x
    mask = cloth & side_mask

    multiplier = 1.0 - shadow_strength
    shaded = np.rint(rgba[..., :3][mask].astype(np.float32) * multiplier).astype(np.uint8)
    # Keep a restrained cool-indigo bias consistent with the existing cloth.
    shaded[:, 2] = np.minimum(255, np.rint(shaded[:, 2] * 1.06)).astype(np.uint8)
    rgba[..., :3][mask] = shaded
    return Image.fromarray(rgba, "RGBA"), int(np.count_nonzero(mask))


def main() -> int:
    args = parse_args()
    if args.frame_count != len(B_LEG_SIDES):
        raise ValueError(f"frame count must be {len(B_LEG_SIDES)}")
    if not 0.0 <= args.shadow_strength <= 0.5:
        raise ValueError("shadow strength must be between 0 and 0.5")

    atlas = Image.open(args.source).convert("RGBA")
    if atlas.width % args.columns or atlas.height % args.rows:
        raise ValueError(f"{args.source} is not divisible by {args.columns}x{args.rows}")
    if args.start_frame < 0 or args.start_frame + args.frame_count > args.columns * args.rows:
        raise ValueError("walk frame range exceeds the atlas")

    frame_width = atlas.width // args.columns
    frame_height = atlas.height // args.rows
    output = atlas.copy()
    frames = []
    for offset, side in enumerate(B_LEG_SIDES):
        frame_index = args.start_frame + offset
        column = frame_index % args.columns
        row = frame_index // args.columns
        box = (
            column * frame_width,
            row * frame_height,
            (column + 1) * frame_width,
            (row + 1) * frame_height,
        )
        frame = atlas.crop(box)
        reinforced, changed_pixels = reinforce_frame(frame, side, args.shadow_strength)
        output.paste(reinforced, box)
        frames.append(
            {
                "frame": offset + 1,
                "atlas_frame": frame_index,
                "b_leg_side": side,
                "changed_pixels": changed_pixels,
            }
        )

    args.output.parent.mkdir(parents=True, exist_ok=True)
    output.save(args.output)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(
            json.dumps(
                {
                    "source": str(args.source).replace("\\", "/"),
                    "output": str(args.output).replace("\\", "/"),
                    "start_frame": args.start_frame,
                    "shadow_strength": args.shadow_strength,
                    "frames": frames,
                },
                indent=2,
            )
            + "\n",
            encoding="utf-8",
        )
    print(f"SAVED {args.output}")
    print(f"CHANGED {sum(frame['changed_pixels'] for frame in frames)} walk-row pixels")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())