#!/usr/bin/env python3
"""Create clean AIR-timed GIF previews for Ryu's V5 action atlas."""

import json
from pathlib import Path

from PIL import Image


ATLAS_PATH = Path("Assets/Sprites/Ryu/ryu_higgsfield_v5_actions.png")
MAP_PATH = Path("Assets/Sprites/Ryu/ryu_higgsfield_v5_animation_map.json")
OUTPUT_DIR = Path("Assets/Sprites/Ryu/Diagnostics/V5")
FRAME_SIZE = 256
BACKGROUND = (38, 42, 48, 255)
PREVIEWS = (
    "ryu_collarbone_breaker",
    "ryu_solar_plexus",
    "ryu_shoulder_throw",
    "ryu_back_throw",
    "ryu_hadouken_heavy",
    "ryu_shoryuken_heavy",
    "ryu_tatsumaki_heavy",
    "ryu_air_tatsumaki_heavy",
    "ryu_joudan_heavy",
    "ryu_shinku_hadouken",
    "ryu_shin_shoryuken",
    "ryu_denjin_hadouken",
)


def main() -> int:
    if not ATLAS_PATH.exists() or not MAP_PATH.exists():
        print("ERROR: Build the V5 atlas and animation map first.")
        return 1

    animation_map = json.loads(MAP_PATH.read_text(encoding="utf-8"))
    columns = animation_map["atlas"]["columns"]
    with Image.open(ATLAS_PATH) as atlas_image:
        atlas = atlas_image.convert("RGBA")

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    for move_id in PREVIEWS:
        move = animation_map["moves"][move_id]
        frames = []
        durations = []
        for frame_index, air_ticks in zip(move["frames"], move["durations"]):
            column = frame_index % columns
            row = frame_index // columns
            sprite = atlas.crop(
                (
                    column * FRAME_SIZE,
                    row * FRAME_SIZE,
                    (column + 1) * FRAME_SIZE,
                    (row + 1) * FRAME_SIZE,
                )
            )
            preview = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), BACKGROUND)
            preview.alpha_composite(sprite)
            frames.append(preview.convert("P", palette=Image.Palette.ADAPTIVE))
            durations.append(max(24, min(500, round(max(1, air_ticks) * 1000 / 60))))

        output = OUTPUT_DIR / f"{move_id}.gif"
        frames[0].save(
            output,
            save_all=True,
            append_images=frames[1:],
            duration=durations,
            loop=0,
            disposal=2,
        )
        print(f"Saved {output} ({len(frames)} AIR elements)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
