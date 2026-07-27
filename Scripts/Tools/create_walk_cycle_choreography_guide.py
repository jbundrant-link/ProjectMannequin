#!/usr/bin/env python3
"""Create an eight-frame walk guide with persistent color-coded limbs."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw


OUTPUT = Path("Artifacts/mannequin_walk_choreography_guide.png")
OUTPUT_A_HALF = Path("Artifacts/mannequin_walk_choreography_a_half.png")
OUTPUT_B_HALF = Path("Artifacts/mannequin_walk_choreography_b_half.png")
WIDTH = 2048
HEIGHT = 2048
COLUMNS = 4
ROWS = 2
CELL_WIDTH = WIDTH // COLUMNS
CELL_HEIGHT = HEIGHT // ROWS
GROUND_Y = 900

BACKGROUND = (248, 248, 244, 255)
INK = (28, 27, 34, 255)
TORSO = (116, 112, 122, 255)
LIMB_A = (246, 184, 48, 255)
LIMB_B = (38, 164, 218, 255)
JOINT = (72, 46, 74, 255)
GROUND = (198, 196, 190, 255)


Point = tuple[int, int]


@dataclass(frozen=True)
class Pose:
    hip: Point
    limb_a_knee: Point
    limb_a_ankle: Point
    limb_a_toe: Point
    limb_b_knee: Point
    limb_b_ankle: Point
    limb_b_toe: Point
    arm_a_hand: Point
    arm_b_hand: Point


POSES = (
    Pose((250, 500), (308, 700), (365, 880), (414, 900), (184, 708), (126, 882), (88, 900), (137, 725), (371, 690)),
    Pose((250, 540), (292, 720), (326, 890), (377, 900), (192, 714), (151, 876), (116, 900), (153, 735), (354, 711)),
    Pose((250, 515), (246, 710), (242, 880), (279, 900), (306, 663), (280, 790), (318, 815), (326, 730), (180, 711)),
    Pose((250, 475), (222, 676), (183, 865), (220, 900), (333, 638), (364, 770), (399, 801), (354, 704), (148, 683)),
    Pose((250, 500), (184, 708), (126, 882), (88, 900), (308, 700), (365, 880), (414, 900), (371, 690), (137, 725)),
    Pose((250, 540), (192, 714), (151, 876), (116, 900), (292, 720), (326, 890), (377, 900), (354, 711), (153, 735)),
    Pose((250, 515), (306, 663), (280, 790), (318, 815), (246, 710), (242, 880), (279, 900), (180, 711), (326, 730)),
    Pose((250, 475), (333, 638), (364, 770), (399, 801), (222, 676), (183, 865), (220, 900), (148, 683), (354, 704)),
)


def offset(point: Point, origin_x: int, origin_y: int) -> Point:
    return point[0] + origin_x, point[1] + origin_y


def draw_limb(
    draw: ImageDraw.ImageDraw,
    points: tuple[Point, ...],
    color: tuple[int, int, int, int],
    origin_x: int,
    origin_y: int,
) -> None:
    absolute = [offset(point, origin_x, origin_y) for point in points]
    draw.line(absolute, fill=INK, width=62, joint="curve")
    draw.line(absolute, fill=color, width=44, joint="curve")
    for point in absolute[:-1]:
        radius = 24
        draw.ellipse(
            (point[0] - radius, point[1] - radius, point[0] + radius, point[1] + radius),
            fill=JOINT,
            outline=INK,
            width=7,
        )


def draw_pose(
    draw: ImageDraw.ImageDraw,
    pose: Pose,
    frame_index: int,
    origin_x: int,
    origin_y: int,
) -> None:
    hip = offset(pose.hip, origin_x, origin_y)
    shoulder = offset((250, pose.hip[1] - 210), origin_x, origin_y)
    head = offset((276, pose.hip[1] - 320), origin_x, origin_y)

    draw.line(
        (
            (origin_x + 36, origin_y + GROUND_Y + 12),
            (origin_x + CELL_WIDTH - 36, origin_y + GROUND_Y + 12),
        ),
        fill=GROUND,
        width=5,
    )

    # Draw the camera-far limb first so overlap remains readable.
    draw_limb(
        draw,
        (pose.hip, pose.limb_b_knee, pose.limb_b_ankle, pose.limb_b_toe),
        LIMB_B,
        origin_x,
        origin_y,
    )
    draw_limb(
        draw,
        (pose.hip, pose.limb_a_knee, pose.limb_a_ankle, pose.limb_a_toe),
        LIMB_A,
        origin_x,
        origin_y,
    )

    draw.line((shoulder, hip), fill=INK, width=112)
    draw.line((shoulder, hip), fill=TORSO, width=92)
    draw.ellipse(
        (head[0] - 64, head[1] - 78, head[0] + 64, head[1] + 78),
        fill=TORSO,
        outline=INK,
        width=10,
    )

    arm_a_shoulder = (218, pose.hip[1] - 200)
    arm_b_shoulder = (282, pose.hip[1] - 200)
    draw_limb(
        draw,
        (arm_b_shoulder, pose.arm_b_hand),
        LIMB_B,
        origin_x,
        origin_y,
    )
    draw_limb(
        draw,
        (arm_a_shoulder, pose.arm_a_hand),
        LIMB_A,
        origin_x,
        origin_y,
    )

    phase = ("CONTACT", "DOWN", "PASSING", "UP")[frame_index % 4]
    lead = "A" if frame_index < 4 else "B"
    draw.text(
        (origin_x + 24, origin_y + 24),
        f"{frame_index + 1}  {lead}-{phase}",
        fill=INK,
        stroke_width=1,
        stroke_fill=BACKGROUND,
    )
    draw.ellipse(
        (origin_x + 25, origin_y + 62, origin_x + 55, origin_y + 92),
        fill=LIMB_A,
        outline=INK,
        width=4,
    )
    draw.text((origin_x + 66, origin_y + 67), "A", fill=INK)
    draw.ellipse(
        (origin_x + 112, origin_y + 62, origin_x + 142, origin_y + 92),
        fill=LIMB_B,
        outline=INK,
        width=4,
    )
    draw.text((origin_x + 153, origin_y + 67), "B", fill=INK)


def save_half_guide(image: Image.Image, source_row: int, output: Path) -> None:
    half = Image.new("RGBA", (WIDTH, HEIGHT), BACKGROUND)
    for index in range(COLUMNS):
        cell = image.crop(
            (
                index * CELL_WIDTH,
                source_row * CELL_HEIGHT,
                (index + 1) * CELL_WIDTH,
                (source_row + 1) * CELL_HEIGHT,
            )
        )
        target_column = index % 2
        target_row = index // 2
        target_x = target_column * (WIDTH // 2) + (WIDTH // 2 - CELL_WIDTH) // 2
        target_y = target_row * (HEIGHT // 2)
        half.alpha_composite(cell, (target_x, target_y))
    half.convert("RGB").save(output)


def main() -> int:
    image = Image.new("RGBA", (WIDTH, HEIGHT), BACKGROUND)
    draw = ImageDraw.Draw(image)
    for index, pose in enumerate(POSES):
        column = index % COLUMNS
        row = index // COLUMNS
        draw_pose(draw, pose, index, column * CELL_WIDTH, row * CELL_HEIGHT)

    for column in range(1, COLUMNS):
        x = column * CELL_WIDTH
        draw.line((x, 0, x, HEIGHT), fill=(220, 218, 212, 255), width=3)
    draw.line((0, CELL_HEIGHT, WIDTH, CELL_HEIGHT), fill=(220, 218, 212, 255), width=3)

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    image.convert("RGB").save(OUTPUT)
    save_half_guide(image, 0, OUTPUT_A_HALF)
    save_half_guide(image, 1, OUTPUT_B_HALF)
    print(f"SAVED {OUTPUT} ({WIDTH}x{HEIGHT})")
    print(f"SAVED {OUTPUT_A_HALF} ({WIDTH}x{HEIGHT})")
    print(f"SAVED {OUTPUT_B_HALF} ({WIDTH}x{HEIGHT})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())