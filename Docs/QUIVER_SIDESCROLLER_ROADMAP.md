# Quiver-Inspired Side-Scroller Roadmap

This roadmap adapts useful stage-flow patterns from the MIT-licensed Quiver
beat-em-up template while preserving Project Mannequin's Godot 4 C# fixed-tick
combat simulation and custom 2.5D combat boxes.

Reference:

- https://github.com/quiver-dev/template-beat-em-up
- Reviewed commit: `ba9542d79b013b6446af1501f74dea3fcde0dcfc`

No Quiver art or other third-party assets are included.

Next roadmap: [Castagne-Inspired Fighting-Game Roadmap](CASTAGNE_FIGHTING_ROADMAP.md).

## Task 1: Fight Rooms And Traversal Gates

Status: Implemented

- Treat every stage encounter as an authored fight room.
- Seal players inside `ArenaMinX` and `ArenaMaxX` during combat.
- Keep enemy entry space outside the room while reinforcements arrive.
- Transition the camera to `CameraLockX` over fixed simulation frames.
- Hold the room through the post-fight intermission.
- Reopen traversal after the configured release delay.
- Validate room bounds, trigger order, camera locks, and timing values.
- Verify room lock/open events and player containment in the scrolling smoke test.

## Task 2: Multi-Wave Room Spawners

Status: Implemented

- Add explicit wave groups inside one fight room.
- Emit wave started, wave cleared, and room cleared events.
- Support delays between waves and optional health/meter rewards.
- Keep deterministic spawn order and maximum active-enemy limits.
- Preserve current edge and lane entry behavior.
- Normalize existing `Spawns` encounters into one runtime wave for compatibility.
- Resolve random pools into runtime copies without mutating mission data.
- Exercise a real two-wave room in the scrolling-stage smoke scenario.

## Task 3: Camera Composition And Party Tethering

Status: Implemented

- Frame all living local players without following enemies.
- Add encounter-specific zoom and transition settings.
- Add soft separation limits and lagging-player catch-up.
- Keep cinematic boss/super focus temporary and recover cleanly.
- Add reduced camera shake rules for three or four players.
- Use frame-rate-independent camera position and zoom smoothing.
- Preserve boss-intro freezes and frame every active boss clone for Classic,
  Isolated Duel, and Tag-Team encounter archetypes.
- Verify two-player framing and deterministic catch-up in a headless smoke test.

## Task 4: Enemy Entrances And Crowd Lanes

Status: Implemented

- Add walk-in, drop-in, foreground, background, and ambush entry profiles.
- Reserve crowd slots around player groups.
- Prevent enemies from stacking on one lane or attacking simultaneously.
- Add offscreen intent indicators for dangerous entries.
- Balance enemy target assignments across living local players.
- Keep attack reservations deterministic and configurable per encounter.
- Verify all entrance profiles, slot uniqueness, warnings, and attacker limits
  in the full scrolling smoke scenario.

## Task 5: Layered Stage Presentation

Status: Implemented

- Replace single stretched backgrounds with authored stage segments.
- Add far, middle, gameplay, and foreground layers.
- Add camera-relative parallax without affecting simulation coordinates.
- Support room-specific lighting and destroyed variants.
- Preserve source-image aspect ratio and disable mipmap blur on stage panels.
- Crop perspective-painted floors from repeated backdrops so the 3D gameplay
  plane remains visually continuous.
- Resize floor and lane geometry when boss phases expand stage bounds.
- Verify Archive District, World Warrior, and Astral compositions with rendered
  frame captures.

## Task 6: Props And Environmental Hazards

Status: Implemented

- Add deterministic breakable props, pickups, and throwable hazards.
- Finish launched-hazard damage against valid targets.
- Add authored hazard zones and readable warnings.
- Keep all interactions inside the custom combat resolver.

## Task 7: Stage Authoring And Debug Tools

Status: Implemented

- Draw room bounds, triggers, camera centers, lanes, and spawn entrances.
- Report invalid or overlapping progression data before play.
- Add encounter and wave controls to the debug overlay.
- Add smoke scenarios for every official stage.

## Task 8: Side-Scroller Acceptance Pass

Status: Implemented

- Complete a full stage with traversal, multiple room shapes, and waves.
- Test one through four local players.
- Tune route length, enemy rhythm, camera movement, and recovery pacing.
- Verify boss handoff, stage completion, and restart behavior.
- Freeze the side-scroller foundation before beginning the Castagne-inspired
  fighting-game tooling pass.
