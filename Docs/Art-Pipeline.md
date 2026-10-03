# Art Pipeline — the HD-2D look, asset volume and tooling

> Owns: how every visual asset is produced, imported, validated and budgeted - roadmap **G11** (real art pipeline
> undefined) and **G12** (device perf budgets).
> Parent: `Architecture.md` (§6 decision log), `Roadmap.md` Step 20, `Content.md` (zones/enemies), `UI-UX.md` (screens).
> **Location:** `Docs/` (folder index: `README.md`). **Last verified:** 2026-09-26 (new).
> **Status: design for approval; §17 (the UI pass) is built and waits on one owner pick.** Nothing in §4-§8 is built.
> §9 lists the decisions that must be made first.

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

---

## 17. UI art pass — pixel/retro, kit imported, UI Lab built (2026-09-29)

Owner decision (2026-09-29): **the UI look is pixel/retro**, to match the locked pixel goblin. Scope of the first
pass is the **battle screen only**: top bar, battle viewport frame, combat log, bottom nav. The UI shell already
exists and works (`MvpSceneBuilder`), so this is a **reskin, not a rebuild**.

### What was added

| Thing | Where | Notes |
|---|---|---|
| UI art pack | `Assets/ThirdParty/Kenney_PixelUI/` | Kenney Pixel UI Pack, **CC0**, 35 PNGs: 9-slice panels/buttons in 3 families + a 16x16 tile sheet |
| Pixel fonts | `Assets/ThirdParty/PixelFonts/` + `Assets/IdleRPG/Resources/Fonts/*.asset` | Silkscreen, VT323, Jersey 10 - all OFL 1.1. Fonts live in a **Resources** folder so runtime-created text (log lines, hero HP bars, damage numbers) can load them too |
| Import policy tool | `Tools > Idle RPG > Art > Apply UI-Art Presets` (`UiArtPresetApplier`) | Point filter, no mips, **uncompressed**, PPU 100, centre pivot, FullRect, 12px 9-slice border |
| Font asset tool | `Tools > Idle RPG > Art > Generate Pixel Fonts` (`PixelFontAssetGenerator`) | SDF render mode, sampled at 64px - see the font policy below for why raster was rejected |
| Comparison scene | `Tools > Idle RPG > Build UI Lab Scene` → `Scenes/UiLab.unity` | Three complete looks stacked in one portrait screen. **Not in build settings** |
| Pixel-exact snapshot | `Tools > Idle RPG > Capture UI Lab Snapshot` | Renders 1080x1920 at 1:1 and writes `Assets/Screenshots/ui-lab/uilab-{A,B,C}.png` - one full-screen PNG per look |
| Licence record | `Assets/ThirdParty/PROVENANCE.md` | Source + licence + date per pack, plus the two known licence gaps |

### Measured, not guessed (the import numbers)

Read straight out of the pack's pixels (2026-09-29):

| Fact | Value | Consequence |
|---|---|---|
| 9-slice pieces | 48x48 | 3 x 16px design tiles |
| Plain/outline pieces | 1px transparent margin, 1-2px outline, **square corners** | any border >= 2 keeps the outline crisp |
| `list.png` | two horizontal inlay lines at y=16 and y=32 | they must sit inside the stretched middle band, so the border must be <= 16 |
| `Ancient` family | serrated edge decoration down the whole side | safe to slice, but a large stretch spreads the serrations - a look call, judged in the lab |
| **Chosen border** | **12 px** | covers every family's detail, keeps middle area for small chips |

**Compression must stay off.** These are 48px textures with 1px outlines; block compression smears them into mush.
That is a memory cost, and it is why the pack should stay to panels/buttons rather than being used as an atlas of everything.

### Font policy (SDF - the raster experiment failed, and here is the record)

Version 1 of the lab used **Raster (bitmap)** fonts sampled at each font's native grid (8 / 16 / 10 px). Raster is
crisp ONLY at whole-number scaling. The game canvas rescales to fit arbitrary phones (`ScaleWithScreenSize`; 0.667x
in a 1280x720 editor window), so the fonts **smeared - "garbled" - in the Game view, and would have done the same
on most real devices**. That rejection is a documented finding, not a dead end.

Decision: **SDF render mode, sampled at 64 px**. SDF is smooth at any scale, so the text is legible in the editor,
on every phone, at every width. The fonts' square letterforms still read "pixel"; the pixel identity lives in the
panels, chips, icons and backdrops, which are images and scale cleanly. The old rule "display sizes must be whole
multiples of the native grid" is gone. Rules that remain: dynamic atlas population, auto-sizing off, and text
sizes are **tokens**, not ad-hoc numbers.

### The canvas-scale trap (found the expensive way)

A pixel font is only crisp when **canvas pixels equal screen pixels**. The MVP canvas uses
`ScaleWithScreenSize` + match 0.5, which lands on a fractional scale (0.667 in a 1280x720 editor view) and turns
crisp glyphs into uneven ones. It looked like a font bug and was a scaling bug. Consequences:

- Verdicts on pixel text must be made at 1:1 - that is why the lab renders its own 1080x1920 snapshot instead of
  screenshotting the editor window.
- **Open decision before the reskin lands:** either accept fractional scaling on device (chunky, slightly uneven -
  the honest cheap option), or move the UI to integer scaling / a fixed reference canvas (crisp, costs letterboxing
  on some phone aspect ratios). Measure on a real device before choosing.

### How to look at the lab

Open `Assets/IdleRPG/Scenes/UiLab.unity` and press Play (look A shows), then run the snapshot menu item. The
decision images are the three 1:1 1080x1920 PNGs in `Assets/Screenshots/ui-lab/uilab-{A,B,C}.png` - **judge from
the PNGs, not the editor window** (the Game view scales the canvas by a fraction and smears a pixel font).

One look per screen, not three stacked: each is the full battle screen - top bar with currency chips, the dimmed
stage as a backdrop, the hero formation board on the LEFT and the enemy stack on the RIGHT (mirroring
`MvpSceneBuilder.Battle.cs` bands, not an invented layout), and the bottom nav. Each look pairs a font with a
full palette, so the three PNGs are clearly different.

| Look | Font | Palette | Log / chunky / numbers |
|---|---|---|---|
| A | Silkscreen | dark slate + gold | 24 / 32 / 40 |
| B | VT323 | dark parchment + amber | 32 / 48 / 48 |
| C | Jersey 10 | indigo + cyan | 30 / 40 / 40 |

Palette lesson learned in the lab: tinting a sprite that already carries baked colour shifts the hue (grey-blue
panels went muddy blue, yellow tinted with gold went orange). The only reliable tint base is a **white/light-grey
face** - the pack's `Colored/grey.png` is (238,238,238), so panels tint exactly to their token colour.

### Applied to the battle page (2026-09-30, live in Main.unity)

The pick hit the real scene, not another lab:

- **Tokens**: new `Scripts/UI/UiTheme.cs` - palette + fonts + type sizes in one file. Every battle-page
  colour/size in the scene builder now reads from it (header, chips, HP bars, damage numbers, log, nav).
- **Fonts**: Silkscreen 40 for currency, 34 stage, 24 nav/Hp/enemy names; VT323 **34** for the log (was 22),
  line height 46. Damage floats 42, crits **1.4x bigger + gold**. Mixed sizes (numbers vs sentences) are the
  "chunky but readable" contract the owner asked for.
- **Palette**: panels slate `(37,43,62)` family, active nav gold `(214,158,66)` with dark ink, text
  `(236,240,248)`. Hero HP bar labels and hero names (runtime-created by the formation board) are themed too.
- **Fonts moved to `Assets/IdleRPG/Resources/Fonts/`** so runtime-created text (log pool, hero bars, damage
  pool) can load them - a path-only change, GUIDs preserved.
- **Bug found + fixed on the way**: `MvpSceneBuilder.PersistUiActionsAsset` saved the UI input-actions file
  with `AssetDatabase.CreateAsset`, which leaves a dangling reference - `InputSystemUIInputModule` asserted on
  enable ("Map must be contained in state") and play mode froze on frame 1. Writes `.inputactions` via
  `ToJson()` + import now, and binds the persistent asset.
- **Second pass (2026-09-30, owner: "much darker than the image")**: the first application tinted the theme
  over the OLD placeholder sprites, whose grey faces are only 0.10-0.20 brightness - so slate rendered
  near-black and gold nav rendered muddy. All battle-page surfaces (header, chips, boost chip, log strip, nav
  bar, nav buttons) now use the kit's **white-face** sprite (`Colored/grey.png`) via `KitPanel(...)`; a tint now
  lands **exactly** on its token. Nav reworked in the same pass: selected tab = **bright gold** (247,203,110)
  with dark ink (c.11:1 contrast), unselected = light slate, labels 26px. Verified live + on rendered pixels
  (header (35,39,57) vs the old (10,13,20); nav active gold).
- **Verified live** (play mode): currency font `Silkscreen Pixel @40`, log template `VT323 Pixel @34`, nav
  active colour gold; regression console 0 errors, save loaded, combat resumed.
- **Sixth pass - battle-log repair (2026-09-30):** combat feed had gone structurally silent. Three orphaned
  raise sites (WaveCompleted/BossFailed were dead after the director refactor; PartyWiped fired twice) plus all
  structural lines sharing the 5-line/s hit budget. Fix: CombatManager now bridges WaveCleared ->
  GameEvents.RaiseWaveCompleted and BossFailed -> GameEvents.RaiseBossFailed; the GameManager no longer
  double-raises PartyWiped (one source in CombatManager). New one-sentence news channel `GameEvents.CombatMessage`
  (LogMessage{Text, Kind}) raised from CombatManager for wave headers/summaries (via EncounterFactory.Describe),
  wave-cleared, boss-fail and wipe. CombatLogUI dropped 11 handlers to 8, appends alwaysShow lines on a separate
  4/s priority budget so progression punctuation can never starve, and clears the feed on SaveLoaded with a
  "-- session resumed --" divider (restart/offline loads start visibly fresh). FUTURE MECHANICS RULE: systems
  that want to say something in the feed (item drops etc.) call GameEvents.RaiseCombatMessage - zero UI wiring.
  Verified live: wave headers, "Stage X wave Y cleared", kills/crits flow; SaveLoaded clears + divider appears.
  Screenshot `Assets/Screenshots/battle-page-v8-log.png`.

- **Fifth pass - battle-page bugfixes (2026-09-30):** damage numbers smaller (32px, crit 1.3x, shorter travel),
  and spawns now adopt the unit anchor's anchors/pivot + clamp inside the battle view (numbers can no longer
derive below the sim); empty hero seats are fully invisible (resting colour alpha 0 - `Update()` was
re-painting the old white ghost); enemy health bars are sprite-less solid colour fills with the players' exact
styling and the same relative placement/size; the BOSS gets a bigger name (1.3x) while its
health bar stays the standard size (reverted same day - owner call). Verified live: enemy name y 0.14-0.32 + bar anchors (0.10,0.02)-(0.90,0.12) + fill sprite=none,
empty seats alpha 0, damage template 32px. Screenshot `Assets/Screenshots/battle-page-v7.png`.
- **Fourth pass - battle-page cleanup part 2 (2026-09-30):** currency header slimmed (12% -> ~7% of screen,
  chips fill it tightly, boost into the same band); formation slot tiles set to transparent (no white layers /
  row indicators on the battle view - the Party page keeps its board); enemy names moved **below** the sprite
  (same as players), HP bar at the bottom; the full-slot boss frame panel **removed** (boss = red BOSS name tag
  + larger sprite only); floating damage numbers now pop from the enemy's top-LEFT and the hero's top-RIGHT.
  Verified live (anchors/colors/null checks) + screenshot `Assets/Screenshots/battle-page-v6.png`.
- **Third pass - battle-page cleanup (2026-09-30, owner-directed):** proper static stand-in art so the
  layout can be judged: heroes = three Soldier frames (`Soldier_Idle_0/3`, `Soldier_Attack01_2`) tinted
  white/steel-blue/violet; enemies Goblin = goblin art, Ogre = Orc art. **Slime + Bat keep the procedural
  silhouette until 2 PixelLab generations are spent** (blocked: `Temp/pixellab.token` not present; 30/40
  generations remain). Hero slots 132x150 -> **160x176**, enemy icon cap 170 -> **200**. Layout moves:
  Stage/wave now lives in its own **centered row between the header and the battle sim** (was overlapping the
  tokens chip); the header's duplicate "Ascend: X" is **deleted** (the Ascend page already shows the yield);
  a **"BATTLE LOG" row** now separates the sim from the log (future tab anchor). Verified live: actor + data
  checks + screenshot `Assets/Screenshots/battle-page-v5.png`.

Not in this pass (still open below): other pages (Shop/Upgrades/Ascend still use their old colours - `UiTheme`
exists, wiring them in is mechanical), currency icons, and unit art.

### Open items this pass did not close

- [x] **The pick (2026-09-30).** **Font A (Silkscreen)** for numbers, buttons, currency, damage/crits;
      **font B (VT323)** for the battle log and sentences, at **34px** (bigger than the lab's 32); **palette A**
      (dark slate + gold). Applied to the real battle page the same day - see "Applied to the battle page" below.
- [ ] **Currency icons** (gold, gem, token, scroll, ad). The kit has none. Needs one CC0 pixel icon set, or the
      existing procedural generator redrawn at pixel scale.
- [ ] **Unit art** in the viewport is deliberately out of scope: hero/enemy sprites come from `HeroData`/`EnemyData`
      and are a character pass, not a UI pass.
- [~] **Design tokens.** ~109 colour values were hard-coded across 21 files (88 distinct). **Started 2026-09-30:
      `Scripts/UI/UiTheme.cs`** (palette + fonts + sizes) exists and the battle page reads from it; the other
      pages (Shop/Upgrades/Ascend) and their runtime views are the mechanical remainder.
- [ ] **Mobile import check.** Uncompressed UI textures and the 2-3 heavy post effects need a texture-memory
      measurement on device before the reskin is called done (§6 budgets).

## 18. Tiny RPG character pass - animated units (2026-09-30)

Four pack characters replace the placeholder/lab art in the battle view: **Soldier** (all three heroes),
**Orc** (boss), **Demon_A + Blood Monster_A** (the goblin wave slot, rotated by stage `index = stage % 2`,
stage-1 = Demon). Slime/Bat/Goblin placeholders are untouched.

- **Folder**: canonical shadowed strips at `Assets/IdleRPG/Art/Characters/TinyRPG/<Char>/<Char>_<Action>.png`;
  the raw drop + aseprite sources live under `TinyRPG/_Source/` for traceability. `.DS_Store` files removed.
- **Import**: `Tools > Idle RPG > Art > Import TinyRPG Characters` slices every `N x 100px` strip on the
  100px grid (Point/NoMips/Uncompressed/PPU100) and (re)generates `Data/Characters/<Char>_Art.asset`
  (`CharacterArtSet`: Idle/Walk/Attack01-03/Hurt/Death + fps). Width is read from the PNG header so the
  frame count can never depend on a stale imported-texture cache.
- **Runtime**: `CharacterAnimator` swaps frames on the unit icon `Image` (no AnimatorController). Idle loops,
  Attack/Hurt/Death are one-shots back to Idle. Facing rule is constructed so heroes always face right and
  enemies always face left from the art's own facing.
- **Data**: `HeroData.artSet`, `EnemyData.artSet` + `EnemyData.stageAlternates` (rotation). Generator
  (`DataAssetGenerator`) wires these at build time - pasted SerializedObject edits do NOT survive a rebuild.
- **Verified live**: idle/attack/hurt/death all observed (death frame `Blood Monster_A_Death_3`, hurt
  `Blood Monster_A_Hurt_1`, hero mid-swing `Soldier_Attack02_2`); boss = Orc; alternation Demon/Blood Monster
  by stage parity; stale-animator clearance on placeholder waves; enemies face left. Screenshot
  `Assets/Screenshots/battle-page-v9-tinyrpg.png`.
- **Licence**: TESTING ONLY until the pack's terms are confirmed (no licence text shipped) - see PROVENANCE.
- **Immediate issue (same hour): characters rendered extremely tiny.** The 100x100 pack tiles only carry ~20% body pixels, so full-tile sprites in the fixed slot rects drew as small dark blobs. Fixed by cropping every strip to the union of its opaque bounds (shared window per action -> animation stays stable) inside the importer, so the character fills its tile; hero board icons raised from 0.62x to 0.8x of the slot. Result live: heroes ~128px, enemies ~170-200px (boss). Both the facing and the attack-size observed: the pack art faces RIGHT natively, so the ArtFacesLeft flag was corrected to false - heroes now keep the native right facing, enemies get mirrored (scaleX verified -1.00 live), the requested directions. Attack/hurt/death frames have much wider windows than idle (Soldier Attack02 29x29 vs Idle 17x22); a fixed rect would shrink those poses, so CharacterAnimator now sizes the icon rect per action at a single per-character pixel scale (idle 103x128, attack 162x162 live) - every pose renders its body at the same size, and extreme swings are capped at 1.5x the slot base. Screenshot battle-page-v10-tinyrpg.png.
- **Battle-page cleanup pass 3 (2026-09-30): no death fade (sprite/name/hp never alpha-tween - death clip then instant hide for enemies, heroes stay opaque on the death pose), enemy HP bars are now hero-identical style at ~128px (was stretching across the 557px enemy column), columns staggered: heroes front-slash (front column raised 28px), enemies backslash (lower slots drift right 24px). Screenshot battle-page-v11.png.
- **Battle-page cleanup pass 4 (2026-09-30): hero lean fixed (front column now RAISED - the top-anchored rects made the old +28 y drift DOWN, so heroes leaned left before), and enemy slots now use the hero geometry exactly: 160x176 box, 188px row pitch (176+12 gap, same as heroes), 128px icons, 128px bars - grouped like the heroes instead of spread over the tall column. Screenshot battle-page-v12.png.
- **Battle-page cleanup pass 5 (2026-09-30): the lean is a collective block shear, not a column offset.** Heroes row-drift LEFT as they descend (base nudge +48, -24/row) so the whole two-column block points right like a front slash (back col x 48/24, front col x 234/210/186 live). Enemy group is now centered vertically inside its column (group mid == container mid == hero board mid, world-space verified), and no longer hugs the column top. Screenshot battle-page-v13.png.
- **Battle-log reliability pass (2026-09-30) - three root causes found and fixed.** (1) The log strip is a child of the battle page and subscribed in OnEnable/OnDisable, so opening Upgrades/Ascend/Shop silently threw away every event until you returned - subscriptions now live in Awake/OnDestroy and fire even while the page is hidden (verified: 28 events raised with the page off, +29 lines captured). (2) The auto-follow heuristic fought the ScrollRect: the log's ScrollRect is Elastic + inertia, so the ScrollRect's own clamp looked like the player scrolling - follow paused (feed froze) then re-engaged on its own. The log scroll is now Clamped + no inertia and follow pauses only for a real drag (LogScrollDragRelay), verified pinned (contentY - overflowY = 0.00). (3) The per-second drop budget silently discarded distinct lines; it is gone - one event = one line, always, with a 100-line scrollback window for memory. Screenshot battle-page-v14-log.png.
- **Numbers + positioning pass (2026-09-30).** Numbers: `NumberFormatter` is now the single owner of game-number formatting and is integer-only (rounds for display, whole mantissa with tier carry, a positive value never shows as 0) - so HP, damage, gold/gems/tokens, kills and the log are decimal-free by construction; the wave header text was switched off its inline `0.#` format too. Verified: 0 decimal labels across the battle page (104 labels) and the ascend/shop tabs. Upgrade/multiplier descriptors (`x1.05 / lvl`, `+0.5% per level`, back-row weight) keep decimals by explicit decision - they are not hp/damage/gold. Positioning: the party-board glitches traced to views re-binding on every repaint and `CharacterAnimator` deriving its base size from the LIVE icon rect (a wider action pose captured as the new base ratcheted the sprite bigger on every tap). The slot now owns its icon size (`SetSlotSize`), `HeroUnitView.Apply` is idempotent, and battle seats refresh live HP the moment they rebind. Party taps only repaint the selection highlight (`RefreshSelection`). Verified: 5 taps -> rect and pixelScale constant; a mid-fight move shows the destination seat's live HP (41/130) with no zero flash. Screenshot battle-page-v15.png.

### Verification notes (2026-09-30)

- Live-feed/runtime checks: with the IDE focused the Editor can stop rendering Play frames (combat, logs and
  damage numbers all look frozen even though `eval` still answers - `Time.frameCount` stuck near 1 is the tell).
  Call `unity-mcp editor_focus` (plus set_autotick if you prefer headless ticking) before any live observation.

