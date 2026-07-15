# Sprite Source Audit

Reviewed: June 30, 2026

## Sources

- The Spriters Resource: https://www.spriters-resource.com/
- The Spriters Resource terms: https://www.spriters-resource.com/page/tou/
- Sprite Database: https://spritedatabase.net/
- Sprite Database terms: https://spritedatabase.net/terms
- MUGEN Free For All character sprites: https://mugenfreeforall.com/forum/210-character-sprites/
- MUGEN Free For All guidelines: https://mugenfreeforall.com/guidelines/
- The Simpsons arcade gameplay reference: https://www.youtube.com/watch?v=LVRKjhjtdwI

## Project Use Policy

These sites are research libraries, not blanket asset licenses.

- The Spriters Resource permits site content in legally permitted, unpublished,
  non-commercial work. Its terms prohibit commercial use of hosted content.
  Custom work still requires artist credit, and commercial use requires direct
  artist permission plus clearance of any underlying character rights.
- Sprite Database limits downloads to personal, non-commercial, transitory
  viewing and prohibits public display. Treat its files as reference-only.
- MUGEN Free For All is a sharing forum. Its community sharing policy is not a
  substitute for permission from each creator or the owner of the underlying IP.
- A Higgsfield restyle of a copyrighted sprite is not automatically an original,
  cleared asset. Do not upload a franchise sprite merely to produce a close copy.

Before importing any third-party file, record:

1. Source URL and author.
2. Original game or character rights holder.
3. License or direct permission.
4. Required credit and redistribution conditions.
5. Whether modification and AI-assisted derivatives are permitted.

Files without a clear answer stay outside the game repository.

## Design Lessons Used

The Simpsons reference demonstrates the missing arcade stage rhythm:

1. Move through a readable side-scrolling environment.
2. Lock the current camera area when enemies enter.
3. Mix enemy speeds, durability, and entry lanes.
4. Clear the group before forward movement reopens.
5. Repeat with escalation, then reveal the stage boss.

The Archive District MVP now uses that structure with six horde encounters,
seventeen minions, brief recovery beats, a final boss gate, and form inheritance.
The characters, world names, stage art, HUD art, and gameplay data remain
original Project Mannequin content.

## Future Reference Targets

- Animation timing and silhouette readability.
- Enemy archetype contrast.
- Sprite-sheet packing conventions.
- Impact effects and readable attack anticipation.
- Stage layering, props, and foreground/background separation.
- Portrait, icon, and HUD information hierarchy.
