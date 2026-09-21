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

### Step 7c — Director + RunController + GameContext  `DONE`
- Delivered: `Combat/CombatDirector.cs` (accumulator + simulated-time inter-wave pause, catch-up cap),
  `Core/RunController.cs` (the single heartbeat: combat tick + 1s boost/autosave chores),
  `Core/GameContext.cs` (one box of wired systems, built in `GameManager.BuildContext()`),
  `CombatManager` is now a 246-line facade over the sim + director. No gameplay coroutines remain.
- Verified live: `Run controller attached | GameContext(ready=True...)`, waves advance under a driven loop,
  `Stage 5 complete -> now stage 6`, `DEFEAT on stage 90 -> rolled back to 89`, `Retry accepted` with
  combat running again, autosave cadence (saveCount 62) and play-time accrual intact.
- **Deviation:** `GameManager` is still 810 lines. The coroutine and slow loop are gone, but snapshot/apply,
  offline evaluation and lifetime stats stay there until Step 9 extracts `SaveCoordinator` + `IdleTimeService`.
  Claiming the < 150-line target now would be dishonest; the acceptance test (identical live behaviour,
  no coroutines in the gameplay flow) is met.

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

### Step 8a — Spec files + generator  `DONE`
- Delivered: `Editor/Content/ContentSpecs.cs`, `ContentSpecIO.cs`, `ContentGenerator.cs`, plus shared
  `Editor/Editable.cs` and `Editor/SoField.cs` (extracted from `DataAssetGenerator`, which now uses them).
- Verified: export -> generate -> export is byte-identical; Balance Lab golden numbers unchanged; two real
  bugs caught and fixed (prestige enum spelling flip; unresolved wave references wiping `WaveConfig`).
- Safety rule added: unresolved references abort the write with an error, never silently empty a list.

### Step 8b — Validator + dev overlay + telemetry  `DONE`
- Delivered: `Editor/Content/ContentValidator.cs` (8 check groups + CSV report), `Debug/TelemetryFeed.cs`
  (in-memory ring buffer on the event bus), `Debug/DevOverlay.cs` (runtime F3 dashboard, dev builds only).
- Verified: validator clean on the shipped catalog **and** a negative test with 4 injected faults reported all 4;
  overlay rendered live values (`stage 88 wave 1/11`, `crit x1.05`, `pace x1.6 floor 15%`, `rng state 0x...`,
  `save file x84`), telemetry buffered stage/defeat/save entries.
- Debt: overlay maths moves onto `SimLedger` in Step 9.

### Step 8b (original scope)  `[-]`
- **Owner doc:** `Content.md` §8-§10, `Architecture.md` B10
- **Goal:** spec-driven content + validation + overlay, so every later step can add content safely.
- **Deliverables:** `ContentValidator` (unique ids, resolvable references, sane curves, hero/zone completeness,
  orphan assets, balance bands) + CSV report under `Temp/content-report.csv`; dev overlay (F3) showing party DPS,
  eHP, gold/min, stage ETA, wave/state and rng state; local telemetry ring buffer.
- **Acceptance:** `Validate` runs clean on the shipped catalog; the overlay numbers match Balance Lab.
- **Blocked by:** Step 8a (done).

### Step 9a — SimLedger + reward funnel  `DONE`
- Delivered: `Sim/SimLedger.cs` (rolling rates, pure C#, `Tick(dt)`-driven), `Economy/RewardService.cs` (the single
  till: `GrantGold` = compute+pay, `GrantQuotedGold` = pay the quoted amount, gems/tokens, ledger receipts),
  `EconomyRateTracker` deleted, kills/boss-gems/tokens/offline claim routed through the till, `RunController`
  error containment (G5), overlay now reads the ledger.
- Verified: Balance Lab unchanged (74s @x1.0 / 120s @x1.6, 272 gold, 2.26 gold/s); offline claim pays exactly the
  quoted amount and does not move the measured rate; safe mode trips only after 10 consecutive tick failures.
- Fixed in-step: double-applied multipliers on offline claims (quoted 36,093 paid 57,749) - the two-button split
  makes it structurally impossible.
- Carried to 9b: **G6** save backup rotation.

### Step 9 (original scope, split into 9a/9b)  `[-]`
- **Split:** `9a` ledger + reward funnel + one rate source; `9b` `CurrencyDef` rows, gems sinks, `IdleTimeService`
- **Folded in from the review:** **G5** sim-tick error containment (try/catch + safe mode + autosave on first
  failure) and **G6** save backup rotation (3 versions, tagged with the schema version)
- **Owner doc:** `Idle-Economy.md` §1-§3, §7; `Progression.md` §2-§3
- **Goal:** define all currencies (kills the dead-gem problem - B4/C2), one ledger-fed reward funnel, one owner for
  time-based payouts (B13).
- **Deliverables:** `CurrencyDef` rows; `EconomyService` with the funnel + ledger writes; live gem sinks
  (fast-forward, offline-cap extension); `IdleTimeService` absorbing `OfflineProgressManager`.
- **Acceptance:** gems earnable + spendable; the offline claim pays the same numbers as today; the ledger prints
  2+ rates; `F10` rewind still works.
- **Blocked by:** Step 7.

## 2. Combat depth (the player's asks)

### Step 10 — Formation: rows, slots, targeting  `IN PROGRESS`
- **Owner doc:** `Sim-Core.md` §5-§6; `UI-UX.md` §3, §6; `Architecture.md` C1/C3
- **Goal:** ask #1. Rows/columns, slot unlocks, row targeting rules, positional modifiers, save v3 `partySlots`.
- **Split for testability** (the sim can be proven before any UI exists):
  - **10a `DONE`** - model + row targeting: `FormationData`/`Formation`, row-aware `FrontMost`, `BacklineFirst`,
    per-slot/team-size unlocks, back-row damage reduction, per-hero `role`/`targetRule`, simulator/manager wiring,
    validator coverage. Verified: tank to back row -> enemy hits the new front-most; back-row hit x0.75 exactly;
    exposed when the front row is empty; golden numbers unchanged.
  - **10b `DONE`** - save v3 + `partySlots` + `SaveMigrations.v2ToV3` + a save round-trip drift tool
    (`Save > Round-Trip Drift Test`). Verified: the layout survives a real relaunch, the dirty flag fires on a
    swap, and an older v2 file upgrades additive-only with 0 lines lost.
  - **10c `SUPERSEDED`** - first cut of the board + TEAM tab. Shipped locked slots, presets and battle-screen
    tap-swap; all removed in 10d. Kept in history only.
  - **10d `DONE`** - formation rework: rows change targeting *frequency* (not damage), the front-rank-empty stall
    bug and the duplicated-hero bug are fixed, the board is a vertical two-column layout, battle is display-only,
    editing is Party-only, all slots usable, presets deleted. Party tab structured for stats/gear rows.
- **Blocked by:** Steps 7, 8. **Risk:** schema v3 migration - test against the existing v2 save.

### Step 10e — Even spread inside a rank  `DONE`
- Targeting inside a rank is now an even random draw (was: the front-most hero took every hit). Rank odds and damage
  maths untouched; a single-member rank consumes no RNG, so a one-hero board is unchanged. Golden numbers were
  deliberately re-baselined (79s@x1.0 / 124s@x1.6, 272 gold, per-enemy TTK identical) so Step 11 has one stable
  parity net to build on instead of two.

### Step 11a — Encounter factory + composition + enemy ranks  `DONE`
- **Simplified on purpose:** composition is the wave's enemy plus its rotation neighbours (deterministic from
  stage/wave -> no save change, no second content pipeline). The `EncounterData`/`encounters.json` idea is parked
  until hand-authored mixes are actually needed (11c at the earliest).
- `EncounterFactory` (team size capped at 3, boss always 1, ranks from `EnemyData.preferredRow`, per-enemy rule from
  `EnemyData.targetRule`, wave budget ratios in `BalanceConfig`), `WaveConfig.GetEnemiesFor`, `Combatant.TargetRule`.
- Parity: golden numbers byte-identical, validator + drift clean, factory probe shows identical totals at 1 and 3
  enemies. Also fixed a pre-existing `HeroUnitView` index crash and the "wave ends on first kill" landmine.
- **Ahead:** 11b targeting symmetry (heroes + a ranged enemy archetype), 11c presentation + flipping the switch.

### Step 11b — 1-3 enemies per wave, stacked on the battle page  `DONE`
- **Design change:** enemy ranks dropped (never wanted). Enemies are a flat 1-3 list, stacked vertically on the
  battle page, each with its own sprite, name, HP bar and damage-number anchor.
- A wave now ends on the **last** kill, per-enemy events carry an index, and duplicate names log as Goblin A/B/C.
- Balance: `enemiesPerWave 3` + `waveHealthMultiplier 0.95` -> stage 1 = 126s / 31 kills / 272 gold / 2.16 gold/s
  against the single-enemy baseline of 124s / 11 kills / 272 gold / 2.20. Verified live (screenshot + logs).
- **Parked:** hero-side `targetRule`, a ranged enemy archetype, per-wave counts, hand-authored mixes.
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

### Step 9b-1 — Currencies + gem sink + backup rotation  `DONE`
- Delivered: `CurrencyDef` + 7 currency assets, `ShopService` (gems -> extra offline income cap), BalanceConfig
  knobs, save fields, offline calculator honouring the purchase, code-built shop row, `SaveSystem` 3-backup
  rotation.
- Verified live: bought +1h for 50 gems; a 9h absence paid **10800s** (2h base + 1h bought) vs 7200s before;
  max-out refuses the 4th purchase; save round-trips the bonus; 4 save files on disk (.json/.bak/.bak1/.bak2).

### Step 9b-2 — `IdleTimeService` + instant-income sink  `DONE`
- Delivered: `OfflineProgressManager` renamed into `IdleTimeService` (one owner of time-based payouts), shared
  rate/efficiency/multiplier/external-payment helpers, `FormulaUtility.TimeBasedGold`, instant income gem sink
  (`+1h` for 30 gems, repeatable) and the shop row builder.
- Verified live: purchase paid 32,326 gold for 30 gems while the **measured rate did not move**; a broke player
  is refused without paying; offline window unchanged (3h -> 10800s / 71,678 gold); validator + golden numbers
  clean.

### Step 9b-2b — Dev hotkey catalogue + in-game legend  `DONE`
- One catalogue (`DebugHotkeyCatalog`) feeds the F3 overlay legend and a debug menu printer; new `C` (gems) and
  `F` (instant income) keys added. Overlay panels auto-fit so the legend never overlaps the stats.

### Step 9b-4 — Full reset (`F8`)  `DONE`
- `GameManager.ResetGame()` wipes save + backups + PlayerPrefs and reloads the scene (fresh install); hotkey `F8`
  and a Save menu item call the same method. Verified live end to end.

### Step 9b-3 — Placeholder presentation hooks (audio + icons)  `DONE`
- Delivered: `IAudioService`/`SfxCue`, `PlaceholderAudioService` (procedural tones, no asset files, PlayerPrefs
  volume/mute), `AudioDirector` (events -> cues with throttles), 4 new currency icons wired onto all 7
  `CurrencyDef` assets.
- Verified live: 10 cue paths through the real event bus; silent normal spawns stay silent; mute persists;
  validator + golden numbers clean. Key presses need Editor focus (frames do not tick when backgrounded).
- **Goal:** every place that *should* make a noise or show an icon gets the hook now, with placeholder assets;
  the real polish (sound design, final art, animations) stays in Step 20.
- **Deliverables:** `IAudioService` + `PlaceholderAudioService` (procedurally generated beeps/clicks or silent
  stubs behind the same interface, exactly like `MockAdService`), event-driven SFX calls (hit, crit, enemy died,
  boss wave, level up, purchase denied, offline claim, ascend, defeat), volume/mute in the settings stub,
  `PlaceholderSpriteGenerator` extended for new content types (currencies, abilities, relics, zone banners).
- **Acceptance:** muting silences everything; every wired event produces exactly one cue; swapping to a real
  SDK/asset pack is a single implementation change.
- **Note:** placeholders are generated on demand and are expected to look/sound crude until Step 20.

### Step 21 — CI, versioning & release readiness  `TODO`
- **Gaps:** **G8** scripted `Unity -batchmode` build + tag-driven versioning; **G9** version/save-compat policy;
  **G7** crash/ANR reporting; **G10** analytics opt-in + privacy decision; **G15** store/legal checklist
  (privacy URL, age rating, data-safety form, ad disclosure, iOS ATT).
- **Acceptance:** one command produces a signed-ish dev build with the right version; the checklist is written down.

### Step 22 — Local notifications  `TODO`
- **Gap G2.** "Your offline cap is full", expedition finished, daily reset. Platform plugin + permission flow +
  an in-app toggle; must never fire when notifications are denied.

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

### Step 20 — Visual/audio/UX polish + l10n + a11y + perf  `TODO`
- **Also owns:** **G4** settings screen (audio, notifications, font scale, reduced motion, battery, language),
  **G11** real art pipeline (atlases, import presets, Addressables when content grows), **G12** device perf
  budgets (draw calls, GC alloc/frame, memory), **G14** a11y beyond font scale (colorblind palette,
  screen-reader labels, haptics toggle), plus the **real sound design + final art pass** replacing the
  Step 9b placeholders.
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
| **Audio system** (SFX + music + mute) - review gap G1 | nothing competes for the slot yet; needs a service + event hooks |
| **Local notifications** (offline cap full) - G2 | needs a platform plugin + permission flow |
| **Sim-tick error containment** (safe mode + autosave on failure) - G5 | fold into Step 9 |
| **Save backup rotation** (3 versions) - G6 | fold into Step 9 (`SaveCoordinator`) |
| **CI + scripted build + version policy** - G8/G9 | after the feature set stabilises, before Step 20 |
| **Store/legal checklist** (privacy, ratings, ad disclosure, ATT) - G15 | once Step 19 exists |
| **Settings screen** (audio/notifications/a11y/battery/language) - G4 | must be explicit in Step 20 |
| **Device perf budgets + real art pipeline** - G11/G12 | Step 20 |
| **Daily reset boundary decision** (UTC vs local) - G13 | Step 16 |

## 8. Definition of done (every step)

- [ ] Scope matches the owner doc; no unrelated refactor smuggled in
- [ ] Compiles with zero errors through the bridge, recompiled outside Play mode
- [ ] Acceptance test executed and its numbers pasted into this file (or the step log)
- [ ] `Roadmap.md` status + owner doc updated; `Architecture.md` decision log edited if a decision changed
- [ ] No new fixed-size serialized arrays, no runtime `Find*`, no `UnityEngine` inside `Sim/`
- [ ] Save change (if any): migration written + tested against a real v2 save file
- [ ] Manual Unity test handed off; wait for confirmation before the next step


