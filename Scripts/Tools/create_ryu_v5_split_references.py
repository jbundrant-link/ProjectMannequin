#!/usr/bin/env python3
"""Create balanced source chunks for long Ryu MUGEN animations."""

import argparse
from pathlib import Path

from audit_mugen_character import (
    load_act_palette,
    parse_air,
    parse_sff_v1,
    unique_frames_with_sequence,
    write_reference_sheet,
)


OUTPUT_DIR = Path("Assets/Sprites/Ryu/Higgsfield/V5References")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("character_dir", type=Path)
    args = parser.parse_args()

    character_dir = args.character_dir.resolve()
    actions = parse_air(character_dir / "sf3_ryu.air")
    palette = load_act_palette(character_dir / "act" / "ryu01.act")
    sprites = parse_sff_v1(character_dir / "sf3_ryu.sff", palette)

    shinku_unique, _ = unique_frames_with_sequence(actions[3100])
    if len(shinku_unique) != 24:
        raise RuntimeError(
            f"Expected 24 unique Shinku poses, found {len(shinku_unique)}"
        )

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    first_path, first_count = write_reference_sheet(
        3100,
        "chunk_a",
        shinku_unique[:12],
        sprites,
        OUTPUT_DIR,
    )
    second_path, second_count = write_reference_sheet(
        3100,
        "chunk_b",
        shinku_unique[12:],
        sprites,
        OUTPUT_DIR,
    )
    print(f"Saved {first_path} ({first_count} poses)")
    print(f"Saved {second_path} ({second_count} poses)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
