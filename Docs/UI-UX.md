# UI-UX — screens, patterns, layout, accessibility

> Owns: everything the player sees and taps.
> Parent: `Architecture.md` (AD8, E7, E10, C7, D6) and `.clinerules`.
> **Location:** `Docs/` (folder index: `README.md`). **Last verified:** 2026-10-11 (the type scale, §1b - every label on one list, plus the guard that keeps it there).
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
| One overlay component | Every card that floats over the game is a `UiCard` on the modal canvas (§1a) - no screen builds its own panel |

### 1a. Cards - the one overlay component (2026-10-11)

Everything that floats over the game (ranks, defeat, offline claim, later the item sheet) is **`UiCard`**
(`Scripts/UI/Common/UiCard.cs`). The card builds a fixed set of parts, so every overlay looks the same and a page only
fills the middle:

| Part | Name | What it is |
|---|---|---|
| Root | the card's own name | **Stretched** over the overlay canvas, so every anchor inside is a fraction of the SCREEN |
| Scrim | `Panel` (first child) | full-screen dim + tap eater; tapping it closes the card |
| Face | `Panel` (second child) | one 9-slice sprite, anchored to the card's rectangle |
| Title | `Title` | the skin's display font |
| Content | `Content` | where the page builds its rows |
| Close | `CloseButton` | `X`; **hides** the card |

Rules:
- **The card owns show/hide** (`Show()` / `Hide()`). A page must never destroy a card's frame - that is exactly how the
  ranks sheet went dead after one close.
- **Cards live on the modal canvas** (`HudController.ModalRoot`, sorting 100), so they sit above the HUD and the damage
  numbers. The reference comes from the HUD, not from a scene lookup (E5).
- **The look comes from one asset**: `Resources/UiSkin.asset` - 9-slice faces, colours, text sizes. Swapping the UI art is
  meant to be that file. A missing asset is not an error: the code defaults draw flat-coloured cards.
- **Rows are pooled** (`Pool anything spawned`): build the slots once, then only fill them.
- **The 9-slice border is not set in code.** It comes from the sprite's import preset (12 px, `Art-Pipeline.md` §17), so a
  replacement PNG only has to be imported the same way.

Why this exists: the ranks sheet was hand-built on `UiDockedSheet`, its node was left at Unity's default 100x100 rect, so
the whole card measured **80x74 units on a 1080x1920 screen** (rows ~3.5 units tall, all overlapping), its close
destroyed the frame instead of the sheet, and it was parented to the base canvas (sort 0, so no scrim and the damage
numbers drew over it). Details and the live measurements: `Checklist.md` §1ae.

### 1b. The type scale - one list, no hand-written sizes (2026-10-11)

Every piece of text picks a **role**, never a number. The roles live in one file, `Scripts/UI/UiType.cs`:

| Role | Size | Font | What it is for |
|---|---|---|---|
| Display | 56 | Silkscreen | currency values, the reward number |
| Title | 48 | Silkscreen | card / screen / modal titles |
| Section | 40 | Silkscreen | section heads, button captions |
| Sentence | 36 | VT323 | running text: the battle log, effect lines |
| Damage | 32 | Silkscreen | floating damage (crit text is x1.3) |
| Small | 30 | Silkscreen | nav tabs, tags, costs, column heads |
| Micro | 26 | Silkscreen | captions - never something the player must act on |

Two rules hold it together:
- **Chrome is Silkscreen, sentences are VT323.** VT323's x-height is only 0.41 of its size, so it reads smaller
  than Silkscreen at the same number; it never carries a label the player reads at a glance.
- **Micro is the floor.** Nothing is smaller. On a 1080-wide phone one canvas unit is about 0.4 dp, so Micro's
  cap height is about 1.0 mm - the smallest text a phone can carry.

How a screen asks for text:
- Building the scene: `UiFactory.Text(parent, name, text, UiType.Role.Section, ...)`.
- Building a widget while the game runs: `UiRuntime.CreateText(parent, name, text, UiType.Role.Small, ...)`. A
  button caption takes a role too (`CreateButton(..., role)`); section is the default.
- Text whose length varies (nav tabs, hero names, item names, prices) uses `UiType.ApplyAuto(label, role)`: it
  starts at the role's size and shrinks towards Micro instead of spilling over its neighbour.
- The design labs (`UiLab.unity`, `ArtLab.unity`) use `UiFactory.RawText` on purpose - a lab compares looks, so it
  is allowed to pick a size by hand.

**The guard:** `Tools > Idle RPG > UI > Check Type Scale` walks every shipping scene and every prefab, reads each
label's size and font, reads the code for hand-written sizes, and prints PASS/FAIL. It also runs inside
`Run All Checks (regression)`, so the problem cannot come back one label at a time. (Seen to fail on purpose:
a prefab with a 21-unit label in the default font and one hand-written `fontSize =` line produced three errors.)

Why this exists: sizes were written by hand in about 90 places - 15 different numbers between 13 and 42 - so half
the game came out in Unity's smooth default font (Liberation Sans) at roughly half the size a phone needs, and no
two screens matched. Measured before: 136 labels in Liberation Sans. After: 0, and only 26/30/32/36/40/48/56 in use.
Full record: `Checklist.md` §1af.

### 1c. Containers and gaps - one ruler, no hand-written numbers (2026-10-14)

The same idea as the type scale, applied to the space between things. Every gap picks a **role** from
`Scripts/UI/UiSpace.cs` (the ladder: None 0, Hairline 2, Tight 4, Snug 6, Small 8, Base 10, Medium 12,
Gutter 14, Large 24; plus roles such as `ScreenMargin` 14, `CardPadding` 12, `RowGap` 8).

The structure underneath: three layers (main, damage, modal), every overlay on a **safe box**, two
**fixed-height** chrome bars (header 115, nav 230) with the page stretched between them, and
`matchWidthOrHeight = 0` (match width) on every canvas, so taps and text stay the same on every phone and a
taller phone shows more content.

**The guard:** `Tools > Idle RPG > UI > Check Spacing` fails on a hand-written gap (a non-zero inset, a
hand-built `RectOffset`, a numeric scroll / stack spacing). It runs inside `Run All Checks (regression)` too.

**The full picture - containers, the ladder, the mobile decisions, the measured numbers and the known
exceptions - is in `UI-Layout.md`.** That file owns spacing; this one owns screens and text.


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
- Swing timing: the sim announces a swing and lands the damage `SwingDurationSec` later (0.35 s). The unit starts its
  **attack clip on the announcement** and runs in at the same time, so the strike frame lands on the hit instead of
  the hit starting the animation. The clip finishes during the pose, then the unit blinks home.
- Defeat feedback: the party falls back one stage and resumes by itself after `defeatPauseSeconds`. A defeat card
  shows the run so far with a countdown plus optional CONTINUE / ASCEND buttons (Run-Identity pass) - the run still
  never waits for the player.

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

> The full party vision, information architecture and phasing now live in **`Docs/Party-Page.md`**
> (`ROSTER | FORMATION` sub-nav; Inventory/Abilities arrive later). The sketch below stays as the summary.

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

