"""Audit full-frame stage plate source art and visualise the gameplay ground band.

Two source-art defects are invisible to dimension, aspect, and pixel-fidelity
checks yet obvious in game:

* A flat horizontal strip left behind when a backdrop and a floor are stacked
  into one "complete" plate instead of being painted as one scene.
* A painted floor that does not line up with the frame rows the camera actually
  projects the gameplay ground plane onto, which reads as fighters hovering.

Grounding is gated in C# by ``StageGroundProjection`` and ``WorldRunTests``;
this tool takes the projected band as explicit arguments so the camera maths
keeps a single source of truth, and renders it over the art as review evidence.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--plate",
        action="append",
        nargs=3,
        metavar=("LABEL", "SOURCE", "FLIP_H"),
        required=True,
    )
    parser.add_argument(
        "--overlay-dir",
        type=Path,
        help="Write ground-band review images here.",
    )
    parser.add_argument(
        "--ground-far",
        type=float,
        help="Projected frame row of the far lane edge (0=top, 1=bottom).",
    )
    parser.add_argument(
        "--ground-near",
        type=float,
        help="Projected frame row of the near lane edge.",
    )
    parser.add_argument("--report", type=Path)
    parser.add_argument(
        "--max-flat-band-fraction",
        type=float,
        default=0.01,
        help="Largest tolerated flat horizontal strip, as a fraction of height.",
    )
    parser.add_argument("--flat-band-std", type=float, default=8.0)
    return parser.parse_args()


def parse_bool(value: str) -> bool:
    normalized = value.strip().lower()
    if normalized in {"1", "true", "yes"}:
        return True
    if normalized in {"0", "false", "no"}:
        return False
    raise ValueError(f"Invalid boolean value: {value}")


def find_flat_bands(
    pixels: np.ndarray,
    std_threshold: float,
    minimum_rows: int,
) -> list[tuple[int, int]]:
    """Row runs whose horizontal variation is too low to be painted detail."""
    row_variation = pixels.std(axis=1).mean(axis=1)
    flat = row_variation < std_threshold
    bands: list[tuple[int, int]] = []
    start: int | None = None
    for index, value in enumerate(flat):
        if value and start is None:
            start = index
        elif not value and start is not None:
            if index - start >= minimum_rows:
                bands.append((start, index - 1))
            start = None
    if start is not None and len(flat) - start >= minimum_rows:
        bands.append((start, len(flat) - 1))
    return bands


def write_overlay(
    image: Image.Image,
    output_path: Path,
    ground_far: float,
    ground_near: float,
    flat_bands: list[tuple[int, int]],
) -> None:
    scale = min(1.0, 1600 / image.width)
    preview = image.resize(
        (round(image.width * scale), round(image.height * scale)),
        Image.Resampling.LANCZOS,
    )
    draw = ImageDraw.Draw(preview, "RGBA")
    top = round(ground_far * preview.height)
    bottom = round(ground_near * preview.height)
    draw.rectangle([(0, top), (preview.width - 1, bottom)], fill=(0, 255, 128, 56))
    for label, fraction, colour in (
        ("far lane", ground_far, (255, 96, 96)),
        ("near lane", ground_near, (96, 200, 255)),
    ):
        row = round(fraction * preview.height)
        draw.line([(0, row), (preview.width, row)], fill=colour, width=3)
        draw.text((12, max(0, row - 18)), f"{label} {fraction:.3f}", fill=colour)
    for start, end in flat_bands:
        top_row = round(start / image.height * preview.height)
        bottom_row = round(end / image.height * preview.height)
        draw.rectangle(
            [(0, top_row), (preview.width - 1, bottom_row)],
            outline=(255, 0, 0),
            fill=(255, 0, 0, 72),
            width=3,
        )
        draw.text((12, max(0, top_row - 18)), "flat strip", fill=(255, 0, 0))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    preview.save(output_path, optimize=True)


def main() -> int:
    args = parse_args()
    wants_overlay = args.overlay_dir is not None
    if wants_overlay and (args.ground_far is None or args.ground_near is None):
        print(
            "--overlay-dir requires --ground-far and --ground-near",
            file=sys.stderr,
        )
        return 2

    plates: list[dict[str, object]] = []
    failed = False
    for label, source, flip_text in args.plate:
        source_path = Path(source)
        with Image.open(source_path).convert("RGB") as opened:
            image = (
                opened.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
                if parse_bool(flip_text)
                else opened.copy()
            )
        pixels = np.asarray(image, dtype=np.float32)
        minimum_rows = max(4, round(image.height * args.max_flat_band_fraction))
        flat_bands = find_flat_bands(pixels, args.flat_band_std, minimum_rows)
        # A plate legitimately ends on flat sky or shadow, so only interior
        # strips indicate a backdrop and floor stacked into one image.
        interior = [
            band
            for band in flat_bands
            if band[0] > image.height * 0.02 and band[1] < image.height * 0.98
        ]
        plate_failed = len(interior) > 0
        failed |= plate_failed
        plates.append(
            {
                "label": label,
                "source": source_path.as_posix(),
                "size": [image.width, image.height],
                "exact_16_9": image.width * 9 == image.height * 16,
                "interior_flat_bands": [
                    {
                        "top_fraction": round(start / image.height, 5),
                        "bottom_fraction": round(end / image.height, 5),
                        "height_fraction": round(
                            (end - start + 1) / image.height, 5
                        ),
                    }
                    for start, end in interior
                ],
                "passed": not plate_failed,
            }
        )
        if wants_overlay:
            output_path = args.overlay_dir / f"{label}_ground_band.png"
            write_overlay(
                image,
                output_path,
                args.ground_far,
                args.ground_near,
                interior,
            )
            plates[-1]["overlay"] = output_path.as_posix()

    report = {
        "max_flat_band_fraction": args.max_flat_band_fraction,
        "flat_band_std": args.flat_band_std,
        "projected_ground_far": args.ground_far,
        "projected_ground_near": args.ground_near,
        "plates": plates,
        "passed": not failed,
    }
    text = json.dumps(report, indent=2)
    print(text)
    if args.report is not None:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(text + "\n", encoding="utf-8")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
