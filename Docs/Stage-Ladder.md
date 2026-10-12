# Stage Ladder — 100 stages, 10 bands

Target: **100 stages for the MVP.** 42 characters, ~42 elite recolours, ~40 boss definitions — all from the existing
art, with no new drawing.

Companions: `Character-Catalog.md` (what each character animates), `Ability-Map.md` (abilities — separate track).

---

## The design model

**Variety is front-loaded.** Most players only ever see the early stages, so the early stages get the *most* distinct
enemies — not the fewest. The pool then grows as stages advance: variety is never taken away.

| Rule | Why |
|---|---|
| **Front-load variety** | band 1 opens with **6** distinct enemies and reaches **8** |
| **Overlap, never hard-cut** | every enemy spans at least two bands (windows of ~15–30 stages), so a new theme fades in while the old one fades out |
| **Elites are the progression** | once the base roster is known, recoloured variants take over as the visual novelty |
| **A boss on every stage** | already true in code — every stage ends in a boss wave |
| **Ordered by strength** | each band's enemies must be genuinely stronger than the last band's, or variety breaks the difficulty curve |

### What a stage actually contains (measured from the code)

| Fact | Value | Source |
|---|---|---|
| Normal waves per stage | **10** | `BalanceConfig.normalWavesPerStage = 10` |
| Boss waves per stage | **1**, after the ten | `WaveConfig.IsBossWave` = wave `normalWavesPerStage + 1` |
| Enemies per wave | **1–3** | `minEnemiesPerWave = 1`, `maxEnemiesPerWave = 3` |
| Enemy encounters per stage | **10–30**, plus 1 boss | derived |
| A wave's team | the wave's own pick plus its rotation neighbours | `WaveConfig.GetEnemiesFor` |

At a pool of 6–8, each enemy appears **2–5 times per stage**: recognisable, not monotonous. A pool of 3 would show
each enemy 3–10 times per stage — which is why the first draft of this document was wrong.

## Variety per stage

| Stages | Distinct enemies | What is new |
|---|---|---|
| **1–10** | **6 → 8** | Slime, Bat, Soldier, Swordsman, Werewolf, Skeleton; Armored Skeleton joins at 8 |
| **11–20** | 8 → 12 | Greatsword Skeleton, Orc, Armored Orc; first **elites** |
| **21–30** | 12 → 14 | Armored Axeman, Hellhound; more elites |
| **31–50** | 14 | Werebear, Hellbat, Lava Slime, Ghostfire + elites |
| **51–80** | 12–14 | Demon_A–E, Demoness_B, Blood Monster_A/B, Eyeball Monster + elites |
| **81–100** | 12–14 | mostly **elites of earlier enemies**; the weakest have retired |

---

## Stage windows — first and last stage per enemy

This is exactly what S5 encodes: **two numbers per enemy.** Windows overlap, so a theme change is a fade and never a
cut.

| Enemy | First | Last | Note |
|---|---|---|---|
| Slime | 1 | 24 | retired once undead and orcs take over |
| Bat | 1 | 24 | |
| Soldier | 1 | 26 | the early rivalry |
| Swordsman | 1 | 26 | |
| Werewolf | 1 | 42 | carries into the beasts band |
| Skeleton | 1 | 34 | teased early, then owns the undead band |
| Armored Skeleton | 8 | 34 | the first new face the player meets |
| Greatsword Skeleton | 11 | 36 | |
| *Skeleton Archer* | *—* | *—* | **summoned only**, never in the pool |
| Orc | 14 | 48 | |
| Armored Orc | 17 | 50 | |
| Armored Axeman | 21 | 52 | |
| Hellhound | 24 | 62 | |
| Werebear | 29 | 62 | |
| Hellbat | 36 | 68 | |
| Lava Slime | 39 | 70 | |
| Ghostfire | 42 | 72 | |
| Demon_A | 47 | 82 | |
| Demon_B | 50 | 84 | |
| Demon_C | 53 | 86 | |
| Demon_D | 58 | 92 | |
| Demoness_B | 61 | 94 | |
| Blood Monster_A | 64 | 96 | |
| Demon_E | 68 | 100 | |
| Blood Monster_B | 72 | 100 | |
| Eyeball Monster | 74 | 100 | |
| **elites** | band − 10 | 100 | recoloured variants take over the late pool |

**Bosses are not in this pool.** They come from the boss table below.

## Elites — where the visual progression lives

A recoloured variant of an existing character, used from band 3 onwards. The licence **allows modification** and
forbids redistribution, so recolours are fine inside the game — and stay out of any shared repository, along with the
source art.

- **One elite per character** roughly doubles the visual roster (~84).
- They are **not** 42 hand edits. A **palette table** (2–3 curated palettes, chosen by the owner) is applied by a
  small tool at import, so a variant is reproducible and changing a palette is one line.
- **Do not hue-shift blindly.** A naive shift wrecks the outline and palette discipline the pack is built on. The
  palettes are an art call; the tool only applies them.

---

## Bosses — all 100 stages

Every stage ends in a boss wave. A boss is an `EnemyData` asset flagged `isBoss`, **rendered larger** and named
differently — no new art. Each band gets **3 rotating sub-bosses** plus its **milestone villain**.

| Stages | Sub-bosses (one every 3 stages) | Milestone boss |
|---|---|---|
| 1–10 | 1–3 Slime King · 4–6 Wolf Alpha · 7–9 Soldier Captain | **10 Elite Orc** |
| 11–20 | 11–13 Bone Captain · 14–16 Greatsword Warden · 17–19 Skeleton Marksman | **20 Necromancer** (summons) |
| 21–30 | 21–23 Axeman Brute · 24–26 Orc Skirmisher · 27–29 Orc Chieftain | **30 Orc rider** |
| 31–40 | 31–33 Hellhound Alpha · 34–36 Dire Werewolf · 37–39 Werebear Titan | **40 Minotaur** |
| 41–50 | 41–43 Hellbat Swarmlord · 44–46 Lava Behemoth · 47–49 Ghostfire Wraith | **50 Flame Golem** |
| 51–60 | 51–53 Demon Brute · 54–56 Demoness Blade · 57–59 Blood Horror | **60 Black Knight_A** |
| 61–70 | 61–63 Demon Captain · 64–66 Blood Elder · 67–69 Demoness Sorceress | **70 Warlock** |
| 71–80 | 71–73 Eyeball Beholder · 74–76 Archfiend · 77–79 Blood Abomination | **80 Demoness_A** |
| 81–90 | 81–83 Elite Demon · 84–86 Elite Black Knight · 87–89 Elite Demoness | **90 Black Knight_B** |
| 91–100 | 91–93 Elite Minotaur · 94–96 Elite Flame Golem · 97–99 Elite Warlock | **100 Black Knight_C** |

**40 boss definitions cover every stage**: 30 sub-bosses (existing art, larger, renamed) plus the 10 milestone
villains. The three Black Knights are one family used three times — a three-part villain arc from one silhouette.

---

## What this needs

| Item | Cost | Owner |
|---|---|---|
| Stage windows (two numbers per enemy) | the S5 mechanism, unchanged | builder |
| ~40 boss assets, ~42 elite assets, ~84 window rows | data, generated from the spec | builder |
| **A boss size field** — so `isBoss` art renders larger | **the only code addition** | builder |
| A palette-recolour tool plus a curated palette table | small tool; the palettes are an art call | builder + owner |
| Licence record | already verified: modification allowed, redistribution not | done |
| **Art wiring in the SPEC** — `heroes.json` has **no art field**, so all three heroes were hand-wired to `Soldier_Art`, and `Enemy_Slime` / `Enemy_Bat` / `Enemy_Goblin` have **no art set at all** | **DONE (S6a, `ce0675a`)** — specs carry an `art` field; the generator resolves it to `Data/Characters/<name>_Art.asset` and Export writes it back, so the wiring survives regeneration. Heroes are now Archer / Knight / Wizard and `Boss_Ogre` is Orc (all three heroes previously pointed at `Soldier_Art`). **Still hand-set: enemy `stageAlternates`** (alternation art is not in the spec yet — S6b) | builder |

## How we will know it works

1. The validator reports **no unknown action names** and **no dropped frames** across both packs.
2. For a spread of stages (1, 6, 12, 25, 40, 60, 80, 95, 100) the wave shows **only** enemies whose window covers
   that stage, and the pool size lands inside the range the table above promises.
3. Every stage's boss wave shows that stage's boss, and the `isBoss` flag is set where expected.
4. A live walk of three bands, judged at 1:1 on a device — the owner's eye is the gate.

## Verification tooling — the debug stage stepper (F3 overlay)

Walking the ladder by hand means playing 100 stages. The overlay gets three functions so a band can be jumped to and
inspected in seconds.

| Function | Input | Behaviour |
|---|---|---|
| **Stage +1** | `]` key and an overlay row | `GameManager.StartRun(CurrentStage + 1)` |
| **Stage −1** | `[` key and an overlay row | `GameManager.StartRun(CurrentStage - 1)` |
| **Stage readout** | overlay row | current stage, wave, how many enemies the pool holds, and their names — so a window can be checked at a glance |

**Rules**

- **Editor and development builds only.** The overlay is already compiled behind `UNITY_EDITOR || DEVELOPMENT_BUILD`
  and absent from a release build (B1b verifies this); the stepper inherits that and must never leak into release.
- It calls **`GameManager.StartRun` only** — the same call the debug menu already uses (`DebugHotkeys.cs`), so it adds
  no new gameplay path and no new state.
- **The readout is the point.** The stepper exists to answer one question fast: *which enemies does stage N show, and
  how many of them?* That is how the windows and pool sizes in this document get checked.

## Open decisions

| # | Decision | Why it matters |
|---|---|---|
| 1 | **Boss size** — how much larger, and one value or per-boss? | 10% bigger reads as "same sprite"; 40% reads as a boss |
| 2 | **Elite palettes** — which 2–3, and which characters get which | an art call; the tool cannot judge it |
| 3 | **The wave count** — 10 normal + 1 boss (11 fights) as it stands, or set `normalWavesPerStage = 9` for 10 total | one config number, but it changes every pacing calculation |
| 4 | **Balance gate** — author 100 stages, validate the first 30–40 | 100 stages of balance is a lot of work; players live in the early ones |

