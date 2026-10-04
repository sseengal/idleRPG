# Equipment MVP — the plan

> Design record + build order (2026-10-04). "Items" (consumables/materials/crafting) are a LATER system; this
> doc covers **equipment only**: gear that drops from boss kills, is class-locked, is manually equipped into
> three typed slots, and has two token-priced quality-of-life upgrades.

---

## 1. Decisions (locked with the user)

- **3 typed slots per hero — Weapon / Armor / Trinket → ATK / DEF / HP.**
  - *Why:* "best item" is deterministic (max ATK weapon / max DEF armor / max HP trinket), auto-equip is
    unambiguous, and the picker shows one number per item. Untyped slots would allow three ATK sticks and a
    degenerate loadout.
- **Class-locked:** each drop is for exactly one of the three hero classes (Tank / Damage / Support); only a
  hero of that class can equip it.
- **Drops:** every cleared boss rolls the chance (boss gems keep their first-clear-only rule; drops are the
  repeatable reward).
- **Manual equip first.** Tap a gear slot → picker of matching items → tap to equip (swap returns the old item
  to the bag).
- **Major upgrade (tokens): Auto-Equip.** Unlock → a drop is worn automatically when it beats the class's
  current piece in that slot (lifts the weakest of the matching heroes).
- **Minor upgrade (tokens): Auto-Salvage.** Unlock → overlapping gear (full bag or a swap-overflow) becomes
  gold instead of being lost. Without it, overflow is an explicitly announced discard — never silent.
- **Placeholder art:** rarity-tinted tile + slot letter (W/A/T). The item model keeps room for real sprites.

## 2. Balance (hypotheses to measure in the tuning pass, not trusted)

| Knob | Value | Where |
|---|---|---|
| Boss drop chance | 35% | `BalanceConfig.BossGearDropChance` |
| Inventory cap | 20 | `BalanceConfig.InventoryCap` |
| Stat formula | `(0.015 + 0.0012·level) × rarity{1,1.6,2.4,3.6}`, capped 0.25 | `BalanceConfig` gear fields |
| Stat meaning | **% of the hero's BASE stat** (stays relevant as stages climb) | resolver applies before level/prestige |
| Price | `(2 + 0.9·level) × rarity{1,2,4,8}` gold | `BalanceConfig` |
| Salvage payout | 40% of price | `GearSalvageFraction` |
| Auto-Equip cost | 25 tokens, one-time | Prestige asset |
| Auto-Salvage cost | 18 tokens, one-time | Prestige asset |

Target: one piece ≈ +2–5% of the hero's base stat at its stage; a full Legendary set ≈ +20–40%. Gear must
NEVER outshine gold upgrades — the tuning pass measures the gear share of total stats by stage and renumbers
the data assets if it creeps above ~30% of total power.
## 3. Architecture (modularity + scale)

```
Equipment/
  ItemSlotType.cs   enum Weapon/Armor/Trinket + primary-stat mapping
  ItemRarity.cs     enum + display/multiplier tables
  ItemInstance.cs   one dropped piece (id, slot, rarity, role, level, stat%, price)
  ItemFactory.cs    rolls instances from BalanceConfig (stage-scaled)
  ItemService.cs    inventory + equipped slots + drop pipeline + save
```

- **Combat never knows items exist.** `StatResolver.GearBonusReader` returns per-hero `ItemBonuses`
  (fractions); `GetMaxHealth/GetAttack/GetDefense` fold gear into the base before the existing level/prestige
  formula, then the existing `StatsChanged` event refreshes the sim and the party sheet.
- **Events:** `ItemService.Changed` -> save dirty + resolver refresh + party-page repaint.
- **Drop pipeline (`TryBossDrop`):** roll chance -> roll item -> if Auto-Equip unlocked and strictly beats a
  matching hero's slot, equip (previous piece returned) -> else bag -> bag full -> salvage or announced discard.
- **Persistence:** save schema **v6** (additive; migration v5->v6 = empty lists). All instances (worn + bag)
  travel in one list; `equippedGear` records reference instance ids. Ascension keeps everything.
- **Scale hooks:** the stat value is a single fraction today but the resolver summation + save shape are the
  extension points for affixes/set bonuses (data + save only, no combat change); Auto-Equip's policy is a
  private method (swap in a weighted DPS policy later); curated/shop items later = instances with a `definedBy`
  field (recorded, not built).

## 4. UX (the tap loop)

- Roster: the three equipped slots, view-only, with the equipped bonus shown next to each stat in the
  stats card (e.g. DEF 13 +4.8%).
- INVENTORY tab (live since 2026-10-04, see Docs/Inventory-Tab-Plan.md): two dropdowns (SLOT, SORT), a
  fixed 4x5 = 20 slot bag (empty slots visible), E badge + ring on equipped, bottom-sheet popup with
  EQUIP-><hero> / UNEQUIP and a two-tap DISCARD. Equip is only offered when it can succeed.
- Every drop/equip/discard/salvage writes a line into the battle log (Reward = gold colour); Legendary drops,
  discards and salvages also toast.
- The two upgrades appear as normal Ascension cards (token prices, one-time).

## 5. Build order (as executed)

| # | Step | Files |
|---|---|---|
| 1 | Data model + schema v6 (records, migration) | `SaveData.Records.cs`, `SaveData.cs`, `SaveMigrations.cs` |
| 2 | Equipment domain (slot/rarity/instance/factory/service) | `Equipment/*` |
| 3 | Resolver + prestige hooks (2 utility effects) | `StatResolver.cs`, `PrestigeUpgradeData.cs`, `BalanceConfig.cs` |
| 4 | Wiring + boss-drop hook + battle-log lines | `GameManager*.cs` |
| 5 | Party UI: 3 typed slots, inventory picker, two-tap discard | `PartyPanelUI.cs` |
| 6 | Token upgrades as Ascension cards (2 assets + scene list) | `Data/Config/Prestige_*`, `Main.unity` |
| 7 | Verification + tuning | play probes / edit-mode round trips |
| 8 | Docs + screenshots + commit | this doc, `Party-Page.md`, `Checklist.md` |

## 6. Verified

- Drops roll class/slot/rarity/level with stage-scaled stat + price (at stage 4: Common +2% tank trinket,
  Epic +4.8% support trinket).
- Manual equip respects the class gate ("Support gear only") and updates the sim + party sheet live
  (Knight HP 240 -> 245 with a +2% trinket).
- Save round-trip keeps worn + bag items and their rolls (dropped -> equipped -> saved -> reloaded -> worn,
  same instance id).
- Auto-Equip unlock routes drops straight onto the matching hero (bag stays empty).
- Auto-Salvage at a full bag converts overflow into gold (price x 40%) instead of losing it.
- Battle log receives a line for every drop/equip/discard/salvage via the existing CombatMessage event.

## 7. Out of scope (v1.1 backlog)

Item levelling/upgrading, crafting, affix rerolls, multiple affixes, set bonuses, sell button, shop-sold
items, enchants, filters/sort, abilities/presets interplay. "Items" (consumables/materials) stay separate.
