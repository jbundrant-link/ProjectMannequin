# MASTER PROMPT: PROJECT MANNEQUIN

Act as a senior game designer, combat designer, systems designer, technical architect, Godot 4.x C# engineer, and creative director helping me design and prototype a personal-use 2.5D side-scrolling beat ’em up, fighting-game hybrid, and M.U.G.E.N-inspired local creator sandbox.

Do not try to build the entire game at once. Work section by section. Prioritize the MVP combat loop first.

The first goal is:

A blank mannequin can move, attack, use fighting-game inputs, hit an enemy with custom hitboxes, defeat a boss, unlock that boss as a form, and shapeshift into that form.

---

# 1. High-Level Game Concept

I want to design a 2.5D side-scrolling action game inspired by classic arcade beat ’em ups like _Teenage Mutant Ninja Turtles: Shredder’s Revenge_ and Konami’s 1992 _X-Men_, but with deeper fighting-game mechanics inspired by _Street Fighter_, _Tekken_, _Mortal Kombat_, _Dragon Ball FighterZ_, and other combo-heavy fighters.

The game follows a mysterious blank mannequin character who starts as an empty vessel with no identity, no powers, and only basic combat abilities.

The mannequin travels through major pop culture worlds from video games, anime, comics, arcade games, fantasy, sci-fi, horror, and other entertainment universes.

The player begins as a blank mannequin / training-dummy-style character. As the mannequin defeats powerful characters, bosses, champions, heroes, villains, monsters, gods, and fighters across different worlds, it can archive their combat essence, shapeshift into them, inherit their powers, and use their full fighting styles in future battles.

The central fantasy is:

A blank mannequin travels across major pop culture worlds, defeats powerful characters and bosses, archives their combat essence, shapeshifts into them, inherits their abilities, and builds an expanding living roster of forms while fighting through local co-op side-scrolling stages, fighting-game-style boss battles, local versus modes, boss raids, and creator-made M.U.G.E.N-style content.

---

# 2. Core Inspirations

The game should combine the feeling of:

- _Teenage Mutant Ninja Turtles: Shredder’s Revenge_ for side-scrolling co-op beat ’em up action
- Konami’s 1992 _X-Men_ arcade game for chaotic multiplayer arcade energy
- _Street Fighter_ for command inputs, fireballs, supers, martial arts duels, and readable combat
- _Tekken_ for launchers, juggles, heavy strikes, grappling, and wall-bounce-style combo depth
- _Mortal Kombat_ for cinematic special moves, brutal impact, and strong character personality
- _Dragon Ball FighterZ_ for anime-style speed, aerial combat, explosive supers, and dramatic camera angles
- _M.U.G.E.N._ for custom characters, custom stages, imported content, and endless local expansion
- Amazo from _Justice League Unlimited_ as inspiration for the mannequin’s adaptive power-copying concept, except this mannequin fully shapeshifts into defeated characters instead of only copying their abilities

---

# 3. Narrative Framework: The Living Archive

The multiverse is a massive cosmic archive shaped by imagination, memory, entertainment, and combat mythology.

Every world exists as a **data-reality** inside a giant cosmic arcade/archive system. These worlds are real to the characters who live inside them, but they also function like playable archive sectors that can be scanned, loaded, repaired, modified, corrupted, remixed, and expanded.

The archive is maintained by an ancient cosmic intelligence known as **The Curator**.

The Curator created the mannequin as a blank vessel designed to preserve combat identities before entire worlds collapse, glitch, corrupt, or are erased.

The mannequin’s mission is not simply to destroy enemies. Its true purpose is to archive them.

When the mannequin defeats a world’s champion, boss, hero, villain, or major fighter, it records a living combat copy of that character into the **Living Roster**.

The original character can remain in their home world, but the mannequin gains a shapeshiftable archived version of their body, movement, attacks, powers, supers, and fighting style.

The mannequin does not know whether it is a hero, weapon, experiment, mistake, or replacement god. As it collects more forms, it begins to develop personality, emotions, memories, and identity glitches.

The main opposing force is a corrupted faction called **The Erasers**. The Erasers believe the multiverse is too unstable to save and want to permanently delete worlds before they can be archived. They appear across different realities, corrupting bosses, breaking stages, remixing characters, and forcing the mannequin to race against time.

Core story questions to explore:

- Is the mannequin saving worlds or consuming them?
- Is it preserving identities or stealing them?
- Does it become more human as it collects more forms?
- Can it create its own identity instead of borrowing others?
- What happens when archived characters begin speaking inside the mannequin’s mind?
- What happens if The Erasers corrupt one of the mannequin’s stored forms?
- Is The Curator truly helping, or is it using the mannequin as a cosmic backup drive?

---

# 4. Visual Style

The game should have a modern 2.5D fighting-game visual style.

The graphics should blend:

- the high-fidelity polish, impact, and cinematic feel of _Street Fighter 6_
- the stylized anime energy, speed, and explosive visual effects of _Dragon Ball FighterZ_
- the readable action of classic arcade beat ’em ups
- the dramatic camera work of modern fighting games
- the chaos and variety of M.U.G.E.N-style crossover battles

The game should use high-quality 3D character models on a 2D gameplay plane.

Characters move through side-scrolling environments, but combat has the precision, impact, and visual drama of a fighting game.

The game should include:

- dynamic camera angles during supers
- cinematic boss intros
- dramatic transformations
- explosive hit effects
- flashy special attacks
- anime-style impact frames
- expressive character animations
- strong silhouette readability
- large team attacks
- dramatic boss finishers
- clear effects for 1–4 player local play

The starting mannequin should look like a blank humanoid training dummy, living mannequin, or unfinished character template. It should feel neutral, mysterious, customizable, and visually simple at first.

When the mannequin transforms, it should fully shapeshift into the archived character, but it may retain a subtle visual clue that reminds the player this is still the mannequin underneath, such as glowing white joints, porcelain-like seams, archive-code effects, or brief morphing animation frames.

## Operational Art Direction Lock

The accepted runtime art for the mannequin, Ryu, and Goku defines the canonical rendering language for the whole game. Characters, enemies, stages, props, pickups, hazards, VFX, HUD, menus, maps, portraits, and icons must look cohesive beside those characters even when each world has a different palette and motif.

The shared rendering grammar is:

- confident dark contours and selective interior linework
- broad anime/cel-shaded value planes
- clean designed highlights instead of noisy realistic reflections
- simplified materials and large readable forms
- saturated, controlled focal accents
- cinematic fighting-game lighting
- strong silhouettes and arcade readability at gameplay distance
- dimensional 2.5D presentation without photoreal/PBR surface treatment

High fidelity means stronger anatomy, animation, composition, lighting, effects, and finish. It does not mean photorealistic industrial concept art, gritty military sci-fi, dense hard-surface greebles, realistic product-render loot, painterly concept drift, or enterprise/dashboard UI.

During the current sprite-based prototype, art should imply the intended high-quality 3D volume through consistent cel shading, perspective, lighting, parallax, and camera composition.

[VISUAL_STYLE_BIBLE.md](VISUAL_STYLE_BIBLE.md) is the implementation source of truth for canonical references, category rules, generation prompts, rejection criteria, review scoring, and current replacement-art status. Technical integration alone never makes an asset visually complete.

---

# 5. Core Gameplay Pillars

## Pillar 1: Arcade Soul, Fighter Brain

The game should feel like a classic side-scrolling arcade beat ’em up with the mechanical depth of a fighting game.

Players run through stages, fight groups of enemies, break objects, dodge hazards, and face bosses, but combat rewards command inputs, cancels, launchers, juggles, parries, counters, supers, and team combo routes.

## Pillar 2: The Mannequin Is the Living Roster

Players do not only select characters. They collect forms by defeating powerful characters and bosses.

Each unlocked form gives the mannequin a new identity, moveset, combat role, and playstyle.

## Pillar 3: Boss Inheritance as Progression

Defeating bosses is the main progression hook. Every major boss becomes a playable shapeshift form after defeat.

The player builds power by archiving stronger and more varied combat identities.

## Pillar 4: Couch Play First

The game is focused only on local play for now. Online play is not important.

The game should support 1–4 players maximum on one system, with shared-screen co-op, local versus, local boss raids, local party chaos, and local creator testing.

## Pillar 5: Local Creator Sandbox Architecture

The game should function like a local M.U.G.E.N-inspired platform where custom characters, bosses, stages, music, effects, campaigns, and rulesets can be imported from local folders.

---

# 6. Pop Culture World Structure

The mannequin can travel through major pop culture worlds and creator-made realities such as:

- _Street Fighter_
- _Tekken_
- _Mortal Kombat_
- _Dragon Ball_
- _Naruto_
- _One Piece_
- _One Punch Man_
- Marvel
- DC
- classic arcade worlds
- anime battle worlds
- superhero comic worlds
- fantasy worlds
- sci-fi worlds
- horror worlds
- custom creator-made worlds

Each world should have its own characters, enemies, bosses, music style, stage hazards, visual identity, combat rules, and unlockable forms.

Example realms:

## Street Fighter Realm

A martial arts tournament world focused on grounded combat, fireballs, command inputs, rival fighters, world warriors, street arenas, and intense 1v1-style boss battles.

## Tekken Realm

A martial arts world focused on heavy strikes, launchers, wall bounces, juggling, grapplers, cybernetic fighters, martial arts families, and brutal hand-to-hand combat.

## Mortal Kombat Realm

A darker tournament world focused on brutal special moves, supernatural fighters, ninjas, monsters, weapons, sorcery, realms, and cinematic finishing attacks.

## Dragon Ball Realm

An anime energy battle world focused on ki blasts, flight, transformations, beam clashes, speed bursts, aerial combos, planet-shaking supers, and extreme power escalation.

## Naruto Realm

A ninja combat world focused on chakra, clones, substitutions, elemental jutsu, summons, weapons, stealth, movement tricks, and team-based ninja battles.

## One Piece Realm

A pirate adventure world focused on strange powers, treasure, ships, sea monsters, sword fighters, brawlers, elastic movement, wild islands, and chaotic crew battles.

## One Punch Man Realm

A superhero parody/action world focused on overpowered fighters, monster associations, absurd strength, city destruction, ranked heroes, ridiculous villains, and comedic boss fights.

## Marvel Realm

A superhero universe focused on street heroes, cosmic threats, mutants, armored heroes, alien invasions, magic users, villains, city battles, and crossover-style boss encounters.

## DC Realm

A superhero mythology world focused on iconic heroes, gods, speedsters, aliens, vigilantes, cosmic beings, multiverse threats, dark cities, and legendary boss fights.

## Custom Creator Realm

A realm created by players or developers using imported characters, stages, bosses, music, and rulesets.

---

# 7. Mannequin Evolution System

The mannequin begins with:

- basic punches and kicks
- basic jump
- basic dash
- simple grab
- weak combo string
- basic block or dodge
- simple launcher
- simple parry
- no special identity
- no elemental power
- no advanced movement
- no cinematic super

As the mannequin defeats bosses and major characters, it unlocks forms.

Each unlocked form should include:

- full visual transformation
- unique stance
- unique idle animation
- unique walk, run, jump, and dash animations
- unique basic attacks
- unique special moves
- unique combo routes
- unique super attacks
- unique cinematic ultimate
- unique defensive options
- unique hurtbox and weight class
- unique strengths and weaknesses
- unique role classification
- unique synergy tags
- unique assist behavior
- unique mastery path

The strongest system name is:

**The Living Roster System**

Other possible names:

- Mannequin Evolution System
- Boss Inheritance System
- Form Archive System
- Combat Form Absorption
- Identity Archive System

---

# 8. Boss Inheritance System

When players defeat a major boss or character:

- that boss becomes an unlocked playable form
- the mannequin archives a living combat copy of that character
- the original boss can remain in their world for story, replay, boss rush, and rematch purposes
- the mannequin can shapeshift into that character
- the boss’s powers become usable in future levels
- the boss’s moveset is adapted into a playable version
- the form can be mastered and customized over time
- the form can be used in co-op, versus, boss rush, raids, training, and custom modes

Boss fights should feel like fighting-game encounters inside a beat ’em up structure.

Bosses should have:

- unique move lists
- combo strings
- special attacks
- defensive options
- cinematic supers
- armor phases
- shield phases
- weak points
- phase transitions
- personality-driven combat style
- boss-specific stage mechanics
- unlockable playable versions after defeat

---

# 9. Form Swap Rules

The player can switch between equipped forms during gameplay, but the system should have rules so it does not become broken or visually confusing.

Balanced form swap rules:

- players equip a limited number of active forms before a mission
- form swap has a short cooldown
- form swap cannot happen while the player is being hit
- form swap has a brief morph animation
- form swap may cost meter in harder modes
- form swap may be restricted during boss supers or cinematic attacks
- switching forms may reset certain combo states
- different modes can use different form switching rules

Suggested standard loadout:

- 1 base mannequin style
- 3 active forms
- 1 assist form
- 1 ultimate/super form
- 1 passive trait
- 1 item or relic slot

Chaos Mode can allow unrestricted form switching.

---

# 10. Combat System

The combat should feel like a side-scrolling beat ’em up with fighting-game depth.

Core combat should include:

- light, medium, and heavy attacks
- special attack button
- command inputs
- quarter-circle-style moves
- charge moves
- basic attack chains
- launchers
- air combos
- juggling
- wall bounces
- ground bounces
- cancels
- super meter
- cinematic ultimates
- parries
- counters
- dodges
- blocks
- grabs
- throws
- assist attacks
- team combo extensions
- form switching
- cinematic finishers

Each form should have a different combat identity.

Examples:

- martial artists rely on footsies, fireballs, command inputs, counters, and clean combos
- Tekken-style fighters rely on launchers, stance transitions, juggles, wall bounces, and heavy strikes
- anime fighters rely on flight, energy blasts, transformations, rush attacks, and beam supers
- ninja fighters rely on clones, substitutions, traps, speed, and elemental attacks
- pirate fighters rely on strange powers, weapons, movement tricks, and chaotic brawling
- superheroes rely on strength, speed, gadgets, flight, magic, cosmic powers, or mutant abilities
- villains and bosses rely on large attacks, armor, grabs, summons, transformations, or arena control

---

# 11. Local-Only Multiplayer Design

The game should focus only on local multiplayer for now.

Do not prioritize:

- online multiplayer
- public matchmaking
- ranked matchmaking
- private online rooms
- rollback netcode
- host migration
- online mod syncing
- online lobbies
- LAN play

The multiplayer should be designed around couch play, shared-screen action, local versus, local co-op, local creator testing, and local chaos modes.

The maximum player count is 4 players.

This is a hard design rule. Every camera system, UI element, boss mechanic, stage rule, revive rule, creator tool, and combat readability decision should be designed around 1–4 local players.

Supported player counts:

- 1-player solo
- 2-player co-op
- 3-player co-op
- 4-player co-op
- 1v1 versus
- 2v2 versus
- 3-player free-for-all
- 4-player free-for-all
- 1v2 boss challenge
- 1v3 boss challenge
- 2v1 boss challenge
- 3v1 custom boss-style battles

---

# 12. Local Multiplayer Modes

## Co-op Campaign

The official campaign should support 1–4 local players.

Each player controls their own mannequin character. Every player can collect boss forms, equip loadouts, customize their style, and build their own version of the roster.

Features:

- 1–4 player local story mode
- shared-screen gameplay
- shared mission progress
- individual player progression
- boss forms unlocked for all participating players
- difficulty scaling based on player count
- revive system
- team attacks
- co-op finishers
- shared checkpoints
- optional friendly fire
- co-op-specific enemy waves
- co-op boss patterns
- replayable stages

When the team defeats a boss, all active players unlock that boss form. However, each player can build and use that form differently through mastery, alternate moves, perks, colors, assists, or loadout choices.

## Custom Co-op Campaigns

Players can build local playlists using official and imported stages.

Features:

- custom stage order
- custom boss order
- custom enemy sets
- custom music
- custom difficulty rules
- local file-based campaign packs
- creator-made world packs
- local playlist saving

## Arcade Co-op Run

A shorter local co-op mode where players fight through a set number of stages.

Options:

- 5-stage run
- 10-stage run
- random stage run
- random boss run
- limited continues
- score ranking
- combo bonuses
- survival bonuses
- unlock rewards

## Boss Rush Co-op

Players fight multiple bosses back-to-back.

Features:

- official boss rush
- custom boss rush
- random boss rush
- survival health rules
- shared lives
- revive limits
- difficulty modifiers
- boss form unlock rewards

## Local Boss Raids

Boss raids support 1–4 players.

Raid bosses should be larger, more cinematic, and more mechanic-heavy than normal bosses.

Features:

- giant bosses
- multi-phase battles
- weak points
- armor-breaking phases
- arena hazards
- team counter moments
- revive pressure
- shield-breaking objectives
- combined super opportunities
- secret phase conditions
- final team finisher opportunities
- creator-made raid bosses

Bosses should not simply gain more health in multiplayer. They should gain new mechanics based on player count.

Example mechanics:

- the boss grabs one player and teammates must free them
- two players must hit separate weak points at the same time
- the boss marks one player while others attack exposed targets
- the boss copies one player’s current form
- the arena splits into multiple danger zones
- players must combine supers to stop the boss’s ultimate attack

## Local Arena Versus

This mode supports fighting-game-style local battles.

Match types:

- 1v1
- 2v2
- 3-player free-for-all
- 4-player free-for-all
- tag battle
- assist battle
- draft battle
- random form battle
- boss form duel

Features:

- smaller arenas
- tighter camera
- clear health bars
- round system
- timer options
- hazards on/off
- official characters only option
- custom characters allowed option
- chaos rules option

## Side-Scrolling Versus

Players fight each other inside a scrolling level instead of a fixed arena.

Features:

- enemies still appear
- hazards
- moving camera
- pickups
- race-to-finish option
- boss interruptions
- team versus option
- score objectives

## Local PvPvE Missions

Players fight enemies and bosses while also competing against each other.

Mode ideas:

- boss damage contest
- relic race
- survival score battle
- stage control
- capture the form
- hazard trigger battle
- final hit capture
- rival teams
- enemy wave control

## Local Party Mode

Party Mode embraces chaotic M.U.G.E.N-style fun.

Possible rules:

- random form every 30 seconds
- random supers
- giant character mode
- tiny character mode
- one-hit launch mode
- boss hazard mode
- item chaos
- random assists
- low gravity
- speed boost
- invisible health
- exploding enemies
- mirror match madness
- infinite meter
- boss possession
- infection mode

## Local Training Room

The training room should support solo testing and local multiplayer testing.

Features:

- solo training
- 2-player training
- 4-player sandbox
- hitbox viewer
- hurtbox viewer
- damage numbers
- combo counter
- input display
- dummy settings
- record/playback
- co-op combo testing
- versus testing
- boss AI testing
- enemy wave testing
- stage hazard testing
- Synergy Art testing
- super meter controls
- slow motion option

---

# 13. Player Identity System

Each player controls their own mannequin.

Instead of all players sharing one main character, every player has a personal blank mannequin that can collect forms and equip custom loadouts.

Each player can have:

- mannequin name
- mannequin visual style
- color palette
- equipped forms
- unlocked boss forms
- custom imported characters
- assist slot
- super slot
- passive traits
- emotes
- profile card
- title/banner

This prevents multiplayer from feeling like everyone is controlling the same character.

---

# 14. Co-op Roles

Forms should naturally support team roles.

Possible roles:

- Striker: fast melee attacker focused on combos and pressure
- Tank: high-health form that protects teammates and controls space
- Launcher: specializes in launching enemies into air combos
- Grappler: throws enemies, bosses, and heavy units
- Zoner: uses projectiles, traps, summons, and ranged attacks
- Support: heals, shields, buffs, revives, or restores meter
- Controller: freezes, stuns, slows, pulls, or groups enemies
- Boss Breaker: breaks armor, shields, and boss weak points
- Mobility Form: helps the team move through stages or avoid hazards
- Specialist: uses unusual mechanics like clones, counters, traps, or transformations

Players are not locked into roles permanently. Their equipped forms create their role.

---

# 15. Team Combo System

The multiplayer should reward players for attacking together.

Team combo features:

- shared combo counter
- global juggle timer
- combo assist bonus
- air juggle handoff
- wall bounce handoff
- launcher follow-up
- assist cancels
- tag attacks
- dual supers
- triple supers
- 4-player ultimate attacks
- team finishers
- co-op boss stun attacks

Example team combo:

Player 1 launches an enemy.
Player 2 catches the enemy in the air.
Player 3 wall-bounces the enemy.
Player 4 finishes with a cinematic slam attack.

The game should recognize team sequences and reward players with extra damage, meter, score, style rank, or special finisher opportunities.

---

# 16. Synergy Arts

Synergy Arts are special attacks created when two or more players combine compatible forms or powers.

Examples:

- Fire + Wind = Flame Tornado
- Ice + Heavy Slam = Shatterquake
- Lightning + Metal = Chain Shock
- Shadow + Blade = Phantom Slash Barrage
- Water + Lightning = Thunder Current
- Gravity + Grappler = Meteor Piledriver
- Speed + Martial Arts = Afterimage Rush
- Support + Damage Dealer = Overdrive Boost
- Summoner + Zoner = Swarm Barrage
- Dragon Form + Knight Form = Dragonblade Finisher

Synergy Art rules:

- require meter from participating players
- require players to be near each other
- require compatible form types
- can require an enemy stun state or boss vulnerability window
- can trigger cinematic camera angles
- can be disabled in competitive modes
- can be simplified in casual modes

If all 4 players have full meter, they can activate a rare cinematic team super.

The strongest name for the 4-player ultimate should be:

**Multiverse Break**

---

# 17. Revive and Downed System

When a player loses all health, they enter a downed state.

Revive rules:

- teammates can revive downed players
- reviving takes time
- reviving can be interrupted by damage
- support forms revive faster
- players can crawl slowly while downed
- self-revive can exist as an item, perk, or meter-spend option
- each revive during the same fight can increase the next revive timer
- the mission fails when all players are defeated at the same time

Optional settings:

- instant revive item
- team super revive
- boss phase revive checkpoint
- shared life pool
- individual life pool
- no-revive challenge mode

---

# 18. Difficulty Scaling

The game should scale based on player count.

Solo:

- fewer enemies
- lower boss health
- simpler boss mechanics
- more forgiving checkpoints

2 players:

- moderate enemy count
- slightly tougher bosses
- basic co-op mechanics

3 players:

- more enemies
- stronger elite enemies
- bosses gain extra patterns

4 players:

- full enemy waves
- stronger boss phases
- co-op mechanics
- more hazards
- extra elite enemies
- additional team-based boss objectives

Bosses should not only gain more health. Multiplayer scaling should add mechanics, not just larger health bars.

---

# 19. Camera System

The default camera should be a shared camera because the game is focused on local play.

Shared camera rules:

- camera follows the group center
- camera zooms out when players separate
- camera prevents players from moving too far apart
- offscreen players get warning arrows
- lagging players can be pulled forward
- teleport catch-up can happen if a player gets stuck
- camera focuses on the boss arena during boss fights
- camera shake is reduced during 4-player combat
- players can adjust camera zoom in settings

Split-screen should not be the default. It may be used only for special modes like race missions or large experimental PvPvE stages.

Arena camera rules:

- keeps all fighters visible
- zooms out during big attacks
- zooms in during supers
- reduces camera shake in competitive modes
- keeps UI and hit effects readable

---

# 20. Stage Design Rules for Local Multiplayer

Co-op stages should be built for 1–4 players.

Co-op stage rules:

- enough space for 2–4 players
- enemy waves enter from multiple sides
- platforming should not punish slower players too harshly
- camera gates should wait for group progress
- revive-safe areas should appear before bosses
- respawning players should have safe re-entry points
- team-based secrets can reward cooperation
- optional split paths can exist, but should not break the camera
- hazards need clear warnings

Versus stage rules:

- readable boundaries
- fair spawn positions
- hazards can be turned off
- no unavoidable hazards
- no camera traps
- clear foreground/background separation

Raid stage rules:

- large boss arena
- visible weak points
- safe zones
- hazard zones
- revive windows
- phase transition space
- clear boss attack warnings
- team mechanic zones

---

# 21. Technical Direction

Build the first serious prototype in:

- Engine: Godot 4.x
- Primary language: C#
- Game type: 2.5D side-scrolling beat ’em up / fighting-game hybrid
- Player count: 1–4 local players only
- Target simulation: fixed 60 Hz combat simulation
- Rendering: visual frame rate can fluctuate independently from combat logic
- Content style: local folder-based content packs inspired by M.U.G.E.N.
- Core priority: gameplay architecture, mod support, and combat correctness over visual polish during early development

Do not build the real-time game runtime in Python.

Python may be used only for optional offline tools such as asset validation, content packaging, move-list generation, or batch conversion.

The game should be architected as a modular, data-driven local creator sandbox. It should not be a single hardcoded campaign or a single hardcoded character controller.

---

# 22. Non-Negotiable Architecture Rules

The implementation must follow these rules:

- Combat logic runs on a fixed 60 Hz simulation tick.
- Godot’s `_PhysicsProcess` may drive the fixed simulation tick for the MVP.
- Rendering, camera smoothing, particles, and animation presentation must be separate from combat state logic.
- Do not rely on Godot’s default physics contacts for fighting-game hit detection.
- Use custom combat boxes for hitboxes, hurtboxes, pushboxes, grabboxes, armor boxes, weak-point boxes, and projectile boxes.
- Character data should be loaded from structured data files whenever possible.
- Moves should be data-driven instead of hardcoded per character.
- Cancel rules should be loaded from declarative data, then compiled into runtime dictionaries or objects.
- Local custom content should be loaded from folders.
- The first version should not execute arbitrary external code from mods.
- Custom content should use declarative data such as JSON, supported assets, and predefined behavior templates.
- Every system should be designed around 1–4 local players maximum.
- Online multiplayer, matchmaking, rollback, public lobbies, and LAN are out of scope for now.

---

# 23. Official MVP Architecture Choices

For the first playable prototype, use these technical decisions:

- Engine: Godot 4.x
- Language: C#
- Fixed simulation: Godot `_PhysicsProcess` driving `GameSimulation.ProcessTick()`
- Input system: bitmask state array internally, readable command strings externally
- State machine: enum-driven controller for MVP
- Box evaluation: 2.5D AABB combat boxes with lane/depth overlap checks
- Cancel rules: declarative JSON data compiled into runtime `MoveData`
- Mod/content scope: local folder-based content packs
- Custom scripting: do not execute arbitrary external scripts in the first prototype

---

# 24. High-Level Engine Layers

Structure the game around these major systems:

## Core Simulation Layer

- fixed 60 Hz combat tick
- player state machines
- boss state machines
- enemy state machines
- hitbox / hurtbox resolver
- input buffer resolver
- command interpreter
- combo and juggle manager
- form swap manager
- damage / meter / status resolver

## Presentation Layer

- animation controller
- VFX triggers
- SFX triggers
- camera controller
- cinematic super camera
- UI display
- accessibility visual filters

## Local Multiplayer Layer

- local player registration
- controller assignment
- shared camera rules
- player join/drop logic
- revive system
- team combo logic
- Synergy Art logic
- 4-player UI layout

## Content Loading Layer

- local folder scanner
- character pack loader
- boss pack loader
- stage pack loader
- campaign pack loader
- ruleset loader
- dependency validator
- content error reporter

## Creator / Debug Layer

- training room
- hitbox viewer
- hurtbox viewer
- frame data display
- input display
- move test menu
- boss AI test mode
- stage hazard test mode
- content validation tools

---

# 25. Recommended Godot Project Structure

Use a clean Godot project layout like this:

res://

- scenes/
  - game/
    - MainGame.tscn
    - StageRunner.tscn
    - TrainingRoom.tscn
    - CharacterSelect.tscn

  - characters/
    - CharacterBodyRoot.tscn
    - PlayerController.tscn
    - BossController.tscn
    - Projectile.tscn

  - combat/
    - Hitbox.tscn
    - Hurtbox.tscn
    - Pushbox.tscn
    - Grabbox.tscn
    - CombatDebugOverlay.tscn

  - ui/
    - PlayerHUD.tscn
    - BossHUD.tscn
    - ContentManagerUI.tscn
    - PauseMenu.tscn

  - camera/
    - SharedCamera.tscn
    - ArenaCamera.tscn

- scripts/
  - core/
    - GameSimulation.cs
    - FixedTickRunner.cs
    - GameClock.cs
    - GameConstants.cs

  - input/
    - LocalInputManager.cs
    - PlayerInputBuffer.cs
    - CommandInterpreter.cs
    - InputCommand.cs

  - combat/
    - CombatActor.cs
    - CombatStateMachine.cs
    - MoveExecutor.cs
    - CombatBox.cs
    - HitResolver.cs
    - DamageResolver.cs
    - ComboManager.cs
    - JuggleManager.cs
    - MeterManager.cs

  - forms/
    - FormManager.cs
    - FormLoadout.cs
    - FormSwapRules.cs
    - LivingRoster.cs

  - multiplayer/
    - LocalPlayerManager.cs
    - ReviveManager.cs
    - TeamComboManager.cs
    - SynergyArtManager.cs
    - LocalScalingManager.cs

  - content/
    - ContentManager.cs
    - ContentPack.cs
    - CharacterPackLoader.cs
    - BossPackLoader.cs
    - StagePackLoader.cs
    - RulesetLoader.cs
    - ContentValidator.cs

  - debug/
    - CombatDebugUI.cs
    - HitboxVisualizer.cs
    - FrameDataViewer.cs
    - InputDisplay.cs

- data/
  - default_characters/
  - default_bosses/
  - default_stages/
  - default_rulesets/
  - schemas/

- assets/
  - models/
  - textures/
  - animations/
  - vfx/
  - sfx/
  - music/

---

# 26. Local Custom Content Folder Structure

The game should scan a local user content directory outside the compiled project.

Suggested local folder:

UserContent/

- Characters/
  - CharacterName/
    - character.json
    - moveset.json
    - animations.json
    - model.glb
    - textures/
    - animations/
    - sounds/
    - effects/
    - portrait.png

- Bosses/
  - BossName/
    - boss.json
    - moveset.json
    - ai.json
    - phases.json
    - model.glb
    - textures/
    - animations/
    - sounds/
    - portrait.png

- Stages/
  - StageName/
    - stage.json
    - layout.json
    - enemy_waves.json
    - hazards.json
    - background.glb
    - music.ogg
    - preview.png

- Campaigns/
  - CampaignName/
    - campaign.json
    - stage_order.json
    - boss_order.json
    - ruleset.json

- Rulesets/
  - RulesetName/
    - ruleset.json

- BossRush/
  - BossRushName/
    - bossrush.json

Supported first-pass asset formats:

- `.json` for gameplay data
- `.glb` or `.gltf` for 3D models
- `.png` for portraits, icons, textures, and previews
- `.ogg` or `.wav` for music and sound effects
- Godot-compatible animation data where possible

Do not attempt to support every file format at first. Start with a strict supported format list.

---

# 27. Input System Architecture

Use a hybrid input buffer system.

Internally, store input as compact bitmasks for performance and consistency.

Externally, allow movesets to define commands using readable fighting-game notation:

- `L` = Light
- `M` = Medium
- `H` = Heavy
- `S` = Special
- `A` = Assist
- `U` = Ultimate
- `F` = Form Swap
- `236S` = quarter-circle forward + special
- `214H` = quarter-circle back + heavy
- `623M` = dragon punch motion + medium
- `22S` = down-down + special

Internal input bitmask example:

- `1 << 0` = Up
- `1 << 1` = Down
- `1 << 2` = Left
- `1 << 3` = Right
- `1 << 4` = Light
- `1 << 5` = Medium
- `1 << 6` = Heavy
- `1 << 7` = Special
- `1 << 8` = Assist
- `1 << 9` = Ultimate
- `1 << 10` = Form Swap

The input buffer should store at least the last 60 frames.

The `CommandInterpreter` should translate creator-friendly command strings like `236S` into internal command patterns that can be checked against the input buffer.

Important implementation rule:

`PlayerInputBuffer` should support direction lookup by historical frame.

Required method concept:

`GetNumericDirection(framesBack, facingRight)`

Do not only check the current frame direction when scanning command history.

---

# 28. State Machine Architecture

Use an enum-driven state machine for the MVP.

This keeps the early prototype easier to read, debug, and modify.

Recommended MVP actor states:

- Idle
- Walk
- Run
- Dash
- JumpStart
- Airborne
- Landing
- NormalAttack
- SpecialAttack
- SuperAttack
- Blocking
- Parrying
- Dodging
- GrabStartup
- GrabSuccess
- GrabWhiff
- Hitstun
- Blockstun
- Knockdown
- Wakeup
- Downed
- Reviving
- FormSwapStartup
- FormSwapActive
- FormSwapRecovery
- CinematicLocked
- Dead

Later, after the MVP is stable, the project can refactor specific complex states into modular classes if needed.

Do not start with a heavy state-pattern architecture before the combat loop is proven.

---

# 29. Combat Box System

Use custom combat boxes, not Godot physics contacts.

For the MVP, use **2.5D AABB combat boxes with simple lane/depth overlap**.

This means:

- Use axis-aligned boxes for fighting-game style consistency.
- Evaluate overlap manually in code.
- Keep box logic deterministic and easy to debug.
- Add a simple lane/depth check so side-scrolling beat ’em up positioning still matters.
- Do not use complex 3D oriented bounding box collision for the MVP.

Recommended coordinate concept:

- X = horizontal side-scrolling axis
- Y = vertical height / jumping axis
- Z = lane depth / beat ’em up depth

A hit connects when:

- attacker hitbox overlaps defender hurtbox on X/Y
- attacker and defender are within valid Z lane/depth range
- teams/friendly-fire rules allow the hit
- invulnerability, armor, parry, block, or cinematic locks do not prevent the hit

Combat box types:

- Hitbox
- Hurtbox
- Pushbox
- Grabbox
- ProjectileBox
- ArmorBox
- WeakPointBox

The debug viewer must be able to display these boxes clearly.

---

# 30. Cancel Rule Architecture

Use declarative JSON cancel data.

Movesets should define cancel rules like:

`"cancelInto": ["medium_punch", "fireball", "super_fire_burst"]`

However, do not read raw JSON every frame.

Load JSON once, validate it, and compile it into runtime `MoveData` objects and dictionaries.

The state machine and move executor should query compiled runtime data.

This gives the project:

- creator-friendly moveset editing
- M.U.G.E.N-style flexibility
- good runtime performance
- easier validation
- easier debugging

---

# 31. Character Pack Data Design

A character pack should define a playable form.

Example character data:

{
"id": "fire_champion",
"displayName": "Fire Champion",
"author": "Local Creator",
"version": "1.0.0",
"type": "playable_form",
"roleTags": ["striker", "zoner"],
"synergyTags": ["fire", "martial_arts", "rushdown"],
"model": "model.glb",
"portrait": "portrait.png",
"moveset": "moveset.json",
"animations": "animations.json",
"baseStats": {
"health": 1000,
"walkSpeed": 4.5,
"dashSpeed": 8.0,
"jumpHeight": 5.0,
"weight": 1.0,
"meterGain": 1.0
},
"combatProfile": {
"canDoubleJump": false,
"canAirDash": true,
"hasFlight": false,
"hasArmor": false,
"defaultBlockType": "standing"
}
}

Example move data:

{
"moves": [
{
"id": "light_punch",
"input": "L",
"type": "normal",
"startup": 4,
"active": 3,
"recovery": 8,
"damage": 35,
"hitstun": 12,
"blockstun": 7,
"cancelInto": ["medium_punch", "fireball"],
"hitboxes": [
{
"frameStart": 4,
"frameEnd": 6,
"x": 0.8,
"y": 1.2,
"z": 0.0,
"width": 0.7,
"height": 0.5,
"depth": 0.8,
"knockbackX": 1.5,
"knockbackY": 0.2
}
]
},
{
"id": "fireball",
"input": "236S",
"type": "special",
"startup": 12,
"active": 1,
"recovery": 24,
"damage": 80,
"meterGain": 8,
"spawnsProjectile": "fireball_projectile",
"cancelInto": ["super_fire_burst"]
}
]
}

---

# 32. Boss Pack Data Design

Bosses should use similar data to playable forms, but with additional AI and phase data.

Example boss data:

{
"id": "dragon_emperor",
"displayName": "Dragon Emperor",
"type": "boss",
"unlockFormOnDefeat": true,
"playableFormId": "dragon_emperor_form",
"model": "model.glb",
"portrait": "portrait.png",
"moveset": "moveset.json",
"ai": "ai.json",
"phases": "phases.json",
"baseStats": {
"healthSolo": 3500,
"health2P": 5000,
"health3P": 6500,
"health4P": 8000,
"armor": 100,
"weight": 3.0
}
}

Boss phases example:

{
"phases": [
{
"id": "phase_1",
"healthThreshold": 1.0,
"enabledMoves": ["claw_swipe", "fire_breath", "tail_slam"],
"hazards": []
},
{
"id": "phase_2",
"healthThreshold": 0.66,
"enabledMoves": ["claw_swipe", "fire_breath", "meteor_call"],
"hazards": ["falling_meteors"],
"adds": ["flame_minion"]
},
{
"id": "phase_3",
"healthThreshold": 0.33,
"enabledMoves": ["dragon_super", "arena_sweep", "grab_player"],
"requiresTeamMechanic": true,
"teamMechanicType": "break_grab"
}
]
}

Bosses should scale by player count using mechanics, not only health.

Scaling examples:

- 1 player: standard move list
- 2 players: extra enemy adds
- 3 players: extra armor phase
- 4 players: team mechanic required to interrupt ultimate attack

---

# 33. Stage Pack Data Design

Stages should support side-scrolling beat ’em up layouts on a 2.5D plane.

Example stage data:

{
"id": "neon_dojo_rooftop",
"displayName": "Neon Dojo Rooftop",
"type": "side_scrolling_stage",
"preview": "preview.png",
"background": "background.glb",
"music": "music.ogg",
"layout": "layout.json",
"enemyWaves": "enemy_waves.json",
"hazards": "hazards.json",
"supportsPlayers": [1, 2, 3, 4],
"tags": ["co_op", "versus", "martial_arts", "local"]
}

Example stage layout:

{
"startPosition": { "x": 0, "y": 0, "z": 0 },
"cameraBounds": [
{ "xMin": 0, "xMax": 30, "zMin": -4, "zMax": 4 },
{ "xMin": 30, "xMax": 70, "zMin": -4, "zMax": 4 }
],
"encounterGates": [
{
"id": "gate_01",
"xPosition": 20,
"locksCamera": true,
"requiresAllEnemiesDefeated": true
}
],
"bossArena": {
"xMin": 85,
"xMax": 110,
"zMin": -5,
"zMax": 5
}
}

---

# 34. Fixed Tick Combat Simulation

The combat simulation should run at a fixed 60 Hz.

Each simulation frame should process in this order:

1. Poll local inputs
2. Update input buffers
3. Resolve command inputs
4. Update form swap requests
5. Update actor state machines
6. Advance move frame counters
7. Spawn / despawn combat boxes
8. Resolve movement intent
9. Resolve pushbox collisions
10. Resolve hitbox vs hurtbox collisions
11. Resolve grabs
12. Apply damage, hitstun, blockstun, armor, meter gain
13. Update combo and juggle state
14. Update projectiles
15. Update boss AI
16. Update stage hazards
17. Update camera targets
18. Send presentation events to animation, VFX, SFX, and UI

The simulation should create presentation events instead of directly controlling visuals from combat logic.

Example event types:

- MoveStarted
- HitConnected
- Blocked
- Parried
- CounterHit
- LauncherHit
- WallBounce
- GroundBounce
- SuperStarted
- FormSwapStarted
- FormSwapCompleted
- BossPhaseChanged
- PlayerDowned
- PlayerRevived
- SynergyArtTriggered

---

# 35. Form Swapping Implementation Rules

Form swapping should be implemented as a controlled state transition.

Balanced rules:

- Cannot swap while in hitstun, blockstun, knockdown, downed, grabbed, or cinematic locked state.
- Can swap while idle, walking, running, or after certain cancel windows.
- Has startup frames, active morph frames, and recovery frames.
- Can cost meter depending on ruleset.
- Has cooldown per player.
- Can be disabled in certain modes.
- Can be unrestricted in Chaos Mode.

Example form swap config:

{
"formSwapRules": {
"allowedForms": 3,
"cooldownFrames": 180,
"startupFrames": 8,
"activeFrames": 12,
"recoveryFrames": 10,
"meterCost": 25,
"allowDuringComboCancel": true,
"allowDuringHitstun": false,
"allowDuringBossCinematic": false
}
}

---

# 36. Local Multiplayer Manager

The local multiplayer system should support up to 4 players.

Responsibilities:

- detect connected controllers
- assign controller to player slot
- allow keyboard/controller testing if possible
- spawn personal mannequin per player
- assign player color
- assign UI panel
- track active players
- support join/drop from character select or pause menu
- handle shared progress rewards
- handle local co-op scaling

Player colors:

- P1 = Blue
- P2 = Red
- P3 = Green
- P4 = Yellow

The player manager should never support more than 4 active players.

---

# 37. Shared Camera Rules

The shared camera should prioritize readability.

Rules:

- Keep all active players visible whenever possible.
- Follow the weighted center of active players.
- Bias toward the current combat encounter.
- Clamp inside stage camera bounds.
- Zoom out when players separate.
- Prevent players from moving too far offscreen.
- Show directional arrows for players near camera edge.
- Pull or teleport lagging players forward when necessary.
- During boss fights, include boss, active weak points, and all players.
- Reduce camera shake when 3–4 players are active.
- Cinematic supers may briefly take over camera, then return control smoothly.

---

# 38. Team Combo and Juggle Rules

The game should support co-op combos without becoming infinite.

Use a Global Juggle Timer and juggle scaling.

Suggested rules:

- When an enemy is launched, it enters Shared Juggle State.
- All players can continue the combo while the juggle timer is active.
- Each successful juggle extension increases gravity scaling.
- Each wall bounce or ground bounce has a per-combo limit.
- Bosses have stricter juggle limits than normal enemies.
- Heavy bosses may only enter partial stagger instead of full launch.
- Team handoff bonuses reward multiple players contributing to one combo.
- Style rank increases when multiple players extend the same combo.

Example combo rule data:

{
"comboRules": {
"maxWallBounces": 2,
"maxGroundBounces": 1,
"baseJuggleTimerFrames": 180,
"juggleTimerDecayPerHit": 6,
"gravityScalingPerHit": 0.05,
"bossJuggleResistance": 0.75,
"teamHandoffMeterBonus": 10
}
}

---

# 39. Synergy Art System

Synergy Arts are team attacks generated from form tags.

Each form should have synergy tags such as:

- fire
- ice
- lightning
- wind
- metal
- shadow
- light
- gravity
- martial_arts
- sword
- dragon
- ninja
- pirate
- cosmic
- magic
- machine
- speed
- grappler
- support
- summoner

Synergy Art trigger rules:

- 2 or more players must be close enough.
- Each participating player must have enough meter.
- Players must input the Synergy command within the same timing window.
- Current forms must have compatible synergy tags.
- Some Synergy Arts require boss stun or enemy launch state.
- Competitive modes may disable Synergy Arts.
- Party Mode may allow random Synergy Arts.

Example synergy rule data:

{
"synergyRules": [
{
"requiredTags": ["fire", "wind"],
"result": "flame_tornado",
"meterCostEach": 50
},
{
"requiredTags": ["gravity", "grappler"],
"result": "meteor_piledriver",
"meterCostEach": 75
},
{
"requiredTags": ["dragon", "sword"],
"result": "dragonblade_finisher",
"meterCostEach": 100
}
]
}

The 4-player ultimate should be called **Multiverse Break**.

---

# 40. Content Validation Rules

The game should validate custom content before loading it.

Validation should check:

- required files exist
- JSON format is valid
- required fields exist
- IDs are unique
- referenced assets exist
- move frame data is valid
- hitboxes have valid dimensions
- animations referenced by moves exist
- stats are within safe ranges
- supported player tags are valid
- content version is valid
- dependency list is satisfied

If content fails validation, it should not crash the game. It should be disabled and listed in an error report.

Example validation output:

Character FireChampion disabled:

- Missing file: moveset.json
- Invalid move: fireball has negative recovery frames
- Missing animation reference: fireball_cast

---

# 41. Safe Custom Content Philosophy

For the first version, custom content should be declarative, not executable.

Do not load arbitrary external scripts in the first prototype.

Instead, custom content should be built from:

- JSON stats
- JSON movesets
- predefined behavior templates
- predefined AI templates
- supported model formats
- supported animation formats
- supported sound formats
- supported visual effect references

Later versions can explore advanced scripting, but the first goal is stability.

---

# 42. Debug Tools Required Early

Build debugging tools early, not at the end.

Required debug tools:

- input display
- frame counter
- state display
- current move display
- hitbox viewer
- hurtbox viewer
- pushbox viewer
- damage numbers
- hitstun/blockstun display
- combo counter
- juggle timer display
- meter gain display
- form swap cooldown display
- boss phase display
- content validation report
- local player controller status

These tools are essential because the game depends on custom combat and creator-made content.

---

# 43. Screen Readability Rules

With 4 local players, custom characters, enemies, bosses, assists, and supers, readability is critical.

Readability options:

- player outline colors
- player number icons
- player arrows
- character name labels
- reduce visual effects option
- reduce screen shake option
- reduce super flash option
- transparent effects option
- hitbox viewer for testing
- boss attack warning indicators
- offscreen player indicators
- camera zoom controls
- accessibility-friendly color options

Visual priority should be:

1. player position
2. enemy and boss attacks
3. hazards
4. hit effects
5. background effects

Gameplay clarity should matter more than visual overload.

---

# 44. 4-Player UI Requirements

The UI should be built around 1–4 players from the beginning.

Each player should have:

- health bar
- super meter
- form icon
- assist icon
- player number
- player color
- status effects
- revive status
- ultimate meter status

Suggested player colors:

- Player 1: blue
- Player 2: red
- Player 3: green
- Player 4: yellow

The game should also support colorblind-friendly alternatives.

Boss health should be large and readable without covering the action.

---

# 45. Accessibility Options for Local Play

Because 4-player combat can become visually intense, the game should include options to reduce chaos.

Accessibility options:

- reduce effects
- reduce camera shake
- reduce flashing
- simplify background motion
- larger player indicators
- larger UI
- high-contrast outlines
- colorblind player colors
- subtitle support
- audio cue options
- auto-combo assist option
- simplified input option
- hold-to-revive or tap-to-revive option

---

# 46. Progression and Replayability

The game should reward players for defeating bosses, mastering forms, replaying stages, experimenting with loadouts, and trying custom content.

Progression systems can include:

- boss form unlocks
- form mastery levels
- alternate colors
- alternate costumes
- move variants
- assist unlocks
- passive traits
- ultimate attacks
- relics/items
- titles
- player profile banners
- secret forms
- challenge rewards
- boss rush rewards
- stage medals
- combo rankings
- survival rankings

Replayability should come from:

- collecting forms
- mastering forms
- local co-op
- local versus
- boss rush
- raid bosses
- custom stages
- random stage runs
- custom rosters
- party modifiers
- M.U.G.E.N-style imported content

---

# 47. MVP Prototype Scope

The first playable prototype should not attempt to include every feature.

The MVP should include:

- one local player
- one blank mannequin
- one test enemy
- one test boss
- one unlockable boss form
- one side-scrolling test stage
- basic movement on a 2.5D plane
- basic attacks
- one launcher
- one air combo
- one special move input
- one super move
- hitboxes and hurtboxes
- health and meter
- fixed tick simulation
- input buffer
- simple form unlock after boss defeat
- form swap between mannequin and boss form
- basic debug overlay

Do not start with 4-player chaos, creator tools, raids, or massive roster support.

Build the core combat and form inheritance loop first.

---

# 48. Vertical Slice Scope

After the MVP works, build a vertical slice.

The vertical slice should include:

- 1–4 local players
- shared camera
- four player HUDs
- controller assignment
- revive system
- one complete stage
- one boss with 2–3 phases
- boss unlock as playable form
- three playable forms total
- one team combo mechanic
- one Synergy Art
- one local versus arena
- one local content-loaded character pack
- one local content-loaded stage pack
- content validation UI
- training room with hitbox viewer

The vertical slice should prove the entire game concept at small scale.

---

# 49. Development Roadmap

## Phase 1: Core Combat Prototype

- blank mannequin playable character
- basic movement
- basic attacks
- simple combos
- special move input system
- input buffer system
- one test enemy
- one test boss
- one test stage

## Phase 2: Mannequin Shapeshifting Prototype

- defeat boss
- unlock boss form
- shapeshift into boss form
- form loadout menu
- basic form switching rules
- test mid-combat form swapping without breaking frame rate or animation logic

## Phase 3: Local Co-op Foundation

- 1–4 players local
- shared camera
- controller support
- basic enemy scaling
- revive system
- boss form unlocks for participating players
- basic team attacks
- readable 4-player UI

## Phase 4: Local Versus Foundation

- 1v1
- 2v2
- free-for-all
- arena rules
- random form battle
- draft mode
- hazards on/off
- friendly fire settings

## Phase 5: Local Boss Raids

- raid bosses
- multi-phase boss fights
- weak points
- team revive mechanics
- co-op boss mechanics
- final team finishers

## Phase 6: Local PvPvE and Party Modes

- boss damage contest
- relic race
- score battle
- random form shuffle
- party modifiers
- chaos rules

## Phase 7: Local Creator Tools

- custom character loader
- custom stage loader
- custom boss loader
- local content manager
- multiplayer spawn tools
- co-op triggers
- PvPvE objectives
- raid boss tools
- custom ruleset editor

---

# 50. Coding Guidelines for Codex, Claude, Gemini, or ChatGPT

When generating code for this project, follow these rules:

- Use C# for Godot 4.x.
- Prefer small focused classes over giant scripts.
- Use strong typing.
- Use clear interfaces where useful.
- Keep simulation logic separate from presentation logic.
- Keep data definitions separate from runtime state.
- Add comments for non-obvious combat logic.
- Do not hardcode all characters into the player controller.
- Do not hardcode all moves into one script.
- Do not rely on random engine physics behavior for fighting-game hit resolution.
- Do not implement online multiplayer.
- Do not implement arbitrary script execution from mods.
- Prioritize a working prototype over a huge feature list.
- Include debug tools with each combat feature.
- Every generated system should be testable with placeholder assets.
- Use bitmask input internally.
- Use readable command strings externally.
- Use enum-driven states for the MVP.
- Use 2.5D AABB combat boxes with lane/depth checks.
- Use declarative JSON cancel rules compiled into runtime data.
- Validate all custom content before loading it.
- Disable invalid content instead of crashing.

---

# 51. First Coding Task

Start by creating the Godot 4.x C# project skeleton for Project Mannequin.

Implement the following first:

## 1. `GameConstants.cs`

Defines:

- tick rate
- max players
- input buffer size
- input bitmasks
- default movement constants
- standard player colors

## 2. `GameSimulation.cs`

Central simulation coordinator.

Responsibilities:

- track current simulation tick
- register combat actors
- update input buffers
- update actor state machines
- update move execution
- update combat boxes
- resolve hits
- resolve grabs
- apply damage and stun
- update combo/juggle state
- dispatch presentation events

## 3. `LocalInputManager.cs`

Handles local player input.

Responsibilities:

- support up to 4 players
- assign devices to player slots
- poll actions once per simulation tick
- generate input bitmasks
- send inputs to each player’s input buffer

## 4. `PlayerInputBuffer.cs`

Stores recent inputs.

Responsibilities:

- store the last 60 frames of input
- expose current held buttons
- expose just-pressed buttons
- expose directional history
- expose `GetNumericDirection(framesBack, facingRight)`
- allow command lookup by the command interpreter

## 5. `CommandInterpreter.cs`

Converts readable command notation into engine checks.

Responsibilities:

- parse commands like `L`, `236S`, `214H`, `623M`
- compare command patterns against the input buffer
- support command leniency
- support facing direction correction
- return the highest-priority valid command
- avoid the bug of checking only current-frame direction while scanning history

## 6. `CombatActor.cs`

Base actor class for players, enemies, bosses, and projectiles.

Responsibilities:

- health
- meter
- facing
- velocity
- current state
- current move
- current form
- team ID
- player ID
- combat boxes
- state update entry point

## 7. `CombatStateMachine.cs`

Enum-driven state machine.

Responsibilities:

- handle transitions between idle, movement, attacks, hitstun, knockdown, form swapping, and death
- enforce cancel rules
- enforce form swap restrictions
- process hitstun/blockstun timers
- process move startup, active, and recovery frames

## 8. `CombatBox.cs`

Defines custom combat collision boxes.

Box types:

- Hitbox
- Hurtbox
- Pushbox
- Grabbox
- ProjectileBox
- ArmorBox
- WeakPointBox

## 9. `HitResolver.cs`

Processes combat box interactions.

Responsibilities:

- resolve hitbox vs hurtbox
- check lane/depth overlap
- prevent duplicate hits from the same move unless allowed
- apply damage
- apply hitstun/blockstun
- apply launch/wall bounce/ground bounce
- generate presentation events

## 10. `MoveData.cs` and `CharacterData.cs`

Define data structures that can be loaded from JSON.

Must support:

- input command
- startup frames
- active frames
- recovery frames
- damage
- hitstun
- blockstun
- cancel rules
- hitbox definitions
- projectile spawns
- meter gain
- meter cost
- character stats
- role tags
- synergy tags

## 11. `ContentManager.cs`

Loads local content packs.

Responsibilities:

- scan `UserContent/`
- discover character folders
- validate required files
- parse JSON
- report validation errors
- load safe metadata
- disable invalid content instead of crashing

The first goal is not beautiful graphics.

The first goal is a clean, expandable combat prototype where the blank mannequin can attack, read inputs, hit a test enemy, defeat a test boss, unlock the boss form, and switch into that form.

---

# 52. Final Game Identity

The game should be described as:

A 1–4 player local M.U.G.E.N-inspired 2.5D side-scrolling fighting sandbox where players begin as blank mannequins, travel through major pop culture archive-realms, defeat powerful characters and bosses, shapeshift into archived combat copies of them, inherit their abilities, build custom form loadouts, fight through co-op stages, battle in local versus modes, raid cinematic bosses, and use local creator-made content to keep the game endlessly expandable.

The game is built for personal use and local play first.

The priority is couch multiplayer, shared-screen action, readable 4-player combat, local custom content, creator testing, and chaotic M.U.G.E.N-style fun.

---

# 53. How You Should Respond

When helping me with this project:

1. Work section by section.
2. Do not build the entire game at once.
3. Prioritize the MVP combat loop first.
4. Use Godot 4.x C#.
5. Keep code modular and data-driven.
6. Explain architecture decisions clearly.
7. Provide working code in manageable chunks.
8. Include folder paths and file names.
9. Include placeholder-friendly code.
10. Include debugging tools early.
11. Ask focused questions only when a design decision blocks implementation.
12. Otherwise, make the best technical decision and continue.

Start with the MVP combat foundation:

- blank mannequin
- fixed-tick combat
- bitmask input buffer
- readable command parser
- enum-driven state machine
- 2.5D AABB combat boxes
- hitbox/hurtbox resolver
- one test enemy
- one test boss
- boss unlock
- form swapping
