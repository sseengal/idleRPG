# Inventory — as-built implementation record

> (2026-10-04). How the equipment/inventory feature ACTUALLY works today: the authoritative reference for
> anyone touching this code. Design rationale lives in `Docs/Equipment-MVP-Plan.md` and the change history in
> `Docs/Inventory-Tab-Plan.md`; this doc is the map of what exists, how data flows, and where the knobs are.

---

## 1. What the player has today

**Party page — three tabs:** `ROSTER | FORMATION | INVENTORY`.

- **ROSTER**: three animated portrait cards, a big selected-hero portrait with its stats card. The stats card
  shows `HP / ATK / DEF / DPS` (integer values) and, next to `HP/ATK/DEF`, the **equipped gear bonus %** in
  green (e.g. `DEF  13  +4.8%`). Below it the `GEAR (equipped)` row shows the three worn pieces — view-only
  (rarity-tinted tile + stat, no taps). `ABILITIES` row stays locked (roadmap).
- **FORMATION**: the existing board (tap hero → tap slot), rank readout, hint. Unchanged by inventory work.
- **INVENTORY**: the bag.
  - Header `INVENTORY n/20` (accent when full).
  - Two dropdowns: `SLOT: ALL/WEAPON/ARMOR/TRINKET` and `SORT: NEWEST/RARITY/BONUS`. Option menus dismiss on
    tap-outside or on select (same-frame close; overlay + touch-catcher pattern).
  - A **fixed 4×5 = 20 slot bag**: filled tiles are rarity tinted with a slot letter (placeholder art), the
    stat line and a class corner tag; **empty slots** render as dark inert tiles.
  - Equipped items carry an **`E` badge** (top-left) and a **bright border ring**.
  - Tapping a filled tile docks a **bottom sheet** over the bag's lower rows (non-modal — the tiles above
    stay tappable and re-target the sheet). The sheet shows class ("TANK ONLY"), slot+stat ("Armor ·
    +3.2% DEF"), source ("stage 4 drop · 11 gold") and two actions:
    - `EQUIP → <hero>` / `UNEQUIP` (the hero is implied by the item's class — Tank/Damage/Support ↔
      Knight/Archer/Mage, `HeroFor`)
    - `DISCARD` with a **two-tap guard** ("TAP AGAIN", red, second tap deletes)
  - Tapping an empty slot, the `X`, or after an action closes the sheet. Every drop/equip/unequip/discard/
    salvage writes a battle-log line (Reward colour); Legendary drops, discards and salvages toast.

## 2. Code map

```
IdleRPG.Scripts/Equipment/
  ItemSlotType.cs    enum Weapon/Armor/Trinket + primary-stat mapping (ATK/DEF/HP)
  ItemRarity.cs      enum Common/Rare/Epic/Legendary + stat/price multiplier tables
  ItemInstance.cs    one piece: id, slot, rarity, class, level, statFraction, price
  ItemFactory.cs     rolls instances from BalanceConfig (class+slot+rarity+stage)
  ItemService.cs     the whole system: bag, equipped slots, drop pipeline, save
IdleRPG.Scripts/UI/
  InventoryTabUI.cs  the INVENTORY tab (grid, dropdowns, sheet) — built by PartyPanelUI
  PartyPanelUI.cs    the Party page shell (3 tabs; Roster stats + gear row; Formation)
IdleRPG.Scripts/Progression/StatResolver.cs
  GearBonusReader     stat-panel/combat integration point (see §3)
```

### ItemService — the one owner
- **State**: `inventory` (the unworn bag), `equippedByHero` (per-hero 3-slot arrays).
- **Events**: `Changed` → save dirty + resolver refresh + party-page/inventory repaint.
- **Drop pipeline** (`TryBossDrop(stage)`): roll chance → roll item → if **Auto-Equip** unlocked and the drop
  strictly beats a matching hero's current piece (weakest slot first), equip it (previous piece returns to the
  bag) → else into the bag → bag full → **Auto-Salvage** (gold) or an announced discard. Nothing is ever
  lost silently.
- **Public API**: `Equip(hero, id)`, `Unequip(hero, slot)`, `Discard(id)` (bag only), `AllInstances()`
  (worn + bag, deduped), `IsEquipped(id)`, `HeroFor(item)` (class → party hero), `FindFor(id)`,
  `GetGearBonusFraction(hero)` → the `ItemBonuses` (hp/atk/def fractions) the resolver consumes,
  `WriteToSave` / `FillFromSave`.
- **Equip gating invariant**: the UI only offers `EQUIP` when the item is in the bag and its class has a
  hero; the button label names that hero. Overriding a filled slot always succeeds (old piece → bag /
  salvage / announced loss).

### Screenshot of the patterns that keep it maintainable
- Every transient UI goes through **instant close** (`SetActive(false)` + `Destroy`) so nothing swallows a
  tap for a leftover frame; only one transient is open at a time (dropdown overlay vs docked sheet are
  mutually exclusive).
- Runtime-built UI only — no scene edits for these components (party page builds all of it from code).
- No polling: everything refreshes from `ItemService.Changed` / `OnEnable` / tab-show.

## 3. How gear reaches combat (the stat pipe)

1. `ItemService.GetGearBonusFraction(heroIndex)` sums the worn items' `StatFraction`s per stat
   (weapon → ATK, armor → DEF, trinket → HP). Fractions are **% of the hero's BASE stat**.
2. `StatResolver.GearBonusReader` (set at wiring time) returns those fractions.
3. `GetMaxHealth/GetAttack/GetDefense` fold them into the base: `base × (1 + gearFraction)`, then the existing
   level/prestige formula runs. `StatsChanged` → sim refresh + party-sheet repaint.
## 4. Data & save (schema v6)

- `SaveData.CurrentVersion = 6`; migration v5→v6 is additive (old saves load with no gear).
- `inventory`: list of `ItemSaveRecord` (id, slot, rarity, class, level, statFraction, price) — holds BOTH
  worn and bag instances; `equippedGear`: per-hero entries referencing instance ids (weapon/armor/trinketId).
- Worn items survive save/load (they travel in the same list; the equipped records only reference ids).
- Bag cap (default 20) is enforced on load (worn kept, bag trimmed).
- Ascension does NOT wipe gear (like tokens, it is an owned asset that softens the reset).

## 5. Balance knobs (all data-driven in BalanceConfig)

| Knob | Default | Notes |
|---|---|---|
| BossGearDropChance | 0.35 | every boss rolls; gems stay first-clear-only |
| InventoryCap | 20 | bag size; the fixed 20-slot grid renders the full bag |
| GearStatBaseFraction / PerStage / MaxFraction | 0.015 / 0.0012 / 0.25 | `base + perStage·level`, capped |
| GearRarityWeights | 55/28/12/5 | Common/Rare/Epic/Legendary |
| GearPriceBase / PerStage | 2 / 0.9 | price = `(base + perStage·level) × rarity{1,2,4,8}` |
| GearSalvageFraction | 0.4 | auto-salvage payout of price |
| Auto-Equip / Auto-Salvage | 25 / 18 tokens, one-time | two Ascension-tree cards (PrestigeEffectType) |

Balance intent: one piece ≈ +2–5% of a hero's base stat at its stage; a full Legendary set ≈ +20–40%. Gear
must never outshine gold upgrades (the tuning pass watches the gear share of total power, target < ~30%).

## 6. Class<->hero mapping

- Tank -> Knight, Damage -> Archer, Support -> Mage (HeroRole indexes 0/1/2).
- `HeroFor(item)` = first party hero whose class matches the item's class. **Known debt:** a roster with
  duplicate classes would need "pick the hero with the weakest slot", not "first".
- **Data fix shipped 2026-10-04:** Hero_Mage.role was `Damage`; Support-class drops had no owner and Damage
  had two heroes. Mage is now Support.
4. Combat never knows items exist; the sim reads only the final numbers.\n## 4. Data & save (schema v6)\n\n- `SaveData.CurrentVersion = 6`, migration v5→v6 is additive (old saves load with no gear).\n- `inventory`: list of `ItemSaveRecord` (id, slot, rarity, class, level, statFraction, price) — holds BOTH\n  worn and bag instances; `equippedGear`: per-hero entries referencing instance ids (weapon/armor/trinketId).\n- Worn items survive save/load (they travel in the same list; the equipped records only reference ids).\n- Bag cap (default 20) is enforced on load (worn kept, bag trimmed).\n- Ascension does NOT wipe gear (like tokens, it is an owned asset that softens the reset).\n\n## 5. Balance knobs (all data-driven in BalanceConfig)\n\n| Knob | Default | Notes |\n|---|---|---|\n| BossGearDropChance | 0.35 | every boss rolls; gems stay first-clear-only |\n| InventoryCap | 20 | bag size; fixed 20-slot grid renders the full bag |\n| GearStatBaseFraction / PerStage / MaxFraction | 0.015 / 0.0012 / 0.25 | `base + perStage·level`, capped |\n| GearRarityWeights | 55/28/12/5 | Common/Rare/Epic/Legendary |\n| GearPriceBase / PerStage | 2 / 0.9 | price = `(base + perStage·level) × rarity{1,2,4,8}` |\n| GearSalvageFraction | 0.4 | auto-salvage payout of price |\n| Auto-Equip / Auto-Salvage | 25 / 18 tokens, one-time | two Ascension-tree cards (PrestigeEffectType) |\n\nBalance intent: one piece ≈ +2–5% of a hero's base stat at its stage; a full Legendary set ≈ +20–40%. Gear\nmust never outshine gold upgrades (the tuning pass watches the gear share of total power, target < ~30%).\n\n## 6. Class↔hero mapping\n\n- Tank → Knight, Damage → Archer, Support → Mage (HeroRole indexes 0/1/2).\n- `HeroFor(item)` = first party hero whose class matches the item's class. **Known debt:** a roster with\n  duplicate classes would need "pick the hero with the weakest slot", not "first".\n- **Data fix shipped 2026-10-04:** Hero_Mage.role was `Damage`; Support-class drops had no owner and Damage\n  had two heroes. Mage is now Support.\n\n## 7. Interaction rules (the UX contract)\n\n1. Two transient layers, mutually exclusive: the dropdown overlay (dismiss on outside/select) and the\n   docked item sheet. Opening one closes the other.\n2. **No modal.** The sheet covers only its own rect; the bag above stays tappable and re-targets the sheet.\n3. **Instant close everywhere**: `SetActive(false)` then `Destroy`, so a dismissed widget never swallows\n   the next tap.\n4. Two-tap discard only; equip/unequip are single-tap. Equip is only *offered* when it can succeed.\n5. Every mutation lands in the battle log; big moments toast.\n\n## 8. Mobile minimums (1080×1920 canvas, measured)\n\nDropdowns ~105px tall, option rows ~55px, sheet buttons ≥48px, bag tiles 150px, roster gear preview 106px.\nBody font 14px+; only the non-interactive E badge / class tag use 12px.\n\n## 9. Verified (live probes, 2026-10-04)\n\nRounds documented in `Inventory-Tab-Plan.md`: grid settles to exactly 20 slots; dropdowns open/apply/dismiss\nin-frame; sheet docks (y 0.04–0.41), retargets, closes instantly; Support item routes to Mage and equips;\nworn+bag round-trip through save; auto-equip routes to the weakest matching slot; salvage pays price × 40%;\nRoster gear rows are button-free (view-only); per-stat bonuses render (DEF 13 +4.8%).\n\n## 10. Known debts / fast-follow\n\n- Duplicate-class `HeroFor` routing (records above).\n- Placeholder art: slot letters on rarity tint; real square sprites swap in later.\n- Auto-equip compares the *single* stat (no DPS-weighted policy yet).\n- No class dropdown, favourites/locks, compare arrows, tear-and-reorder, sell button (all recorded in\n  `Inventory-Tab-Plan.md §5`).\nEOF\necho done && wc -l Docs/Inventory-AS-BUILT.md"]
## 7. Interaction rules (the UX contract)

1. Two transient layers, mutually exclusive: the dropdown overlay (dismiss on outside/select) and the
   docked item sheet. Opening one closes the other.
2. **No modal.** The sheet covers only its own rect; the bag above stays tappable and re-targets the sheet.
3. **Instant close everywhere**: `SetActive(false)` then `Destroy`, so a dismissed widget never swallows
   the next tap.
4. Two-tap discard only; equip/unequip are single-tap. Equip is only *offered* when it can succeed.
5. Every mutation lands in the battle log; big moments toast.

## 8. Mobile minimums (1080x1920 canvas, measured)

Dropdowns ~105px tall, option rows ~55px, sheet buttons >=48px, bag tiles 150px, roster gear preview 106px.
Body font 14px+; only the non-interactive E badge / class tag use 12px.

## 9. Verified (live probes, 2026-10-04)

Rounds documented in `Inventory-Tab-Plan.md`: grid settles to exactly 20 slots; dropdowns open/apply/dismiss
in-frame; sheet docks (y 0.04-0.41), retargets, closes instantly; Support item routes to Mage and equips;
worn+bag round-trip through save; auto-equip routes to the weakest matching slot; salvage pays price x 40%;
Roster gear rows are button-free (view-only); per-stat bonuses render (DEF 13 +4.8%).

## 10. Known debts / fast-follow

- Duplicate-class `HeroFor` routing (recorded above).
- Placeholder art: slot letters on rarity tint; real square sprites swap in later.
- Auto-equip compares the *single* stat (no DPS-weighted policy yet).
- No class dropdown, favourites/locks, compare arrows, tear-and-reorder, sell button (all recorded in
  `Inventory-Tab-Plan.md` section 5).
