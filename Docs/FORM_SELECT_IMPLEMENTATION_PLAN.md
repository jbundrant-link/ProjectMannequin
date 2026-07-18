# Form Select Swap Implementation Plan

## Goal

Replace blind form-cycling with a fighting-game character-select overlay that
lets the player choose exactly which archived form to swap into during combat.

The feature should feel like an in-fight M.U.G.E.N / screenpack-style selector:
visible roster slots, cursor movement, selected-name feedback, portrait art, and
confirm/cancel flow. Combat should still use the existing deterministic form-swap
state machine once a target form is chosen.

## Research Summary

M.U.G.E.N and IKEMEN-style character select screens split responsibility across
two data layers:

- `select.def`: roster entries, ordering, hidden entries, random select, and
  optional slot grouping.
- `system.def` / screenpack motif: rows, columns, slot size, slot spacing,
  cursor graphics, small portraits, large portraits, selected names, sounds, and
  screen layout.

Useful concepts for Project Mannequin:

- Keep the roster data separate from the visual layout.
- Use rows/columns/slot spacing as explicit layout settings rather than hardcoded
  per-form positions.
- Show small portraits in the grid and a larger selected-form preview.
- Highlight the current form and prevent selecting unusable forms.
- Support grouped slots later, similar to IKEMEN's multi-character `slot = {}`
  behavior, for forms/variants that share a base character.

Import policy:

- M.U.G.E.N layouts and screenpacks are reference-only unless explicit permission
  exists for specific assets.
- Runtime implementation must use original Project Mannequin UI art or generated
  portraits made for this project.
- No `.sff`, `.snd`, screenpack sprites, franchise portraits, or creator assets
  should be imported directly without a manifest and permission check.

## Current Project Hooks

The combat side is already close to the desired target:

- `Scripts/Combat/CombatStateMachine.cs`
  - Current `InputButtons.FormSwap` behavior cycles to the next equipped form.
  - `TryStartFormSwap(string targetFormId, int tick)` already accepts an explicit
    target form ID.
- `Scripts/Progression/FormArchive.cs`
  - `UnlockedForms` stores every archived form.
  - `ActiveLoadout` stores the currently usable form slots.
  - `CanUse(formId)` already gates valid swap targets.
- `Scripts/Combat/CombatActor.cs`
  - `CurrentForm`, `SetForm`, and `FormArchive` already own actor form state.
- `Scripts/Input/LocalInputManager.cs`
  - `Q` maps to `p1_form_swap` and `InputButtons.FormSwap`.
  - Keyboard and gamepad polling already produce directions and confirm buttons.
- `Scripts/UI/PauseMenu.cs`
  - Existing example of a `CanvasLayer` using `ProcessModeEnum.Always` and
    pausing the tree.
- `Scenes/Main.tscn`
  - Existing UI root already includes `MvpHud`, `DebugOverlay`, and `PauseMenu`.
    A new `FormSelectOverlay` can be added here as another `CanvasLayer`.

Key implementation warning:

- UI input consumption alone will not stop the current automatic cycle, because
  combat input is polled through `LocalInputManager`. The auto-cycle block in
  `CombatStateMachine` must be removed, gated, or replaced with a request event
  so `Q` opens the selector instead of immediately swapping.

## Proposed Runtime Design

Add a new in-fight selector node:

```text
Scripts/UI/FormSelectOverlay.cs
Scenes/Main.tscn -> FormSelectOverlay CanvasLayer
```

Responsibilities:

- Find the local player actor from `GameSimulation.Actors`.
- Build selectable entries from `player.FormArchive.ActiveLoadout` for the MVP.
- Open when the player presses `Q` / `FormSwap` and at least two forms are usable.
- Pause or selection-freeze the game while open.
- Move a cursor across a grid using directions.
- Confirm the selected form using `Enter`, `J`, or controller accept.
- Cancel with `Q`, `Escape`, or controller cancel.
- On confirm, close the overlay, unpause, and call:

```csharp
player.StateMachine.TryStartFormSwap(selectedForm.Id, simulation.CurrentTick);
```

Initial visual structure:

- Full-screen dim wash over the current fight.
- Compact select grid centered or right-centered.
- Current form slot highlighted.
- Selected form slot gets a bright cursor frame.
- Large selected preview panel on the left.
- Form name, role, tags, health, movement, and a short command hint.
- Empty cells rendered as dark inactive slots for a M.U.G.E.N screenpack feel.

## Data Model Plan

Phase 1 can avoid data changes by deriving portraits from sprite sheets.

Phase 2 should add explicit portrait metadata:

```csharp
public string SelectPortraitPath { get; set; } = "";
public string SelectLargePortraitPath { get; set; } = "";
public string SelectPaletteColor { get; set; } = "";
```

Potential follow-up type:

```csharp
public sealed class FormSelectSlotData
{
    public string FormId { get; set; } = "";
    public bool HiddenUntilUnlocked { get; set; }
    public string GroupId { get; set; } = "";
}
```

Potential screenpack-style config:

```csharp
public sealed class FormSelectLayoutData
{
    public int Columns { get; set; } = 4;
    public int Rows { get; set; } = 2;
    public Vector2 SlotSize { get; set; } = new(92, 92);
    public Vector2 SlotSpacing { get; set; } = new(10, 10);
}
```

Do not add these data types until the overlay is playable. Keep the first slice
small and driven by `ActiveLoadout`.

## Asset Plan

MVP derived portraits:

- Add a tool to crop each form's idle frame into UI portraits.
- Output paths:
  - `Assets/UI/FormSelect/<form_id>_small.png`
  - `Assets/UI/FormSelect/<form_id>_large.png`
- Use transparent PNGs with consistent framing and no external dependencies.
- Generate from the active sprite sheet and idle frame for:
  - blank mannequin
  - Archive Knight form
  - Ryu form
  - Goku archive form

Polished portrait pass:

- Generate original character-select portraits in a unified Project Mannequin
  style.
- Use the current form sprite sheets as references for costume and silhouette.
- Keep portraits visually distinct from copied M.U.G.E.N or franchise assets.
- Add a lightweight import manifest if any external reference is used.

Screenpack UI assets:

- Cursor frame texture or procedural neon border.
- Empty slot background.
- Current-form badge.
- Locked/unavailable badge for later phases.
- Optional animated background grid or scanline shader.

## Phased Implementation

### Phase 0: Code Path Cleanup — DONE

Objective: prevent blind cycling from fighting the selector.

Implemented in `Scripts/Combat/CombatStateMachine.cs`:

- Removed the `GetNextEquippedForm` auto-cycle from the neutral input handler.
  Pressing `FormSwap` no longer swaps in combat logic; the overlay will own it.
- Added `RequestFormSwap(string targetFormId)` as the single UI-facing choke
  point. It stores a pending target that is consumed at the top of `Update`
  inside the fixed-tick loop, so the swap stays deterministic and the UI never
  needs the current tick or to mutate combat state directly. Same-form requests
  are dropped.
- Added `CanOpenFormSelect` (public) = not on cooldown, in a swap-ready state,
  and `ActiveLoadout.Count >= 2`. Refactored the shared state check into
  `IsFormSwapReadyState` so `CanSwapForm` and `CanOpenFormSelect` cannot drift.
- `TryStartFormSwap(targetFormId, tick)` remains the single place a swap starts;
  `GetNextEquippedForm` is retained (unused) for a possible future quick-cycle.

Acceptance (met):

- Pressing `Q` no longer instantly cycles forms through combat logic.
- Existing direct calls to `TryStartFormSwap` still work in tests or debug code.
- `dotnet build` clean: 0 warnings, 0 errors.

### Phase 1: Playable Selector Overlay — DONE (pending playtest sign-off)

Objective: ship a functional selector with placeholder/derived portraits.

Implemented:

- Added `Scripts/UI/FormSelectOverlay.cs` (CanvasLayer, `ProcessMode = Always`,
  `Layer = 25`). Text-only slots for Phase 1; portraits come in Phase 2.
- Wired into `Scenes/Main.tscn` as a sibling CanvasLayer (`13_formselect`,
  `SimulationPath = ../GameSimulation`).
- Resolves P1 from `GameSimulation.Actors`; builds a grid from
  `player.FormArchive.ActiveLoadout`, starting the cursor on the current form.
- Input handled in `_Input` (not `_UnhandledInput`) so the overlay is modal and
  swallows keys before `PauseMenu`/`MvpHud` can react — this avoids the
  double-pause conflict with the Escape key. `ui_cancel` (Esc) and `p1_form_swap`
  (Q) both cancel; `p1_lp` (J) / `p1_start` (Enter) confirm; `p1_left/right/up/down`
  (WASD) move the cursor.
- Opens only when `!GetTree().Paused` and `StateMachine.CanOpenFormSelect` and
  loadout >= 2. Pauses the tree while open; confirm calls
  `StateMachine.RequestFormSwap(id)` then unpauses. Selecting the current form is
  a no-op cancel.
- Left panel previews the highlighted form: name, role, HP/walk/dash, role tags.

Verified:

- `dotnet build` clean (0/0).
- Headless load of `Main.tscn` shows no runtime/script errors (overlay UI builds).

Known Phase 1 limitations at implementation time:

- Opening + cursor navigation were keyboard-only. This was subsequently resolved
  by the shared `UiInputRouter` and P1 device-assignment work; D-pad parity now
  exists, with left-stick modal navigation still subject to final QA/policy.
- Slots are text-only until the Phase 2 portrait pipeline.

Acceptance (to confirm in playtest):

- With multiple forms unlocked, pressing `Q` opens the selector.
- The player can choose Ryu, Goku, Archive Knight, or mannequin if they are in
  the active loadout.
- Confirming starts the existing form-swap animation into that exact form.
- Canceling returns to combat without swapping.
- Current form is visibly identified and cannot accidentally be selected as a
  no-op unless we intentionally allow it as cancel-equivalent.

### Phase 2: Derived Portrait Pipeline — DONE (runtime crop; pending playtest)

Objective: give every selectable form a stable visual identity in the grid.

Implemented `Scripts/UI/FormSelectPortraitResolver.cs` (cached per form id) with
the planned priority chain:

1. `CharacterData.SelectPortraitPath` (new field) — explicit hand-drawn override.
2. Derived asset `res://Assets/UI/FormSelect/<id>.png` — for a future offline pass.
3. Runtime idle-frame crop — loads the form's sprite sheet when no explicit art
   exists, or generates the mannequin sheet through
   `ProceduralMannequinSpriteSheetFactory` as a final fallback. It crops cell
   (row 0, col 0) to the alpha bounding box (with padding) and returns an
   `AtlasTexture`; if pixel data cannot be read it falls back to the full idle cell.

The blank mannequin and Archive Knight boss/inherited form now use explicit
style-approved illustrated portraits, so their matchup and form-select cards no
longer depend on the procedural/runtime crop fallback.

Overlay updated: each slot shows a portrait thumbnail above the name/role, and the
left preview panel shows a large portrait for the highlighted form.

Deviation from the original task list (intentional): the runtime crop replaces the
offline crop tool + committed PNGs for the MVP. This matches the project's
"procedural/runtime-first, always available" convention, needs no committed
binaries, and covers every form (including the procedural ones the offline tool
could not). The explicit/derived path branches remain so committed or hand-drawn
portraits can drop in later with no code change — that offline tool is deferred to
the Phase 3 polish pass if we want hand-tuned/head-framed art.

Verified:

- `dotnet build` clean (0/0).

Caveats to confirm in playtest:

- Portraits are full-figure idle crops (head-to-feet), not head-framed close-ups.
- First open of a form with a very large sheet (Ryu v4 is 5120x4800) does a
  one-time image decompress for the bbox scan; result is cached thereafter.

Acceptance (to confirm):

- Every active loadout form has a visible small portrait and selected preview.
- Portraits use consistent scale, framing, and transparent background.
- Missing portrait assets degrade cleanly to labels or generated placeholders.

### Phase 3: Screenpack-Style Layout Polish — DONE (pending playtest)

Objective: make the selector feel intentionally fighting-game-like instead of a
plain pause menu.

Implemented (all in `Scripts/UI/FormSelectOverlay.cs` + resolver):

- Fixed grid with empty slots: `Columns` (default 3) x `Rows` (default 2) sized
  for the 1152x648 window; forms fill the first cells, remaining cells render as
  dark inactive slots for the screenpack look.
- Pulsing cursor: the selected slot's border animates via `_Process` (runs while
  paused since the node is `Always`), lerping cyan brightness on a sine.
- `EQUIPPED` badge: the current form's slot shows a styled pill instead of plain
  text; non-current slots show the role.
- Bust-framed slot portraits: the resolver gained a `bust` option that crops the
  idle bbox to a head-and-shoulders slice (cached separately); slots use the bust,
  the preview keeps the full figure.
- Title accent bar under "SELECT FORM"; dim raised to 0.82 alpha for readability
  over bright stages.
- Asset-free selection sounds via `ProceduralAudioFactory.CreateStinger` on open /
  move / confirm / cancel, played through an `Always` `AudioStreamPlayer` on the
  "SFX" bus so they sound while the tree is paused.

Verified:

- `dotnet build` clean (0/0).
- Headless load of `Main.tscn` clean (exercises `BuildAudio` stinger creation).

Deferred from this phase at implementation time:

- Honeycomb / diagonal slot placement (rectangular grid is stable and readable).
- Gamepad navigation parity was completed later through `UiInputRouter` and P1
  device selection; left-stick navigation remains a separate UX decision.

Acceptance (to confirm in playtest):

- The overlay reads as a character-select screen at a glance.
- Text fits at 720p and common desktop sizes.
- The selector remains readable over bright stages.

### Phase 4: Loadout And Unlock Expansion — DONE

Objective: support more than the active combat loadout without losing clarity.

Decision: the in-combat selector stays **`ActiveLoadout`-only** (already enforces
swap rules); browsing the full archive and choosing which forms are equipped now
lives in a **pause-menu Form Loadout editor**, matching "editing from pause/hub,
not the in-combat selector."

Implemented:

- `FormArchive.Unequip(formId)` — removes a form from the active loadout, keeping
  at least one equipped. (`TryEquip` already caps at `min(ActiveFormLimit, MaxPlayers)`.)
- Pause menu "Form Loadout" button + panel (`Scripts/UI/PauseMenu.cs`): lists every
  archived form with a bust portrait (reuses `FormSelectPortraitResolver`), its
  state (`EQUIPPED / EQUIPPED - ACTIVE / ARCHIVED`), and an Equip/Unequip button.
  Equip is disabled when the loadout is full; Unequip is disabled for the active
  form and when only one form remains. A hint shows `N/limit equipped`. Panel is
  mutually exclusive with the move-card and move-list panels.

This satisfies the acceptance criteria: in-combat selection can't bypass loadout
rules (unchanged), the editor communicates equipped/archived/active state without
clutter, and equipping is confined to the pause menu.

Completed in a later cross-cutting input slice:

- **Gamepad navigation parity**: `LocalInputManager` now supports an assigned P1
  joypad with persisted preferences, while `UiInputRouter` routes the selected
  device through form-select and other modal UI. D-pad navigation is implemented;
  left-stick modal navigation remains a final UX/QA decision.

Still deferred:

- **Grouped slots** for variant families: still deferred per the plan until more
  than four forms are routinely available.

Verified:

- `dotnet build` clean (0/0).
- Headless load of `Main.tscn` clean (builds the Form Loadout panel).

Acceptance (to confirm in playtest):

- In-combat selection never lets the player bypass active-loadout rules.
- Locked/unusable slots communicate their state without clutter.
- Gamepad and keyboard navigation behave consistently.

### Phase 5: Multiplayer And Replay Safety — DONE (core) + documented deferrals

Objective: make the feature deterministic and ready for future local co-op.

Implemented:

- Headless smoke test `Scripts/Debug/FormSelectTests.cs` (env
  `PROJECT_MANNEQUIN_FORM_SELECT_TEST=1`, dispatched from `GameSimulation._Ready`).
  Drives the swap state machine directly (no UI) and asserts: selector opens with
  3 equipped forms; the next equipped form is Ryu; a swap to the explicit **non-next**
  form (Goku) is applied; a swap to a **non-equipped** form is rejected. Result:
  `SUMMARY passed=True (4/4)`.
- Replay guard: `GameSimulation.IsReplayPlayback` + the overlay refuses to open
  during replay playback, so a live UI swap can't diverge from the recorded masks.

Why the determinism criterion holds today: replays are enabled only via env vars
(debug/CI). The determinism harness records/plays **headlessly with no UI**, so it
never performs an overlay swap — recorded masks fully determine state and
fingerprints match. The overlay guard closes the only interactive gap.

Deferred with a ready-to-implement path (debug-only value, kept out to avoid adding
surface/risk to the determinism tooling):

- **Replay _reproduction_ of interactive UI swaps.** The `ReplaySerializer` parser
  ignores unknown line types, so this is backward-compatible: add a
  `ReplayFormSwap(tick, playerId, formId)` list to the recorder/recording, emit
  `swap <tick> <playerId> <formId>` lines, capture `FormSwapStarted` presentation
  events during recording, and inject `RequestFormSwap` at the recorded tick during
  playback (before the actor update loop). No version bump needed.
- **Multiplayer per-player overlays + co-op sim-freeze.** Local co-op is out of MVP
  scope. The overlay already targets P1 unambiguously; for co-op it would need
  per-player overlay instances/cursors and a simulation-freeze (rather than full
  `GetTree().Paused`) so other players keep acting. P1 gamepad selection now
  exists, but per-player UI ownership and independent cursors are still required.

Acceptance:

- Replay fingerprints stay deterministic for the headless harness (no UI swaps),
  and interactive playback can't be perturbed (overlay guarded). Full replay
  reproduction of UI swaps is specced above.
- Future player slots can open independent selectors once per-player instances are
  added; the current single overlay targets P1 without global ambiguity.

Verified:

- `dotnet build` clean (0/0).
- `FormSelectTests` SUMMARY passed=True (4/4) headless.

## Validation Plan

Build checks:

```powershell
dotnet build ProjectMannequin.csproj -nologo
```

Manual checks:

- Unlock at least three forms.
- Press `Q` while idle, walking, dashing, and jumping.
- Select a form that is not the next form in loadout order.
- Confirm that the chosen form is the one applied after form-swap active frames.
- Cancel the selector and verify no meter, cooldown, or form state changes.
- Try opening while dead, attacking, blocking, hitstunned, and during reward
  screens; verify it does not open in illegal states.

Smoke-test candidates:

- A world-warrior smoke scenario that unlocks Ryu, opens form select, chooses
  Ryu by target ID, and asserts `CurrentForm.Id == "world_warrior_ryu_form"`.
- An astral smoke scenario that unlocks Goku and selects it from a non-adjacent
  loadout slot.
- A UI smoke that confirms portrait resolver fallback works when a portrait is
  missing.

## Risks And Decisions

Open decisions:

- Should the selector pause the entire tree or freeze only simulation?
- Should `Q` tap open the selector and `Q` hold quick-cycle later, or should all
  swapping go through the selector?
- Should in-combat selection show only `ActiveLoadout` or all unlocked forms?
- Should selecting the current form cancel, close, or restart the swap effect?

Likely first decisions:

- Show only `ActiveLoadout` in Phase 1.
- Pause the tree while the overlay is open, matching `PauseMenu` behavior.
- Treat selecting the current form as cancel/no-op.
- Do not add grouped slots until more than four forms are routinely available.

Main technical risk:

- Combat input polling can still see `FormSwap` even if UI handles a key event.
  The state machine auto-cycle must be removed or guarded before the overlay is
  enabled.

Main art risk:

- Cropped idle sprites will be readable but not as expressive as character-select
  portraits. This is acceptable for Phase 1, but the polished version needs a
  dedicated portrait pass.

## Suggested First PR Slice

1. Disable blind form cycling in `CombatStateMachine`.
2. Add `FormSelectOverlay` with text-only slots and confirm/cancel.
3. Add it to `Main.tscn`.
4. Build and manually verify exact-target swapping.
5. Add derived portraits in the next slice.

This creates the gameplay behavior first, then lets the visual treatment improve
without blocking the core feature.
