# MUGEN Reference Audit

## Reviewed Sources

- https://mugenguild.com/
- https://network.mugenguild.com/guild/start.html
- https://network.mugenguild.com/pots/

## What Is Useful

PotS provides practical references for:

- mirrored lifebar and power-meter layouts
- 1P, simul, 3P, 4P, and tag HUD variants
- stun, guard, recoverable-health, combo-damage, and round displays
- training attack-data presentation
- dramatic zoom and automatic stage-camera behavior
- tag, assist, dash-cancel, and clash system design
- screenpack flow and stage metadata
- original and AI-generated stage composition

The MUGEN Fighters Guild database and forum are discovery tools. Their presence does not grant blanket permission to reuse every hosted character, sprite, stage, sound, or franchise asset.

## Permission Rules

PotS states in the site FAQ that PotS work may be used and modified, with a preference that derivatives remain free. Each package readme must still be checked for work owned by other contributors.

The reviewed `MUGEN1 Lifebar Remix` readme identifies the package as an edit of Elecbyte and Ikemen GO lifebars and credits a third-party announcer pack. Project Mannequin therefore uses only its information hierarchy as a reference. No original SFF sprites, fonts, sounds, portraits, or announcer audio were imported.

Licensed franchise characters and stages are reference-only unless the relevant rights holder and creator explicitly permit reuse. Running artwork through an AI model does not remove those underlying rights.

## Import Policy

Every candidate asset needs a manifest containing:

- asset name and type
- creator and contributor credits
- original source URL
- download URL
- license or written permission
- modification permission
- redistribution permission
- franchise or original-content status
- files used by Project Mannequin
- derivative-generation notes

Unknown permission means reference-only.

## Technical Mapping

- `.sff` sprite archives -> offline extraction to PNG atlases
- `.air` animation definitions -> validated animation JSON
- stage and lifebar `.def` files -> declarative Godot stage/HUD metadata
- `.snd` archives -> offline conversion to supported audio formats
- `.cmd`, `.cns`, and executable logic -> reference-only; never executed

All conversion happens offline. Imported content must remain declarative and pass the existing content validator before entering the runtime.

## First Adaptation

The first adaptation is the original Project Mannequin lifebar ornament in:

```text
Assets/UI/Hud/project_mannequin_lifebar_frame_higgsfield_v1.png
```

It preserves the useful mirrored HUD hierarchy while using newly generated Project Mannequin materials, shapes, colors, and archive iconography.
