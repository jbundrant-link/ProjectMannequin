#!/usr/bin/env python3
"""Extract a MUGEN 1.x character's move animation references and timing.

This is an offline authoring tool. It does not ship MUGEN data with the game.
It reads the character's CMD/CNS/AIR/SFF files and writes transparent pose
references plus a machine-readable animation audit for comparison work.
"""

from __future__ import annotations

import argparse
import io
import json
import re
import struct
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image


ACTION_NAMES = {
    0: "idle",
    20: "walk_forward",
    21: "walk_back",
    40: "jump_start",
    41: "neutral_jump",
    42: "forward_jump",
    100: "dash_forward",
    105: "dash_back",
    200: "standing_light_punch_far",
    205: "standing_light_punch_close",
    210: "standing_medium_punch_far",
    215: "standing_medium_punch_close",
    220: "standing_heavy_punch_far",
    225: "standing_heavy_punch_close",
    230: "standing_light_kick",
    240: "standing_medium_kick_far",
    245: "standing_medium_kick_close",
    250: "standing_heavy_kick",
    251: "target_combo_heavy_kick",
    400: "crouching_light_punch",
    410: "crouching_medium_punch",
    420: "crouching_heavy_punch",
    430: "crouching_light_kick",
    440: "crouching_medium_kick",
    450: "crouching_heavy_kick",
    600: "jump_light_punch_forward",
    605: "jump_light_punch_neutral",
    610: "jump_medium_punch_forward",
    615: "jump_medium_punch_neutral",
    620: "jump_heavy_punch_forward",
    625: "jump_heavy_punch_neutral",
    630: "jump_light_kick_forward",
    635: "jump_light_kick_neutral",
    640: "jump_medium_kick_forward",
    645: "jump_medium_kick_neutral",
    650: "jump_heavy_kick_forward",
    655: "jump_heavy_kick_neutral",
    800: "throw_start",
    810: "shoulder_throw",
    820: "back_throw",
    830: "throw_whiff",
    900: "collarbone_breaker",
    910: "solar_plexus_strike",
    920: "leap_attack",
    1000: "hadouken",
    1030: "ex_hadouken",
    1100: "light_shoryuken",
    1110: "medium_shoryuken",
    1120: "heavy_shoryuken",
    1130: "ex_shoryuken",
    1200: "light_tatsumaki",
    1210: "medium_tatsumaki",
    1220: "heavy_tatsumaki",
    1230: "ex_tatsumaki",
    1300: "air_light_tatsumaki",
    1310: "air_medium_tatsumaki",
    1320: "air_heavy_tatsumaki",
    1330: "air_ex_tatsumaki",
    1400: "light_joudan",
    1410: "medium_joudan",
    1420: "heavy_joudan",
    1430: "ex_joudan",
    3000: "shin_shoryuken_start",
    3010: "shin_shoryuken_finish",
    3020: "shin_shoryuken_land",
    3100: "shinku_hadouken",
    3200: "denjin_hadouken_charge",
    3220: "denjin_hadouken_recover",
}


@dataclass(frozen=True)
class AirFrame:
    group: int
    image: int
    offset_x: int
    offset_y: int
    duration: int
    flags: str = ""


@dataclass
class SffSprite:
    group: int
    image: int
    axis_x: int
    axis_y: int
    pixels: Image.Image


def read_game_text(path: Path) -> str:
    data = path.read_bytes()
    try:
        return data.decode("utf-8")
    except UnicodeDecodeError:
        return data.decode("cp932", errors="replace")


def parse_air(path: Path) -> dict[int, list[AirFrame]]:
    text = read_game_text(path)
    pattern = re.compile(
        r"^\[Begin Action\s+(?P<id>\d+)\]\s*(?P<body>.*?)(?=^\[Begin Action|\Z)",
        re.MULTILINE | re.DOTALL,
    )
    actions: dict[int, list[AirFrame]] = {}
    frame_pattern = re.compile(
        r"^(?P<group>-?\d+)\s*,\s*(?P<image>-?\d+)\s*,\s*"
        r"(?P<x>-?\d+)\s*,\s*(?P<y>-?\d+)\s*,\s*(?P<duration>-?\d+)"
        r"(?:\s*,\s*(?P<flags>[^,\s;]+))?"
    )
    for match in pattern.finditer(text):
        action_id = int(match.group("id"))
        frames: list[AirFrame] = []
        for source_line in match.group("body").splitlines():
            line = source_line.split(";", 1)[0].strip()
            frame_match = frame_pattern.match(line)
            if not frame_match:
                continue
            frames.append(
                AirFrame(
                    group=int(frame_match.group("group")),
                    image=int(frame_match.group("image")),
                    offset_x=int(frame_match.group("x")),
                    offset_y=int(frame_match.group("y")),
                    duration=int(frame_match.group("duration")),
                    flags=(frame_match.group("flags") or "").upper(),
                )
            )
        if frames:
            actions[action_id] = frames
    return actions


def load_act_palette(path: Path) -> list[int]:
    data = path.read_bytes()
    if len(data) != 768:
        raise ValueError(f"Expected a 768-byte ACT palette, got {len(data)} bytes: {path}")
    entries = [data[index : index + 3] for index in range(0, len(data), 3)]
    return list(b"".join(reversed(entries)))


def indexed_to_rgba(source: Image.Image, palette: list[int]) -> Image.Image:
    indexed = Image.frombytes("P", source.size, source.tobytes())
    indexed.putpalette(palette)
    rgba = indexed.convert("RGBA")
    alpha = Image.eval(source, lambda value: 0 if value == 0 else 255)
    rgba.putalpha(alpha)
    return rgba


def parse_sff_v1(path: Path, palette: list[int]) -> dict[tuple[int, int], SffSprite]:
    data = path.read_bytes()
    if data[:12] != b"ElecbyteSpr\x00":
        raise ValueError(f"Unsupported SFF signature: {path}")
    version = tuple(data[12:16])
    if version[1] != 1:
        raise ValueError(f"Only SFF v1 is supported, found version bytes {version}")

    image_count = struct.unpack_from("<I", data, 20)[0]
    offset = struct.unpack_from("<I", data, 24)[0]
    subheader_size = struct.unpack_from("<I", data, 28)[0]
    sprites: dict[tuple[int, int], SffSprite] = {}
    previous: SffSprite | None = None

    for _ in range(image_count):
        if offset <= 0 or offset + subheader_size > len(data):
            break
        next_offset, data_length, axis_x, axis_y, group, image, _shared = struct.unpack_from(
            "<IIhhHHH", data, offset
        )
        blob_start = offset + subheader_size
        blob_end = blob_start + data_length

        if data_length > 0:
            source = Image.open(io.BytesIO(data[blob_start:blob_end]))
            source.load()
            if source.mode not in {"L", "P"}:
                source = source.convert("L")
            pixels = indexed_to_rgba(source.convert("L"), palette)
            previous = SffSprite(group, image, axis_x, axis_y, pixels)
        elif previous is not None:
            previous = SffSprite(group, image, axis_x, axis_y, previous.pixels.copy())
        else:
            previous = None

        if previous is not None:
            sprites[(group, image)] = previous
        if next_offset == 0:
            break
        offset = next_offset

    return sprites


def transformed_sprite(sprite: SffSprite, flags: str) -> tuple[Image.Image, int, int]:
    pixels = sprite.pixels
    axis_x = sprite.axis_x
    axis_y = sprite.axis_y
    if "H" in flags:
        pixels = pixels.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        axis_x = pixels.width - axis_x
    if "V" in flags:
        pixels = pixels.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        axis_y = pixels.height - axis_y
    return pixels, axis_x, axis_y


def render_pose(
    frame: AirFrame,
    sprites: dict[tuple[int, int], SffSprite],
    cell_size: tuple[int, int],
) -> Image.Image | None:
    sprite = sprites.get((frame.group, frame.image))
    if sprite is None:
        return None
    pixels, axis_x, axis_y = transformed_sprite(sprite, frame.flags)
    cell_width, cell_height = cell_size
    anchor_x = cell_width // 2
    anchor_y = cell_height - 24
    x = anchor_x - axis_x + frame.offset_x
    y = anchor_y - axis_y + frame.offset_y
    cell = Image.new("RGBA", cell_size, (0, 0, 0, 0))
    cell.alpha_composite(pixels, (x, y))
    return cell


def collapse_consecutive_frames(frames: Iterable[AirFrame]) -> list[AirFrame]:
    collapsed: list[AirFrame] = []
    for frame in frames:
        key = (frame.group, frame.image, frame.offset_x, frame.offset_y, frame.flags)
        if collapsed:
            previous = collapsed[-1]
            previous_key = (
                previous.group,
                previous.image,
                previous.offset_x,
                previous.offset_y,
                previous.flags,
            )
            if key == previous_key:
                continue
        collapsed.append(frame)
    return collapsed


def unique_frames_with_sequence(
    frames: Iterable[AirFrame],
) -> tuple[list[AirFrame], list[int]]:
    unique_frames: list[AirFrame] = []
    key_to_index: dict[tuple[int, int, int, int, str], int] = {}
    sequence: list[int] = []
    for frame in frames:
        key = (frame.group, frame.image, frame.offset_x, frame.offset_y, frame.flags)
        if key not in key_to_index:
            key_to_index[key] = len(unique_frames)
            unique_frames.append(frame)
        sequence.append(key_to_index[key])
    return unique_frames, sequence


def write_reference_sheet(
    action_id: int,
    reference_name: str,
    frames: list[AirFrame],
    sprites: dict[tuple[int, int], SffSprite],
    output_dir: Path,
) -> tuple[Path, int]:
    cell_size = (256, 224)
    rendered = [render_pose(frame, sprites, cell_size) for frame in frames]
    rendered = [frame for frame in rendered if frame is not None]
    if 9 <= len(rendered) <= 16:
        columns = (len(rendered) + 1) // 2
    else:
        columns = min(8, max(1, len(rendered)))
    rows = (len(rendered) + columns - 1) // columns
    sheet = Image.new(
        "RGBA",
        (columns * cell_size[0], rows * cell_size[1]),
        (0, 255, 0, 255),
    )
    for index, frame in enumerate(rendered):
        x = (index % columns) * cell_size[0]
        y = (index // columns) * cell_size[1]
        sheet.alpha_composite(frame, (x, y))

    name = ACTION_NAMES[action_id]
    output_path = output_dir / f"action_{action_id}_{name}_{reference_name}.png"
    sheet.save(output_path)
    return output_path, len(rendered)


def write_action_reference(
    action_id: int,
    frames: list[AirFrame],
    sprites: dict[tuple[int, int], SffSprite],
    output_dir: Path,
) -> dict[str, object]:
    reference_frames = collapse_consecutive_frames(frames)
    output_path, rendered_count = write_reference_sheet(
        action_id,
        "sequence",
        reference_frames,
        sprites,
        output_dir,
    )
    unique_frames, pose_sequence = unique_frames_with_sequence(frames)
    unique_dir = output_dir.parent / "UniquePoseReferences"
    unique_dir.mkdir(parents=True, exist_ok=True)
    unique_path, unique_count = write_reference_sheet(
        action_id,
        "unique",
        unique_frames,
        sprites,
        unique_dir,
    )
    return {
        "action": action_id,
        "name": ACTION_NAMES[action_id],
        "air_frame_count": len(frames),
        "reference_pose_count": rendered_count,
        "unique_pose_count": unique_count,
        "total_positive_ticks": sum(max(0, frame.duration) for frame in frames),
        "durations": [frame.duration for frame in frames],
        "sprites": [f"{frame.group}:{frame.image}" for frame in frames],
        "pose_sequence": pose_sequence,
        "reference": output_path.as_posix(),
        "unique_reference": unique_path.as_posix(),
    }


def command_summary(path: Path) -> list[dict[str, object]]:
    text = read_game_text(path)
    pattern = re.compile(
        r"^\[Command\]\s*(?P<body>.*?)(?=^\[|\Z)",
        re.MULTILINE | re.DOTALL,
    )
    grouped: dict[str, dict[str, object]] = {}
    for match in pattern.finditer(text):
        body = match.group("body")
        name_match = re.search(r'^name\s*=\s*"?([^"\r\n]+)', body, re.MULTILINE)
        command_match = re.search(r"^command\s*=\s*([^\r\n;]+)", body, re.MULTILINE)
        time_match = re.search(r"^time\s*=\s*(\d+)", body, re.MULTILINE)
        if not name_match or not command_match:
            continue
        name = name_match.group(1).strip()
        entry = grouped.setdefault(name, {"name": name, "commands": [], "times": []})
        command = command_match.group(1).strip()
        if command not in entry["commands"]:
            entry["commands"].append(command)
        if time_match:
            time = int(time_match.group(1))
            if time not in entry["times"]:
                entry["times"].append(time)
    return list(grouped.values())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("character_dir", type=Path)
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("Assets/Sprites/Ryu/MugenAudit"),
    )
    parser.add_argument("--palette", default="act/ryu01.act")
    args = parser.parse_args()

    character_dir = args.character_dir.resolve()
    output_dir = args.output.resolve()
    references_dir = output_dir / "ActionReferences"
    references_dir.mkdir(parents=True, exist_ok=True)

    air_path = character_dir / "sf3_ryu.air"
    cmd_path = character_dir / "sf3_ryu.cmd"
    sff_path = character_dir / "sf3_ryu.sff"
    palette_path = character_dir / args.palette

    actions = parse_air(air_path)
    palette = load_act_palette(palette_path)
    sprites = parse_sff_v1(sff_path, palette)

    action_results = []
    missing_actions = []
    for action_id in ACTION_NAMES:
        frames = actions.get(action_id)
        if not frames:
            missing_actions.append(action_id)
            continue
        action_results.append(
            write_action_reference(action_id, frames, sprites, references_dir)
        )

    audit = {
        "source": {
            "character_dir": character_dir.as_posix(),
            "sff_version": "1.0",
            "sprite_count_extracted": len(sprites),
        },
        "commands": command_summary(cmd_path),
        "actions": action_results,
        "missing_actions": missing_actions,
    }
    output_dir.mkdir(parents=True, exist_ok=True)
    audit_path = output_dir / "ryu_mugen_move_audit.json"
    audit_path.write_text(json.dumps(audit, indent=2, ensure_ascii=True), encoding="utf-8")
    print(f"Extracted {len(sprites)} sprites")
    print(f"Wrote {len(action_results)} action references")
    print(audit_path)


if __name__ == "__main__":
    main()
