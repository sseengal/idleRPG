# Content — zones, difficulty, enemies, abilities, authoring

> Owns: what the player fights and how hard it gets, plus the content pipeline.
> Parent: `Architecture.md` (§3.1 combat SOs, §3.3 pipeline, A2-A4, C4).
> Specs Steps 8, 11, 13, 15 + every future content drop.

---

## 1. Content types

| Type | SO | Volume now | Volume target |
|---|---|---|---|
| Heroes | `HeroData` | 3 | 12-20 |
| Abilities | `AbilityData` | 0 | 3-5 per hero + 8-12 enemy abilities |
| Enemies | `EnemyData` | 4 (3 + 1 boss) | 6 archetypes x 3-4 variants + 1 boss per zone |
| Encounters | `EncounterData` | implicit (1 enemy) | 8-12 per zone |
| Zones | `ZoneData` | implicit (endless stage counter) | 1 every 25-30 stages |
| Affixes | `AffixData` | 0 | 12-20 |
| Milestones | `MilestoneDef` | 0 | 3-5 per zone + account milestones |
| Loot tables | `LootTable` | implicit (gold on enemy) | 1 per enemy archetype + boss + bounty |

## 2. Stage → zone model

```
stage            = (zoneIndex * stagesPerZone) + localStage        (localStage 1..stagesPerZone)
encounter in wave= EncounterData from ZoneData.encounterTableIds, rotated deterministically
wave 1..10       = normal encounters (1-3 enemies)
wave 11          = boss encounter (boss + 0-2 escorts)
localStage % wallEveryStages == 0 -> WALL (hp x wallHpMult, gold x wallGoldMult, milestone chest)
zone end         -> ZoneBoss + zone clear milestone (team slot / new mechanic unlock)
```

`stagesPerZone` default **25**, `wallEveryStages` default **5**. Zone rebase keeps numbers sane (§4.4 of
`Architecture.md`) and gives the player a visible "I finished a thing" beat every ~1-1.5h at the frontier.

## 3. Difficulty curves (replaces flat exponents - K5)

```
DifficultyCurve (SO)
  segments[]      { fromStage, hpGrowth, atkGrowth, goldGrowth, armorGrowth }
  wallHpMult      per wall (e.g. 1.5)
  wallGoldMult    (e.g. 1.3)
```

Starting point (tune in Step 15 with Balance Lab):

| Band | Stages | hpGrowth | atkGrowth | goldGrowth | Intent |
|---|---|---|---|---|---|
| Onboarding | 1-10 | 1.15 | 1.08 | 1.12 | never fail, teach upgrades |
| Early | 11-25 | 1.14 | 1.08 | 1.12 | first walls, first ascension |
| Mid | 26-100 | 1.13 | 1.07 | 1.115 | prestige + relics carry |
| Late | 101+ | 1.12 + zone rebase | 1.06 | 1.11 | automation + L2 layers |

Invariant: **player power must out-grow content at the frontier while idle** (E13 checks: frontier clear
2-3 min, wall breaks within 3 min of accumulated income).

## 4. Enemy archetypes (`EnemyData.archetype`)

| Archetype | Shape | Behaviour | Counter |
|---|---|---|---|
| `Swarm` | 3 per encounter, low HP | fast, weak hits | AoE abilities, cleave |
| `Brute` | 1-2, high HP/def | slow, heavy hits | armorPen, crit |
| `Ranged` | 1-2, back row | hits backline (ignores row protection) | backline protection, haste burst |
| `Caster` | 1-2, mid HP | applies debuffs/dots | cleanse, resist |
| `Elite` | 1, buffed variant of another archetype | extra ability, +rewards | target priority |
| `Boss` | 1 (+ escorts) | scripted ability cycle, enrage below 30% HP | burst phase, defensive cooldowns |

Each archetype carries 0-2 `AbilityData` refs. Enemy abilities are the main source of "the fight has a shape"
before heroes have many abilities - ship them at Step 12 with the pipeline.

## 5. Encounter budget (keeps idle rate stable - K1)

```
encounterHpBudget(stage)   = f(stage, DifficultyCurve)          # one strong enemy worth
enemyHp                    = encounterHpBudget / enemyCount * bulkFactor(archetype)
encounterGoldBudget(stage) = f(stage, goldGrowth) * rewardMultiplier(affixes, zone)
enemyGold                  = encounterGoldBudget / enemyCount
```

Rules:

1. **3 weak enemies must take about as long as 1 strong one** - otherwise multi-enemy changes the idle rate.
2. Total gold per encounter is preserved (split across enemies) so `gold/s` and the offline payout stay put.
3. `EnemiesPerWave` (the existing half-wired knob) becomes the live encounter-size control.
4. Boss encounters add gold/gems but never a "jackpot" that distorts the rate by more than ~2x a normal encounter.

## 6. Affixes (player-chosen difficulty = reward multiplier)

| Affix | Effect | Reward mult |
|---|---|---|
| `EnemyHpUp` | +40% enemy HP | x1.25 gold |
| `EnemyAtkUp` | +25% enemy attack | x1.2 |
| `FasterEnemies` | -20% enemy attack interval | x1.2 |
| `NoHealing` | stage advance does not heal | x1.3 |
| `Shielded` | +15% enemy armor | x1.25 |
| `Enraged` | bosses enrage at 50% HP | x1.35 |

Affixes are opt-in per zone (toggled in the battle screen), stack up to 3, and always trade difficulty for
reward - the classic idle self-tuning lever. `AffixData` is an SO row; the sim consumes them as run modifiers
in `SimContext.Rules`.

## 7. Ability design rules

1. Every ability must be **auto-cast**. No ability requires input; the player tunes *priority + unlock*.
2. Every ability has: a trigger, a target rule, 1-3 effects, a scaling stat, tags, and a visible cue (log line + VFX key).
3. Cooldown + energy: `cooldownSec` primary; `charges` for "stored" abilities; energy only if a hero's kit needs it.
4. Role kit template:
   - `Tank`: taunt/guard (redirects a share of damage), shield, thorns
   - `Dps`: single-target burst, cleave/AoE, execute (bonus below X% HP)
   - `Support`: heal, haste buff, cleanse
5. Tags drive synergy: `Fire/Burn`, `Frost/Chill`, `Physical`, `Ranged`, `Holy`. Two abilities may key off tags
   (e.g. "deal +20% to Burning targets") - no tag-specific code, only `EffectSpec` conditions.
6. Numbers: an ability's contribution should be **10-30% of a kit's output**, never 60%+ (idle games punish
   burst-only scaling because offline rate is averaged).
7. The pipeline order is fixed (see `Sim-Core.md` §8); abilities never bypass it.

## 8. Authoring pipeline (spec → generate → validate)

1. **Spec files** live in `Assets/IdleRPG/Content/Specs/*.json` (heroes, abilities, enemies, encounters, zones,
   affixes, tracks, loot). One file per content type; human-editable, diffable, reviewable in PRs.
2. `ContentGenerator` (Editor menu `Tools > Idle RPG > Content > Generate From Specs`) creates/updates SO assets
   idempotently, keyed by `id`; never deletes by default (a `--prune` toggle exists behind a confirm dialog).
3. `ContentValidator` (Editor menu `Tools > Idle RPG > Content > Validate`) checks:
   - unique ids per type; filenames match ids
   - every reference resolves (`abilityIds`, `lootTableId`, `encounterTableIds`, `statId`, `currencyId`, icon/sprite)
   - curves monotonic + caps sane; no `NaN`/`Infinity`/negative costs
   - every hero: >= 1 ability, role + tags set, base stats inside the archetype band
   - every zone: >= 1 boss encounter, wall cadence set, reward multiplier in range
   - every ability: valid trigger + >= 1 effect + scaling stat that exists in `StatDefinition`
   - balance bands: stage-1 clear 90-180s, frontier clear 2-3 min (uses the same headless sim as Balance Lab)
   - no orphan assets (an SO with no spec entry is reported, never deleted silently)
4. Validation output: per-item PASS/FAIL list in the console + a CSV under `Temp/content-report.csv`.

## 9. Hero-add checklist (must stay 100% data)

1. Add a `HeroData` entry to `heroes.json` (id, name, icon key, role, tags, base stats, interval, rarity, unlock).
2. Add 1-3 ability entries in `abilities.json` (+ their `EffectSpec` rows).
3. Add the hero to loot pools / unlock sources if it is a shard hero.
4. Run `Generate From Specs` → `Validate`.
5. Balance Lab: verify the hero's DPS/eHP sits in the intended band vs the existing roster.
6. No code change, no scene change (the Team screen builds rows from the roster).

## 10. Balance Lab (the only regression mechanism allowed - E13)

| Tool | What it does | Bounded work |
|---|---|---|
| `Simulate Stage` (n) | runs one stage headless, prints clear time, kills, gold, dps, ehp | 1 stage |
| `Sweep Stages` (from..to) | table of clear time / gold per stage + wall detection | user-specified range, e.g. 1..60 |
| `Curve Plot` | exports log-space player-power vs content-power CSV (Excel/Sheets) | same range |
| `Roster Compare` | every owned hero's dps/ehp at a given stat-level budget | roster size |
| `Offline Table` | existing payout table (kept) | fixed 12 rows |
| `Golden Numbers` | the MVP baseline table (§6 `Sim-Core`/§12) compared against the current build | 1 run |

Rules: single invocation, no background loops, no auto-tuning, results pasted into the step log.

## 11. Numeric + difficulty budget guards

| Guard | Where | Rule |
|---|---|---|
| Encounter size | `SimCaps.MaxEnemiesPerEncounter` | 3 now, 5 max; validate the portrait layout before raising |
| Status cap | `SimCaps.MaxStatusesPerCombatant` | 8; lowest-priority status is evicted (with a log line) |
| Trigger depth | `SimCaps.MaxTriggerDepth` | 3; deeper triggers are dropped and counted |
| Exponent safety | `DifficultyCurve` | cumulative stage exponent <= 600 (keeps values under ~1e260) |
| Gold sanity | `ContentValidator` | no encounter may pay more than 2.5x the zone's average encounter gold |
| Wall cadence | `ContentValidator` | walls every 5 local stages (+-1), boss every 11th wave |

## 12. Open questions

1. `EncounterData` per zone (8-12 assets) vs a single table with weights per zone range? Start per-zone (clearer
   authoring), merge if the count explodes.
2. Are elite enemies their own `EnemyData` or a "buff wrapper" over a base enemy? Prefer wrapper (one asset, two roles).
3. Do affixes apply to bosses too (`Enraged` implies yes)? Decide in Step 15 with Balance Lab numbers.

