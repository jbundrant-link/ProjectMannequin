#!/usr/bin/env python3
"""Create gameplay-scale review proxies for the Champion's Courtyard Lantern Urn pilot.

Per VISUAL_STYLE_BIBLE.md > "Proportion and scale calibration", the real gate is:

1. Choose target_world_height analytically against the canonical mannequin's
   true rendered height (~4.10 units) BEFORE wiring -- done here via
   LANTERN_URN_TARGET_WORLD_HEIGHT, reasoned in the generator script's
   comments and below.
2. process_single_green_sprite.py's pixel_size = target_world_height /
   visible_height_px guarantees the production sprite renders at exactly that
   true world height by construction -- no proxy needed to "prove" the ratio.
3. After wiring, a real runtime capture confirms the asset actually looks
   right at gameplay size, the same way the Rack Chest fix and the Trophy
   Podium's from-the-start calibration were verified.

The proxy below anchors to the canonical mannequin true height rather than to
any previous crate's assumed on-screen size, so it does not inherit any prior
miscalibration.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

from compose_enemy_sheet import load_source_rgba

PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_champions_courtyard_lantern_urn_style_pilot_v1.png"
)
SUPPLY_CRATE_PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_supply_crate_style_pilot_v1.png"
)
RACK_CHEST_PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_pavilion_rack_chest_style_pilot_v1.png"
)
TROPHY_PODIUM_PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_grand_tournament_trophy_podium_style_pilot_v1.png"
)
STAGE_CAPTURE = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_champions_courtyard_stage_only_1280x720.png"
)
STAGE_PROXY = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_champions_courtyard_lantern_urn_pilot_v1_stage_scale_proxy.png"
)
READABILITY_STRIP = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_champions_courtyard_lantern_urn_pilot_v1_readability_strip.png"
)
SILHOUETTE_COMPARISON = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_champions_courtyard_lantern_urn_pilot_v1_silhouette_comparison.png"
)

# Canonical mannequin true rendered height, measured via
# measure_true_sprite_world_height.py against mannequin_sheet_higgsfield_v1.png
# (idle frame, 10x9 sheet, SpritePixelSize=0.018): 4.104 world units.
MANNEQUIN_TRUE_WORLD_HEIGHT = 4.104
# Rough placeholder for how tall the mannequin reads in a 1280x720 World
# Warrior stage capture. Pre-visualization only; does not determine the real
# calibration.
ASSUMED_MANNEQUIN_ONSCREEN_HEIGHT_PX = 300

# Reasoned target: a round ceremonial urn with a lantern-dome cap reads as
# substantial finale-arena furniture, between the wide/stepped Trophy Podium
# (56.0%) and the tall standing Rack Chest (63.4%) -- round-bodied but
# heightened by its dome cap and finial. 2.45 world units = 59.7% of the
# mannequin's true height, solidly inside the "tall standing
# furniture/apparatus" 55-75% band from VISUAL_STYLE_BIBLE.md.
LANTERN_URN_TARGET_WORLD_HEIGHT = 2.45
# Already-approved reference heights, for the review notes / manifest only.
SUPPLY_CRATE_WORLD_HEIGHT = 1.30
RACK_CHEST_WORLD_HEIGHT = 2.60
TROPHY_PODIUM_WORLD_HEIGHT = 2.30


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
        raise FileNotFoundError("pilot or Champion's Courtyard stage capture is missing")
    for reference in (SUPPLY_CRATE_PILOT, RACK_CHEST_PILOT, TROPHY_PODIUM_PILOT):
        if not reference.is_file():
            raise FileNotFoundError(f"an approved crate pilot reference is missing: {reference}")

    figure = load_figure(PILOT)
    supply_crate = load_figure(SUPPLY_CRATE_PILOT)
    rack_chest = load_figure(RACK_CHEST_PILOT)
    trophy_podium = load_figure(TROPHY_PODIUM_PILOT)

    # Stage scale proxy: composite onto the real Champion's Courtyard at a
    # height derived from the mannequin true-height ratio (see module
    # docstring -- this is a rough pre-viz, not the proportion gate itself).
    onscreen_height = round(
        ASSUMED_MANNEQUIN_ONSCREEN_HEIGHT_PX
        * (LANTERN_URN_TARGET_WORLD_HEIGHT / MANNEQUIN_TRUE_WORLD_HEIGHT)
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

    # Silhouette comparison: lantern urn beside ALL THREE approved crates at a
    # common height, proving none of the four share a silhouette.
    common_height = 220
    figures = [
        (supply_crate, "Supply Crate (approved)"),
        (rack_chest, "Rack Chest (approved)"),
        (trophy_podium, "Trophy Podium (approved)"),
        (figure, "Lantern Urn (pilot)"),
    ]
    rendered = [(resize_to_height(fig, common_height), label) for fig, label in figures]
    gap = 36
    combo_width = sum(r.width for r, _ in rendered) + gap * (len(rendered) - 1) + 40
    combo = checkerboard(combo_width, common_height + 40)
    combo_draw = ImageDraw.Draw(combo)
    x_cursor = 20
    for rendered_figure, label in rendered:
        combo.alpha_composite(rendered_figure, (x_cursor, 20))
        combo_draw.text((x_cursor, 4), label, fill=(245, 247, 250, 255))
        x_cursor += rendered_figure.width + gap
    combo.convert("RGB").save(SILHOUETTE_COMPARISON)

    print(f"SAVED {STAGE_PROXY}")
    print(f"SAVED {READABILITY_STRIP}")
    print(f"SAVED {SILHOUETTE_COMPARISON}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
