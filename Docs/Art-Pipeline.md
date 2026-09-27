# Art Pipeline — the HD-2D look, asset volume and tooling

> Owns: how every visual asset is produced, imported, validated and budgeted - roadmap **G11** (real art pipeline
> undefined) and **G12** (device perf budgets).
> Parent: `Architecture.md` (§6 decision log), `Roadmap.md` Step 20, `Content.md` (zones/enemies), `UI-UX.md` (screens).
> **Location:** `Docs/` (folder index: `README.md`). **Last verified:** 2026-09-26 (new).
> **Status: design for approval.** Nothing in §4-§8 is built. §9 lists the decisions that must be made first.

---

## 0. The insight this document is built on

Octopath Traveler's look is delivered by a **camera that moves through hand-authored 3D dioramas**. An idle game's
camera **never moves**: the player stares at one fixed composition for hours.

Building "3D world + free camera" therefore buys the most expensive part of that look (authored 3D levels, camera
work, occlusion design) and uses none of it. Everything static can be **baked**. That is a 10-50x cost lever on the
single largest line item in an HD-2D budget.

> **Rule 1 — Bake what is static. Spend the money on the sprites, the one composition, and the post stack.**
>
> **Rule 2 — Buy variety with parameters, not with new assets.** One authored thing must become many shipped things.

An idle RPG's content volume (8 zones x 4 enemy archetypes x variants x icons x VFX) is only affordable through Rule 2.

---

## 1. What "HD-2D" decomposes into (five layers, five production methods)

| # | Layer | What it is | Volume | Per-unit cost | Multiplier |
|---|---|---|---|---|---|
| **L1** | Characters + enemies | flat 2D art, **lit**, standing in a 3D frame | high | **high** | palette swap, tint, scale, gear overlay |
| **L2** | Backdrop diorama | the "3D-ness": ground, cliffs, props, water | per zone | medium, **once per kit** | palette + light + fog + camera preset |
| **L3** | Post + grade identity | bloom, tilt-shift DOF, vignette, warm grade, grain, sprite rim/outline | **one-time** | low | per-biome profile |
| **L4** | VFX | hit sparks, flame, dust, damage numbers | low | low | event-driven |
| **L5** | UI + icons | chips, frames, currency/upgrade icons | medium | **near-zero** | procedural generator |

The mistake to avoid: treating L2 as the main event. In this project L1 + L3 produce most of the perceived style,
and both are cheap to iterate; L2 is where the volume and the schedule risk live, so L2 is where automation goes.

---

## 2. Volume math: ~150 authored assets -> ~1500-2000 shipped

The "thousands of assets" requirement is met by combination, not by authorship.

| Category | Authored for v1.0 | Shipped instances | How the multiplier works |
|---|---|---|---|
| Backdrop kits | 3 kits (~35 props each) | 8-12 zone frames | prop kit + palette + light + fog + camera preset |
| Enemy units | ~12 sprite sheets | 30-40 units | `Content.md`: 6 archetypes x 3-4 variants per zone |
| Heroes | ~12 sheets | 12 heroes + skins | palette + gear overlay + VFX tint |
| Icons / UI | 1 procedural generator | 40-60 icons | name + shape + colour tables |
| Post presets | 5 | every zone's identity | per-biome volume overrides |
| VFX | ~8 sheets/shaders | all combat feedback | event -> cue mapping (mirrors `AudioDirector`) |
| **Total** | **~150** | **~1500-2000** | |

Consequence for the roadmap: a **new zone = 1 backdrop preset + 1 post preset + 3-5 enemy sprites**. That is the
weekly content cadence an idle RPG needs, and it is the thing this pipeline must be able to produce.

---

## 3. Right tool per layer (external tools are expected, not a fallback)

| Layer | Tool | Why this tool | Output into the project |
|---|---|---|---|
| L1 sprites | **external image model** with reference-image consistency + **Aseprite** for cleanup/palette/export | 2D art is the medium AI is actually good at, and a human can judge quality in seconds; frames stay consistent via a locked reference + palette | PNG sheet + a JSON sidecar (frame rects, pivot, palette) |
| L2 props | **MagicaVoxel** (free) for hero props; **procedural Unity meshes** for bulk (rocks, foliage, crystals); **CC0 kits** (Kenney/Quaternius) as filler | human-authored voxels give instant visual feedback - exactly what text-to-coordinate lacks; procedural covers repetition for free | `.vox`/`.obj` -> merged mesh + palette material |
| L2 terrain layout | **Unity Editor generator** driven by a per-zone spec | 100+ stages cannot be hand-placed; this is the project's existing `spec -> generator -> validator` culture | diorama prefab per zone |
| L3 post | Unity URP **Volume profiles** + **one** custom sprite shader (cutout + `GetMainLight` + shadow caster + rim) | zero per-asset cost, largest visual return; the shader is what makes flat sprites belong in a lit 3D frame | `VolumeProfile` per biome + `HD2D/SpriteLit` |
| L4 VFX | shader-driven + small sheets; TextMeshPro (already in project) | feedback is what an idle game lives on | event -> cue map |
| L5 icons | **procedural generator** (`PlaceholderSpriteGenerator` already does this) | marginal cost ~0 | PNG + atlas |
| Cross-cutting | **Editor import normaliser + validators + provenance manifest** | at 1000+ assets, drift is the default outcome; this is the thing that stops it | see §4-§5 |

Explicit non-tool: **text-to-coordinate 3D generation for hero assets.** A blind text -> voxel-matrix pipeline was
attempted on 2026-09-25/26 and produced a shape that did not read as its subject. It is kept only for **repeatable
props** (rocks, veins, ruins) where a parameterised generator with a seed gives free variety, never for characters,
weapons or boss silhouettes.

---

## 4. Pipeline architecture

```
zone.json  ──►  Editor: DioramaBuilder  ──►  diorama prefab + Volume profile + camera framing
  id, biome        prop grid (rows of prop ids)
  propGrid[]       palette / light / fog preset
  postPreset       enemy ids + variant params
  enemies[]

enemy id + variant  ──►  EnemyVariantResolver  ──►  sprite + palette + scale + overlay + VFX tint
hero id             ──►  HeroVariantResolver   ──►  same, plus gear overlays

Battle page = ONE static composition
  [ baked backdrop (L2+L3) ]  <- cheapest: a pre-rendered image that already carries DOF/bloom depth cueing
  [ lit sprite billboards    ]  <- L1, runtime, the only things that need real lighting
  [ VFX + damage numbers     ]  <- L4, always on top
  [ HUD (uGUI overlay)       ]  <- L5, unchanged from today
```

Two consequences worth naming now:

1. **The battle board stays uGUI.** The HUD, HP bars and damage numbers already work and are already pooled. The
   3D-ness lives *behind* them in a backdrop layer. This is the cheapest integration and it keeps the existing
   `HeroUnitView` / `FormationBoardView` contracts. (§9.5 is the alternative if you want real 3D parallax.)
2. **Everything visual is data.** A zone is a spec file; a variant is parameters. No new scene wiring, no new
   prefab per enemy - matching AD9 ("features are data, not code").

---

## 5. Import + preset policy (the rules that stop 1000 assets becoming chaos)

| Asset kind | Rule |
|---|---|
| Pixel/painted sprites | PPU **100** (matches existing placeholders), **pivot = bottom-centre** (feet on the ground), sprite mode single; filter mode is an **open decision** (§9.1) |
| Sprite atlases | 1 page per biome/zone, <= 2048x2048, packed at build; no per-sprite textures shipped |
| 3D props | pivot = **base centre**, 1 unit = 1 m, authored at final scale, merged into one mesh per prop, one shared material per palette entry |
| Voxel props | **never** one GameObject per voxel at runtime (measured: 180 voxels = 180 renderers); bake to a merged mesh |
| Materials | URP/Lit only (this project is URP - `Standard` renders magenta); hex colours are sRGB and must be converted with `.linear` before use as `_BaseColor` |
| Naming / ids | `zone_<biome>_<n>`, `enemy_<archetype>_<variant>`, `hero_<name>`, `prop_<kit>_<name>`, `vfx_<event>`, `ico_<name>` |
| Provenance | every asset carries `{ id, source tool, licence, date, palette, author }` in a manifest - required for the store/legal pass (G15) and for replacing generated art later |
| Validators | `Tools > Idle RPG > Art > Validate Art Pipeline` runs: **preset drift** (wrong filter/PPU/mipmap/compression), **budget** (draw calls/tris/texture MB on the battle page), **manifest** (missing sprite id, missing provenance) |

---

## 6. Performance budgets (G12) — provisional, to be measured on device

| Metric | Budget | Why this number |
|---|---|---|
| Battle page draw calls | <= 40 | backdrop 1-3 (merged) + <= 6 sprites + VFX <= 6 + HUD <= 6 batches |
| Battle page triangles | <= 60k | enough for a merged diorama at this scale, trivial for quads |
| Heavy runtime post effects | **max 1** on mobile (bloom **or** DOF) | bloom + Bokeh DOF together is the biggest GPU line item on mid-range phones |
| Texture memory (resident) | <= 64 MB total; sprites <= 24 MB; backdrop <= 16 MB | two 2048x2048 pages worst case |
| Atlas pages | 1 per biome | keeps batching sane and memory predictable |
| Presentation CPU | <= 2 ms/frame, **zero** per-frame allocations in the view layer | existing project rule; idle games run for hours |
| Frame rate | 60 fps idle / 30 fps battery mode | B11 |
| Asset throughput | >= 5 enemy variants per day; 1 new zone look per week | this is the number that decides whether content keeps shipping |

Lands in Step 20 alongside B11/B12; the budget validator in §5 enforces it mechanically.

---

## 7. Design rules for an idle HD-2D

1. **Readability contract.** Backdrop graded *below* mid value; sprites and their rim/outline *above* it; HUD above
   everything; damage numbers always on top. Verify at ~30% screen brightness (players play outdoors).
2. **Fixed composition.** Portrait 1080x1920, characters in the bottom third, tall props for vertical depth.
   Compose once, then never move it.
3. **Repetition tolerance.** This frame is watched for thousands of hours: subtle idle motion only. The backdrop
   swap is the reward for a new zone.
4. **Depth without cost.** One blurred foreground prop in front of the battle line + a value-graded backdrop reads
   as 2.5D and costs zero gameplay surface.
5. **Per-zone payoff beats per-enemy variety.** A zone drop = backdrop + palette + post preset + 3-5 enemy sprites.
   That is a sustainable weekly cadence; per-enemy bespoke art is not.

---

## 8. Phases and gates

| Phase | Deliverable | Gate (your judgement) | Budget |
|---|---|---|---|
| **A** | vertical slice frame: locked portrait camera, crude diorama, 1 hero + 1 enemy, full post stack, `HD2D/SpriteLit` | "**that's the look**" - one screenshot worth a store page | 1-2 sessions |
| **B** | both production loops proven end-to-end: sprite (generate -> import preset -> atlas -> lit billboard) and prop (MagicaVoxel -> normalise -> placed) | you can make **10 sprites + 3 props in an afternoon without me** | 2-3 sessions |
| **C** | the multipliers: zone spec -> diorama builder, variant resolvers, biome post presets, baked backdrop | a whole new zone look from a JSON file in **< 1 hour** | 3-4 sessions |
| **D** | volume ramp: throughput measurement, hero-vs-bulk quality tiers, icon generator, manifest + validators | 20 assets through the pipeline with **zero preset drift** | 2 sessions |
| **E** | device budget: mobile lighting mode, atlas/memory audit, 60 fps, battery mode | measured in budget on a real device | 1-2 sessions |

Gate rule (standing rule 1): **no phase starts until the previous gate is a human "yes"**. Phase A is the only phase
that matters until it passes - everything else is execution.

---

## 9. Open decisions (needed before Phase A starts)

- [x] **9.1 Sprite filter mode** - **Point** (used and verified with the Tiny RPG pack). Resolution of the test art is far
      below HD-2D scale, so "crisp vs smooth" still matters when real art lands; provisional.
- [ ] **9.2 Backdrop strategy** - (a) fully baked 2D image (cheapest, camera-locked by definition);
      (b) a real camera rendering a static 3D diorama (parallax/tilt possible, more cost);
      (c) **hybrid: baked background + a few real 3D foreground props** - recommended.
- [ ] **9.3 Prop source mix** - MagicaVoxel for hero props + procedural for bulk + CC0 for filler. Confirm, or pick one.
- [x] **9.4 Character animation** - **sprite frame strips + a parameter-driven AnimatorController** (tested with the Tiny
      pack: Idle/Walk/Attack01-03/Hurt/Death; the sim can drive it with triggers and an int variant, no clip knowledge needed).
- [ ] **9.5 Battle page integration** - keep the uGUI board with the backdrop behind it (**recommended**), or move
      the board into world space.
- [ ] **9.6 Style anchor** - commit to **one** professional key-art frame every generated asset references.
      Strongly recommended: cheapest defence against style drift across 40 generated sprites.
- [x] **9.7 Vision calibration** - partially settled (ArtLab): I can read ASCII renderings and measure pixel data
      reliably, and I cannot judge aesthetics. Not needed further for this spike.
- [x] **9.8 Camera fixity** - the game camera never moves, so **no billboard script is required**; sprites are authored
      once facing the camera (owner decision, ArtLab).

---

## 10. Explicitly rejected

- A free-roaming 3D camera, or characters that walk anywhere. It is an idle game.
- Hand-placed 3D levels per stage - 100+ stages exist only as data.
- Runtime Bokeh DOF on mobile when a static backdrop can carry a **baked** tilt-shift.
- Text-to-3D generation for hero assets (characters, weapons, bosses).
- One GameObject per voxel at runtime; bake merged meshes instead.
- 16:9 landscape composition logic in a 1080x1920 portrait game.
- Any look that costs readability: bloom and dark dioramas must never fight HP bars or damage numbers.

---

## 11. Risks

| Risk | Why it bites | Mitigation |
|---|---|---|
| Style drift across generated sprites | 40 enemies from 40 prompts look like 40 different games | locked reference + fixed palette + one shared rim/outline shader + the style anchor (§9.6) |
| Post-processing cost on mobile | bloom + DOF is the largest GPU line item on mid-range devices | bake DOF into the backdrop, one heavy effect maximum, budget validator |
| Diorama authoring bottleneck | 8 zones of hand-built 3D is the schedule | prop kit + generator (Phase C); palette-only variants ship first |
| Provenance / licence for generated art | blocks store review (G15) and blocks swapping art later | manifest from day one, licence recorded per asset |
| Readability loss | idle players watch for hours, often in daylight | value-separation contract (§7.1) + brightness test |
| My visual judgement | measured 2026-09-26: I cannot reliably judge a rendered image | human approves every frame; §9.7 calibration; text-mode previews for silhouettes |

---

## 12. Note on the reverted attempt

A blind voxel-generator attempt (2026-09-25/26) was reverted at the owner's request. It was never committed, so the
files are gone. The lessons are kept here instead of in code: the layer/palette/JSON authoring format, the URP
material rules in §5, and one measured fact worth remembering - 180 voxels = 180 renderers = **+5 draw calls** under
the SRP Batcher. Batching was never the problem. Authoring the *shape* is the hard part, and that is a human/visual
job, which is exactly why this document puts a human at every gate.

---

## 13. ArtLab spike - verified outcomes (2026-09-26)

Verified mechanically, not by taste:

| Claim | Result |
|---|---|
| URP stock sprite materials cannot cast or receive shadows in this renderer | **Proven.** `HD2D/SpriteLit` adds Forward + ShadowCaster + DepthOnly passes. Sprite region lit `(68,54,81)` vs unlit `(33,34,60)` when the key light toggles; cast-shadow toggle changed ~11k px at the ground contact area only |
| `SpriteRenderer.flipX` needs no custom shader support | **Proven.** Unity flips the sprite mesh itself; a clean mirror, no transform trick needed |
| PPU must derive from the ART, not the frame cell | **Proven.** 21 px character inside a 100 px cell: full-cell PPU made an 8-unit quad; art-derived PPU 12.9 gave the intended 1.7 m |
| Padded cells must be trimmed per animation strip | **Proven.** One union rect per strip = one shared rect per animation (no walk-cycle jitter), feet pivot at the bottom |
| Frame strips -> clips -> parameter controller | **Proven.** 13 clips + 2 controllers generated; frozen frames at normalized 0.45 vs 0.85 differ by 71k px |
| "with shadows" pack variants | **Skipped by the importer** - they bake a fake drop shadow; the scene casts real ones |
| Camera fixity | This game's camera never moves, so **no billboard script is required** (owner decision): sprites are authored once, facing the camera |
| Budget, ArtLab frame (play mode) | **~50 draw calls, 40 set passes, 5.9k triangles** for ground + one prop + two lit shadow-casting sprites + bloom/vignette/ACES. Batching room exists; measure the real battle page in Phase E |

Open issues carried from the spike:

- The Tiny RPG pack has **no licence file**. Source + licence must be recorded before it can ship or be committed.
- **Art resolution**: 21 px characters render at ~36 screen px per art pixel at this framing - deliberately chunky.
  HD-2D wants roughly 2-4x upscale. Decision: accept a chunky style, or source higher-resolution art for the real game.
- `TextureImporter.spritesheet` is removed in Unity 6 (only a warning so far); the import tool must migrate to
  `ISpriteEditorDataProvider` before it can be relied on.
- The ArtLab scene keeps the working rig: ground, one 3D prop, `Hero_Soldier` + `Enemy_Orc` on `HD2D/SpriteLit` with
  Animator controllers, a PostFX volume, and a portrait fixed camera.

---

## 14. PixelLab pilot plan (2026-09-26) — status: DONE, see §16 for the locked + animated result

PixelLab (`pixellab.ai`) is under evaluation as the AI generation partner: pixel-art game assets with sprite-sheet
output, an official MCP server (`pixellab-code/pixellab-mcp`, HTTP transport at `https://api.pixellab.ai/mcp` with a
Bearer token) and an HTTP API. Free tier = **40 generations** - this is the audit budget.

**Standing rules (the pilot is run on these, not enthusiasm):**

- Every generation is logged against the 40; the probe and all downstream processing cost 0.
- A request that hangs is abandoned, never waited on (background-removal steps have a reported stall trend).
- Garbage output is shown to the owner BEFORE any regeneration is spent.
- The remaining budget is reported at the end of every pass.

**Owner-scoped sequence (2026-09-26):**

1. Probe (0 gen): authenticate via `initialize` + `prompts/list`. `tools/list` is SKIPPED - it has a known regression
   (hangs at ~17,891 bytes; open issue on `pixellab-code/pixellab-mcp`).
2. Generate the **goblin only** (1 gen) via `create_character`.
3. Lock the sprite (owner approval).
4. Add animations **only after the lock**, in a later pass (idle first, then attack).
5. Backdrop / background decisions are deferred (`§15` fork stays PARKED until the goblin is locked).

**Style for the first sprite (evidence-led, not taste-led):**

- GBA-clean 16-bit, side view, single-color outline, medium detail + medium shading, ~64×64 canvas.
- Basis: the model's own "GBA-Style Sprites" tutorial plus parameters reported by real users in the MCP repo issues.

**Token custody:** the account token lives in `Temp/pixellab.token` (gitignored), never in a committed file. A leak
means revoke-and-rotate, not panic.

**Downstream is already built and verified:** `ArtPresetApplier` (trim/pivot/PPU) -> `CharacterAnimationBuilder`
(strips -> clips + controller) -> `HD2D/SpriteLit` in `ArtLab.unity`. The factory's output only has to arrive in
`Assets/ThirdParty/`.

---

## 15. HD-2D re-scoped for this title (owner question, 2026-09-26)

"HD-2D" has two halves: (1) **2D characters lit and shadowed inside a 3D stage** - cheap, AI-compatible, and already
proven in `ArtLab`; and (2) **hand-authored 3D dioramas** - Octopath's expensive half. Text-to-3D does not exist for
this, AI cannot author dioramas, and an idle game's locked camera never exploits full 3D the way a moving camera does.

**Decision: define the target as a fixed-camera 2.5D layered diorama** ("diorama battler look" internally; keep
HD-2D as the marketing word):

```
L1 furthest   AI-authored backdrop (static -> AI's best case, zero frame-consistency risk)
L2 mid        a few real 3D props on the battle line (ground, rocks, pillars) -> real light + contact shadows
L3 front      lit sprite billboards + one blurred foreground occlusion prop (depth on the cheap)
post          bloom, vignette, grade, baked DOF (built in ArtLab)
```

~10x less authoring than Octopath-style authored 3D levels, mobile-safe, and AI-compatible by construction. The
props are real 3D geometry (they catch the key light and cast shadows the sprites share); the backdrop is value-graded
below the battle line (the `§7.1` readability contract).

**Backdrop fork (painterly vs full-pixel vs hand-art): PARKED.** The owner deferred background decisions
(2026-09-26). It is decided by ONE experiment after the goblin locks: the sprite standing on one painterly backdrop
vs one pixel backdrop, 1 generation total.
---

## 16. PixelLab goblin — locked + full combat loop animated (2026-09-27)

The benchmark goblin is locked and the full Idle → Attack → Hurt → Death loop is proven in `ArtLab.unity`.

### Ledger (audit budget)

`get_balance` at close of pass: **10/40 used, 30 remaining** (6 character generations + 4 animations; animation
jobs bill on completion). Credits `$0.00`, subscription `trial`.

### Benchmark locked

- **character_id `9206bec9-aa68-4631-8ae1-6e69898487d2`** — "Pilot Goblin SW Club" (generation 6), the only goblin in
  the scene (`Benchmark_Goblin`). 8-direction SW pose, 92×92 canvas / 42×63 art, 1.15 m at PPU 53.9, club visible.
- **Winning generation settings:** `mode=standard`, `n_directions=8` (required for diagonals), `view=side`,
  `size=64`, `outline="single color black outline"`, `shading="flat shading"`, `detail="medium detail"`,
  proportions head 1.0 / arms 0.9 / legs 0.8 / shoulders 1.3 / hips 1.1, `text_guidance_scale=10`, description leads
  with the weapon held away from the body. **No color-mood words** (muted/earthy/desaturated caused rejected palettes).

### Animation pass (all 4, re-poses of the same character_id, not new generations)

| State | Template | Frames |
|---|---|---|
| Idle   | `fight-stance-idle-8-frames` | 8 |
| Attack | `cross-punch` | 6 |
| Hurt   | `taking-punch` | 6 |
| Death  | `falling-back-death` | 7 |

`direction=south-west`, `mode=template`, `ai_freedom=150`. Completion + billing confirmed via `list_jobs` +
`get_balance`. **Delivery channel:** `get_character` cut its `animations:` frame-URL section after billing, but the
`download` endpoint (auth header) is the source of truth — zip layout
`Idle/animations/<Clip>/south-west/frame_NNN.png`. Raw zip archived as `Goblin_anim_pack.zip`.

### Import + animation build (tools generalized, both verified)

- `ArtPresetApplier`: strip detection is now height-based (uniform row of N≥2 square cells, any cell size ≥16) —
  handles the 92 px PixelLab cells and the pack's 100 px cells. Per-strip union trim + feet pivot + art-derived PPU.
  Goblin strips: Idle 8 spr / Attack 6 / Hurt 6 / Death 7; PPU 52-55 → 1.15 m.
- `CharacterAnimationBuilder`: scans all of `Assets/ThirdParty` via the `<Character>_<Clip>_anim.png` contract
  (`StateKey` maps Attack→Attack01 etc.). Built 4 clips + `Goblin.controller` at 10 fps: Idle 0.8 s loop;
  Attack01/Hurt/Death 0.8/0.8/0.9 s hold-last-frame. Parameters: `Moving`, `Attack`, `AttackVariant`, `Hurt`, `Dead`.
  Transitions: Idle→Attack01 (Attack trigger + AttackVariant=0), Idle→Hurt (Hurt), exit-time returns to Idle,
  AnyState→Death (Dead). Strips live at `Assets/ThirdParty/PixelLab/goblin/Goblin_<Clip>_anim.png`.
- **Scene wiring:** `Benchmark_Goblin` got the `Goblin` Animator + sprite `Goblin_Idle_anim_0` (1.15 m bounds kept).

### Verified combat loop (play mode, freeze-frame stepping via `Animator.Update`)

States reached and sprites confirmed: Idle → `Goblin_Idle_anim_0`; Attack (clipTime 0.41) → `Goblin_Attack_anim_3`
(mid-lunge); Hurt (0.48) → `Goblin_Hurt_anim_3` (recoil); Death (0.92) → `Goblin_Death_anim_6` (fallen, held).
`P10_goblin_{idle_cam,attack,hurt,death}.png` + `P10_contact.png` in `Assets/Screenshots/art-lab/` (gitignored).
Same-pipeline camera captures differ between every pair (2.7-3.7% of the goblin crop, mean delta 48-62) with the
background pixel-identical — the pose change is on screen, not just in the asset.

### Source-frame QA (before Unity)

- Palette: 24-27 unique colors per strip, consistent across all four re-poses.
- Consecutive-frame silhouette overlap (intersection/union): Idle 0.92-1.00, Attack 0.48-0.93, Hurt 0.54-0.73,
  Death 0.39-0.64 (falling-back = intentionally high motion).
- Feet-line stability: Idle/Attack/Hurt ≤2 px. Death drops 13 px — the deliberate rotate-to-ground fall.

### Parked (not spent)

Club reads as "a log of wood". Prompt fix for the next regen (1 gen): weapon leads as
"tapered cudgel with a thick knobbed head and a slender handle, held in one hand at an angle", and `sturdy` instead
of `large`. Only spend if the owner wants the club re-cut.

### Still open

- `TextureImporter.spritesheet` deprecation migration to `ISpriteEditorDataProvider` (warning only so far).
- Tiny RPG pack licence file still missing.
- 30 generations remain for the production unit set.
