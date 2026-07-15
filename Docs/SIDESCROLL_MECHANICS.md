# Arcade Sidescroll Mechanics

This document records the pre-boss stage mechanics adapted from the MIT-licensed code in:

https://github.com/jowie94/The-Simpsons-Arcade

Reference commit reviewed: `61ec495fb035024cfef1af11493f3a3de2756036`.

No Simpsons graphics, sounds, character data, or other third-party assets are used by Project Mannequin.

## Reference Behavior

### Route and Camera

`FirstScene.cpp` uses a `1663` pixel stage against a `288` pixel viewport, producing approximately `5.8` screens of horizontal travel.

The camera:

- follows the midpoint of living players, not enemies
- begins moving when the player midpoint reaches approximately screen X `138`
- advances at a fixed rate while progression allows it
- does not permit players to fall behind the camera
- updates player movement limits from the current camera bounds

This keeps players moving through the frame while enemies enter independently.

### Progression Queue

Enemy piles are queued at camera positions `0`, `300`, `600`, `980`, `1000`, and `1300`.

When the camera reaches a queued position, that pile becomes active. Spawn positions mix:

- the current route position
- approximately one viewport beyond the right side
- positions behind the group
- far and near depth lanes

Camera advancement is limited by an enemy-clearance ratio. The implementation is rough, but the design result is clear: combat gates forward movement without turning the stage into one static arena.

### Enemy Rhythm

The Royd enemy does not continuously occupy the player's center.

Its cycle is:

1. Select a living player target.
2. Approach using normalized horizontal and depth movement.
3. Change walk presentation based on depth direction.
4. Enter attack when its feet collider touches the player.
5. Finish the attack animation.
6. Retreat away from the player until horizontal separation is restored.
7. Re-enter the approach cycle.

The retreat after every attack, including a missed attack, is the important crowd-spacing rule.

### Depth and Collision

- X is horizontal route movement.
- Y is jump height.
- Z is the beat-em-up lane.
- Collision requires close Z distance in addition to X/Y overlap.
- Foreground sprites are sorted by Z so lower characters draw in front.
- Players are clamped to camera and lane bounds.

## Project Mannequin Adaptation

Project Mannequin preserves those principles while keeping its fixed-tick combat architecture.

### Route

- `102` world-unit route, approximately six camera widths.
- Six pre-boss horde sections.
- Seventeen pre-boss enemies.
- A separate final travel section leading to the existing boss gate.

### Camera

- Tracks living players only.
- Uses a forward dead zone instead of centering the player continuously.
- Advances at a fixed simulation speed.
- Stops at the current encounter's camera gate.
- Keeps players inside screen-relative left and right movement margins.
- Never zooms out merely because an enemy is waiting offscreen.

### Enemy Entrances

Each spawn declares:

- entry edge: left, right, far lane, or near lane
- delay in fixed simulation frames
- entry distance
- starting lane

Encounters limit simultaneously active enemies. Reinforcements remain queued until their delay and active-count rules permit entry.

### Enemy Crowd AI

Each enemy receives an attack slot around the current target.

The shared loop is:

1. Enter from the configured stage edge.
2. Move into an assigned horizontal and depth slot.
3. Wait through a per-enemy attack stagger.
4. Perform a legal data-driven combat move.
5. Retreat after attack recovery.
6. Hold reset distance for a role-specific delay.
7. Re-enter from its slot.

Role tuning:

- Archive Scout: fast approach, short retreat, quick re-entry.
- Archive Raider: balanced spacing and recovery.
- Archive Bruiser: slower approach, larger retreat, longer reset.

All attacks still use normal startup, active, recovery, hitboxes, hit-stop, damage, and interruption rules.

## Deliberate Differences

- Deterministic fixed-tick behavior replaces `rand()`-driven decisions.
- Explicit attack slots reduce stacking better than collision contact alone.
- Spawn timing and entry edges are declarative mission data.
- Custom AABB combat boxes remain independent from Godot physics contacts.
- Different archetypes have distinct pacing instead of sharing one enemy implementation.
- The existing Archive Knight boss is not redesigned in this stage pass.
