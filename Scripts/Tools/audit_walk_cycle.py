#!/usr/bin/env python3
"""Audit an atlas walk row for repeated or near-duplicate poses."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


NORMALIZED_SIZE = 96
DEFAULT_POSE_DISTANCE = 0.20


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("atlas", type=Path)
    parser.add_argument("--columns", type=int, default=10)
    parser.add_argument("--rows", type=int, default=9)
    parser.add_argument("--walk-row", type=int, default=1)
    parser.add_argument("--start-frame", type=int)
    parser.add_argument("--frame-count", type=int, default=8)
    parser.add_argument("--minimum-distinct-poses", type=int, default=6)
    parser.add_argument("--pose-distance", type=float, default=DEFAULT_POSE_DISTANCE)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--preview", type=Path)
    return parser.parse_args()


def normalized_pose_mask(frame: Image.Image) -> np.ndarray:
    rgba = np.asarray(frame.convert("RGBA"))
    alpha = rgba[..., 3]
    if np.any(alpha < 250):
        foreground = alpha > 32
    else:
        rgb = rgba[..., :3].astype(np.int16)
        corner_size = max(2, min(frame.width, frame.height) // 32)
        corner_pixels = np.concatenate(
            (
                rgb[:corner_size, :corner_size].reshape(-1, 3),
                rgb[:corner_size, -corner_size:].reshape(-1, 3),
                rgb[-corner_size:, :corner_size].reshape(-1, 3),
                rgb[-corner_size:, -corner_size:].reshape(-1, 3),
            ),
            axis=0,
        )
        background = np.median(corner_pixels, axis=0)
        color_distance = np.max(np.abs(rgb - background), axis=2)
        foreground = color_distance > 24

    alpha_mask = Image.fromarray(np.where(foreground, 255, 0).astype(np.uint8), "L")
    bounds = alpha_mask.getbbox()
    if bounds is None:
        return np.zeros((NORMALIZED_SIZE, NORMALIZED_SIZE), dtype=np.bool_)

    figure = alpha_mask.crop(bounds)
    scale = min(
        (NORMALIZED_SIZE - 8) / figure.width,
        (NORMALIZED_SIZE - 8) / figure.height,
    )
    resized = figure.resize(
        (
            max(1, round(figure.width * scale)),
            max(1, round(figure.height * scale)),
        ),
        Image.Resampling.LANCZOS,
    )
    normalized = Image.new("L", (NORMALIZED_SIZE, NORMALIZED_SIZE), 0)
    normalized.paste(
        resized,
        (
            (NORMALIZED_SIZE - resized.width) // 2,
            NORMALIZED_SIZE - resized.height - 4,
        ),
    )
    pose = np.asarray(normalized) > 32
    # Walk quality is determined by hips, legs, and feet. Excluding the upper
    # 30 percent prevents long hair, aura rims, and arm motion from hiding or
    # falsely inflating the actual gait changes.
    pose[: round(NORMALIZED_SIZE * 0.30)] = False
    return pose


def pose_distance(first: np.ndarray, second: np.ndarray) -> float:
    union = np.logical_or(first, second)
    if not np.any(union):
        return 0.0
    difference = np.logical_xor(first, second)
    return float(np.count_nonzero(difference) / np.count_nonzero(union))


def cluster_poses(masks: list[np.ndarray], threshold: float) -> list[list[int]]:
    clusters: list[list[int]] = []
    for frame_index, mask in enumerate(masks):
        for cluster in clusters:
            if min(pose_distance(mask, masks[index]) for index in cluster) < threshold:
                cluster.append(frame_index)
                break
        else:
            clusters.append([frame_index])
    return clusters


def save_preview(frames: list[Image.Image], path: Path) -> None:
    frame_width, frame_height = frames[0].size
    label_height = 28
    preview = Image.new(
        "RGBA",
        (frame_width * len(frames), frame_height + label_height),
        (22, 22, 28, 255),
    )
    draw = ImageDraw.Draw(preview)
    for index, frame in enumerate(frames):
        preview.alpha_composite(frame, (index * frame_width, label_height))
        draw.text((index * frame_width + 8, 7), f"FRAME {index + 1}", fill="white")
    path.parent.mkdir(parents=True, exist_ok=True)
    preview.save(path)


def main() -> int:
    args = parse_args()
    atlas = Image.open(args.atlas).convert("RGBA")
    if atlas.width % args.columns or atlas.height % args.rows:
        raise ValueError(
            f"{args.atlas} is not evenly divisible by {args.columns}x{args.rows}"
        )
    start_frame = (
        args.start_frame
        if args.start_frame is not None
        else args.walk_row * args.columns
    )
    if not 0 <= start_frame < args.columns * args.rows:
        raise ValueError(f"start frame {start_frame} is outside the atlas")
    available_frames = args.columns * args.rows - start_frame
    if not 1 <= args.frame_count <= available_frames:
        raise ValueError("frame count must fit between the walk row and atlas end")

    frame_width = atlas.width // args.columns
    frame_height = atlas.height // args.rows
    frames = []
    for frame_index in range(args.frame_count):
        atlas_frame = start_frame + frame_index
        row = atlas_frame // args.columns
        column = atlas_frame % args.columns
        frames.append(
            atlas.crop(
                (
                    column * frame_width,
                    row * frame_height,
                    (column + 1) * frame_width,
                    (row + 1) * frame_height,
                )
            )
        )
    masks = [normalized_pose_mask(frame) for frame in frames]
    clusters = cluster_poses(masks, args.pose_distance)
    pair_distances = [
        {
            "frames": [first + 1, second + 1],
            "distance": round(pose_distance(masks[first], masks[second]), 4),
        }
        for first in range(len(masks))
        for second in range(first + 1, len(masks))
    ]
    pair_distances.sort(key=lambda item: item["distance"])

    report = {
        "atlas": str(args.atlas).replace("\\", "/"),
        "start_frame": start_frame,
        "frame_count": args.frame_count,
        "minimum_distinct_poses": args.minimum_distinct_poses,
        "pose_distance_threshold": args.pose_distance,
        "distinct_pose_count": len(clusters),
        "pose_clusters": [[index + 1 for index in cluster] for cluster in clusters],
        "nearest_pose_pairs": pair_distances[:8],
        "passed": len(clusters) >= args.minimum_distinct_poses,
    }

    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    if args.preview:
        save_preview(frames, args.preview)

    print(json.dumps(report, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())