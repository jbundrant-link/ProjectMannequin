# Reference Implementation Audit

Reviewed on June 30, 2026. These projects are design and architecture references for Project Mannequin. They are not asset packs.

## Sources

### The Simpsons Arcade

Source: https://github.com/jowie94/The-Simpsons-Arcade

- The repository's code is MIT licensed.
- Its README says some graphics came from The Spriters Resource and other graphics and sounds were ripped from the original game.
- We may study or adapt MIT-licensed code patterns, but we will not import those third-party game assets.
- Useful patterns: encounters queued by stage position, camera gates tied to enemy clearance, depth-sorted 2.5D actors, JSON animation pivots, enemy targeting, and collision debug views.
- Project Mannequin equivalent: `ArcadeEncounterDirector` owns travel, waves, intermissions, gates, and boss activation.

### StreetFighter by Mayank Jain

Source: https://github.com/Mayank-Jain-1/StreetFighter

- No license file was present in the reviewed repository, so its code is reference-only.
- Useful patterns: six attack buttons, input history for motion commands, explicit jump startup/air/landing states, held crouch transitions, per-state transition validity, frame-owned combat boxes, hit-stop, layered stage rendering, and camera boundaries.
- Project Mannequin equivalent: six-button bitmask input, one-use buffered commands, standing/crouching/aerial move postures, dash and jump-cancel windows, enum state machine, data-driven boxes, fixed-tick hit-stop, and the shared stage camera.
- Deliberate extension: the reference does not implement a full set of crouching or aerial normals, so Project Mannequin adds six of each and keeps them declarative in `MoveData`.

### street-fighter-game by Alfred Ang

Source: https://github.com/alfredang/street-fighter-game

- No license file was present in the reviewed repository, so its code is reference-only.
- Useful patterns: atlas animation, hit-stop, camera shake, particles, round feedback, facing, and push separation.
- Project Mannequin equivalent: sprite atlas presentation, deterministic simulation pause for hit-stop, presentation-only shake, actor facing, and custom pushboxes.

### godot-tut01

Source: https://github.com/yockgen/godot-tut01

- No license file was present in the reviewed repository, so its code is reference-only.
- Useful patterns: stage resources, weighted enemy pools, stage unlock progression, behavior profiles, boss phases, and layer boundaries.
- Project Mannequin equivalent: world mission data, reusable enemy archetypes, Archive Map progression, data-driven boss phases, and separate simulation/presentation layers.

### MAME X-Men Driver

Source: https://github.com/mamedev/mame/blob/master/src/mame/konami/xmen.cpp

- The source file declares the BSD-3-Clause license.
- It documents original hardware behavior rather than modern gameplay architecture.
- Useful pattern: render tile layers in priority order with sprites interleaved by priority.
- Future Project Mannequin use: split stage art into ordered far background, playable ground, foreground occluders, and actors. The six-player dual-screen hardware path is not relevant to the four-player shared-camera target.

### X-Men Arcade Remake / XMKO

Source: https://gamejolt.com/games/XMKO/994483

- The page does not expose reusable source code or an asset license.
- It is a visual and game-feel reference only.
- Useful patterns to study during playtesting: crowd density, stage cadence, boss framing, readable telegraphs, and how an arcade remake expands the original loop.

## Adopted Direction

- Stage progression is a data-driven sequence of travel, encounter, recovery, and boss-gate phases.
- Enemies arrive in escalating groups before the boss instead of spawning one permanent training target.
- Fighter mechanics remain fixed-tick and frame-driven: six buttons, buffered commands, move states, hitboxes, hurtboxes, hit-stop, and cancels.
- The Archive Knight uses three data-driven phases with distinct move pools and pacing.
- Camera shake and future effects react to simulation events but never change combat results.
- Stage presentation will evolve toward priority-ordered background, ground, actor, effect, and foreground layers.

## Explicitly Rejected

- Importing copyrighted Simpsons, X-Men, Street Fighter, or MUGEN character/stage assets without clear permission.
- Copying code from repositories that do not provide a license.
- Tying combat correctness to animation callbacks, render frame rate, or engine contact physics.
- Reproducing six-player dual-screen hardware behavior when the project target is one to four local players on a shared screen.
- Building the whole campaign as hardcoded scene logic. Worlds, missions, encounters, actors, and moves remain data-driven.
