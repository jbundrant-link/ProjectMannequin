# World Warrior Sector MVP

Tournament District is the second playable mission. It follows the same arcade route contract as Archive District: travel, defeat six gated hordes, enter the champion gate, defeat the boss, archive the boss form, and complete the mission.

## Route

- 102-unit side-scrolling stage.
- Six horde encounters.
- Seventeen pre-boss enemies.
- Three roles: Dojo Rookie, Street Challenger, and Tournament Grappler.
- Staggered entries from the left, right, near lane, and far lane.
- Three-phase Ryu boss at the champion gate.

## Boss Kit

- `LP`: jab.
- `MP`: strong.
- `HP`: fierce.
- `2HK`: sweep.
- `236LP`: Hadouken projectile.
- `623HP`: invulnerable anti-air Shoryuken.
- `214HK`: Tatsumaki.
- `236236HP`: Shinku Hadouken super.

The CPU profile uses reaction delay, range preferences, guard decisions, recovery punishes, anti-air selection, phase move pools, and authored mistakes. It does not read player inputs directly.

## Presentation

The Tournament District and active Ryu atlas use a shared modern cel-shaded 2.5D art direction generated through Higgsfield. The Ryu atlas was style-matched from locally supplied prototype references and remains personal-use derivative art. The horde roles still use tinted mannequin visuals and need original character sheets in a later art pass.

## Verification

- `PROJECT_MANNEQUIN_WORLD_ID=world_warrior_sector` selects this route.
- `PROJECT_MANNEQUIN_SCROLL_SMOKE_TEST=1` validates all six encounters and seventeen entries.
- `PROJECT_MANNEQUIN_CPU_SMOKE_TEST=1` validates guard, anti-air, punish, and approach decisions.
- `PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST=1` validates the Ryu profile, projectile hit, Shoryuken, form unlock, and stage completion.
