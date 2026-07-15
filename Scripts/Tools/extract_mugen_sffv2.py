#!/usr/bin/env python3
"""Extract selected M.U.G.E.N SFFv2 sprites and align them by their AIR axes."""

from __future__ import annotations

import argparse
import io
import json
import math
import re
import struct
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw


ACTION_PATTERN = re.compile(r"^\s*\[\s*Begin\s+Action\s+(-?\d+)\s*\]", re.I)
SPRITE_PATTERN = re.compile(
    r"^\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*,"
    r"\s*(-?\d+)(?:\s*,\s*([^,\s]*))?"
)


@dataclass
class AirFrame:
    action: int
    group: int
    number: int
    offset_x: int
    offset_y: int
    duration: int
    flip: str


@dataclass
class PaletteHeader:
    group: int
    number: int
    color_count: int
    link: int
    offset: int
    size: int


@dataclass
class SpriteHeader:
    group: int
    number: int
    width: int
    height: int
    axis_x: int
    axis_y: int
    link: int
    data_format: int
    color_depth: int
    offset: int
    size: int
    palette_index: int
    flags: int


def read_exact(stream: io.BufferedReader, size: int) -> bytes:
    data = stream.read(size)
    if len(data) != size:
        raise EOFError(f"Expected {size} bytes, received {len(data)}")
    return data


def parse_actions(air_path: Path, action_ids: set[int]) -> list[AirFrame]:
    frames: list[AirFrame] = []
    current_action: int | None = None
    with air_path.open("r", encoding="utf-8", errors="replace") as stream:
        for raw_line in stream:
            line = raw_line.split(";", 1)[0]
            action_match = ACTION_PATTERN.match(line)
            if action_match:
                current_action = int(action_match.group(1))
                continue
            if current_action not in action_ids:
                continue
            sprite_match = SPRITE_PATTERN.match(line)
            if not sprite_match:
                continue
            frames.append(
                AirFrame(
                    action=current_action,
                    group=int(sprite_match.group(1)),
                    number=int(sprite_match.group(2)),
                    offset_x=int(sprite_match.group(3)),
                    offset_y=int(sprite_match.group(4)),
                    duration=int(sprite_match.group(5)),
                    flip=(sprite_match.group(6) or "").upper(),
                )
            )
    return frames


def decode_rle8(data: bytes, pixel_count: int) -> bytes:
    output = bytearray()
    cursor = 0
    while len(output) < pixel_count and cursor < len(data):
        value = data[cursor]
        cursor += 1
        count = 1
        if value & 0xC0 == 0x40 and cursor < len(data):
            count = value & 0x3F
            value = data[cursor]
            cursor += 1
        output.extend([value] * count)
    return bytes(output[:pixel_count])


def decode_rle5(data: bytes, pixel_count: int) -> bytes:
    output = bytearray()
    cursor = 0
    while len(output) < pixel_count and cursor + 1 < len(data):
        run_length = data[cursor]
        cursor += 1
        data_length = data[cursor] & 0x7F
        color = 0
        if data[cursor] & 0x80:
            cursor += 1
            if cursor >= len(data):
                break
            color = data[cursor]
        cursor += 1
        while True:
            output.append(color)
            run_length -= 1
            if run_length < 0:
                data_length -= 1
                if data_length < 0 or cursor >= len(data):
                    break
                packed = data[cursor]
                cursor += 1
                color = packed & 0x1F
                run_length = packed >> 5
    return bytes(output[:pixel_count])


def decode_lz5(data: bytes, pixel_count: int) -> bytes:
    output = bytearray()
    cursor = 0
    if not data:
        return bytes(output)
    control = data[cursor]
    cursor += 1
    control_shift = 0
    recycle_byte = 0
    recycle_count = 0
    while len(output) < pixel_count and cursor < len(data):
        value = data[cursor]
        cursor += 1
        if control & (1 << control_shift):
            if value & 0x3F == 0:
                if cursor + 1 >= len(data):
                    break
                distance = ((value << 2) | data[cursor]) + 1
                cursor += 1
                count = data[cursor] + 3
                cursor += 1
            else:
                recycle_byte |= (value & 0xC0) >> recycle_count
                recycle_count += 2
                count = (value & 0x3F) + 1
                if recycle_count < 8:
                    if cursor >= len(data):
                        break
                    distance = data[cursor] + 1
                    cursor += 1
                else:
                    distance = recycle_byte + 1
                    recycle_byte = 0
                    recycle_count = 0
            for _ in range(count):
                source = len(output) - distance
                output.append(output[source] if source >= 0 else 0)
                if len(output) >= pixel_count:
                    break
        else:
            if value & 0xE0 == 0:
                if cursor >= len(data):
                    break
                count = data[cursor] + 8
                cursor += 1
                color = value
            else:
                count = value >> 5
                color = value & 0x1F
            output.extend([color] * count)
        control_shift += 1
        if control_shift >= 8:
            if cursor >= len(data):
                break
            control = data[cursor]
            cursor += 1
            control_shift = 0
    return bytes(output[:pixel_count])


class SffV2:
    def __init__(self, path: Path) -> None:
        self.path = path
        self.version = (0, 0, 0, 0)
        self.sprite_headers: list[SpriteHeader] = []
        self.palettes: list[list[tuple[int, int, int, int]]] = []
        self._local_data_offset = 0
        self._translated_data_offset = 0
        self._read_headers()

    def _read_headers(self) -> None:
        with self.path.open("rb") as stream:
            if read_exact(stream, 12) != b"ElecbyteSpr\x00":
                raise ValueError(f"{self.path} is not an Elecbyte SFF file")
            version_bytes = read_exact(stream, 4)
            self.version = tuple(reversed(version_bytes))
            if self.version[0] != 2:
                raise ValueError(f"Only SFFv2 is supported; found {self.version}")
            stream.seek(4, io.SEEK_CUR)
            stream.seek(16, io.SEEK_CUR)
            first_sprite_offset, sprite_count = struct.unpack("<II", read_exact(stream, 8))
            first_palette_offset, palette_count = struct.unpack("<II", read_exact(stream, 8))
            self._local_data_offset = struct.unpack("<I", read_exact(stream, 4))[0]
            stream.seek(4, io.SEEK_CUR)
            self._translated_data_offset = struct.unpack("<I", read_exact(stream, 4))[0]

            palette_headers: list[PaletteHeader] = []
            for index in range(palette_count):
                stream.seek(first_palette_offset + index * 16)
                values = struct.unpack("<HHHHII", read_exact(stream, 16))
                palette_headers.append(PaletteHeader(*values))
            self.palettes = self._read_palettes(stream, palette_headers)

            for index in range(sprite_count):
                stream.seek(first_sprite_offset + index * 28)
                values = struct.unpack("<HHHHhhHBBIIHH", read_exact(stream, 28))
                header = SpriteHeader(*values)
                data_base = (
                    self._translated_data_offset
                    if header.flags & 1
                    else self._local_data_offset
                )
                header.offset += data_base
                self.sprite_headers.append(header)

    def _read_palettes(
        self,
        stream: io.BufferedReader,
        headers: list[PaletteHeader],
    ) -> list[list[tuple[int, int, int, int]]]:
        palettes: list[list[tuple[int, int, int, int]]] = []
        for header in headers:
            if header.size == 0:
                palette = (
                    palettes[header.link]
                    if 0 <= header.link < len(palettes)
                    else [(0, 0, 0, 0)] * 256
                )
                palettes.append(palette)
                continue
            stream.seek(self._local_data_offset + header.offset)
            raw = read_exact(stream, header.size)
            colors: list[tuple[int, int, int, int]] = []
            for offset in range(0, len(raw) - 3, 4):
                red, green, blue, alpha = raw[offset : offset + 4]
                if self.version[2] == 0:
                    alpha = 0 if offset == 0 else 255
                colors.append((red, green, blue, alpha))
            while len(colors) < 256:
                colors.append((0, 0, 0, 0))
            palettes.append(colors[:256])
        return palettes

    def sprite_lookup(self) -> dict[tuple[int, int], int]:
        result: dict[tuple[int, int], int] = {}
        for index, header in enumerate(self.sprite_headers):
            result.setdefault((header.group, header.number), index)
        return result

    def decode_sprite(self, index: int) -> Image.Image:
        header = self.sprite_headers[index]
        if header.size == 0:
            if header.link >= index:
                raise ValueError(f"Invalid linked sprite {index} -> {header.link}")
            linked = self.decode_sprite(header.link)
            return linked.copy()

        with self.path.open("rb") as stream:
            stream.seek(header.offset)
            raw = read_exact(stream, header.size)

        pixel_count = header.width * header.height
        if header.data_format in (10, 11, 12):
            decoded = Image.open(io.BytesIO(raw[4:]))
            decoded.load()
            if header.data_format == 10 and decoded.mode == "P":
                indices = decoded.tobytes()
                return self._indices_to_rgba(indices, header)
            return decoded.convert("RGBA")

        if header.data_format == 0:
            pixels = raw
        elif header.data_format == 2:
            pixels = decode_rle8(raw[4:], pixel_count)
        elif header.data_format == 3:
            pixels = decode_rle5(raw[4:], pixel_count)
        elif header.data_format == 4:
            pixels = decode_lz5(raw[4:], pixel_count)
        else:
            raise ValueError(
                f"Unsupported SFF sprite format {header.data_format} "
                f"for {header.group},{header.number}"
            )

        if header.color_depth <= 8:
            return self._indices_to_rgba(pixels, header)
        if header.color_depth == 24:
            return Image.frombytes(
                "RGB",
                (header.width, header.height),
                pixels[: pixel_count * 3],
            ).convert("RGBA")
        if header.color_depth == 32:
            return Image.frombytes(
                "RGBA",
                (header.width, header.height),
                pixels[: pixel_count * 4],
            )
        raise ValueError(f"Unsupported color depth {header.color_depth}")

    def _indices_to_rgba(
        self,
        indices: bytes,
        header: SpriteHeader,
    ) -> Image.Image:
        palette = (
            self.palettes[header.palette_index]
            if 0 <= header.palette_index < len(self.palettes)
            else [(0, 0, 0, 0)] * 256
        )
        rgba = bytearray()
        for index in indices[: header.width * header.height]:
            rgba.extend(palette[index])
        expected = header.width * header.height * 4
        if len(rgba) < expected:
            rgba.extend(b"\x00" * (expected - len(rgba)))
        return Image.frombytes("RGBA", (header.width, header.height), bytes(rgba))


def save_contact_sheet(
    output_path: Path,
    extracted: list[tuple[AirFrame, SpriteHeader | None, Image.Image | None]],
    cell_width: int,
    cell_height: int,
    clean: bool,
) -> None:
    columns = min(8, max(1, len(extracted)))
    rows = max(1, math.ceil(len(extracted) / columns))
    background = (0, 255, 0, 255) if clean else (18, 32, 48, 255)
    sheet = Image.new("RGBA", (columns * cell_width, rows * cell_height), background)
    draw = ImageDraw.Draw(sheet)
    for index, (air_frame, header, sprite) in enumerate(extracted):
        column = index % columns
        row = index // columns
        cell_x = column * cell_width
        cell_y = row * cell_height
        axis_x = cell_x + cell_width // 2
        axis_y = cell_y + cell_height - 24
        if header is not None and sprite is not None:
            frame_sprite = sprite
            frame_axis_x = header.axis_x
            frame_axis_y = header.axis_y
            if "H" in air_frame.flip:
                frame_sprite = frame_sprite.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
                frame_axis_x = header.width - frame_axis_x
            if "V" in air_frame.flip:
                frame_sprite = frame_sprite.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
                frame_axis_y = header.height - frame_axis_y
            paste_x = axis_x - frame_axis_x + air_frame.offset_x
            paste_y = axis_y - frame_axis_y + air_frame.offset_y
            sheet.alpha_composite(frame_sprite, (paste_x, paste_y))
        if not clean:
            draw.line((axis_x - 5, axis_y, axis_x + 5, axis_y), fill=(255, 80, 80, 210))
            draw.line((axis_x, axis_y - 5, axis_x, axis_y + 5), fill=(255, 80, 80, 210))
            draw.text(
                (cell_x + 5, cell_y + 4),
                f"A{air_frame.action} {air_frame.group},{air_frame.number} "
                f"t={air_frame.duration}",
                fill=(238, 245, 255, 255),
            )
    sheet.save(output_path)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("sff", type=Path)
    parser.add_argument("air", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument(
        "--actions",
        default="0,10,11,20,40,41,47,100,120,200,210,220,230,240,250,260",
        help="Comma-separated AIR action IDs",
    )
    parser.add_argument("--cell-width", type=int, default=192)
    parser.add_argument("--cell-height", type=int, default=192)
    parser.add_argument(
        "--clean-contact",
        action="store_true",
        help="Use a pure-green background with no labels or axis marks",
    )
    args = parser.parse_args()

    action_ids = {int(value.strip()) for value in args.actions.split(",") if value.strip()}
    action_frames = parse_actions(args.air, action_ids)
    archive = SffV2(args.sff)
    lookup = archive.sprite_lookup()
    args.output.mkdir(parents=True, exist_ok=True)

    extracted: list[tuple[AirFrame, SpriteHeader | None, Image.Image | None]] = []
    metadata: list[dict[str, int | str]] = []
    for sequence_index, air_frame in enumerate(action_frames):
        sprite_index = lookup.get((air_frame.group, air_frame.number))
        header = archive.sprite_headers[sprite_index] if sprite_index is not None else None
        image = archive.decode_sprite(sprite_index) if sprite_index is not None else None
        filename = (
            f"{sequence_index:04d}_a{air_frame.action}_"
            f"g{air_frame.group}_n{air_frame.number}.png"
        )
        if image is not None:
            image.save(args.output / filename)
        else:
            filename = ""
        extracted.append((air_frame, header, image))
        metadata.append(
            {
                "file": filename,
                "action": air_frame.action,
                "group": air_frame.group,
                "number": air_frame.number,
                "offset_x": air_frame.offset_x,
                "offset_y": air_frame.offset_y,
                "duration": air_frame.duration,
                "flip": air_frame.flip,
                "axis_x": header.axis_x if header is not None else 0,
                "axis_y": header.axis_y if header is not None else 0,
                "width": header.width if header is not None else 0,
                "height": header.height if header is not None else 0,
            }
        )

    save_contact_sheet(
        args.output / "contact_sheet.png",
        extracted,
        args.cell_width,
        args.cell_height,
        args.clean_contact,
    )
    (args.output / "metadata.json").write_text(
        json.dumps(metadata, indent=2),
        encoding="utf-8",
    )
    print(
        f"Extracted {len(extracted)} AIR frames from {len(action_ids)} actions "
        f"to {args.output}"
    )


if __name__ == "__main__":
    main()
