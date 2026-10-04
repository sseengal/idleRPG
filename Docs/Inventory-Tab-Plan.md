# Inventory tab — design + build record

> (2026-10-04). Follow-up to `Docs/Equipment-MVP-Plan.md`. The first equipment UI shoved the bag into a tiny
> pager drawer on the Roster (13px rows, 11px trash, overlapping the abilities row). This design moves ALL of
> inventory selection/discarding onto its own Party page tab with mobile-first touch sizes.

---

## 1. What changed

- Party page top bar: `ROSTER | FORMATION | INVENTORY` (3 equal tabs, 44px+ tall).
- **Roster** now shows ONLY the equipped gear: the three typed slots stay display-only (rarity tint + stat)
  and a tap on a slot jumps to the INVENTORY tab pre-filtered to that slot type. All pickers/drawers deleted.
- **INVENTORY tab** (new `InventoryTabUI` component, built by `PartyPanelUI` like `PartyBoardUI`):
  - header "INVENTORY n/20"
  - slot filter chips: ALL / WEAPON / ARMOR / TRINKET
  - sort chips: NEWEST / RARITY / BONUS
  - a square tile grid (4 columns, ~150px tiles, vertical ScrollRect + mask) — rarity tinted tile, slot
    letter (placeholder art), stat line, class corner tag
  - **equipped tiles:** a small **'E' badge** (top-left) + a **bright border ring**
  - tap a tile → **bottom-sheet popup**: big sprite, class ("TANK ONLY"), slot + stat ("Armor · +3.2% DEF"),
    source ("stage 4 drop · 11 gold"), **[EQUIP → KNIGHT]** / **[UNEQUIP]**, **[DISCARD]** with a two-tap
    guard ("TAP AGAIN"), close via X or tapping the dimmed backdrop
- Equip target is deterministic: an item's class maps 1:1 to its hero (Tank/Damage/Support ↔ Knight/Archer/
  Mage), so the sheet names the hero — no target selector needed.

## 2. Mobile minimums (measured on the 1080×1920 canvas)

- Chips: **96px** tall · Roster gear slots: **106px** · Tiles: **150px** (all far above the 44px floor).
- Fonts: body 14px, titles 16–18px; the only sub-14px text is the non-interactive E badge / class tag (12px).

## 3. Backend

Zero schema/save changes. `ItemService` gained three read helpers: `AllInstances()` (worn + bag, deduped),
`IsEquipped(id, out hero, out slot)` and `HeroFor(item)` (class → party hero). Existing `Equip/Unequip/
Discard` + `Changed` events drive everything; the battle-log lines and toasts were already in place.

## 4. Verified (Play, live probes)

- 3 tabs render; tab switch activates the right panel; Roster slot tap → INVENTORY with that slot filtered.
- Grid shows all instances (worn first); equipped tile carries the E badge + ring.
- Popup reads correctly ("Rare Armor / TANK ONLY / Armor · +3.2% DEF / stage 4 drop · 11 gold").
- UNEQUIP returns the item to the bag + grid refreshes; reopened sheet says "EQUIP → KNIGHT".
- DISCARD arms ("TAP AGAIN", nothing deleted) then confirms (bag −1, sheet closes).
- Slot filter (ALL/WEAPON/ARMOR/TRINKET) and sort (NEWEST/RARITY/BONUS) reorder the grid.
- Roster remains display-only: gear row shows the equipped piece + stat.

## 5. Fast-follow (recorded, not built)

Class filter chips (same builder as the slot chips), drag-to-reorder favorites, compare arrows against the
equipped piece, sort by hero instead of slot. Placeholder letter tiles swap to real square sprites later.