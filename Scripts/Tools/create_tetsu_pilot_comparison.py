#!/usr/bin/env python3
"""Compare Tetsu with approved World Warrior identities at gameplay scale."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

from compose_enemy_sheet import load_source_rgba


FRAME_SIZE = 256
ACTOR_HEIGHT = 230
PANEL_WIDTH = 256
LABEL_HEIGHT = 44
OUTPUT = Path(
    "Artifacts/StyleCalibration/"
    "world_warrior_grand_grappler_tetsu_pilot_v3_comparison.png"
)
SOURCES = (
    (
        "Tournament Grappler",
        Path("Assets/Sprites/Enemies/world_warrior_grappler_style_v3.png"),
        "atlas",
    ),
    (
        "Grand Grappler Tetsu",
        Path(
            "Assets/Sprites/Concepts/StyleCalibration/"
            "world_warrior_grand_grappler_tetsu_style_pilot_v3_normalized.png"
        ),
        "green",
    ),
    (
        "Pavilion Ace Makoto",
        Path(
            "Assets/Sprites/Enemies/"
            "world_warrior_pavilion_ace_makoto_style_v1.png"
        ),
        "atlas",
    ),
)


def checkerboard(width: int, height: int, cell: int = 16) -> Image.Image:
    canvas = Image.new("RGBA", (width, height), (35, 38, 46, 255))
    draw = ImageDraw.Draw(canvas)
    for y in range(0, height, cell):
        for x in range(0, width, cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle(
                    (x, y, x + cell - 1, y + cell - 1),
                    fill=(43, 46, 55, 255),
                )
    return canvas


def load_figure(path: Path, source_type: str) -> Image.Image:
    if source_type == "atlas":
        source = Image.open(path).convert("RGBA")
        figure = source.crop((0, 0, FRAME_SIZE, FRAME_SIZE))
    else:
        figure = load_source_rgba(path, source_type)

    bounds = figure.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError(f"{path}: no visible figure")
    return figure.crop(bounds)


def main() -> int:
    for _, path, _ in SOURCES:
        if not path.is_file():
            raise FileNotFoundError(path)

    canvas = checkerboard(PANEL_WIDTH * len(SOURCES), LABEL_HEIGHT + FRAME_SIZE)
    draw = ImageDraw.Draw(canvas)
    baseline = LABEL_HEIGHT + FRAME_SIZE - 8
    draw.line(
        (0, baseline, canvas.width, baseline),
        fill=(91, 213, 255, 190),
        width=1,
    )

    for index, (label, path, source_type) in enumerate(SOURCES):
        figure = load_figure(path, source_type)
        scale = min(
            ACTOR_HEIGHT / figure.height,
            (PANEL_WIDTH - 24) / figure.width,
        )
        figure = figure.resize(
            (
                max(1, round(figure.width * scale)),
                max(1, round(figure.height * scale)),
            ),
            Image.Resampling.LANCZOS,
        )
        panel_x = index * PANEL_WIDTH
        x = panel_x + (PANEL_WIDTH - figure.width) // 2
        y = baseline - figure.height
        canvas.alpha_composite(figure, (x, y))
        draw.text((panel_x + 8, 7), label, fill=(245, 247, 250, 255))
        draw.text(
            (panel_x + 8, 23),
            f"{figure.width}x{figure.height}",
            fill=(155, 184, 205, 255),
        )

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(OUTPUT)
    print(f"SAVED {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())