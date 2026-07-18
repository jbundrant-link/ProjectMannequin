# Project Mannequin Status

This document tracks progress against the original MVP prompt.

## Current Build

The project is currently a playable arcade combat MVP with a startup Archive Map, a pause menu, three selectable six-screen missions, fixed-tick combat logic, custom combat boxes, six-button ground/crouch/air combat, mixed guard rules, parries, counters, combo scaling, projectile attacks, CPU fighter bosses, gameplay HUD, damage numbers, debug overlays, saved boss-form unlocks, shapeshift completion, and sprite-sheet presentation support.

The canonical Phase 0-7 product scope, locked decisions, completion gates, and
roadmap precedence live in the
[Master Implementation Plan](MASTER_IMPLEMENTATION_PLAN.md). Near-term sequencing
for gamepad-as-P1 QA closure, combat spectacle polish, Hollow Archive expansion,
and the reserved Iron Fist Foundry branch lives in the
[Next Implementation Plan](NEXT_IMPLEMENTATION_PLAN.md); it does not replace the
master plan. The exact cross-machine continuation checkpoint, including the
active Tournament Grappler source review and transfer checklist, lives in the
[Project Handoff](PROJECT_HANDOFF.md).

The playable routes are:

- Archive Nexus / Archive District: six hordes, seventeen enemies, and the three-phase Archive Knight.
- World Warrior Sector / Tournament District: six tournament hordes, seventeen enemies, and a three-phase Ryu boss.
- Astral Battlefront / Shattered Skyway: six mixed aerial-energy hordes and a twelve-phase Goku boss.

## MVP Progress

Done:

- Godot 4.x C# project skeleton.
- Fixed 60 Hz simulation coordinator.
- Local input manager for player slots.
- Bitmask input buffer with 60-frame history.
- Command interpreter for Street Fighter-style six-button notation such as `LP`, `MP`, `HP`, `LK`, `MK`, `HK`, `2HP`, `236LP`, and `236HK`.
- Six core fight buttons: light/medium/heavy punch and light/medium/heavy kick.
- Launcher into air follow-up path: `2HP`, jump, air `LP`, then air `HP`.
- Six crouching normals selected with dedicated crouch plus `LP/MP/HP/LK/MK/HK`; keyboard `C` crouches while `S` remains lane movement.
- Six aerial normals with light-to-medium-to-heavy cancel routes.
- Hit-confirmed launcher jump cancel for grounded-to-air combo routing.
- Timed dash burst with its dedicated sprite row plus attack and jump cancel windows.
- Four-frame jump startup, taller fixed-tick arc, air steering, height-aware camera framing, and landing recovery.
- Buffered attack presses are consumed once so old commands cannot restart or corrupt later combo steps.
- Live combo counter and deterministic mobility/combo smoke scenario.
- Enum-driven combat state machine.
- Data-driven `MoveData` and `CharacterData` structures.
- 2.5D AABB hitbox, hurtbox, and pushbox definitions.
- Hit resolver with duplicate-hit prevention.
- One player mannequin.
- Three reusable enemy archetypes: Archive Scout, Raider, and Bruiser.
- Six gated horde encounters containing seventeen pre-boss enemies.
- A 102-unit route spanning approximately six gameplay camera widths.
- Player-only forward camera with a movement threshold, fixed scroll speed, encounter gates, and screen-relative player limits.
- Staggered reinforcement queues with active-enemy limits and left, right, near-lane, and far-lane entrances.
- Role-specific arcade enemy brains with target-relative attack slots, depth alignment, attack staggering, post-attack retreat, and re-entry delay.
- Forward-travel phases, locked combat gates, multi-lane enemy entry, and short recovery intermissions.
- One three-phase test boss with phase-specific move pools, pacing, movement, and transition locks.
- Five Archive Knight boss attacks distributed across its three phases, including the meter-gated Archive Cataclysm super.
- Data-driven CPU fighter profile with reaction delay, decision cadence, preferred range, aggression, defense, anti-air, punish, retreat, jump-evade, and mistake tuning.
- Deterministic CPU intentions for observation, approach, lane alignment, retreat, guard, jump evade, neutral attacks, anti-airs, and recovery punishes.
- Story-mode CPU fairness rules: one probability roll per opportunity, no direct input reading, no recovery bypass, and only phase-legal moves.
- Functional player and CPU guarding with facing checks, blockstun, pushback, reduced hit-stop, meter gain, and no blocked-hit health loss.
- Data-driven mid, low, overhead, throw, unblockable, and unparryable attack rules.
- Standing guard stops mids/overheads; crouching guard stops mids/lows.
- Six-frame `MP+MK` parry window with stronger perfect-parry recoil and a meter reward.
- Counter-hit and punish-counter detection based on the defender's startup/active/recovery frames.
- Ten-percent-per-hit combo damage scaling with configurable move floors and a higher super floor.
- Boss Resolve gauge with move-authored guard damage, delayed recovery, visible HUD bar, and a timed guard-break vulnerability.
- MUGEN-informed super pause that continues presentation while freezing the fixed-tick duel state.
- Phase-three Archive Cataclysm with meter cost, super freeze, startup invulnerability, lane-avoidable reach, parry counterplay, and punishable recovery.
- Cinematic boss-super camera focus, stage wash, color pulse, and priority HUD announcement.
- Keyboard `B` and controller left trigger block controls.
- CPU intention, target, reaction delay, decision cooldown, and reasoning in the F1 debug overlay.
- Deterministic hit-stop on confirmed attacks and presentation-only camera shake.
- Boss defeat can unlock a playable boss form.
- Boss-form unlock persists to `user://project_mannequin_mvp_progress.json`.
- Form archive and form swapping rules.
- Debug overlay with tick, inputs, health, meter, state, move, events, and form data.
- Debug overlay readouts for super pause, Resolve, parry timing, and guard-break recovery.
- Combat box viewer toggle.
- Sprite3D-based 2.5D character presentation.
- Offline PixelLab/Aseprite tooling path for mannequin sprite sheets.
- Higgsfield-generated mannequin master art and complete movement, attack, reaction, form-swap, knockdown, and death pose rows.
- Reproducible Higgsfield sheet processor with transparent `256x256` frames and a game-ready `10x9` atlas.
- Dedicated crouch, air-attack, and uppercut rows with connected-pose extraction, consistent grounding, one baked anatomical scale, and nearest texture filtering.
- Held crouch presentation and an isolated no-enemy visual-pose test mode.
- Clean idle/crouch/air scale comparison generated from the exact runtime atlas frames.
- Higgsfield-generated Archive District stage with a clear arcade combat lane and layered city backdrop.
- Corrected sprite grounding so character feet remain visible above the stage plane.
- Responsive top HUD and reduced world-space actor label clutter.
- 16/16 Living Archive mirrored lifebar package with a real-alpha porcelain/plum frame, live Godot health/meter values, active-form bust portraits, concise split telemetry, and centered safe-area behavior across 16:9, 21:9, 4:3, and isolated-duel split-screen views.
- MUGEN source/permission audit covering creator credits, declarative conversion, and reference-only restrictions.
- Reference implementation audit covering the supplied Simpsons, Street Fighter, Godot, MAME X-Men, and GameJolt projects.
- Detailed Simpsons-style pre-boss camera, route, spawn, collision, and enemy-rhythm adaptation notes.
- Startup main menu.
- In-game pause menu.
- Gameplay HUD with player health, meter, boss health, objective text, notifications, and damage numbers.
- MVP completion/failure panel with restart and main-menu shortcuts.
- Complete arcade stage sequence: travel through six escalating horde sectors, advance to the boss gate, defeat the Archive Knight, and shapeshift.
- Data-driven world catalog and mission/encounter definitions for future archive realms.
- 15/16 illustrated Living Archive startup map with four realm controls, four-node route spine, selected-realm state, responsive 16:9/21:9 composition, and safe checkpoint replacement confirmation.
- 16/16 Living Archive results package shared by Stage Clear, World Complete, and Game Over, with asymmetric rank/tally presentation, responsive exact captures, and all seven transactional routes verified.
- 16/16 Living Archive pause package shared by root, Move List, Move Cards, Form Loadout, and Artifacts, with active-form identity and responsive nested-view validation.
- 16/16 Living Archive form-select package with full-body preview, six-slot inheritance vault, current/cursor separation, cancel/reopen/confirm flow, and deterministic explicit Goku swap.
- Mission selection shared by the bootstrap, stage renderer, encounter director, HUD, completion flow, and debug scenarios.
- World Warrior route with six gated horde encounters, three enemy roles, seventeen staggered spawns, and all four entry edges.
- Ryu boss with a Higgsfield-restyled sprite atlas, CPU profile, three phases, valid six-button commands, Hadouken, Shoryuken, Tatsumaki, and super data.
- Fixed-tick projectile runtime with lane-aware collision, blocking, parrying, hit-stop, clashes, lifetime, and camera-bound cleanup.
- Dedicated playable Ryu archive form unlocked after the World Warrior boss is defeated.
- Higgsfield-restyled Tournament District and Ryu presentation using one modern cel-shaded 2.5D art direction.
- Deterministic World Warrior route and boss-loop smoke scenarios.
- Astral Battlefront route with six horde gates, four enemy profiles, a dedicated duel arena, and a twelve-phase Goku boss.
- Data-driven flight stance with bounded movement, legal-move filtering, timed landing recovery, and CPU flight decisions.
- Finite Instinct evades with charges, cooldowns, legal-state checks, and throw counterplay.
- Authored, direction-aware Kamehameha animation with a hand-anchored charge
  orb, stationary beam body, lane-aware combat, and clash metadata.
- Dedicated playable Goku archive form unlocked after the Astral boss is defeated.
- Playable Goku archive transformation cycling through all twelve forms with
  `22HP`.
- Higgsfield-generated Shattered Skyway background and twelve normalized
  `8x18` runtime atlases: Base, Kaioken, False Super Saiyan, SS1, SS2, SS3,
  SS4, God, Blue, Blue Kaioken, UI Sign, and Mastered Ultra Instinct.
- Separate 55-frame signature-move atlases for all twelve Goku forms, including
  Kamehameha, aerial beam, rush, rising attack, Meteor Smash, teleport,
  flight pressure, Spirit Bomb, and Instinct Rush.
- Dedicated animated ki blast, Kamehameha, and Spirit Bomb effect atlases.
- Dedicated 17-frame Blue and 64-frame Mastered Instinct transformation
  atlases, plus a dedicated Instinct evade sequence.
- M.U.G.E.N AIR extraction that preserves action order, frame duration,
  offsets, flips, and intentional invisible teleport frames.
- Twelve-form phase presentation with real silhouette changes and
  phase-colored grounded aura treatment.
- Deterministic Astral boss-loop smoke coverage for all twelve forms,
  hand-anchored beams, God-tier flight, Instinct evasion, unlock, playable
  transformation cycling, and completion.
- Arcade stage HUD with realm, stage, encounter, remaining-hostile, cleared-enemy, and forward-advance feedback.
- Sprite-source audit for The Spriters Resource, Sprite Database, and MUGEN Free For All.
- Player attacks auto-face the nearest live opponent when a move starts, making hit testing less brittle.
- Basic shapeshift/morph flash when form swapping.

Tuning Backlog After MVP:

- Motion specials and supers now use attack buttons, but command tuning and visual feedback need more work.
- Player super data exists through `236HK`; its VFX and move-specific animation still need polish.
- A character-specific sheet is still needed for the training enemy; the Archive Knight boss and inherited form now share a unique runtime-approved 10×9 atlas.
- Original, redistributable World Warrior character art is still needed; the current Ryu frames are a local personal-use prototype source with no bundled license metadata.
- World Warrior Dojo Rookie and Pavilion Striker now use 16/16 original 60-frame style-v2 atlases with phase-aligned Quick Palm/Turning Kick and exact 720p/1080p evidence. Only the tactical Grappler base atlas remains explicitly rejected.
- Dojo Approach now uses a 15/16 layered dusk-dojo environment with a quiet packed-earth floor and exact 720p/1080p evidence; the reused tournament composite remains rejected for World Warrior Stages 2–4.
- Archive Nexus stage art, enemies, boss presentation, hazards, HUD, Archive Map, results, pause, and form-select are runtime-approved; World Warrior is the next active art-completeness pass.
- Astral Battlefront currently uses one wide stage panel and still needs segmented travel panels, parallax, breakables, and dedicated horde art.
- Goku still needs audio and additional cinematic super camera framing.
- Stronger boss defeat celebration/unlock presentation.
- Boss armor, weak points, audio, impact VFX, and more authored attack telegraphs.
- Additional original enemy and boss sprite sheets.

Out Of Scope Until After MVP:

- 1-4 player co-op.
- Shared camera constraints for four players.
- Boss armor, weak points, and raid-style team mechanics.
- Revive system.
- Synergy Arts.
- Local versus.
- Boss raids.
- Training room controls.
- Content validation report UI.
- Local creator UI/editor.
- Additional finished worlds and the full campaign/world structure beyond the three MVP routes.
