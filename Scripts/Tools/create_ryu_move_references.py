from __future__ import annotations

from pathlib import Path

from pack_ryu_prototype_sprites import FRAME_SIZE, prepare_frame, source_path


OUTPUT_DIR = Path("Assets/Sprites/Ryu/Higgsfield/MoveReferences")
COLUMNS = 5
ROWS = 2
BACKGROUND = (0, 255, 0, 255)

FAMILIES = {
    "movement": (61, 63, 70, 72, 74, 76, 81, 84, 90, 96),
    "normals": (200, 201, 202, 203, 206, 207, 210, 212, 215, 217),
    "crouch_air": (218, 219, 220, 227, 228, 229, 238, 241, 242, 249),
    "crouch": (218, 219, 220, 221, 222, 223, 224, 227, 228, 229),
    "air": (238, 239, 240, 241, 242, 243, 247, 248, 249, 250),
    "specials": (277, 279, 307, 281, 282, 283, 284, 290, 292, 294),
    "hadouken": (277, 278, 279, 300, 301, 302, 307, 308, 309, 313),
    "shoryuken": (281, 282, 283, 284, 285, 315, 316, 317, 318, 319),
    "tatsumaki": (289, 290, 291, 292, 293, 294, 295, 296, 297, 309),
    "reactions": (340, 341, 342, 343, 344, 345, 346, 347, 358, 359),
}


def main() -> int:
    try:
        from PIL import Image
    except ImportError:
        print("ERROR: Pillow is required.")
        return 1

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    for family, indices in FAMILIES.items():
        canvas = Image.new(
            "RGBA",
            (FRAME_SIZE * COLUMNS, FRAME_SIZE * ROWS),
            BACKGROUND,
        )
        for position, source_index in enumerate(indices):
            path = source_path(source_index)
            if not path.exists():
                print(f"ERROR: Missing source frame: {path}")
                return 2

            frame = prepare_frame(path, Image)
            x = position % COLUMNS * FRAME_SIZE
            y = position // COLUMNS * FRAME_SIZE
            canvas.alpha_composite(frame, (x, y))

        output = OUTPUT_DIR / f"ryu_{family}_reference.png"
        canvas.save(output)
        print(f"Saved {output}: {indices}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
