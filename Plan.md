# Idle RPG — Build Plan & Progress Tracker

> 2D Mobile Idle RPG (Unity 6000.6.0f1, URP 2D). iOS + Android.
> Repo: https://github.com/sseengal/idleRPG.git
> Last updated: Step 0

---

## 0. Tooling (VERIFIED)

| Tool | Status | Notes |
|---|---|---|
| Unity Editor 6000.6.0f1 | running, PID 91620 | project `/Users/siddharth/Idle RPG` |
| Unity Pipeline server | **ready** on 127.0.0.1:7800 | `com.unity.pipeline 0.7.0-exp.1` |
| Unity MCP (stdio) | **LIVE** | `/Users/siddharth/.unity/bin/unity mcp --project-path "/Users/siddharth/Idle RPG"` |
| Cline MCP entry | configured | `cline_mcp_settings.json` → server name `unity` (stale `unityMCP` @ :8080 removed) |
| CLI fallback | works | `unity command <name> --json` (same catalog as MCP tools) |
| Git remote | `origin` added | no commits yet on `main` |

**If MCP ever drops:** re-add the `unity` stdio server in Cline → MCP Servers, or use CLI:
`/Users/siddharth/.unity/bin/unity command editor_status --json`

**Rule reminders (.clinerules):** caveman terse; one small change per step then STOP for manual Unity test; no `GameObject.Find`/`GetComponent` in `Update()`; `[SerializeField] private`; Editor code only under `Editor/`; trigger recompile after every .cs change (`unity-mcp__recompile` or CLI).

---

## 1. Spec Decisions (locked unless changed)

| # | Gap in spec | Decision |
|---|---|---|
| 1 | DEF unused in damage | `dmg = Max(atk − def, atk × minDamageRatio)`; `minDamageRatio = 0.1` in `BalanceConfig` |
| 2 | EnemyData lacks interval/DEF | add `attackIntervalSec` (def 2.0), boss `hpMultiplier`/`goldMultiplier`, `enemyDefense` |
| 3 | "3 heroes" party undefined | `PartyConfig` SO = 3 `HeroData` + lane anchors |
| 4 | Wave composition undefined | `WaveConfig` SO: 10 normal waves from enemy pool → 1 boss wave |
| 5 | "party wipe" undefined | heroes have HP; all 3 dead → `DefeatState` |
| 6 | Gems source undefined | +1 gem per boss kill |
| 7 | Number type | `double` + `NumberFormatter` (K/M/B/T/aa…). NOT BigInteger (JsonUtility-safe) |
| 8 | Hardcoded exponents | all in `BalanceConfig` SO: 1.15 HP, 1.12 gold, 1.07 cost, 1.5 token, 0.7 offline, 28800s cap |
| 9 | Offline gold/sec undefined | rolling `lastGoldPerSecond` in save (60s window) + formula fallback |
| 10 | Ascension reset scope | reset Gold + Stage→1 + hero levels; keep Gems/Tokens/permanent upgrades |
| 11 | Auto-retry flag | `autoRetryEnabled`; `DefeatState` sets false; manual Retry resumes at stage−1 |

Formulas (in `FormulaUtility`):
- `EnemyHP = BaseHP × 1.15^(stage−1)`
- `EnemyGold = BaseGold × 1.12^(stage−1)`
- `UpgradeCost = BaseCost × 1.07^level`
- `PrestigeTokens = floor((HighestStage / 10)^1.5)`
- `OfflineGold = sec × goldPerSecAtMaxStage × 0.7`, sec capped 28 800

---
## 2. Architecture & Folder Layout

```
Assets/IdleRPG/
  Scripts/
    Core/        GameManager, GameState, GameEvents, GameClock, GameBootstrap
    Data/        HeroData, EnemyData, WaveConfig, BalanceConfig, PartyConfig,
                 StatUpgradeData, PrestigeUpgradeData
    Economy/     CurrencyType, EconomyManager, BoostManager, GoldRewardCalculator
    Combat/      CombatSimulator (pure C#), CombatManager, HeroUnit, EnemyUnit,
                 HpBar, FloatingDamageText
    Progression/ FormulaUtility, UpgradeManager, AscensionManager, StatResolver
    Save/        SaveData, SaveSystem, OfflineProgressManager
    UI/          HeaderUI, TabController, UpgradePanelUI, AscensionPanelUI,
                 ShopPanelUI, OfflineRewardsPopup, SafeAreaFitter
    Services/    IAdService, MockAdService
    Utils/       NumberFormatter
  Data/          Heroes/, Enemies/, Config/   (generated SO assets)
  Prefabs/       HeroUnit, EnemyUnit, FloatingDamageText, panels
  Scenes/        Main.unity (built by MvpSceneBuilder; SampleScene kept as backup)
  Art/Placeholder/ procedural PNG sprites
  Editor/        MvpSceneBuilder, DataAssetGenerator, PlaceholderSpriteGenerator
```

Asmdefs: `IdleRPG.Runtime` (refs `UnityEngine.UI`, `Unity.InputSystem`), `IdleRPG.Editor` (refs Runtime).

Design rules:
- Pure-C# `CombatSimulator` -> live combat, offline estimate, fast-forward reuse one code path.
- `GameEvents` = static `System.Action` bus, reset via `[RuntimeInitializeOnLoadMethod]`.
- UI binds to events only; no polling in `Update()`.
- Everything tunable lives in ScriptableObjects; new hero/upgrade = new asset, no code.
- `MvpSceneBuilder` is idempotent -> re-run to regenerate scene/prefabs/SO assets.

---

## 3. Phase Checklist

### Step 0 — Skeleton + hygiene  `[DONE 2026-09-19]`
- [x] `git remote add origin`
- [x] folder tree created (17 folders under `Assets/IdleRPG/`)
- [x] `IdleRPG.Runtime.asmdef`, `IdleRPG.Editor.asmdef`
- [x] PlayerSettings: portrait lock (auto-rotate all off), iOS iPhoneAndiPad, Android ARM64
- [x] recompile clean (0 errors; only "asmdef has no scripts yet" warnings)
- [x] commit

### Step 1 — Phase 1 data + economy  `[DONE 2026-09-19]`
- [x] `HeroData.cs`, `EnemyData.cs`, `BalanceConfig.cs`, `WaveConfig.cs`, `PartyConfig.cs`
- [x] `StatUpgradeData.cs` (+ `HeroStatType`), `PrestigeUpgradeData.cs` (+ `PrestigeEffectType`)
- [x] `FormulaUtility.cs`, `NumberFormatter.cs`
- [x] `CurrencyType.cs`, `EconomyManager.cs`, `GameEvents.cs`, `GameEventPayloads.cs`, `GameState.cs`
- [x] `SaveData.cs`, `OfflineRewardResult.cs`
- [x] recompile clean (0 errors) — formula sanity check logged:
      hp10=175.89, gold10=27.73, cost5=14.03, bulk10=138.16, tokens(100)=31, offline=2,419,200, dmg(100,95)=10
- [x] commit
- **Note:** SO *assets* (Hero/Enemy/Config) are generated in Step 4 by `DataAssetGenerator`; this step is code only.

### Step 2 — Combat core (Phase 2)  `[DONE 2026-09-19]`
Headless logic only (views moved to Step 4, matching Phase 2 = "debug logs").
- [x] `CombatScaling.cs`, `EnemyTargetingMode.cs`
- [x] `ICombatStatProvider.cs` + `DefaultStatProvider.cs` (Step 3 swaps in upgrades/prestige)
- [x] `HeroCombatant.cs`, `EnemyCombatant.cs`, `CombatRewardCalculator.cs` (pure runtime state)
- [x] `CombatSimulator.cs` (pure, seeded RNG, targeting, crits, events)
- [x] `CombatManager.cs` (fixed-step ticker coroutine, 10 waves + boss, mirrors to GameEvents)
- [x] `GameManager.cs` FSM: Boot / Combat / Boss / Defeat / Ascension + economy + stage progression
- [x] `CombatEventLogger.cs`, `CombatDebugSceneBuilder.cs` (editor menu tools)
- [x] **BOSS TIMER REMOVED** (user decision): defeat only via party wipe; `BossTimerTick` deleted
- [x] Crits ON: 5% chance, x2 damage (`IsCritical` in payload, logged as CRIT)
- [x] Balance review applied + verified (sections 6 and 7) -> commit

### Step 3 — Progression (Phase 3)  `[DONE 2026-09-19]`
- [x] `StatResolver.cs` — implements `ICombatStatProvider`; owns hero levels + prestige levels;
      `FillFromSave` / `WriteToSave` hooks ready for Step 5
- [x] `UpgradeManager.cs` — `TryUpgrade(hero, stat, levels)`, `TryUpgradeAll`, `MaxAffordableLevels`
      (closed-form: `n = log_r(1 + gold*(r-1)/(base*r^L))`, no purchase loops)
- [x] `AscensionManager.cs` — token yield, `TryAscend`, prestige tree purchases, cost queries
- [x] `BoostManager.cs` (2x gold 1h, ad-extendable, expiry persisted), `GameClock.cs` (single time source)
- [x] `IAdService.cs` + `MockAdService.cs` (3s simulated ad, SDK-ready)
- [x] `DebugHotkeys.cs` (Play-mode keys 1/2/3/4/G/T/B/A/R/S/L) + `ProgressionDebugMenu.cs`
      (Tools > Idle RPG > Debug: balance summary, grants, jump to stage 10, live state)
- [x] `GameManager` wiring: resolver injected into `CombatManager.SetStatProvider`, `ResolveGoldReward`
      (prestige x boost, single funnel), 1s slow tick for boost expiry, `WatchAdForGoldBoost`
- [x] Verified headless + live (see section 8) -> commit

### Step 4 — UI + Scene (Phase 4)  `[DONE 2026-09-19]`
- [x] `TmpBootstrapper` — auto-imports TMP Essential Resources via `AssetDatabase.ImportPackage`
- [x] `PlaceholderSpriteGenerator` — 19 procedural PNGs (7 units, 7 panels, 4 icons, 1 background)
- [x] `DataAssetGenerator` — 16 SO assets + assigns `heroIcon` / `enemySprite` from the art folder
- [x] 14 UI view scripts: SafeAreaFitter, HudHeaderUI, TabController, UpgradePanelUI,
      HeroUpgradeRowUI, AscensionPanelUI, PrestigeUpgradeRowUI, ShopPanelUI, OfflineRewardsPopup,
      ToastUI, HpBarView, HeroUnitView, EnemyUnitView, FloatingDamageTextView/Pool, HudController
- [x] `UiFactory` + `SceneWiringUtility` + `MvpSceneBuilder` (menu) — builds `Main.unity`:
      Canvas 1080x1920 match 0.5, SafeArea, Header (gold/gems/tokens/stage/yield/boost chip),
      Viewport (3 hero lanes + enemy slot + HP bars), Dock (3 tabs + panels), DamageCanvas
      (separate overlay canvas + pooled numbers), OfflineRewardsPopup, Toast, EventSystem
- [x] verified live: correct sprites/tints, HP labels, tabs, pools, 0 console errors
- **Deviation:** UI is built directly into the scene (no prefabs yet). Prefabs are the planned
  follow-up if hand-editing is preferred over regenerating the scene.
- **Note:** the Game View must be portrait (1080x1920) to judge layout; the default 826x422
  landscape view makes the portrait canvas look tiny.

### Step 4b — Combat log + scrolling  `[DONE 2026-09-19]`
- [x] `EnemyDamagedInfo.AttackerIndex` (simulator fills the hero lane) so lines can name the attacker
- [x] `CombatLogUI` — 60-line ring buffer, pooled TMP labels, same-attacker aggregation
      (`Mage hits Ogre for 24 x3`), 6 lines/sec budget with `... N more hits` overflow summary,
      auto-scroll to bottom, colours per line type
- [x] `UiFactory.CreateScrollView` — ScrollRect + RectMask2D + LayoutGroup (+ optional ContentSizeFitter)
- [x] layout bands: header 88-100%, viewport 44-88%, combat log 20-44%, dock 0-20%
- [x] verified headlessly (player loop frozen while the Editor is unfocused -> drove
      `CombatSimulator.Step` + `CombatLogUI.Update` via reflection)
- [ ] **user test**: focus the Editor, Play, read the feed under the battle screen

### Step 4c — Page split + full scrolling  `[DONE 2026-09-19]`
- [x] `ScreenController` — BattlePage / ManagementPage + 4-button nav bar (BATTLE / UPGRADES / ASCEND / SHOP)
      with active highlight; nav jumps straight to a tab; combat keeps running while browsing
- [x] page shell: header 88-100% | pages 12-88% | nav bar 0-12%
- [x] battle page: viewport 38-100% of the page, combat log 0-38%
- [x] management page: tab bar 90-100%, panels 0-90%
- [x] `ScrollRect` + `RectMask2D` on the upgrades list, the prestige list and the combat log
      (uGUI mouse-wheel + drag both work; EventSystem already present)
- [x] verified: page switching, 4 nav buttons, 3 scroll views, live attack intervals x1.6
- [ ] **user test**: focus the Editor, Play, switch pages, scroll the lists

### Step 5 — Persistence (Phase 5)  `[PENDING]`
- [ ] `SaveSystem.cs` (JsonUtility, version, atomic write, persistentDataPath/savegame.json)
- [ ] `OfflineProgressManager.cs` (PlayerPrefs binary ts, 28800s cap, x0.7)
- [ ] autosave (interval + pause/quit), offline popup claim flow
- [ ] -> **user tests quit/relaunch** -> commit

### Step 6 — Mobile polish  `[PENDING]`
- [ ] `SafeAreaFitter` for notches, iOS/Android build settings sanity
- [ ] README, final `git push -u origin main`

---

## 6. Balance Review (Step 2, user-approved)

| Item | Before | Now | Reason |
|---|---|---|---|
| Hero DPS | 8 / 15 / 12 | **8 / 8 / 8** | one shared upgrade cost curve -> all 3 viable |
| Knight | HP 200 DEF 10 | HP 240 DEF 12 | front-lane tank |
| Archer | HP 120 ATK 18 @1.2s | HP 110 ATK 8 @1.0s | equal DPS, fast cadence |
| Mage | HP 100 ATK 24 @2.0s | HP 130 ATK 16 @2.0s | equal DPS, slow cadence |
| Boss (stage 1) | 600 x10 = 6000 HP (~171s) | 120 x5 = **600 HP (~45s)** | was unfightable |
| Boss gold | 60 x5 = 300 | 25 x6 = **150** | ~half a stage income, not a jackpot |
| Stage-1 income | ~142 | **~292/stage** | funds ~20 ATK levels after stage 1 |
| Enemy DEF scaling | n/a | **off** | 1.08^S DEF vs 0.15 floor -> late DPS collapse |
| minDamageRatio | 0.10 | **0.15** | floor less punishing |
| Hero healing | full revive every wave | **damage carries across the 10 waves; heal on stage advance** | only defeat source now that timers are gone |
| Crit | off | **5% x2.0** (~+5% DPS) | visible in damage text |
| Wave transition | 0.50s | **0.35s** | snappier |
| Boss (stage 1) v2 | 120 x5 = 600 HP (~45s) | **100 x5 = 500 HP (~35s)** | tighter first boss |
| DEF upgrade gain | +10% of base/level | **+15%** | additive DEF was negligible vs 1.08^S enemy ATK |
| Prestige effect | +5%/level (x1.50 max) | **+10%/level (x2.00 max)** | +50% was a weak payoff for ~30 ascensions |
| Prestige cost growth | 1.5 (~113 tokens/tree) | **1.4 (~70 tokens/tree)** | a tree must be reachable |
| Ascension reset | spec: gold + stage only | **also resets hero levels** (`resetHeroLevelsOnAscension` knob) | without it, re-climbing at kept levels = infinite token farm |

**Known weak spot for Step 3:** DEF upgrades are minor (Knight +1.2 DEF/level vs enemy ATK x1.08/stage) -> tune DEF `statGainPerLevelFraction` or add a damage-reduction curve.

---

## 7. Step 2 Verification Results

Headless deterministic run (seed 12345, `DefaultStatProvider`, no upgrades/heals):
`wiped=True at stage=2 wave=11 t=185.3s | kills=21 gold=413 crits=22 | stage1ClearTime=87.8s`
- Stage-1 clear **87.8s** vs 60-90s target -> on target (hand-calc predicted 89s).
- Crit rate 22/~355 hits = ~6% (expected 5%) -> OK.
- Wipe triggers correctly when out-scaled.

Live play test (CombatDebug scene, temporary waves=1 / boss x1 for speed):
```
[State] Combat -> Boss
[CombatManager] Stage 1 | Wave 2/2 (BOSS) -> Ogre Chieftain HP 120 ATK 20 gold 150
[CombatManager] Ogre Chieftain killed -> +150 gold
[State] Boss -> Combat
[CombatManager] Stage 2 | Wave 1/2 -> Bat HP 103 ATK 10 gold 13
[GameManager] Stage 1 complete -> now stage 2 | [Economy] Gold=158 Gems=1 Tokens=0
```
- Wave FSM, boss wave, stage advance, heal-on-advance, gems +1, gold math, stage scaling
  (Bat 90->103 = x1.15, ATK 9->10 = x1.08, gold 12->13 = x1.12) confirmed.

---

## 8. Step 3 Verification Results

Headless (`StatResolver` + `UpgradeManager` + `AscensionManager` + `BoostManager` with real assets):
```
R1 gains ATK=0.1 HP=0.1 DEF=0.15 | resetHeroLevels=True | cost10=138.16
R2 buy10=True level=10 gold=861.84 knightAtk=24.0            (12 x 2.0)
R3 knightDef=30.0                                             (12 + 10 x 15%)
R4 ascend tokens=1 gold=0 heroLevels=0                        (prestige levels kept)
R5 dmg buy=True cost=1.40 dmgMul=1.10 knightAtk=13.2         (12 x 1.10)
R6 goldPrestige levels=10 goldMul=2.00 nextCost=28.93 maxed=True
```
Live in the debug scene (bugs found and fixed by these checks):
```
N1 atkBought=3 knightAtkInSim=24.0 mageAtkInSim=32.0
N2 hpBought=3 knightMaxHpInSim=360 currentHp=360 healthPctPreserved=True
N3 defBought=3 knightDefInSim=21.0
N4 dmgMul=1.10 knightAtkInSim=26.4
N5 boost x2 resolveReward(100)=200
L3 ascend=True tokens=1 gold=0 stage=1 best=10 yield=1
```
Two real bugs caught and fixed:
1. `StatResolver.SetHeroLevel` never raised `StatsChanged` -> combat never refreshed after a purchase.
2. `CombatSimulator` kept the provider reference from construction, so `SetStatProvider` updated only
   the manager -> sim read base stats forever. The simulator now owns a swappable provider.

---

## 9. Step 4 Verification Results

Built via `Tools > Idle RPG > Build MVP Scene`; hierarchy verified through the MCP bridge
(GameManager + HUD/Canvas/SafeArea + Header/Viewport/Dock + DamageCanvas + EventSystem).
Runtime dump during Play (all values read from the live scene):
```
[UI1] hud.gameManager=True state=Combat stage=1
  header GoldChip/Value='0'  GemChip/Value='0'  TokenChip/Value='0'
  StageLabel='Stage 1  wave 1'   YieldLabel='Ascend: 0'
[UI2] tabs activeIndex=0 | UpgradesPanel:active=True AscensionPanel:False ShopPanel:False
[UI5] damage pool prewarmed children=8
[V1] enemy sprite=enemy_slime enabled=True
[V2] HeroLane0 sprite=hero_knight hpFill=1.00 label='240 / 240'
[V2] HeroLane1 sprite=hero_archer hpFill=1.00 label='110 / 110'
[V2] HeroLane2 sprite=hero_mage  hpFill=1.00 label='130 / 130'
[V3] sim enemy=Slime hp=60/60 hero0atk=12
```
Bug found and fixed: `DataAssetGenerator` never assigned `heroIcon`/`enemySprite`, so the units
rendered as untextured tinted boxes -> the generator now assigns art by asset-name convention.

---

## 10. Step 4b Verification Results

The Editor throttles the player loop while unfocused (`frameCount=2` after 15s), so the log was
verified headlessly by driving the simulation and the view directly:
```
[L] lines=10 height=372 enemyHp=0
   ... 1 more hit
   Archer hits Slime for 8
   Knight hits Slime for 12
   Mage hits Slime for 16
   Slime hits Knight for 0.9  (239/240)
   Archer hits Slime for 8
   Knight hits Slime for 12
   Archer hits Slime for 8
   Slime defeated  +8 gold
   Stage 1 wave 1 cleared
```
Bugs found and fixed on the way:
1. **`MvpSceneBuilder` loaded the data assets before `EditorSceneManager.NewScene`** -> the reimport
   in the same tick invalidated them and the scene saved with an **unwired GameManager**
   (`[SceneWiringUtility] GameManager wiring verification failed`). Loads now happen after the scene
   swap and the build aborts (without overwriting the scene) if wiring fails.
2. `ContentSizeFitter` on the log content reported height 12 (extra layout pass needed) -> the log
   computes its content height explicitly (`count * (lineHeight + spacing) + padding`).
3. `ShopPanelUI.Refresh` threw when `Economy` was null (unwired build) -> now guards.

---

## 11. Pacing & Balance Knobs (single source of truth)

**`BalanceConfig.combatPaceMultiplier` is THE game-pace knob.** It multiplies every unit's attack
interval (heroes *and* enemies), so it stretches wall-clock time while leaving every ratio intact:

| Untouched by pace | Stretched by pace |
|---|---|
| damage per hit, HP, DEF mitigation, crit rate | attacks per second |
| gold per kill, upgrade costs, token yield | seconds per wave / stage / boss |
| healing, defeat pressure (damage taken per kill) | how fast the log scrolls |

So difficulty tuning never needs a re-rebalance: change enemy HP/gold/attack growth for difficulty,
change `combatPaceMultiplier` for feel. Shipped value: **1.6** (was effectively 1.0).

**Watch it with `Tools > Idle RPG > Debug > Log Balance Summary`** (now prints a Pace block):
```
-- Pace (knob: BalanceConfig.combatPaceMultiplier) --
  pace x1.6 -> attacks per hero are 60% slower | crit factor x1.05
  Slime                 60 HP  TTK    3.8s  x3.3 waves
  Bat                   90 HP  TTK    6.3s  x3.3 waves
  Goblin               130 HP  TTK   11.3s  x3.3 waves
  Ogre Chieftain       400 HP  TTK   46.3s  (boss)
  full stage estimate: 124s (2.1 min)
  gold/stage 276 -> 2.2 gold/s -> first ATK level every 4.5s
```
Other knobs that shape pacing (all in `BalanceConfig`): `normalWavesPerStage` (10),
`waveTransitionDelaySec` (0.6), `enemyHealthGrowth` 1.15, `enemyGoldGrowth` 1.12,
`enemyAttackGrowth` 1.08, `upgradeCostGrowth` 1.07, `minDamageRatio` 0.15.
Boss multipliers live on `Boss_Ogre` (`BossHealthMultiplier` 4, `BossGoldMultiplier` 6).

---

## 14. UI Fixes Round 2 (2026-09-19)

Reported: duplicate navigation, damage numbers on non-battle pages, and dead buttons on the new pages.

1. **Duplicate navigation removed.** The management page had its own tab bar (Upgrades/Ascension/Shop)
   *plus* the bottom nav bar. The bottom bar is now the single navigation; `TabController` keeps only
   its panel-toggling job (definitions are created with no button). Verified `tabBarStillPresent=False`.
2. **Damage numbers no longer appear off the battle page.** `ScreenController.hideWhileBrowsing[]`
   deactivates the DamageCanvas while browsing (the pool also stops ticking, so nothing spawns).
   Verified `damageCanvasActive=False` while browsing, `True` back on the battle page.
3. **Buttons on the new pages were dead.** The panels live on a page that starts hidden, so their
   `Start()` ran only on first activation and their guard *disabled the component* when the manager was
   not ready yet; row listeners were also added in `Awake` (first activation only). Now every panel
   binds lazily through `EnsureBound()` (called from `Start` **and** `OnEnable`) and every button
   listener is wired exactly once inside `Configure()`/`EnsureBound()` after `RemoveAllListeners()`.
   Verified with real pointer clicks through the EventSystem:
```
[X2] atkLevel before=0 afterInvoke=1 afterRealPointerClick=2 gold=49979
[Z2] watchAd clicked  -> [MockAdService] Simulating a 3s rewarded ad...
[Z4] prestige buy -> level=1 tokensLeft=19
[A2] first tap -> label='CONFIRM?' tokens=19
[A3] second tap -> tokens=20 stage=1 best=10 gold=0 heroLevels=0 label='ASCEND'
```
Lesson for future screens: **anything living on a page that starts inactive must bind lazily and must
never disable itself when a dependency is late.**

---

## 13. Combat Log + Navigation Fixes (2026-09-19)

Reported by the user: the log did not visibly update and pages could not be changed by clicking.

**1. Log follow (newest line was never reached).** The old code treated *content growth* as the player
scrolling up, so auto-follow switched itself off (`contentY=4` with `overflow=1318`). Rewritten to
compare the scroll position against the position **we** last wrote; only a foreign move counts as user
input. Verified:
```
[Q1] contentH=1500 overflow=958 contentY=958 distanceFromNewest=0.0 pinned=True
[Q2] after one more line: contentY=994 distanceFromNewest=0.0 pinned=True
[Q3] newest child='NEWEST LINE'
```
Also: `smoothFollow` (lerp) instead of snapping, auto-follow pauses when the player scrolls up and
re-engages within `reengageDistance` px of the bottom, and the strip shows ~15 lines at 1080x1920
(viewport 542 px) so with the new pace the feed is calm (~1 line/second).

**2. Navigation was dead to the mouse** (see the *Watch items* entry on the input asset). Code path was
fine all along: `[G0..G3] invoke ok` for all four nav buttons. After persisting the input action asset,
real clicks reach the UI. Verified page switching: `ShowManagement(1)` -> battle off/management on,
`ShowBattle()` -> back. New `Tab` key cycles pages for keyboard testing.

---

## 12. Step 4c Verification Results
```
[S1] rows=3 upgradeContent=748 | prestigeRows=3 prestigeContent=372 | navButtons=4
[S2] after ShowManagement(1): battleActive=False managementActive=True isManagementOpen=True
[S3] after ShowBattle(): battleActive=True managementActive=False
[T1] pace=1.60 | Knight interval=2.40 | Archer interval=1.60 | Mage interval=3.20 | enemy interval=3.20
[T2] battlePageActive=True managementVisible=False scrollViews=3
```
Scene contains `BattlePage`, `ManagementPage`, `NavBar`, `UpgradeScroll`, `PrestigeScroll`, `CombatLog`.
Scroll content heights exceed their viewports, so both lists scroll (and fit entirely on a 1080x1920
portrait screen, which is why the old fixed dock felt broken only in landscape).

---

## 4. Open Risks / Watch Items
- Unity **6000.6.0f1** (not 2022.3 LTS) — APIs used are version-stable.
- New Input System only (`activeInputHandler = 1`) -> EventSystem needs `InputSystemUIInputModule`.
- `.clinerules` forbids automated tests -> no EditMode tests for formulas unless user asks.
- `SampleScene` untouched until `Main.unity` proven; then swap build settings scene.
- `.meta` files ARE tracked by git (verified) — must commit them.
- **MCP: Editor throttles the player loop when unfocused** (20 wall-seconds produced `frameCount=2`).
  Focus the Editor or verify headlessly via `eval`; never trust a Play test on a minimised Editor.
- **MCP: `SerializedObject` asset writes during Play mode PERSIST** (not reverted on stop).
  Re-run `Tools > Idle RPG > Generate Data Assets` after such a test.
- **MCP: asset refs loaded before `EditorSceneManager.NewScene` go stale** if a reimport lands in the
  same tick -> load data assets *after* the scene swap (the builder does this now).
- Editor **menu items** are the reliable way to run authoring code; `eval` runs in a throwaway assembly
  and can hit the 5s main-thread timeout on long operations.
- **UI clicks die if the EventSystem's input action asset is not a real asset.** `AssignDefaultActions()`
  called from editor tooling produces an *in-memory* `InputActionAsset`; serialized into the scene it
  becomes a dangling reference (`actionsAsset == null` while the individual action refs are non-null).
  Symptom: navigation/every button ignores the mouse, while `button.onClick.Invoke()` still works.
  Fixed twice over: the builder persists it to `Assets/IdleRPG/Settings/UiInputActions.inputactions`,
  and `UiInputBootstrap` re-creates the default actions at runtime if they are ever missing.
- **`Object.FindAnyObjectByType` skips inactive objects.** The management page is inactive while the
  battle page is shown, so its components (e.g. `TabController`) are invisible to that lookup —
  use `FindObjectsByType(..., FindObjectsInactive.Include, ...)` in probes/tools.
- **Driving play-mode logic from `eval` while the Editor is unfocused**: call `CombatSimulator.Step(dt)`
  and invoke private `Update()` via reflection. Note `Time.deltaTime` is stale then, so rate-limited
  systems (like the log budget) must have their budget field set directly for bulk tests.

## 5. Change Log
- **Step 0** (2026-09-19): MCP bridge fixed (stdio `unity` server; stale `unityMCP` @ :8080 removed), `origin` remote added, `Plan.md` created, folder tree in place, both asmdefs added, portrait lock applied, recompile clean.
- Note: `set_player_settings` MCP command only exposes companyName/productName/bundleVersion/scriptingBackend/apiCompatibilityLevel → orientation set via `eval` + `PlayerSettings` API instead.
- **Step 1** (2026-09-19): Phase 1 code complete (16 scripts). Verified formulas via single `eval` sanity log. Compile clean.
  - `MCP note`: large file writes via the `editor` tool can time out; keep each edit under ~6000 chars and append in chunks.
  - `MCP note`: `recompile` may report `up_to_date` after filesystem writes → call `AssetDatabase.Refresh()` + `CompilationPipeline.RequestScriptCompilation()` via `eval`, then poll `recompile_status`.
  - `MCP note`: `recompile`/`eval` must not be issued in the same response as the file edit that they depend on.


- **Step 2** (2026-09-19): Combat core landed (12 new files) + reviewed balance written through
  `DataAssetGenerator.ApplyBalance`. Boss timer removed per user decision; crits enabled (5% x2).
  Verified headless (87.8s stage-1 clear, wipe path) and live in the Editor (boss -> stage advance -> gems).

- **Step 3** (2026-09-19): Progression landed (9 new files). Balance v2 applied through
  `DataAssetGenerator.ApplyBalance` (DEF +15%/level, prestige +10%/level @1.4 growth, boss 500 HP,
  ascension also resets hero levels). Verified headless + live; two wiring bugs found and fixed.

- **Step 4** (2026-09-19): UI + scene landed (TMP bootstrap, 19 placeholder sprites, 14 UI scripts,
  UiFactory/SceneWiringUtility/MvpSceneBuilder). `Main.unity` generated and verified live.
  Fixed: unit sprites were unassigned in the data assets. Deprecation warnings cleaned up
  (`FindAnyObjectByType`).

- **Step 4b** (2026-09-19): Combat log landed (`CombatLogUI`, `AttackerIndex` payload field,
  `UiFactory.CreateScrollView`). Scene builder ordering bug fixed (stale asset refs -> unwired scene).
  Log verified headlessly with real formatted output.

- **Step 4c + pacing** (2026-09-19): Two-page shell (`ScreenController` + nav bar), ScrollRect/RectMask2D
  on upgrades, prestige and the combat log. New single-knob game pace `combatPaceMultiplier` = 1.6
  (stage 124s vs 88s before), boss trimmed x5 -> x4, transition 0.6s, log rate 3/s. Balance summary
  tool now prints per-enemy TTK, full-stage estimate and gold/sec so pacing stays measurable.

- **Log + nav fixes** (2026-09-19): Combat log now pins to the newest line (smooth follow, pauses while
  the player reads history); nav-bar clicks fixed by persisting the EventSystem's UI action asset
  (`Assets/IdleRPG/Settings/UiInputActions.inputactions`) plus a runtime `UiInputBootstrap` safety net;
  `Tab` cycles pages.

- **UI fixes round 2** (2026-09-19): single navigation (removed the management tab bar), damage numbers
  hidden while browsing, and panel buttons fixed via lazy binding + one-time listener wiring
  (upgrade +1/+10, watch ad, prestige buy and the two-tap ascend all verified with real pointer clicks).
