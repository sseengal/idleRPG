# Run-Loop — the automated battler (CANON for the core loop)

Status: **CANON for the core loop** — 7 Nov 2026. This page **supersedes `Game-Vision.md` as the spine** of the
game. `Game-Vision.md` described a short story campaign with a fixed ending; that is now the *flavor wrapper*, not the
loop. Story canon (`Story-Canon.md`, `Story-Greenwake.md`, `Story-Nodes.md`, `Story-Scenes.md`) stays valid as the
**binder** that gives the run a reason, but it is no longer a linear campaign that gates content.

Genre anchor: **Soda Dungeon** — auto-fight floors, spend gold on upgrades during the run, die, restart stronger with
permanent upgrades. This is an **automated battler with run-based meta-progression**, not an idle game.

---

## 1. The one-line change

The game is an **automated battler with stage progression**. The player does not steer the fight. The player spends
the gold they earn **this run** on upgrades, pushes as many stages as they can, and **is expected to die**. On death
the run ends, a small permanent reward is banked, and the next run is faster. Over many runs, permanent upgrades move
how deep the player gets.

Plain words: fight plays itself. Spend gold now. Die. Keep a little forever. Go again, get further.

---

## 2. The loop

```
RUN START (stage 1, power = permanent upgrades only)
   |
   v
auto-battle waves -> stage boss -> next stage        (gold paid per kill)
   |
   v
spend RUN GOLD on RUN upgrades (this run only)
   |
   v
push deeper  ------------------------------------------------.
   |                                                         |
   v                                                         |
WIPE  or  player taps "END RUN"                             |
   |  (run over)                                             |
   v                                                         |
RUN OVER summary (free): stage / gold / kills / meta         |
   |                                                         |
   +-- "REVIVE" (rewarded ad, ONCE/run) -> heal -> resume ---+
   |                                                         |
   +-- timer / "END" -> bank META -> reset run -> stage 1 ---'
```

A run is measured in **stages reached**, not story nodes.

---

## 3. Two currencies, two lifetimes

| Currency | Earned | Spent on | Survives death? | Internal type today |
|---|---|---|---|---|
| **Run Gold** | every kill, this run | **run upgrades** (this run only) | **No** — wiped on run end | `CurrencyType.Gold` (already reset by ascension) |
| **META (Shards)** | at run end, scaled by depth | **permanent upgrades** | **Yes** — always kept | `CurrencyType.PrestigeTokens` (keep the enum; display name "Shards") |
| Gems | milestones, IAP, streak | shortcuts, cosmetics, revive-adjacent | Yes | `CurrencyType.Gems` |

- Run Gold is already a run currency: the current ascension reset (**`GameManager.ResetProgressForAscension`**) wipes
  gold and hero levels. The pivot does not change that — it only changes *when* the reset fires (on death, not on a
  manual button).
- META reuses the existing prestige token layer (**`AscensionManager`**, **`PrestigeUpgradeData`**). No new save
  currency, no schema fork. Only the *trigger* and the *tree size* change.

---

## 4. Run end — the exact flow

The run ends on a **party wipe** or when the player taps **END RUN**. Both go through one path, `EndRun(reason)`.

```
WIPE  (the run ends)
  1. combat stops
  2. META = max(1, f(depth)) if >=1 stage cleared, else 0    (banked on NEXT RUN)
  3. RUN OVER card (FREE, stays put - NO auto-resume):
        stage reached · gold earned · kills · Perk Points banked
        [NEXT RUN] -> fresh run from stage 1
  4. [REVIVE] rewarded ad, ONCE per run -> resume the same stage
```

Rules to lock:

- **Summary is always free.** It never costs anything to see the run.
- **Revive is a rewarded ad, once per run.** Uses the existing ad stack: a new
  `AdPlacementId.RunRevive` row in `BalanceConfig.adPlacements`, enforced by `AdCapsService` (per-day cap + min gap).
- **Banking happens on death; there is no auto-resume.** The reward is granted when the run ends and the card then
  WAITS for **NEXT RUN**. No countdown, no automatic restart (the old idle "never waits for input" rule is retired).
- **End early is allowed.** A mid-run END RUN button banks the same way. It is the player's lever to stop at a good
  depth.
- The revive is the only interrupt. It is aspirational (a second chance), not pity gold, so it fits `Monetisation.md`
  (never sell raw power; limited; earnable-free elsewhere).

What changes in code (Phase B): **`GameManager.OnPartyWiped`** rolls back one stage today and resumes — it must call
`EndRun(RunOver)` instead. **`RequestAscension`**'s grant+reset body becomes the shared `EndRun`. **`DefeatModalUI`**
becomes the RUN OVER summary (add the REVIVE-ad button). **`GameManager.Stages.cs`** is the main file.

---

## 5. The two upgrade layers (the core product)

The upgrade mechanism is the game. Everything else is support. Two layers, two currencies.

> **Frozen v1, 7 Nov 2026.** Numbers are starting points; Phase D retunes on the Balance Lab robot. Costs use the
> code's own curve: `cost(level) = base * growth^level`.

### 5.1 Run upgrades — GOLD, wiped on death (this run only)

| Stat | Effect per level | Base cost | Growth | Cap | Status |
|---|---|---|---|---|---|
| ATK | +10% of base | 10 | 1.07 | none | built (`StatUpgradeData`) |
| HP | +10% of base | 10 | 1.07 | none | built |
| DEF | +10% of base | 10 | 1.07 | none | built |
| CRIT | +0.5 points | 25 | 1.10 | none | built |
| CRIT DMG | +0.1x | 25 | 1.10 | none | built |
| Attack Speed | +3% | 25 | 1.12 | 20 | new |
| Lifesteal | +1% | 40 | 1.15 | 15 | new |
| Gold Find | +5% | 20 | 1.10 | 20 | new |

Hero ATK/HP/DEF/CRIT/CRIT-DMG are the existing per-hero tracks (unchanged). The three new rows are global run buffs.
One new row = one data asset + one read in `StatResolver`.

### 5.2 Permanent upgrades — META (Shards), kept forever

| Upgrade | Effect per level | Base cost | Growth | Cap | Status |
|---|---|---|---|---|---|
| Damage % | +15% global damage | 1 | 1.5 | 20 | built (`PrestigeUpgradeData`) |
| HP % | +15% global HP | 1 | 1.5 | 20 | built |
| Gold % | +15% run gold | 1 | 1.5 | 20 | built |
| Auto-Equip / Auto-Salvage | utility unlock | 1 | - | 1 | built |
| Start Gold | +50 run gold at start | 2 | 1.8 | 10 | new |
| Start at Stage | +1 starting stage | 5 | 2.0 | 10 | new |
| Cost Discount | -3% run upgrade cost | 3 | 1.6 | 10 | new |
| Crit Chance | +1 point global crit | 2 | 1.5 | 10 | new |
| Extra Party Slot | +1 hero slot | 10 | - | 1 | new |
| Ability Tier | open next ability tier | 8 | 2.0 | 3 | new |

Each new permanent row = one `PrestigeEffectType` value + one read in `StatResolver` + one asset row. **One row per
build step** (do not batch).

### 5.3 Pick-1-of-3 stage offer (Phase C2)

After each stage clear, show 3 cards drawn from the run-buff pool; picking one applies it free for this run. Pool =
Attack Speed, Lifesteal, Gold Find, +ATK %, +HP %, heal-now, and later ability tiers. One skip/reroll allowed.

### 5.4 Revive-ad placement (`RunRevive`)

| Field | v1 value |
|---|---|
| Placement id | `AdPlacementId.RunRevive` (new) |
| Reward | resume the current run (heal party, same stage) |
| Once per run | yes (a save flag) |
| Per-day cap | 3 |
| Min gap | 300 s |

Enforced by `AdCapsService`; served by `MockAdService` / `IAdService`. No new ad code.

**Decision to make before coding C:** auto-buy. Because the game is "automated", decide whether leftover run gold
auto-buys a policy the player sets, or the player always taps. Recommendation: manual by default, with an auto-buy
card as a later META unlock (reuses the existing `AutomationService`).

---

## 6. Stage progression and the death curve

The wave -> boss machinery (`WaveConfig`, `CombatManager`, `StageWavePlan`) is kept. What changes is the **intent of
the numbers**: today they are tuned for an endless bounce; they must be tuned so death is an expected, fair event.

| Run | Target deepest stage | Why |
|---|---|---|
| 1 | ~15-25 | player learns the loop, dies, sees the meta reward |
| 2 | ~30 | first permanent buys are felt |
| 3-5 | ~40-60 | the tree compounds; "start at stage" kicks in |
| later | pushes a **soft ceiling** that rises slowly | long-term goal |

Tuning tool: the **Balance Lab robot** (`Tools > Idle RPG > Debug > Log Balance Summary`). The robot must be re-run on
the new run-end model, not the bounce model, or the numbers lie.

---

## 7. Story as binder (light), not spine

The story stops gating content. It gives the loop a reason. The canon already supplies the perfect hook: **dawn
shards pull the Guard back** (`Story-Canon.md` §4). That *is* a roguelite resurrection fantasy — a run is one descent,
death is the shard pulling you home, and permanent upgrades are what the shard remembers.

- Keep: `StoryLayer` for **flavor lines** — run-start line, a line at each new stage band, a rival boss intro, a
  run-over epitaph.
- Drop: the map, chapter select, and any scene that must play in order to unlock content.
- The rival/antagonist is the recurring face of "how deep did you get this time".

---

## 8. What happens to existing systems

| System | Keep | Change |
|---|---|---|
| `Sim/Encounter`, `CombatManager` | everything | nothing |
| `UpgradeManager` (gold -> hero stats) | yes | becomes the run layer; add rows |
| `AscensionManager` + `PrestigeUpgradeData` | yes | trigger on death; grow the tree |
| `GameManager.OnPartyWiped` | the modal, the wipe event | **rollback -> EndRun** |
| `GameManager.RequestAscension` | grant + reset body | becomes shared `EndRun`; no manual min-stage gate |
| `DefeatModalUI` | the card | becomes RUN OVER summary + REVIVE ad |
| `BalanceConfig` | all knobs | add `runEndsOnDeath`, `RunRevive` ad placement; retune |
| `StoryLayer` + story docs | the binder | campaign structure demoted |
| Idle / offline / camp / ledger | the code works | **out of the core loop**; optional side, Phase F |
| Gear / abilities / camp | work | **keep but not required** for the loop to be fun (off the critical path) |
| Zones / affixes / relics / expeditions | — | parked (v1.1) |

---

## 9. Naming

The theme already fits a resurrection loop. Candidates: **Dawnbound**, The Ascent, Wardens' Ascent,
Second Dawn: Descent, Eternal Watch. Recommendation: **Dawnbound** (short, ties to the dawn-shard hook,
store-friendly). Internal code, save keys and `CurrencyType` stay unchanged regardless of the display name.

---

## 10. Team view (who wins, who bends)

| Discipline | Verdict |
|---|---|
| **Design** | Wins. The two upgrade catalogs and the death curve are the product. Define before code. |
| **Balance** | Wins early. The whole game is tuned for bounce + idle and must be retuned on the run-end model. |
| **Monetisation** | Bends. Revive-ad + meta sinks replace endless-idle sinks. Never sell raw power. |
| **Compliance** | No change. Reward-ad consent + caps are already in the B10 plan. |
| **QA** | Wins early. Dying, reviving, banking and reloading must be tested from a real save. |
| **Pipeline (art)** | Bends. New upgrade icons, a rival face and the store rename are lead-time items — start by Phase C. |
| **Delivery** | Bends. Ship the small playable loop first (Phases B-D), not all systems. |
| **Scope** | Biggest risk. A roguelite swallows gear + abilities + camp + idle. Keep them off the critical path. |

---

## 11. Open questions

1. Auto-buy: manual default, with a META auto-buy unlock? (Recommend yes.) See §5.2.
2. Reset scope: always restart at stage 1, or META "start at best checkpoint"? (Recommend stage 1 first, then
   Start-at-Stage as a META row.)
3. Meta currency display name: "Shards" (dawn shards) vs "Tokens". Recommend Shards; enum unchanged.
4. Offline/camp: keep as optional "training between runs" or disable from the loop? (Phase F decision.)

---

## 12. Build order (pointer)

Phases A-F with acceptance tests live in the pivot plan: **A** define (this doc + catalogs), **B** run lifecycle
(`EndRun`), **C** upgrade layers, **D** balance pass on the robot, **E** story binder + name, **F** idle decision +
doc cleanup. Each step ends with a manual Unity test, per `.clinerules`.
