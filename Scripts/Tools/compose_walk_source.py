#!/usr/bin/env python3
"""Compose an authored 4x2 walk source from explicit candidate poses."""

from __future__ import annotations

import json
import sys
from pathlib import Path

from PIL import Image

from compose_enemy_sheet import extract_connected_components


COLUMNS = 4
ROWS = 2
OUTPUT_SIZE = 2048
CELL_WIDTH = OUTPUT_SIZE // COLUMNS
CELL_HEIGHT = OUTPUT_SIZE // ROWS
CELL_MARGIN = 12
BASELINE = CELL_HEIGHT - CELL_MARGIN
DEFAULT_TARGET_HEIGHTS = (900, 860, 880, 900, 900, 860, 880, 900)


def row_major(components: list[dict[str, object]], rows: int) -> list[dict[str, object]]:
    ordered = sorted(components, key=lambda item: float(item["center_y"]))
    if rows == 1:
        return sorted(ordered, key=lambda item: float(item["center_x"]))
    row_size = len(ordered) // rows
    result: list[dict[str, object]] = []
    for row in range(rows):
        result.extend(
            sorted(
                ordered[row * row_size : (row + 1) * row_size],
                key=lambda item: float(item["center_x"]),
            )
        )
    return result


def load_pose(spec: dict[str, object], minimum_area: int) -> tuple[Image.Image, dict[str, object]]:
    source = Path(str(spec["source"]))
    background = str(spec.get("background", "green"))
    columns = int(spec.get("columns", 4))
    rows = int(spec.get("rows", 2))
    index = int(spec["index"])
    expected_count = columns * rows
    components = extract_connected_components(source, background, minimum_area)
    if len(components) != expected_count:
        raise ValueError(
            f"{source}: found {len(components)} complete figures, expected {expected_count}"
        )
    components = row_major(components, rows)
    if not 0 <= index < len(components):
        raise ValueError(f"{source}: frame index {index} is outside the source")
    component = components[index]
    return component["image"], {
        "source": str(source).replace("\\", "/"),
        "source_index": index,
        "source_bounds": component["bbox"],
    }


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: compose_walk_source.py <manifest.json>")
        return 2

    manifest_path = Path(sys.argv[1])
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    frame_specs = manifest["frames"]
    if len(frame_specs) != COLUMNS * ROWS:
        raise ValueError(f"manifest must select exactly {COLUMNS * ROWS} frames")
    minimum_area = int(manifest.get("minimum_component_area", 50_000))

    poses: list[Image.Image] = []
    reports: list[dict[str, object]] = []
    for frame_spec in frame_specs:
        pose, report = load_pose(frame_spec, minimum_area)
        poses.append(pose)
        reports.append(report)

    target_heights = tuple(
        int(height)
        for height in manifest.get("target_heights", DEFAULT_TARGET_HEIGHTS)
    )
    if len(target_heights) != COLUMNS * ROWS:
        raise ValueError("target_heights must contain exactly eight values")
    output = Image.new("RGBA", (OUTPUT_SIZE, OUTPUT_SIZE), (0, 255, 0, 255))
    for index, pose in enumerate(poses):
        scale = min(
            target_heights[index] / pose.height,
            (CELL_WIDTH - 2 * CELL_MARGIN) / pose.width,
        )
        pose = pose.resize(
            (
                max(1, round(pose.width * scale)),
                max(1, round(pose.height * scale)),
            ),
            Image.Resampling.LANCZOS,
        )
        column = index % COLUMNS
        row = index // COLUMNS
        x = column * CELL_WIDTH + (CELL_WIDTH - pose.width) // 2
        y = row * CELL_HEIGHT + BASELINE - pose.height
        output.alpha_composite(pose, (x, y))
        reports[index].update(
            {
                "frame": index + 1,
                "target_height": target_heights[index],
                "scale": round(scale, 6),
                "normalized_bounds": [x, y, x + pose.width, y + pose.height],
            }
        )

    output_path = Path(manifest["output"])
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output.convert("RGB").save(output_path)
    report = {
        "manifest": str(manifest_path).replace("\\", "/"),
        "output": str(output_path).replace("\\", "/"),
        "layout": [COLUMNS, ROWS],
        "target_heights": list(target_heights),
        "frames": reports,
    }
    if report_path_value := manifest.get("report"):
        report_path = Path(report_path_value)
        report_path.parent.mkdir(parents=True, exist_ok=True)
        report_path.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"SAVED {output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())