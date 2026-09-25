# Checklist — master progress record

> The single place that answers "what is left to finish the game, and where are we right now?"
> Roadmap detail: `Roadmap.md`. Design: `Architecture.md` + topic docs. Folder index: `README.md`.
> MVP journal: deleted 2026-09-22 (obsolete - see `REVIEW.md` R1; `git log` still has it).
> Rule: **one sub-step at a time** - implement, recompile, verify, tick the box, hand off for a manual test.
> Legend: `[x]` done · `[~]` in progress · `[ ]` todo · `[!]` blocked · `[-]` dropped/deferred.

---

## 0. Status

| Field | Value |
|---|---|
| Current step | **B6 automation IN PROGRESS** (Steps 1-3 done 2026-09-25: cards, engine, AUTO panel, speed button + the rate guard; all verified live). Next: Step 4 robot learns the cards (or straight to B7), then B8 -> B9, device pass last |
| Dropped | ranged enemy archetype / "11f" - deleted 2026-09-21 (content depth, no loop or money path). See `Roadmap.md` §2 |
| v1.0 gate | **the loop + money.** No statuses/abilities/zones/affixes/gear/roster/relics before the base is done |
| Parked (v1.1) | Steps 12, 13, 15b, 17, 18, 22 + the rest of 19/21 - plan kept in `Roadmap.md` §3 |
| Last completed | B3d compounding upgrades - data + pipeline + robot-player proof, 2026-09-25 |
| Next after this | B4 sweep-wide validator band (stages 1-20, no-stall + dangling-reference checks); then the loop walk (ascend, offline, shop, first-run) with B8 |
| Save schema | v4 (v3 -> v4 adds `runBestStage`, the per-run ascension gate; additive migration, no data loss) |
| Shipped build | `Builds/IdleRPG-mac.app` verified (boot, save, offline, combat) |
| Docs | all live in `Docs/` - index `Docs/README.md`; status here; backlog `Roadmap.md`; removals `Docs/REVIEW.md` (R1: obsolete MVP journal deleted) |

**Standing regression surfaces** (run these on every combat-affecting change): golden numbers, content validator,
save drift, **battle log**. The log is a *contract with the player*, not decoration: any feature that produces or
renames combat text (statuses, abilities, **enemy item drops**, **item usage in battle**, affixes, pets) must be
re-checked for correct attribution, one line per wave, no mid-line renaming and no "... N more hits" flooding.

## 1d. Compounding upgrades (B3d)  ·  **built + measured 2026-09-25**

The last arithmetic blocker on the loop. Hero upgrades were additive (`base x (1 + 0.1 x level)`) while content is
exponential (`1.15^stage`), so affordable power was a straight line: stage time 126s -> 493s, walls 8 -> 40 min,
hard stall ~stage 24. Fixed in **data**, not with a difficulty-curve table.

- `FormulaUtility.StatEffectMode` (`AdditiveBase` | `Multiplicative`) + `HeroStatValue(..., mode)`; level 0 returns the
  base stat in **both** modes, which is why the unupgraded parity net still matches byte for byte.
- `StatUpgradeData.effectMode` is the data switch; `StatResolver` reads it per stat. The gain is **derived**:
  `CompoundingGainFor(content = enemy HP 1.15, cost 1.07, gold 1.12)` = **0.087** for ATK/HP, and 0.047 for DEF
  (damage is `ATK - DEF`, so defence races enemy **attack** 1.08, not HP). Data rounds up with a ~0.5%/stage margin:
  **ATK/HP x1.09, DEF x1.05**.
- The mode travels the whole pipeline: `UpgradeSpecFile.effectMode` ("additive" | "multiplicative"), exported by
  `Export Specs From Assets`, applied by `Generate Assets From Specs`, defaulted by `DataAssetGenerator`, and checked by
  the validator - so a regenerate can never silently drop a track back to additive.
- `HeroUpgradeRowUI` labels per mode: `x1.09 / lvl` vs `+9% base / lvl`.
- **Measured (robot player, pace x1.6, 3h budget):** stage 30 in **70.1 min** (cheapest-buy, 428 buys, 3 walls, worst
  wall **8.8 min**) and **34.4 min** (attack-only, 222 buys, 5 walls, worst wall 7.3 min). Stage 30 is the tool ceiling
  (`ClimbSimulation.MaxStage`) - raising it is B4's job.
- **Regressions:** `Run All Checks (regression)` = **PASS**; validator `clean (0 warning(s))` with
  `3 stat tracks checked (3 compounding)`; golden numbers unchanged (80s @x1.0 / 126s @x1.6, 22 kills, 272 gold,
  3.41 / 2.16 gold/s).
- **Spec files resynced by the export:** `upgrades.json` picked up the shipped B3b prestige tune (+20%, cap 25) and the
  new mode field; `enemies.json` dropped the stale `preferredRow` key left over from the deleted enemy ranks (the field
  exists in neither code nor assets).

## 1a. Loop beat: the fallback bounce  ·  **built + verified 2026-09-22**

The party never stops and never waits for input. Two rules, no new state:

```
clear a stage (boss dies):  next = stage + 1 | new best -> raise HighestStageReached,
                            pay gems ONLY if stage % milestoneStageInterval == 0
wipe:                       stage = max(1, stage - 1) | beat defeatPauseSeconds | fight again
```

- Player on 25: wipe -> 24 -> clear -> 25 -> wipe -> 24 ... forever. Income never stops.
- Wipes can cascade (4 -> 3) when even the fallback fails. Self-healing, never starves.
- Gems are **new-best + milestone only** (`CalculateMilestoneGems`), so bouncing can never farm
  gems and an ascend-then-reclimb pays none either.
- `defeatPauseSeconds` (0.75) is the only new knob. `resetAutoRetryOnDefeat` is gone.

Verified in Play Mode on the exact save that used to freeze (stage 4, wave 5, zero upgrades):
stage 4 -> 5 (clear), wipe on 5 -> fallback 4, cascade 4 -> 3 -> clear -> 4, gold rising throughout,
gems 3 unchanged across the bounce, 131 upgrades bought -> ceiling moved to 6, milestone at stage 5 paid
+5 gems (3 -> 8), stage 10 paid +5 again (8 -> 13), ascend reset to stage 1 with best kept at 11 and
**no** re-climb gems, post-ascend climb restarted from 1 and bounced at 4.

`[!]` **Open blocker found while playing (next step):** `IdleTimeService.ResolveRate` accepts any
`savedGoldPerSecond > 0`, so a near-zero ledger residue (seen live: `1.58e-15`, floating-point
cancellation in `SimLedger.Trim`) wins over the "estimated" fallback and the offline popup pays **0**.
Fix: clamp tiny `windowTotal` residues in `SimLedger.Trim`, and require a meaningful rate in
`ResolveRate` (else fall through to the estimate).

## 1b. Ascension retune (B3b)  ·  **built 2026-09-22**

Found in play: the ascend button gated on `HighestStageReached`, which **ascending never lowers** - so after the
first ascension it stayed live forever and every press paid the full yield again. Tokens were farmable by
spam-clicking, and the payoff was far too small to matter (+20% at the first wall, when the wall needs ~x3.4).

| Change | Before | After |
|---|---|---|
| Gate | lifetime best >= 10 | **run** best >= 10 (`runBestStage`, saved, resets on ascend) |
| Yield | `floor((lifetimeBest/10)^1.5)` | `floor((runBest/10)^1.5)` |
| Effect | +10% per level | **+20%** per level (`effectPerLevel` 0.2) |
| Cap | 10 levels (x2.0, tree died ~stage 170) | **25 levels** (x6.0 per track) |
| Panel | "Best stage N - ready" | "This run: stage N - ready" |

Save schema **v3 -> v4** (`runBestStage`, additive; an older save converts to "run best = where you are now").
Still to verify in play: the re-climb after an ascension must be visibly faster than the first climb.

## 1c. Offline rate guard (B3c)  ·  **built 2026-09-22**

The offline popup could show **+0 gold**. `SimLedger.Trim()` subtracts samples one at a time, so the rolling window
could be left holding a floating-point crumb instead of exactly 0; that crumb was saved as `lastGoldPerSecond`
(seen live: `1.58e-15`). On the next launch there are no live samples yet, so `IdleTimeService.ResolveRate` picked the
"saved" branch - and because the test was `> 0`, the crumb won over the formula estimate and the payout was zero.

Two guards: crumbs under `1e-6` gold are snapped to zero in `Trim()`, and any rate under `0.001` gold/s is treated as
"no measurement" so the estimate is used instead. Verified the healthy path in play (8h away at a measured 11.6/s paid
58,693 gold at the 2h equivalent cap).

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

### 10b — Save v3 + `partySlots` persistence  `[x]`
- [x] `SaveData.CurrentVersion = 3` + `List<int> partySlots` (slot -> party index, -1 = empty). An **empty list**
      means "no layout saved", which the game reads as the default front row - so a v2 file plays exactly as before
- [x] `SaveMigrations.v2ToV3`: keeps every value, adds an empty board (it must not invent a layout), logs the
      upgrade; v1 still lands on the current schema
- [x] `GameManager`: the board is created **before** the load so `ApplySnapshot` can restore it, `CaptureSnapshot`
      writes it, and `Formation.Changed` marks the save dirty **and** re-stamps the live fight (a swap takes
      effect on the next swing without any caller remembering to call `ApplyFormation`)
- [x] `Editor/SaveRoundTripMenu.cs`: drift test (serialise -> parse -> serialise, byte-identical) for a default
      and a fully-populated payload, v1/v2 migration checks, and a real-file check (upgraded file stable +
      **additive-only**: every line that existed on disk is still present)
- [x] **verified live**: moved the tank to the back-left slot -> save file shows `partySlots: [-1,1,2,0,-1,-1]`,
      `schemaVersion 3`; **relaunched Play** -> `loadedFromSave=True`, `board=front[- 1 2] back[0 - -]`,
      `rows=Knight[Back/col0]`; dirty flag `False -> True` on a swap; swap auto-changed the Knight's row with no
      manual call
- [x] drift on the real save: `file savegame.json: v3 -> v3, stable, additive-only (1513 -> 1513 chars, 6 slot(s), 0 line(s) lost)`;
      the *pre-10b* file was v2 and upgraded `v2 -> v3 ... 0 line(s) lost`
- [x] regression: content validator clean, golden numbers unchanged (74s@x1.0 / 120s@x1.6, 272 gold)
- **Note:** the keyed `statLevels` / `currency[]` / `tracks[]` shape listed in the plan is deliberately **not** part
      of v3 - nothing needs it yet, and reshaping live data would risk the very saves the step is protecting. It
      lands with the systems that need it (relics/abilities), as an additive migration of its own

### 10c — Team screen + battle formation strip  `[~] superseded by 10d`
> Kept for the record. The locked slots, the two preset buttons, the tap-swap on the battle screen and the
> `FormationStripUI` option flags were all removed in 10d after review - see below.
- [x] `UI/FormationStripUI.cs`: one view **per formation slot**, built in code from `FormationData` (rows and
      columns straight from the asset), tap-hero-then-tap-slot swapping, `LOCKED` slots refused with a toast,
      selected slot highlighted; works as the battle strip (health bars) or the Team board (role tags)
- [x] `UI/TeamPanelUI.cs` + a new **TEAM** tab (nav is now BATTLE / TEAM / UPGRADES / ASCEND / SHOP): roster with
      role + slot + hp/atk, the board, `TANK FRONT` (auto-arrange) and `HIDE THE TANK (BACK ROW)` presets
- [x] `UI/UiRuntime.cs`: runtime twin of the editor `UiFactory` (panels/texts/buttons/health bars in code)
- [x] deleted `PartyConfig.DesiredPartySize` and the fixed `heroViews[]`/`HeroLane0..2` lanes; `TabController`
      exposes `TabCount`, `ScreenController` derives the tab range from it (no magic 3s left)
- [x] damage numbers now follow the **hero**, not the lane: the strip hands the pool fresh anchors on every change
      (and the hidden Team board deliberately feeds no pool, so it cannot hijack them)
- [x] validator: formation checks added (slots >= party, unlocked >= party, team size >= party, multiplier in (0,1])
- [x] **verified live**: strip built 6 slots - `Slot0='front' Slot1='Archer' Slot2='Mage' Slot3='Knight'
      Slot4='back' Slot5='LOCKED'(not interactable)`; real button taps moved the Knight to the back row
      (`front[- 1 2] back[0 - -]`, knightRow Back, save marked dirty); `NavTEAM` opened the tab; `HIDE THE TANK`
      -> `front[2 1 -] back[0 - -]`, `TANK FRONT` -> `front[0 2 1] back[- - -]`; anchors read
      `Slot0<FormationStrip> Slot2<FormationStrip> Slot1<FormationStrip>` (per hero, battle board only)
- [x] regression: validator clean, golden numbers unchanged, save drift PASS (v3, 6 slots, additive-only)
- **Bugs found and fixed by the probes:** the hidden Team board was overriding the damage-number anchors (it now
      passes `null`); `HeroUnitView.Apply()` and the strip were writing the *same* label, so role tags never
      showed (slots now have a view-owned `Name` and a strip-owned `Caption`)

### 10d — Formation rework (review fixes)  `[x]`
- [x] **Rows now change *who gets picked*, never how hard they are hit.** `backRowDamageTakenMultiplier` and
      `frontRowProtectsBackRow` are gone; `SimRules.BackRowTargetWeight` (0.35) replaces them: per swing the
      attacker draws a rank with `weight / (weight + 1)` odds for the back rank, then takes the front-most hero of
      that rank. No damage maths touches position any more
- [x] **Stall bug fixed**: `FrontMost` used to return `null` when the front rank was empty, and `ResolvePhase`
      `return`ed - so a party hiding behind a dead front line was never attacked again (the fight only ended via
      the boss timer). Selection now falls back to the other rank, so nobody is untargetable
- [x] **Duplicated-hero bug fixed**: empty slots were only disabled, never blanked, so a moved hero kept rendering
      in his old slot. `HeroUnitView.ClearVisual()` now blanks icon/name/HP and both boards call it
- [x] **Battle screen is display-only**: `FormationBoardView` builds slots with **no Button components at all**, so
      a stray tap during a fight cannot reshuffle the team
- [x] **Editing lives only on the Party tab** (nav label `TEAM` -> `PARTY`, `TeamPanelUI` -> `PartyPanelUI`)
- [x] **Board is vertical**: front and back rank are two *columns* (front nearest the enemy on the right), positions
       stack downwards. One shared `FormationBoardLayout` draws both boards so they cannot drift
- [x] **No locked slots, no presets, no auto-arrange**: every slot is usable from the first minute;
      `AutoArrange`/`ArrangeTankInBack`/`MaxTeamSize`/both unlock arrays deleted. Team size 3 -> 4 -> 5 is parked
      as a **Step 17 roster rule** (recorded there), not a board rule
- [x] **Party tab planned for what is coming**: board + roster rows (name, role, rank+position, hp/atk) + a hero
      card (stats today, 4 reserved gear slots for Step 18). All rows are built from data, so later systems append
      rows instead of redesigning the screen
- [x] **`SimRules` owns the row rule** (stamped from the formation asset by the simulator), so a fight can never run
      with stale weighting because a caller forgot a push
- [x] verified live: board `front[- 0 1] back[2 - -]`; back-rank share of enemy swings **28.0%** over 25 swings
      vs **25.9%** expected (weight 0.35); back-rank hit damage `1.0 == formula x1.0` -> **no reduction**; front rank
      killed -> **9 swings still landed**, all on the lone back-rank hero (old code: 0); battle board `0` buttons and
      mirrors the Party board exactly; party tap-swap moved the Knight and the vacated slot read `empty` with the
      hero appearing exactly once; save `partySlots: [-1,0,1,2,-1,-1]` and the validator line reads
      `board front 3 / back 3 (6 slots, all usable), back-rank target weight 0.35`
- [x] regression: validator clean, golden numbers unchanged (74s@x1.0 / 120s@x1.6, 272 gold), save drift PASS
      (**baseline superseded by 10e**: 79s / 124s - the targeting rule changed in the next step)
- **Notes:** the default layout (front rank first) keeps the unupgraded MVP party entirely in the front rank, which
      is why golden parity survives untouched; `HeroData.targetRule` is still inert (wired in Step 11)

### 10e — Even spread inside a rank  `[x]`
- [x] `Encounter.SelectByRow` (front-most soaks everything) -> `SelectRandomOfRow`: hits inside the chosen rank are
      spread **evenly at random** using the sim RNG. A rank with one living member draws nothing, so a one-hero
      board is unchanged. Rank odds are untouched (front rank 1.0, back rank `BackRowTargetWeight` 0.35)
- [x] **verified** (simple probe, 75 swings each): all three in the front rank -> Knight **37%** / Archer **32%** /
      Mage **31%** (was 100/0/0); mixed board `front[0 1 -] back[2 - -]` -> front **73%** / back **27%**
      (expected ~74/26)
- [x] **golden numbers re-baselined** (the targeting rule consumes RNG draws, so crit luck moves - expected, and the
      reason this landed *before* Step 11 instead of after)::
```
per-enemy TTK   unchanged: Slime 4.0s, Bat 6.0s, Goblin 11.3s (pace x1.6)
pace x1.0       stage 1 = 79s  (was 74s) | 272 gold | 3.45 gold/s (was 3.69)
pace x1.6       stage 1 = 124s (was 120s) | 272 gold | 2.20 gold/s (was 2.26) | boss 44.0s (was 45.6s)
stage sweep     1:123.6s 272g no | 2:171.6s 309g no | 3:200.1s wipe | 4..10 wipe (new baseline)
validator       ok [balance] Stage 1: 124s, 11 kills, 272 gold, 2.20 gold/s (band 90-180s)
```
- [x] regression: validator clean, save drift PASS (v3, 6 slots, additive-only)
- **Read this as a design change, not a bug:** the front line now shares the beating instead of hero 1 tanking
      alone, so the party survives longer on hard stages. The few seconds of drift on stage totals is crit luck from
      the changed RNG stream - the per-enemy TTK is identical, which is the number that proves the *combat* model did
      not move. Enemy damage was deliberately **not** retuned; difficulty impact is measured in the sweep above

### 11a — Encounter factory + composition + enemy ranks  `[x]`  (still 1 enemy per wave)
- [x] **simplified the plan on purpose:** no `EncounterData` SO and no `encounters.json` yet. Composition is the
      wave's existing enemy plus its rotation neighbours (`WaveConfig.GetEnemiesFor`), so it stays deterministic
      from (stage, wave) - **no save/schema change** - and one content pipeline instead of two. If 11c needs
      hand-authored mixes, add an optional block to `waves.json` then, when the data is actually exercised
- [x] `Scripts/Combat/EncounterFactory.cs`: team size (boss always 1, cap 3), rows/columns from
      `EnemyData.preferredRow`, per-enemy target rule from `EnemyData.targetRule`, wave budget split
- [x] `EnemyData` += `targetRule` (`EnemyTargetingMode`, new `Inherit` = wave default, new `BacklineFirst`) and
      `preferredRow` (`CombatRow`); authored in `enemies.json`, exported/generated/validated by the existing pipeline
- [x] `WaveConfig.GetEnemiesFor(stage, wave, normalWaves, count)`; `GetEnemyFor` unchanged (single-pick authority)
- [x] `Combatant.TargetRule` (nullable) - `null` = use the side rule, so every existing unit is unchanged
- [x] `BalanceConfig` += `waveHealthMultiplier` / `waveAttackMultiplier` / `waveGoldMultiplier` (all 1 = parity);
      `enemiesPerWave` clamped to 1..3
- [x] `CombatDirector.BeginWave` builds through the factory; `CombatSimulator.StartEncounter(team)` embeds it
- [x] parity-safe landmine fixes: `Simulator.IsEncounterActive` now means "any enemy alive" (it was "enemy 0
      alive", which would have ended a 3-enemy wave on the first kill), and `MapTargeting` shares one table
- [x] **verified - factory probe** (new menu `Tools/Idle RPG/Balance Lab/Encounter Factory Probe`, 1 click):
```
count 1 wave 1: 1 -> Slime[F0] 60hp 6atk 8g                       | total 60hp 6atk 8g
count 3 wave 1: 3 -> Slime[F0] 12.9hp | Bat[F1] 19.3hp | Goblin[F2] 27.9hp | total 60hp 6atk 8g
count 1 wave 3: 1 -> Goblin[F0] 130hp 12atk 18g                   | total 130hp 12atk 18g
count 3 wave 3: 3 -> Goblin[F0] 60.4hp | Slime[F1] 27.9hp | Bat[F2] 41.8hp | total 130hp 12atk 18g
```
      i.e. totals identical at 1 and 3 enemies, the wave's primary enemy keeps slot 0, and relative beefiness
      survives (goblin > bat > slime)
- [x] **verified - parity byte-identical** to the 10e baseline: TTK 4.0/6.0/11.3s, boss 44.0s, stage 79s@x1 and
      124s@x1.6, 11 kills, 272 gold, 2.20 gold/s. validator clean (new `ok [encounters]` line), save drift PASS
- [x] spec round-trip: `enemies.json` gained `targetRule: "inherit"` / `preferredRow: "front"`; export -> generate
      -> export stable (one-time cosmetic tint normalisation: specs store 8-bit hex, assets now match them)
- [x] **incidental crash fix found in the smoke test** (pre-existing, not from 11a): `HeroUnitView.RefreshFromSimulator`
      indexed `Heroes[heroIndex]` with `heroIndex = -1` for unbound/pooled views, and a negative index passes the
      `Length > heroIndex` guard -> `IndexOutOfRangeException` on every StageChanged. Play mode now logs 0 errors

### 11b — 1-3 enemies per wave, stacked on the battle page  `[x]`  (design changed: no enemy ranks)
- [x] **dropped enemy ranks entirely** (your call): `EnemyData.preferredRow` and the factory's rank assignment are
      gone, so the enemy side is a flat list of 1-3. Data, spec, generator and validator all cleaned up
- [x] **a wave ends when the last enemy dies** (was: first kill). `CombatDirector.NotifyEnemyKilled(index, gold)`
      still pays every kill, but only starts the inter-wave pause when `Simulator.AliveEnemyCount == 0`
- [x] **events carry the enemy index**: `EnemyDamagedInfo.EnemyIndex`, `GameEvents.EnemySpawned(...,index)` raised
      once per enemy, `GameEvents.EnemyKilled(...,index)`; 5 subscribers updated (UI, log, audio, debug logger)
- [x] `EnemyStackView` (new): up to 3 slots stacked vertically, one view per enemy, laid out top-down and bound
      from the simulator on every spawn. `EnemyUnitView` now knows its own index and ignores other enemies' events
- [x] each enemy: own sprite (auto-sized so 3 fit), own name, own HP bar, own damage-number anchor
- [x] log disambiguates duplicates as **Goblin A / Goblin B / Goblin C**; single enemies and bosses print bare
- [x] `Simulator.IsEncounterActive` fixed to "any enemy alive" (was "enemy 0 alive" - would have ended a 3-enemy
      wave one kill early); `BalanceLabMenu.RunStage` now builds waves through the factory and sums gold per kill
- [x] **verified live** (screenshot + logs): 3 stacked enemies each with its own bar; "Goblin A hits Knight",
      "Archer hits Slime B for 9", "Bat C for 7.7", "Bat defeated +5.7 gold", "(N left in wave)"; boss waves stay 1
- [x] **balance**: `enemiesPerWave 3`, `waveHealthMultiplier 0.95`. Measured stage 1 = **126s / 31 kills / 272 gold /
      2.16 gold/s** vs the 1-enemy baseline 124s / 11 kills / 272 gold / 2.20 (2% off). Tuning was measured, not
      guessed: x1.0 was 140s, x0.85 116s (the fight is quantised by hits, so the knob is coarse at low HP)
- [x] validator clean (`enemiesPerWave 3 ... wave budget HP x0.95`), save drift PASS, parity at 1 enemy byte-identical
- **Still open (next, optional):** hero-side `HeroData.targetRule` is still inert, and a ranged enemy archetype
      (`targetRule: "backlinefirst"`) that reaches the party's back rank is not authored yet

### 11c — Randomised wave size (recipe A)  `[x]`
- [x] **`BalanceConfig` recipe** replaces the fixed count: `minEnemiesPerWave` (1), `maxEnemiesPerWave` (3) and a
      `waveCountWeights` list. Recipe A shipped: **1 x25, 2 x50, 3 x25** -> out of 20 fights 5 singles, 10 doubles,
      5 triples (average 2.0). `MeanEnemiesPerWave` is computed from the recipe for the offline payout
- [x] **`WaveComposition.ResolveCount(stage, wave, isBoss)`** (new, pure): hashes (stage, wave) through an integer
      mixer and reads the recipe. Deliberately **not** the sim RNG (would shift crit rolls and needs no save data)
      and **not** a modulo cycle - `(stage*31+wave) % 3` would visibly alternate 1,2,3. Boss waves are always 1
- [x] factory + offline + generator + validator all wired to the resolver; the offline estimate now uses the
      **mean** (was the max), which would otherwise have inflated the away-time payout by ~1.5x
- [x] validator gained recipe sanity (weights >= 0, counts inside min..max, sum > 0, mean >= 1) and reports the
      recipe; the data generator writes the recipe so "Generate Data Assets" can no longer reset the wave size
- [x] **verified - histogram probe** (100 waves, 5 stages): `1x=26 2x=48 3x=26, average 2.00` against the recipe's
      25/50/25 mean 2.00; stage 1 sequence `22222322311221223212` and stage 2 a different one - no visible cycle
- [x] **verified - parity lever**: setting the recipe to `1x 100%` reproduces the single-enemy baseline **exactly**
      (79s @x1, 124s @x1.6, 11 kills, 272 gold, 3.45 / 2.20 gold/s). The multi-enemy feature is purely additive
- [x] **balance re-measured for the mix**: 0.95 -> 119s (2.29 gold/s), 0.98 and 1.00 -> 126s (2.16 gold/s). Kept
      `waveHealthMultiplier = 1` (neutral, no fudge) since it is the closest to the 124s / 2.20 baseline (2% off)
- [x] validator clean (`wave size 1-3, recipe 1x 25%, 2x 50%, 3x 25% (mean 2, boss always 1)`), drift PASS,
      live check: a 2-enemy Slime+Bat wave rendered with 24/36 hp split and the log read `Bat A` / `Goblin B`

### 11e — Battle log fixed for multi-enemy  `[x]`
Four defects, all in the event/log plumbing (the stacked views and HP bars were fine):
1. **`GameEvents.HeroDamaged` carried no attacker index**, so every incoming-hit line named enemy #0 ("Slime A hits
   Knight" while the Goblin swung). The sim already knew the attacker (`DamageEvent.AttackerIndex`) - it is now
   forwarded through `Simulator.HeroDamaged` -> `GameEvents.HeroDamaged` (new 5-arg `SafeInvoke` overload) -> the
   log, which keys per `hero:{hero}:{enemy}`
2. **Three "appears" lines per wave** overran the 3/s budget and pushed real events into "... N more hits". Now
   **one line per wave**, written on the first spawn event: `-- Wave 5: Bat, Goblin --` (duplicates collapse to
   `Goblin x2`, bosses get a `(BOSS)` suffix)
3. **A line could rename itself**: the aggregator re-rendered by reading the *live* wave, so a wave rollover could
   relabel an open "xN" line. The aggregate now closes on every wave spawn and is keyed per enemy - names are
   captured once
4. **Feed tuned for multi-enemy volume**: `maxLinesPerSecond` 3 -> 5, `aggregateWindowSec` 0.35 -> 0.5,
   `poolSize` 24 -> 36 (scene values, set by `MvpSceneBuilder.BuildCombatLog`)
- [x] subscriber signatures updated (hero view, damage-text pool, debug logger) - all ignore the new index except
      the log
- [x] **verified live** (dumped the visible lines): `Bat B hits Archer for 1.1  (103/110)`, `Slime defeated  +3.2
      gold`, `-- Wave 5: Bat, Goblin --`; no "more hits" summary across 20s of combat; 2-enemy stack drawn with the
      24/36 hp split
- **Rule extracted:** the battle log is a standing regression surface - see the note in section 0. Every new
      combat-text feature (statuses, abilities, drops, item use) re-opens it

## 1f. Refactor + regression command (2026-09-23)  ·  **done + verified**

The six largest files were split into `partial` classes (same class, same fields, same behaviour - moved, not
rewritten). Full table in `Architecture.md` §6.6. Largest file went 1057 → 507 lines; every split was diffed against
`git show HEAD` and proved to be a pure move (0 code lines changed).

| Class | Before | After |
|---|---|---|
| `GameManager` | 1057 | 507 + 342 (`Stages`) + 292 (`Save`) |
| `MvpSceneBuilder` | 942 | 331 + 291 (`Battle`) + 356 (`Panels`) + 99 (`Overlays`) |
| `PlaceholderSpriteGenerator` | 673 | 322 + 389 (`Shapes`) |
| `ContentValidator` | 617 | 201 + 472 (`Checks`) |
| `CombatLogUI` | 576 | 449 + 190 (`Events`) |
| `Encounter` | 500 | 332 + 218 (`Targeting`) |

Dead code removed (each verified to have no caller): `RetryAfterDefeat` + its `R` hotkey, `AutoRetryEnabled`
(property + save field; the retired key is allow-listed in the drift test), `UpgradePanelUI.isVisible` +
`OnBecameVisible` (always-true guards), `MvpSceneBuilder.CreateTab` + `ManagementTabBarBottom` (3-tab era leftovers).

**New regression entry point:** `Tools > Idle RPG > Run All Checks (regression)`
(`RegressionCheckMenu.RunAllChecks`) - save drift + content validation + the golden-number report, one PASS/FAIL
line. Batch-mode: `-executeMethod IdleRPG.EditorTools.RegressionCheckMenu.RunAllChecks`.

Verified after the refactor (batch-mode run, log `/tmp/idlerpg-regression.log`):
`[SaveRoundTrip] PASS` (v4 round-trip + v1/v2/v3 migrations, including the new v3 -> v4 check) ·
`Content validation: RESULT: clean (0 warning(s))` · golden numbers **exact**
(`80s @x1` / `126s @x1.6, 22 kills, 272 gold, 2.16 gold/s`) · `[Regression] REGRESSION: PASS`.

Play-verified in the same session: bounce loop, ascension (gate closes on ascend, re-opens only after a re-climb,
lifetime best kept, run best reset, no gem farming on re-clears), and the offline modal.

## 1g. Sweep-wide validator + loop harness (B4)  ·  **built + verified 2026-09-25**

The regression net stopped being cosmetic. Before: `Run All Checks` *printed* the golden numbers and never ran the
robot, so a change that halved DPS or reintroduced the additive stall still said PASS. Now it is a gate.

- **`BalanceBaseline.cs`** - the recorded golden numbers in one place (±2%). `Run All Checks` asserts:
  `[Golden] ok stage 1 seconds @x1.0/x1.6, kills, gold, gold/s` (measured 126.05s / 22 kills / 272 gold / 2.158 gold/s
  vs baseline - all 0.0%/0.1% off). Re-baselining is one deliberate edit with a date+reason.
- **The robot joined the build** - `ClimbSimulation` now returns a machine-readable `ClimbResult`
  (ReachedStage / StuckStage / WorstWallSeconds / MedianShoppingGap / rows). The printed report is byte-identical
  (menu output verified against the B3d run). `ContentValidator.CheckLoopHealth()` runs it with a bounded budget
  (target stage 25, max stage 40): a STUCK or undershoot is an **Error** (build fails), a slow wall is a **Warning**.
- **Reference checks (the deleted-Slinger class of bug)** - `WaveConfig` pools and `PartyConfig` lanes are walked for
  null/stale/out-of-spec elements; every stat upgrade asset must have a spec entry and agree on `effectMode` and gain;
  duplicate stat tracks are an error.
- **Deliberately NOT asserted:** stage-time monotonicity. A wall makes stage times legitimately rise inside a band
  (46s -> 229s), so a monotonic curve is the healthy shape - the loop check asserts the frontier *moves*, not that
  seconds are flat.
- **Planted-failure proof (both reverted, final PASS):**
  - ATK/HP set back to `AdditiveBase` -> validator `-- [loop] the robot is STUCK on stage 25 after 187.4 min (451
    upgrades, 42.3 min worst wall)` - the exact additive-failure mode, caught by the build.
  - Archer attack +20% -> `[Golden] FAIL stage 1 seconds @x1.6: 112.85 (baseline 126, -10.4%)` x3, REGRESSION: FAIL.
- **Measured on clean data:** the validator's robot reaches stage 40 in 96.9 min (worst wall 8.8 min, peak stage time
  268s) and the whole check costs under a second.

## 1h. B5 sessions 1-2: one purchase path + save v5  ·  **built + verified 2026-09-25**

- **Session 1 - `TrackService` (one checkout).** `ProgressionTrack` (a menu row: id/name/currency/scope/effect/gain/
  cost/cap/reset) built from the shipped assets, and `TrackService.TryBuy` is now the ONLY place that turns a currency
  into a progression level. `UpgradeManager` / `AscensionManager` kept their public API and delegate to it, so the
  game, UI and robot are untouched. Verified: goldens exact, validator green, robot climb identical, `Run All Checks`
  PASS; a live ATK buy (2,242 gold) and a prestige buy (1 token) behaved exactly as before.
- **Session 2 - save v5 (keyed levels).** `SaveData.CurrentVersion = 5`; hero columns + prestige list became ONE
  `levels[{key,level}]` list (`"hero_knight.attack" = 80`, `"Prestige_Damage" = 3`). Keys are a permanent contract
  (B35). `SaveMigrations` copies v4 values by hand (a rename, nothing lost); legacy lists write empty. Verified:
  drift stable for default + fully-populated payloads, v1..v4 all migrate, a dedicated v4->v5 numbers-preserved test
  passes, and the **real save** migrated live: v4 on disk -> identical stage 24 / gold 4,793.29 / ATK 80,80,91 ->
  v5 file written -> v5 relaunch loads the same stats. Restored the v4 file so the next launch runs the real
  migration. Fixture `SaveSamples/v4_sample.json` committed per the schema-bump rule (B35/B36 recorded in
  `Architecture.md`).

- **Sessions 3-4 - `tracks.json` + the zero-code proof.** The upgrade spec file became `tracks.json` (one card for
  every row: hero stats + permanent tracks). `SceneWiringUtility` now loads the scene's lists FROM the spec (not from
  three hardcoded names), so a new row is picked up by `Build MVP Scene` automatically. **Proven live:**
  `Track_GoldHoarder` (+2% Gold per lvl, 2 tokens, cap 20) was added to `tracks.json` by hand - no C# edits - then
  `Generate Assets From Specs` made the asset, `Build MVP Scene` wired it, and in Play Mode it appeared as a 4th
  ASCEND row, cost 2 tokens, bought, multiplied gold x1.02, and saved as the generic key `Track_GoldHoarder` in the
  v5 file - reloaded level 1 on the next launch. Export -> Generate -> Export is byte-identical for the six shipped
  rows. Add a new upgrade anywhere (B6 auto-buy, B7 gem sinks) = one card + two menu clicks.

## 1i. B6 Step 1: automation cards + engine  ·  **built + verified live 2026-09-25**

- **Cards are data, earned with rebirth tokens** (never free): new effect kind `AutomationUnlock`, two cards in
  `tracks.json` (`AutoBuy` automated "Auto-Buy Manager" = 4 tokens, needs 1 rebirth) + (`FastForward` "Speed Button"
  = 6 tokens, needs 2; speech behaviour is Step 3). Teaser-locked UI is Step 2.
- **`AutomationService`** runs on the 1s tick (RunController). Auto-buy: buys the cheapest affordable stat upgrade
  across all heroes, always staying above a **reserve** you set (the dial), one aggregated toast per second.
  **The big reset stays 100% manual** - no auto-ascend card, ever.
- **Save (schema v5, additive, no bump):** ownership rides the keyed `levels` (`autoBuy: 1`); settings
  (`enabled`, `budgetFraction`) in a small `automation` list. Drift test + full payload updated.
- **Verified live:** cards listed, bought (tokens 2->8, owned), enabled, budget 0.20; the machine bought ATK levels
  on two heroes while combat advanced to stage 25 and gold never went below the 20% reserve; SaveNow wrote
  `autoBuy: 1` + the settings; save restored after. Compile clean, `Run All Checks` PASS (goldens, validator incl.
  the new automation checks, loop health, save drift).
- **Crash note (2026-09-25):** the editor crashed between two probe evals; the save was byte-identical afterwards,
  the MCP request timeout was raised (client config, 60s -> 180s) and the session was re-verified from scratch.
- **Steps 2-3 - the AUTO panel + the Speed Button.** `AutomationPanelUI` (runtime-built AUTO strip on the UPGRADES
  page, same boot-race pattern as UpgradePanelUI): owned cards show ON/OFF + the "Keep X% gold" dial; unowned cards
  show `LOCKED — costs N tokens (after M rebirths)` with a BUY button. The Speed Button multiplies combat tempo x2
  (RunController) while the **honesty guard** keeps the money-tape at base tempo: the saved measured rate is divided
  by the speed multiplier at capture, so x2 can never inflate the offline / instant-income quotes. **Verified live:**
  bought both cards, toggled both on; ledger gold/s ~1262 while speed on, saved `lastGoldPerSecond` = 631 (=/2);
  panel rows read `Auto-Buy Manager (on)` + `Keep 20% gold` and `Speed Button (on)`; save round-trips `autoBuy:1`,
  `fastForward:1` + settings. Compile clean; `Run All Checks` PASS after the rebuild.
- **Step 4 (robot learns the cards) - deferred:** the robot already buys-cheapest like auto-buy; a meaningful
  version means simulating the (manual, player-only) rebirth loop, which deserves its own small step.

## 1e. Remaining path to MVP  ·  **what is left, in order**

Base gate: every step below names the loop beat or money path it serves. Anything that cannot is not in the base.

| # | Step | Serves | Acceptance | Size |
|---|---|---|---|---|
| **B3d** | Phase 2: the per-level stat effect is **multiplicative** (ATK/HP x1.09, DEF x1.05) behind a data field on `StatUpgradeData`, the gain derived from the content/cost/gold growth | the loop beat "buy -> push further": additive power could not race exponential content (measured: stage time 126s -> 493s, stall ~stage 24) | `[x]` 2026-09-25: robot player clears stage 30 in 70 min (cheapest) / 34 min (attack-only), worst wall 8.8 min; goldens unchanged because level 0 = base stat | done |
| B4 | **Sweep-wide validator + loop harness** | keeps the loop healthy: an accidental stall must fail the build | `[x]` 2026-09-25 (see §1g): dangling-ref/spec-asset checks, the robot climb runs inside the validator (target stage 25), golden numbers asserted +-2% via `BalanceBaseline`; planted stall and planted +20% buff both fail the build | done |
| **B5** | Generic progression tracks (schema v5) | money path: more tracks = more to buy = more gem/ad relevance | a new track = spec + generate, zero code; a v4 save migrates to identical numbers | medium |
| **B6** | Automation & QoL (`AutomationUnlock` cards; **ascension stays manual forever** - owner decision) | retention: idling must pay off while away | `[~]` Step 1 done 2026-09-25 (see §1i): AutoBuy + FastForward cards as data, `AutomationService` on the 1s tick, auto-buy proven live with the reserve respected; panel + speed + robot steps remain | medium |
| **B7** | Monetisation pass (`Monetisation.md`) | money path: ad placements + gem sinks + mock IAP | every payout goes through the ledger rate; per-day caps enforced; mock IAP swaps for a real store with one implementation | medium |
| **B8** | Content + onboarding pass | first-run clarity: a stranger knows what to press | ~8-12 enemies, 5-6 heroes (data only) + 3 first-run tips; content added with no code | medium |
| **B9** | Release plumbing | ship it | version stamp, crash log file, one scripted `-batchmode` build command | small-med |
| **B1** | **Device pass (LAST, owner-run)** | does it actually feel good on a phone | 60fps idle with 3v3 on device; safe area clean; offline modal + CLAIM work by touch | ½ day |

Loop-walk items still open from the bounce work (do them with B8 unless a play test says otherwise): first-run hint,
shop beat verification (boss/milestone gems -> fast-forward + offline cap), and the cosmetic formation-slot tiles
(3 empty slots draw red HP bars).

## 2b. Base v1.0 — the loop + money  (current focus)

| # | Step | State |
|---|---|---|
| B3 | loop beat: the fallback bounce (+ milestone gems) | `[x]` |
| B3b | ascension retune: per-run gate, +20%/level, cap 25 | `[x]` |
| B3c | offline rate guard (near-zero saved rate paid 0) | `[x]` |
| B3d | **phase 2 - the compounding-upgrade fix** (additive effect vs exponential content) | `[x]` 2026-09-25 (see §1d) |
| B4 | **sweep-wide validator band + bounce detection** | `[x]` 2026-09-25 (see §1g) |
| B5 | 14 - generic progression tracks (**schema v5**) | `[x]` 2026-09-25 (see §1h): TrackService = single purchase path; save v5 = one keyed `levels` list; `tracks.json` = the only upgrade spec; zero-code demo track (`Track_GoldHoarder`) added in data and proven live |
| B6 | 16 - automation & QoL | `[~]` Step 1 done 2026-09-25 (see §1i) |
| B7 | monetisation pass (design: `Monetisation.md`) | `[ ]` |
| B8 | content + onboarding pass | `[ ]` |
| B9 | 21-lite - version stamp, crash log, scripted build | `[ ]` |
| B1 | device pass (deliberately last, owner-run) | `[ ]` |

Dropped from B3 on 2026-09-22: the piecewise `DifficultyCurve` and the "you need ~X more power" guidance banner.
The curve cannot fix a lost race, and the banner quoted a number the player cannot see. See `Content.md` §3 for the
measured arithmetic and `UI-UX.md` §5 for the guidance rule that replaced it.

Detail, acceptance and sizes: `Roadmap.md` §2. Every B-step is "done" only when the parity net is green,

the battle-log contract is checked, and the touched screen has had a device smoke.

### 11d — index-0 cleanups  `[x]`
- [x] `DevOverlay` no longer reads enemy 0: it gained a `wave` line with every enemy and its own HP, and the
      `enemy hp` / `eta` lines now use the **wave total** (`Simulator.EnemyHealthPercent` was already total)
- [x] verified live: `wave  3 enemies (3 alive)  Goblin#0 47.4/60.4  Slime#1 15.9/27.9  Bat#2 27.8/41.8` and
      `enemy hp  108.6 total (100% of wave)   eta 6.6s`; `HudController`/HUD header already used the index-aware
      `CombatManager.CurrentEnemyName` ("Goblin +2")
- [x] regressions re-run: validator clean, drift PASS, golden numbers unchanged (126s / 22 kills / 272 gold / 2.16)
- **Dropped and deleted (2026-09-21):** the ranged enemy archetype **and** the hero-side target-rule data
      (`HeroData.targetRule`) are gone - field, generator lines and the orphaned asset keys. No loop beat, no money
      path, no dead data (see `Roadmap.md` §2, "Dropped")

### 12 — Effect pipeline + statuses  `[ ]`
- [ ] `EffectPipeline` (11 ordered stages; per-type floors; armor% + pen)
- [ ] per-entity crit (`crit`, `critDmg`) - delete the global crit from `CombatScaling`
- [ ] `StatusContainer` (stack modes, expiry, dot/hot ticks, tags, eviction cap)
- [ ] `StatAggregator` sources (stats, statuses, auras, run modifiers)
- [ ] prototype enemy ability (enrage below 30% HP) visible in log/UI
- [ ] **battle log re-checked** with the new texts (status ticks, per-type damage, crit changes) - see section 0
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

### 15 — Zones, difficulty curves, walls, affixes, guidance  `[~ split]`
- `[x]` milestone gems - landed early with the loop beat (first clear of every 5th stage, 5 gems, new-best only)
- `[-]` wall guidance banner + "wall breaks in ~N min" ETA - **dropped**: it quoted an invented index the player
      cannot see. Replaced by affordability ETAs + attempt progress (`UI-UX.md` §5)
- `[ ]` `ZoneData` + piecewise `DifficultyCurve`; stage -> zone mapping (v1.1)
- `[ ]` walls every N stages (+ milestone chests); zone boss + zone milestones
- `[ ]` affix chips (max 3) trading difficulty for reward
- `[ ]` Balance Lab `Sweep Stages` + bounce detection + curve CSV
- `[ ]` acceptance: zone 2 gate; band change at walls; the bounce stalls and then moves within ~3 min of income

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

