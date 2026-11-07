# Training — the permanent gold tree

Status: **design of record; data, effects and the page are built** - 8 Nov 2026. Training is the permanent upgrade
tree bought with **Gold**.
It is the gold sink that gives money a purpose across runs. Parent: `Run-Loop.md` (gold is permanent), `Progression.md`
(the permanent-upgrade axis), `UI-UX.md` (the lobby tabs).

## 1. Shape — two vertical branches

```
LEFT  (dense, 600 nodes = 75 levels x 8)   RIGHT (sparse, few nodes, gated by LEFT depth)
  Attack +N                                  R1  +1 Active Skill Slot  (left 0)
  Armor  +N   (flat defense)                 R2  +% Gold from battle  (left ~13)
  Max HP +N                                  R3  +% Gold for finishing a level (left ~26)
  Camp Heal +N%                              R4  +5 Experience in camps (left ~38)      [deferred]
  ... repeats ... up to 75                   R5  Heal %maxHP on level up (left ~44)     [deferred]
                                             R6  Survive death once + shield stacks (left ~50) [deferred]
```

- **LEFT** is the grind bar: **600 single-unlock nodes = 75 visual levels × 8** (two full
  `Attack → Armor → Max HP → Camp Heal` sets per level). Amounts step **+1 on every appearance of that kind**:
  Attack `3→152`, Armor `20→169`, Max HP `20→169`, Camp Heal `1→150`. Linear (each node needs the one below it).
  **"Level" is a visual background marker every 8 nodes** - not bought, not a gate.
- **RIGHT** is sparse: a few big nodes, each unlocked once the LEFT branch has climbed to a threshold
  (`leftNodesUnlocked >= requiresLeftNodes`). It **ends around left-node 50** (not 75).

## 2. Currency

**Training costs Gold.** Gold is permanent (kept across runs; see `Run-Loop.md`).

> **Gold ≠ Experience.** They are **separate** resources. Older notes used them loosely; do not merge them.
> Experience is a **future system** (earned in camps, used for levels). Until it exists, any node whose effect is
> "experience" or "on level up" is **deferred** (visible but greyed "coming soon", not purchasable).

## 3. Effects — mapped to the code

| Effect | Build? | Where it applies |
|---|---|---|
| Attack (flat) | **now** | `StatResolver.GetAttack` + training total |
| Armor (flat defense) | **now** | `StatResolver.GetDefense` |
| Max HP (flat) | **now** | `StatResolver.GetMaxHealth` |
| Camp Heal (%) | **now** | add to `BalanceConfig.CampShortRestPercent` / `CampLongRestPercent` via `CampService` |
| Gold from battle (%) | **now** | the gold funnel (`ResolveGoldReward` / global gold multiplier) |
| Gold for finishing a level (%) | **now** | bonus gold granted on stage/level clear |
| Active Skill Slot | **now** | unlock the **first ability for all heroes** (`AbilityService`) |
| Experience in camps | deferred | needs an XP system |
| Heal %maxHP on level up | deferred | needs a level system |
| Survive once + shield stacks | deferred | needs a death-save/shield system |

## 4. Data model

- `TrainingBranch` — `Left`, `Right`.
- `TrainingEffect` — `AttackFlat`, `ArmorFlat`, `HpFlat`, `CampHealPercent`, `BattleGoldPercent`,
  `FinishGoldPercent`, `UnlockFirstAbility`, `CampExperience`, `LevelUpHealPercent`, `SurviveDeathShield`.
- `TrainingNode : ScriptableObject` — `nodeID, branch, effect, amount (double), costGold (double), order (int),
  requiresLeftNodes (int), title, description`. `order` is the node's index up its branch; a LEFT node needs the
  previous LEFT node unlocked; a RIGHT node needs `leftNodesUnlocked >= requiresLeftNodes`.
- `TrainingTreeDef : ScriptableObject` — the list of `TrainingNode`s (one asset; add nodes by data).
- `TrainingService` — unlocked set, `LeftUnlockedCount`, `CanUnlock` (branch gate + gold + not deferred),
  `TryUnlock` (spends gold), per-effect totals, event. Save = unlocked node ids (additive).

## 5. UI (built)

- Lives on the **Start-Screen lobby** under the **TRAINING** tab. The game is paused while the lobby is open.
- **One scroll, two tracks.** LEFT nodes are a dense column at x = 350 (canvas units); RIGHT nodes are a sparse
  column at x = 752. Both are laid out **bottom-up**: node `order 0` sits at the very bottom, so the page
  **opens scrolled to the bottom** and the player climbs by scrolling up.
- **Rows are 220 units tall; nodes are 112 x 112.** About **5-6 nodes** are visible at a time, which keeps every
  touch target large.
- **Node art:** a diamond (square rotated 45 deg) with an upright letter icon - A = attack, D = armor, H = max HP,
  C = camp heal, F = first ability, `$` = gold. Placeholder art; real icons replace the letters later.
- **States by color:** unlocked = gold, next buyable = mid grey with a green up-arrow badge, locked = dark grey,
  deferred = near-black. The connector above a node turns gold once that node is unlocked, grey otherwise.
- **Zones:** a soft gold band grows from the bottom up to the frontier (the highest unlocked node); above it is
  charcoal. The band is a tint, not a solid fill.
- **The right track is spaced by its gate.** Each right node sits on the row of the LEFT count it needs
  (0 / 13 / 26 / 38 / 44 / 50), and its connector runs the whole gap up to the next right node, so the side track
  reads as one continuous line that meets the main line at the right heights.
- **Tap a node -> a card opens beside it** (right of a LEFT node, left of a RIGHT node). It is **not** a modal:
  no backdrop, no dimming, and it scrolls with the node. Tapping the same node closes it.
  - The card shows a short name (`Camp heal`), one effect line (`Camp healing +3`), and an orange
    `Buy <cost>` button with a gold coin. Nothing else.
  - **Once a node is owned the Buy button is removed** (the card shrinks); it is not greyed out.
- **Header:** the page uses the **shared battle header** (`HudHeaderUI` - gold / gems / tokens + RANKS), not a
  copy. `StartScreenUI` re-parents the real header into the lobby canvas while the lobby is open and puts it back
  when a run starts. The training page adds no title text of its own.
- **The bottom nav highlights the open tab** (gold = selected, dark = the rest).
- The scroll-to-bottom button (▼) jumps back down to the frontier.

## 6. Save

- `TrainingService` writes each unlocked node as a keyed level in the existing save list:
  `levels["train.<nodeID>"] = 1`. No schema version bump; the mapper already carries the list.

## 7. Open notes (kept on purpose)

1. **Gold ≠ Experience.** Separate resources; Training spends Gold. Experience is a future system.
2. **Experience system** — camps grant experience, levels, heal-on-level-up: not built; R4/R5 wait on it.
3. **Stage / Level / Chapter** wording needs redefining (chapter = band of stages, 3 bosses, a camp after each
   boss). "Finishing a level" currently means clearing a stage until settled.
4. **Numbers are placeholders** — left-branch curve, right-gate thresholds, gold costs and caps are best guesses;
   tune on the Balance Lab after the loop is playable.
5. Node R6 ("survive once + diving shield stacks") needs a new death-save/shield mechanic — deferred.

## 8. Build order

1. This doc + decision notes (gold-costed; experience separate; deferrals).
2. Data: `TrainingNode` / `TrainingTreeDef` + editor generator (75 left + right) + `TrainingService` + save ids.
3. Left effects (Attack / Armor / HP / Camp Heal) → `StatResolver` / `CampService`.
4. Right **R1–R3** (unlock-abilities, battle gold%, finish gold%). R4–R6 greyed "coming soon".
5. Training tree UI on the lobby tab (two tracks, node card, gold unlock) - **built**; shared header and nav
   highlight wired in.
6. Update `Run-Loop`, `Progression`, `UI-UX`, `Architecture`, `Checklist` to this vision.

