# Roguelike Analysis: How To Improve Project Mannequin

## Document Role

This repository-owned document preserves the complete design analysis that the
2026-07-15 Codex audit evaluated. It replaces the need to reference a machine-local
Codex attachment.

This is **design inspiration and proposal evidence**, not roadmap authority.
Recommendations become required scope only when accepted into
[MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md). Current
implementation validation, adaptations, defects, and explicit non-adoptions are
tracked in [ROGUELIKE_BOSS_BACKLOG.md](ROGUELIKE_BOSS_BACKLOG.md).

## Source Context

The analysis was based on the video **"32 Upcoming 2.5D Side-Scroller Games You
Need to See"**, a review of the highlighted roguelike and fighting-game examples,
and contemporary genre practices. Its purpose was to identify ideas that could
strengthen Project Mannequin's run structure without automatically overriding the
game's fighting-system identity or later locked product decisions.

## Games And Takeaways

### 1. Mad King Redemption

_2.5D roguelite beat 'em up_

| Feature | Description | Project Mannequin takeaway |
| --- | --- | --- |
| Corruption system | Gain forbidden dark-magic powers at a cost, creating a per-run risk/reward tradeoff. | Absorbed boss forms could carry corruption drawbacks. Example: an instinct form auto-dodges but drains health over time. |
| Randomized dungeons | Enemy layouts and encounters differ between runs. | Stage encounters could shuffle enemy compositions and spawn positions between playthroughs. |
| Four playable heroes | Each hero has a unique moveset and playstyle. | Each unlocked boss form can function as a distinct hero through the Form Archive. |

### 2. Shot One Fighters

_2.5D roguelite fighting game_

| Feature | Description | Project Mannequin takeaway |
| --- | --- | --- |
| Custom moveset building | Start with basic actions and earn launchers, counters, projectiles, and other moves during a run. | The Mannequin could start barebones and acquire Move Cards from defeated enemies or encounter rewards. |
| 100+ artifacts, including cursed artifacts | Artifacts can warp combat rules and create powerful builds with meaningful risk. | Stage pickups could modify combat properties. Example: Burning Fists adds damage over time but increases damage taken. |
| Boss as gatekeeper | Bosses reward the strongest moves and artifacts. | Boss defeat naturally fits form absorption; defeating a boss can unlock its signature move and full form. |
| Mech hub between runs | A persistent hub evolves as content is unlocked. | The Archive could become a persistent lobby that visibly grows as worlds and forms are completed. |

### 3. Steel Maiden

_Lightning-fast action roguelite_

| Feature | Description | Project Mannequin takeaway |
| --- | --- | --- |
| Two-button simplicity | Fast, explosive combat uses minimal inputs. | Keep the core loop approachable through auto-combos and simplified specials without discarding traditional inputs. |
| Builds shift every run | Upgrade variety makes each run play differently. | Each world and stage could offer power-ups unique to that world's combat identity. |
| Frame-by-frame animation | High-fidelity 2D animation supplies the visual identity. | Project Mannequin's sprite-sheet animation quality is itself part of the art style and deserves more investment than polygon count. |

### 4. Marvel Tokon: Fighting Souls

_4v4 tag-team fighter_

| Feature | Description | Project Mannequin takeaway |
| --- | --- | --- |
| 4v4 tag-team system | One leader and three assists can swap control during combos. | Form Swap could act as an active tag system, allowing the Mannequin to shapeshift between archived forms without pausing. |
| Wall breaks and stage transitions | Knockback transitions fights into new stage areas and creates act breaks. | Boss transformations could trigger wall-break cinematics and move the fight to a crater, sky arena, void, or other phase-specific space. |
| Directional assists | Neutral, forward, and down inputs select different assist actions. | Archived forms could provide form-specific projectile, anti-air, and unique assist variants. |
| Dynamic team naming | Team names derive from roster composition. | The Archive could generate a squad name from the equipped form set. |
| Approachable plus deep | Simplified quick inputs coexist with full traditional commands. | Preserve the dual-input philosophy: auto-combos for newcomers and motion inputs for experienced players. |

### 5. SlashZero

_3D anime roguelite action platformer_

| Feature | Description | Project Mannequin takeaway |
| --- | --- | --- |
| Vertical and aerial combat | Air dashes, wall-runs, and aerial juggles are core mechanics. | DBZ-inspired stages and optional modes could extend the existing flight system with wall movement and aerial combat. |
| Hacking build system | Abilities are slotted into a grid where placement creates synergies. | Move Cards could form a modular grid; adjacency such as Launcher beside Air Combo could unlock a juggle extension. |
| Hades-style narrative | Story unfolds across runs through NPC relationships and lore fragments. | Boss victories could unlock per-world lore; completing all fragments could reveal a secret ending or hidden boss. |
| Parkour movement | Traversal includes vaulting, dashing, vertical layers, and multiple paths. | Some stages or optional modes could include vertical sections, wall-jump segments, and route choices rather than only flat scrolling. |
| Character variety | Playable characters have distinct ranges and combat roles. | Every absorbed boss form should feel mechanically distinct in range, speed, and combo structure. |

## Genre Best Practices

### Meaningful Choices Over Flat Stats

- Weak choice: `+10% damage` with no behavior change.
- Strong choice: light attacks become projectiles while heavy attacks are disabled.
- Project Mannequin takeaway: rewards should change **how** the player fights,
  not only how hard attacks hit.

### In-Run Progression

- Every pickup should alter combat flow.
- Contextual buffs should respond to the threats the player is facing.
- Project Mannequin takeaway: after clearing a horde encounter, offer three
  randomized Move Card or artifact choices.

### Meta-Progression Between Runs

- Avoid permanent stat grinding as the primary route to victory.
- Prefer content unlocks such as characters, biomes, and mutators.
- Project Mannequin takeaway: boss forms are the main meta-progression; each form
  opens new strategies, while the Archive grows as a visible trophy space.

### Every Death Is A Learning Opportunity

- A failed run should teach something about the combat system.
- Project Mannequin takeaway: explain what caused the death and suggest a
  counter-strategy. Example: "Goku used Instant Transmission 12 times - try using
  throws to catch his teleport recovery."

## Recommended Features

### Priority 1: Run-Based Move Acquisition

This was identified as the single highest-impact roguelike addition.

- Start each run with only basic punches and kicks.
- Give defeated enemies a chance to drop a Move Card that can be slotted into the
  current moveset.
- Provide a modular Move Card grid where adjacent cards create combo synergies.
  Example: Launcher beside Air Combo unlocks a juggle extension.
- Make boss defeat grant a signature move and unlock the full boss form.
- Limit the number of Move Cards carried per run so builds require tradeoffs.
- Favor moves that change combat behavior over flat damage increases.

### Priority 2: Form-Swap Tag System

- Allow an unlocked form to tag in during a combo instead of requiring a paused
  form-select flow.
- Make every archived form callable as an assist attack.
- Author directional variants: forward assist for a projectile, down assist for
  an anti-air, and neutral assist for a form-specific action.
- Ensure each form differs in range, speed, and combo structure like a separate
  playable character.
- Generate a dynamic squad name from the equipped forms.

### Priority 3: Artifact And Corruption System

- Offer three randomized artifacts between encounters.
- Include powerful cursed artifacts with meaningful drawbacks.
- Allow boss forms themselves to carry corruption drawbacks. Example: a powerful
  auto-dodge form drains health over time.
- Author world-themed artifacts, including examples such as:
  - Dragon Ball world: Senzu Bean for a one-use full heal; Gravity Training for
    increased damage with reduced mobility.
  - Street Fighter world: Parry Master for a larger perfect-parry window;
    Revenge Gauge for charging a counter through damage taken.
- Give each world and stage power-ups that express its own combat identity.

### Priority 4: Boss Arena Transitions

- Trigger a wall-break cinematic when a boss transforms.
- Transition to a new arena section such as a crater, sky, or void.
- Use transitions as act breaks that escalate each boss phase.
- Connect arena transitions to transformation progression for Goku and other
  multi-phase bosses.

### Priority 5: Verticality And Aerial Combat

- Use air dashes, wall-runs, and aerial juggles as core mechanics in appropriate
  stages or modes.
- Extend the existing Goku flight direction with authored aerial encounters.
- Add traversal variety through vertical sections, wall-jump sequences, and
  multi-path routes where they support the mode.

### Priority 6: Randomized Encounter Composition

- Randomize enemy types, counts, and spawn positions between playthroughs.
- Add occasional elite variants with modified behavior and a visual cue.
- Keep final boss identity fixed for each world.

### Priority 7: Persistent Archive Hub

- Build a visual hub between runs that grows as forms are unlocked.
- Add trophy pedestals for defeated bosses.
- Provide a Training Room for practicing with unlocked forms.
- Add NPC interactions that expand each world's lore.

### Priority 8: Hades-Style Narrative Across Runs

- Unfold story through recurring NPC interactions and lore fragments.
- Unlock a world-specific lore fragment after boss defeat.
- Reveal a secret ending or hidden boss after completing the lore set.

### Priority 9: Limited Lives System

- Display a fixed number of lives alongside health.
- On zero health, consume a life and reset the encounter or immediately revive
  the player with brief invincibility.
- End the run when all lives are depleted.
- Treat lives as a resource that could optionally be traded for a powerful reward.
- Award extra lives from selected difficult encounters, bosses, or score goals.

### Priority 10: Accessibility And Input Philosophy

- Keep the core loop approachable with auto-combos and simplified specials.
- Preserve full traditional motion inputs for advanced players.
- Maintain a dual-input philosophy that is approachable and deep.

### Priority 11: Death As A Teacher

- After death, identify the move or behavior that caused the loss.
- Suggest a relevant counter-strategy.
- Use every failed run to teach something new about the combat system.

### Priority 12: Quality Animation As Art Style

- Treat high-fidelity frame-by-frame 2.5D animation as the visual identity.
- Invest in sprite animation quality over polygon count.

## Proposed Architecture Mapping

These mappings describe the original proposal. Actual implementation may use a
different owning type where the repository has evolved.

| Recommended feature | Inspiration | Proposed system to extend |
| --- | --- | --- |
| Move Card acquisition | Shot One Fighters, SlashZero | Extend `MoveData` with Move Card pickup and limited-slot grid behavior. |
| Form-Swap tag system | Marvel Tokon | Extend `FormArchive` with mid-combo tag-in and assist calls. |
| Artifacts and corruption | Mad King Redemption, Shot One Fighters | Add artifact data and apply modifiers through `CombatActor`. |
| Wall-break transitions | Marvel Tokon | Trigger arena swaps from boss phases in `ArcadeEncounterDirector`. |
| Aerial combat and flight | SlashZero | Extend combat physics, flight, and wall-movement states. |
| Randomized waves | Mad King Redemption | Add a random-pool mode to encounter spawn data. |
| Archive Hub | Shot One Fighters | Expand persistent progress into a visual hub scene. |
| Narrative lore drops | SlashZero / Hades-style narrative | Track authored lore fragments in permanent progression. |
| Death screen | Genre best practice | Handle death events and teaching feedback in `MvpHud`. |
| Dual input philosophy | Marvel Tokon, Steel Maiden | Validate both quick and traditional paths in `CommandInterpreter`. |
| Dynamic squad names | Marvel Tokon | Generate a name from the equipped form set. |

## Audit And Adoption Status

The strict repository audit of every takeaway and Priority 1-12 is maintained in
[ROGUELIKE_BOSS_BACKLOG.md](ROGUELIKE_BOSS_BACKLOG.md). That audit distinguishes:

- implemented behavior;
- adapted or incomplete behavior;
- true defects;
- intentional scope decisions;
- proposals that remain unaccepted by the master plan; and
- visual claims that require manual review rather than code inspection.