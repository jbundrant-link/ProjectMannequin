#!/usr/bin/env python3
"""Create versioned Goku movement atlases by replacing only walk frames 9-18."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

import process_goku_higgsfield_sprites as goku


GOKU_ROOT = Path("Assets/Sprites/Goku")
SOURCE_ROOT = Path("Assets/Sprites/Concepts/StyleCalibration")
REPORT = Path("Artifacts/goku_walk_atlas_v2_patch_report.json")


def atlas(stem: str, source_name: str) -> tuple[Path, Path, Path]:
    if stem == "base":
        base = GOKU_ROOT / "goku_astral_higgsfield_v1_sheet.png"
        output = GOKU_ROOT / "goku_astral_higgsfield_v2_sheet.png"
    else:
        base = GOKU_ROOT / f"goku_astral_{stem}_higgsfield_v1_sheet.png"
        output = GOKU_ROOT / f"goku_astral_{stem}_higgsfield_v2_sheet.png"
    return base, SOURCE_ROOT / source_name, output


PATCHES = (
    atlas("base", "goku_base_walk_sheet_pilot_v2.png"),
    atlas("kaioken", "goku_kaioken_walk_sheet_pilot_v1.png"),
    atlas("false_super", "goku_false_super_walk_sheet_v1.png"),
    atlas("ss1", "goku_ss1_walk_sheet_v1.png"),
    atlas("ss2", "goku_ss2_walk_sheet_v1.png"),
    atlas("ss3", "goku_ss3_walk_sheet_v2_normalized.png"),
    atlas("ss4", "goku_ss4_walk_sheet_v1.png"),
    atlas("god", "goku_god_walk_sheet_v1.png"),
    atlas("blue", "goku_blue_walk_sheet_v1.png"),
    atlas("blue_kaioken", "goku_blue_kaioken_walk_sheet_v2_normalized.png"),
    atlas("ui_sign", "goku_ui_sign_walk_sheet_v1.png"),
    atlas("instinct", "goku_instinct_walk_sheet_v1.png"),
)


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def frame(image: np.ndarray, frame_index: int) -> np.ndarray:
    column = frame_index % goku.COLUMNS
    row = frame_index // goku.COLUMNS
    return image[
        row * goku.CELL_HEIGHT : (row + 1) * goku.CELL_HEIGHT,
        column * goku.CELL_WIDTH : (column + 1) * goku.CELL_WIDTH,
    ]


def patch_one(base_path: Path, walk_path: Path, output_path: Path) -> dict[str, object]:
    missing = [path for path in (base_path, walk_path) if not path.exists()]
    if missing:
        raise FileNotFoundError("Missing Goku patch input: " + ", ".join(map(str, missing)))

    base = Image.open(base_path).convert("RGBA")
    output = base.copy()
    goku.patch_walk_frames(output, walk_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output.save(output_path)

    base_pixels = np.asarray(base)
    output_pixels = np.asarray(output)
    changed_frames = [
        frame_index
        for frame_index in range(goku.COLUMNS * 18)
        if not np.array_equal(
            frame(base_pixels, frame_index),
            frame(output_pixels, frame_index),
        )
    ]
    requested_frames = list(
        range(goku.WALK_RUNTIME_START, goku.WALK_RUNTIME_START + len(goku.WALK_SOURCE_SEQUENCE))
    )
    unchanged_frames = [
        frame_index
        for frame_index in range(goku.COLUMNS * 18)
        if frame_index not in changed_frames
    ]
    passed = changed_frames == requested_frames
    return {
        "base": str(base_path).replace("\\", "/"),
        "walk_source": str(walk_path).replace("\\", "/"),
        "output": str(output_path).replace("\\", "/"),
        "base_sha256": sha256(base_path),
        "output_sha256": sha256(output_path),
        "requested_frames": requested_frames,
        "changed_frames": changed_frames,
        "unchanged_frame_count": len(unchanged_frames),
        "passed": passed,
    }


def main() -> int:
    reports = [patch_one(*patch) for patch in PATCHES]
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
                "unchanged_frames_per_atlas": sorted(
                    {report["unchanged_frame_count"] for report in reports}
                ),
            },
            indent=2,
        )
    )
    return 0 if output["failed_count"] == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())