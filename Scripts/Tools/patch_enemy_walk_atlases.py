#!/usr/bin/env python3
"""Create versioned enemy atlases by replacing only walk row 1."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

from compose_enemy_sheet import extract_connected_components, normalize_component_frames


FRAME = 256
COLUMNS = 10
ROWS = 9
WALK_ROW = 1
SOURCE_ROOT = Path("Assets/Sprites/Concepts/StyleCalibration")
ENEMY_ROOT = Path("Assets/Sprites/Enemies")
REPORT = Path("Artifacts/enemy_walk_atlas_patch_report.json")


def patch(base_name: str, source_name: str, output_name: str) -> tuple[Path, Path, Path]:
    return ENEMY_ROOT / base_name, SOURCE_ROOT / source_name, ENEMY_ROOT / output_name


PATCHES = (
    patch("archive_knight_style_v1.png", "archive_knight_walk_sheet_v4.png", "archive_knight_style_v2.png"),
    patch("archive_raider_style_v2.png", "archive_raider_walk_sheet_style_v4.png", "archive_raider_style_v3.png"),
    patch("archive_bruiser_style_v2.png", "archive_bruiser_walk_sheet_style_v4_candidate.png", "archive_bruiser_style_v3.png"),
    patch("overseer_basalt_style_v1.png", "overseer_basalt_walk_sheet_v3_normalized.png", "overseer_basalt_style_v2.png"),
    patch("world_warrior_rookie_style_v2.png", "world_warrior_rookie_walk_sheet_style_v3.png", "world_warrior_rookie_style_v3.png"),
    patch("world_warrior_striker_style_v2.png", "world_warrior_striker_walk_sheet_style_v2.png", "world_warrior_striker_style_v3.png"),
    patch("world_warrior_grappler_style_v2.png", "world_warrior_grappler_walk_sheet_style_v2_normalized.png", "world_warrior_grappler_style_v3.png"),
    patch("astral_saibaman_higgsfield_v1.png", "astral_saibaman_walk_sheet_v2.png", "astral_saibaman_higgsfield_v2.png"),
    patch("astral_frieza_heavy_higgsfield_v1.png", "astral_frieza_heavy_walk_sheet_v5_candidate.png", "astral_frieza_heavy_higgsfield_v2.png"),
)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def row_major(components: list[dict[str, object]]) -> list[dict[str, object]]:
    ordered = sorted(components, key=lambda item: float(item["center_y"]))
    top = sorted(ordered[:4], key=lambda item: float(item["center_x"]))
    bottom = sorted(ordered[4:], key=lambda item: float(item["center_x"]))
    return top + bottom


def patch_one(base_path: Path, source_path: Path, output_path: Path) -> dict[str, object]:
    missing = [path for path in (base_path, source_path) if not path.exists()]
    if missing:
        raise FileNotFoundError("Missing enemy walk patch input: " + ", ".join(map(str, missing)))

    base = Image.open(base_path).convert("RGBA")
    if base.size != (FRAME * COLUMNS, FRAME * ROWS):
        raise ValueError(f"{base_path}: expected 2560x2304 atlas, found {base.size}")

    components = extract_connected_components(source_path, "green", 50_000)
    if len(components) != 8:
        raise ValueError(f"{source_path}: found {len(components)} complete figures, expected 8")
    components = row_major(components)
    frames, scale = normalize_component_frames(components)

    output = base.copy()
    transparent_row = Image.new("RGBA", (FRAME * COLUMNS, FRAME), (0, 0, 0, 0))
    output.paste(transparent_row, (0, WALK_ROW * FRAME))
    for column in range(COLUMNS):
        output.alpha_composite(
            frames[min(column, len(frames) - 1)],
            (column * FRAME, WALK_ROW * FRAME),
        )

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output.save(output_path)
    base_pixels = np.asarray(base)
    output_pixels = np.asarray(output)
    changed_rows = [
        row
        for row in range(ROWS)
        if not np.array_equal(
            base_pixels[row * FRAME : (row + 1) * FRAME],
            output_pixels[row * FRAME : (row + 1) * FRAME],
        )
    ]
    return {
        "base": str(base_path).replace("\\", "/"),
        "walk_source": str(source_path).replace("\\", "/"),
        "output": str(output_path).replace("\\", "/"),
        "base_sha256": sha256(base_path),
        "output_sha256": sha256(output_path),
        "walk_scale": round(scale, 6),
        "changed_rows": changed_rows,
        "unchanged_rows": [row for row in range(ROWS) if row not in changed_rows],
        "passed": changed_rows == [WALK_ROW],
    }


def main() -> int:
    reports = [patch_one(*item) for item in PATCHES]
    output = {
        "atlas_count": len(reports),
        "passed_count": sum(bool(report["passed"]) for report in reports),
        "failed_count": sum(not bool(report["passed"]) for report in reports),
        "atlases": reports,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
    print(
        json.dumps(
            {
                "atlas_count": output["atlas_count"],
                "passed_count": output["passed_count"],
                "failed_count": output["failed_count"],
                "unchanged_rows_per_atlas": sorted(
                    {tuple(report["unchanged_rows"]) for report in reports}
                ),
            },
            indent=2,
        )
    )
    return 0 if output["failed_count"] == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())