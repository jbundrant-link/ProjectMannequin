---
name: "Project Mannequin Visual Style"
description: "Use when generating, editing, reviewing, importing, wiring, or planning Project Mannequin character sprites, enemies, bosses, stages, props, pickups, hazards, VFX, HUD, UI, menus, maps, portraits, icons, or art prompts."
applyTo: ["Assets/**", "Artifacts/**", "Docs/**/*ART*.md", "Docs/MasterPrompt.md", "Scripts/Presentation/**", "Scripts/UI/**", "Scripts/Data/**"]
---

# Project Mannequin Visual Style

- Read and follow `Docs/VISUAL_STYLE_BIBLE.md`; it is authoritative.
- Treat the accepted mannequin, Ryu, and Goku runtime art as canonical style anchors.
- Target one cohesive modern 2.5D fighting-game look: controlled dark contours, broad anime/cel-shaded value planes, clean designed highlights, simplified materials, saturated focal accents, cinematic lighting, and arcade-readable silhouettes.
- Do not approve photoreal/PBR, rusty/gritty military sci-fi, dense kit-bash microdetail, product-render loot, painterly concept drift, generic mobile-game art, flat vector art, or enterprise/dashboard UI.
- Every generated-art job uses at least two canonical character references; difficult cross-category work uses all three. An off-style asset may be supplied for structure only and must be labeled non-authoritative.
- Generate and approve one pilot before batching an asset family. Review raw art beside accepted characters before cutout, processing, import, or runtime wiring.
- Technical correctness is not style approval. Require the visual-bible score of at least 14/16, no automatic failure, explicit review metadata, and runtime captures at 1280×720 and 1920×1080.
- Keep world motifs distinct while preserving the shared rendering grammar.
- Never mark a stage or phase complete until both `Docs/ART_ASSET_COMPLETENESS_PLAN.md` and the visual-style gates pass.
