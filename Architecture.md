# Idle RPG — Architecture & Scale-Up

> 2D mobile idle RPG. Unity 6000.6.0f1, URP 2D, portrait, iOS + Android.
> Status: MVP shipped (Steps 0-6). This is the **design north star** for everything after the MVP.
> `Plan.md` remains the MVP build journal (what was done + verification logs). This file is *how the game is
> built and how it scales*.

---

## 0. How to use this file

1. Read §1-§2 before touching systems code. §3 is the data contract, §4 the save contract.
2. New feature? Add it to the doc set first (`Roadmap.md` step), then code it.
3. One topic = one file. **Never duplicate a number.** Numbers live in Balance Lab output, pasted per step.
4. §6 is the decision log. To change a locked decision, edit the log line - do not fork behaviour.
5. Every implementation step ends with: scope, files touched, acceptance test, logged result, status.

### Doc map

| File | Covers | Status |
|---|---|---|
| `Architecture.md` (this) | Index, layers, boundaries, data catalog, save schema, rules, decisions | **written** |
| `Sim-Core.md` | Pure sim: `SimContext`, `StatBlock`, `Encounter`, `Formation`, `EffectPipeline`, `AbilityRuntime`, `StatusContainer`, `SimLedger` | **written** |
| `Roadmap.md` | Ordered backlog, Step 7-20 with acceptance tests + status board | **written** |
| `Progression.md` | Currencies/sinks, tracks, roster/stars/shards, relics, prestige layers, automation | **written** |
| `Content.md` | Zones, walls, affixes, encounter budgets, enemy archetypes, content pipeline + validator | **written** |
| `Idle-Economy.md` | Ledger rates, offline model, bounty/expedition payouts, ads/gems/shop, anti-abuse, telemetry | **written** |
| `UI-UX.md` | Screen inventory, return hub, wall guidance, presenter pattern, prefab policy, a11y/l10n | **written** |

---

## 1. Where the MVP stands

Verdict: **the MVP's core ideas are correct and must be extended, not replaced.** Five assumptions are baked
in that block every planned feature. Fix them in dependency order (Steps 7-11).

### 1.1 Keep (extend these)

| Pattern | Why it survives |
|---|---|
| Pure-C# `CombatSimulator.Step(dt)` | One code path for live, fast-forward and offline. Never break this. |
| `CombatScaling` snapshot (sim never touches a ScriptableObject) | Keeps the sim Unity-free and drivable from editor tooling |
| `ICombatStatProvider` seam | The single place progression becomes combat numbers |
| SO data catalog + idempotent generators | Content = assets, not code |
| `GameEvents` static bus, UI binds events only | No polling; screens stay decoupled |
| `SaveData` + version header + additive migrations | Already survived one schema bump cleanly |
| `BalanceConfig.combatPaceMultiplier` as the feel knob | Difficulty tuning never needs a re-pace |
| One reward funnel (`ResolveGoldReward`) | Offline rate stays honest |

### 1.2 Blocking couplings (fix in Steps 7-11)

| # | Evidence in code | What it blocks |
|---|---|---|
| **K1** | `CombatSimulator.Enemy` is a **single** property; `CombatManager.BeginWave()` spawns exactly one `EnemyData` from `WaveConfig.GetEnemyFor(stage, wave, normalWaves)`; `HudController.enemyView` is one `EnemyUnitView` | Any enemy count > 1 (ask #3) |
| **K2** | `ICombatStatProvider(..., int heroIndex)` uses the **lane** as identity; `HeroCombatant.Index` ("0 = front") is read by `EnemyTargetingMode.FrontMost`; `HudController.heroViews[]` is a fixed array filled by lane | Formation swaps (ask #1) |
| **K3** | Stat surface = `HeroStatType{Attack,Health,Defense}` + 4 interface methods + `HeroProgressRecord{attackLevel,healthLevel,defenseLevel}` | Any new stat costs interface churn + a save field (ask #2) |
| **K4** | Damage computed inline in `ResolveHeroAttacks()` / `ResolveEnemyAttack()`; crit = one global roll from `CombatScaling` | Abilities, buffs/debuffs, dots, per-hero crit (ask #2) |
| **K5** | Difficulty = flat compounding (`1.15^stage` HP / `1.12^stage` gold); wave flow = coroutine + `WaitForSeconds(WaveTransitionDelaySec)` | Zones, walls, affixes, and sim == offline encounter timing (ask #4) |

### 1.3 Secondary debt (clear in Steps 7-9, before it spreads)

| # | Debt | Impact |
|---|---|---|
| D1 | `GameManager` = composition root + FSM + save + offline + lifetime stats (~700 lines) | Every feature edits the same file |
| D2 | `BalanceConfig.enemiesPerWave` exists but only feeds the offline estimator; tooltip says "MVP uses 1 for lane clarity" | Half-wired knob; misleading |
| D3 | `EnemySpawned(name,hp,isBoss)`, `EnemyKilled(name,gold)`, `EnemyDamagedInfo` carry **no enemy index** | Log/UI cannot attribute damage with 2-3 enemies |
| D4 | `HeroStatType` enum is the save key; levels are 3 fixed ints | Keyed stats required before content grows |
| D5 | Runtime reachability via `FindAnyObjectByType` fallbacks + panel `EnsureBound()` | Already caused a real `Save == null` bug after a play-mode reload |
| D6 | Scene-authored fixed arrays (`heroViews[]`, single `enemyView`) + hardcoded 3 lanes in the scene builder | Team > 3, 2-3 enemies, roster screens |
| D7 | `UpgradeManager` / `AscensionManager` hardcode cost + effect maths per type | Cost-curve families and new tracks (Step 14) |

---

## 2. Target architecture

### 2.1 Layers (dependency direction is strictly downward)

```
UI ──────────────────────────────────────────────────────────────────
Views (dumb) + Presenters (bind events, format) + data-driven rows
                    ▲
Runtime services ─────────────────────────────────────────────────────
GameContext · GameStateMachine · RunController · SaveCoordinator
OfflineCoordinator · CombatDirector · ProgressionService
RosterService · AbilityService · RewardService · IdleTimeService
                    ▲
Pure simulation (no UnityEngine) ────────────────────────────────────
SimContext · Encounter · Combatant · StatBlock · Formation
TargetResolver · EffectPipeline · AbilityRuntime · StatusContainer
RewardResolver · SimLedger
                    ▲
Data (ScriptableObjects) ────────────────────────────────────────────
HeroData · EnemyData · AbilityData · EffectSpec · EncounterData
ZoneData · DifficultyCurve · StatDefinition · ProgressionTrack
CurrencyDef · LootTable · FormationData · RelicData · MilestoneDef

Tools (Assets/IdleRPG/Editor/, never shipped): Balance Lab · Content Validator + Generator ·
Save Inspector · Scene Builders
```

Rules:

- **Data → Sim → Services → UI only.** The sim never references services, UI, or `UnityEngine`.
- Sim inputs = `SimContext` (rules snapshot + `DeterministicRng` + `SimMode{Live, FastForward, Offline}`) plus a
  party/encounter spec. Sim outputs = events + `SimLedger` + final state. Nothing else.
- Services own all mutable game state. The sim is handed specs/copies, never live service objects.
- UI never mutates gameplay: it calls services and re-renders on events.

### 2.2 Folder contract (`Assets/IdleRPG/Scripts/`)

| Folder | Contains | Note |
|---|---|---|
| `Core/` | `GameContext`, `GameStateMachine`, bootstrap, clock, events, FSM states | Wiring only, no gameplay maths |
| `Sim/` | Pure combat + reward maths (moves out of `Combat/`) | Zero `UnityEngine` references (enforced in review) |
| `Combat/` | `CombatDirector`, encounter/wave/zone flow, view adapters | MonoBehaviour layer |
| `Content/` | SO definitions (`HeroData`, `AbilityData`, `ZoneData`, ...) | Moved from `Data/`; definitions only |
| `Progression/` | Tracks, upgrades, prestige layers, automation rules | Cost curves + purchase logic |
| `Economy/` | Currencies, boosts, ledger, reward funnel | One funnel, one ledger |
| `Save/` | `SaveData` v3, mapper, migrations, system, `IdleTimeService` | IO + schema only, `IdleTimeService` is the only reader of wall-clock time |
| `Services/` | Ads, audio (`IAudioService` + placeholder), telemetry, asset provider, platform | External boundaries behind interfaces |
| `UI/` | Views, presenters, widgets, factories | No gameplay state |
| `Debug/` | Hotkeys, loggers, dev overlay (F3) | Stripped from release builds |
| `Editor/` | Builders, Balance Lab, validators, debug menus | Editor-only code lives only here |

Naming: `XxxData` = SO definition, `Xxx` = runtime object, `XxxService` = stateful service,
`XxxView`/`XxxPresenter` = UI, `XxxRules` = pure rules object.

### 2.3 Runtime flow (one tick, one owner)

```
RunController.Tick(dt)          // the ONLY gameplay driver - no coroutines in the flow
  ├─ CombatDirector.Tick(dt)    // wave/encounter state machine (accumulator + delay timers)
  │    └─ SimContext.Step(dt)   // pure sim: abilities -> effects -> statuses -> targeting -> death
  ├─ SimLedger.Sample(dt)       // gold/s, kills/s, s/stage  (feeds UI, offline, tools)
  ├─ IdleTimeService.Tick()     // boost expiry, expedition timers, daily resets
  └─ SaveCoordinator.Tick(1s)   // autosave cadence, dirty flags, playtime
```

Why: today `CombatManager` waits with `WaitForSeconds` between waves, so an offline or fast-forward run
cannot reproduce live pacing exactly. One accumulator-driven director kills that bug class permanently (K5/A5).

### 2.4 `GameContext` (one locator, frozen lifetime)

```
GameContext
  Balance: BalanceConfig    Waves: WaveConfig          Party: PartyConfig
  Content: IContentCatalog  (heroes/enemies/abilities/zones/tracks by id)
  Economy: EconomyService   Progression: ProgressionService   Roster: RosterService
  Combat: CombatDirector    Save: SaveCoordinator             Offline: OfflineCoordinator
  Time: GameClock           Ads: IAdService                   Telemetry: ITelemetry
  Audio: IAudioService (placeholder tones) -> driven by AudioDirector (events -> cues)
  Assets: IAssetProvider    Ui: UiContext (screen stack, toast, popup host)
```

- Built once in `Awake` by the bootstrap, then injected everywhere. No runtime scene lookups (kills D5).
- `IContentCatalog` wraps asset lookups so nothing calls `Resources.Load` or `Find` ad hoc.
- A service may read `GameContext`; it must never construct a peer service itself.

### 2.5 Service responsibilities (single owner per concern)

| Service | Owns | Must not |
|---|---|---|
| `RunController` | The gameplay tick, run start/stop, defeat/retry policy | Know about file IO or UI widgets |
| `CombatDirector` | Wave/encounter/zone flow, spawn specs, run modifiers | Touch tracking/persistence or `UnityEngine.UI` |
| `ProgressionService` | All tracks (stats, abilities, relics, prestige), purchase rules | Compute combat damage |
| `RosterService` | Owned heroes, slot assignments, formation order, team size | Compute stats (asks `ProgressionService`) |
| `AbilityService` | Equip/level/priority, auto-cast policy | Run cooldowns (sim does that) |
| `EconomyService` | Currencies, boosts, one reward funnel + ledger writes | Decide content rewards (loot tables do) |
| `SaveCoordinator` | Snapshot/apply, cadence, migrations, dirty flags | Contain gameplay maths |
| `IdleTimeService` | Time-based payouts: offline claim + instant income today, expeditions/bounties in Step 17; caps, tamper guards, rate resolution | Duplicate the offline formula |
| `ShopService` | Gem pricing only (what an offer costs, whether it is affordable) | Compute payouts (that is `IdleTimeService`) |

---

## 3. Data catalog (ScriptableObject contracts)

Definitions only - no logic. Every definition has a stable string `id` (the only thing saves store).

### 3.1 Combat definitions

| SO | Key fields | Notes |
|---|---|---|
| `StatDefinition` | `id`, `displayName`, `icon`, `base`, `growthPerLevel`, `cap`, `tags[]`, `isPercent`, `aggregation{Additive,Multiplicative}` | **New in Step 7.** Every stat (atk, hp, def, critChance, critDamage, haste, lifesteal, armorPen, ...) is a data row - code never enumerates stats |
| `HeroData` | `id`, `name`, `icon`, `role{Tank,Dps,Support}`, `tags[]`, `baseStats` (list of statId -> value), `attackIntervalSec`, `rarity`, `unlockRule`, `abilityIds[]`, `formationPref` | Extends today's `HeroData` (identity + base stats) |
| `AbilityData` | `id`, `name`, `icon`, `type{Active,Passive,Aura}`, `trigger`, `cooldownSec`, `charges`, `targetRule`, `effects[]` (`EffectSpec`), `scaling{statId,coeff,flat}`, `tags[]`, `vfxKey`, `levels[]` | Step 13 |
| `EffectSpec` | `op{Damage,Heal,Shield,Buff,Debuff,Dot,Hot,Summon,Modifier}`, `magnitude`, `durationSec`, `tickSec`, `stackMode{Refresh,Stack,Extend}`, `maxStacks`, `targetRule`, `tags[]` | The single vocabulary for every ability, relic and affix |
| `EnemyData` | today's fields + `archetype{Swarm,Brute,Ranged,Caster,Elite,Boss}`, `size` (row footprint), `abilityIds[]`, `tags[]`, `lootTableId` | Extends current `EnemyData` |
| `EncounterData` | `spawnGroups[]` (pool + weights + count), `eliteChance`, `affixPool[]`, `hpBudgetMultiplier`, `rewardMultiplier`, `bossId` | Step 11 |
| `FormationData` | `patterns[]` (rows x columns), `slotUnlockStage[]`, `rowRules{backRowProtected, backRowDamageTakenMult, positionalModifiers[]}` | Step 10 |
| `ZoneData` | `id`, `displayName`, `stageRange`, `enemyPools[]`, `encounterTableIds[]`, `difficultyCurve`, `affixPool[]`, `rewardMultiplier`, `wallEveryStages`, `milestoneIds[]`, `seasonTag` | Step 15 |
| `DifficultyCurve` | piecewise `segments[]` (`fromStage`, `hpGrowth`, `atkGrowth`, `goldGrowth`), `wallMultipliers` | Step 15 - replaces flat exponents |

### 3.2 Progression / economy definitions

| SO | Key fields | Notes |
|---|---|---|
| `ProgressionTrack` | `id`, `name`, `currency`, `costCurve` (`baseCost`, `growth`, `cap`), `effect` (`statId` + value per level OR `EffectSpec`), `prerequisites[]`, `resetPolicy{Ascension,Transcendence,Never}` | Replaces hardcoded per-stat maths (D7) |
| `CostCurve` | `baseCost`, `growth`, `cap`, `curveType{Geometric,Polynomial,Step}`, `milestoneDiscounts[]` | One place for all pricing |
| `CurrencyDef` | `id`, `displayName`, `icon`, `isPremium`, `decimals`, `capPolicy` | Gold, gems, tokens, shards, essence, ... (kills the "dead gems" problem) |
| `LootTable` | `entries[]` (`currencyId`/`itemId`, `weight`, `min`, `max`, `isGuaranteed`), `bonusPerStageGrowth` | One reward vocabulary for enemies, bosses, milestones, bounties |
| `RelicData` / `RelicSetData` | `id`, `slot`, `effects[]`, `setId`, `setBonuses[]`, `upgradeCurve` | Step 18 |
| `MilestoneDef` | `id`, `condition` (stage/zone/kills), `rewards[]` (`LootTable` refs), `repeatable`, `claimMode` | Walls, zone clears, first-time rewards |
| `ExpeditionDef` | `id`, `durationOptions[]`, `rewardTableId`, `slotCost`, `heroRequirement` | Offline idle assignment (Step 17) |
| `BountyDef` | `id`, `weight`, `rewardTableId`, `durationScaling`, `choicesOffered` | Return hub (Step 17) |
| `AutomationDef` | `id`, `unlockCondition`, `target` (auto-buy/ascend/equip), `priorityDefault` | Step 16 |
| `AdPlacementDef` | `id`, `placement{AscendBooster,OfflineDouble,ExpeditionSkip,GemTopUp,...}`, `dailyCap`, `cooldownSec`, `rewardTableId` | Step 19; real SDK = data swap only |

### 3.3 Content pipeline rules

1. Authoring comes from a **spec file** (JSON/CSV) -> `ContentGenerator` (Editor) builds/updates SO assets idempotently.
2. `ContentValidator` (Editor) enforces on demand: unique ids, all references resolve, curves monotonic, caps sane,
   every hero has >= 1 ability, every zone has a boss encounter, no orphan assets, balance bands hit
   (e.g. stage-1 clear 90-180s). Fails loudly with a per-item report.
3. Adding a hero / ability / zone / track is a **spec edit + generate + validate** - never a code change.
4. `IContentCatalog` (runtime) resolves ids -> SO once at boot and caches; sim/presenters only ever see ids + data.

---

## 4. Save contract (schema v3)

### 4.1 Shape

```
schemaVersion: 3
# --- run state ---
zoneProgress   { zoneIndex, localStage, currentWave, highestZone, highestStage, autoRetry. difficulty[affixes] }
encounterSeed  int          # determinism for the current encounter
rngState       long         # sim RNG continuity across a save/load

# --- account ---
roster         [ { heroId, owned, star, shards, unlockedAbilities[], abilityLevels[{id,level}] } ]
partySlots     [ { heroId, row, column, order } ]        # formation (ask #1)
statLevels     [ { key, level } ]                        # key = "heroId.statId" (generic, no enum)
tracks         [ { trackId, level } ]                    # progression tracks (stats, relics, talents, automation)
currency       [ { currencyId, amount } ]                # gold/gems/tokens/shards/essence/...
boosts         [ { boostId, expiresAtBinary, stacks } ]
expeditions    [ { slot, heroId, kind, startedAtBinary, endsAtBinary } ]
milestones     [ { id, claimedCount } ]
automation     { rules[{ id, enabled, threshold }] }
telemetryLocal { sessionsCount, lastSessionSeconds }     # local only, opt-in upload later
# --- meta ---
lifetime       { totalKills, totalGoldEarned, playTimeSeconds, ascensions, transcendentions, saveCount }
ledger         { goldPerSecond, shardsPerSecond, essencePerSecond, secondsPerStage }   # measured rates
timestamps     { lastSaveBinary, lastLogoutBinary }
ui             { lastScreenIndex, settings{...} }
```

### 4.2 Migration table

| From -> To | Rule |
|---|---|
| v1 -> v2 | (done) lifetime stats added, additive defaults |
| v2 -> v3 | `heroes[{heroId,attackLevel,healthLevel,defenseLevel}]` -> `statLevels[{key:"heroId.attack",level}]` etc.; party order -> `partySlots` (row 0, columns 0..2); `prestigeUpgrades[]` -> `tracks[]`; `gold/gems/prestigeTokens` -> `currency[]`; `lastGoldPerSecond` -> `ledger.goldPerSecond`; everything else carries over |
| Rule for **v4+** | Every future bump adds a `SaveMigrations.vNToVN1` method + a golden sample file under `Assets/IdleRPG/SaveSamples/` used by the Save Inspector |

### 4.3 Rules that keep saves safe

1. **Ids only.** No enum values, no asset references, no scene paths, no `int` stat indices in a save.
2. Lists, never dictionaries (`JsonUtility`), and every list entry keyed by a string id.
3. Unknown ids on load are kept (forward compatibility) but flagged; missing ids resolve to defaults + a warning.
4. Write path unchanged from the MVP: atomic `.tmp` -> replace, `.bak`, corrupt quarantine.
5. `SaveCoordinator` is the only writer; `IdleTimeService` is the only reader of wall-clock timestamps.
6. Determinism: `encounterSeed` + `rngState` persist so a reload continues the same fight and offline/live agree.

### 4.4 Numeric safety (idle games overflow)

| Concern | Rule |
|---|---|
| Magnitude | `double` is fine to ~1e300, but **zone rebase** keeps live values in a sane band: zone N starts at base x `zoneRebaseMultiplier^N` only for display/reward, enemy HP always computed from zone-local growth |
| Exponents | clamp total exponent (stage/level) so `pow` cannot exceed 1e300; log a warning once when > 1e100 |
| Precision | above ~1e15 a double cannot show every digit: `NumberFormatter` must stop printing fake precision (K/M/B/T/aa..zz, then scientific) |
| Time | all time maths through `GameClock`; every payout path clamps `awaySeconds >= 0` (rewound clock pays 0) |

---

## 5. Engineering rules (project-specific)

| # | Rule |
|---|---|
| E1 | The pure sim (`Sim/`) has **zero** `UnityEngine` references (no `Vector2`, no `Mathf`, no `Debug`). Enforce in review; a `Sim` asmdef without a Unity reference makes it mechanical |
| E2 | One gameplay driver (`RunController.Tick`). No coroutines, no `WaitForSeconds`, no `Invoke` in gameplay flow |
| E3 | `Time.deltaTime` never enters the sim; the driver passes `dt` explicitly |
| E4 | `[SerializeField] private` for inspector data; public properties for reads; no public fields (`Debug/` exceptions ok) |
| E5 | No `Find*`/`GetComponent` at runtime or in `Update()`; all references come from `GameContext`/`Awake` |
| E6 | Editor-only code lives only in `Editor/`; runtime code never `#if UNITY_EDITOR`-guards gameplay |
| E7 | UI binds to events and reads services; presenters format, views display, neither mutates gameplay |
| E8 | Any new number goes into an SO (`BalanceConfig`, `CostCurve`, `DifficultyCurve`, `StatDefinition`) - never a literal in `Sim/` |
| E9 | Any new payout path must write to `SimLedger` + go through the reward funnel, or offline drifts |
| E10 | Any new screen/row must be built from lists + prefabs; never extend a fixed-size serialized array |
| E11 | Changing a `.cs` -> recompile through the bridge before any play test; never recompile while in Play mode |
| E12 | One small step per iteration, then hand off for a manual Unity test (`.clinerules`) |
| E13 | Balance changes are verified by running the Balance Lab and pasting the numbers into the step log |
| E14 | Docs: update the owning doc + `Roadmap.md` status in the same step as the code |

---

## 6. Decision log

`Locked` = do not re-litigate without editing this row. `Deferred` = agreed direction, not scheduled yet.

### 6.1 Architecture decisions (this document)

| # | Decision | Status |
|---|---|---|
| AD1 | Four layers, dependency downward: Data -> Sim -> Services -> UI; `Sim/` has zero Unity refs | Locked |
| AD2 | One gameplay driver: `RunController.Tick(dt)`; no coroutines in gameplay flow | Locked - **implemented in 7c** |
| AD3 | `GameContext` built once and injected; no runtime `Find*`/`GetComponent` | Locked - **implemented in 7c** (panel `EnsureBound()` fallbacks retire in Step 8) |
| AD4 | `SimContext` snapshot (rules + rng + `SimMode`) is the sim's only input besides specs | Locked |
| AD5 | Save stores **ids + string keys only**; lists not dictionaries; schema v3 generic records | Locked |
| AD6 | Offline always uses the measured `SimLedger` rate first; no second offline formula | Locked |
| AD7 | Every payout path goes through the reward funnel + ledger | Locked |
| AD8 | UI: dumb views + presenters, rows built from lists/prefab templates | Locked |
| AD9 | Content authored via spec -> generator -> validator; features are data, not code | Locked |

### 6.2 Alterations to existing systems

| # | Change | Step | Status |
|---|---|---|---|
| A1 | Global crit -> per-entity crit stats | 12 | Locked |
| A2 | Global `EnemyTargetingMode` -> per-enemy-archetype + per-ability target rules | 11-12 | Locked |
| A3 | Flat DEF mitigation -> armor% + armorPen (flat kept for the early game) | 12 | Locked |
| A4 | Global `minDamageRatio` -> per-attack-type floor | 12 | Locked |
| A5 | Coroutine wave flow -> accumulator `CombatDirector` | 7 | Locked |
| A6 | Split `GameManager` into `GameStateMachine` / `RunController` / `SaveCoordinator` / `OfflineCoordinator` | 7 | Locked |
| A7 | `GameContext` injection replaces `FindAnyObjectByType` fallbacks | 7 | Locked |
| A8 | Keep `combatPaceMultiplier` for feel; haste is additive + capped, never stacked multiplicatively | 12 | Locked |
| A9 | Gold-only income -> multi-currency ledger with one primary offline meter | 9 | Locked |

### 6.3 Additions (idle-genre essentials)

| # | Addition | Step | Status |
|---|---|---|---|
| B1 | Return hub: claim + dispatch expedition + pick bounty in <= 3 taps | 17 | Locked |
| B2 | Wall guidance: "+X% DPS next upgrade", wall-break ETA, ascend preview | 15 | Locked |
| B3 | Automation (auto-buy/auto-ascend/auto-retry) as the **early** prestige reward, not late QoL | 16 | Locked |
| B4 | Gems sink (fast-forward, offline-cap extension, extra expedition/auto-buy slot, rerolls) | 9 | Locked |
| B5 | Monetise efficiency only (cap extension, x2 offline, instant expedition, no-ads, cosmetics) - never power | 19 | Locked |
| B6 | `AdPlacementDef` data + daily caps; real SDK = data swap | 19 | Locked |
| B7 | Daily/weekly layer (boons, bounty rerolls, weekly ladder) | 17 | Locked |
| B8 | Layered prestige L1 ascension / L2 transcendence / L3, each = multiplier **and** a new mechanic | 16-18 | Locked |
| B9 | Pity/guarantee if pulls are ever added | 18 | Deferred |
| B10 | Local telemetry ring buffer + dev overlay (F3): DPS, eHP, gold/min, wall ETA | 8 | Locked |
| B11 | Battery mode: 30fps + trimmed VFX while idling | 20 | Locked |
| B12 | Localization keys + colorblind-safe log colours from day one | 20 | Locked |
| B13 | `IdleTimeService` owns every time-based payout (offline, expedition, bounty) with caps + tamper guards | 9 | **Landed 9b-2** |
| B14 | Instant income (gems -> `seconds x rate x 0.7`) is booked external so buying gold never raises the measured rate | 9b | Locked |
| B15 | Gameplay never calls audio directly: `AudioDirector` maps events -> cues, so Step 20 swaps clips without touching logic | 9b-3 | **Landed 9b-3** |
| B16 | `ResetGame()` = fresh install (save + backups + prefs + scene reload); `DeleteSave()` only removes the file | 9b-4 | **Landed 9b-4** |
| B14 | Codex/bestiary doubles as the difficulty-hint system | 18 | Locked |

### 6.4 Removals / avoid

| # | Item | Status |
|---|---|---|
| C1 | `PartyConfig.DesiredPartySize` hardcode + warning | Remove in Step 10 |
| C2 | `HeroStatType` enum as a save key | Remove in Step 14 (schema v3) |
| C3 | Fixed `heroViews[]` / single `enemyView` arrays | Remove in Steps 10-11 |
| C4 | Gear with randomized stats **before** the stat registry + effect pipeline exist | Avoid (Step 18 at the earliest) |
| C5 | PvP / guilds / leaderboards (needs a server) | Out of scope |
| C6 | Forced/interstitial ads, timed boss failure, punishing-absence timers | Avoid permanently |
| C7 | Player-visible "power score" early | Avoid; show DPS / eHP / gold-per-min / stage ETA |

### 6.5 Preserved from the MVP (do not regress)

| Item | Reason |
|---|---|
| Dual offline caps (8h wall + 2h equivalent) + x0.7 efficiency | Live, verified, and the tamper/double-claim guards depend on it |
| `combatPaceMultiplier` as the single feel knob | Keeps difficulty tuning separate from pacing |
| Atomic save writes, `.bak`, corrupt quarantine, PlayerPrefs logout mirror | Proven in a real player build |
| `ResolveGoldReward` single funnel | Prestige + boost stay consistent everywhere |
| Event-bus UI binding, lazy `EnsureBound()` panel binding | Fixed real click/reload bugs |
| Balance Lab + balance summary tools | Only regression mechanism allowed by `.clinerules` |

---

## 7. Status & next

| Item | State |
|---|---|
| Docs | **complete** - `Architecture.md`, `Sim-Core.md`, `Progression.md`, `Content.md`, `Idle-Economy.md`, `UI-UX.md`, `Roadmap.md` |
| Code changes | none - documentation only |
| Ready to implement | **yes** - start at `Roadmap.md` Step 7 (sim core refactor, behaviour-identical) |
| Per-step contract | owner doc section -> one change -> recompile -> acceptance test -> docs+status -> manual test handoff |

Handoff rules for the next steps: write the doc part for a system, then implement **one** small code change,
then stop for a manual Unity test (`.clinerules` E12). Every code step must update its owning doc and the
`Roadmap.md` status line in the same iteration.




