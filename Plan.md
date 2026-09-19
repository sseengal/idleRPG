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

### Step 1 — Phase 1 data + economy  `[PENDING]`
- [ ] `HeroData.cs`, `EnemyData.cs`, `BalanceConfig.cs`, `WaveConfig.cs`, `PartyConfig.cs`
- [ ] `StatUpgradeData.cs`, `PrestigeUpgradeData.cs`
- [ ] `FormulaUtility.cs`, `NumberFormatter.cs`
- [ ] `CurrencyType.cs`, `EconomyManager.cs`, `GameEvents.cs`, `SaveData.cs`
- [ ] recompile clean -> **user tests in Inspector** -> commit

### Step 2 — Combat (Phase 2)  `[PENDING]`
- [ ] `CombatSimulator.cs` (pure, no MonoBehaviour)
- [ ] `HeroUnit.cs`, `EnemyUnit.cs`, `HpBar.cs`
- [ ] `CombatManager.cs` (10 waves + boss, ticker coroutine, events)
- [ ] `GameManager.cs` FSM: CombatState / BossState / DefeatState / AscensionState
- [ ] debug-log auto battle, 30s boss timer, defeat -> stage-1 + auto-retry off
- [ ] -> **user plays empty scene, reads Console** -> commit

### Step 3 — Progression (Phase 3)  `[PENDING]`
- [ ] `StatResolver.cs` (base + levels + prestige -> final stats)
- [ ] `UpgradeManager.cs` (+1 / +10 ATK/HP/DEF, cost check via EconomyManager)
- [ ] `AscensionManager.cs` (+3 permanent upgrades: %Gold, %Damage, %HP)
- [ ] `BoostManager.cs` (2x gold 1h, persists), `IAdService`/`MockAdService`
- [ ] -> **user tests** -> commit

### Step 4 — UI + Scene (Phase 4)  `[PENDING]`
- [ ] `PlaceholderSpriteGenerator` (procedural PNG hero/enemy/boss/icon)
- [ ] `DataAssetGenerator` (SO assets for heroes/enemies/config)
- [ ] `MvpSceneBuilder` (menu: Tools/Idle RPG/Build MVP Scene)
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

## 4. Open Risks / Watch Items
- Unity **6000.6.0f1** (not 2022.3 LTS) — APIs used are version-stable.
- New Input System only (`activeInputHandler = 1`) -> EventSystem needs `InputSystemUIInputModule`.
- `.clinerules` forbids automated tests -> no EditMode tests for formulas unless user asks.
- `SampleScene` untouched until `Main.unity` proven; then swap build settings scene.
- `.meta` files ARE tracked by git (verified) — must commit them.

## 5. Change Log
- **Step 0** (2026-09-19): MCP bridge fixed (stdio `unity` server; stale `unityMCP` @ :8080 removed), `origin` remote added, `Plan.md` created, folder tree in place, both asmdefs added, portrait lock applied, recompile clean.
- Note: `set_player_settings` MCP command only exposes companyName/productName/bundleVersion/scriptingBackend/apiCompatibilityLevel → orientation set via `eval` + `PlayerSettings` API instead.

