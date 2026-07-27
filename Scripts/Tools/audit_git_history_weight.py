"""Report what a history rewrite would actually reclaim, per path.

Deleting a file in a new commit does NOT shrink a repository: every earlier
version of the blob stays reachable from earlier commits, so the pack keeps
it and a fresh clone still downloads it. The only operations that reclaim the
space are a history rewrite (git-filter-repo / BFG) or converting the blobs
to LFS pointers with `git lfs migrate import`.

Both are disruptive enough that they should be aimed with measurements rather
than guesses, which is what this produces: total bytes across ALL historical
versions of every blob, grouped by directory, so the biggest offenders are
obvious before anything irreversible happens.

Sizes are uncompressed object sizes. The on-disk pack is smaller because it
is compressed and deltified, but for large PNGs - which barely delta against
each other - the two track closely.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from collections import defaultdict


def run(args: list[str], cwd: str) -> str:
    result = subprocess.run(args, cwd=cwd, capture_output=True, check=True)
    return result.stdout.decode("utf-8", errors="replace")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=".")
    parser.add_argument("--depth", type=int, default=3,
                        help="Directory depth to group by.")
    parser.add_argument("--top", type=int, default=18)
    args = parser.parse_args()

    # Every object reachable from any ref, with the path it was stored under.
    listing = run(["git", "rev-list", "--objects", "--all"], args.root)

    paths: dict[str, str] = {}
    for line in listing.splitlines():
        sha, _, path = line.partition(" ")
        if path:
            paths[sha] = path

    check = subprocess.run(
        ["git", "cat-file", "--batch-check=%(objecttype) %(objectname) %(objectsize)"],
        cwd=args.root, input="\n".join(paths).encode(), capture_output=True,
        check=True).stdout.decode("utf-8", errors="replace")

    by_group: dict[str, list[int]] = defaultdict(lambda: [0, 0])
    total = 0
    for line in check.splitlines():
        parts = line.split()
        if len(parts) != 3 or parts[0] != "blob":
            continue
        sha, size = parts[1], int(parts[2])
        path = paths.get(sha)
        if path is None:
            continue
        segments = path.split("/")
        group = "/".join(segments[:args.depth]) if len(segments) > args.depth \
            else str(path.rsplit("/", 1)[0] if "/" in path else "(repo root)")
        by_group[group][0] += size
        by_group[group][1] += 1
        total += size

    ranked = sorted(by_group.items(), key=lambda kv: -kv[1][0])
    print(f"Total blob bytes across all history: {total / 1048576:,.0f} MB\n")
    print(f"{'directory':58s} {'versions':>9} {'MB':>9}  {'share':>6}")
    for group, (size, count) in ranked[:args.top]:
        share = 100.0 * size / total if total else 0.0
        print(f"{group:58s} {count:9d} {size / 1048576:9.1f}  {share:5.1f}%")

    print("\nReclaiming any of this requires rewriting history, not deleting files:")
    print("  git lfs migrate import --include='<glob>' --everything")
    print("      keeps the files usable, moves their bulk into LFS storage")
    print("  git filter-repo --path <dir> --invert-paths")
    print("      removes them from history entirely")
    print("Both rewrite every later commit hash, so everyone must re-clone.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
