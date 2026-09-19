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

### Step 3 — Progression (Phase 3)  `[PENDING]`
- [ ] `StatResolver.cs` (base + levels + prestige -> final stats)
- [ ] `UpgradeManager.cs` (+1 / +10 ATK/HP/DEF, cost check via EconomyManager)
- [ ] `AscensionManager.cs` (+3 permanent upgrades: %Gold, %Damage, %HP)
- [ ] `BoostManager.cs` (2x gold 1h, persists), `IAdService`/`MockAdService`
- [ ] -> **user tests** -> commit

### Step 4 — UI + Scene (Phase 4)  `[IN PROGRESS]`
- [ ] `PlaceholderSpriteGenerator` (procedural PNG hero/enemy/boss/icon)
- [x] `DataAssetGenerator` (`Tools > Idle RPG > Generate Data Assets`) — idempotent; 16 SO assets generated
      (BalanceConfig, WaveConfig, PartyConfig, 3 StatUpgrade, 3 PrestigeUpgrade, 3 HeroData, 4 EnemyData incl. Boss_Ogre)
- [ ] `MvpSceneBuilder` (menu: Tools/Idle RPG/Build MVP Scene)
- [ ] Canvas 1080x1920, Match 0.5; header (gold/gems/tokens/stage+wave)
- [ ] viewport: 3 heroes left, enemy right, HP bars, floating damage text (pooled)
- [ ] tabs: Upgrades / Ascension / Shop
- [ ] `OfflineRewardsPopup`
- [ ] -> **user plays + eyeballs layout** -> commit
- [ ] Canvas 1080x1920, Match 0.5; header (gold/gems/tokens/stage+wave)
- [ ] viewport: 3 heroes left, enemy right, HP bars, floating damage text (pooled)
- [ ] tabs: Upgrades / Ascension / Shop
- [ ] `OfflineRewardsPopup`
- [ ] -> **user plays + eyeballs layout** -> commit

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
