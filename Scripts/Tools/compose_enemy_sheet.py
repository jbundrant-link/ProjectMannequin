"""Composite per-animation sub-sheets into a final 10x9 enemy sprite sheet.

Usage:
    python Scripts/Tools/compose_enemy_sheet.py <enemy_id> [green|magenta]
    python Scripts/Tools/compose_enemy_sheet.py --manifest <manifest.json>

Expects sub-sheets at:
    Assets/Sprites/Enemies/<enemy_id>_src_idle.png      (2 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_walk.png      (4 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_dash.png      (3 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_jump.png      (2 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_attacks.png   (5 cols x 2 rows)
    Assets/Sprites/Enemies/<enemy_id>_src_misc.png      (4 cols x 2 rows)

Sub-sheets are composited into a 10-col x 9-row 2560x2304 sheet at:
    Assets/Sprites/Enemies/<enemy_id>_higgsfield_v1.png

Rows 6-8 (crouch attacks, air attacks, uppercut) are left transparent;
basic enemies never animate those frames.

Manifest mode is intended for style-locked generated sheets whose complete
figures are visually separated but do not always stay inside a mathematical
grid cell. It extracts connected foreground silhouettes from the whole image,
selects them using authored center anchors, preserves one scale per animation,
and fails instead of silently clipping or dropping a figure.
"""
from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path

FRAME = 256
SHEET_COLS = 10
SHEET_ROWS = 9
BASELINE = 248
PAD = 6
MAX_UPSCALE = 1.7
TARGET_FIGURE_HEIGHT = 230
DEFAULT_MIN_COMPONENT_AREA = 5000
ENEMY_DIR = Path("Assets/Sprites/Enemies")

# Each entry: (animation_name, src_cols, src_rows, target_sheet_row)
ANIMATIONS = [
    ("idle",    2, 2, 0),
    ("walk",    4, 2, 1),
    ("dash",    3, 2, 2),
    ("jump",    2, 2, 3),
    ("attacks", 5, 2, 4),
    ("misc",    4, 2, 5),
]


def _threshold(channel, cutoff):
    return channel.point(lambda v, c=cutoff: 255 if v > c else 0)


def _and(*masks):
    from PIL import ImageChops
    result = masks[0]
    for m in masks[1:]:
        result = ImageChops.multiply(result, m)
    return result


def build_alpha(rgb, bg):
    from PIL import ImageChops
    r, g, b = rgb.split()
    if bg == "magenta":
        is_bg = _and(
            _threshold(r, 100), _threshold(b, 100),
            _threshold(ImageChops.subtract(r, g), 25),
            _threshold(ImageChops.subtract(b, g), 25),
        )
    else:
        is_bg = _and(
            _threshold(g, 100),
            _threshold(ImageChops.subtract(g, r), 25),
            _threshold(ImageChops.subtract(g, b), 25),
        )
    return ImageChops.invert(is_bg)


def despill(rgba, bg):
    from PIL import Image, ImageChops
    r, g, b, a = rgba.split()
    if bg == "magenta":
        g10 = g.point(lambda v: min(255, v + 10))
        return Image.merge("RGBA", (ImageChops.darker(r, g10), g, ImageChops.darker(b, g10), a))
    max_rb = ImageChops.lighter(r, b)
    max_rb8 = max_rb.point(lambda v: min(255, v + 8))
    return Image.merge("RGBA", (r, ImageChops.darker(g, max_rb8), b, a))


def extract_grid_frames(path: Path, src_cols: int, src_rows: int, bg: str):
    """Open a sub-sheet, chroma-key it, return list of normalized 256x256 RGBA frames."""
    from PIL import Image
    im = Image.open(path).convert("RGB")
    alpha = build_alpha(im, bg)
    rgba = im.convert("RGBA")
    rgba.putalpha(alpha)
    rgba = despill(rgba, bg)

    W, H = rgba.size
    cell_w = W / src_cols
    cell_h = H / src_rows
    inset = 4

    frames = []
    for row in range(src_rows):
        for col in range(src_cols):
            box = (
                int(col * cell_w) + inset,
                int(row * cell_h) + inset,
                int((col + 1) * cell_w) - inset,
                int((row + 1) * cell_h) - inset,
            )
            if box[2] <= box[0] or box[3] <= box[1]:
                frames.append(Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0)))
                continue
            cell = rgba.crop(box)
            bbox = cell.getbbox()
            out = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
            if bbox:
                fw, fh = bbox[2] - bbox[0], bbox[3] - bbox[1]
                if fw >= 8 and fh >= 8:
                    scale = min((FRAME - 2 * PAD) / fw, (BASELINE - PAD) / fh, MAX_UPSCALE)
                    nw, nh = max(1, int(fw * scale)), max(1, int(fh * scale))
                    fig = cell.crop(bbox).resize(
                        (nw, nh),
                        Image.Resampling.LANCZOS,
                    )
                    ox = (FRAME - nw) // 2
                    oy = BASELINE - nh
                    out.alpha_composite(fig, (ox, oy))
            frames.append(out)
    return frames


def build_soft_alpha(rgb_array, bg: str):
    """Return anti-aliased alpha from chroma dominance without eating cyan."""
    import numpy as np

    values = rgb_array.astype(np.int16)
    r, g, b = values[..., 0], values[..., 1], values[..., 2]
    if bg == "magenta":
        dominance = np.minimum(r - g, b - g)
        background_score = np.minimum(np.minimum(r - 100, b - 100), dominance - 25)
    else:
        dominance = np.minimum(g - r, g - b)
        background_score = np.minimum(g - 100, dominance - 25)

    transition = np.clip(background_score, 0, 48) / 48.0
    return np.rint(255.0 * (1.0 - transition)).astype(np.uint8)


def load_source_rgba(path: Path, bg: str):
    """Load a source with either authored alpha or a chroma-key background."""
    import numpy as np
    from PIL import Image

    source = Image.open(path)
    if bg == "alpha":
        return source.convert("RGBA")

    source_rgb = source.convert("RGB")
    rgba = source_rgb.convert("RGBA")
    rgba.putalpha(Image.fromarray(build_soft_alpha(np.asarray(source_rgb), bg), "L"))
    return despill(rgba, bg)


def extract_connected_components(
    path: Path,
    bg: str,
    minimum_area: int,
):
    """Extract complete generated figures from a whole chroma-key sheet."""
    try:
        import numpy as np
        from scipy import ndimage
    except ImportError as exception:
        raise RuntimeError(
            "Manifest composition requires numpy and scipy. Install "
            "Scripts/Tools/requirements.txt in the selected Python environment."
        ) from exception

    from PIL import Image

    source_rgba = load_source_rgba(path, bg)
    alpha_array = np.asarray(source_rgba.getchannel("A"))
    binary_foreground = alpha_array > 8
    labels, _ = ndimage.label(
        binary_foreground,
        structure=np.ones((3, 3), dtype=np.uint8),
    )

    components = []
    for label_id, slices in enumerate(ndimage.find_objects(labels), 1):
        if slices is None:
            continue
        y_slice, x_slice = slices
        area = int(np.count_nonzero(labels[slices] == label_id))
        if area < minimum_area:
            continue

        x0 = max(0, x_slice.start - 2)
        y0 = max(0, y_slice.start - 2)
        x1 = min(source_rgba.width, x_slice.stop + 2)
        y1 = min(source_rgba.height, y_slice.stop + 2)
        local_labels = labels[y0:y1, x0:x1]
        component_mask = local_labels == label_id
        component_mask = ndimage.binary_dilation(component_mask, iterations=2)
        alpha = np.where(
            component_mask,
            alpha_array[y0:y1, x0:x1],
            0,
        ).astype(np.uint8)
        figure = source_rgba.crop((x0, y0, x1, y1))
        figure.putalpha(Image.fromarray(alpha, "L"))
        alpha_bounds = figure.getchannel("A").getbbox()
        if alpha_bounds is None:
            continue
        figure = figure.crop(alpha_bounds)

        components.append(
            {
                "area": area,
                "center_x": (x_slice.start + x_slice.stop) * 0.5,
                "center_y": (y_slice.start + y_slice.stop) * 0.5,
                "bbox": [x_slice.start, y_slice.start, x_slice.stop, y_slice.stop],
                "image": figure,
            }
        )

    return components


def extract_box_components(
    path: Path,
    bg: str,
    boxes,
    minimum_area: int,
    primary_x_margin: int,
):
    """Extract one complete pose per authored box, preserving detached parts."""
    try:
        import numpy as np
        from scipy import ndimage
    except ImportError as exception:
        raise RuntimeError(
            "Manifest composition requires numpy and scipy. Install "
            "Scripts/Tools/requirements.txt in the selected Python environment."
        ) from exception

    from PIL import Image

    source_rgba = load_source_rgba(path, bg)
    components = []
    for box_index, raw_box in enumerate(boxes):
        if len(raw_box) != 4:
            raise ValueError(f"{path}: box {box_index + 1} must contain four values.")
        x0, y0, x1, y1 = (int(value) for value in raw_box)
        if x0 < 0 or y0 < 0 or x1 > source_rgba.width or y1 > source_rgba.height:
            raise ValueError(
                f"{path}: box {box_index + 1} {raw_box} exceeds "
                f"{source_rgba.width}x{source_rgba.height}."
            )
        if x1 <= x0 or y1 <= y0:
            raise ValueError(f"{path}: box {box_index + 1} is empty: {raw_box}.")

        crop = source_rgba.crop((x0, y0, x1, y1))
        alpha_array = np.asarray(crop.getchannel("A"))
        labels, _ = ndimage.label(
            alpha_array > 8,
            structure=np.ones((3, 3), dtype=np.uint8),
        )
        retained_labels = []
        primary_candidates = []
        for label_id, slices in enumerate(ndimage.find_objects(labels), 1):
            if slices is None:
                continue
            area = int(np.count_nonzero(labels[slices] == label_id))
            if area < minimum_area:
                continue
            retained_labels.append(label_id)
            _, x_slice = slices
            center_x = (x_slice.start + x_slice.stop) * 0.5
            if primary_x_margin <= center_x <= crop.width - primary_x_margin:
                primary_candidates.append((area, label_id))

        if not retained_labels:
            raise ValueError(
                f"{path}: box {box_index + 1} contains no component "
                f"at least {minimum_area}px."
            )
        if primary_x_margin > 0 and not primary_candidates:
            raise ValueError(
                f"{path}: box {box_index + 1} has no primary component "
                f"inside its {primary_x_margin}px horizontal margins."
            )

        retained_mask = np.isin(labels, retained_labels)
        kept_alpha = np.where(retained_mask, alpha_array, 0).astype(np.uint8)
        figure = crop.copy()
        figure.putalpha(Image.fromarray(kept_alpha, "L"))
        alpha_bounds = figure.getchannel("A").getbbox()
        if alpha_bounds is None:
            raise ValueError(f"{path}: box {box_index + 1} became transparent.")
        figure = figure.crop(alpha_bounds)
        area = int(np.count_nonzero(kept_alpha))
        components.append(
            {
                "area": area,
                "center_x": x0 + (alpha_bounds[0] + alpha_bounds[2]) * 0.5,
                "center_y": y0 + (alpha_bounds[1] + alpha_bounds[3]) * 0.5,
                "bbox": [x0, y0, x1, y1],
                "image": figure,
            }
        )

    return components


def select_components(components, anchors, source_name: str):
    if len(components) < len(anchors):
        raise ValueError(
            f"{source_name}: found {len(components)} components for "
            f"{len(anchors)} authored anchors."
        )
    selected = []
    available = set(range(len(components)))
    for anchor_index, anchor in enumerate(anchors):
        anchor_x, anchor_y = float(anchor[0]), float(anchor[1])
        component_index = min(
            available,
            key=lambda index, x=anchor_x, y=anchor_y: (
                (components[index]["center_x"] - x) ** 2
                + (components[index]["center_y"] - y) ** 2
            ),
        )
        component = components[component_index]
        distance = (
            (component["center_x"] - anchor_x) ** 2
            + (component["center_y"] - anchor_y) ** 2
        ) ** 0.5
        if distance > 220.0:
            raise ValueError(
                f"{source_name}: anchor {anchor_index + 1} at {anchor} "
                f"is {distance:.1f}px from the nearest unused figure."
            )
        selected.append(component)
        available.remove(component_index)
    return selected


def normalize_component_frames(components):
    """Normalize one animation at a single scale to prevent size flicker."""
    from PIL import Image

    maximum_width = max(component["image"].width for component in components)
    maximum_height = max(component["image"].height for component in components)
    scale = min(
        (FRAME - 2 * PAD) / maximum_width,
        TARGET_FIGURE_HEIGHT / maximum_height,
        MAX_UPSCALE,
    )

    normalized = []
    for component in components:
        figure = component["image"]
        width = max(1, round(figure.width * scale))
        height = max(1, round(figure.height * scale))
        figure = figure.resize((width, height), Image.Resampling.LANCZOS)
        component_background = component.get("background")
        if component_background in ("green", "magenta"):
            figure = despill(figure, component_background)
        frame = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
        frame.alpha_composite(
            figure,
            ((FRAME - width) // 2, BASELINE - height),
        )
        normalized.append(frame)

    return normalized, scale


def write_preview(sheet, path: Path, used_rows: int = 6):
    from PIL import Image, ImageDraw

    preview_frame = 128
    checker_size = 16
    canvas = Image.new(
        "RGBA",
        (preview_frame * SHEET_COLS, preview_frame * used_rows),
        (40, 40, 48, 255),
    )
    draw = ImageDraw.Draw(canvas)
    for row in range(used_rows):
        for col in range(SHEET_COLS):
            x0 = col * preview_frame
            y0 = row * preview_frame
            for y in range(0, preview_frame, checker_size):
                for x in range(0, preview_frame, checker_size):
                    color = (70, 70, 80, 255) if (x // checker_size + y // checker_size) % 2 == 0 else (48, 48, 58, 255)
                    draw.rectangle(
                        (x0 + x, y0 + y, x0 + x + checker_size, y0 + y + checker_size),
                        fill=color,
                    )
            frame = sheet.crop(
                (
                    col * FRAME,
                    row * FRAME,
                    (col + 1) * FRAME,
                    (row + 1) * FRAME,
                )
            ).resize((preview_frame, preview_frame), Image.Resampling.LANCZOS)
            canvas.alpha_composite(frame, (x0, y0))
    path.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(path)


def compose_manifest(manifest_path: Path, output_override: Path | None = None) -> int:
    from PIL import Image

    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    background = manifest.get("background", "green")
    minimum_area = int(
        manifest.get("minimum_component_area", DEFAULT_MIN_COMPONENT_AREA)
    )
    sheet = Image.new(
        "RGBA",
        (FRAME * SHEET_COLS, FRAME * SHEET_ROWS),
        (0, 0, 0, 0),
    )
    report = {
        "manifest": str(manifest_path).replace("\\", "/"),
        "output": str(output_override or Path(manifest["output"])).replace("\\", "/"),
        "rows": [],
    }

    for animation in manifest["animations"]:
        target_row = int(animation["target_row"])
        if target_row < 0 or target_row >= SHEET_ROWS:
            raise ValueError(f"Invalid target row {target_row}.")

        groups = animation.get("source_groups") or [animation]
        selected = []
        group_reports = []
        discovered_total = 0
        for group in groups:
            source_path = Path(group["source"])
            if not source_path.exists():
                raise FileNotFoundError(f"Missing animation source: {source_path}")
            group_background = group.get("background", background)
            group_minimum_area = int(group.get("minimum_component_area", minimum_area))

            if boxes := group.get("boxes"):
                group_components = extract_box_components(
                    source_path,
                    group_background,
                    boxes,
                    group_minimum_area,
                    int(group.get("primary_x_margin", 0)),
                )
                group_selected = group_components
            else:
                anchors = group.get("anchors")
                if not anchors:
                    raise ValueError(
                        f"{source_path}: expected authored anchors or boxes."
                    )
                group_components = extract_connected_components(
                    source_path,
                    group_background,
                    group_minimum_area,
                )
                group_selected = select_components(
                    group_components,
                    anchors,
                    str(source_path),
                )

            discovered_total += len(group_components)
            for component in group_selected:
                component["background"] = group_background
            selected.extend(group_selected)
            group_reports.append(
                {
                    "source": str(source_path).replace("\\", "/"),
                    "background": group_background,
                    "discovered_components": len(group_components),
                    "selected_components": len(group_selected),
                    "source_bboxes": [
                        component["bbox"] for component in group_selected
                    ],
                }
            )

        if not selected or len(selected) > SHEET_COLS:
            raise ValueError(
                f"{animation['name']}: expected 1..{SHEET_COLS} selected poses, "
                f"found {len(selected)}."
            )
        frames, scale = normalize_component_frames(selected)
        for column in range(SHEET_COLS):
            frame = frames[min(column, len(frames) - 1)]
            sheet.alpha_composite(frame, (column * FRAME, target_row * FRAME))

        row_report = {
            "name": animation["name"],
            "target_row": target_row,
            "discovered_components": discovered_total,
            "selected_components": len(selected),
            "scale": round(scale, 6),
            "source_bboxes": [component["bbox"] for component in selected],
        }
        if len(group_reports) == 1:
            row_report["source"] = group_reports[0]["source"]
        if animation.get("source_groups"):
            row_report["source_groups"] = group_reports
        report["rows"].append(row_report)
        print(
            f"  row {target_row} ({animation['name']}): "
            f"selected={len(selected)} discovered={discovered_total} scale={scale:.4f}"
        )

    output_path = output_override or Path(manifest["output"])
    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path)
    report["sha256"] = hashlib.sha256(output_path.read_bytes()).hexdigest()
    report["size"] = [sheet.width, sheet.height]
    report["nonempty_frames"] = sum(
        1
        for row in range(SHEET_ROWS)
        for column in range(SHEET_COLS)
        if sheet.crop(
            (
                column * FRAME,
                row * FRAME,
                (column + 1) * FRAME,
                (row + 1) * FRAME,
            )
        ).getbbox()
        is not None
    )

    if output_override is None and (report_path_value := manifest.get("report")):
        report_path = Path(report_path_value)
        report_path.parent.mkdir(parents=True, exist_ok=True)
        report_path.write_text(
            json.dumps(report, indent=2) + "\n",
            encoding="utf-8",
        )
    if output_override is None and (preview_path_value := manifest.get("preview")):
        write_preview(sheet, Path(preview_path_value))

    print(
        f"SAVED {output_path} ({sheet.width}x{sheet.height}) "
        f"nonempty={report['nonempty_frames']} sha256={report['sha256']}"
    )
    return 0


def compose_legacy(enemy_id: str, bg: str) -> int:
    from PIL import Image

    sheet = Image.new("RGBA", (FRAME * SHEET_COLS, FRAME * SHEET_ROWS), (0, 0, 0, 0))
    filled_total = 0

    for anim_name, src_cols, src_rows, target_row in ANIMATIONS:
        src_path = ENEMY_DIR / f"{enemy_id}_src_{anim_name}.png"
        if not src_path.exists():
            print(f"  SKIP {anim_name} (not found: {src_path})")
            continue

        frames = extract_grid_frames(src_path, src_cols, src_rows, bg)
        filled = sum(1 for f in frames if f.getbbox() is not None)

        for col in range(SHEET_COLS):
            frame = frames[min(col, len(frames) - 1)].copy()
            sheet.alpha_composite(frame, (col * FRAME, target_row * FRAME))

        filled_total += filled
        total_cells = src_cols * src_rows
        print(f"  row {target_row} ({anim_name}): {filled}/{total_cells} frames")

    out_path = ENEMY_DIR / f"{enemy_id}_higgsfield_v1.png"
    sheet.save(out_path)
    print(f"SAVED {out_path} ({sheet.width}x{sheet.height}) filled={filled_total}")
    return 0


def main() -> int:
    if len(sys.argv) < 2:
        print(
            "usage: compose_enemy_sheet.py <enemy_id> [green|magenta]\n"
            "       compose_enemy_sheet.py --manifest <manifest.json>"
        )
        return 2

    if sys.argv[1] == "--manifest":
        if len(sys.argv) < 3:
            print("ERROR: --manifest requires a JSON path")
            return 2
        output_override = None
        if len(sys.argv) > 3:
            if len(sys.argv) != 5 or sys.argv[3] != "--output":
                print("ERROR: optional manifest syntax is --output <path>")
                return 2
            output_override = Path(sys.argv[4])
        return compose_manifest(Path(sys.argv[2]), output_override)

    enemy_id = sys.argv[1]
    background = sys.argv[2] if len(sys.argv) > 2 else "green"
    return compose_legacy(enemy_id, background)


if __name__ == "__main__":
    raise SystemExit(main())
