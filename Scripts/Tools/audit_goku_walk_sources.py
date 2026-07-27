#!/usr/bin/env python3
"""Audit all generated Goku walk sources before runtime packing."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw

from audit_active_walk_cycles import AtlasSpec, audit_atlas, checker_thumbnail
from compose_enemy_sheet import extract_connected_components


OUTPUT_REPORT = Path("Artifacts/goku_walk_source_audit.json")
OUTPUT_CONTACT = Path("Artifacts/goku_walk_source_contact_sheet.png")
LABEL_WIDTH = 260
THUMBNAIL_SIZE = 96
ROW_HEIGHT = 112


@dataclass(frozen=True)
class SourceSpec:
    key: str
    name: str
    path: Path


def source(key: str, name: str, filename: str) -> SourceSpec:
    return SourceSpec(
        key,
        name,
        Path("Assets/Sprites/Concepts/StyleCalibration") / filename,
    )


SOURCES = (
    source("base", "Base", "goku_base_walk_sheet_pilot_v2.png"),
    source("kaioken", "Kaioken", "goku_kaioken_walk_sheet_pilot_v1.png"),
    source("false_super", "False Super", "goku_false_super_walk_sheet_v1.png"),
    source("ss1", "Super Saiyan", "goku_ss1_walk_sheet_v1.png"),
    source("ss2", "Super Saiyan 2", "goku_ss2_walk_sheet_v1.png"),
    source("ss3", "Super Saiyan 3", "goku_ss3_walk_sheet_v2_normalized.png"),
    source("ss4", "Super Saiyan 4", "goku_ss4_walk_sheet_v1.png"),
    source("god", "God", "goku_god_walk_sheet_v1.png"),
    source("blue", "Blue", "goku_blue_walk_sheet_v1.png"),
    source("blue_kaioken", "Blue Kaioken", "goku_blue_kaioken_walk_sheet_v2_normalized.png"),
    source("ui_sign", "UI Sign", "goku_ui_sign_walk_sheet_v1.png"),
    source("instinct", "Instinct", "goku_instinct_walk_sheet_v1.png"),
)


def audit_source(spec: SourceSpec) -> tuple[dict[str, object], list[Image.Image]]:
    atlas_spec = AtlasSpec(spec.name, spec.path, 4, 2, 0, 8)
    pose_report, frames = audit_atlas(atlas_spec)
    if not spec.path.exists():
        pose_report.update({"key": spec.key, "component_count": 0, "passed": False})
        return pose_report, frames

    components = extract_connected_components(spec.path, "green", 50_000)
    component_count = len(components)
    figure_bounds = [component["bbox"] for component in components]
    source_image = Image.open(spec.path)
    expected_cell_width = source_image.width // 4
    expected_cell_height = source_image.height // 2
    cell_containment = all(
        bounds[0] >= column * expected_cell_width
        and bounds[2] <= (column + 1) * expected_cell_width
        and bounds[1] >= row * expected_cell_height
        and bounds[3] <= (row + 1) * expected_cell_height
        for row in range(2)
        for column in range(4)
        for bounds in figure_bounds
        if (
            column * expected_cell_width
            <= (bounds[0] + bounds[2]) * 0.5
            < (column + 1) * expected_cell_width
            and row * expected_cell_height
            <= (bounds[1] + bounds[3]) * 0.5
            < (row + 1) * expected_cell_height
        )
    )
    source_edge_clearance = all(
        bounds[0] > 0
        and bounds[1] > 0
        and bounds[2] < source_image.width
        and bounds[3] < source_image.height
        for bounds in figure_bounds
    )
    passed = (
        bool(pose_report["passed"])
        and component_count == 8
        and cell_containment
        and source_edge_clearance
    )
    pose_report.update(
        {
            "key": spec.key,
            "component_count": component_count,
            "expected_component_count": 8,
            "cell_containment": cell_containment,
            "source_edge_clearance": source_edge_clearance,
            "component_bounds": figure_bounds,
            "passed": passed,
        }
    )
    return pose_report, frames


def write_contact_sheet(
    audited: list[tuple[dict[str, object], list[Image.Image]]],
) -> None:
    width = LABEL_WIDTH + 8 * THUMBNAIL_SIZE
    height = len(audited) * ROW_HEIGHT
    sheet = Image.new("RGBA", (width, height), (23, 23, 29, 255))
    draw = ImageDraw.Draw(sheet)
    for row, (report, frames) in enumerate(audited):
        y = row * ROW_HEIGHT
        passed = bool(report["passed"])
        color = (102, 220, 150, 255) if passed else (244, 112, 112, 255)
        draw.text((12, y + 24), str(report["name"]), fill=(240, 240, 244, 255))
        draw.text(
            (12, y + 48),
            (
                f"{report.get('component_count', 0)}/8 figures, "
                f"{report.get('distinct_pose_count', 0)}/6 poses"
            ),
            fill=color,
        )
        draw.text((12, y + 72), "PASS" if passed else "REPAIR", fill=color)
        for column, frame in enumerate(frames):
            sheet.alpha_composite(
                checker_thumbnail(frame),
                (LABEL_WIDTH + column * THUMBNAIL_SIZE, y + 8),
            )
        draw.line((0, y + ROW_HEIGHT - 1, width, y + ROW_HEIGHT - 1), fill=(66, 66, 76, 255))
    OUTPUT_CONTACT.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(OUTPUT_CONTACT)


def main() -> int:
    audited = [audit_source(spec) for spec in SOURCES]
    reports = [report for report, _ in audited]
    passed_count = sum(bool(report["passed"]) for report in reports)
    output = {
        "source_count": len(reports),
        "passed_count": passed_count,
        "failed_count": len(reports) - passed_count,
        "sources": reports,
    }
    OUTPUT_REPORT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_REPORT.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    write_contact_sheet(audited)
    print(
        json.dumps(
            {
                "source_count": output["source_count"],
                "passed_count": output["passed_count"],
                "failed_count": output["failed_count"],
                "failed": [
                    {
                        "key": report["key"],
                        "figures": report.get("component_count", 0),
                        "poses": report.get("distinct_pose_count", 0),
                        "cell_containment": report.get("cell_containment", False),
                        "source_edge_clearance": report.get("source_edge_clearance", False),
                    }
                    for report in reports
                    if not report["passed"]
                ],
            },
            indent=2,
        )
    )
    return 0 if output["failed_count"] == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())