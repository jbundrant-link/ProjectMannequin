# CPU Fighter AI

The story boss uses the same `CombatActor`, `CombatStateMachine`, `MoveData`, meter, frame timing, and combat-box resolver as the player. The CPU chooses intentions; it does not directly apply damage or skip move rules.

## Decision Priority

Each fixed simulation tick evaluates the following priorities:

1. Select the nearest live opposing actor.
2. Face the opponent and observe its current move instance.
3. Respect the configured reaction delay before responding.
4. Guard, jump-evade, or retreat from an incoming in-range attack.
5. Anti-air an airborne opponent with a phase-legal tagged move.
6. Punish recovery when a legal move can start before recovery ends.
7. Align to the opponent's depth lane.
8. Continue committed defensive movement.
9. Approach or dash toward preferred range.
10. Retreat when crowded.
11. Select a weighted neutral attack or briefly hold neutral.

## Fairness Rules

- The brain observes actor state, move frame, position, lane, health, and meter. It does not inspect raw player inputs.
- Reaction delay starts when a new move or airborne state is first observed.
- Guard, anti-air, and punish probabilities are rolled once per opportunity.
- A failed probability check remains failed for that move instance.
- Every attack still uses authored startup, active, recovery, meter, hitboxes, hit-stop, and state restrictions.
- Any startup invulnerability is declared on the move data; the CPU cannot invent invulnerability.
- The CPU cannot attack during hitstun, blockstun, cinematic locks, or its own attack recovery.
- A configurable mistake chance creates intentional missed decisions.
- Deterministic random state makes identical simulations reproducible.

## Data

`CpuFighterProfileData` controls:

- reaction and decision frames
- guard and movement commitment duration
- preferred horizontal range and lane tolerance
- dash distance
- aggression
- guard, anti-air, punish, retreat, and jump-evade chances
- intentional mistake chance

Each `BossPhaseData` can modify reaction time, aggression, defense, movement speed, attack cadence, range, and the enabled move pool.

Moves use tactical tags such as:

- `anti_air`
- `gap_closer`
- `punish`
- `keep_out`
- `wide`

## Archive Knight

- Phase 1, Sentinel Stance: slower reactions, lower aggression and defense, deliberate cleave pressure.
- Phase 2, Broken Seal: normal reaction speed, stronger defense, an overhead lunge, and a low sweep.
- Phase 3, Last Archive: faster reactions, higher aggression, stronger defense, Final Archive Breaker, and one meter-gated Archive Cataclysm super.

The phase multipliers alter decision quality without granting illegal cancels. Archive Cataclysm's startup invulnerability is explicit move data and ends before its punishable recovery.

## Debugging

Press `F1` during combat to view:

- current CPU intention
- selected target
- reaction frames remaining
- decision cooldown
- the reason for the latest decision
- guard and blockstun state
- Resolve gauge and guard-break recovery
- parry active/recovery frames
- simulation hit-stop and super-pause frames

The `PROJECT_MANNEQUIN_CPU_SMOKE_TEST=1` environment flag runs deterministic guard, anti-air, punish, and approach probes. `PROJECT_MANNEQUIN_BOSS_DUEL_SMOKE_TEST=1` verifies block mix-ups, parry, Resolve break, damage scaling, counters, and super pause. `PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST=1` runs an enabled-CPU three-phase boss sparring test.
