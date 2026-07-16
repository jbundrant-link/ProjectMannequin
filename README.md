# Project Mannequin

Project Mannequin is a Godot 4.7 C# prototype for a local-first 2.5D side-scrolling fighting sandbox.

## Requirements

- Godot 4.7 .NET/Mono edition
- .NET 8 SDK, 64-bit

This first skeleton focuses on the MVP combat foundation:

- fixed 60 Hz simulation
- bitmask input buffering
- command parsing for motions like `236S`, `214H`, and `623M`
- enum-driven combat states
- 2.5D AABB combat boxes
- hit resolution with duplicate-hit prevention
- test player, enemy, and boss actors
- selectable Archive District, Tournament District, and Shattered Skyway missions
- six-screen scrolling route with gated horde encounters
- staggered enemies entering from four screen edges
- arcade enemy approach, attack, retreat, and re-entry behavior
- six-button standing, crouching, and aerial normals
- cancellable ground and air combo strings with a visible combo counter
- burst dash with attack/jump cancels
- jump startup, full-height arc, air steering, and landing recovery
- standing/crouching guard rules for mid, low, and overhead attacks
- timed `MP+MK` parry with perfect-parry recoil
- counter hits, punish counters, combo damage scaling, and hit-stop
- boss Resolve gauge, guard break, and phase-three cinematic super pause
- boss-form unlock and form swapping
- fixed-tick lane-aware projectile attacks
- local content scanning under `UserContent/`
- basic debug overlay

## Run

From PowerShell:

```powershell
cd "C:\Users\Joseph Bundrant\Code Projects\SideScroller"
& "C:\Users\Joseph Bundrant\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe" --path .
```

Open the editor:

```powershell
cd "C:\Users\Joseph Bundrant\Code Projects\SideScroller"
& "C:\Users\Joseph Bundrant\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe" --editor --path .
```

The configured main scene is `res://Scenes/UI/MainMenu.tscn`. Choose Archive District, Tournament District, or Shattered Skyway from the Archive Map. In the editor, press `F6` to run the open scene or `F5` to run the configured project.

## Combat Controls

- `WASD`: move horizontally and through the lane
- `E`: dash in the held direction
- `Space`: jump
- `J`, `K`, `L`: light, medium, heavy punch
- `U`, `I`, `O`: light, medium, heavy kick
- `C` + attack: crouching version of that normal (`S` remains lane movement)
- Double-tap `A`/`D` to run (uses dash animation)
- `B`: standing block
- `S + B`: crouching block
- `K + I`: parry (`MP + MK`)
- `Q`: form swap
- `R`: assist

Ground chain example: `J`, `K`, `L`.

Launcher route: `C + L`, then `Space`, `J`, `K`, `L` in the air.

Uppercut: facing right, tap `D`, then `C`, then `C + D + L` (`623HP`).

Special example: quarter-circle forward + `J` (`236LP`). Super: quarter-circle forward + `O` (`236HK`) with full meter.

Unlocked Goku archive form: tap `C`, release, then tap `C + L` (`22HP`) to
advance through its twelve transformations.
