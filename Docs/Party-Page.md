# The Party page - vision, information architecture, and the path there

> Design record (2026-09-30). Classic RPG/JRPG framing, adapted to an **idle** game. The MVP ships the
> shell plus two tabs (Roster, Formation); everything else is recorded here so the layout never has to be
> redesigned when it lands.

---

## 1. Why this page exists

The battle page is the **what is happening**; the Party page is the **who and how**. It is the only place the
player composes their team, and it must grow from "3 fixed heroes" to "a roster you collect, equip, teach and
arrange" without ever changing shape.

Design pillars:

1. **One page, tabs that fill in.** The page owns a segmented sub-nav; new systems become new tabs (or new
   rows inside an existing card), never a new screen.
2. **Data-built rows.** Stats, gear slots and ability slots are rendered from lists. Adding a stat or a slot
   is a data change.
3. **Concrete numbers only** (`UI-UX` C7): no single opaque "power" value. Integer display (except upgrade/
   multiplier descriptors, which keep decimals by decision).
4. **The idle glue.** A party that fights while you are away must be *set once*: auto-arrange, auto-equip,
   presets, and a clear split between *push* and *farm* loadouts.

## 2. The four sub-menus (long-term)

### 2.1 Roster  *(MVP)*
The collection and the hero sheet.

- Owned cards + **unowned silhouettes** (collection aspiration; needs an `owned` flag on the roster data).
- Card: animated idle portrait, name, role/tag chips, stars/rarity, level.
- Hero sheet: stats (base + derived, deltas marked), role/tags, **gear slots**, **ability slots**, bonds/lore
  later. Informational only - buying stays in Grow (`one job per screen`).
- Party management (later): active vs bench, team 3 -> 5, add/remove, favourites/locks, sort/filter.
- Growth hooks (later): shards -> star-up (a star unlocks an ability slot, per `Roadmap` Step 17), XP from
  idle/expeditions.
- Virtualise the list beyond ~50 rows (`UI-UX` §8).

### 2.2 Formation  *(MVP)*
The board - the only place heroes are moved (`PartyBoardUI`), plus the *why*.

- Front/back ranks already carry real weight (`FormationData.BackRowTargetWeight`): the page should say so in
  plain words ("the back rank is targeted 0.8x as often").
- Later: **presets** (push / farm / boss), **auto-arrange** by role, per-zone recommended layouts, row-gated
  abilities (taunts, back-row auras), adjacency synergies.
- MVP keeps: tap a hero -> tap a slot, and the rank-mechanics line. No presets, no auto-arrange (3 heroes).

### 2.3 Inventory  *(gear LIVE in the MVP; items v1.1)*
Equipment and items.

- **Equipment M1 (2026-10-04):** three typed slots per hero (Weapon/Armor/Trinket -> ATK/DEF/HP) with boss
drops, class locks, manual equip via a picker, and the Inventory drawer inside the Roster row. Full design
record: Docs/Equipment-MVP-Plan.md.
- Later slots: relic and other expansions as they ship. The typed-slot model replaces the four placeholders.
- Items as data: slot, rarity, affix rows, level; drops from stages/bosses (the reserved `Materials`,
  `Essence`, `Scrolls` currencies are the crafting/upgrade inputs).
- Quality-of-life that an idle game cannot live without: **auto-equip best**, **auto-salvage with locks**,
  comparison arrows, stash + cap.
- Idle-flavoured stats on gear: gold/s, offline cap, drop rate, team auras; set bonuses; gear levelling.

### 2.4 Abilities  *(v1.1 - Roadmap Step 13)*
Auto-cast loadouts.

- `AbilityData` (trigger, cooldown, charges, target) + `AbilityRuntime`; auto-cast in player-ordered
  **priority**; 1 ability per hero first, 3-4 later; enemy kits later.
- Slots unlock by star level / zone; a locked slot shows its condition ("2 stars", "zone 3").
- Upgrade with scrolls. The combat log already prints every cast, so abilities read immediately.

### 2.5 Recorded for later, not planned yet
Expeditions (offline parties), bounties/dailies, bonds/support, elements/affinities + statuses,
transcendence/relics (Step 18), codex/bestiary, cosmetics/season cards.

## 3. Information architecture (the shape of the page)

```
PARTY                                   <- management page, first tab (keeps the bottom nav at 4 items)
[ ROSTER | FORMATION ]                  <- segmented sub-nav (Inventory/Abilities appear when built)

ROSTER
  selector strip:  3 animated portrait cards (tap = select)
  big animated selected hero (left)   |   hero card (right):
                                            name, role chips, stars (reserved)
                                            stat rows (HP/ATK/DEF - displayed as a role header + labelled rows; DPS = ATK + attack interval) - live from the Resolver
                                            GEAR row (3 typed slots, LIVE: drops/equip/discard)
                                            ABILITIES row (3 slots, locked)

FORMATION
  formation board (tap hero -> tap slot)
  rank readout + hint
```

Rules that keep it scalable:

- Reuse the existing segmented-control language (`TabController` is the visual precedent; the Party page uses
  a lightweight 2-button switcher so it does not fight serialized wiring).
- Every list is data-built; slots are reserved now and filled later.
- Idempotent binds: switching tabs or selection must never resize a sprite or restart an animation
  (`Party-Page` relies on the `CharacterAnimator.SetSlotSize` contract shipped 2026-09-30).
- Integer numbers everywhere except upgrade/multiplier descriptors.

## 4. MVP scope (what ships now)

- **Shell**: Party page with a `ROSTER | FORMATION` sub-nav. Inventory/Abilities are *not* shown (no dead
  buttons).
- **Roster**: 3 animated portrait cards; big animated selected hero; stats panel with live derived stats
  (gear boosts included); reserved stars line; gear row (3 typed slots, live equipment) and ability row (3 locked).
- **Formation**: the existing board + rank readout + hint. No presets/auto-arrange.
- 3 heroes, no add/remove/bench.
- Known cosmetic: all three portraits share the Soldier art until distinct hero art exists.

## 5. Phasing

| Phase | Contents |
|---|---|
| **M1 (now)** | Shell + Roster (stats) + Formation (board) + Equipment v1 (boss drops, 3 typed slots, manual equip, auto-equip/auto-salvage upgrades) |
| **M2** | Presets, rank tooltips, roster silhouettes (data hook only) |
| **v1.1** | Roster growth (owned/stars/shards), Inventory (items/drops/auto-equip), Abilities (Step 13), Expeditions (Step 17), Relics (Step 18) |

## 6. Data-model needs (recorded so the MVP does not paint us into a corner)

| Need | Today | Note |
|---|---|---|
| Roster ownership | `PartyConfig.heroes` is a fixed list of 3 | v1.1 adds `owned` + active-party separation |
| Stars / rarity / level | none | reserved UI space only |
| Stat list for the sheet | derived stats exist via `StatResolver` | sheet renders rows from stat kinds, so gear/ability stats slot in later |
| Gear / items | none (`Materials`/`Essence`/`Scrolls` currencies reserved) | v1.1 |
| Abilities | none | Roadmap Step 13 |
| Formation presets | none (`Formation` has one layout) | M2 |
