"""Classify image assets by what actually references them.

Answers a question that decides whether an asset is worth committing at all:
is it loaded by the game, is it only an input to a generation script, or is
nothing referring to it any more?

Reference detection tokenises every text file into identifier-like words and
looks for the asset's STEM, not its full name. That matters because the
codebase cites these files three different ways - a "res://" path in C#, a
relative path in a PowerShell generation script, and a BARE STEM with no
extension inside a review-manifest entry. An earlier version of this audit
matched only names ending in .png and therefore reported manifest-referenced
pilot art as unreferenced, which would have argued for deleting it.

The bias is deliberately toward calling a file USED: a false "used" only
costs disk, while a false "orphan" could delete irreplaceable art.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

# Anything named here is a place a reference can live. Ordered most to least
# authoritative, because a file is classified by its strongest referrer.
RUNTIME_DIRS = ("Scripts/Data", "Scripts/Core", "Scripts/Presentation",
                "Scripts/Stage", "Scripts/Combat", "Scripts/UI",
                "Scripts/Progression", "Scripts/Input", "Scripts/Settings",
                "Scenes")
BUILD_DIRS = ("Scripts/Tools",)
EVIDENCE_DIRS = ("Docs", "Artifacts")

NAME_PATTERN = re.compile(r"[A-Za-z0-9_\-]+")

TEXT_SUFFIXES = {".cs", ".tscn", ".godot", ".ps1", ".py", ".json", ".md",
                 ".tres", ".cfg", ".import", ".uid", ".csproj"}


def classify_referrer(path: str) -> str:
    normalized = path.replace("\\", "/")
    if normalized.endswith(".import"):
        # An .import file always names its own source, which would make every
        # asset look referenced. It proves Godot imported the file, not that
        # anything uses it.
        return "import-sidecar"
    for prefix in RUNTIME_DIRS:
        if normalized.startswith(prefix):
            return "runtime"
    for prefix in BUILD_DIRS:
        if normalized.startswith(prefix):
            return "build"
    for prefix in EVIDENCE_DIRS:
        if normalized.startswith(prefix):
            return "evidence"
    if normalized == "project.godot":
        return "runtime"
    return "other"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--targets", required=True,
                        help="File listing the asset paths to classify.")
    parser.add_argument("--root", default=".")
    parser.add_argument("--report", default="Artifacts/asset_usage_report.json")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    targets = [line.strip() for line in
               Path(args.targets).read_text(encoding="utf-8").splitlines()
               if line.strip()]
    if not targets:
        print("no target assets supplied")
        return 1

    corpus = subprocess.run(
        ["git", "ls-files", "--cached", "--others", "--exclude-standard"],
        cwd=root, capture_output=True, text=True, check=True).stdout.splitlines()

    # name -> {category -> [referrer, ...]}
    references: dict[str, dict[str, list[str]]] = defaultdict(
        lambda: defaultdict(list))

    for relative in corpus:
        path = root / relative
        if path.suffix.lower() not in TEXT_SUFFIXES:
            continue
        category = classify_referrer(relative)
        if category == "import-sidecar":
            continue
        try:
            text = path.read_text(encoding="utf-8", errors="ignore")
        except OSError:
            continue
        # Tokenising rather than regex-matching filenames is what lets a bare
        # stem in a manifest count as a reference.
        for token in set(NAME_PATTERN.findall(text.lower())):
            bucket = references[token][category]
            if len(bucket) < 3:
                bucket.append(relative)

    buckets: dict[str, list[dict]] = defaultdict(list)
    for relative in targets:
        name = Path(relative).stem.lower()
        found = references.get(name, {})
        size = 0
        absolute = root / relative
        if absolute.exists():
            size = absolute.stat().st_size
        for category in ("runtime", "build", "evidence", "other"):
            if found.get(category):
                verdict = category
                break
        else:
            verdict = "orphan"
        buckets[verdict].append({
            "path": relative,
            "bytes": size,
            "referrers": {k: v for k, v in found.items()},
        })

    order = ("runtime", "build", "evidence", "other", "orphan")
    labels = {
        "runtime": "LOADED BY THE GAME        ",
        "build": "generation-script input   ",
        "evidence": "referenced only by docs   ",
        "other": "referenced elsewhere      ",
        "orphan": "NOTHING REFERENCES IT     ",
    }
    print(f"{len(targets)} asset(s) classified\n")
    total_orphan = 0
    for key in order:
        entries = buckets.get(key, [])
        if not entries:
            continue
        megabytes = sum(e["bytes"] for e in entries) / (1024 * 1024)
        print(f"  {labels[key]} {len(entries):4d} files   {megabytes:9.1f} MB")
        if key == "orphan":
            total_orphan = megabytes

    Path(args.report).parent.mkdir(parents=True, exist_ok=True)
    Path(args.report).write_text(
        json.dumps({k: buckets.get(k, []) for k in order}, indent=2),
        encoding="utf-8")
    print(f"\nreport={args.report}")
    if total_orphan:
        print(f"reclaimable if orphans are deleted: {total_orphan:.1f} MB")
    return 0


if __name__ == "__main__":
    sys.exit(main())
