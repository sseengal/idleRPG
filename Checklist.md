# Checklist — master progress record

> The single place that answers "what is left to finish the game, and where are we right now?"
> Roadmap detail: `Roadmap.md`. Design: `Architecture.md` + topic docs. MVP journal: `Plan.md`.
> Rule: **one sub-step at a time** - implement, recompile, verify, tick the box, hand off for a manual test.
> Legend: `[x]` done · `[~]` in progress · `[ ]` todo · `[!]` blocked · `[-]` dropped/deferred.

---

## 0. Status

| Field | Value |
|---|---|
| Current step | **10b** save v3 + `partySlots` persistence (10a done, 10c UI) |
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

### 8a — Spec files + generator (round-trip proven)  `[x]`
- [x] `Editor/Content/ContentSpecs.cs` (JSON shapes: heroes, enemies, party, waves, stat + prestige upgrades)
- [x] `Editor/Content/ContentSpecIO.cs` (paths, pretty JSON, hex colours, create-or-load, folder helper)
- [x] `Editor/Content/ContentGenerator.cs` (Export Specs From Assets / Generate Assets From Specs)
- [x] shared `Editor/Editable.cs` + `Editor/SoField.cs` (write/read private [SerializeField] fields)
- [x] **never write unresolved references**: a party/pool with an unknown id now aborts the write with an
      error instead of silently emptying the list
- [x] verified: export -> generate -> export is **byte-identical**; Balance Lab golden numbers unchanged
      (stage 1: 74s @x1.0, 120s @x1.6, 272 gold, 2.26 gold/s)
- **Two real bugs caught by the round-trip test:**
  1. `ParseEffectType` only knew "damage"/"health" while export wrote "damagepercent"/"healthpercent" ->
     generating silently turned the Damage and Health prestige upgrades into Gold (fixed: both spellings
     accepted; specs restored and re-applied).
  2. normalising enemy ids (`Enemy_Slime` -> `enemy_slime`) left `waves.json` referencing the old ids, and the
     generator wrote the resulting **empty** pools, wiping `WaveConfig` (fixed: refuse to write unresolved
     refs; pools restored).
- [x] conventions locked: spec `id` is lowercase/stable (save-key style), `asset` carries the file name

### 8b — Validator + dev overlay + telemetry  `[x]`
- [x] `ContentValidator` (menu `Tools > Idle RPG > Content > Validate Content`): unique ids + asset names,
      hero/enemy numeric sanity, party lanes, wave pools, upgrade tracks, spec<->asset agreement (orphans,
      hand edits), stage-1 balance band (90-180s) reusing the Balance Lab runner, CSV to `Temp/content-report.csv`
- [x] **negative test passed**: 4 injected faults (unknown enemy id, empty boss pool, baseHealth -5,
      attackIntervalSec 0.01) produced exactly 4 errors; clean run after restoring
- [x] `Debug/TelemetryFeed.cs` - in-memory ring buffer (200) of notable events; no network, no per-frame cost
- [x] `Debug/DevOverlay.cs` - F3 dashboard built at runtime (own canvas): enemy, stage/wave/boss, party DPS,
      eHP + alive, enemy HP + wave ETA, gold/s + gold/min, pace + floor, rng state (hex), save info, fps
- [x] wired in `GameManager.BuildContext()`: telemetry always, overlay inside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`
- [x] verified live: `RESULT: clean`; overlay rendered `stage 88 wave 1/11`, `crit x1.05`, `pace x1.6`,
      `rng state 0x...`, `save file x84`
- **Debt:** the overlay computes DPS/eHP itself until `SimLedger` exists (Step 9); it must then read the ledger

### 8c — One-click checks + spec coverage for future types  `[ ]`
- [ ] `Tools > Idle RPG > Content > Run All Checks`: validate -> round-trip -> golden numbers in one command
- [ ] extend specs to abilities/encounters/zones/tracks/loot **as** Steps 11/13/15 introduce them

### 9a — SimLedger + reward funnel (one rate source)  `[x]`
- [x] `Sim/SimLedger.cs` - rolling-window rates (gold/s, kills/s, seconds/stage, best stage time, session
      totals), pure C#, advanced by `Tick(dt)` so live and offline measure identically
- [x] `Economy/RewardService.cs` - the single till with **two buttons**: `GrantGold` (compute multipliers then
      pay) and `GrantQuotedGold` (pay exactly what the UI quoted), plus `GrantGems`/`GrantTokens`, all writing
      ledger receipts; combat income feeds the rate, every other source is external
- [x] retired `Economy/EconomyRateTracker.cs` (deleted) - the ledger is now the only rate source; offline
      calculator, save field and F3 overlay all read it
- [x] payouts routed through the till: kills, boss gems, ascension tokens, offline claim
- [x] folded-in **G5**: `RunController.StepSafely` - tick exceptions are caught, logged, autosaved and skipped;
      intermittent faults let the run continue, 10 in a row stop it (safe mode)
- [ ] folded-in **G6** (3-version backup rotation) moved to **9b** - not done yet, do not report as complete
- **Verification log:**
```
Balance Lab unchanged: pace x1.0 74s / 272 gold | pace x1.6 120s / 272 gold / 2.26 gold/s
live: SimLedger(gold/s 4.48, kills/s 0.18, session 76 gold / 3 kills) | wallet +76 | RewardService(granted 76 gold over 3 payouts)
offline claim: offered=41996 paid=41996 (exactly quoted) | rate 5.208 -> 5.208 unchanged (external excluded)
              sessionGold=42049 = 53 combat + 41996 claim (funnel accounting exact)
G5: intermittent - "Combat tick threw; skipping it and saving" (failures=13, enabled=true, run continued)
    persistent  - "12 consecutive failures: safe mode, run stopped" (safeMode=true, enabled=false)
```
- **Bug found and fixed by this step (worth remembering):** the funnel initially applied the gold multipliers to
  an amount the popup had *already* resolved, paying 57,749 for a quoted 36,093 (x1.6 double-apply). Fixed by
  splitting `GrantGold` (compute+pay) from `GrantQuotedGold` (pay the receipt verbatim) so the class of bug is
  impossible rather than just corrected.

### 9b-1 — Currencies + gem sink + backup rotation  `[x]`
- [x] `Data/CurrencyDef.cs` (id, display, icon, isPremium, **isImplemented**, earnSource, sinkDescription)
- [x] 7 currency assets generated: gold/gems/tokens live; shards/materials/essence/scrolls are labelled
      placeholders so later steps flip a flag instead of inventing a concept
- [x] **gem sink**: `Economy/ShopService.cs` trades gems for extra offline income cap (`+1h`, 50 gems,
      max `+3h`); BalanceConfig knobs `offlineCapExtensionSeconds/GemCost/MaxSeconds`
- [x] offline calculator honours the purchase (`BonusEquivalentCapSeconds`), saved as
      `offlineEquivalentCapBonusSeconds` + `offlineCapExtensionsPurchased` (schema v2 additive, no migration)
- [x] shop UI row built **in code** inside the existing shop panel (placeholder styling; Step 19 redesigns it,
      and no scene rebuild was needed)
- [x] **G6**: `SaveSystem` now rotates 3 backups (`.bak`, `.bak1`, `.bak2`) and the recovery path walks them
- [x] verified live: offer `'+60 min offline income - 50 gems'` -> bought -> cap 0 -> 3600s, gems 201 -> 151;
      **9h away paid 10800s (2h base + 1h bought)** instead of 7200s; 3 purchases max out (`offline cap is
      maxed`, 4th refused); save contains `capBonusSeconds: 10800.0, purchases: 3`; files on disk:
      `savegame.json`, `.bak`, `.bak1`, `.bak2`
- **Design note:** the shop raises the *equivalent* cap, not the 8h wall clock - the equivalent cap is what
      actually limits a payout, so raising the wall clock would have been a purchase with no effect

### 9b-2 — `IdleTimeService` + instant-income sink  `[x]`
- [x] `OfflineProgressManager` -> **`IdleTimeService`** (git mv, GUIDs kept): one owner of every time-based
      payout. Offline behaviour byte-identical; expeditions/bounties (Step 17) now extend one class
- [x] shared privates for the parts both payouts need: `ResolveRate` (measured -> saved -> estimated),
      `TimeGold` (seconds x rate x efficiency + gold multiplier) and `PayExternal` (funnel + `RewardPaid`)
- [x] `FormulaUtility.OfflineGold` -> **`TimeBasedGold`** (one formula, two callers - the two can never
      disagree about the discount)
- [x] **second gem sink**: instant income / fast-forward (`instantIncomeSeconds` 3600, `instantIncomeGemCost`
      30, repeatable). Quote -> charge -> pay order, so gems are never spent on a zero payout
- [x] `RewardService.Source.InstantIncome` receipt source; payout booked **external** like every time payout
- [x] shop UI: `CreateOfferRow` builder extracted, rows stack from the bottom; second row wired
- [x] verified live: `'Fast-forward 60 min - 30 gems'` bought for 30 gems -> `+32,326 gold` (3600 x 8.017 x 0.7
      x 1.6 mult) and **measured rate unchanged** (5.9258 -> 5.9258) across a 23,893 gold purchase;
      broken-player purchase refused (no gems spent, no payout); both rows present and labelled
      (`OfflineCapRow='Offline cap is maxed' interactable=False`, `InstantIncomeRow` enabled);
      offline regression 3h away -> 10800s / 71,678 gold through the renamed service
- [x] regression: content validator clean, golden numbers unchanged (74s@x1.0 / 120s@x1.6, 272 gold)

### 9b-2b — Dev hotkey catalogue + in-game legend  `[x]`
- [x] `Debug/DebugHotkeyCatalog.cs`: the one list of dev keys (key + description). The F3 overlay renders it and
      `Tools/Idle RPG/Debug/Log Hotkeys` prints it, so code/overlay/README cannot drift
- [x] `DebugHotkeys` doc comment now points at the catalogue instead of duplicating the list
- [x] new keys: `C` = grant gems, `F` = buy instant income (calls the same `GameManager.BuyInstantIncome` as the
      shop button, so the key tests the real purchase path)
- [x] `DevOverlay` grew a second panel for the legend, both panels **auto-fit** their text and re-stack, so new
      stats lines or new keys cannot overlap
- [x] verified live: `F3` -> stats panel h=206 at y=-8, hotkey panel h=221 at y=-220, all 12 rows listed;
      `C` gems 270 -> 370; `F` logged `Instant income bought (F): +4.7K gold for 30 gems` (30 gems spent,
      `InstantIncomePurchases` 0 -> 1)

### 9b-3 — Placeholder presentation hooks (audio + icons)  `[x]`
- [x] `Services/IAudioService.cs` (volume, mute, `PlayedCount`, `Play`, `ToggleMuted`, `Describe`) +
      `SfxCue` enum: hit, crit, kill, boss, level-up, claim, purchase, ascend, defeat
- [x] `Services/PlaceholderAudioService.cs`: **no audio files** - each cue is a short procedural tone
      (pitch sweep + brightness harmonic + decay envelope) generated once and cached; volume/mute in PlayerPrefs
- [x] `Services/AudioDirector.cs`: the only class that maps events -> cues, with throttles (hit 70ms, kill 50ms)
      so a burst of combat cannot turn into one buzz; crits always play
- [x] `PlaceholderSpriteGenerator` grew shard/ingot/flask/scroll shapes -> `ui_icon_shard/material/essence/scroll`
- [x] all 7 `CurrencyDef` assets now carry `icon` (gold/gem/token/shard/material/essence/scroll) so Step 19 swaps
      art without touching code
- [x] verified live: service created + `AudioDirector` wired; every cue path fired through the **real event bus**
      (EnemyDamaged hit + crit, EnemyKilled, boss spawn, HeroLevelChanged, UpgradePurchased, OfflineRewardsClaimed,
      AscensionCompleted, PartyWiped, BossFailed -> 10 cues, and a normal enemy spawn correctly made **no** sound);
      mute: `Describe()='muted'`, `EffectiveVolume=0`, pref written; icons verified by GUID match against each
      `CurrencyDef.icon`; validator clean; golden numbers unchanged
- **Note:** key presses need the Editor focused (frames do not tick while it is backgrounded), so cue wiring was
  proved through the event bus; the manual pass confirms it audibly

### 9b-4 — Full reset (`F8`)  `[x]`
- [x] `GameManager.ResetGame()`: wipes save + every backup + PlayerPrefs, then reloads the active scene, so a
      fresh run starts immediately (F9 only deletes the file and the old progress keeps running)
- [x] hotkey `F8` + `Tools/Idle RPG/Save/Reset Game Completely` (calls the same method in Play mode, wipes files
      directly in edit mode)
- [x] verified live: before reset stage 6 / 321,752 gold / cap +1h bought / audio muted at 30%; after reset
      stage 1, highest 1, gold-gems-tokens 0, `LoadedFromSave=false`, shop purchases 0, audio back to 50%
      unmuted with `volPref` absent (prefs really wiped); only a fresh 1,390-byte save + one `.bak` on disk;
      `F8` itself fired through `DebugHotkeys`

### 9b (original scope)  `[-]`
- [ ] `CurrencyDef` rows (gold, gems, tokens, shards, materials, essence, scrolls)
- [ ] **placeholder presentation hooks** (audio service + icons) - see `Roadmap.md` Step 9b
- [ ] `EconomyService` single reward funnel + ledger writes
- [ ] gem **sinks** live (fast-forward, offline-cap extension)
- [ ] `IdleTimeService` absorbs `OfflineProgressManager` (caps, tamper, pending claim)
- [ ] acceptance: gems earn + spend; offline numbers unchanged; `F10` still works

## 3. Combat depth

### 10a — Formation model + row targeting (sim)  `[x]`
- [ ] `Data/FormationData.cs`: rows/columns, slot unlock stages, row rules (`backRowDamageTakenMultiplier`
      0.75, `frontRowProtectsBackRow`), slot -> row/column mapping; all values data, none hardcoded in the sim
- [ ] `Combat/Formation.cs`: runtime slot assignment (slot -> hero), `TrySwap`, `AutoArrange`, `ToSave`/`FromSave`
      hooks, combat-order helpers; pure C#, no UI
- [ ] `Encounter`: row-aware `SelectTarget` (`FrontMost` = lowest row first, then column) + new `BacklineFirst`
      rule + back-row damage multiplier applied while the front row has a living member
- [ ] `HeroData` grows `role` (Tank/Damage/Support) + `targetRule` (per-hero override lives here, wired in Step 11)
- [ ] `CombatSimulator.SetupParty(party, formation)` stamps each hero's row/column; `CombatManager` and
      `GameManager` carry the formation asset
- [x] acceptance **verified live**: board `front[0 1 2] back[- - -]` on start (= the MVP's fixed lanes); moved the
      tank to the back-left slot -> `front[- 1 2] back[0 - -]`, rows re-stamped `Knight[Back/col0]`; with
      `FrontMost` the enemy's first hit landed on **index 1 (Archer)**, i.e. the new front-most; with
      `BacklineFirst` it reached the back-row Knight and the sampled minimum hit went 0.9 -> **0.675 = exactly
      x0.75**; with the front row empty the same hit stayed 0.9 (back row exposed) as Sim-Core rule 2 requires
- [x] golden parity: Balance Lab unchanged (74s@x1.0, 120s@x1.6, 272 gold, 2.26 gold/s) and the validator is
      clean, now with a formation line (`board 2x3, 5 slots open at stage 1, team 3 -> 5, back-row x0.75`)
- **Design correction found by the probe:** board slots and *team size* are two different rules. The first model
      gated the back row behind stage 21, so no player could reach the back row on day one - wrong. Now
      `slotUnlockStages` (5 of 6 slots open from stage 1) and `teamSizeUnlockStages` (3 -> 4 @ stage 21 -> 5 @
      stage 41, per `Progression.md`) are separate, so the acceptance test works at stage 1

### 10b — Save v3 + `partySlots` persistence  `[ ]`
- [ ] save **v3** + `SaveMigrations.v2ToV3` (`partySlots`, keyed `statLevels`, `currency[]`, `tracks[]`)
- [ ] formation survives reload; round-trip drift check (export -> generate -> export byte-identical)

### 10c — Team screen + battle formation strip  `[ ]`
- [ ] Team screen: roster grid, formation board, tap-swap, auto-arrange, presets
- [ ] battle formation strip; delete `DesiredPartySize` + the fixed `heroViews[]`
- [ ] validator: formation asset sane (slots >= party, unlock stages ascending, multiplier in (0,1])

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

### 20 — Visual/audio/UX polish + l10n + a11y + perf  `[ ]` (owns G4, G11, G12, G14)
- [ ] string table + settings (font scale, reduced motion, battery mode)
- [ ] sprite atlases, scroll virtualisation, pooling audit
- [ ] device builds: iOS + Android full-loop verification
- [ ] `Mobile Verify Settings` all green; 60fps idle / 30fps battery mode
- [ ] real sound design + final art replacing the Step 9b placeholders

### 21 — CI, versioning & release readiness  `[ ]` (owns G7, G8, G9, G10, G15)
- [ ] scripted `Unity -batchmode` build + tag-driven version; version/save-compat policy written
- [ ] crash/ANR reporting hook; analytics opt-in + privacy decision; store/legal checklist

### 22 — Local notifications  `[ ]` (G2)
- [ ] offline-cap-full, expedition-finished, daily-reset notifications + permission flow + in-app toggle

## 5. Gaps found in the review - now SCHEDULED

Found while reviewing progress against the design docs. Each needs a home in `Roadmap.md`.

| # | Gap | Why it matters | Proposed home |
|---|---|---|---|
| G1 | **No audio at all** - no SFX/music service, no mute setting (`IAudioService` + placeholder + mute: 9b-3; authored SFX/music still Step 20) | Idle games live on feedback (hit, crit, level-up, claim); silence feels broken | new step after 19 (or fold into 20) |
| G2 | **No local notifications** ("your offline cap is full") | Biggest single re-engagement lever for an idle game; needs a platform plugin + permission flow | new step after 19 |
| G3 | **No first-run onboarding** - new players get no goals | The first 2 minutes decide retention; wall guidance (15) covers later sessions only | fold into 15 as "first-session goals" |
| G4 | **No settings screen** (audio, notifications, font scale, reduced motion, battery, language) | Required before any store build; currently settings do not exist at all | part of 20, but must be explicit |
| G5 | **Sim tick has no error containment** - one exception freezes an unattended idle game | Add try/catch in `RunController` + safe-mode skip + autosave on first failure | small; fold into 9 |
| G6 | **Save keeps only one `.bak`** | Idle saves are played for months; rotate 3 backups + tag them with the schema version | fold into 9 (`SaveCoordinator`) |
| G7 | **No crash/ANR reporting** | You cannot fix what you cannot see on device | fold into 19/20 |
| G8 | **No CI or scripted build** | "Production-ready" needs `Unity -batchmode` build + tag-driven versioning + store upload stubs | new step before 20 |
| G9 | **No version/release policy** (`bundleVersion` 0.1.0, no changelog, no save-compat promise) | Save compatibility is a promise to players; needs a written rule | part of G8 |
| G10 | **Analytics decision unrecorded** (store privacy policy + opt-in) | Required for both stores if any telemetry leaves the device | part of 19 |
| G11 | **Real art pipeline undefined** (atlases, import presets, Addressables later) | Placeholders are procedural; shipping art needs a policy | part of 20 |
| G12 | **Device performance budgets not set** (draw calls, GC alloc/frame, memory) | Step 20 says "verify" but nothing to verify against | part of 20 |
| G13 | **Daily reset boundary undefined** (UTC vs local midnight, DST) | Daily/weekly systems land in 16-17 | decide in 16 |
| G14 | **Accessibility beyond font scale** (colorblind palette, TMP labels for screen readers, haptics toggle) | Cheap now, expensive later | part of 20 |
| G15 | **Store/legal checklist** (privacy URL, age rating, data-safety form, ad disclosure, iOS ATT) | Blocks submission once ads/IAP exist | part of 19 |
| G16 | **No single "run all checks" command** | Our regression net is manual today; one command makes it habitual | Step 8c |

---

## 6. Cross-cutting (apply in the step that touches it)

- [ ] every payout path writes `SimLedger` (from Step 9 on)
- [ ] every new screen: data-driven rows + prefab template (no fixed arrays)
- [ ] every new number: SO-driven (no literals inside `Sim/`)
- [ ] `Sim/` stays free of `UnityEngine` (check on every compile)
- [ ] save changes: migration + a real previous-version file test before shipping the step
- [ ] `Roadmap.md` status board + this checklist updated in the same iteration
- [ ] manual Unity test handed off at the end of every sub-step (`.clinerules`)

