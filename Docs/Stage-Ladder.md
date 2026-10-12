# Stage Ladder — 100 stages, 10 bands

Target: **100 stages for the MVP.** 42 characters, no new art after band 7 — the last thirty stages carry novelty
through escalating skins, boss families and the enemy mix, never through new characters.

Roles come from `Character-Catalog.md`. Ability animations come from `Ability-Map.md`.

---

## What the ladder needs from the game

Today `WaveConfig` holds **one list of enemies** and picks by **counting through it** — a rotation. So the same four
enemies appear at stage 1 and at stage 100. A ladder cannot be expressed that way.

**The change:** two numbers on each enemy — **first stage** and **last stage** it may appear. When the game counts
through the list, it **skips any enemy that does not fit the current stage.** Bosses get the same two numbers.

- Two fields, one filter. Still deterministic from `(stage, wave)` — **no save change**.
- **No balance effect:** enemy stats already scale with the stage. These numbers only decide *which picture* appears.
- **The no-code alternative:** `stageAlternates` already rotates art per stage for one archetype (Demon_A /
  Blood Monster_A on the goblin slot). That gives variety but cannot make a *new* enemy appear only later.

---

## The ten bands

Bands **overlap by two stages** on each side, so the seam between themes is never a hard cut: a band's enemies fade in
while the previous band's fade out.

| Stages | Theme | Normal enemies | Milestone boss |
|---|---|---|---|
| **1–10** | critters | Slime, Bat, Werewolf | **Elite Orc** |
| **11–20** | undead | Skeleton, Armored Skeleton, Greatsword Skeleton | **Necromancer** (+ summons Skeleton Archers) |
| **21–30** | orc warband | Orc, Armored Orc, Armored Axeman | **Orc rider** |
| **31–40** | beasts | Werewolf, Werebear, Hellhound | **Minotaur** |
| **41–50** | hellfire | Hellbat, Lava Slime, Ghostfire | **Flame Golem** |
| **51–60** | demons | Demon_A, Demon_B, Demon_C | **Black Knight_A** |
| **61–70** | dark magic | Demon_D, Demoness_B, Blood Monster_A | **Warlock** |
| **71–80** | abyss | Demon_E, Blood Monster_B, Eyeball Monster | **Demoness_A** |
| **81–90** | escalation | every previous band, deeper alternates | **Black Knight_B** |
| **91–100** | final | every previous band + summons | **Black Knight_C** |

---

## Bosses — the villain cast, in order

**Twelve boss-capable characters; ten fill the milestone slots below.** The two spares are **Demon_E** and
**Eyeball Monster** — the bands above list them as normal enemies, so promote either one if you want more boss variety.
Three milestones are the **same family** (Black Knight A, B, C), which is how one archetype becomes a three-part
villain arc.

| Stage | Boss | Note |
|---|---|---|
| 10 | Elite Orc | the first real wall |
| 20 | Necromancer | summons Skeleton Archers — see below |
| 30 | Orc rider | mounted champion |
| 40 | Minotaur | the brute |
| 50 | Flame Golem | mid-point spectacle |
| 60 | Black Knight_A | villain arc, part one |
| 70 | Warlock | magic |
| 80 | Demoness_A | the abyss |
| 90 | Black Knight_B | villain arc, part two |
| 100 | Black Knight_C | villain arc, finale |

**Summoned units are not in the pool.** Skeleton Archer appears **only** inside the Necromancer's fight, summoned by
its `Summon` animation. That is a new combat behaviour — a v1.1 mechanic, planned here so the art is not wasted.

---

## Introduction order — when each character first appears

| First stage | Characters |
|---|---|
| 1 | Slime, Bat, Werewolf |
| 11 | Skeleton, Armored Skeleton, Greatsword Skeleton |
| 21 | Orc, Armored Orc, Armored Axeman |
| 31 | Werebear, Hellhound |
| 41 | Hellbat, Lava Slime, Ghostfire |
| 51 | Demon_A, Demon_B, Demon_C |
| 61 | Demon_D, Demoness_B, Blood Monster_A |
| 71 | Demon_E, Blood Monster_B, Eyeball Monster |
| 81–100 | **no new characters** — escalation only |

**Heroes are not in the ladder.** The six heroes come from `Ability-Map.md` and are unlocked separately.

---

## How the last thirty stages avoid repetition

1. **Escalating alternates.** One slot wears up to three skins across the run (Orc → Armored Orc → Elite Orc).
2. **Boss families.** Black Knight A/B/C gives three escalating bosses from one silhouette.
3. **The mix.** Multi-enemy waves hold 1–3 units, so each wave shows several looks at once.
4. **Recolours are available** — both packs ship their Aseprite sources, so a palette variant is an art edit, not new art.

That is the honest limit: **bands 1–7 use all 25 normal enemies; bands 8–10 reuse them.** Beyond 100 stages repetition
becomes visible.

---

## Open decisions this document depends on

| # | Decision | Why it matters |
|---|---|---|
| 1 | **Approve the two numbers** (first/last stage per enemy) | the ladder cannot exist without it |
| 2 | **Per-clip frame rate, or accept the variance** | attacks run 6–18 frames with one fps per character, a 3× spread |
| 3 | **Renaming goblin and ogre** | neither exists in either pack. Keep the **id** stable (save keys use it); rename the asset and the display name only |
| 4 | **When summons arrive** | the Necromancer's Skeleton Archers are a v1.1 combat mechanic |

## How we will know it works

1. The validator reports **no unknown action names** across both packs (`Character-Catalog.md` rule 1).
2. For a spread of stages (1, 10, 20, 35, 50, 65, 80, 95, 100) the wave shows **only** enemies whose band includes that stage.
3. Every boss stage shows its intended boss, and the stage is flagged `isBoss`.
4. A live walk of three bands, judged at 1:1 on a device — the owner's eye is the gate.
