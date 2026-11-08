# Boons - the run's build (design + build plan)

> Owns: the level-up cards (boons), their rarities, the offer rules and the fight-side effect work.
> Parent: `Progression.md` (levels, XP, run lifetime). Fight internals: `Sim-Core.md`, `Combat-Sequencing.md`.
> **Status 2026-11-09:** XP, levels, the card, save/load and the re-roll are **built**. The fight hooks are
> **built** and wired end to end, and **four mechanic cards ship** (Cleaving Blow, Battle Echo, Vampiric Edge,
> Thorned Mail). Rarity escalation (§6), the last two epic hooks and the legendaries (§5), statuses (§4 layer C)
> and the art pass are **plan**, not code.

---

## 1. Decisions already locked

| # | Decision | Why |
|---|---|---|
| 1 | Boons are **run-scoped** and die with the run | a card may be strong without touching the permanent ladder; every run is a different build |
| 2 | **No boon list page** | nothing to browse between runs; the hand is felt, not read. Cheap fallback if play shows confusion: a count on the XP bar |
| 3 | One level = **one pick**; extra levels queue | a big kill cannot waste a choice |
| 4 | **1 free re-roll per run** (`REFRESH`) | a bad hand must not decide a run, without making re-rolls the game |
| 5 | The offer is **saved** | quitting on the card is not a free re-roll |
| 6 | The fight **stops** while the card is open | reading must never cost health (counted pause, reason `boon`) |
| 7 | A card can target **one hero** or the whole party | hero-specific cards are the interesting kind ("thorns for the Mage") |

## 2. What exists today (slice 1 + 2)

| Piece | File | What it does |
|---|---|---|
| Run level + XP | `Progression/PartyProgression.cs` | 20 XP for the first level, x1.18 each; 5 XP a kill at stage 1, x1.1 a stage, boss x5; 20 level cap a run |
| The hand | `Progression/PerkLoadout.cs` | chosen cards, summed per hero, ids in the save |
| The dealer | `Progression/PerkOfferService.cs` | 3 different cards, rarity-weighted, never a maxed one, seeded |
| Wiring | `Core/GameManager.Perks.cs` | kill -> XP -> level -> card -> pick -> stats; save/restore/reset |
| Stats | `Progression/StatResolver.cs` | seven ops folded into the getters it already had |
| The card | `UI/BoonCardUI.cs` | three rows, rarity tag, letter badge, `REFRESH n/n`, own canvas at order 300 |
| Content | `Data/Boons/*.asset` | **13 cards**: 9 numbers (Common/Rare) + **4 mechanics** (Cleaving Blow, Battle Echo, Vampiric Edge, Thorned Mail), dealt by `PerkPool.asset` (65/25/8/2, 3 per offer, 1 re-roll) |

## 3. The shape of an effect: three layers, one source each

A boon is not a monolith. Every effect belongs to exactly one of three layers, and **each layer has one owner**.
That rule is what keeps this maintainable: no number is computed twice.

| Layer | What it is | Owner | Example | Status |
|---|---|---|---|---|
| **A. Stat** | a number added to a stat the fight already reads | `StatResolver` (Unity side) | `+12% attack`, `+4% crit` | built |
| **B. Trigger** | "when X happens, do Y", applied inside the fight | `Sim/` (Unity-free) | `on hit: cleave 40%`, `on kill: shield` | **next** |
| **C. Status** | a timed state that ticks (burn, poison, chill) | `Sim/` with a status container | `on hit: burn 5/s for 3s` | later |

The Unity side never computes a trigger and the sim never reads a ScriptableObject: the loadout is **compiled**
into a plain snapshot when an encounter starts (`SimRules` already works this way).

## 4. What a card is

```
PerkData (ScriptableObject)
  boonID, displayName, description (usually empty -> built from the effects)
  rarity, icon
  allHeroes, heroID                (who it helps)
  maxStacks                        (0 = unlimited)
  effects[]                        (1..N records)
PerkEffectSpec
  layer:  Stat | Trigger          (Status joins later)
  op:     the vocabulary below
  amount, chance, hits, trigger
```

**The vocabulary is the contract.** Adding a card that uses an existing op is a data asset. Adding a *new* op is one
case in the resolver (layer A) or one case in the trigger pass (layer B), plus a test.

### Layer A ops (built)

`AttackPercent`, `HealthPercent`, `DefensePercent`, `GoldPercent`, `AttackSpeedPercent`, `CritChanceFlat`,
`CritDamageFlat`.

Cheap additions to A (no sim work at all): `StartGold`, `XpPercent`, `CampHealPercent`, `RerollCharge`.

### Layer B ops (to build)

| Op | Reads as | Needs |
|---|---|---|
| `Cleave` | "on hit: X% of the hit to the other enemies" | hit hook + neighbour lookup |
| `Echo` | "on hit: X% chance to strike again for Y%" | hit hook, depth cap |
| `OverkillCarry` | "excess damage from a kill hits the next enemy" | kill hook (damage before/after) |
| `Lifesteal` | "heal X% of damage dealt" | hit hook + heal path |
| `Thorns` | "when hit: X% of the damage back" | hurt hook |
| `Execute` | "+X% damage to enemies under Y% HP" | damage multiplier at hit time |
| `ShieldOnKill` | "on kill: shield worth X% max HP" | kill hook + shield path (exists) |
| `SecondWind` | "once a wave, below Y% HP heal Z%" | hurt hook + once-a-wave flag |

`Thorns`, `Lifesteal` and `Execute` give the most "feel" per line of code. `Cleave` and `Echo` are the poster cards
from the reference game.

### Layer C ops (later)

`Burn`, `Poison`, `Chill`, `Bleed` - all one mechanism: apply a status that ticks damage over time. Build it once,
then every damage-over-time card and every enemy equivalent is data. This is where the sim grows the most, so it is
last.

## 5. The catalogue (what a player can actually draft)

**Rarity rule (decision 2026-11-09):** Common and Rare are **numbers**. **Every Epic and every Legendary adds a
mechanic** - one that changes when or how something happens, not just how big it is. That keeps the top of the
ladder memorable and contains the fight-side work to a known list.

"now" = data only, "hook" = needs layer B (the trigger pass), "later" = needs layer C (statuses).

| # | Card | Rarity | Target | Effect | Cost |
|---|---|---|---|---|---|
| **Commons - numbers, 10-15%, 5 stacks** | | | | | |
| 1 | Sharpened Blades | Common | all | +12% attack | now |
| 2 | Iron Skin | Common | all | +12% health | now |
| 3 | Greedy Hands | Common | all | +10% gold | now |
| 4 | Steady Hands | Common | all | +4% crit chance | now |
| 5 | Prospector | Common | all | +15% gold from bosses | now |
| 6 | Warm Fire | Common | all | camp rests heal 20% more | now |
| **Rares - numbers, 20-25%, 3 stacks, often one hero** | | | | | |
| 7 | Knight's Wall | Rare | knight | +25% defense | now |
| 8 | Mage's Focus | Rare | mage | +12% crit chance | now |
| 9 | Soldier's Tempo | Rare | soldier | +15% attack speed | now |
| 10 | Brutal Strikes | Rare | all | +22% crit damage | now |
| 11 | Titan's Blood | Rare | all | +22% health | now |
| 12 | Scholar | Rare | all | +15% party XP | now |
| **Epics - a mechanic, 2 stacks** | | | | | |
| 13 | **Vampiric Edge** | Epic | all | heal 8% of damage dealt | **built** |
| 14 | **Thorned Mail** | Epic | knight | reflect 20% of damage taken | **built** |
| 15 | **Cleaving Blow** | Epic | soldier | on hit: 50% of the hit to each other enemy | **built** |
| 16 | **Battle Echo** | Epic | all | on hit: 25% chance to strike again for 50% | **built** |
| 17 | **Executioner** | Epic | all | +40% damage to enemies under 30% HP | hook |
| 18 | **Overflow** | Epic | mage | a killing blow's excess carries to the next enemy | hook |
| 19 | **Extra Roll** | Epic | all | +1 re-roll this run | now |
| **Legendaries - a rule, 1 stack** | | | | | |
| 20 | **Last Stand** | Legendary | all | once a wave, under 25% HP heal 30% | hook |
| 21 | **Undying** | Legendary | all | survive one killing blow per run at 1 HP | hook |
| 22 | **Berserker** | Legendary | all | +1% attack for every 2% health the party is missing | hook |

Twenty-two cards is enough for a run (a run sees 8-15 picks). The mix matters more than the count: **twelve numbers
carry the early game**, seven epics define the build, three legendaries are the story you tell after.

**Built so far: 9 numbers + 4 mechanics.** The three mechanics that reuse an existing hook (Battle Echo, Vampiric
Edge, Thorned Mail) are data only. Overflow, Executioner and the three legendaries each need one small hook, and that
is the rest of P1.


## 6. Rarity: what it means, and how it escalates

Rarity means power and scarcity at the same time - that is the whole point of a rarity system.

| Rarity | Power per card | Stacks | Weight today | Job |
|---|---|---|---|---|
| Common | +10-15% | 5 | 65 | the bread: always useful, never exciting |
| Rare | +20-25% | 3 | 25 | the hero cards, one mechanic each |
| Epic | +30-40% | 2 | 8 | the build-defining effect cards |
| Legendary | a run-shaping rule | 1 | 2 | one per run at most; the story you tell after |

**How the odds escalate** - the model I recommend, in three parts:

1. **Level bands** (a table in the pool): the weights improve as the party levels, so early picks are boring and
   useful and late picks are the payoff.

   | Party level | Common | Rare | Epic | Legendary |
   |---|---|---|---|---|
   | 1-3 | 70 | 25 | 5 | 0 |
   | 4-7 | 55 | 30 | 13 | 2 |
   | 8-12 | 40 | 33 | 22 | 5 |
   | 13+ | 30 | 33 | 28 | 9 |

2. **Soft pity**: if no Rare+ has appeared in the last 4 offers, the next offer must include one; if no Epic+ in the
   last 9, force one. Two counters in the save. This is what stops four useless hands in a row.
3. **Diversity rule**: one offer may not contain two cards of the same op family (two attack cards), so a hand is
   always three different decisions. If the pool cannot fill it, fall back to any card.

Rarity must not be the only lever on power. A common card taken five times beats a legendary taken once - that is
what keeps the common tier worth picking.



## 7. Offer rules (the part players feel without seeing)

1. Three **different** cards, never a repeat inside one offer.
2. Never a card at its stack cap.
3. Never a card aimed at a hero who is not in the party (a "Mage's Focus" with the Mage benched is a dead card).
4. One card per op family at most (see 6.3).
5. Party-wide and hero cards both appear: roughly two party cards and one hero card per offer.
6. Seeded from (level, re-rolls spent, kills), and the offer is written to the save.

## 8. The fight-side work (layer B)

**One hook pass, three entry points.** Every hit already funnels through `Encounter.ApplySwing` -> `TakeDamage` ->
`RaiseDamage`, and every death through `ResolveDeath`, so no new pipeline is needed.

| Piece | Where | Note |
|---|---|---|
| `Sim/PerkTrigger.cs` | new | `OnHit`, `OnKill`, `OnHurt`, `OnWaveStart` |
| `Sim/PerkEffect.cs` | new | op, amount, chance, hits (one / neighbours / all), target rule |
| `Sim/PerkRuntime.cs` | new | hero id -> compiled effects; immutable for one encounter |
| `Encounter` hooks | existing file | after a landed hero hit, inside `ResolveDeath`, when a hero is hurt, at wave start |
| `Progression/BoonRuntimeFactory.cs` | new | loadout + hero ids -> `PerkRuntime`; the only place that knows both worlds |
| `EncounterFactory` / `CombatDirector` | existing | pass the snapshot the way `SimRules` already travels |

**Wired end to end 2026-11-09.** `Progression/BoonRuntimeFactory.cs` compiles the hand into the fight's
vocabulary (stat cards are skipped - they belong to the resolver), `CombatSimulator.ApplyBoons` hands the snapshot
to every encounter it builds and to the live one, and `GameManager.OnBoonsChanged` rebuilds it on every pick and on
load. Proven live (2026-11-09), from the battle feed:

- *Cleaving Blow* filed one effect on the soldier slot; the feed showed `Soldier hits Bat A for 8` then `Bat B for 4`.
- *Battle Echo* showed `Soldier hits Bat B for 8` then `Bat B for 4` (the extra strike on the same target).
- *Vampiric Edge* showed `Knight healed for 1` / `Mage healed for 1` as they hit.
- *Thorned Mail* is wired (the knight's slot carries it) but is invisible at stage 1: a 1-damage hit reflected at
  20% rounds to nothing. It will read once hits are bigger - a balance note, not a bug.

**Built 2026-11-09 (foundation):** `Sim/PerkTrigger.cs`, `Sim/PerkEffect.cs`, `Sim/PerkRuntime.cs`, and the
hooks inside `Encounter` (`RunTriggeredEffects`, `RunOwnEffects`, `ApplyPerkEffect`, `DamageOne`, `HealSelf`). The
snapshot travels on `Encounter.Perks`; it is empty until the Unity side compiles a hand into it (next step). Seven
headless tests in `Tests/PerkTriggerTests.cs` cover cleave, echo, thorns, lifesteal, on-kill and the no-boons control.

**Content trap, found and fixed:** `SerializedProperty.enumValueIndex` is an ORDINAL into the displayed list, so
a sparse enum (ops 0-6, then 10-15) writes the neighbouring value - *Cleaving Blow* first landed on `Lifesteal`.
The generator now writes `intValue`, and `BoonRuntimeFactoryTests` reads the real assets so the mistake cannot
come back silently.

**Guardrails - these are what keep it debuggable (and they are in the code):**

- **Triggered damage never triggers.** `DamageOne` deliberately does not call `RunTriggeredEffects`, so a cleave
  cannot cleave and a chain cannot run away. `MaxTriggeredDepth = 2` is the second lock on that door.
- `MaxExtraHitsPerSwing = 2`, so one swing cannot become a light show.
- Randomness comes from the sim's own `DeterministicRng`, never `UnityEngine.Random`: replays and offline stay equal.
- A triggered hit pays gold and XP exactly like a normal hit, because it goes through the same death path.
- Every triggered hit is a normal damage number in the log ("Cleave hits Bat B for 3"), so the player can see it.

## 9. Balance and how we prove it

| Knob | First pass | Where |
|---|---|---|
| XP per kill | 5 at stage 1, x1.1 a stage, boss x5 | `BalanceConfig` |
| XP for the first level | 20, x1.18 each | `BalanceConfig` |
| Levels a run | 20 cap | `BalanceConfig` |
| Cards per offer / re-rolls | 3 / 1 | `PerkPool.asset` |
| Rarity weights | by level band (see 6.1) | `PerkPool.asset` |
| Card power | 12 / 25 / 35 / 55 % | the cards themselves |

**The measurement is the point.** `ClimbSimulation` (Balance Lab) already plays the loop headless. Add a boon policy
that drafts like a reasonable player (prefer an op family it lacks) and compare stage reached at a fixed time with
and without boons:

- Target: boons add **15-25% depth** at equal time. Under 10% they are decoration; over 40% the permanent layer stops
  mattering.
- Second number: **picks per run** (target 8-15). More than that and the card interrupts the fight too often.
- Third number: **share of offers taken** (target 70%+). If players re-roll or hesitate, the pool is too flat.

## 10. UX

- Card row: rarity tag, badge (placeholder letter until art), name, `All heroes` / `Mage only`, and the effect line.
- Trigger cards read `On hit:` in front of the effect, so the shape of the card tells the player when it fires.
- Battle log gets one line per pick (**built**): `Boon: Cleaving Blow`. Since boons have no list page, the feed is
  the only written record of the hand a run is building.
- A boon heal reads `Knight healed for 1  (239/240)`: the feed drops the "by ..." part when no ability is behind the
  heal, instead of printing an empty source.
- No list page. If clarity suffers, add a count to the XP bar (`LV 4 - 6 boons`) before anything bigger.
- Sound: `SfxCue.LevelUp` already exists; a card tap should reuse the purchase sound.

## 11. Phasing

| Phase | Ships | Acceptance |
|---|---|---|
| **P0 (done)** | XP, levels, card, 10 stat cards, re-roll, save | a pick changes a stat in the next swing; reload keeps the hand |
| **P1** | trigger foundation (done) + 4 mechanic cards (done) + pick line (done); Overflow, Executioner + 3 legendaries left | cleave hits only the neighbours; echo cannot recurse; thorns can kill; same seed gives the same fight. **Next: two small hooks (overkill carry, low-HP multiplier), then the legendaries** |
| **P2** | level bands + soft pity + diversity rule | a 13+ run sees epics; four flat hands in a row cannot happen |
| **P3** | cheap layer-A cards (XP, gold-on-boss, extra roll, camp heal) | no sim change; `Scholar` visibly speeds the bar |
| **P4** | statuses (burn/poison/chill) | a burn card ticks damage and appears in the log per tick |

Each phase is a separate commit with its own tests; P1 is the only one that touches the fight.

## 12. Risks and the plain downside

| Risk | Downside if ignored | Mitigation |
|---|---|---|
| The sim grows a second effect pipeline | two ways to deal damage = two sets of bugs | one hook pass, triggered damage goes through `TakeDamage` only |
| Rare cards are just bigger commons | the rarity system is cosmetic | epics and legendaries must add a *rule*, not a number |
| Run power outgrows the permanent layer | training and tokens stop mattering | the 15-25% depth target, measured |
| Card pool bloat | offers get flatter as the deck grows | op-family diversity rule, and cards retire by band if unused |
| Pausing on every level annoys an idle player | the run stalls unattended | already accepted (decision 1 of the plan); re-rolls and cap keep picks finite |

## 13. Open decisions

1. **Epic/legendary shape - LOCKED (2026-11-09):** every Epic and every Legendary adds a mechanic; Common and Rare
   are numbers. The sim work is therefore a known list (section 5), not an open-ended one.
2. **Pity tuning**: 4 and 9 offers (above) or none until play says otherwise?
3. **Card count per offer**: stays 3 (reference) or grows to 4 at high level as a "reward" moment?
4. **Re-roll economy**: 1 per run forever, or +1 at level 8 to reward the deep run?
5. **Hero-specific share**: 1 of 3 cards per offer (above) or more, to push build identity?
