#!/usr/bin/env python3
"""Compose the corrected alternating-leg mannequin walk source."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image


ROOT = Path("Assets/Sprites")
CONCEPT_ROOT = ROOT / "Concepts" / "StyleCalibration"
MANNEQUIN_ROOT = ROOT / "Mannequin"
OUTPUT = MANNEQUIN_ROOT / "higgsfield_walk_sheet_v2.png"
PREVIEW = Path("Artifacts/mannequin_walk_sheet_v2_preview.png")
REPORT = Path("Artifacts/mannequin_walk_sheet_v2_composition.json")
SOURCE_SIZE = 2048
COLUMNS = 4
ROWS = 2
CELL_WIDTH = SOURCE_SIZE // COLUMNS
CELL_HEIGHT = SOURCE_SIZE // ROWS
CELL_PADDING = 20
BASELINE = CELL_HEIGHT - 24
TARGET_HEIGHTS = (900, 860, 880, 900)


@dataclass(frozen=True)
class FrameSource:
    path: Path
    columns: int
    rows: int
    index: int
    phase: str
    leading_leg: str


FRAMES = (
    FrameSource(
        CONCEPT_ROOT / "mannequin_walk_sheet_v2.png",
        4,
        2,
        0,
        "light_contact",
        "light",
    ),
    FrameSource(
        CONCEPT_ROOT / "mannequin_walk_sheet_v2.png",
        4,
        2,
        1,
        "light_down",
        "light",
    ),
    FrameSource(
        CONCEPT_ROOT / "mannequin_walk_sheet_v2.png",
        4,
        2,
        2,
        "light_passing",
        "light",
    ),
    FrameSource(
        CONCEPT_ROOT / "mannequin_walk_sheet_v2.png",
        4,
        2,
        3,
        "light_up",
        "light",
    ),
    FrameSource(
        CONCEPT_ROOT / "mannequin_walk_b_contact_v2.png",
        1,
        1,
        0,
        "dark_contact",
        "dark",
    ),
    FrameSource(
        CONCEPT_ROOT / "mannequin_walk_b_down_v2.png",
        1,
        1,
        0,
        "dark_down",
        "dark",
    ),
    FrameSource(
        CONCEPT_ROOT / "mannequin_walk_opposite_sheet_v2.png",
        2,
        2,
        2,
        "dark_passing",
        "dark",
    ),
    FrameSource(
        CONCEPT_ROOT / "mannequin_walk_opposite_sheet_v2.png",
        2,
        2,
        3,
        "dark_up",
        "dark",
    ),
)


def crop_source_cell(source: Image.Image, frame: FrameSource) -> Image.Image:
    column = frame.index % frame.columns
    row = frame.index // frame.columns
    left = round(column * source.width / frame.columns)
    right = round((column + 1) * source.width / frame.columns)
    top = round(row * source.height / frame.rows)
    bottom = round((row + 1) * source.height / frame.rows)
    return source.crop((left, top, right, bottom))


def key_green(source: Image.Image) -> Image.Image:
    rgb = np.asarray(source.convert("RGB"), dtype=np.int16)
    red = rgb[..., 0]
    green = rgb[..., 1]
    blue = rgb[..., 2]
    dominance = green - np.maximum(red, blue)
    background_strength = np.minimum(green - 50, dominance - 12)
    alpha = np.rint(
        255.0 * (1.0 - np.clip(background_strength, 0, 56) / 56.0)
    ).astype(np.uint8)

    output = np.dstack((rgb.astype(np.uint8), alpha))
    spill = (alpha > 0) & (alpha < 255)
    output[..., 1][spill] = np.minimum(
        output[..., 1][spill],
        np.maximum(output[..., 0][spill], output[..., 2][spill]),
    )
    return Image.fromarray(output, "RGBA")


def extract_figure(frame: FrameSource) -> tuple[Image.Image, list[int]]:
    with Image.open(frame.path) as source:
        cell = crop_source_cell(source.convert("RGBA"), frame)
    keyed = key_green(cell)
    bounds = keyed.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"{frame.path}: {frame.phase} contains no figure")
    figure = keyed.crop(bounds)
    if figure.width < 100 or figure.height < 200:
        raise ValueError(
            f"{frame.path}: {frame.phase} figure is unexpectedly small "
            f"({figure.width}x{figure.height})"
        )
    return figure, list(bounds)


def normalize_figure(figure: Image.Image, phase_index: int) -> tuple[Image.Image, float]:
    target_height = TARGET_HEIGHTS[phase_index % 4]
    scale = min(
        target_height / figure.height,
        (CELL_WIDTH - 2 * CELL_PADDING) / figure.width,
    )
    resized = figure.resize(
        (
            max(1, round(figure.width * scale)),
            max(1, round(figure.height * scale)),
        ),
        Image.Resampling.LANCZOS,
    )
    cell = Image.new("RGBA", (CELL_WIDTH, CELL_HEIGHT), (0, 0, 0, 0))
    x = (CELL_WIDTH - resized.width) // 2
    y = BASELINE - resized.height
    cell.alpha_composite(resized, (x, y))
    return cell, scale


def save_preview(sheet: Image.Image) -> None:
    green = Image.new("RGBA", sheet.size, (0, 255, 0, 255))
    green.alpha_composite(sheet)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    green.convert("RGB").save(PREVIEW)


def main() -> int:
    missing = sorted({str(frame.path) for frame in FRAMES if not frame.path.exists()})
    if missing:
        raise FileNotFoundError("Missing walk sources:\n  " + "\n  ".join(missing))

    sheet = Image.new("RGBA", (SOURCE_SIZE, SOURCE_SIZE), (0, 0, 0, 0))
    frame_reports = []
    for index, frame in enumerate(FRAMES):
        figure, source_bounds = extract_figure(frame)
        normalized, scale = normalize_figure(figure, index)
        column = index % COLUMNS
        row = index // COLUMNS
        sheet.alpha_composite(normalized, (column * CELL_WIDTH, row * CELL_HEIGHT))
        frame_reports.append(
            {
                "frame": index + 1,
                "phase": frame.phase,
                "leading_leg": frame.leading_leg,
                "source": str(frame.path).replace("\\", "/"),
                "source_index": frame.index,
                "source_bounds": source_bounds,
                "source_figure_size": [figure.width, figure.height],
                "composition_scale": round(scale, 6),
            }
        )

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OUTPUT)
    save_preview(sheet)
    report = {
        "output": str(OUTPUT).replace("\\", "/"),
        "size": list(sheet.size),
        "layout": [COLUMNS, ROWS],
        "frame_order": [frame.phase for frame in FRAMES],
        "frames": frame_reports,
    }
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"SAVED {OUTPUT} ({sheet.width}x{sheet.height})")
    print(f"SAVED {PREVIEW}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())