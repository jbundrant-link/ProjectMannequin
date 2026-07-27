#!/usr/bin/env python3
"""Audit separated enemy animation sources before atlas composition."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw

from audit_walk_cycle import (
    cluster_poses,
    normalized_pose_mask,
    pose_distance,
)
from compose_enemy_sheet import extract_connected_components


THUMBNAIL_SIZE = 96
LABEL_WIDTH = 320
ROW_HEIGHT = 112


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("spec", type=Path)
    return parser.parse_args()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def row_major(
    components: list[dict[str, object]],
    row_counts: list[int],
) -> list[dict[str, object]]:
    ordered_by_height = sorted(components, key=lambda item: float(item["center_y"]))
    ordered: list[dict[str, object]] = []
    offset = 0
    for row_count in row_counts:
        row = ordered_by_height[offset : offset + row_count]
        ordered.extend(sorted(row, key=lambda item: float(item["center_x"])))
        offset += row_count
    return ordered


def checker_thumbnail(figure: Image.Image) -> Image.Image:
    checker = Image.new(
        "RGBA",
        (THUMBNAIL_SIZE, THUMBNAIL_SIZE),
        (38, 38, 46, 255),
    )
    draw = ImageDraw.Draw(checker)
    cell = 12
    for y in range(0, THUMBNAIL_SIZE, cell):
        for x in range(0, THUMBNAIL_SIZE, cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle(
                    (x, y, x + cell - 1, y + cell - 1),
                    fill=(52, 52, 62, 255),
                )

    scale = min(
        (THUMBNAIL_SIZE - 8) / figure.width,
        (THUMBNAIL_SIZE - 8) / figure.height,
    )
    resized = figure.resize(
        (
            max(1, round(figure.width * scale)),
            max(1, round(figure.height * scale)),
        ),
        Image.Resampling.LANCZOS,
    )
    checker.alpha_composite(
        resized,
        (
            (THUMBNAIL_SIZE - resized.width) // 2,
            THUMBNAIL_SIZE - resized.height - 4,
        ),
    )
    return checker


def audit_family(
    family: dict[str, object],
    background: str,
    minimum_component_area: int,
    minimum_distinct_poses: int,
    pose_distance_threshold: float,
) -> tuple[dict[str, object], list[Image.Image]]:
    source = Path(str(family["source"]))
    expected_count = int(family["expected_count"])
    row_counts = [int(count) for count in family["row_counts"]]
    if sum(row_counts) != expected_count:
        raise ValueError(
            f"{family['name']}: row counts total {sum(row_counts)}, "
            f"expected {expected_count}"
        )

    base_report: dict[str, object] = {
        "name": str(family["name"]),
        "source": source.as_posix(),
        "expected_component_count": expected_count,
        "row_counts": row_counts,
    }
    if not source.is_file():
        base_report.update(
            {
                "exists": False,
                "component_count": 0,
                "source_edge_clearance": False,
                "passed": False,
            }
        )
        return base_report, []

    with Image.open(source) as image:
        width, height = image.size
        mode = image.mode
    components = extract_connected_components(
        source,
        background,
        minimum_component_area,
    )
    ordered = row_major(components, row_counts) if len(components) == expected_count else []
    source_edge_clearance = all(
        int(component["bbox"][0]) > 0
        and int(component["bbox"][1]) > 0
        and int(component["bbox"][2]) < width
        and int(component["bbox"][3]) < height
        for component in components
    )
    report = {
        **base_report,
        "exists": True,
        "sha256": sha256(source),
        "mode": mode,
        "size": [width, height],
        "component_count": len(components),
        "source_edge_clearance": source_edge_clearance,
        "components": [
            {
                "bbox": [int(value) for value in component["bbox"]],
                "center": [
                    round(float(component["center_x"]), 2),
                    round(float(component["center_y"]), 2),
                ],
                "area": int(component["area"]),
            }
            for component in ordered
        ],
    }
    figures = [component["image"] for component in ordered]

    if bool(family.get("walk", False)) and figures:
        masks = [normalized_pose_mask(figure) for figure in figures]
        clusters = cluster_poses(masks, pose_distance_threshold)
        pair_distances = [
            {
                "frames": [first + 1, second + 1],
                "distance": round(pose_distance(masks[first], masks[second]), 4),
            }
            for first in range(len(masks))
            for second in range(first + 1, len(masks))
        ]
        pair_distances.sort(key=lambda item: float(item["distance"]))
        report.update(
            {
                "minimum_distinct_poses": minimum_distinct_poses,
                "pose_distance_threshold": pose_distance_threshold,
                "distinct_pose_count": len(clusters),
                "pose_clusters": [
                    [index + 1 for index in cluster] for cluster in clusters
                ],
                "nearest_pose_pairs": pair_distances[:8],
            }
        )

    walk_passed = (
        int(report.get("distinct_pose_count", minimum_distinct_poses))
        >= minimum_distinct_poses
    )
    report["passed"] = (
        len(components) == expected_count
        and source_edge_clearance
        and walk_passed
    )
    return report, figures


def write_contact_sheet(
    display_name: str,
    audited: list[tuple[dict[str, object], list[Image.Image]]],
    output: Path,
) -> None:
    maximum_count = max(int(report["expected_component_count"]) for report, _ in audited)
    width = LABEL_WIDTH + maximum_count * THUMBNAIL_SIZE
    height = 34 + len(audited) * ROW_HEIGHT
    sheet = Image.new("RGBA", (width, height), (23, 23, 29, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 10), display_name, fill=(245, 247, 250, 255))

    for row, (report, figures) in enumerate(audited):
        y = 34 + row * ROW_HEIGHT
        passed = bool(report["passed"])
        color = (102, 220, 150, 255) if passed else (244, 112, 112, 255)
        expected = int(report["expected_component_count"])
        draw.text((12, y + 18), str(report["name"]), fill=(240, 240, 244, 255))
        detail = f"{report.get('component_count', 0)}/{expected} figures"
        if "minimum_distinct_poses" in report:
            detail += (
                f", {report.get('distinct_pose_count', 0)}/"
                f"{report['minimum_distinct_poses']} poses"
            )
        draw.text((12, y + 43), detail, fill=color)
        draw.text((12, y + 69), "PASS" if passed else "REPAIR", fill=color)
        for column, figure in enumerate(figures):
            sheet.alpha_composite(
                checker_thumbnail(figure),
                (LABEL_WIDTH + column * THUMBNAIL_SIZE, y + 8),
            )
        draw.line(
            (0, y + ROW_HEIGHT - 1, width, y + ROW_HEIGHT - 1),
            fill=(66, 66, 76, 255),
        )

    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(output)


def main() -> int:
    args = parse_args()
    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    families = spec.get("families", [])
    if not families:
        raise ValueError(f"{args.spec}: families must not be empty")

    background = str(spec.get("background", "green"))
    minimum_component_area = int(spec.get("minimum_component_area", 50_000))
    minimum_distinct_poses = int(spec.get("minimum_distinct_poses", 6))
    pose_distance_threshold = float(spec.get("pose_distance_threshold", 0.20))
    audited = [
        audit_family(
            family,
            background,
            minimum_component_area,
            minimum_distinct_poses,
            pose_distance_threshold,
        )
        for family in families
    ]
    reports = [report for report, _ in audited]
    passed_count = sum(bool(report["passed"]) for report in reports)
    output = {
        "spec": args.spec.as_posix(),
        "enemy_id": str(spec["enemy_id"]),
        "display_name": str(spec["display_name"]),
        "family_count": len(reports),
        "passed_count": passed_count,
        "failed_count": len(reports) - passed_count,
        "families": reports,
    }

    output_report = Path(str(spec["output_report"]))
    output_contact = Path(str(spec["output_contact_sheet"]))
    output_report.parent.mkdir(parents=True, exist_ok=True)
    output_report.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    write_contact_sheet(str(spec["display_name"]), audited, output_contact)

    summary = {
        "family_count": output["family_count"],
        "passed_count": output["passed_count"],
        "failed_count": output["failed_count"],
        "failed": [
            {
                "name": report["name"],
                "exists": report["exists"],
                "components": report["component_count"],
                "expected": report["expected_component_count"],
                "distinct_poses": report.get("distinct_pose_count"),
                "edge_clearance": report["source_edge_clearance"],
            }
            for report in reports
            if not report["passed"]
        ],
    }
    print(json.dumps(summary, indent=2))
    return 0 if output["failed_count"] == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())