#!/usr/bin/env python3
"""Create gameplay-scale review proxies for the Grand Tournament Trophy Podium pilot.

Unlike the earlier crate reviews, the stage-scale proxy here is explicitly a
rough pre-visualization aid only, not the proportion gate itself. Per
VISUAL_STYLE_BIBLE.md > "Proportion and scale calibration", the real gate is:

1. Choose target_world_height analytically against the canonical mannequin's
   true rendered height (~4.10 units) BEFORE wiring -- done here via
   TROPHY_PODIUM_TARGET_WORLD_HEIGHT, reasoned in the generator script's
   comments.
2. process_single_green_sprite.py's pixel_size = target_world_height /
   visible_height_px guarantees the production sprite renders at exactly that
   true world height by construction -- no proxy needed to "prove" the ratio.
3. After wiring, a real runtime capture confirms the asset actually looks
   right at gameplay size (see capture_world_warrior_grand_tournament_trophy_
   podium_runtime.ps1), the same way the Pavilion Rack Chest fix was verified.

The proxy below anchors to the canonical mannequin true height rather than to
either previous crate's assumed on-screen size, so it does not inherit any
prior miscalibration.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

from compose_enemy_sheet import load_source_rgba

PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_grand_tournament_trophy_podium_style_pilot_v1.png"
)
SUPPLY_CRATE_PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_supply_crate_style_pilot_v1.png"
)
RACK_CHEST_PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_pavilion_rack_chest_style_pilot_v1.png"
)
STAGE_CAPTURE = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_grand_tournament_stage_only_1280x720.png"
)
STAGE_PROXY = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_grand_tournament_trophy_podium_pilot_v1_stage_scale_proxy.png"
)
READABILITY_STRIP = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_grand_tournament_trophy_podium_pilot_v1_readability_strip.png"
)
SILHOUETTE_COMPARISON = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_grand_tournament_trophy_podium_pilot_v1_silhouette_comparison.png"
)

# Canonical mannequin true rendered height, measured via
# measure_true_sprite_world_height.py against mannequin_sheet_higgsfield_v1.png
# (idle frame, 10x9 sheet, SpritePixelSize=0.018): 4.104 world units.
MANNEQUIN_TRUE_WORLD_HEIGHT = 4.104
# Rough placeholder for how tall the mannequin reads in a 1280x720 World
# Warrior stage capture. This is a pre-visualization assumption only; it does
# NOT determine the asset's real calibration (target_world_height does that
# directly), only how big to draw it in this proxy image.
ASSUMED_MANNEQUIN_ONSCREEN_HEIGHT_PX = 300

# Reasoned target: a stepped stone podium reads as substantial tournament
# furniture (not a footstool) but is deliberately the shortest-profile of the
# three crates since it is wide-based rather than tall-standing. 2.30 world
# units = 56.0% of the mannequin's true height, solidly inside the
# "tall standing furniture/apparatus" 55-75% band from VISUAL_STYLE_BIBLE.md,
# at its low end because the silhouette is wide/stepped rather than a single
# tall vertical mass.
TROPHY_PODIUM_TARGET_WORLD_HEIGHT = 2.30
# Already-approved reference heights, for the review notes / manifest only.
SUPPLY_CRATE_WORLD_HEIGHT = 1.30
RACK_CHEST_WORLD_HEIGHT = 2.60


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
        raise FileNotFoundError("pilot or Grand Tournament Floor stage capture is missing")
    if not SUPPLY_CRATE_PILOT.is_file() or not RACK_CHEST_PILOT.is_file():
        raise FileNotFoundError("an approved crate pilot reference is missing")

    figure = load_figure(PILOT)
    supply_crate = load_figure(SUPPLY_CRATE_PILOT)
    rack_chest = load_figure(RACK_CHEST_PILOT)

    # Stage scale proxy: composite onto the real Grand Tournament Floor at a
    # height derived from the mannequin true-height ratio (see module
    # docstring -- this is a rough pre-viz, not the proportion gate itself).
    onscreen_height = round(
        ASSUMED_MANNEQUIN_ONSCREEN_HEIGHT_PX
        * (TROPHY_PODIUM_TARGET_WORLD_HEIGHT / MANNEQUIN_TRUE_WORLD_HEIGHT)
    )
    stage = Image.open(STAGE_CAPTURE).convert("RGBA")
    stage_figure = resize_to_height(figure, onscreen_height)
    ground_y = 660
    stage.alpha_composite(stage_figure, (600, ground_y - stage_figure.height))
    stage_draw = ImageDraw.Draw(stage)
    stage_draw.rectangle((584, 20, 1050, 48), fill=(18, 19, 24, 205))
    stage_draw.text(
        (594, 28),
        "PILOT SCALE PROXY - NOT RUNTIME - true height sets the real scale",
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

    # Silhouette comparison: trophy podium beside BOTH approved crates at a
    # common height, proving all three do not share a silhouette.
    common_height = 220
    left = resize_to_height(supply_crate, common_height)
    middle = resize_to_height(rack_chest, common_height)
    right = resize_to_height(figure, common_height)
    gap = 40
    combo_width = left.width + gap + middle.width + gap + right.width + 40
    combo = checkerboard(combo_width, common_height + 40)
    combo_draw = ImageDraw.Draw(combo)
    x_cursor = 20
    combo.alpha_composite(left, (x_cursor, 20))
    combo_draw.text((x_cursor, 4), "Supply Crate (approved)", fill=(245, 247, 250, 255))
    x_cursor += left.width + gap
    combo.alpha_composite(middle, (x_cursor, 20))
    combo_draw.text((x_cursor, 4), "Rack Chest (approved)", fill=(245, 247, 250, 255))
    x_cursor += middle.width + gap
    combo.alpha_composite(right, (x_cursor, 20))
    combo_draw.text((x_cursor, 4), "Trophy Podium (pilot)", fill=(245, 247, 250, 255))
    combo.convert("RGB").save(SILHOUETTE_COMPARISON)

    print(f"SAVED {STAGE_PROXY}")
    print(f"SAVED {READABILITY_STRIP}")
    print(f"SAVED {SILHOUETTE_COMPARISON}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
