# Boss Duel Mechanics

This slice adapts fighting-game principles to Project Mannequin's one-player arcade MVP. It does not reproduce another game's complete ruleset.

## Source-Derived Principles

### Street Fighter 6

- Keep the six-button punch/kick layout.
- Make defense a resource and decision, not indefinite passive blocking.
- Distinguish ordinary hits, counter hits, and recovery punishes.
- Provide a universal timed defensive answer.

Primary reference: [Street Fighter 6 official Battle HUD manual](https://game.capcom.com/manual/SF6/en/ps5/page/3/1).

### Dragon Ball FighterZ

- Convert launchers into controllable aerial routes.
- Use a strong universal approach tool without removing counterplay.
- Give supers a clear freeze, focus change, and spectacle beat.
- Keep character attacks on shared input grammar while varying their combat role.

Primary references: [Dragon Ball official Super Dash and Z Assist guide](https://en.dragon-ball-official.com/news/01_734.html) and [Bandai Namco Dragon Ball FighterZ overview](https://en.bandainamcoent.eu/dragon-ball/dragon-ball-fighterz).

### MUGEN / Ikemen GO

- Keep commands, move timing, boxes, and state behavior data-driven.
- Treat super pause as simulation timing with presentation controls, not an animation delay.
- Preserve deterministic target and hit ownership.

Primary reference: [Ikemen GO source repository](https://github.com/ikemen-engine/Ikemen-GO).

## Implemented MVP Rules

- `B`: standing guard; blocks mids and overheads.
- `Down + B`: crouching guard; blocks mids and lows.
- `MP + MK`: six-frame parry. The first three frames produce stronger recoil.
- Boss blocks drain Resolve according to each move's `GuardDamage`.
- Empty Resolve causes a 78-frame guard break, then refills.
- Hitting startup/active frames gives `CounterHit`: 1.15x damage and four extra hitstun frames.
- Hitting recovery gives `PunishCounter`: 1.25x damage, six extra hitstun frames, and stronger hit-stop.
- Continued combos lose 10% damage per hit down to a move-authored floor.
- Supers can define `SuperFreezeFrames` and `InvulnerableStartupFrames`.
- Archive Cataclysm costs the boss's full meter, freezes for 24 frames, is unblockable but parryable, can be avoided through lane positioning, and has long recovery.

## Archive Knight Match Flow

1. Sentinel Stance teaches spacing, guard, and a readable mid cleave.
2. Broken Seal adds the overhead Vault Lunge and low Archive Sweep.
3. Last Archive grants full meter and introduces Archive Cataclysm.
4. The player answers Cataclysm by lane movement, a timed parry, or spacing, then punishes recovery.
5. Defeat still unlocks the playable Archive Knight form and preserves the original MVP inheritance loop.

## Deliberately Deferred

- DBFZ-style 3v3 tags and assists remain outside the one-player MVP.
- SF6's complete Drive system is not copied; Resolve is a boss-pressure mechanic tailored to this game.
- Homing dash, throws, projectile clashes, wall bounce, wake-up options, and cinematic finishers need dedicated later slices.
- Audio, unique boss animation frames, VFX, and telegraph art remain presentation work.
