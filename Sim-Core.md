# Sim-Core — pure simulation design

> Owns: the Unity-free combat + reward simulation (`Assets/IdleRPG/Scripts/Sim/`).
> Parent: `Architecture.md` (§2 layers, §3 data catalog, §4 save, E1-E14 rules).
> Specs the Steps 7-13 changes; acceptance tests live in `Roadmap.md`.
> **Rule: this folder never references `UnityEngine`** (no `Mathf`, no `Vector2`, no `Debug`).

---

## 1. Current inventory (what exists, what moves)

| Today | Type | Action |
|---|---|---|
| `Combat/CombatSimulator.cs` | 1 enemy vs N heroes, `Step(dt)`, inline damage, global crit | Becomes `Sim/EncounterSimulator` (multi-enemy, pipeline) |
| `Combat/HeroCombatant.cs` | HP/ATK/DEF/interval + Tick/TakeDamage | Becomes `Sim/Combatant` (unified, stat-block driven) |
| `Combat/EnemyCombatant.cs` | scaled enemy from `EnemyData` | Becomes `Sim/Combatant` + `EnemyFactory` |
| `Combat/CombatScaling.cs` | flat rules struct | Becomes `Sim/SimContext` (+ rng, + mode, + rules sub-objects) |
| `Data/EnemyTargetingMode.cs` | global targeting enum | Moved to per-archetype/per-ability rules (`Sim/Targeting`) |
| `Progression/ICombatStatProvider.cs` | 4 stat getters + 3 global multipliers | Replaced by `StatBlock` + `ProgressionService` (kept as a thin adapter during Step 7-13 migration) |
| `Combat/CombatManager.cs` | coroutine wave flow | Split: `Combat/CombatDirector.cs` (flow) + `Core/RunController.cs` (driver) |

## 2. Target file layout

```
Sim/
  SimContext.cs            rules snapshot + rng + SimMode + caps
  SimMode.cs               Live | FastForward | Offline
  DeterministicRng.cs      seeded splittable streams
  Stats/
    StatId.cs              string constants ("atk","hp","def","crit","critDmg","haste","lifesteal","armorPen",...)
    StatBlock.cs           id -> value, aggregation, derived getters
    StatAggregator.cs      collects sources into the final block
  Combatant.cs             identity, side, row/column, stat block, hp, statuses, ability runtimes
  CombatantSide.cs         Party | Enemy
  Formation.cs             slot layout + row rules + positional modifiers
  Encounter.cs             enemy list, wave identity, budget, lifecycle
  EncounterFactory.cs      EncounterData -> Encounter instances (deterministic)
  Targeting/
    ITargetRule.cs         side-agnostic target selection
    TargetRules.cs         FrontMost, LowestHp, HighestThreat, Random, BacklineFirst, All, Self, LowestHpAlly, ...
    TargetResolver.cs      caches + validates picks per swing/effect
  Effects/
    EffectSpec.cs          data mirror (op, magnitude, duration, stacking, target rule)
    EffectPipeline.cs      ordered damage/heal resolution
    EffectResult.cs        structured outcome for UI + ledger
  Abilities/
    AbilityRuntime.cs      cooldown/charges/energy + trigger evaluation
    TriggerKind.cs         OnCooldown, OnHit, OnKill, OnDamaged, OnAllyDeath, OnLowHealth, OnEncounterStart, Passive
  Statuses/
    StatusInstance.cs      id, stacks, remaining, tick cadence, source
    StatusContainer.cs     apply/refresh/stack/expire/tick, tag queries
  Rewards/
    RewardResolver.cs      LootTable -> reward bundles (no currency mutation)
    RewardBundle.cs        currencyId -> amount
  SimLedger.cs             measured rates (gold/s, kills/s, s/stage, party dps, ehp)
```

## 3. `SimContext` — the sim's whole world

```
SimContext
  Rules: SimRules            # snapshot of BalanceConfig + zone/curve values (no SO refs)
  Rng:   DeterministicRng
  Mode:  SimMode             # Live | FastForward | Offline
  Caps:  SimCaps             # max entities, max effects/step, max trigger depth, max steps/frame
```

- Built by `CombatDirector` from `BalanceConfig` + `ZoneData` + run modifiers when an encounter starts.
- Immutable during an encounter. Changing balance mid-fight = new encounter (today's `ApplyScaling` behaviour).
- `SimMode.Offline` suppresses view-only events (no damage-number/log events) but keeps numeric results identical.
- `SimCaps` (new): `MaxTriggerDepth = 3`, `MaxStatusesPerCombatant = 8`, `MaxEnemiesPerEncounter = 3` (5 later),
  `MaxStepsPerFrame` (guards a hitch), `MaxOfflineSimSeconds` (see §13).

## 4. Stats

**Problem today:** `HeroStatType{Attack,Health,Defense}` + `ICombatStatProvider.GetMaxHealth/GetAttack/GetDefense/GetAttackInterval`
means every new stat edits an interface, a resolver, a save record and the UI rows.

**Target:** stats are data.

```
StatDefinition (SO)      id, displayName, icon, base, growthPerLevel, cap, isPercent,
                         aggregation{Additive, Multiplicative}, tags[], format
StatBlock                Dictionary-free runtime: parallel arrays of (id, value), built once per encounter
StatAggregator           sources -> block:  base(hero/enemy) + track levels + relics + statuses + auras
                                           + run modifiers + prestige multipliers
```

Aggregation order (locked):

```
final = (base + sum(additive contributions)) * prod(multiplicative contributions) * globalMultipliers
clamp: atk/hp/def >= 0 ; crit/critDmg/haste/lifesteal/armorPen clamped to their StatDefinition caps
derived: attackInterval = baseInterval / (1 + haste)   (haste additive, capped - A8)
         effectiveArmor = clamp(armor%, 0, ArmorCap) ; pen reduces it (A3)
```

Starter stat set (data rows, no code):

| id | base source | notes |
|---|---|---|
| `atk` | hero/enemy data | |
| `hp` | hero/enemy data | |
| `def` | hero/enemy data | flat, early game only |
| `armor` | new | % mitigation, replaces flat DEF scaling late (A3) |
| `armorPen` | new | reduces target armor |
| `crit` | new | per-entity (A1) |
| `critDmg` | new | per-entity (A1) |
| `haste` | new | additive % attack speed (A8) |
| `lifesteal` | new | % of damage dealt healed |
| `dmgTaken` | new | negative = mitigation (buff/debuff carrier) |
| `dmgDealt` | new | generic damage % |

`StatId` is a static class of string constants; **ids are the save keys** (schema v3 `statLevels`).

## 5. Combatant model (hero and enemy are the same thing)

```
Combatant
  Identity   id, displayName, side{Party,Enemy}, archetype/role, data ref (HeroData | EnemyData)
  Position   row{Round, Back}, column, slotIndex          # formation (K2)
  Stats      StatBlock (computed once per encounter, refreshed on modifier change)
  Health     maxHealth, currentHealth, shield, IsAlive
  Timers     attackTimer, ability timers[], status ticks
  Statuses   StatusContainer
  Abilities  AbilityRuntime[]
  Threat     threatValue (aggro weight: damage dealt + taunt modifiers)
```

Unified so targeting, effects and statuses never special-case side. Differences:

| Concern | Hero | Enemy |
|---|---|---|
| Position | from `Formation` slots | from `Encounter` layout (row/column in `EncounterData`) |
| Scaling | `ProgressionService` stat levels + relics + prestige | `DifficultyCurve` + archetype + affixes |
| Rewards | none (kills reward) | `LootTable` per archetype/boss |
| Revival | heal on stage advance (`healHeroesOnStageAdvance`) | none |

Row/column rules (locked):

1. Enemies with `row = Round` must be dead before `row = Back` enemies can be targeted - **unless** the attacker
   has the `Ranged` tag or the ability's target rule says otherwise (`BacklineFirst`).
2. Party back row takes `backRowDamageTakenMultiplier` (default 0.75) damage while its front row has a living
   member; once the front row is empty the back row is exposed at 1.0.
3. A `Ranged` enemy always ignores row protection when picking its target (it is the counter to a stacked back row).
4. Taunt/guard statuses force targeting regardless of row rules (they are how tanks keep working late game).

## 6. Targeting

```
ITargetRule: Pick(Combatant[] candidates, TargetingContext ctx) -> Combatant
TargetRules: Self | FrontMost | LowestHp | LowestHpPercent | HighestThreat | Random |
             BacklineFirst | HighestAtk | All | AllEnemies | LowestHpAlly | MostInjuredAlly | RandomAlly
TargetingContext: attacker, side, rng, rowRules, tauntOverrides, previousTarget
```

- Replace the global `EnemyTargetingMode` enum (A2) with rules resolved per **attacker**: enemy archetype default
  (`EncounterData.enemyTargetRule`) + per-ability override (`AbilityData.targetRule`).
- Hero basic attacks use a per-hero `HeroData.targetRule` (default `FrontMost` for melee, `BacklineFirst` for `Ranged`).
- Resolve the target **per swing/effect**, not per frame; cache within a step.
- Deterministic: `Random` rules use the sim RNG, so offline and live agree.

## 7. Encounter

```
Encounter
  Id (stage, wave, isBoss), SimContext, heroes[], enemies[] (1..MaxEnemiesPerEncounter)
  Phase{Begin, Fighting, Resolving, Cleared, Wiped, TimedOut}
  Rewards (RewardBundle from RewardResolver)
  Step(dt) -> CombatEvent[]  # structured, index-tagged (fixes D3)
  Result { cleared, wiped, secondsElapsed, damageDealt[], damageTaken[], kills[] }
```

- `EncounterFactory` builds enemies from `EncounterData` + `DifficultyCurve` + affixes + rng seed, applying the
  HP/gold budget split (§5 of `Content.md`).
- Event payloads gain `enemyIndex` / `heroIndex` (fixes D3 so log and UI can attribute with 2-3 enemies).
- `Result` is what the ledger consumes; it is also what a headless Balance Lab run returns.

## 8. Effect pipeline (the only place damage/healing happens)

Ordered stages - every ability, auto-attack, dot, relic and affix goes through this:

```
1  Gather      sources: stats, statuses, auras, relics, formation modifiers, run modifiers, prestige globals
2  Additive%   sum all "+X%" modifiers into one bucket per damage type
3  Multiply    multiply multiplicative buckets together
4  Flat        add flat contributions (rare; mostly for shields/heals)
5  Clamp       per-attack-type floor (A4): Basic 0.15*atk, Heavy 0.08, Dot 0.05; never below 1
6  Crit        per-entity roll (crit, critDmg) - replaces the global crit (A1)
7  Mitigate    armor% with armorPen (A3)  ->  damage * (1 - clamp(armor - pen, 0, ArmorCap))
8  Apply       damage -> shield -> health; heal -> missing health; record EffectResult
9  Triggers    on-hit / on-crit / on-kill / on-damaged / on-ally-death, depth-capped (SimCaps.MaxTriggerDepth)
10 Lifesteal   attacker heals (lifesteal% x damage dealt)
11 Log         EffectResult -> events (suppressed in SimMode.Offline)
```

Rules: no stage may be bypassed by an ability; a stage that needs data (e.g. per-type floors) reads it from
`SimContext.Rules`/`StatDefinition`, never a literal (E8).

## 9. Abilities

```
AbilityRuntime
  data: AbilityData            trigger, cooldown, charges, target rule, effects[], scaling
  timer, chargesLeft, enabled
  Evaluate(Combatant owner, TargetingContext ctx) -> EffectRequest[] (or nothing)
  OnEvent(TriggerKind, CombatEvent ctx)             # reactive triggers (on-kill, on-damaged, ...)
  Advance(dt)                                       # cooldown/charge regeneration only
```

- Actives fire by themselves (auto-cast, idle-safe). Priority = a per-hero list the player reorders in the UI;
  ties resolve by `AbilityData.priority` then id (deterministic).
- Reactive triggers resolve in the **same step** that raised them, depth-capped so an on-kill chain cannot explode.
- Energy is optional: `charges` covers stored casts. If a kit needs energy it is a `StatBlock` stat
  (`energyMax`, `energyRegen`) - still data-only.
- Enemy abilities use the identical runtime: an enemy kit is just `AbilityData` refs on `EnemyData`.
- Scaling: `value = owner.stats[statId] * coeff + flat`, times the ability's level multiplier from its track.
- Buff/debuff effects are emitted as `StatusInstance` (§10), never as direct stat mutations.

## 10. Statuses

```
StatusInstance
  id, sourceId, tags[], stacks, maxStacks, stackMode{Refresh, Stack, Extend}
  remainingSec, tickSec, tickAccumulator
  effects[] (EffectSpec rows re-applied while active), priority (eviction order)

StatusContainer
  Apply(instance) -> merged / refreshed / evicted
  Remove(id) / Dispel(tag, count) / Clear()
  Tick(dt) -> EffectRequest[]            # dot / hot ticks
  Query: HasTag(tag), Stacks(id), ModifiersFor(statId)
```

Rules: a status is the **only** way a temporary modifier exists. `SimCaps.MaxStatusesPerCombatant` evicts the
lowest `priority` (with a log line). Expiry is exact; dots/hots tick on accumulators (deterministic, no frame
coupling). Immunities are a `Combatant` flag set by a status/relic, never a special case in damage code.

## 11. SimLedger (the bridge to the idle economy)

```
SimLedger
  Sample(dt, EncounterResult)   # rolling 60s windows, multi-currency
  Rates:     goldPerSecond, shardsPerSecond, essencePerSecond, killsPerSecond, secondsPerStage
  Qualities: partyDps, partyEhp, frontierStage, wallStage, wallBreakEtaSeconds
  Persist(ledger) -> SaveData.ledger      # offline reads these first (AD6)
```

- Written by the reward funnel only (AD7). A payout that skips the ledger is a bug.
- `EconomyRateTracker` keeps its currency-observer role; the ledger aggregates instead of one hardcoded stream.
- Balance Lab + dev overlay read the same object, so what the player feels is what the tools measure.

## 12. Determinism

| Rule | Why |
|---|---|
| One `DeterministicRng` per encounter, seeded from `SaveData.encounterSeed` | A reload continues the same fight |
| Sub-streams by purpose (targeting, crit, loot) so adding a roll never shifts another roll | Old numbers stay comparable |
| `rngState` persisted on save | Offline and live agree after an app switch |
| No `DateTime`, `Time` or `System.Random` inside `Sim/` | Identical results in Live / FastForward / Offline |
| Cross-version caveat | `System.Random` sequences are not contractually stable: only the distribution matters |

## 13. Performance & offline bounding

| Concern | Budget | Mechanism |
|---|---|---|
| Step rate | 20/s live (`combatTickIntervalSec` 0.05) | unchanged |
| Entity budget | 5 heroes + 5 enemies, 8 statuses each | `SimCaps` |
| Per-step work | ~25 entities x (abilities + statuses) = a few hundred cheap ops | no per-step allocation |
| Allocation | zero per step | shared arrays; no LINQ, no closures in hot paths |
| Live event volume | 3 enemies multiply damage-number events | pooled text + per-enemy aggregation; log budget exists |
| Offline estimate | never step more than `MaxOfflineSimSeconds` (600s) of sim time | extrapolate by `ledger` rate; analytic path preferred |
| Fast-forward x2-x10 | `SimMode.FastForward` batches steps and coalesces view events | gameplay numbers identical |
| Hitch guard | `MaxStepsPerFrame` caps catch-up after a stall | a stall can never spiral |

## 14. Regression baseline (golden numbers Step 7 must preserve)

MVP headless (seed 12345, `DefaultStatProvider`, pace 1.0):
`wiped=True at stage 2 wave 11, t=185.3s, kills=21, gold=413, crits=22, stage1ClearTime=87.8s`

Pace 1.6 with the shipped roster:

| Enemy | HP | TTK |
|---|---|---|
| Slime | 60 | 3.8s |
| Bat | 90 | 6.3s |
| Goblin | 130 | 11.3s |
| Ogre Chieftain (boss) | 400 | 46.3s |
| Full stage | - | 124s |
| Gold per stage | 276 | 2.2 gold/s |

Step 7 acceptance: these reproduce within a few percent (refactor is behaviour-identical) and the offline table in
`Plan.md` §17 still matches.

## 15. Step acceptance summary (details + status in `Roadmap.md`)

| Step | Sim change | Acceptance |
|---|---|---|
| 7 | `SimContext`, `StatBlock`, `Combatant`, `EncounterSimulator`, `CombatDirector` - no behaviour change | §14 golden numbers reproduce |
| 10 | `Formation` rows/slots + row rules | tank to back row → enemy hits the new front-most; TTK shifts as predicted |
| 11 | multi-enemy `Encounter`, indexed events, HP/gold budget | 3-enemy wave: same gold/s, log attributes per enemy |
| 12 | effect pipeline, statuses, per-entity crit, armor% + pen | enemy enrage line; TTK matches hand-calc; no global crit left |
| 13 | abilities, triggers, auto-cast, enemy kits | cooldown fires deterministically; dps + log change; offline rate consistent |

## 16. Open questions

1. Basic attacks as `AbilityData` (uniform) or a lightweight path? Uniform is cleaner but costs one pipeline call
   per swing x 3 heroes x 20/s - measure in Step 12 before deciding.
2. Energy vs charges for support kits: add energy only if a kit truly needs it.
3. Do statuses persist across waves inside a stage? Proposal: yes for the party (dots keep ticking), enemies always
   spawn clean. Confirm in Step 12.


