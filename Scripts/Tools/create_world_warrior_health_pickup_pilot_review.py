#!/usr/bin/env python3
"""Create gameplay-scale review proxies for the Vitality Gourd pilot."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

from compose_enemy_sheet import load_source_rgba


PILOT = Path(
    "Assets/Sprites/Concepts/StyleCalibration/"
    "world_warrior_health_pickup_style_pilot_v1.png"
)
STAGE_CAPTURE = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_training_dummy_intact_runtime_1280x720.png"
)
STAGE_PROXY = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_health_pickup_pilot_v1_stage_scale_proxy.png"
)
READABILITY_STRIP = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_health_pickup_pilot_v1_readability_strip.png"
)


def load_figure() -> Image.Image:
    figure = load_source_rgba(PILOT, "green")
    bounds = figure.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"{PILOT}: no keyed object")
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
        raise FileNotFoundError("pilot or approved Stage 1 capture is missing")

    figure = load_figure()
    stage = Image.open(STAGE_CAPTURE).convert("RGBA")
    stage_figure = resize_to_height(figure, 80)
    stage.alpha_composite(stage_figure, (740, 510 - stage_figure.height))
    stage_draw = ImageDraw.Draw(stage)
    stage_draw.rectangle((724, 392, 1008, 420), fill=(18, 19, 24, 205))
    stage_draw.text(
        (734, 400),
        "PILOT SCALE PROXY - NOT RUNTIME",
        fill=(245, 247, 250, 255),
    )
    STAGE_PROXY.parent.mkdir(parents=True, exist_ok=True)
    stage.convert("RGB").save(STAGE_PROXY)

    heights = (64, 80, 96)
    panel_width = 180
    strip = checkerboard(panel_width * len(heights), 160)
    draw = ImageDraw.Draw(strip)
    baseline = 146
    draw.line((0, baseline, strip.width, baseline), fill=(91, 213, 255, 200), width=1)
    for index, height in enumerate(heights):
        rendered = resize_to_height(figure, height)
        panel_x = index * panel_width
        x = panel_x + (panel_width - rendered.width) // 2
        strip.alpha_composite(rendered, (x, baseline - rendered.height))
        draw.text((panel_x + 10, 9), f"{height}px", fill=(245, 247, 250, 255))
    strip.convert("RGB").save(READABILITY_STRIP)

    print(f"SAVED {STAGE_PROXY}")
    print(f"SAVED {READABILITY_STRIP}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())