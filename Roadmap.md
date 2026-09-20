# Roadmap — post-MVP delivery plan

> Owns: **the order of work.** One step at a time, one small change per iteration, manual test, then next step.
> Parent: `Architecture.md` (§6 decision log). Design detail lives in the topic docs, not here.
> Status values: `TODO` | `WIP` | `DONE` | `BLOCKED` | `DEFERRED`.

---

## 0. How a step runs (mirrors `.clinerules` + `Architecture.md` E11-E14)

1. **Read** the owning doc section for the step (column "owner doc").
2. **Implement** one coherent change - no half-migrations, no dead code.
3. **Recompile** through the bridge; never recompile while in Play mode.
4. **Verify** with the step's acceptance test (paste Balance Lab numbers, or probe the live scene).
5. **Update** this file's status and the owning doc if a decision changed.
6. **Stop** and hand off for a manual Unity test before starting the next step.

"Blocked by" is a hard gate: do not start a step until its blockers are `DONE`.

---

## 1. Foundation (behaviour-preserving)

### Step 7a — Sim assembly + stat primitives + Balance Lab  `DONE`
- Delivered: `IdleRPG.Sim` asmdef (`noEngineReferences`), `SimLog`(+bridge), `SimMode`, `SimCaps`, `SimRules`,
  `SimContext`, `IRng`/`SystemRng`, `StatId`/`StatBlock`, combatants migrated to `StatBlock`, `CombatSimulator`
  now driven by a `SimContext`, `SimRulesFactory`, `Editor/BalanceLabMenu` (`Golden Numbers`, `Sweep Stages 1-10`).
- Verified: stage 1 = 117.9s / 272 gold / 2.31 gold/s at pace 1.6 (+6.6s of transitions = the 124s analytic
  baseline); stage 1 at pace 1.0 = 74s, which reconciles to the MVP's 87.8s once the old 600 HP boss is
  accounted for; unupgraded party wipes from stage 5 (expected). Compile clean.

### Step 7b — Unified combatant + encounter  `DONE`
- Delivered: `Sim/Combatant.cs` (one sheet for both sides: side, row/column, stat block, hp, shield, threat,
  timers), `Sim/Encounter.cs` (referee: party vs 1..N, target rules, indexed `DamageEvent`/`DeathEvent`),
  `Sim/DeterministicRng.cs` (splitmix64 + `Fork` sub-streams + persistable state), `HeroCombatant` /
  `EnemyCombatant` became thin subclasses, `CombatSimulator` is a facade over `Encounter` with an unchanged
  public surface, `EnemyData.EnemyID`, `FormulaUtility` moved into `Sim/`.
- Verified: Balance Lab baseline re-recorded after the RNG swap (stage 1: 74s @x1.0, 120s @x1.6, 272 gold,
  2.26 gold/s); live probe shows `Encounter(party 3/3, enemies 0/1)`, party/enemy keys and a live
  `DeterministicRng`; gold/kills unchanged, only crit sequences shifted.
- Note: the `EncounterSimulator` rename was dropped - `CombatSimulator` stays as the runtime facade name so no
  caller churn; the pure engine is `Encounter` inside `Sim/`.

### Step 7c — Director + RunController + GameManager split  `TODO`
- Deliverables: `CombatDirector` accumulator flow, `RunController.Tick`, `GameContext`, `GameManager` < 150 lines.
- Acceptance: identical pacing/defeat/retry in live Play; no coroutines left in the gameplay flow.

### Step 7 (original scope, superseded by the splits above)  `[-]`
- **Owner doc:** `Sim-Core.md` §2-§5, §14
- **Goal:** introduce `SimContext` / `StatBlock` / `StatDefinition` / `Combatant` / `EncounterSimulator` /
  `CombatDirector` / `RunController` / `GameContext` with **zero behaviour change**; split `GameManager` (A6/A7);
  replace the coroutine wave loop with the accumulator director (A5).
- **Deliverables:** `Sim/` folder + asmdef with no Unity refs; `CombatDirector` + `RunController`; `GameManager`
  reduced to composition root (< 150 lines); Balance Lab menu with `Simulate Stage` + `Golden Numbers`.
- **Acceptance:** `Sim-Core.md` §14 golden numbers reproduce within a few percent; the offline payout table in
  `Plan.md` §17 is unchanged; live Play still battles, buys and saves.
- **Blocked by:** nothing. **Risk:** medium (biggest refactor) - mitigated by "no new features" scope.

### Step 8 — Content pipeline tools + dev overlay  `TODO`
- **Owner doc:** `Content.md` §8-§10, `Architecture.md` B10
- **Goal:** spec-driven content + validation + overlay, so every later step can add content safely.
- **Deliverables:** `Content/Specs/*.json`; `ContentGenerator`; `ContentValidator` (report + CSV); dev overlay (F3)
  showing DPS/eHP/gold-per-min/stage ETA from `SimLedger`; local telemetry ring buffer.
- **Acceptance:** `Validate` clean on the existing catalog; regenerating from specs reproduces today's assets with
  no gameplay drift.
- **Blocked by:** Step 7 (needs `SimLedger` + the headless sim).

### Step 9 — Economy audit: currencies, funnel, `IdleTimeService`  `TODO`
- **Owner doc:** `Idle-Economy.md` §1-§3, §7; `Progression.md` §2-§3
- **Goal:** define all currencies (kills the dead-gem problem - B4/C2), one ledger-fed reward funnel, one owner for
  time-based payouts (B13).
- **Deliverables:** `CurrencyDef` rows; `EconomyService` with the funnel + ledger writes; live gem sinks
  (fast-forward, offline-cap extension); `IdleTimeService` absorbing `OfflineProgressManager`.
- **Acceptance:** gems earnable + spendable; the offline claim pays the same numbers as today; the ledger prints
  2+ rates; `F10` rewind still works.
- **Blocked by:** Step 7.

## 2. Combat depth (the player's asks)

### Step 10 — Formation: rows, slots, targeting  `TODO`
- **Owner doc:** `Sim-Core.md` §5-§6; `UI-UX.md` §3, §6; `Architecture.md` C1/C3
- **Goal:** ask #1. Rows/columns, slot unlocks, row targeting rules, positional modifiers, save v3 `partySlots`.
- **Deliverables:** `Formation`/`FormationData`; `TargetResolver` + rules; Team screen (roster + board + swap +
  auto-arrange + presets); battle formation strip; delete `DesiredPartySize` and the fixed `heroViews[]`.
- **Acceptance:** moving the tank to the back row makes the enemy hit the new front-most hero; back-row damage
  reduction applies; the swap persists across save/load; presets switch in one tap.
- **Blocked by:** Steps 7, 8. **Risk:** schema v3 migration - test against the existing v2 save.

### Step 11 — Multi-enemy encounters (up to 3)  `TODO`
- **Owner doc:** `Sim-Core.md` §7; `Content.md` §4-§5; `UI-UX.md` §3
- **Goal:** ask #3. 1-3 enemies per encounter with an HP/gold budget that preserves the idle rate.
- **Deliverables:** `Encounter` + `EncounterFactory`; `EncounterData` rows; indexed event payloads (fixes D3);
  pooled enemy views; per-enemy HP + status; `enemiesPerWave` becomes live.
- **Acceptance:** a 3-enemy wave clears in about the same time as the equivalent single enemy, pays the same
  gold/s, and the log attributes damage per enemy.
- **Blocked by:** Steps 7, 10.

### Step 12 — Effect pipeline + statuses  `TODO`
- **Owner doc:** `Sim-Core.md` §8, §10
- **Goal:** ask #2 foundation: ordered pipeline, per-entity crit (A1), armor% + pen (A3), per-type floors (A4),
  buffs/debuffs/dots as the only temporary modifiers.
- **Deliverables:** `EffectPipeline`, `StatusContainer`, `StatAggregator` sources; one enemy prototype ability that
  applies a visible status (e.g. enrage below 30% HP); the global crit in `CombatScaling` is deleted.
- **Acceptance:** the enemy status shows in log/UI, TTK matches a hand calculation, no global crit remains, and
  `MaxTriggerDepth` demonstrably stops recursion.
- **Blocked by:** Steps 7, 11.

### Step 13 — Ability system  `TODO`
- **Owner doc:** `Sim-Core.md` §9; `Content.md` §7; `UI-UX.md` §6
- **Goal:** ask #2 proper: `AbilityData` + `EffectSpec` rows, `AbilityRuntime` (cooldown/charges/triggers/targets),
  auto-cast with player-ordered priority, 1 ability per existing hero + 3-4 enemy abilities, ability UI.
- **Deliverables:** ability runtime; abilities screen (list/levels/equip/priority); hero kits; enemy kits; cast log
  lines ("Mage casts Fireball -> 3 targets").
- **Acceptance:** a cooldown fires deterministically at the expected cadence, DPS/log visibly change, the offline
  rate stays consistent with the new DPS, and levelling an ability improves its numbers.
- **Blocked by:** Step 12.

## 3. Meta & difficulty

### Step 14 — Generic progression tracks (schema v3 core)  `TODO`
- **Owner doc:** `Progression.md` §3-§4, §10; `Architecture.md` C2
- **Goal:** ask #6 enabler: `ProgressionTrack`/`CostCurve`/keyed `statLevels` replace hardcoded per-stat maths (D7)
  and the `HeroStatType` save enum (C2).
- **Deliverables:** `TrackService` as the single purchase path; hero stats + prestige upgrades migrated to tracks;
  the upgrades UI reads tracks from data instead of deriving costs.
- **Acceptance:** buying behaves as today, a **new** track can be added by spec + generate with no code, and a v2
  save migrates to identical numbers.
- **Blocked by:** Steps 7, 9, 10 (extend the same schema version if Step 10 already bumped it).

### Step 15 — Zones, difficulty curves, walls, affixes, guidance  `TODO`
- **Owner doc:** `Content.md` §2-§6; `UI-UX.md` §5
- **Goal:** ask #4/#5. Replace flat exponents with a piecewise `DifficultyCurve`; zones + walls + affixes +
  milestones; wall guidance (B2).
- **Deliverables:** `ZoneData` + `DifficultyCurve`; zone/wall logic in `CombatDirector`; affix chips UI; milestone
  chests; wall guidance banner + ETA; Balance Lab `Sweep Stages` + wall detection.
- **Acceptance:** zone 2 unlocks after the zone-1 boss; the growth band changes at a wall (Balance Lab proof); a
  wall breaks within ~3 min of accumulated frontier income; guidance numbers match the ledger.
- **Blocked by:** Steps 8, 11, 12, 14.

### Step 16 — Automation & QoL (the churn fix)  `TODO`
- **Owner doc:** `Progression.md` §8; `Architecture.md` B3
- **Goal:** make idling pay off before boredom sets in: auto-buy, auto-ascend, auto-retry (exists), presets.
- **Deliverables:** `AutomationDef` rows + `AutomationService` on the 1s tick; automation UI; toasts per action;
  fast-forward toggle (ad/gem powered, reuses `SimMode.FastForward`).
- **Acceptance:** leave the game running - auto-buy spends inside its budget, auto-ascend fires at the threshold,
  every action toasts, and the config persists across save/load.
- **Blocked by:** Steps 14, 15.

## 4. Depth, money, polish

### Step 17 — Mid game: roster, stars, expeditions, return hub  `TODO`
- **Owner doc:** `Progression.md` §5; `Idle-Economy.md` §2-§4; `UI-UX.md` §4
- **Goal:** ask #5 + B1/B7: roster growth, team 3→5, offline expeditions, bounties, dailies.
- **Deliverables:** shards + star-ups; team slot unlocks; `ExpeditionDef`/`BountyDef` payouts through
  `IdleTimeService`; return hub overlay; daily boons.
- **Acceptance:** an unassigned hero can run an expedition that pays on relaunch at the capped rate; the hub
  completes in <= 3 taps; a star-up unlocks an ability slot as designed.
- **Blocked by:** Steps 13, 14, 15.

### Step 18 — Late game: transcendence, relics, codex  `TODO`
- **Owner doc:** `Progression.md` §6-§7; `Content.md` §1
- **Goal:** ask #5 late: L2 prestige with a new mechanic, relic sinks, bestiary multipliers.
- **Deliverables:** transcendence reset + `essence` tracks; relic slots/sets/upgrades; codex screen with enemy
  hints; account milestones.
- **Acceptance:** transcendence keeps relics/roster/tokens, grants essence spent on L2 tracks and applies a
  measurable multiplier; relics drop from the intended sources and their set bonuses move combat numbers.
- **Blocked by:** Steps 15, 17.

### Step 19 — Monetisation: ads, gems, shop, season skeleton  `TODO`
- **Owner doc:** `Idle-Economy.md` §5-§6
- **Goal:** B5/B6: efficiency-only monetisation, ad placements as data, gem shop, no-ads, local season track.
- **Deliverables:** `AdPlacementDef` rows + daily caps; SDK-ready `IAdService` surface; shop SKUs (offline cap
  extension, expedition slot, automation slot, fast-forward, no-ads, starter pack); season skeleton (local data).
- **Acceptance:** every placement respects cap + cooldown with the mock service; a purchase path mutates the right
  state and saves; nothing sold grants raw power.
- **Blocked by:** Steps 9, 16, 17.

### Step 20 — Polish: l10n, a11y, perf, device builds  `TODO`
- **Owner doc:** `UI-UX.md` §7-§8; `Architecture.md` B11/B12
- **Goal:** ship-ready: localization keys, accessibility settings, pooling/atlases, battery mode, real device runs.
- **Deliverables:** string table + settings screen (font scale, reduced motion, battery mode); sprite atlases;
  scroll virtualisation; iOS/Android device verification of the full loop.
- **Acceptance:** `Mobile Verify Settings` still all green; 60fps idle on device with 3v3 + statuses; battery mode
  runs 30fps; no layout overflow at 0.85 or 1.15 font scale.
- **Blocked by:** every earlier step.

---

## 5. Dependency map (compact)

```
7 Sim core ─┬─ 8 Content tools ─┬─ 10 Formation ─┬─ 11 Multi-enemy ─ 12 Pipeline ─ 13 Abilities ─┐
            │                   │                └─ 14 Tracks ──────────────────────────────────┤
            └─ 9 Economy ───────┘                                                              │
                                       15 Zones/walls ◄── 8,11,12,14 ──► 16 Automation          │
                                       17 Mid game ◄── 13,14,15 ──► 18 Late game ◄── 15,17      │
                                       19 Monetisation ◄── 9,16,17 ──► 20 Polish ◄── all ──────┘
```

## 6. Status board

| Step | Title | Status | Notes |
|---|---|---|---|
| 7 | Sim core refactor + Balance Lab v1 | TODO | next to start |
| 8 | Content pipeline tools + dev overlay | TODO | |
| 9 | Economy audit: currencies, funnel, IdleTimeService | TODO | |
| 10 | Formation | TODO | |
| 11 | Multi-enemy encounters | TODO | |
| 12 | Effect pipeline + statuses | TODO | |
| 13 | Ability system | TODO | |
| 14 | Generic progression tracks | TODO | |
| 15 | Zones, difficulty, walls, affixes, guidance | TODO | |
| 16 | Automation & QoL | TODO | |
| 17 | Roster, stars, expeditions, return hub | TODO | |
| 18 | Transcendence, relics, codex | TODO | |
| 19 | Monetisation | TODO | |
| 20 | Polish, l10n, perf, device | TODO | |

## 7. Backlog (agreed direction, not scheduled)

| Item | Why it waits |
|---|---|
| Seasons/events with exclusive zones | needs `ZoneData.seasonTag` (Step 15) + a content cadence |
| Hero pulls with pity (B9) | needs roster + shards (Step 17); only if the roster grows past ~12 heroes |
| Cloud save / server authority | needs backend; local-only anti-abuse is sufficient for now |
| Leaderboards / guilds / PvP (C5) | out of scope without a server |
| Cosmetics store | after Step 19 |
| Detailed analytics upload | after local telemetry proves useful |

## 8. Definition of done (every step)

- [ ] Scope matches the owner doc; no unrelated refactor smuggled in
- [ ] Compiles with zero errors through the bridge, recompiled outside Play mode
- [ ] Acceptance test executed and its numbers pasted into this file (or the step log)
- [ ] `Roadmap.md` status + owner doc updated; `Architecture.md` decision log edited if a decision changed
- [ ] No new fixed-size serialized arrays, no runtime `Find*`, no `UnityEngine` inside `Sim/`
- [ ] Save change (if any): migration written + tested against a real v2 save file
- [ ] Manual Unity test handed off; wait for confirmation before the next step


