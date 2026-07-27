from __future__ import annotations

import argparse
import math
from pathlib import Path

from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Pad an image to an exact aspect ratio without cropping."
    )
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--aspect-width", type=int, default=16)
    parser.add_argument("--aspect-height", type=int, default=9)
    return parser.parse_args()


def smallest_target_size(
    width: int,
    height: int,
    aspect_width: int,
    aspect_height: int,
) -> tuple[int, int]:
    multiplier = math.ceil(
        max(width / aspect_width, height / aspect_height)
    )
    return aspect_width * multiplier, aspect_height * multiplier


def extend_edges(image: Image.Image, target_width: int, target_height: int) -> Image.Image:
    left = (target_width - image.width) // 2
    top = (target_height - image.height) // 2
    right = target_width - image.width - left
    bottom = target_height - image.height - top
    output = Image.new("RGB", (target_width, target_height))
    output.paste(image, (left, top))

    if left:
        edge = image.crop((0, 0, 1, image.height)).resize((left, image.height))
        output.paste(edge, (0, top))
        edge.close()
    if right:
        edge = image.crop(
            (image.width - 1, 0, image.width, image.height)
        ).resize((right, image.height))
        output.paste(edge, (left + image.width, top))
        edge.close()
    if top:
        edge = output.crop((0, top, target_width, top + 1)).resize(
            (target_width, top)
        )
        output.paste(edge, (0, 0))
        edge.close()
    if bottom:
        edge = output.crop(
            (0, top + image.height - 1, target_width, top + image.height)
        ).resize((target_width, bottom))
        output.paste(edge, (0, top + image.height))
        edge.close()

    return output


def main() -> None:
    args = parse_args()
    if not args.source.is_file():
        raise FileNotFoundError(args.source)
    if args.aspect_width <= 0 or args.aspect_height <= 0:
        raise ValueError("Aspect dimensions must be positive.")

    with Image.open(args.source).convert("RGB") as source:
        target_width, target_height = smallest_target_size(
            source.width,
            source.height,
            args.aspect_width,
            args.aspect_height,
        )
        output = extend_edges(source, target_width, target_height)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    output.save(args.output, optimize=True)
    print(
        f"saved={args.output} source={source.width}x{source.height} "
        f"target={output.width}x{output.height}"
    )
    output.close()


if __name__ == "__main__":
    main()