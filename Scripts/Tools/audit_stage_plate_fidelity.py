from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Compare full-frame source plates with clean runtime captures."
    )
    parser.add_argument(
        "--pair",
        action="append",
        nargs=4,
        metavar=("LABEL", "SOURCE", "CAPTURE", "FLIP_H"),
        required=True,
    )
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--max-mae", type=float, default=4.0)
    parser.add_argument("--min-correlation", type=float, default=0.99)
    return parser.parse_args()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def parse_bool(value: str) -> bool:
    normalized = value.strip().lower()
    if normalized in {"1", "true", "yes"}:
        return True
    if normalized in {"0", "false", "no"}:
        return False
    raise ValueError(f"Invalid boolean value: {value}")


def compare_pair(
    label: str,
    source_path: Path,
    capture_path: Path,
    flip_h: bool,
    max_mae: float,
    min_correlation: float,
) -> dict[str, object]:
    if not source_path.is_file():
        raise FileNotFoundError(source_path)
    if not capture_path.is_file():
        raise FileNotFoundError(capture_path)

    with Image.open(source_path).convert("RGB") as source_image:
        source_size = source_image.size
        if flip_h:
            source_image = source_image.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        with Image.open(capture_path).convert("RGB") as capture_image:
            capture_size = capture_image.size
            expected = source_image.resize(
                capture_size,
                Image.Resampling.BILINEAR,
            )
            expected_pixels = np.asarray(expected, dtype=np.float32)
            capture_pixels = np.asarray(capture_image, dtype=np.float32)

    difference = expected_pixels - capture_pixels
    mae = float(np.mean(np.abs(difference)))
    rmse = float(np.sqrt(np.mean(difference**2)))
    correlation = float(
        np.corrcoef(
            expected_pixels.reshape(-1),
            capture_pixels.reshape(-1),
        )[0, 1]
    )
    source_aspect = source_size[0] / source_size[1]
    capture_aspect = capture_size[0] / capture_size[1]
    passed = (
        abs(source_aspect - capture_aspect) <= 0.001
        and mae <= max_mae
        and correlation >= min_correlation
    )
    return {
        "label": label,
        "source": source_path.as_posix(),
        "capture": capture_path.as_posix(),
        "source_sha256": sha256(source_path),
        "capture_sha256": sha256(capture_path),
        "source_size": list(source_size),
        "capture_size": list(capture_size),
        "flip_h": flip_h,
        "mae": round(mae, 6),
        "rmse": round(rmse, 6),
        "correlation": round(correlation, 9),
        "passed": passed,
    }


def main() -> None:
    args = parse_args()
    results = [
        compare_pair(
            label,
            Path(source),
            Path(capture),
            parse_bool(flip_h),
            args.max_mae,
            args.min_correlation,
        )
        for label, source, capture, flip_h in args.pair
    ]
    report = {
        "max_mae": args.max_mae,
        "min_correlation": args.min_correlation,
        "passed": all(result["passed"] for result in results),
        "results": results,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(report, indent=2) + "\n",
        encoding="utf-8",
    )
    for result in results:
        print(
            f"{result['label']}: passed={result['passed']} "
            f"mae={result['mae']:.4f} "
            f"correlation={result['correlation']:.8f}"
        )
    print(f"report={args.output} passed={report['passed']}")
    if not report["passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()