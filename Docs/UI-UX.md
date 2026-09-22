# UI-UX — screens, patterns, layout, accessibility

> Owns: everything the player sees and taps.
> Parent: `Architecture.md` (AD8, E7, E10, C7, D6) and `.clinerules`.
> **Location:** `Docs/` (folder index: `README.md`). **Last verified:** Step 11e (2026-09-21).
> Specs the UI side of Steps 10, 11, 13, 15, 17, 20.

---

## 1. Patterns that must hold (learned the hard way in the MVP)

| Pattern | Rule |
|---|---|
| Lazy binding | Anything on a page that starts inactive binds in `EnsureBound()` called from **both** `Start` and `OnEnable`; never self-disable on a late dependency |
| One listener | Wire button listeners once, in `Configure()`/`EnsureBound()` after `RemoveAllListeners()` |
| Data-driven rows | Rows come from a list + a prefab template (`RowView`), never a fixed serialized array (E10) |
| Presenter split | `XxxView` = widgets only; `XxxPresenter` = binds events, formats strings, decides visibility |
| Event-only updates | No polling in `Update()`; presenters subscribe to `GameEvents`/service events |
| No scene lookups | References come from `UiContext`/`GameContext` (E5) |
| Pool anything spawned | Damage text, rows, enemy views, toast lines |
| Idle-safe screens | Every screen is safe to open while combat runs; nothing pauses the fight |

## 2. Navigation model

MVP shell today: header + Battle/Management pages + nav (BATTLE / UPGRADES / ASCEND / SHOP).
Target shell adds depth without nav explosion:

```
Bottom nav (max 5):  BATTLE | PARTY | GROW | ASCEND | SHOP
                     (Step 10d ships BATTLE | PARTY | UPGRADES | ASCEND | SHOP; the GROW split is Step 21)
  BATTLE  encounter view, formation strip, combat log, affix chips, speed toggle
  PARTY   roster list, formation board (the only place heroes are moved), hero card: stats now, gear/abilities later
  GROW    upgrades, abilities, relics, automation (one segmented control, data-driven lists)
  ASCEND  ascension (L1), transcendence (L2), prestige trees, milestones
  SHOP    gem sinks, rewarded ads, season pass, no-ads
Overlays: offline popup (modal, own canvas), return hub, toasts, confirms, dev overlay (F3)
```

Rules: max 5 nav items, max 1 tab level inside a page, every list scrolls, every action toasts a confirmation,
close is always top-left, primary button reachable one-handed (1080x1920 reference, safe-area aware).

## 3. Battle screen (multi-enemy + formation)

```
┌ header: gold | gems | stage/wave | boost chip | speed ┐
│           ENEMY ROW   [E1] [E2] [E3]                  │  1-3 enemy views, HP bars, status icons
│                                                       │
│   BACK  FRONT                                         │  two vertical columns, FRONT nearest the enemy,
│   [H4]  [H1]                                          │  positions stacked downwards; nothing to tap here
│   [H5]  [H2]                                          │  (display only - swapping happens on the PARTY tab)
│                                                       │
│ combat log (scrollable, per-enemy attribution)         │
│ affix chips (opt-in difficulty)      [SPEED x1/x2]     │
└ nav ──────────────────────────────────────────────────┘
```

- Damage numbers: pooled + aggregated per target (3 heroes x 3 enemies would be 9 streams), capped per second.
- Statuses: up to 3 icons per unit with stack count; tap a unit for a bottom-sheet inspect (combat keeps running).
- Formation swap happens on the **PARTY** tab only: tap hero, then tap any slot (occupied = swap, empty = move, the
  old slot is vacated). No drag, no auto-arrange, no presets - position is the player's call (Step 10d).
- Defeat feedback: the party falls back one stage and resumes by itself after `defeatPauseSeconds` - no banner, no
  button, no waiting for the player. The stage label simply moves down one and the log says why.

## 4. Return hub & offline claim (B1)

The offline claim is a **modal**: its own canvas at `sortingOrder 100`, so nothing on the HUD (or the damage
canvas at 10) can draw over it. A full-screen scrim dims the game and swallows every click behind it; the panel
sits centred on top. `root` is the scrim, so one SetActive shows dim + dialog together. Labels use TMP ellipsis -
with the default overflow a long line spills into its neighbours and the dialog reads as overlapping text.

```
Modal canvas (order 100)
└ Scrim (black 65%, blocks input)              <- root
  └ Dialog (centred panel, 8-92% x 30-74%)
      "Welcome back!"                          title
      "You were away for 3h 00m"               time
      "+5.6K gold"                             reward
      "Capped at 2h 00m of battle income (8h max away)."   note
      [CLAIM]                                  the only action
```

```
Offline popup:  "You were away for 3h 00m"  +5.6K gold  (+shards when unlocked)
                Capped at 2h 00m of battle income (8h max away).
                [CLAIM]   [WATCH AD x2]      ad button only when a placement is available
Return hub:     Expedition #1 done -> COLLECT     [START ANOTHER]
                Bounty: choose 1 of 3 -> [PICK]   (gem/ad reroll)
                -> [BACK TO BATTLE]
```

Order: claim (instant payoff) -> dispatch (set and forget) -> dismiss. Never more than 3 taps to leave.

## 5. Wall guidance (B2, C7)

**Rule (locked after the loop beat):** player-facing text is a state the player can already see - cost, seconds,
wave, percent. **No invented index.** There is no "you need ~X more power" banner: that number cannot be seen in
the game, and quoting it as an ETA is a promise we cannot keep.

| Situation | UI |
|---|---|
| Something affordable | The buy button lights up. That is 90% of guidance |
| Nothing affordable | ETA on the button: cost / measured gold per second ("next ATK: 340 gold - 12s") |
| Losing attempts | Attempt progress: "wave 8/11" or the enemy HP bar. The gap shrinking is the guidance |
| Ascension affordable | Preview chip: "Ascend now: +14 tokens (push 3 stages: +17)" |
| Idle > 5 min with nothing buyable | Nudge: nearest affordable upgrade highlighted + toast |

Concrete numbers only (gold, seconds, wave, cost) - never one opaque "power" value (C7).

## 6. Team & hero detail

```
TEAM: roster grid (owned + silhouettes) | formation board | presets | auto-arrange
HERO: icon, role/tags, stars, stat rows (from StatDefinition), ability loadout + levels, relics,
      "next upgrade" cost + effect delta, [LEVEL UP] [EQUIP] [REORDER ABILITIES]
```

Ability loadout: tap slot -> pick owned ability; reorder auto-cast priority; locked slots show the unlock
condition ("2 stars", "zone 3").

## 7. Accessibility & localization (B12)

| Item | Rule |
|---|---|
| Colour | Log types and statuses distinguishable without colour (icon + text prefix) |
| Text | All strings from keys; hero/ability names come from data |
| Font scale | Settings multiplier (0.85 / 1.0 / 1.15) applied to TMP sizes |
| Tap targets | >= 88px at 1080p reference |
| Contrast | >= 4.5:1 on card backgrounds |
| Motion | Damage numbers + chips respect a "reduced motion" setting |
| One-hand | Primary action of every screen sits in the bottom 60% |

## 8. Performance budget (UI)

| Concern | Budget |
|---|---|
| Canvas rebuilds | no layout churn during combat except pooled rows/text |
| Damage numbers | <= 12 live views, aggregated per target, pooled |
| Combat log | Ring buffer, budgeted lines (5/s), pool of 36 labels, merge window 0.5s, auto-follow with scroll-back |
| Log contract | **One line per wave** (`-- Wave 5: Bat, Goblin --`, dupes as `Goblin x2`); every line names the *real* attacker (indices come from the events, never guessed); names are captured when the line is written so nothing renames itself; enemy suffixes A/B/C when a wave holds duplicates |
| Scrolls | virtualise beyond ~50 rows (roster, relics) |
| Battery mode (B11) | 30fps target, VFX trimmed, animations simplified, log budget halved |
| Dev overlay (F3) | runtime-built overlay canvas (sorting 500), top-left panel, 4 Hz refresh, dev builds only |

## 9. Screen work per step

| Step | Screens |
|---|---|
| 10 | Team/formation board, battle formation strip, presets, auto-arrange |
| 11 | Battle: 1-3 enemy views with per-enemy HP + status |
| 13 | Abilities list, hero loadout, priority editor |
| 15 | Affix chips, zone banner + milestone claim (the wall-guidance banner is **dropped** - see §5) |
| 17 | Return hub, roster/stars, expeditions, bounty cards |
| 18 | Transcendence screen, relic screen, codex |
| 20 | Settings (font scale, reduced motion, battery mode), localization pass |

## 10. Open questions

1. Bottom nav: 5 items at 1080 wide is tight - mock it before Step 10 (icon-only, label only on the active item).
2. Formation board: 2x3 is comfortable; a 5-hero (2+3) pattern needs design - Step 10.
3. Combat log: keep the scrolling strip, or a collapsible drawer once abilities add lines? Decide in Step 13.

