#!/usr/bin/env python3
"""Audit every active runtime character walk cycle and build a contact sheet."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw

from audit_walk_cycle import cluster_poses, normalized_pose_mask, pose_distance


OUTPUT_REPORT = Path("Artifacts/active_walk_cycle_audit.json")
OUTPUT_CONTACT_SHEET = Path("Artifacts/active_walk_cycle_contact_sheet.png")
MINIMUM_DISTINCT_POSES = 6
POSE_DISTANCE_THRESHOLD = 0.20
THUMBNAIL_SIZE = 96
LABEL_WIDTH = 310
ROW_HEIGHT = 116


@dataclass(frozen=True)
class AtlasSpec:
    name: str
    path: Path
    columns: int
    rows: int
    start_frame: int
    frame_count: int


def enemy(name: str, filename: str) -> AtlasSpec:
    return AtlasSpec(
        name,
        Path("Assets/Sprites/Enemies") / filename,
        10,
        9,
        10,
        8,
    )


def goku_variant(name: str, stem: str) -> AtlasSpec:
    return AtlasSpec(
        f"Goku: {name}",
        Path("Assets/Sprites/Goku") / f"goku_astral_{stem}_higgsfield_v2_sheet.png",
        8,
        18,
        9,
        10,
    )


ATLASES = (
    AtlasSpec(
        "Blank Mannequin",
        Path("Assets/Sprites/Mannequin/mannequin_sheet_higgsfield_v1.png"),
        10,
        9,
        10,
        8,
    ),
    enemy("Archive Knight", "archive_knight_style_v2.png"),
    enemy("Archive Scout", "archive_scout_style_v2.png"),
    enemy("Archive Raider", "archive_raider_style_v3.png"),
    enemy("Archive Bruiser", "archive_bruiser_style_v3.png"),
    enemy("Cipher Captain Rhune", "cipher_captain_rhune_style_v1.png"),
    enemy("Index Warden Veyra", "index_warden_veyra_style_v1.png"),
    enemy("Overseer Basalt", "overseer_basalt_style_v2.png"),
    enemy("World Warrior Rookie", "world_warrior_rookie_style_v3.png"),
    enemy("World Warrior Striker", "world_warrior_striker_style_v3.png"),
    enemy("World Warrior Grappler", "world_warrior_grappler_style_v3.png"),
    enemy(
        "Dojo Prodigy Kenzo",
        "world_warrior_dojo_prodigy_kenzo_style_v2.png",
    ),
    enemy("Astral Saibaman", "astral_saibaman_higgsfield_v2.png"),
    enemy("Astral Frieza Scout", "astral_frieza_scout_higgsfield_v1.png"),
    enemy("Astral Frieza Heavy", "astral_frieza_heavy_higgsfield_v2.png"),
    enemy("Astral Ki Captain", "astral_ki_captain_higgsfield_v1.png"),
    AtlasSpec(
        "Ryu",
        Path("Assets/Sprites/Ryu/ryu_higgsfield_v4_sheet.png"),
        16,
        15,
        16,
        11,
    ),
    AtlasSpec(
        "Goku: base",
        Path("Assets/Sprites/Goku/goku_astral_higgsfield_v2_sheet.png"),
        8,
        18,
        9,
        10,
    ),
    goku_variant("Kaioken", "kaioken"),
    goku_variant("False Super", "false_super"),
    goku_variant("Super Saiyan", "ss1"),
    goku_variant("Super Saiyan 2", "ss2"),
    goku_variant("Super Saiyan 3", "ss3"),
    goku_variant("Super Saiyan 4", "ss4"),
    goku_variant("God", "god"),
    goku_variant("Blue", "blue"),
    goku_variant("Blue Kaioken", "blue_kaioken"),
    goku_variant("UI Sign", "ui_sign"),
    goku_variant("Instinct", "instinct"),
)


def extract_frames(image: Image.Image, spec: AtlasSpec) -> list[Image.Image]:
    if image.width % spec.columns or image.height % spec.rows:
        raise ValueError(
            f"{spec.path} is not divisible by {spec.columns}x{spec.rows}"
        )
    if spec.start_frame + spec.frame_count > spec.columns * spec.rows:
        raise ValueError(f"{spec.name} walk range exceeds its atlas")

    frame_width = image.width // spec.columns
    frame_height = image.height // spec.rows
    frames = []
    for offset in range(spec.frame_count):
        atlas_frame = spec.start_frame + offset
        row = atlas_frame // spec.columns
        column = atlas_frame % spec.columns
        frames.append(
            image.crop(
                (
                    column * frame_width,
                    row * frame_height,
                    (column + 1) * frame_width,
                    (row + 1) * frame_height,
                )
            )
        )
    return frames


def audit_atlas(spec: AtlasSpec) -> tuple[dict[str, object], list[Image.Image]]:
    if not spec.path.exists():
        return (
            {
                "name": spec.name,
                "path": str(spec.path).replace("\\", "/"),
                "passed": False,
                "error": "missing atlas",
            },
            [],
        )

    with Image.open(spec.path) as source:
        image = source.convert("RGBA")
    try:
        frames = extract_frames(image, spec)
    except ValueError as error:
        return (
            {
                "name": spec.name,
                "path": str(spec.path).replace("\\", "/"),
                "passed": False,
                "error": str(error),
            },
            [],
        )

    nonempty = [frame.getchannel("A").getbbox() is not None for frame in frames]
    masks = [normalized_pose_mask(frame) for frame in frames]
    clusters = cluster_poses(masks, POSE_DISTANCE_THRESHOLD)
    nearest_pairs = sorted(
        (
            {
                "frames": [first + 1, second + 1],
                "distance": round(pose_distance(masks[first], masks[second]), 4),
            }
            for first in range(len(masks))
            for second in range(first + 1, len(masks))
        ),
        key=lambda item: item["distance"],
    )[:5]
    passed = all(nonempty) and len(clusters) >= MINIMUM_DISTINCT_POSES
    return (
        {
            "name": spec.name,
            "path": str(spec.path).replace("\\", "/"),
            "layout": [spec.columns, spec.rows],
            "start_frame": spec.start_frame,
            "frame_count": spec.frame_count,
            "nonempty_frame_count": sum(nonempty),
            "minimum_distinct_poses": MINIMUM_DISTINCT_POSES,
            "distinct_pose_count": len(clusters),
            "pose_clusters": [[index + 1 for index in cluster] for cluster in clusters],
            "nearest_pose_pairs": nearest_pairs,
            "passed": passed,
        },
        frames,
    )


def checker_thumbnail(frame: Image.Image) -> Image.Image:
    canvas = Image.new("RGBA", (THUMBNAIL_SIZE, THUMBNAIL_SIZE), (38, 38, 46, 255))
    draw = ImageDraw.Draw(canvas)
    checker = 12
    for y in range(0, THUMBNAIL_SIZE, checker):
        for x in range(0, THUMBNAIL_SIZE, checker):
            if (x // checker + y // checker) % 2:
                draw.rectangle(
                    (x, y, x + checker, y + checker),
                    fill=(54, 54, 64, 255),
                )
    fitted = frame.copy()
    fitted.thumbnail((THUMBNAIL_SIZE, THUMBNAIL_SIZE), Image.Resampling.LANCZOS)
    canvas.alpha_composite(
        fitted,
        (
            (THUMBNAIL_SIZE - fitted.width) // 2,
            (THUMBNAIL_SIZE - fitted.height) // 2,
        ),
    )
    return canvas


def write_contact_sheet(
    audited: list[tuple[dict[str, object], list[Image.Image]]],
) -> None:
    maximum_frames = max(spec.frame_count for spec in ATLASES)
    width = LABEL_WIDTH + maximum_frames * THUMBNAIL_SIZE
    height = len(audited) * ROW_HEIGHT
    sheet = Image.new("RGBA", (width, height), (23, 23, 29, 255))
    draw = ImageDraw.Draw(sheet)
    for row, (report, frames) in enumerate(audited):
        y = row * ROW_HEIGHT
        passed = bool(report["passed"])
        color = (102, 220, 150, 255) if passed else (244, 112, 112, 255)
        detail = (
            f"{report.get('distinct_pose_count', 0)}/"
            f"{report.get('minimum_distinct_poses', MINIMUM_DISTINCT_POSES)} distinct"
        )
        draw.text((12, y + 27), str(report["name"]), fill=(240, 240, 244, 255))
        draw.text((12, y + 52), detail, fill=color)
        draw.text((12, y + 75), "PASS" if passed else "REPAIR", fill=color)
        for column, frame in enumerate(frames):
            sheet.alpha_composite(
                checker_thumbnail(frame),
                (LABEL_WIDTH + column * THUMBNAIL_SIZE, y + 10),
            )
        draw.line((0, y + ROW_HEIGHT - 1, width, y + ROW_HEIGHT - 1), fill=(66, 66, 76, 255))

    OUTPUT_CONTACT_SHEET.parent.mkdir(parents=True, exist_ok=True)
    sheet.convert("RGB").save(OUTPUT_CONTACT_SHEET)


def main() -> int:
    audited = [audit_atlas(spec) for spec in ATLASES]
    reports = [report for report, _ in audited]
    passed_count = sum(bool(report["passed"]) for report in reports)
    output = {
        "minimum_distinct_poses": MINIMUM_DISTINCT_POSES,
        "pose_distance_threshold": POSE_DISTANCE_THRESHOLD,
        "atlas_count": len(reports),
        "passed_count": passed_count,
        "failed_count": len(reports) - passed_count,
        "atlases": reports,
    }
    OUTPUT_REPORT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_REPORT.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    write_contact_sheet(audited)

    print(
        json.dumps(
            {
                "atlas_count": output["atlas_count"],
                "passed_count": output["passed_count"],
                "failed_count": output["failed_count"],
                "failed": [
                    {
                        "name": report["name"],
                        "distinct_pose_count": report.get("distinct_pose_count", 0),
                        "pose_clusters": report.get("pose_clusters", []),
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