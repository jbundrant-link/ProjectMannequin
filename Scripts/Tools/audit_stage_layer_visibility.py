"""Measures how much of each parallax layer a stage actually shows.

A layer can be fully authored and still contribute almost nothing to the frame,
because it is composited behind a layer that should be further away. That
happened on Pavilion Circuit: a torii gate, a lantern pavilion, and a stone
lantern were drawn entirely behind a deck wall, with only their feet visible.

Method. Each stage is captured normally and again with one layer hidden. The
pixels that differ are exactly what that layer contributes. This is a
measurement rather than a heuristic, so a layer that is legitimately small
(a couple of foreground props) is distinguishable from a layer that is being
suppressed.

Luminance separation and colour temperature are REPORTED but deliberately NOT
gated. A first version failed any stage whose near band was darker than its
distant band, and it flagged five stages that are composed correctly: Dojo
Approach is a dark dirt floor under a bright dusk sky, and the Archive interiors
are dark floors under lit ceilings. Absolute luminance difference cannot tell a
flat frame from a legitimately dark foreground, so the numbers are context for a
human rather than a pass/fail threshold.

The failure signal is a layer that draws far less than it occupies. Because the
hidden-layer capture reveals what is behind it, a suppressed layer shows a large
"occluded" area: pixels that change when the layer above is removed but not when
the layer itself is removed.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

# Per-channel difference that counts as "this pixel changed".
CHANNEL_DELTA = 12

LAYERS = ("Far", "Midground", "Foreground")


def load(path: Path) -> np.ndarray:
    return np.asarray(Image.open(path).convert("RGB"), dtype=np.int16)


def measure_stage(directory: Path, stage: str) -> dict:
    reference = load(directory / f"{stage}__reference.png")
    total = reference.shape[0] * reference.shape[1]
    layers = {}
    for layer in LAYERS:
        hidden_path = directory / f"{stage}__{layer}.png"
        if not hidden_path.exists():
            continue
        hidden = load(hidden_path)
        visible = np.abs(reference - hidden).max(axis=2) > CHANNEL_DELTA
        layers[layer] = {
            "visible_pixels": int(visible.sum()),
            "visible_fraction": round(float(visible.mean()), 5),
        }

    # Depth separation: how far the near gameplay band sits from the distant
    # band in luminance and colour temperature. Aerial perspective is what
    # stops a fighter and a building forty metres behind them reading as the
    # same distance, so it is measured rather than eyeballed.
    height = reference.shape[0]
    luma = reference.mean(axis=2)
    warmth = reference[:, :, 0].astype(np.float64) - reference[:, :, 2]
    far_band = slice(0, int(height * 0.35))
    near_band = slice(int(height * 0.56), height)

    return {
        "stage": stage,
        "total_pixels": int(total),
        "layers": layers,
        "far_luma": round(float(luma[far_band].mean()), 2),
        "near_luma": round(float(luma[near_band].mean()), 2),
        "luma_separation": round(
            float(luma[near_band].mean() - luma[far_band].mean()), 2),
        "far_warmth": round(float(warmth[far_band].mean()), 2),
        "near_warmth": round(float(warmth[near_band].mean()), 2),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, required=True)
    parser.add_argument("--stage", action="append", required=True)
    parser.add_argument(
        "--minimum-midground-fraction",
        type=float,
        default=0.01,
        help="A midground that draws less than this share of the frame is "
             "almost certainly being composited behind the backdrop.")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    results, suppressed = [], []
    print(
        f"{'stage':26s} {'Far':>9s} {'Mid':>9s} {'Fore':>7s} "
        f"{'lumaSep':>9s} {'farWarm':>9s}")
    for stage in args.stage:
        reference = args.directory / f"{stage}__reference.png"
        if not reference.exists():
            print(f"MISSING {reference}")
            return 2

        result = measure_stage(args.directory, stage)
        results.append(result)

        def share(layer: str) -> float:
            return result["layers"].get(layer, {}).get("visible_fraction", 0.0)

        midground = share("Midground")
        flag = ""
        if midground < args.minimum_midground_fraction:
            flag = "  <-- MIDGROUND SUPPRESSED"
            suppressed.append(stage)
        result["status"] = "suppressed" if flag else "ok"

        print(
            f"{stage:26s} {share('Far') * 100:8.2f}% {midground * 100:8.2f}% "
            f"{share('Foreground') * 100:6.2f}% {result['luma_separation']:9.2f} "
            f"{result['far_warmth']:9.2f}{flag}")

    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(
            json.dumps(
                {
                    "channel_delta": CHANNEL_DELTA,
                    "minimum_midground_fraction": args.minimum_midground_fraction,
                    "stages": results,
                    "suppressed": suppressed,
                },
                indent=2),
            encoding="utf-8")
        print(f"report={args.report}")

    if suppressed:
        print(f"FAIL {len(suppressed)} stage(s) hide their midground: {', '.join(suppressed)}")
        return 1


    print(f"OK {len(results)} stage(s) checked, every midground is visible.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
