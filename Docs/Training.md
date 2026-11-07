# Training - permanent hero paths + the party track

Status: **built** - 8 Nov 2026. Training is the permanent upgrade page: **three hero columns paid with Gold** and a
**party column paid with Perk Tokens** (the ascension currency). Parent: `Run-Loop.md` (gold is permanent),
`Progression.md` (the permanent-upgrade axis), `UI-UX.md` (the lobby tabs).

## 1. Shape - three hero chains + one party chain

```
Knight (Gold)   Mage (Gold)   Soldier (Gold)   PARTY (Perk Tokens)
  Attack +N      Attack +N      Attack +N        R0 Active Skill Slot      gate 0    2 tokens
  Armor  +N      Armor  +N      Armor  +N        R1 Battle Gold +10%       gate 13   4 tokens
  Max HP +N      Max HP +N      Max HP +N        R2 Camp Healing +25       gate 26   6 tokens
  ...            ...            ...              R3 Finish Gold +15%       gate 38   8 tokens
                                                 R4 Camp XP +5             gate 44  12 tokens [deferred]
                                                 R5 Level-Up Heal 15%      gate 50  18 tokens [deferred]
                                                 R6 Survive Once + Shield  gate 50  30 tokens [deferred]
```

- A hero chain is **450 single-unlock nodes**: 150 cycles of `Attack -> Armor -> Max HP`. Amounts step +1 on every
  appearance: Attack `3 -> 152`, Armor `20 -> 169`, Max HP `20 -> 169`. Linear - each node needs the one below it.
- The party chain is gated by the **LOWEST hero's count** - not the best hero, not the sum. Every party reward is a
  milestone the whole party reaches together, so dumping all gold into one hero cannot rush it.
- The whole asset is **1,357 nodes** (450 x 3 heroes + 7 party).
- Node ids: `hero_knight:L000` ... `hero_soldier:L449`, `party:R0` ... `party:R6`.

## 2. Currency

| Track | Currency | Why this line |
|---|---|---|
| Hero columns | **Gold** (permanent, kept across runs) | the run-long resource; buying one hero competes with the other two |
| Party column | **Perk Tokens** (the ascension currency) | tokens survive resets, so they buy permanent, party-wide things |

Party prices are **token-scale** (2 / 4 / 6 / 8 / 12 / 18 / 30), not gold-scale. Token income is
`max(1, (depth / 10) ^ 1.5)` per run banked, so the first party node is about one or two runs and the last is many.

> **The party track competes with the PERKS tab for the same tokens.** Balance wins: keep the party track few and
> cheap, or nobody buys a perk. The measurement is **runs needed per party node** (tokens per run at depth 10 / 50 /
> 100 against the price) - target about 1-2 runs for the first node and 10+ for the last.

> **Gold =/= Experience.** Separate resources. Any node whose effect is "experience" or "on level up" is deferred
> (shown as `Coming soon.`, not purchasable) until those systems exist.

## 3. Effects - mapped to the code

| Effect | Scope | Build? | Where it applies |
|---|---|---|---|
| Attack (flat) | the hero owning the column | **now** | `StatResolver.GetAttack` adds `TotalFor(AttackFlat, heroId)` |
| Armor (flat defense) | that hero | **now** | `StatResolver.GetDefense` |
| Max HP (flat) | that hero | **now** | `StatResolver.GetMaxHealth` |
| Camp Heal (+ points) | party | **now** | `CampService.TrainingCampHealBonus()` -> `TotalFor(CampHealPercent)` |
| Gold from battle (%) | party | **now** | the gold funnel (`ResolveGoldReward`, `GameManager.Snapshot`) |
| Gold for finishing a level (%) | party | **now** | bonus gold on stage/level clear (`GameManager.Stages`) |
| Active Skill Slot | party | **now** | unlocks the first ability for all heroes (`AbilityService.OpenAllForMvp`) |
| Experience in camps | party | deferred | needs an XP system |
| Heal %maxHP on level up | party | deferred | needs a level system |
| Survive once + shield stacks | party | deferred | needs a death-save/shield system |

Training's flat bonuses are added **after** the perk multiplier chain, so perks never multiply them.

## 4. Data model

- `TrainingBranch` - `Left` (a hero chain), `Right` (the party chain).
- `TrainingEffect` - `AttackFlat`, `ArmorFlat`, `HpFlat`, `CampHealPercent`, `BattleGoldPercent`,
  `FinishGoldPercent`, `UnlockFirstAbility`, `CampExperience`, `LevelUpHealPercent`, `SurviveDeathShield`.
- `TrainingNode` (serializable class inside the tree asset) - `nodeID, heroID, branch, effect, amount (double),
  cost (double), costCurrency (CurrencyType), order (int), requiresLeftNodes (int), title, description`.
  `heroID` empty = the party node. `order` is the index up that hero's chain.
- `TrainingTreeDef : ScriptableObject` - the node list. Add content by data (re-run the generator).
- `TrainingService` - the facts the UI needs, no wording:
  - `LeftUnlockedCount(heroId)`, `TotalLeftUnlockedCount`, `LowestLeftCount`, `TryGetLaggard(out heroId, out count)`
  - `IsUnlocked(node)`, `IsGateMet(node)`, `CanAffordNode(node)`, `CanUnlock(node)`
  - `TryUnlock(node)` - spends `node.CostCurrency` through `EconomyManager.SpendCurrency`
  - `TotalFor(effect)` = party nodes only; `TotalFor(effect, heroId)` = one hero's nodes
  - `Changed` event, `FirstAbilityUnlockRequested` hook

## 5. UI (built, `StartScreenUI.Training.cs`)

```
[ Knight ]   [ Mage ]   [ Soldier ]   [ PARTY  ### ]   <- pinned column headers (portrait + name)
     ◆            ◆           ◆             ◆
     ◆            ◆           ◆             ◆            scrolls; ~5-6 rows visible
     ◆            ◆           ◆             ◆
```

- **Four columns** at x = 129 / 388 / 648 / 907 (canvas units). Nodes are **112 x 112** diamonds on **220** unit
  rows, laid out **bottom-up**: `order 0` sits at the very bottom, so the page opens scrolled to the bottom and the
  player climbs by scrolling up.
- **Pinned headers** (they never scroll away) name the owner: hero portrait + name for the three chains, and a
  PARTY cell with the three mini portraits plus a gate line (`13 each · lowest 2`, `gate met`, `open`,
  `all unlocked`). Tapping a header **jumps that column to its frontier**.
- **States by colour**: unlocked = gold, next buyable = mid grey with a green up-arrow badge, locked = dark grey,
  deferred = near-black. A node's connector turns gold once that node is unlocked.
- **A soft gold band per column** grows from the bottom to that column's frontier. The party band follows the
  lowest hero count (the gate), not party purchases.
- **Tap a node -> a card opens beside it** (right of the node in the two left columns, left of it in the two right
  ones). Not a modal: no backdrop, no dimming, and it scrolls with the node. Tapping the same node closes it.
- **Card rules, in this order** - the first match wins:

| State | Body line | Button |
|---|---|---|
| owned | effect line (`Armor +21`) | **removed** |
| deferred | `Coming soon.` | **removed** |
| party gate not met | `All three heroes need 13 training nodes - Soldier has 2.` | shown, disabled |
| gate met, money short | `Need 4 tokens - you have 1.` / `Need 300 gold - you have 120.` | shown, disabled |
| ready | effect line | live |

  The gate is checked **before** money: gold cannot fix a gate, so naming money first would send the player to the
  wrong shop. The button always carries the real price (`Buy 300` + coin, `Buy 4` + star).
- Card title names the owner: `Knight · Armor`, `Party · Battle gold`.
- Cost icons are drawn in code (circle for gold, star for tokens) - the lobby cannot load editor sprites.
- Level markers were dropped: four columns of `LEVEL n` labels collide, and the pinned header already anchors the
  reader.

## 6. Save

- `TrainingService` writes each unlocked node as a keyed level: `levels["train.<nodeID>"] = 1`, e.g.
  `train.hero_knight:L000`, `train.party:R0`. No schema bump; the mapper already carries the list.
- On load, ids that do not exist in the current tree are **dropped**, so old-shape keys (`train.L000`) clean
  themselves out on the next save instead of piling up.

## 7. Open notes (kept on purpose)

1. **Gold =/= Experience.** Separate resources; Training spends Gold (heroes) and Tokens (party).
2. **Experience system** - camps grant experience, levels, heal-on-level-up: not built; R4/R5 wait on it.
3. **Party gate rule** - built as the LOWEST hero's count. Alternatives were sum (one strong hero carries) and
   average. Revisit only with data.
4. **Numbers are placeholders** - hero curve, token prices and the 3x party grind are best guesses; tune on the
   Balance Lab. The check for tokens is runs-per-node, not a feeling.
5. **Node icons are letters** (A / D / H / C / F / $). Real art replaces them later; the layout does not change.
6. **Cost of three chains** - filling all three hero chains costs 3x the gold of one. `Idle-Economy.md` needs a pass.

## 8. Build order

1. This doc + decision notes (gold for heroes, tokens for the party, per-hero flat stats).
2. Data: `TrainingNode` (`heroID`, `Cost`, `CostCurrency`) + generator (3 x 450 + 7) + `TrainingService` - **built**.
3. Effects: per-hero `StatResolver` read; party camp heal / gold / ability slot - **built**.
4. UI: four columns, pinned headers, per-column bands, node card with the reason table - **built**.
5. Next: real icons, tune the numbers on the Balance Lab, and pass `Idle-Economy.md` for the 3x gold cost.
