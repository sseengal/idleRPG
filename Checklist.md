# Checklist — master progress record

> The single place that answers "what is left to finish the game, and where are we right now?"
> Roadmap detail: `Roadmap.md`. Design: `Architecture.md` + topic docs. MVP journal: `Plan.md`.
> Rule: **one sub-step at a time** - implement, recompile, verify, tick the box, hand off for a manual test.
> Legend: `[x]` done · `[~]` in progress · `[ ]` todo · `[!]` blocked · `[-]` dropped/deferred.

---

## 0. Status

| Field | Value |
|---|---|
| Current step | **8** content pipeline tools + dev overlay (Step 7 complete) |
| Last completed | Step 6 (mobile polish, MVP) |
| Next after this | 7b unified `Combatant` + `Encounter` |
| Save schema | v2 (v3 lands in Steps 10/14 with migration) |
| Shipped build | `Builds/IdleRPG-mac.app` verified (boot, save, offline, combat) |
| Docs | complete (Architecture, Sim-Core, Progression, Content, Idle-Economy, UI-UX, Roadmap) |

## 1. MVP (Steps 0-6) — DONE

- [x] 0 skeleton, asmdefs, portrait lock, git
- [x] 1 data SOs + economy + formulas + save DTOs
- [x] 2 pure combat sim, FSM, crits, balance v1, debug tools
- [x] 3 progression (stat resolver, upgrades, ascension, boosts, ads stub)
- [x] 4 UI + generated scene (header, viewport, dock, damage canvas, popup)
- [x] 4b combat log + scroll views
- [x] 4c page shell + nav + pacing knob
- [x] 5a persistence (atomic write, backup, migrations, autosave, rate tracker)
- [x] 5b offline progress (dual caps, tamper + claim guards, popup)
- [x] 6 mobile polish (runtime policy, settings audit, README, player build verified)
- [ ] **user acceptance pass on hardware** (iOS/Android: offline popup, touch, safe area)

## 2. Foundation (behaviour-preserving)

### 7a — Sim assembly + stat primitives + parity tooling  `[~]`
- [x] `Sim/` folder + `IdleRPG.Sim.asmdef` with **no engine references**
- [x] `SimLog` (delegate logging; the sim must never call `UnityEngine.Debug`)
- [x] `SimMode`, `SimCaps`, `SimRules`, `SimContext`
- [x] `StatId`, `StatAggregation`, `StatBlock` (id -> value, allocation-free read path)
- [x] migrate `HeroCombatant` / `EnemyCombatant` to `StatBlock` (public API unchanged)
- [x] `CombatSimulator` builds a `SimContext` (legacy `CombatScaling` ctor kept for parity)
- [x] `Editor/BalanceLabMenu.cs`: `Golden Numbers` + `Simulate Stage`
- [x] compile clean through the bridge
- [x] `Golden Numbers` / `Sweep Stages` reproduce the shipped balance (see log)
- [ ] **manual test (user)**: Play -> fight, buy, save/reload unchanged
- **Parity log** (`Tools > Idle RPG > Balance Lab > Golden Numbers`, seed 12345, no upgrades):
```
=== Balance Lab: golden numbers (MVP parity) ===
-- pace x1.6 (shipped) --
  Slime      60 HP  TTK  4.0s   (analytic 3.8s)
  Bat        90 HP  TTK  6.0s   (analytic 6.3s)
  Goblin    130 HP  TTK 11.3s   (analytic 11.3s)
  Ogre Chieftain 400 HP TTK 29.0s (pace x1) / 46.3s analytic
  full stage: 117.9s | kills 11 | gold 272 -> 2.31 gold/s
  (analytic baseline 124s / 276 gold / 2.2 gold/s)

=== Balance Lab: stage sweep (pace 1.6) ===
  stage  seconds  kills   gold  gold/s  wiped
      1    117.9     11    272    2.31  no
      2    136.4     11    309    2.27  no
      3    165.1     11    353    2.14  no
      4    197.0     11    382    1.94  no
      5    200.8     11    217    1.08  yes   <- wall without upgrades
      6-10  wipe (as expected for an unupgraded party)
```
**Reconciliation (why the MVP's 87.8s figure is stale):** the tool excludes the director's
`waveTransitionDelaySec` (11 x 0.6s = 6.6s), so 117.9 + 6.6 = **124.5s = the analytic 124s baseline**.
At pace x1.0 the sim gives 74s for stage 1; the MVP's 87.8s was recorded when the boss had 600 HP
(before the x5 -> x4 trim): 74s + ~13.5s boss delta = **87.5s ~= 87.8s**, i.e. behaviour preserved.
Conclusion: 7a is behaviour-identical; the old baseline was measured against a different boss value.

### 7b — Unified combatant + encounter (still 1 enemy)  `[x]`
- [x] `Combatant` (hero/enemy unified: side, row/column, stat block, hp, shield, threat, timers)
- [x] `Encounter` referee (party vs 1..N, target rules, indexed `DamageEvent`/`DeathEvent`)
- [x] `DeterministicRng` (splitmix64, `Fork` sub-streams, persistable state) replacing `System.Random`
- [x] **baseline re-recorded after the RNG swap** (below)
- [x] `CombatSimulator` is now a thin facade over `Encounter`; every existing caller/event unchanged
- [x] `EnemyData.EnemyID` added (stable ids for saves/specs)
- [x] `FormulaUtility` moved into `Sim/` (still Unity-free, namespace unchanged)
- [x] verified: Balance Lab + live Play probe (party/enemy keys, DeterministicRng state, damage applied)
- [ ] **manual test (user)**: Play -> fight/buy/save as before
- **7b baseline** (seed 12345, unupgraded, `Golden Numbers`):
```
pace x1.0 : Slime 2.5s  Bat 3.7s  Goblin 7.0s  boss 28.5s | stage 74s | 272 gold | 3.69 gold/s
pace x1.6 : Slime 4.0s  Bat 6.0s  Goblin 11.3s boss 45.6s | stage 120s | 272 gold | 2.26 gold/s
            (+6.6s of transitions = ~127s vs the 124s analytic figure)
sweep     : stage1 120.4s/272g, stage2 139.5s/309g, stage3 171.5s/353g, wipe from stage 4
live      : Encounter(party 3/3, enemies 0/1) | Slime [enemy:0] | Knight [party:0] 334.7/336
            rng=DeterministicRng state=6018027440424182931
```
Delta vs 7a is crit-luck only (the RNG changed by design); gold/kills identical, stage-1 time within 2%.

### 7c — Director + run controller + GameManager split  `[x]`
- [x] `CombatDirector` accumulator flow (replaces the `WaitForSeconds` coroutine)
- [x] `RunController` as the single gameplay driver (Update -> combat tick + 1s slow chores)
- [x] `GameContext` built once in `BuildContext()` and handed out (nothing hunts the scene)
- [x] `CombatManager` reduced to a 246-line facade (sim = 360-line `Encounter`, flow = 275-line director)
- [!] `GameManager` still 810 lines: the coroutine + slow loop are gone, but snapshot/apply, offline
      evaluation and lifetime stats remain -> they move to `SaveCoordinator`/`IdleTimeService` in Step 9
      (recorded as a deviation rather than claiming the < 150-line target)
- [x] live Play test: waves advance, stage hand-off, wipe/rollback/retry, autosave cadence all verified
- **7c verification log:**
```
[GameManager] Run controller attached | GameContext(ready=True, combat=yes, save=yes)
[7C0] runnerNull=False driving=True
[7C1] after 900 driven updates: stage 6 wave 9 | slowTicks=18 saveCount=62 playTime=207
[GameManager] Stage 5 complete -> now stage 6 | [Economy] Gold=125K Gems=1 Tokens=27
[7C2] (drove 4000 updates) wave=11 state=Boss   <- boss fight takes longer than the window, expected
[GameManager] DEFEAT on stage 90. Rolled back to stage 89, wave 1. Auto-retry=False.
[7C3] state=Defeat stage=89 combatRunning=False
[GameManager] Retry accepted: stage 89, wave 1.
[7C4] state=Combat running=True wave=1
-- gameplay coroutines remaining: none (only UI toast/ascension timers + the mock ad service)
```

### 8 — Content pipeline tools + dev overlay  `[ ]`
- [ ] `Content/Specs/*.json` for heroes / enemies / abilities / zones / loot
- [ ] `ContentGenerator` (idempotent, keyed by id)
- [ ] `ContentValidator` (ids, references, curves, bands) + CSV report
- [ ] dev overlay (F3): DPS, eHP, gold/min, stage ETA
- [ ] local telemetry ring buffer (`Idle-Economy.md` §8)
- [ ] validate clean on the existing catalog; regeneration reproduces today's assets

### 9 — Economy audit: currencies, funnel, IdleTimeService  `[ ]`
- [ ] `CurrencyDef` rows (gold, gems, tokens, shards, materials, essence, scrolls)
- [ ] `EconomyService` single reward funnel + ledger writes
- [ ] gem **sinks** live (fast-forward, offline-cap extension)
- [ ] `IdleTimeService` absorbs `OfflineProgressManager` (caps, tamper, pending claim)
- [ ] acceptance: gems earn + spend; offline numbers unchanged; `F10` still works

## 3. Combat depth

### 10 — Formation (rows, slots, targeting)  `[ ]`
- [ ] `Formation` + `FormationData` (rows/columns, slot unlocks, row rules, positional modifiers)
- [ ] `TargetResolver` + `ITargetRule` set; per-hero default rule
- [ ] save **v3** + `SaveMigrations.v2ToV3` (`partySlots`, keyed `statLevels`, `currency[]`, `tracks[]`)
- [ ] Team screen: roster grid, formation board, tap-swap, auto-arrange, presets
- [ ] battle formation strip; delete `DesiredPartySize` + the fixed `heroViews[]`
- [ ] acceptance: tank to back row -> enemy hits the new front-most; persists across reload

### 11 — Multi-enemy encounters (up to 3)  `[ ]`
- [ ] `Encounter` enemy list + `EncounterFactory` (spawn groups, affix, budget)
- [ ] `EncounterData` rows per zone
- [ ] HP/gold budget split (idle rate preserved)
- [ ] indexed event payloads (`enemyIndex`) + combat log attribution
- [ ] pooled enemy views (1-3) + per-enemy HP/status
- [ ] `enemiesPerWave` becomes live
- [ ] acceptance: 3-enemy wave ~= same clear time and gold/s; log names each enemy

### 12 — Effect pipeline + statuses  `[ ]`
- [ ] `EffectPipeline` (11 ordered stages; per-type floors; armor% + pen)
- [ ] per-entity crit (`crit`, `critDmg`) - delete the global crit from `CombatScaling`
- [ ] `StatusContainer` (stack modes, expiry, dot/hot ticks, tags, eviction cap)
- [ ] `StatAggregator` sources (stats, statuses, auras, run modifiers)
- [ ] prototype enemy ability (enrage below 30% HP) visible in log/UI
- [ ] acceptance: TTK matches hand-calc; trigger depth cap proven

### 13 — Ability system  `[ ]`
- [ ] `AbilityData` + `EffectSpec` rows; `AbilityRuntime` (cooldown/charges/trigger)
- [ ] auto-cast + player-ordered priority
- [ ] 1 ability per existing hero + 3-4 enemy abilities
- [ ] abilities screen (list, levels, equip, priority)
- [ ] acceptance: deterministic cadence; DPS + log change; offline rate consistent

## 4. Meta, difficulty, depth

### 14 — Generic progression tracks (schema v3 core)  `[ ]`
- [ ] `ProgressionTrack` + `CostCurve` rows; `TrackService` as the only purchase path
- [ ] migrate hero stats + prestige upgrades into tracks; drop the `HeroStatType` save enum
- [ ] upgrades UI reads tracks from data
- [ ] acceptance: new track added by spec + generate with no code; v2 save migrates identically

### 15 — Zones, difficulty curves, walls, affixes, guidance  `[ ]`
- [ ] `ZoneData` + piecewise `DifficultyCurve`; stage -> zone mapping
- [ ] walls every N stages (+ milestone chests); zone boss + zone milestones
- [ ] affix chips (max 3) trading difficulty for reward
- [ ] wall guidance banner + "wall breaks in ~N min" ETA
- [ ] Balance Lab `Sweep Stages` + wall detection + curve CSV
- [ ] acceptance: zone 2 gate; band change at walls; wall breaks within ~3 min of income

### 16 — Automation & QoL (churn fix)  `[ ]`
- [ ] `AutomationDef` + `AutomationService` on the 1s tick (auto-buy, auto-ascend)
- [ ] automation UI (enable + threshold per rule) + toast per action
- [ ] fast-forward toggle (x2, ad/gem) reusing `SimMode.FastForward`
- [ ] acceptance: unattended run buys/ascends correctly; config persists

### 17 — Mid game: roster, stars, expeditions, return hub  `[ ]`
- [ ] shards + star-ups (ability slots at 2/4 stars)
- [ ] team 3 -> 4 -> 5 via slot unlocks
- [ ] `ExpeditionDef` / `BountyDef` payouts through `IdleTimeService`
- [ ] return hub overlay (claim -> collect/dispatch -> bounty -> battle, <= 3 taps)
- [ ] daily boons
- [ ] acceptance: expedition pays on relaunch; hub tap count; star-up unlocks a slot

### 18 — Late game: transcendence, relics, codex  `[ ]`
- [ ] L2 prestige (transcendence) + `essence` tracks + new-mechanic unlocks
- [ ] relics: slots, sets, material upgrades (no random rolls yet)
- [ ] codex/bestiary with enemy hints + account milestones
- [ ] acceptance: reset keeps relics/roster/tokens; set bonuses move combat numbers

### 19 — Monetisation: ads, gems, shop, season skeleton  `[ ]`
- [ ] `AdPlacementDef` rows + daily caps; SDK-ready `IAdService`
- [ ] shop SKUs (offline cap, expedition slot, automation slot, fast-forward, no-ads, starter)
- [ ] local season track skeleton
- [ ] acceptance: caps + cooldowns honoured; purchases mutate state + save; no power sold

### 20 — Polish: l10n, a11y, perf, device  `[ ]`
- [ ] string table + settings (font scale, reduced motion, battery mode)
- [ ] sprite atlases, scroll virtualisation, pooling audit
- [ ] device builds: iOS + Android full-loop verification
- [ ] `Mobile Verify Settings` all green; 60fps idle / 30fps battery mode

## 5. Cross-cutting (apply in the step that touches it)

- [ ] every payout path writes `SimLedger` (from Step 9 on)
- [ ] every new screen: data-driven rows + prefab template (no fixed arrays)
- [ ] every new number: SO-driven (no literals inside `Sim/`)
- [ ] `Sim/` stays free of `UnityEngine` (check on every compile)
- [ ] save changes: migration + a real previous-version file test before shipping the step
- [ ] `Roadmap.md` status board + this checklist updated in the same iteration
- [ ] manual Unity test handed off at the end of every sub-step (`.clinerules`)

