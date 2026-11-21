# Routes and Flows — every screen, button and state change

> Owns: how the player moves through the game, what each control does, and the code behind it.
> Parents: `Run-Loop.md` (core-loop canon), `UI-UX.md` (screen rules), `Camp.md`, `Boons.md`, `Training.md`.
> Status: **as built**, verified 2026-11-21 against `ba4a4cf`. When this page and the code disagree, the code wins.

---

## 1. The game in one line

Boot → lobby (paused) → START → automatic battle → camp / level-up card when they fire → party wipe →
RUN OVER card → NEXT RUN banks Perk Points → stage 1. Gold and Training upgrades never reset.

The player never steers the fight. The player chooses **what to buy** (Training, perks) and **when to stop**
(a wipe or, later, END RUN).

---

## 2. Scenes and boot

| Index | Scene | Job |
|---|---|---|
| 0 | `Assets/IdleRPG/Scenes/Boot.unity` | splash; loads Main |
| 1 | `Assets/IdleRPG/Scenes/Main.unity` | the whole game; built by `Tools > Idle RPG > Build MVP Scene` |

**Boot order** (`GameManager.cs:269-307`)

1. `MobileRuntimeBootstrap` sets 60 fps, portrait lock, no screen sleep (device only).
2. `GameManager.Awake` wires services; creates the Android back handler.
3. `GameManager.Start`:
   - save found **and `runActive` true** → `combatManager.StartRun()` at the saved stage and wave;
   - otherwise → state **Lobby**: no fight runs.
   - then `TryClaimDailyStreak()` and the first-run tip.
4. `StartScreenUI.Start`: if the saved run is active, hides the lobby and goes straight into the fight;
   otherwise shows the lobby.

So a live fight is **never behind the lobby**. The lobby is the between-runs screen. Closing the app on a
live run reopens into the same fight; it never opens the menu.

---

## 3. Lobby — `StartScreenUI`

| Property | Value | Code |
|---|---|---|
| Canvas | Screen Space Overlay, sorting order **250** | `StartScreenUI.cs:120` |
| Bottom bar | 5 tabs at `0.03–0.14` height | `StartScreenUI.cs:147-153` |
| Page band | `x 0.02–0.98`, `y 0.13–0.885` | `StartScreenUI.Pages.cs:91-92` |
| Header | the real battle header is borrowed and put back | `AttachSharedHeader` / `DetachSharedHeader` |
| Time | **never paused** for the menu; `Time.timeScale` stays 1 | `ShowLobby` / `HideLobby` |

**One page at a time.** Every tab first calls `HideTraining`, `HideParty`, `HidePerks`.

### Control table

| Control | Handler | What happens | State |
|---|---|---|---|
| **START** | `StartGame` | begins a new run (`StartRun(1)`), closes the lobby | live |
| **SHOP** tab | `OnTab("SHOP")` | closes other pages, highlights SHOP | **Defect R3:** no page yet |
| **PARTY** tab | `ShowParty` | opens the roster page | live |
| **BATTLE** tab | `OnTab("BATTLE")` | same as START: closes the lobby and begins the run | live |
| **ASCEND** tab | `ShowPerks` | opens the PERKS page | live |
| **TRAIN** tab | `ShowTraining` | opens the gold tree | live |
| Header gear | `HudHeaderUI.OnGearClicked` → `GearMenuUI` | **Between runs: SETTINGS** (sound, restore, privacy, version). **In a fight: PAUSE** (resume, sound, quit → confirm). Never an upgrade screen. | live |

**Rule: no upgrade screen during a fight.** The lobby exists only in the `Lobby` state (no run). The battle
gear opens a minimal pause with resume, sound and quit — it cannot shop, train or ascend. That keeps the
"upgrade mid-fight" exploit closed while still allowing a real pause.

---

## 4. Lobby pages

### 4.1 PARTY — `PartyPanelUI`

- One tab, **ROSTER**, built on first open. Shows three hero cards, the selected hero's stats, gear slots and
  ability slots. **Read-only.**
- Formation is **not** here: move heroes on the battle board (tap a hero, tap a free pad).
- The INVENTORY (bag) tab is **off** (`InventoryEnabled = false`). Boss gear drop chance is **0**
  (`BalanceConfig.asset:96`), so no item is created. This is consistent, not a bug.
- Code: `PartyPanelUI.cs`, `PartyRosterView.*`, `InventoryTabUI.*` (dormant).

### 4.2 ASCEND — the PERKS page — `StartScreenUI.Pages.cs`

- One row per token track from `AscensionManager.TokenUpgrades`.
- Live tracks: **Damage**, **Gold**, **Health** (`Data/Config/Prestige_*.asset`). One level per tap.
- Header reads `PERK POINTS <n>`.
- Button states: `Buy <cost>`, `MAX`, or disabled with `Need <cost> tokens - you have <n>`.
- There is **no** END RUN button here (the old `AscensionPanelUI` is retired and not in the scene).

### 4.3 TRAIN — the permanent gold tree — `StartScreenUI.Training.cs`

- Four columns: **Knight / Mage / Soldier (Gold)** and **PARTY (Perk Tokens)**.
- **1,357 nodes**, 75 levels, built from `Data/Training/TrainingTree.asset`.
- Node = 127 px medallion. Art comes from `TrainingIconSet`; a letter is the fallback.
- Popup shows the effect, cost, and a Buy button. `TrainingService` owns every purchase.
- First open builds the whole tree. At `ba4a4cf` this is **one pass** and froze the Editor for ~1.25 s
  (`Training.md` §5b). The measured fix (4 ms per frame) was reverted today with the rest.

### 4.4 SHOP — live (lean shelf)  ·  built 2026-11-21

- `StartScreenUI.Shop.cs` builds the page on first open.
- Rows: **No Ads**, **30 Gems**, **Starter Pack** (50 gems + a 30-minute gold boost), **Golden Foundry**
  (gem track: 25 gems per level, +5% gold, 10 levels).
- Buttons say **BUY**; no invented prices. The real localized price comes from the store SDK (mock today).
- The Golden Foundry is the gem sink. No Ads owners see **OWNED** and no ad buttons.
- **Restore** lives in the gear SETTINGS sheet.

---

## 5. Battle page — `HudController` + `RunController` + `CombatManager`

### 5.1 Bands, top to bottom

| Band | Height (1080×1920 units) | Owner |
|---|---|---|
| Header (currency, boost, menu) | 115 | `HudHeaderUI` |
| Journey band (`GREENWAKE`, trail) | 100 | `JourneyBandUI` |
| Arena (stage RenderTexture, 3 heroes, 1–3 enemies) | `0.373` → stage row | `BattleStage`, `FormationBoardView`, `EnemyStackView` |
| Party XP bar | `0.346` → `0.373` | `XpBarUI` |
| Ability strip | `0.226` → `0.346` | `AbilityPanelUI` |
| Battle log | `0` → `0.226` | `CombatLogUI` |

`MvpSceneBuilder.cs` owns these constants. The scene is a generated artifact.

### 5.2 One wave, step by step

1. `RunController.Update` ticks the fight. Tempo = `Automation.SpeedMultiplier` (1 unless the Speed card is
   owned — the panel is retired, so 1 today).
2. `CombatManager` picks the next actor (ATB-style, one unit at a time).
3. A hero may tap an ability in the strip. `CombatManager.RequestAbility` casts if it is off cooldown and the
   unit is alive. A story scene or boon card blocks the cast.
4. Damage, gold and XP events fire. Gold flies to the counter; XP motes fly to the bar.
5. Wave clears → next wave. Last wave → the boss pool. Stage clears → next stage.
6. **Camp** fires on the configured cadence: the arena becomes a camp, the ability strip becomes four choices
   (`SHORT REST`, `SHORT REST+` ad, `LONG REST` token, `TRAIN`). The game waits for a tap.
7. **Party level** → the level-up card opens, the fight pauses, three boons are dealt, a pick applies for the
   rest of the run. Rerolls are limited per run.
8. Party wipe → `EndRunOnWipe`.

### 5.3 Overlays (modal canvas, sorting order 100)

| Overlay | Opens on | Choices |
|---|---|---|
| `BoonCardUI` | `PartyLevelGained` with picks owed | pick 1 of 3, reroll |
| `DefeatModalUI` | `GameEvents.DefeatShown` | **REVIVE** (rewarded ad, once per run) or **END RUN** (bank + lobby) |
| `GearMenuUI` (pause) | header gear during a fight | **RESUME**, **SOUND: ON/OFF**, **QUIT RUN** → second confirm (**END RUN** / **KEEP FIGHTING**) |
| `OfflineRewardsPopup` | `GameEvents.OfflineRewardsReady` | **unreachable today**: only the debug key / editor menu calls `EvaluateOffline` (defect R9) |
| `ToastUI` | most events | one line, no input |
| `DevOverlay` | F3 | dev text only; must not ship visible |

---

## 6. Run lifecycle and state machine

**States:** `Boot`, `Lobby`, `Combat`, `Boss`, `Defeat`, `Ascension` (`GameState.cs`). Only `GameManager` sets them.

| From | Event | Handler | To | Result |
|---|---|---|---|---|
| Boot | saved run active | `GameManager.Start` | Combat/Boss | resume the fight, no lobby |
| Boot | saved run inactive / no save | `GameManager.Start` | **Lobby** | no fight runs |
| Lobby | START / BATTLE tab | `StartGame` → `StartRun(1)` | Combat | new run, saved `runActive = true` |
| Combat | boss wave starts | `OnWaveStarted` | Boss | state reflects the boss |
| Boss | boss cleared | stage advance | Combat | next stage |
| Combat/Boss | party wipe | `OnPartyWiped` → `EndRunOnWipe` | Defeat | run stops; card shows; `runActive = false` |
| Defeat | REVIVE | `WatchAdForRunRevive` | Combat | same stage; `runActive = true` |
| Defeat | NEXT RUN | `ConfirmEndRun` → `EndRun` | **Lobby** | bank tokens, reset run |
| Combat/Boss | header gear → **PAUSE** → QUIT RUN → confirm | `GearMenuUI` → `EndRun` | **Lobby** | banks, resets, no rejoin |
| any | F1 debug | `EndRun` | Lobby | same as QUIT RUN |
| any | F8 | full reset | Boot | save, backups and prefs wiped |

### 6.1 What `EndRun` does (`GameManager.Stages.cs`)

1. Stops combat; state = `Ascension`.
2. `BankRunReward`: tokens = `max(1, f(depth))` if at least one stage was cleared, else 0.
3. Hero **stat levels** reset (gold and Training are permanent — `Stages.cs:190`).
4. Run counters reset: stage, run best, run gold earned, kills, stages cleared, camp state, party XP, boons.
5. `RunIsActive = false`; state = **Lobby**; **save now**. No new run starts here.

### 6.2 What survives a run end

| Persists | Resets |
|---|---|
| Gold, Training nodes | Party XP and level |
| Perk Points, perk levels | Boons taken this run |
| Gems, daily streak day | Camp tokens and camp checkpoint |
| Highest stage, lifetime totals | Hero stat levels bought with gold |
| Ability assignments | Revive-used flag |
| Save schema **v12** (`runActive` is additive) | — |

**The exit today:** the three-bar button is QUIT RUN (two taps), and it ends the run. There is no "rejoin the
same fight" path. Closing the app on a live run resumes the fight; closing it on the run-over card boots to the
lobby (the wipe already cleared `runActive`).

**Known edge (not fixed yet):** force-quitting on the RUN OVER card skips the bank, so that run's Perk Points are
lost. The card says NEXT RUN banks them. Banking a pending wipe on boot is the follow-up if we want to protect it.

---

## 7. Economy and progression flows

| Currency / bar | Earned by | Spent on | Survives a run end? | Live UI? |
|---|---|---|---|---|
| **Gold** | every kill, stage clear bonus | **TRAIN** hero columns (Attack / Armor / Max HP) | **Yes** (permanent) | TRAIN page |
| **Perk Points** (tokens) | `EndRun`, scaled by depth | **ASCEND** tracks, TRAIN **party** column | **Yes** | ASCEND + TRAIN |
| **Gems** | daily streak, first clear every 5th stage | **Golden Foundry** (+5% gold per level) | **Yes** | SHOP |
| **Party XP** | kills | party levels → boon picks | **No** | XP bar |
| **Camp tokens** | one per run | Long Rest, camp training | **No** | camp strip |
| **Highest stage / lifetime totals** | play | — | **Yes** | header / debug |

### 7.1 Money path today

```
rewarded ad (mock)  ->  REVIVE (defeat card)          live
                    ->  camp SHORT REST+              live
IAP (mock)          ->  No Ads, 30 gems, Starter      live (SHOP page)
gems                ->  Golden Foundry (+5% gold)     live (SHOP page)
```

The reward maths still go through the ledger and `AdCapsService`, so a real SDK can be swapped in later
without touching the game rules. **The surface is live; only the real SDK is missing.**

---

## 8. Known route defects (verified against `ba4a4cf`)

| # | Severity | Defect | Evidence | Fix size |
|---|---|---|---|---|
| R1 | ~~P0~~ | ~~START resets the run~~ **FIXED 2026-11-21**: the lobby has no live run; START begins one | verified by code test | done |
| R2 | ~~P0~~ | ~~BATTLE tab does nothing~~ **FIXED 2026-11-21**: same path as START | verified by code test | done |
| R3 | ~~P1~~ | ~~SHOP tab has no page~~ **FIXED 2026-11-21**: lean SHOP page (No Ads, 30 gems, Starter, Foundry) | `StartScreenUI.Shop.cs` | done |
| R4 | P1 | No settings sheet: mute, Restore, privacy link | no settings UI; mute keys exist only in code | M |
| R5 | P1 | No ads or billing package | `Packages/manifest.json` | L |
| R6 | P1 | First-run hint says "Tap UPGRADE" | `FirstRunTips.cs:62`; no UPGRADES page | S |
| R7 | ~~P1~~ | ~~No mid-run END RUN~~ **FIXED 2026-11-21**: pause menu → QUIT RUN → confirm | `GearMenuUI` | done |
| R8 | P1 | Level-up card is silent | `AudioDirector.cs:106` listens to `HeroLevelChanged` | S |
| R9 | P2 | Offline popup unreachable | `EvaluateOffline` called only by debug code | S (wire or cut) |
| R10 | ~~P2~~ | ~~Dead UI code still compiled~~ **REMOVED 2026-11-21**: retired panels, rows, nav, the old builder file and two unused SKUs | 13 files deleted | done |
| R11 | P2 | Docs disagree with code | `Run-Loop.md` §3 vs `Stages.cs:190`; `UI-UX.md` SHOP row | S |
| R12 | P2 | TRAIN first-open freeze (reverted fix) | `Training.md` §5b; one-pass build at `ba4a4cf` | S (re-apply) |
| R13 | P2 | No app icon or launch screen | `ProjectSettings.asset` icon lists empty | M |
| R14 | P2 | Name / id / version not final | `bundleVersion 0.9.0`, `Idle RPG`, `com.sseengal.idlerpg` | S (decision) |
| R15 | P2 | Crash log never installed | `CrashLog` has 0 call sites | S |
| R16 | P3 | Balance baseline void | `Checklist.md` §0, `Content.md` §10 | M |
| R17 | P3 | Speed / auto-buy unreachable | `AutomationPanelUI` not in scene | S (cut or home) |
| R18 | P3 | Gear parked (consistent) | drop chance `0`, bag off | none |

**Still dead, deliberately not removed:** `FirstRunTips` (hints are off), `LeaderboardService` (no UI),
`OfflineRewardsPopup` (unreachable). They are inert, not risky. Remove them when the scene is rebuilt.

---

## 9. Where to change what (data ownership)

| You want to change | Edit this | Not this |
|---|---|---|
| Gold rates, XP curve, enemy scaling, camp cadence | `Data/Config/BalanceConfig.asset` | code |
| Training node effects, costs, order | `Data/Training/TrainingTree.asset` | `TrainingService` |
| Perk tracks and prices | `Data/Config/Prestige_*.asset` | `AscensionManager` |
| Boon deck and rarity | `PerkPoolDef` asset | `BoonCardUI` |
| Wave recipes, boss pool | `Data/Config/WaveConfig.asset` + `Stage01_WavePlan.asset` | `CombatManager` |
| Land and boss names on the band | `Data/Config/JourneyCatalog.asset` | `JourneyBandUI` |
| Abilities and who owns them | `Data/Abilities/*.asset` + hero/enemy data | `StatResolver` |
| Screen layout, bands, scene objects | `Assets/IdleRPG/Editor/MvpSceneBuilder*.cs`, then rebuild the scene | `Main.unity` by hand |
| Save shape | `SaveData.cs` + `SaveMigrations.cs` (bump the version) | ad-hoc keys |

Rebuilding the scene is the supported way to change layout: `Tools > Idle RPG > Build MVP Scene`.
The builder re-applies the tab icons, training icons, boons and prestige assets, so a rebuild keeps them.

---

## 10. Five-minute smoke test (run after any route change)

1. Boot with no active run. The lobby shows; **no fight runs behind it**; time is not frozen.
2. Tap **PARTY**, **ASCEND**, **TRAIN**. One page at a time; no page stacks; no console error.
3. Tap **SHOP**. The lean shelf opens: No Ads, 30 Gems, Starter, Golden Foundry. Buy 30 gems with the mock.
4. Tap **START**. A run begins at stage 1; the lobby closes.
5. Tap the **gear once**. A pause menu opens: RESUME, SOUND, QUIT RUN. Time stops.
6. Tap **QUIT RUN**. A second confirm shows the Perk Points you keep. Tap **KEEP FIGHTING** to return.
7. Tap **QUIT RUN** → **END RUN**. The run ends, Perk Points are banked, and the lobby opens.
8. Tap **START** again. A fresh stage-1 run begins. The old run is not rejoined.
9. Kill the app mid-fight, reopen. You are back in the **same fight**, no lobby, no upgrade window.
10. Press **D** (debug wipe). The RUN OVER card shows. Tap REVIVE; the same stage resumes.
11. Press **D** again. REVIVE is used. Tap **END RUN**; the lobby opens; Perk Points increased.
12. Open TRAIN for the first time. Record the freeze (defect R12).

Pass = no upgrade screen is reachable during a fight, START always begins a run, and a quit always ends one.

> **If a script was recompiled while Play was running, stop and start Play before testing.** Unity reloads the
> code but not the game. The static handles (`StartScreenUI.Instance`, `HudController.Instance`) go null, so the
> three-bar menu silently does nothing. That is a test-session state, not a game bug. Fresh Play fixes it.

---

## 11. Where this page sits

- Fix order for the defects: `Release-Checklist.md` §3 and §12.
- Loop design and the two upgrade layers: `Run-Loop.md`.
- Screen rules (type scale, spacing, safe area): `UI-UX.md`, `UI-Layout.md`.
- The one manual runbook: `Manual-Tests.md`.

---

## Change log

| Date | Change |
|---|---|
| 2026-11-21 | Created from the code at `ba4a4cf`. Routes, states, economy, defects R1–R18, data ownership. |



