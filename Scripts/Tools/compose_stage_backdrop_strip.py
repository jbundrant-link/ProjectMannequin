from __future__ import annotations

import argparse
import math
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Feather full-resolution stage sections into one strip."
    )
    parser.add_argument("output", type=Path)
    parser.add_argument("sources", nargs="+", type=Path)
    parser.add_argument("--overlap-fraction", type=float, default=0.14)
    parser.add_argument("--chunk-count", type=int, default=0)
    parser.add_argument("--chunk-prefix", type=Path)
    return parser.parse_args()


def cosine_weight(offset: int, overlap: int) -> float:
    progress = (offset + 0.5) / overlap
    return 0.5 - 0.5 * math.cos(math.pi * progress)


def compose_strip(sources: list[Path], overlap_fraction: float) -> Image.Image:
    if len(sources) < 2:
        raise ValueError("At least two source images are required.")
    if not 0.0 < overlap_fraction < 0.5:
        raise ValueError("Overlap fraction must be between 0 and 0.5.")

    images = [Image.open(path).convert("RGB") for path in sources]
    try:
        width, height = images[0].size
        if any(image.size != (width, height) for image in images[1:]):
            raise ValueError("All source images must have identical dimensions.")

        overlap = round(width * overlap_fraction)
        output_width = width * len(images) - overlap * (len(images) - 1)
        output = Image.new("RGB", (output_width, height))
        output.paste(images[0], (0, 0))

        cursor = width - overlap
        for image in images[1:]:
            existing = output.crop((cursor, 0, cursor + overlap, height))
            incoming = image.crop((0, 0, overlap, height))
            mask = Image.new("L", (overlap, 1))
            mask.putdata([
                round(cosine_weight(offset, overlap) * 255)
                for offset in range(overlap)
            ])
            mask = mask.resize((overlap, height))
            output.paste(Image.composite(incoming, existing, mask), (cursor, 0))
            output.paste(image.crop((overlap, 0, width, height)), (cursor + overlap, 0))
            cursor += width - overlap

        return output
    finally:
        for image in images:
            image.close()


def main() -> None:
    args = parse_args()
    for source in args.sources:
        if not source.is_file():
            raise FileNotFoundError(source)

    output = compose_strip(args.sources, args.overlap_fraction)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    output.save(args.output, optimize=True)
    print(f"saved={args.output} size={output.width}x{output.height}")
    if args.chunk_count:
        if args.chunk_count < 2 or args.chunk_prefix is None:
            raise ValueError(
                "Chunk export requires --chunk-count >= 2 and --chunk-prefix."
            )
        if output.width % args.chunk_count != 0:
            raise ValueError("Output width must divide evenly into chunk count.")
        chunk_width = output.width // args.chunk_count
        args.chunk_prefix.parent.mkdir(parents=True, exist_ok=True)
        for chunk_index in range(args.chunk_count):
            chunk = output.crop(
                (
                    chunk_index * chunk_width,
                    0,
                    (chunk_index + 1) * chunk_width,
                    output.height,
                )
            )
            chunk_path = args.chunk_prefix.with_name(
                f"{args.chunk_prefix.name}_{chunk_index + 1:02}.png"
            )
            chunk.save(chunk_path, optimize=True)
            print(f"saved={chunk_path} size={chunk.width}x{chunk.height}")
            chunk.close()
    output.close()


if __name__ == "__main__":
    main()