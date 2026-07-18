# Project Mannequin Machine Handoff

Last verified: 2026-07-18

This is the current execution checkpoint for continuing Project Mannequin on
another machine or in a fresh agent session. It does not replace the roadmap or
waive any completion gate.

## Read First

Use these documents in this order:

1. [MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md) - authoritative
   Phase 0-7 product scope and verification contract.
2. [ART_ASSET_COMPLETENESS_PLAN.md](ART_ASSET_COMPLETENESS_PLAN.md) - mandatory
   content and art gates.
3. [VISUAL_STYLE_BIBLE.md](VISUAL_STYLE_BIBLE.md) - visual language, rejection
   rules, generation workflow, and approval rubric.
4. [PROJECT_STATUS.md](PROJECT_STATUS.md) - broad implementation inventory and
   tuning backlog.
5. This file - exact continuation point and machine-transfer instructions.

If any summary in this file becomes stale, the live code/tests and the four
documents above remain authoritative.

## Git Transfer State

The last verified pre-handoff checkpoint was:

- Branch: `main`
- Commit: `f485e4c2f0c663e25158dbcb76a55de965225e3a`
- Remote: `origin` -> `https://github.com/jbundrant-link/ProjectMannequin.git`
- Remote divergence at verification: `0` behind, `0` ahead
- Worktree at verification: clean
- Grappler pilot/source/job files: all 16 tracked and present on `origin/main`

This handoff file and its README/status/plan links were added after that clean
checkpoint. Commit and push those documentation changes before changing
machines:

```powershell
git status --short
git add README.md Docs/PROJECT_HANDOFF.md Docs/PROJECT_STATUS.md Docs/MASTER_IMPLEMENTATION_PLAN.md
git commit -m "docs: add cross-machine project handoff"
git push origin main
```

Do not assume a clean local worktree means another machine can see the latest
work. Confirm that `git rev-list --left-right --count origin/main...HEAD` prints
`0 0` after the final push.

## Verified Project Checkpoint

The active master-plan phase is **Phase 5**.

Current automated baseline:

- World-run assertions: `146/146`
- Stage-hazard assertions: `37/37`
- Aggregate: `183/183`
- Clean runtime evidence exists for approved World Warrior slices at both
  `1280x720` and `1920x1080`.

Completed quality bar:

- Archive Nexus Stages 1-4, base enemies, named elites, Archive Knight combat
  and intro, hazards, props/pickups, layered environments, destruction, HUD,
  Archive Map, results, pause, and form select are runtime approved.
- World Warrior Dojo Approach is `15/16` runtime approved.
- World Warrior Dojo Rookie style-v2 is `16/16` runtime approved.
- World Warrior Pavilion Striker style-v2 is `16/16` runtime approved.

The immediate unfinished slice is the original non-military World Warrior
Tournament Grappler. Pavilion Circuit follows it.

## Grappler Crash Recovery

Nothing from the submitted Grappler generation batch was lost.

Present and tracked:

- Approved `15/16` identity pilot:
  `Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png`
- Seven generated source sheets under
  `Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_*_sheet_style_v1.png`
- Eight completed job records under
  `Artifacts/style_calibration_world_warrior_grappler_*_job.json`
- Source generator:
  `Scripts/Tools/generate_world_warrior_grappler_sources.ps1`
- Legacy rejection evidence under
  `Artifacts/StyleCalibration/world_warrior_grappler_legacy_*`

All eight generation jobs have `completed` status and result URLs. Technical
component extraction found exactly the intended large-pose counts:

| Source | Pose count | Review state |
| --- | ---: | --- |
| Idle | 4 | Usable |
| Walk | 8 | Usable |
| Dash | 6 | Usable |
| Jump | 4 | Usable |
| Shoulder Drive startup/active | 5 | Regenerate choreography |
| Shoulder Drive recovery | 5 | Regenerate choreography |
| Defense/hit/defeat | 8 | Usable |

The identity, costume, mass, locomotion, jump, and defense/defeat work is
coherent. Do not regenerate those groups.

### Attack Review Finding

The two attack sheets are technically extractable and preserve the approved
identity, but they do not yet satisfy the visual gate. They read mostly as
reaching, clasping, and returning to guard. The sequence lacks the required low,
long shoulder-first body-lock drive that distinguishes the Grappler from a
standing punch.

Keep the existing attack sheets and job records as rejected provenance. Produce
targeted v2 attack sources instead of overwriting them, for example:

- `world_warrior_grappler_attack_startup_sheet_style_v2.png`
- `world_warrior_grappler_attack_recovery_sheet_style_v2.png`
- Matching `*_v2_job.json` records

The strongest active pose must show:

- A deep level change
- Lead shoulder clearly ahead of the hips
- Head safely beside the imaginary opponent's torso
- Both arms visibly curved inward as a body lock
- Front knee deeply bent
- Rear leg fully driving
- No target figure, punch, impact effect, speed line, or military gear

## Immediate Continuation Order

1. Generate and visually review only the v2 startup/active and recovery sheets.
   Preserve the approved pilot and the five usable v1 groups.
2. Author
   `Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_animation_sources_v2.json`
   using the Rookie/Striker manifests as schema examples.
3. Compose with `Scripts/Tools/compose_enemy_sheet.py` into
   `Assets/Sprites/Enemies/world_warrior_grappler_style_v2.png`.
4. Require a `2560x2304`, `10x9` real-alpha atlas with 60 populated cells in
   rows 0-5 and transparent rows 6-8. Reject dropped, merged, or clipped poses.
5. Audit per-cell bounds, one scale per animation, grounding, chroma residue,
   prone-frame width, and the atlas preview before runtime wiring.
6. Update `TestRosterFactory.CreateWorldWarriorGrappler()` to use the style-v2
   atlas and all attack columns `40..49`.
7. Rename the displayed move from `Shoulder Throw` to `Shoulder Drive` unless a
   real victim throw is authored. The current move contract is `20` startup,
   `5` active, and `34` recovery frames (`59` total).
8. A candidate ten-pose duration map is `5/5/5/5/5/6/7/7/7/7`: the first four
   poses total 20 startup frames, column 44 owns the five-frame active window,
   and the final five total 34 recovery frames. Treat this as a candidate until
   runtime review, not an approved lock.
9. Add two focused contracts beside the Rookie/Striker checks in
   `Scripts/Debug/WorldRunTests.cs`: atlas path/import/dimensions/layout/no-tint,
   then exact sequence and phase-duration totals.
10. Import the atlas through Godot and rebuild after every C# test edit so the
    engine does not execute a stale DLL.
11. Capture Tournament Grappler idle plus active move frame `22` in real World
    Warrior Stage 3 at `1280x720` and `1920x1080`.
12. Require complete Stage 3 ladder summaries, no runtime errors, readable
    grounding/alpha/identity, and an unmistakable active shoulder drive.
13. Run the expected new aggregate: `148/148` world plus `37/37` hazard equals
    `185/185`. Verify named assertion lines in the log, not only a wrapper exit
    code.
14. Add the `16/16` approval only after runtime review. Record pilot, accepted
    and rejected sources, all job metadata, composition report, hashes, dual
    resolution captures, validation logs, and the legacy partial-supersession
    link in `Docs/VISUAL_STYLE_REVIEW_MANIFEST.json`.
15. Synchronize the new baseline and next action across the master plan, art
    plan, status, style bible, and repository memory. Pavilion Circuit is next.

Current runtime code still points Grappler to
`world_warrior_grappler_higgsfield_v1.png`; no half-written style-v2 catalog or
test promotion exists.

## Remaining Phase 5

### World Warrior

After Grappler, complete these stage-by-stage with the same pilot, provenance,
runtime, and dual-resolution gates:

1. Pavilion Circuit layered environment and floor
2. Grand Tournament Floor layered environment and floor
3. Champion's Courtyard layered environment and floor
4. Unique Dojo Prodigy Kenzo, Pavilion Ace Makoto, and Grand Grappler Tetsu
   animation-ready atlases
5. Training crates/dummies and independent health, meter, and score pickups
6. Rolling training-log and falling-practice-prop hazard art
7. Spectator/crowd layers and reactions
8. Localized tournament-arena destruction states
9. Stage-specific entrances, landmarks, lane rhythm, recovery pockets, GO
   transitions, lighting, music, and set pieces

Do not reuse the rejected tournament composite as three supposedly unique
stages. Do not treat Ryu's personal-use prototype art as redistributable
original roster art.

### Astral Battlefront

World Warrior is followed by the full Astral art-completeness pass:

1. Four distinct stage/floor compositions; route paintings may guide style but
   cannot be silently repeated as complete stage identities
2. Explicit base-enemy runtime style review and any required replacements
3. Unique Saibaman Alpha, Vanguard Commander Lyra, and Ki Captain Prime atlases
4. Capsule caches and independent Astral health, meter, and score pickups
5. Ki-strafe emitter/effects, meteor/debris, energy-current push, pulse-floor
   machinery, and tournament destruction states
6. Per-stage entrances, landmarks, recovery decisions, music/lighting, boss
   escalation, and dual-resolution runtime approval

No fourth-world full production begins until Archive, World Warrior, and Astral
all reach the master plan's console-demo quality gate.

## Work After Phase 5

Phase 6 remains responsible for:

- A separate robust settings store
- Audio/display/render/HUD/shake/flash/control options
- Non-color-only readability and high-contrast telegraphs
- Input glyph switching, reconnect handling, and rebinding
- Versioned atomic saves, migration, backup recovery, and reset confirmation
- Archive Hub meta surfacing, replay access, and bounded assist/difficulty hooks

Phase 7 remains responsible for:

- Cross-project animation and visual-consistency QA
- Stage/boss camera tuning and zero-shake accessibility
- Real 1080p/60 and split-screen performance profiling
- Bounded VFX/text/audio/node pools and transition hitch checks
- Asset-path/import/frame/hash/orphan validation across all content

The subordinate [NEXT_IMPLEMENTATION_PLAN.md](NEXT_IMPLEMENTATION_PLAN.md) also
retains three required tactical tracks: gamepad-as-P1 QA sign-off, one complete
combat-spectacle vertical slice, and a real sequential-boss Hollow Archive.
Iron Fist Foundry remains gated as documented there and in the master plan.

## New Machine Setup

Required runtime tooling:

- Godot 4.7 .NET/Mono edition
- .NET 8 SDK, 64-bit
- Python environment with Pillow, NumPy, and SciPy for atlas composition/audits
- Higgsfield CLI for new generated art

The old machine used hardcoded paths under `C:\Users\Joseph Bundrant`. Those
paths are not portable. On the new machine:

1. Clone the repository and check out `main`.
2. Pull `origin/main` and verify the handoff file exists.
3. Install Godot and set a local `$godot` path in PowerShell commands.
4. Recreate/activate `.venv`; do not transfer the old virtual environment.
5. Install or locate `higgsfield.exe`, then update/localize the `$cli` path in
   generation scripts before running them.
6. Open the Godot editor once headlessly so new PNG resources import.
7. Run `dotnet build .\ProjectMannequin.csproj --no-restore`.
8. Run the focused world/hazard baseline before editing and require
   `146/146 + 37/37` at this checkpoint.

Machine-local Godot logs, progress saves, and environment variables are not
authoritative. Use `PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE=1` for evidence runs
and clear stale `PROJECT_MANNEQUIN_*` variables between scenarios.

## Non-Negotiable Guardrails

- Follow `.github/instructions/project-mannequin-visual-style.instructions.md`.
- Treat mannequin, Ryu, and Goku as rendering anchors, not identities to copy.
- Keep rejected sources and legacy assets as provenance; do not delete or
  silently overwrite them.
- A technically valid atlas is not visually approved.
- Generate one pilot before batching any new family.
- Require at least `14/16`, no automatic failure, explicit review metadata, and
  exact `1280x720` plus `1920x1080` runtime captures.
- Never mark a stage or phase complete while the art-completeness plan still has
  an applicable open gate.
- Do not weaken or remove deterministic tests merely to restore a green count.
- Verify Godot log contents and named assertions; wrappers have previously
  raced file creation or hidden stale-DLL execution.

## Fresh Session Prompt

Use this prompt on the new machine:

```text
Read Docs/PROJECT_HANDOFF.md first, then the master implementation plan, art
asset completeness plan, visual style bible, and repository visual-style
instructions. Continue the active Phase 5 World Warrior art pass from the
Tournament Grappler checkpoint. The pilot and idle/walk/dash/jump/misc v1
sources are accepted and must not be regenerated. Preserve the current v1
attack sheets as rejected provenance; generate only clearer v2 Shoulder Drive
startup/active and recovery sheets, then compose, audit, wire, test, capture at
720p/1080p, and promote the atlas only if all gates pass. The starting baseline
must be 146/146 world plus 37/37 hazard, and the expected post-promotion baseline
is 148/148 plus 37/37. Continue through Pavilion Circuit after Grappler.
```
