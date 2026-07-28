#!/usr/bin/env python3
"""Measure the TRUE rendered world height of a Sprite3D asset.

Godot's ``Sprite3D.PixelSize`` scales the FULL per-frame texture (texture
size divided by Hframes/Vframes), not just the visible alpha content within
that frame. A prop or pickup's calibrated ``target_world_height`` therefore
has to be validated against how tall the character it stands next to
*actually renders on screen* -- not against the character's gameplay hurtbox
(which is intentionally smaller than the sprite for fairness/feel) and not
against a flat 2D proxy composited at an assumed reference scale.

This script computes true world height the same way for both characters and
props:

    true_world_height = alpha_content_bbox_height_px * pixel_size

For a single-frame prop/pickup (Hframes = Vframes = 1) that is just the
alpha bounding box height of the whole image. For a character sheet frame,
pass --hframes/--vframes/--frame-row/--frame-col to inspect one representative
frame (an idle pose is the best reference -- avoid action poses with
outstretched limbs, which inflate the bounding box).

Usage:
    python measure_true_sprite_world_height.py <image> --pixel-size 0.018 \
        --hframes 10 --vframes 9 --frame-row 0 --frame-col 0

    python measure_true_sprite_world_height.py <prop.png> --pixel-size 0.00132653

Prints the measured true world height and, when --reference-height is given,
the ratio against that reference (use the canonical mannequin/Ryu/Goku true
height as the reference when calibrating a new prop or enemy).
"""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image


def measure(
    path: Path,
    pixel_size: float,
    hframes: int,
    vframes: int,
    frame_row: int,
    frame_col: int,
    alpha_threshold: int,
) -> tuple[int, int, float]:
    image = Image.open(path).convert("RGBA")
    width, height = image.size
    frame_w, frame_h = width // hframes, height // vframes
    arr = np.array(image)
    top = frame_row * frame_h
    left = frame_col * frame_w
    crop = arr[top : top + frame_h, left : left + frame_w]
    alpha = crop[:, :, 3]
    ys, xs = np.where(alpha > alpha_threshold)
    if len(ys) == 0:
        raise SystemExit(f"Frame ({frame_row},{frame_col}) of {path} has no visible pixels.")
    bbox_h = int(ys.max() - ys.min() + 1)
    bbox_w = int(xs.max() - xs.min() + 1)
    true_world_height = bbox_h * pixel_size
    return bbox_h, bbox_w, true_world_height


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", type=Path)
    parser.add_argument("--pixel-size", type=float, required=True)
    parser.add_argument("--hframes", type=int, default=1)
    parser.add_argument("--vframes", type=int, default=1)
    parser.add_argument("--frame-row", type=int, default=0)
    parser.add_argument("--frame-col", type=int, default=0)
    parser.add_argument("--alpha-threshold", type=int, default=10)
    parser.add_argument(
        "--reference-height",
        type=float,
        default=None,
        help="A previously measured true world height (e.g. the canonical "
        "mannequin) to report this asset's ratio against.",
    )
    args = parser.parse_args()

    bbox_h, bbox_w, true_height = measure(
        args.image,
        args.pixel_size,
        args.hframes,
        args.vframes,
        args.frame_row,
        args.frame_col,
        args.alpha_threshold,
    )

    print(f"image: {args.image}")
    print(f"frame alpha bbox: {bbox_w}x{bbox_h}px")
    print(f"true rendered world height: {true_height:.4f} units")
    if args.reference_height:
        ratio = true_height / args.reference_height
        print(
            f"ratio vs reference ({args.reference_height:.4f}): "
            f"{ratio:.3f} ({ratio * 100:.1f}%)"
        )


if __name__ == "__main__":
    main()
