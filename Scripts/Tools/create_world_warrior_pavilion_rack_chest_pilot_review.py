#!/usr/bin/env python3
"""Create gameplay-scale review proxies for the World Warrior Pavilion Rack Chest pilot."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

from compose_enemy_sheet import load_source_rgba


PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_pavilion_rack_chest_style_pilot_v1.png"
)
SUPPLY_CRATE_PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_supply_crate_style_pilot_v1.png"
)
STAGE_CAPTURE = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_pavilion_circuit_stage_only_1280x720.png"
)
STAGE_PROXY = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_pavilion_rack_chest_pilot_v1_stage_scale_proxy.png"
)
READABILITY_STRIP = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_pavilion_rack_chest_pilot_v1_readability_strip.png"
)
SILHOUETTE_COMPARISON = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_pavilion_rack_chest_pilot_v1_silhouette_comparison.png"
)

# The Sparring Supply Crate renders 80px tall in its own approved 1280x720
# runtime capture at its calibrated 1.30-unit world height. This pilot targets
# 1.85 world units (taller standing chest vs the low wide crate), so its
# proxy is scaled by the same world-height ratio for a fair side-by-side.
SUPPLY_CRATE_ONSCREEN_HEIGHT_PX = 80
SUPPLY_CRATE_WORLD_HEIGHT = 1.30
RACK_CHEST_TARGET_WORLD_HEIGHT = 1.85


def load_figure(path: Path) -> Image.Image:
    figure = load_source_rgba(path, "green")
    bounds = figure.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"{path}: no keyed object")
    return figure.crop(bounds)


def resize_to_height(figure: Image.Image, height: int) -> Image.Image:
    scale = height / figure.height
    return figure.resize(
        (max(1, round(figure.width * scale)), height),
        Image.Resampling.LANCZOS,
    )


def checkerboard(width: int, height: int, cell: int = 12) -> Image.Image:
    image = Image.new("RGBA", (width, height), (31, 32, 39, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, height, cell):
        for x in range(0, width, cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle(
                    (x, y, x + cell - 1, y + cell - 1),
                    fill=(43, 44, 53, 255),
                )
    return image


def main() -> int:
    if not PILOT.is_file() or not STAGE_CAPTURE.is_file():
        raise FileNotFoundError("pilot or Pavilion Circuit stage capture is missing")
    if not SUPPLY_CRATE_PILOT.is_file():
        raise FileNotFoundError("approved supply crate pilot is missing")

    figure = load_figure(PILOT)
    supply_crate = load_figure(SUPPLY_CRATE_PILOT)

    # Stage scale proxy: composite onto the real Pavilion Circuit floor at the
    # world-height-scaled proportion relative to the approved crate.
    onscreen_height = round(
        SUPPLY_CRATE_ONSCREEN_HEIGHT_PX
        * (RACK_CHEST_TARGET_WORLD_HEIGHT / SUPPLY_CRATE_WORLD_HEIGHT)
    )
    stage = Image.open(STAGE_CAPTURE).convert("RGBA")
    stage_figure = resize_to_height(figure, onscreen_height)
    ground_y = 660
    stage.alpha_composite(stage_figure, (600, ground_y - stage_figure.height))
    stage_draw = ImageDraw.Draw(stage)
    stage_draw.rectangle((584, 20, 1000, 48), fill=(18, 19, 24, 205))
    stage_draw.text(
        (594, 28),
        "PILOT SCALE PROXY - NOT RUNTIME",
        fill=(245, 247, 250, 255),
    )
    STAGE_PROXY.parent.mkdir(parents=True, exist_ok=True)
    stage.convert("RGB").save(STAGE_PROXY)

    # Readability strip at 96/128/160px, matching the established convention.
    heights = (96, 128, 160)
    panel_width = 260
    strip = checkerboard(panel_width * len(heights), 224)
    draw = ImageDraw.Draw(strip)
    baseline = 206
    draw.line((0, baseline, strip.width, baseline), fill=(91, 213, 255, 200), width=1)
    for index, height in enumerate(heights):
        rendered = resize_to_height(figure, height)
        panel_x = index * panel_width
        x = panel_x + (panel_width - rendered.width) // 2
        strip.alpha_composite(rendered, (x, baseline - rendered.height))
        draw.text((panel_x + 10, 9), f"{height}px", fill=(245, 247, 250, 255))
    strip.convert("RGB").save(READABILITY_STRIP)

    # Silhouette comparison: rack chest beside the approved supply crate at a
    # common height, proving the two crates do not share a silhouette.
    common_height = 220
    left = resize_to_height(supply_crate, common_height)
    right = resize_to_height(figure, common_height)
    gap = 40
    combo = checkerboard(left.width + gap + right.width + 40, common_height + 40)
    combo_draw = ImageDraw.Draw(combo)
    combo.alpha_composite(left, (20, 20))
    combo.alpha_composite(right, (20 + left.width + gap, 20))
    combo_draw.text((20, 4), "Supply Crate (approved)", fill=(245, 247, 250, 255))
    combo_draw.text(
        (20 + left.width + gap, 4), "Pavilion Rack Chest (pilot)", fill=(245, 247, 250, 255)
    )
    combo.convert("RGB").save(SILHOUETTE_COMPARISON)

    print(f"SAVED {STAGE_PROXY}")
    print(f"SAVED {READABILITY_STRIP}")
    print(f"SAVED {SILHOUETTE_COMPARISON}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
