#!/usr/bin/env python3
"""Create gameplay-scale review proxies for the two Pavilion Circuit hazard pilots.

CORRECTED PLACEMENT: these are World Warrior's first hazard zones
(StageHazardZoneData), belonging to Pavilion Circuit (Stage 2) per
MASTER_IMPLEMENTATION_PLAN.md > Phase 5 > item 16, not Dojo Approach where an
earlier session mistakenly wired them. See
generate_world_warrior_pavilion_rolling_log_pilot.ps1 for the full
correction rationale.

Per VISUAL_STYLE_BIBLE.md > "Proportion and scale calibration", hazard
emitters/set pieces scale to the stage's authored geometry and function
rather than a fixed character-height band; the reasoning (unchanged from the
original Dojo-placed design, since it was based on hazard function, not
stage identity) is recorded here and in the review notes:

- Rolling Log (LinearSweep): target_world_height 1.00 unit (diameter, lying
  on its side) -- a thick, unmistakably substantial obstacle log.
- Falling Weight (FallingStrike): target_world_height 1.20 units -- slightly
  larger for clear incoming-danger readability while hanging/falling.
"""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

from compose_enemy_sheet import load_source_rgba

ROLLING_LOG_PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_pavilion_rolling_log_style_pilot_v1.png"
)
FALLING_WEIGHT_PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_pavilion_falling_weight_style_pilot_v1.png"
)
RACK_CHEST_SOURCE = Path(
    "Assets/Sprites/Props/WorldWarrior/"
    "world_warrior_pavilion_rack_chest_style_v1.png"
)
TRAINING_DUMMY_SOURCE = Path(
    "Assets/Sprites/Props/WorldWarrior/"
    "world_warrior_training_dummy_style_v1.png"
)
STAGE_CAPTURE = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_pavilion_circuit_stage_only_1280x720.png"
)
STAGE_PROXY = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_pavilion_hazards_pilot_v1_stage_scale_proxy.png"
)
ROLLING_LOG_READABILITY_STRIP = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_pavilion_rolling_log_pilot_v1_readability_strip.png"
)
FALLING_WEIGHT_READABILITY_STRIP = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_pavilion_falling_weight_pilot_v1_readability_strip.png"
)
SILHOUETTE_COMPARISON = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_pavilion_hazards_pilot_v1_silhouette_comparison.png"
)

MANNEQUIN_TRUE_WORLD_HEIGHT = 4.104
ASSUMED_MANNEQUIN_ONSCREEN_HEIGHT_PX = 300

ROLLING_LOG_TARGET_WORLD_HEIGHT = 1.00
FALLING_WEIGHT_TARGET_WORLD_HEIGHT = 1.20
# Already-approved reference heights, for the review notes / manifest only.
RACK_CHEST_WORLD_HEIGHT = 2.60
TRAINING_DUMMY_WORLD_HEIGHT = 2.80


def load_figure(path: Path, bg: str = "green") -> Image.Image:
    figure = load_source_rgba(path, bg)
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


def readability_strip(figure: Image.Image, output: Path) -> None:
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
    strip.convert("RGB").save(output)


def main() -> int:
    if not ROLLING_LOG_PILOT.is_file() or not FALLING_WEIGHT_PILOT.is_file():
        raise FileNotFoundError("a hazard pilot is missing")
    if not STAGE_CAPTURE.is_file():
        raise FileNotFoundError("Pavilion Circuit stage-only capture is missing")
    for reference in (RACK_CHEST_SOURCE, TRAINING_DUMMY_SOURCE):
        if not reference.is_file():
            raise FileNotFoundError(f"a comparison reference is missing: {reference}")

    rolling_log = load_figure(ROLLING_LOG_PILOT)
    falling_weight = load_figure(FALLING_WEIGHT_PILOT)
    rack_chest = load_figure(RACK_CHEST_SOURCE, bg="alpha")
    training_dummy = load_figure(TRAINING_DUMMY_SOURCE, bg="alpha")

    # Stage scale proxy: both hazards composited onto the real Pavilion
    # Circuit at heights derived from the mannequin true-height ratio.
    stage = Image.open(STAGE_CAPTURE).convert("RGBA")
    log_onscreen_height = round(
        ASSUMED_MANNEQUIN_ONSCREEN_HEIGHT_PX
        * (ROLLING_LOG_TARGET_WORLD_HEIGHT / MANNEQUIN_TRUE_WORLD_HEIGHT)
    )
    weight_onscreen_height = round(
        ASSUMED_MANNEQUIN_ONSCREEN_HEIGHT_PX
        * (FALLING_WEIGHT_TARGET_WORLD_HEIGHT / MANNEQUIN_TRUE_WORLD_HEIGHT)
    )
    ground_y = 660
    log_figure = resize_to_height(rolling_log, log_onscreen_height)
    stage.alpha_composite(log_figure, (420, ground_y - log_figure.height))
    weight_figure = resize_to_height(falling_weight, weight_onscreen_height)
    stage.alpha_composite(weight_figure, (760, ground_y - 340 - weight_figure.height))
    stage_draw = ImageDraw.Draw(stage)
    stage_draw.rectangle((60, 20, 700, 48), fill=(18, 19, 24, 205))
    stage_draw.text(
        (70, 28),
        "PILOT SCALE PROXY - NOT RUNTIME - true height sets the real scale",
        fill=(245, 247, 250, 255),
    )
    STAGE_PROXY.parent.mkdir(parents=True, exist_ok=True)
    stage.convert("RGB").save(STAGE_PROXY)

    # Readability strips, one per hazard, matching the established convention.
    readability_strip(rolling_log, ROLLING_LOG_READABILITY_STRIP)
    readability_strip(falling_weight, FALLING_WEIGHT_READABILITY_STRIP)

    # Silhouette comparison: both new hazards beside the approved training
    # dummy and rack chest at a common height, proving none share a
    # silhouette.
    common_height = 220
    figures = [
        (training_dummy, "Training Dummy (approved)"),
        (rack_chest, "Rack Chest (approved)"),
        (rolling_log, "Rolling Log (pilot)"),
        (falling_weight, "Falling Weight (pilot)"),
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
    print(f"SAVED {ROLLING_LOG_READABILITY_STRIP}")
    print(f"SAVED {FALLING_WEIGHT_READABILITY_STRIP}")
    print(f"SAVED {SILHOUETTE_COMPARISON}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
