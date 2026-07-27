#!/usr/bin/env python3
"""Audit repaired 4x2 enemy walk sources before atlas patching."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw

from audit_active_walk_cycles import AtlasSpec, audit_atlas, checker_thumbnail
from compose_enemy_sheet import extract_connected_components


OUTPUT_REPORT = Path("Artifacts/enemy_walk_repair_source_audit.json")
OUTPUT_CONTACT = Path("Artifacts/enemy_walk_repair_source_contact_sheet.png")
LABEL_WIDTH = 280
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
    source("archive_knight", "Archive Knight", "archive_knight_walk_sheet_v4.png"),
    source("archive_raider", "Archive Raider", "archive_raider_walk_sheet_style_v4.png"),
    source("archive_bruiser", "Archive Bruiser", "archive_bruiser_walk_sheet_style_v4_candidate.png"),
    source("overseer_basalt", "Overseer Basalt", "overseer_basalt_walk_sheet_v3_normalized.png"),
    source("world_warrior_rookie", "Dojo Rookie", "world_warrior_rookie_walk_sheet_style_v3.png"),
    source("world_warrior_striker", "Pavilion Striker", "world_warrior_striker_walk_sheet_style_v2.png"),
    source("world_warrior_grappler", "Tournament Grappler", "world_warrior_grappler_walk_sheet_style_v2_normalized.png"),
    source("astral_saibaman", "Astral Saibaman", "astral_saibaman_walk_sheet_v2.png"),
    source("astral_frieza_heavy", "Astral Frieza Heavy", "astral_frieza_heavy_walk_sheet_v5_candidate.png"),
)


def audit_source(spec: SourceSpec) -> tuple[dict[str, object], list[Image.Image]]:
    report, frames = audit_atlas(AtlasSpec(spec.name, spec.path, 4, 2, 0, 8))
    if not spec.path.exists():
        report.update({"key": spec.key, "component_count": 0, "passed": False})
        return report, frames

    components = extract_connected_components(spec.path, "green", 50_000)
    image = Image.open(spec.path)
    cell_width = image.width // 4
    cell_height = image.height // 2
    bounds = [component["bbox"] for component in components]
    cell_containment = all(
        box[0] >= column * cell_width
        and box[2] <= (column + 1) * cell_width
        and box[1] >= row * cell_height
        and box[3] <= (row + 1) * cell_height
        for row in range(2)
        for column in range(4)
        for box in bounds
        if (
            column * cell_width <= (box[0] + box[2]) * 0.5 < (column + 1) * cell_width
            and row * cell_height <= (box[1] + box[3]) * 0.5 < (row + 1) * cell_height
        )
    )
    source_edge_clearance = all(
        box[0] > 0 and box[1] > 0 and box[2] < image.width and box[3] < image.height
        for box in bounds
    )
    passed = (
        bool(report["passed"])
        and len(components) == 8
        and cell_containment
        and source_edge_clearance
    )
    report.update(
        {
            "key": spec.key,
            "component_count": len(components),
            "expected_component_count": 8,
            "cell_containment": cell_containment,
            "source_edge_clearance": source_edge_clearance,
            "component_bounds": bounds,
            "passed": passed,
        }
    )
    return report, frames


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
            f"{report.get('component_count', 0)}/8 figures, {report.get('distinct_pose_count', 0)}/6 poses",
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