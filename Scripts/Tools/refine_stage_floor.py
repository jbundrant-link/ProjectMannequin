#!/usr/bin/env python3
"""Build a quiet seamless stage floor from a generated color/material source."""

from __future__ import annotations

import argparse
from pathlib import Path

import numpy as np
from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--seed", type=int, default=2719)
    return parser.parse_args()


def periodic_noise(
    rng: np.random.Generator,
    height: int,
    width: int,
    cutoff_y: float,
    cutoff_x: float,
) -> np.ndarray:
    source = rng.standard_normal((height, width), dtype=np.float32)
    spectrum = np.fft.rfft2(source)
    frequency_y = np.fft.fftfreq(height)[:, np.newaxis]
    frequency_x = np.fft.rfftfreq(width)[np.newaxis, :]
    low_pass = np.exp(
        -0.5
        * (
            (frequency_y / cutoff_y) ** 2
            + (frequency_x / cutoff_x) ** 2
        )
    )
    field = np.fft.irfft2(spectrum * low_pass, s=(height, width)).real
    field -= field.mean()
    deviation = field.std()
    return field / deviation if deviation > 0.0 else field


def main() -> int:
    args = parse_args()
    source = Image.open(args.source).convert("RGB")
    source_pixels = np.asarray(source, dtype=np.float32)
    height, width = source_pixels.shape[:2]
    center = source_pixels[
        height // 5 : height * 4 // 5,
        width // 5 : width * 4 // 5,
    ]
    base_color = np.median(center.reshape(-1, 3), axis=0)

    rng = np.random.default_rng(args.seed)
    broad_field = periodic_noise(rng, height, width, 0.0045, 0.0075)
    lateral_field = periodic_noise(rng, height, width, 0.0025, 0.0200)
    fine_field = periodic_noise(rng, height, width, 0.0550, 0.0700)

    result = np.empty_like(source_pixels)
    result[..., 0] = (
        base_color[0] + broad_field * 13.0 + lateral_field * 4.5 + fine_field * 0.8
    )
    result[..., 1] = (
        base_color[1] + broad_field * 9.0 + lateral_field * 2.8 + fine_field * 0.7
    )
    result[..., 2] = (
        base_color[2] + broad_field * 11.0 + lateral_field * 6.0 + fine_field * 1.0
    )
    result = np.clip(np.rint(result), 0, 255).astype(np.uint8)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(result, "RGB").save(args.output)
    print(
        f"SAVED {args.output} {width}x{height} "
        f"base={[round(float(value), 1) for value in base_color]}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())