"""
generate_pixellab.py

Generates Project Mannequin sprite rows through PixelLab, using an existing
reference image when possible.

Preferred source order:
1. PIXELLAB_SOURCE_IMAGE, if set
2. Assets/Sprites/Mannequin/mannequin_reference.png
3. Assets/Sprites/Mannequin/mannequin_sheet.png
4. Valid row*_frame*.png files
5. Assets/Sprites/Concepts/*.{png,jpg,jpeg,webp}
6. PixelLab-generated single-frame reference

For full sheets or concept-board screenshots, install Pillow so the script can
crop a representative seed frame:

    python -m pip install pillow

Useful overrides:
- PIXELLAB_SOURCE_IMAGE="Assets/Sprites/Concepts/example.jpg"
- PIXELLAB_REFERENCE_CROP="x,y,w,h"
- PIXELLAB_REFERENCE_CELL="col,row,columns,rows"
- PIXELLAB_API_KEY="..."
"""

from __future__ import annotations

import base64
import json
import os
import ssl
import struct
import sys
import time
from pathlib import Path
from urllib import error, request


API_KEY = os.getenv("PIXELLAB_API_KEY")
PIXELLAB_IDLE_SOURCE_IMAGE = os.getenv("PIXELLAB_IDLE_SOURCE_IMAGE")
REST_BASE = os.getenv("PIXELLAB_REST_BASE", "https://api.pixellab.ai/v2")
ANIMATE_ENDPOINT = f"{REST_BASE}/animate-with-text-v3"

OUT_DIR = Path("Assets/Sprites/Mannequin")
CONCEPTS_DIR = Path("Assets/Sprites/Concepts")
OUT_DIR.mkdir(parents=True, exist_ok=True)

OUT_PNG = OUT_DIR / "mannequin_sheet.png"
REFERENCE_IMAGE = OUT_DIR / "mannequin_reference.png"
API_REFERENCE_IMAGE = OUT_DIR / "mannequin_reference_256.png"
MIN_VALID_IMAGE_SIZE = 32
MAX_FIRST_FRAME_SIZE = 256
DEFAULT_SHEET_COLUMNS = 10
DEFAULT_SHEET_ROWS = 6

PROMPT = (
    "Original blank humanoid mannequin fighter sprite sheet, retro arcade fighting game style, "
    "tan segmented body plates, dark joints, strong black pixel outline, side-facing 2.5D beat 'em up perspective, "
    "clean transparent background, consistent proportions, game-ready sprite sheet, only the mannequin, no logos, no text, "
    "10 columns x 6 rows, uniform frame size"
)

REFERENCE_PROMPT = (
    "Side-facing retro arcade fighting game humanoid mannequin character, pixel art style, "
    "strong black outline, consistent proportions, clean transparent background, "
    "single frame reference image"
)

ROWS = [
    (
        "idle",
        "8-frame stationary idle stance, both feet firmly planted on the baseline, minimal breathing motion, relaxed arms at the sides",
    ),
    ("walk", "10-frame walking cycle, natural stride, feet contact on the same baseline"),
    ("dash", "10-frame quick dash forward, dynamic low fighting stance, consistent character proportions"),
    ("jump", "10-frame jump takeoff, in-air apex, and landing sequence, consistent silhouette"),
    ("attacks", "10-frame light and medium strike sequence, clear punch extension and follow-through"),
    ("misc", "10-frame hit reaction, form swap pose, knockdown, and death reaction poses"),
]

HEADERS = {
    "Authorization": f"Bearer {API_KEY or ''}",
    "Content-Type": "application/json",
    "Accept": "application/json",
}

CTX = ssl.create_default_context()


def main() -> int:
    print(f"Preparing PixelLab source reference...")
    source = select_reference_source()
    b64_reference = prepare_reference_data_uri(source)

    if not API_KEY:
        print("ERROR: Set PIXELLAB_API_KEY environment variable with your API key.")
        return 1

    print(f"Generating per-row animations via {ANIMATE_ENDPOINT} ...")
    for row_index, (row_name, action_desc) in enumerate(ROWS):
        idle_source = None
        if row_name == "idle" and PIXELLAB_IDLE_SOURCE_IMAGE:
            idle_source = Path(PIXELLAB_IDLE_SOURCE_IMAGE)
            if not idle_source.exists():
                raise SystemExit(f"PIXELLAB_IDLE_SOURCE_IMAGE does not exist: {idle_source}")
            validate_image_file(idle_source)
            print(f"Using idle-specific source image: {idle_source}")

        if idle_source:
            b64_row_reference = prepare_reference_data_uri(idle_source)
        else:
            b64_row_reference = b64_reference

        generate_row(row_index, row_name, action_desc, b64_row_reference)

    print("Generation attempts complete. Check Assets/Sprites/Mannequin/ for outputs.")
    return 0


def select_reference_source() -> Path | None:
    explicit = os.getenv("PIXELLAB_SOURCE_IMAGE")
    if explicit:
        path = Path(explicit)
        if not path.exists():
            raise SystemExit(f"PIXELLAB_SOURCE_IMAGE does not exist: {path}")
        validate_image_file(path)
        print(f"Using explicit source image: {path}")
        return path

    for candidate in [REFERENCE_IMAGE, OUT_PNG]:
        if candidate.exists() and is_valid_image_file(candidate):
            print(f"Using existing source image: {candidate}")
            return candidate

    for row_frame in sorted(OUT_DIR.glob("row*_frame*.png")):
        if is_valid_image_file(row_frame):
            print(f"Using existing generated row frame as source: {row_frame}")
            return row_frame

    concept_candidates: list[Path] = []
    for pattern in ("*.png", "*.jpg", "*.jpeg", "*.webp"):
        concept_candidates.extend(CONCEPTS_DIR.glob(pattern))

    for concept in sorted(concept_candidates):
        if is_valid_image_file(concept):
            print(f"Using concept-board source image: {concept}")
            return concept

    print("No valid local source image found. PixelLab will generate a new single-frame reference.")
    return None


def prepare_reference_data_uri(source: Path | None) -> str:
    if source is None:
        generate_reference_image()
        return reference_to_data_uri(REFERENCE_IMAGE)

    if source.resolve() == REFERENCE_IMAGE.resolve():
        return reference_to_data_uri(source)

    should_crop = (
        "Concepts" in source.parts
        or source.name == OUT_PNG.name
        or source.name.startswith("row") is False
    )

    if should_crop:
        cropped = try_extract_reference_frame(source, REFERENCE_IMAGE)
        if cropped:
            print(f"Extracted reference frame: {REFERENCE_IMAGE}")
            return reference_to_data_uri(REFERENCE_IMAGE)

        if "Concepts" in source.parts or source.name == OUT_PNG.name:
            raise SystemExit(
                "Could not extract a representative frame from the source image.\n"
                "Install Pillow and rerun:\n"
                "  python -m pip install pillow\n"
                "You can also provide an exact crop:\n"
                "  $env:PIXELLAB_REFERENCE_CROP='x,y,w,h'"
            )

    return reference_to_data_uri(source)


def try_extract_reference_frame(source: Path, out_path: Path) -> bool:
    try:
        from PIL import Image
    except ImportError:
        return False

    with Image.open(source) as image:
        image = image.convert("RGBA")
        width, height = image.size
        crop = resolve_crop_box(source, width, height)
        frame = image.crop(crop)
        frame.save(out_path)

    validate_image_file(out_path)
    return True


def reference_to_data_uri(path: Path) -> str:
    api_ready_path = normalize_reference_for_api(path)
    return image_to_data_uri(api_ready_path)


def normalize_reference_for_api(path: Path) -> Path:
    fmt, (width, height) = validate_image_file(path)
    if width <= MAX_FIRST_FRAME_SIZE and height <= MAX_FIRST_FRAME_SIZE:
        return path

    try:
        from PIL import Image
    except ImportError as exc:
        raise SystemExit(
            f"Seed image is {width}x{height}, but PixelLab first_frame requires <= "
            f"{MAX_FIRST_FRAME_SIZE}x{MAX_FIRST_FRAME_SIZE}.\n"
            "Install Pillow so the script can create a normalized seed:\n"
            "  python -m pip install -r Scripts/Tools/requirements.txt"
        ) from exc

    with Image.open(path) as image:
        image = image.convert("RGBA")
        image.thumbnail((MAX_FIRST_FRAME_SIZE, MAX_FIRST_FRAME_SIZE), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (MAX_FIRST_FRAME_SIZE, MAX_FIRST_FRAME_SIZE), (0, 0, 0, 0))
        x = (MAX_FIRST_FRAME_SIZE - image.width) // 2
        y = MAX_FIRST_FRAME_SIZE - image.height
        canvas.alpha_composite(image, (x, y))
        canvas.save(API_REFERENCE_IMAGE)

    _, normalized_dimensions = validate_image_file(API_REFERENCE_IMAGE)
    print(
        f"Normalized PixelLab seed: {API_REFERENCE_IMAGE} "
        f"({width}x{height} {fmt} -> {normalized_dimensions[0]}x{normalized_dimensions[1]} png)"
    )
    return API_REFERENCE_IMAGE


def resolve_crop_box(source: Path, width: int, height: int) -> tuple[int, int, int, int]:
    crop_env = os.getenv("PIXELLAB_REFERENCE_CROP")
    if crop_env:
        x, y, w, h = parse_int_tuple(crop_env, 4, "PIXELLAB_REFERENCE_CROP")
        return clamp_box(x, y, x + w, y + h, width, height)

    cell_env = os.getenv("PIXELLAB_REFERENCE_CELL")
    if cell_env:
        col, row, columns, rows = parse_int_tuple(cell_env, 4, "PIXELLAB_REFERENCE_CELL")
        cell_w = width // max(1, columns)
        cell_h = height // max(1, rows)
        return clamp_box(col * cell_w, row * cell_h, (col + 1) * cell_w, (row + 1) * cell_h, width, height)

    if source.name == OUT_PNG.name:
        cell_w = width // DEFAULT_SHEET_COLUMNS
        cell_h = height // DEFAULT_SHEET_ROWS
        return clamp_box(0, cell_h, cell_w, cell_h * 2, width, height)

    if "Concepts" in source.parts:
        # Default for the current concept-board images: crop the large central
        # side-facing mannequin and let PixelLab use it as a style/proportion seed.
        x = int(width * 0.32)
        y = int(height * 0.10)
        w = int(width * 0.31)
        h = int(height * 0.72)
        return clamp_box(x, y, x + w, y + h, width, height)

    return 0, 0, width, height


def parse_int_tuple(value: str, count: int, name: str) -> tuple[int, ...]:
    parts = [part.strip() for part in value.split(",")]
    if len(parts) != count:
        raise SystemExit(f"{name} must have {count} comma-separated integers.")

    try:
        return tuple(int(part) for part in parts)
    except ValueError as exc:
        raise SystemExit(f"{name} must contain only integers.") from exc


def clamp_box(x0: int, y0: int, x1: int, y1: int, width: int, height: int) -> tuple[int, int, int, int]:
    x0 = max(0, min(width - 1, x0))
    y0 = max(0, min(height - 1, y0))
    x1 = max(x0 + 1, min(width, x1))
    y1 = max(y0 + 1, min(height, y1))
    return x0, y0, x1, y1


def generate_reference_image() -> None:
    print("Generating a reference mannequin frame...")
    ref_resp = post_generate_reference()
    if not isinstance(ref_resp, dict) or "error" in ref_resp:
        raise SystemExit(f"Reference generation failed: {ref_resp}")

    ref_image_b64 = first_image_base64(ref_resp)
    if not ref_image_b64:
        raise SystemExit(f"Could not find reference image in response: {ref_resp}")

    dimensions = save_base64_image(ref_image_b64, REFERENCE_IMAGE)
    print(f"Saved reference image: {REFERENCE_IMAGE} ({dimensions[0]}x{dimensions[1]})")


def generate_row(row_index: int, row_name: str, action_desc: str, b64_reference: str) -> None:
    print(f"Row {row_index}: action='{action_desc}' -> submitting job")

    resp = post_animate_with_text_v3(b64_reference, action_desc, frame_count=8, enhance_prompt=True)
    if not isinstance(resp, dict):
        print(f"Unexpected response type for row {row_index}: {type(resp)}")
        return

    if "error" in resp:
        print(f"Submit error for row {row_index}: {resp.get('error')} - {resp.get('body', '')[:2000]}")
        return

    job_id = resp.get("background_job_id") or resp.get("id") or resp.get("job_id")
    if not job_id:
        print(f"No background job id returned for row {row_index}: keys={list(resp.keys())}")
        return

    print(f"Submitted job {job_id} for row {row_index}, polling...")
    job_result = poll_background_job(job_id, timeout=240, poll_interval=4)
    if not isinstance(job_result, dict):
        print(f"Bad job result for row {row_index}: {job_result}")
        return

    if job_result.get("error"):
        print(f"Job poll error for row {row_index}: {job_result.get('error')}")
        return

    images = extract_image_items(job_result)
    if not images:
        print(f"No images found in completed job for row {row_index}. Job result keys: {list(job_result.keys())}")
        return

    for idx, item in enumerate(images):
        image_b64 = image_item_to_base64(item)
        if not image_b64:
            print(f"Unknown image format for row {row_index} frame {idx}: {item}")
            continue

        out_file = OUT_DIR / f"row{row_index}_{row_name}_frame{idx}.png"
        try:
            dimensions = save_base64_image(image_b64, out_file)
            print(f"Saved {out_file} ({dimensions[0]}x{dimensions[1]})")
        except ValueError as exc:
            print(f"Rejected {out_file}: {exc}")


def post_animate_with_text_v3(first_frame_b64: str, action: str, frame_count: int = 10, enhance_prompt: bool = False):
    body = {
        "first_frame": {"base64": first_frame_b64},
        "action": f"Retro arcade mannequin animation: {action}",
        "frame_count": frame_count,
        "no_background": True,
        "seed": 0,
        "enhance_prompt": enhance_prompt,
    }

    return post_json(ANIMATE_ENDPOINT, body)


def post_generate_reference():
    body = {
        "description": REFERENCE_PROMPT,
        "image_size": {"width": 96, "height": 128},
        "no_background": True,
        "outline": "single color black outline",
        "detail": "medium detail",
        "view": "side",
    }

    return post_json(f"{REST_BASE}/create-image-pixen", body)


def post_json(url: str, body: dict):
    data = json.dumps(body).encode("utf-8")
    req = request.Request(url, data=data, headers=HEADERS, method="POST")
    try:
        with request.urlopen(req, context=CTX, timeout=30) as resp:
            raw = resp.read()
            try:
                return json.loads(raw.decode("utf-8"))
            except Exception:
                return {"raw": raw.decode("utf-8", errors="replace")}
    except error.HTTPError as exc:
        body_text = exc.read().decode("utf-8", errors="replace")
        return {"error": f"HTTP {exc.code}", "body": body_text}
    except Exception as exc:
        return {"error": str(exc)}


def poll_background_job(job_id: str, timeout: int = 180, poll_interval: int = 3):
    job_url = f"{REST_BASE}/background-jobs/{job_id}"
    deadline = time.time() + timeout
    while time.time() < deadline:
        req = request.Request(job_url, headers=HEADERS, method="GET")
        try:
            with request.urlopen(req, context=CTX, timeout=30) as resp:
                resp_json = json.loads(resp.read().decode("utf-8"))
                status = resp_json.get("status")
                if status in {"completed", "failed"}:
                    return resp_json
        except Exception:
            pass

        time.sleep(poll_interval)

    return {"error": "timeout"}


def extract_image_items(job_result: dict) -> list:
    last = job_result.get("last_response") or job_result
    if isinstance(last, dict):
        for key in ("images", "frames", "outputs", "data"):
            if key in last and isinstance(last[key], list):
                return last[key]

    return []


def first_image_base64(payload: dict) -> str | None:
    if "image" in payload:
        return image_item_to_base64(payload["image"])

    if "images" in payload and isinstance(payload["images"], list) and payload["images"]:
        return image_item_to_base64(payload["images"][0])

    data = payload.get("data")
    if isinstance(data, dict) and "image" in data:
        return image_item_to_base64(data["image"])

    return None


def image_item_to_base64(item) -> str | None:
    if isinstance(item, str):
        return item

    if isinstance(item, dict):
        for key in ("b64", "image_base64", "image", "base64", "data"):
            value = item.get(key)
            if isinstance(value, str):
                return value

    return None


def image_to_data_uri(path: Path) -> str:
    fmt, dimensions = validate_image_file(path)
    mime = {"png": "image/png", "jpg": "image/jpeg", "webp": "image/webp"}.get(fmt, "application/octet-stream")
    print(f"Seed image: {path} ({dimensions[0]}x{dimensions[1]}, {fmt})")
    return f"data:{mime};base64," + base64.b64encode(path.read_bytes()).decode("ascii")


def save_base64_image(b64str: str, out_path: Path, min_size: int = MIN_VALID_IMAGE_SIZE) -> tuple[int, int]:
    if b64str.startswith("data:") and ";base64," in b64str:
        b64str = b64str.split(";base64,", 1)[1]

    raw = base64.b64decode(b64str)
    fmt, dimensions = validate_image_bytes(raw, out_path, min_size)
    if fmt not in {"png", "jpg", "webp"}:
        raise ValueError(f"{out_path} decoded to unsupported image type: {fmt}")

    out_path.write_bytes(raw)
    return dimensions


def is_valid_image_file(path: Path) -> bool:
    try:
        validate_image_file(path)
        return True
    except Exception:
        return False


def validate_image_file(path: Path, min_size: int = MIN_VALID_IMAGE_SIZE) -> tuple[str, tuple[int, int]]:
    return validate_image_bytes(path.read_bytes(), path, min_size)


def validate_image_bytes(raw: bytes, label, min_size: int = MIN_VALID_IMAGE_SIZE) -> tuple[str, tuple[int, int]]:
    detected = image_dimensions(raw)
    if detected is None:
        raise ValueError(f"{label} is not a supported PNG/JPEG/WebP image.")

    fmt, (width, height) = detected
    if width < min_size or height < min_size:
        raise ValueError(
            f"{label} decoded to {width}x{height}. "
            f"Rejecting placeholder/sentinel image; expected at least {min_size}x{min_size}."
        )

    return fmt, (width, height)


def image_dimensions(raw: bytes) -> tuple[str, tuple[int, int]] | None:
    if raw[:8] == b"\x89PNG\r\n\x1a\n":
        return "png", struct.unpack(">II", raw[16:24])

    if raw[:2] == b"\xff\xd8":
        return jpeg_dimensions(raw)

    if raw[:4] == b"RIFF" and raw[8:12] == b"WEBP":
        return webp_dimensions(raw)

    return None


def jpeg_dimensions(raw: bytes) -> tuple[str, tuple[int, int]] | None:
    index = 2
    while index < len(raw):
        while index < len(raw) and raw[index] == 0xFF:
            index += 1

        if index >= len(raw):
            return None

        marker = raw[index]
        index += 1

        if marker in (0xD8, 0xD9):
            continue

        if index + 2 > len(raw):
            return None

        segment_length = int.from_bytes(raw[index:index + 2], "big")
        if marker in (0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF):
            height = int.from_bytes(raw[index + 3:index + 5], "big")
            width = int.from_bytes(raw[index + 5:index + 7], "big")
            return "jpg", (width, height)

        index += segment_length

    return None


def webp_dimensions(raw: bytes) -> tuple[str, tuple[int, int]] | None:
    # Minimal VP8X parser for common WebP files.
    if raw[12:16] == b"VP8X" and len(raw) >= 30:
        width = 1 + int.from_bytes(raw[24:27], "little")
        height = 1 + int.from_bytes(raw[27:30], "little")
        return "webp", (width, height)

    return None


if __name__ == "__main__":
    raise SystemExit(main())
