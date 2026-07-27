#!/usr/bin/env python3
"""Build an enemy atlas composition manifest from a passing source audit."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


TARGET_ROWS = {
    "idle": 0,
    "walk": 1,
    "dash": 2,
    "jump": 3,
    "misc": 5,
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("audit_report", type=Path)
    parser.add_argument("manifest_output", type=Path)
    parser.add_argument("--atlas-output", type=Path, required=True)
    parser.add_argument("--preview-output", type=Path, required=True)
    parser.add_argument("--composition-report", type=Path, required=True)
    parser.add_argument("--attack-name", default="signature_attack")
    return parser.parse_args()


def anchors(family: dict[str, object]) -> list[list[float]]:
    components = family.get("components", [])
    if len(components) != int(family["expected_component_count"]):
        raise ValueError(
            f"{family['name']}: audit does not contain all expected components"
        )
    return [
        [float(component["center"][0]), float(component["center"][1])]
        for component in components
    ]


def source_entry(family: dict[str, object]) -> dict[str, object]:
    return {
        "source": str(family["source"]),
        "anchors": anchors(family),
    }


def main() -> int:
    args = parse_args()
    audit = json.loads(args.audit_report.read_text(encoding="utf-8"))
    if int(audit.get("failed_count", -1)) != 0:
        raise ValueError(f"{args.audit_report}: source audit must pass before composition")

    spec_path = Path(str(audit["spec"]))
    spec = json.loads(spec_path.read_text(encoding="utf-8"))
    families = {
        str(family["name"]): family for family in audit.get("families", [])
    }
    required = set(TARGET_ROWS) | {"attack_startup", "attack_recovery"}
    missing = sorted(required - set(families))
    if missing:
        raise ValueError(f"{args.audit_report}: missing families {missing}")
    if any(not bool(families[name]["passed"]) for name in required):
        raise ValueError(f"{args.audit_report}: every required family must pass")

    animations = [
        {
            "name": name,
            "target_row": target_row,
            **source_entry(families[name]),
        }
        for name, target_row in TARGET_ROWS.items()
        if name != "misc"
    ]
    animations.append(
        {
            "name": args.attack_name,
            "target_row": 4,
            "source_groups": [
                source_entry(families["attack_startup"]),
                source_entry(families["attack_recovery"]),
            ],
        }
    )
    animations.append(
        {
            "name": "defense_hit_defeat",
            "target_row": TARGET_ROWS["misc"],
            **source_entry(families["misc"]),
        }
    )

    manifest = {
        "enemyId": str(audit["enemy_id"]),
        "background": str(spec.get("background", "green")),
        "minimum_component_area": int(
            spec.get("minimum_component_area", 50_000)
        ),
        "output": args.atlas_output.as_posix(),
        "preview": args.preview_output.as_posix(),
        "report": args.composition_report.as_posix(),
        "animations": animations,
    }
    args.manifest_output.parent.mkdir(parents=True, exist_ok=True)
    args.manifest_output.write_text(
        json.dumps(manifest, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"SAVED {args.manifest_output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())