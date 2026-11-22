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
| Sentence | 36 | VT323 | effect lines outside the battle log |
| Dialogue | 48 | VT323 | every battle log line, including combat. Speaker names are gold; body text is near-white |
| Damage | 32 | Silkscreen | floating damage (crit text is x1.3) |
| Small | 30 | Silkscreen | nav tabs, tags, costs, column heads |
| Micro | 26 | Silkscreen | captions - never something the player must act on |

Two rules hold it together:
- **Chrome is Silkscreen, sentences are VT323.** VT323's x-height is only 0.41 of its size, so it reads smaller
  than Silkscreen at the same number; it never carries a label the player reads at a glance.
- **The battle log is all Dialogue 48.** Speaker names are gold (`AccentLight`), the words are near-white, and
  lines have a 4-unit gap. The name is stored apart from the words so only the name changes colour.
- **Story text types.** The log switches to one-line story mode. The speaker name appears at once; the words
  reveal at 45 characters per second in real time. `TAP TO CONTINUE` completes the line; the next tap advances. `SKIP SCENE` is a separate button. Combat lines never type. A scene end or skip shows the rest.
  The store, view, colours and layout contract live in `Battle-Log.md`.
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

The structure underneath: three layers (main, damage, modal), every overlay on a **safe box**, one
**fixed-height** chrome band (the header, 115) with the page stretched under it. The old 230-unit bottom
nav bar is retired, so a page now runs from just under the header to the bottom of the safe area (a
`UiSpace.Large` gap keeps content off the screen edge). Every canvas also uses
`matchWidthOrHeight = 0` (match width), so taps and text stay the same on every phone and a taller phone
shows more content.

**The guard:** `Tools > Idle RPG > UI > Check Spacing` fails on a hand-written gap (a non-zero inset, a
hand-built `RectOffset`, a numeric scroll / stack spacing). It runs inside `Run All Checks (regression)` too.

**The full picture - containers, the ladder, the mobile decisions, the measured numbers and the known
exceptions - is in `UI-Layout.md`.** That file owns spacing; this one owns screens and text.


## 2. Navigation model

**Shell today (2026-11-21):** the lobby is the **between-runs** screen - `SHOP / PARTY / BATTLE / ASCEND / TRAINING`
on its own canvas, with the shared header borrowed from the battle page. The old baked 5-tab shell (BATTLE / PARTY /
UPGRADES / PERKS / SHOP) and its management page are **retired** and no longer built into the scene.

| Tab | State | What it does today |
| --- | --- | --- |
| BATTLE | live | begins the run; same path as START. |
| PARTY | live | read-only roster: hero cards, the selected hero's stats, gear and ability slots |
| ASCEND | live | the perk shop: token tracks, one level per tap |
| TRAIN | live | the gold tree (three heroes) plus the party track |
| SHOP | live | lean shelf: No Ads, 30 Gems, Starter, Golden Foundry. Restore lives in the gear settings. |
| Header gear | live (both) | **Lobby:** SETTINGS (sound, restore, privacy, version). **In a fight:** PAUSE (resume, sound, quit → confirm). Never an upgrade screen. |
| No first-run hints | by design | The lobby teaches START; no toast tutorials. |

> **As-built correction, 2026-11-21.** The lobby never covers a live fight; it is the between-runs screen. A run
> is active only in `Combat`/`Boss`. Closing the app mid-run reopens into the same fight. A minimal pause is
> allowed, but it offers no upgrades: no shop, no training, no ascend. Full map: `Routes-and-Flows.md`.

**The level-up card** is an overlay, not a page: it owns the screen while a pick is owed (fighter paused), shows the
three boons, and closes itself when the last pick is spent. `LEVEL UP!` / `CHOOSE A BOON - N PICKS WAITING`, then one
row per card (rarity tag, badge, name, who it helps, what it gives) and `REFRESH n/n` at the bottom.

**Deliberately no boons page** (decision 2026-11-09): boons are run-scoped, so there is nothing to browse between runs.
The hand lives on the level-up card only, and its written record is the battle feed: every pick appends
`Boon: <name>` in the system colour.
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

### 2a. Start screen (lobby) - one header, one nav

- The lobby is a full-screen page on its own canvas above the battle HUD. It **reuses the real header**
  (`HudHeaderUI`): `StartScreenUI` re-parents that object into the lobby canvas while the lobby is open and puts it
  back before a run starts (`DetachSharedHeader`). No second currency bar is drawn anywhere.
- **The bottom nav shows the open tab**: each tab carries an icon; only the selected tab shows its word. The selected
  tab lifts, wears the brighter blue face and shows icon + label centred (see §2b). Opening TRAIN highlights TRAIN;
  going back to BATTLE highlights BATTLE.
- **Runtime menus use the type scale and the touch rule.** Every label picks a `UiType` role and every control clears
  `UiTouch.MinTarget` (144) - no hand-written sizes, and the fonts come from `UiTheme` (Silkscreen chrome, VT323
  sentences), not the TMP default. Glyphs are drawn as sprites when the font lacks one (arrows).
- **The party XP bar sits between the arena and the ability strip**: one 44-unit band built by the scene builder.
  `LV n` and `xp/needed` are packed together on the left (`UiSpace.Small` between them, `UiSpace.Medium` before
  the bar), and the blue bar takes every unit that is left, up to the shared right margin. The numbers always show;
  the level-up card owns the pick count. The fill glides, steps up as blue motes land from the enemy that paid (3-5
  per kill, coin-sized), and flashes once when a level lands. The arena starts above the band, so the bar can never
  sit over the fight.
- **One page at a time.** Every tab closes the other lobby pages before it opens its own, so two pages can
  never stack. All of them sit in the same box (`0.02-0.98` x `0.13-0.885`) and are opaque, so the lobby card
  behind them cannot show through.
- Pages that are not built yet (SHOP alone) show a one-line status note instead of an empty page.

### 2b. Bottom tabs: icons, text only on the selected tab (2026-11-12)

- The five bottom tabs are drawn by `FantasyTabButton` on the chunky `ui_frame` plate.
- **Icons** come from one sheet, `Art/Button Icons.png`, imported as **Multiple** sprites: `icon_shop` (bag),
  `icon_party` (three adventurers), `icon_battle` (crossed swords), `icon_ascend` (arrow + star), `icon_train`
  (dummy). Reading order on the sheet is battle, ascend, train, shop, party.
- **Idle tab:** icon only, large, centred. **Selected tab:** the icon lifts/shrinks and the word appears centred
  beneath it. The word is hidden by fading its alpha, so switching tabs eases instead of snapping.
- **Tuning** (plain numbers; the button holds no magic):
  - `StartScreenUI.TabIconScale(tab)` - idle size (`1` = normal; `PARTY 0.97`, `ASCEND/TRAIN 0.94`).
  - `StartScreenUI.TabIconSelectedScale(tab)` - selected size (`PARTY 1.03`, `BATTLE 1.10`).
  - `StartScreenUI.TabIconVertical(tab)` - idle upward nudge as a fraction of icon height (`BATTLE 0.07`,
    `ASCEND/TRAIN 0.03`). Positive = up.
  - `StartScreenUI.TabIconSelectedVertical(tab)` - selected nudge (`TRAIN 0.07`).
  - `FantasyTabButton.Update` holds the base shape: `0.66` idle height, `0.50` selected height, `0.19` idle bottom,
    `0.35` selected bottom (fractions of the tab height).
- The five sprites are wired on `StartScreenUI` (`tabShopIcon` ... `tabTrainIcon`); the scene builder sets them too,
  so a rebuild keeps them.


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
- Formation swap is a **battle tactic**: tap a hero on the battle board (he is held), free pads brighten, tap a free
  pad and he slides there. Any other tap drops the hold and nothing moves. The move re-stamps the live fight at once,
  so the next enemy swing and every row-gated move already obey it. No drag, no auto-arrange, no presets - position is
  the player's call (Step 10e).
- Swing timing: the sim announces a swing and lands the damage `SwingDurationSec` later (0.35 s). The unit starts its
  **attack clip on the announcement** and runs in at the same time, so the strike frame lands on the hit instead of
  the hit starting the animation. The clip finishes during the pose, then the unit blinks home.
- Defeat feedback: a party wipe **ends the run**. The run-over card shows the depth and the Perk Points, with
  **REVIVE** only when Dawn Ward is owned and unused, and **END RUN** always. No countdown, no auto-resume, no ad.

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
| Shared button / tab tween | a settled widget writes nothing (compare first, write last). Measured 2026-11-15, TRAIN page, 1,374 buttons: **1.43 ms -> 0.32 ms** per full pass (Editor, Mac). 1,364 of 1,364 end exactly on target |
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

1. ~~Bottom nav: 5 items at 1080 wide is tight~~ **Done 2026-11-12:** each tab shows its icon only; the selected tab
   adds its label beneath the icon (see §2b).
2. Formation board: 2x3 is comfortable; a 5-hero (2+3) pattern needs design - Step 10.
3. Combat log: keep the scrolling strip, or a collapsible drawer once abilities add lines? Decide in Step 13.



## 11. The palette: three layers (2026-11-15)

The old palette had 94 tokens and about 40 distinct colours, with 19 near-identical blues and 19 golds
doing the same jobs; most had no doc, so nobody knew which to pick. It is now three layers:

1. **Primitives** - the raw ramp, 10 values. Screens never use them. Slate 950/900/800/700/600/500/400
   (`#0B0F15` -> `#41506F`) and Gold 600/500/300.
2. **Semantic tokens** - 40 names, the only ones a screen may use: `SurfaceBase/Raised/Card/Sunken`,
   `BorderSubtle/Strong`, `ActionFace/Selected/Pressed/Ink`, `InkHigh/Medium/Low/Off/OnGold`,
   `AccentBase/Bright/Glow`, `StatusOk/Warn/Bad/Info` (+ Soft/Deep/Text/Epic), `XpFill/FillLight/Trail/Flash`,
   `ShadeBar`, `VeilSoft/Strong/Trace`, `StageTop/Mid/Bottom`.
3. **Legacy aliases** - every old name (`Panel`, `ButtonBlue`, `GoldSoft`, `Danger`, `HpHealthy`, ...) still
   exists, each pointing at one semantic token. Nothing broke when the values changed, and the whole app
   shifted in one pass.

Rules:
- A screen says what a colour is FOR, never a hex value.
- Do not add a new use of a legacy name; the alias sweep deletes them screen by screen.
- One meaning, one tone: gold = value, green = healthy/ready, amber = waiting/wounded, red = danger,
  blue = information. If a new thing needs a colour, it borrows one of those meanings.
- UiColourCheck (registered in Run All Checks) fails the run on a hand-written constant colour in UI code.
  **Known gap:** it scans code only - the scene and the three UI prefabs are swept by hand, so authored
  Image colours can still drift. Extending the check to assets is the next step.


## 12. One button, one plate (2026-11-15)

New buttons use `UiPlateButton.Create`: the shared `ui_frame` 9-slice face, an idle/selected colour
hand-off, a press squash and a true grayscale disable. The face is `ActionFace` (`#2A3A57`) - deliberately
darker than the old `#385280`, so a button sits with the panels instead of on top of them.

Two changes make the plate read as a button rather than a slab:
- **`pixelsPerUnitMultiplier = 2.5`** on the face: the sprite's 18-pixel border draws as ~7 units.
- **Pressed darkens the face it already has** (`Color.Lerp(face, black, 0.18)`), so a gold button darkens
  when pressed instead of flashing navy.

`FantasyTabButton` keeps its lift and halo for the bottom tabs. Panels come from `UiRuntime.Plate`; small
chips stay flat on purpose.

### 12a. Action button art: three faces, one source (2026-11-22)

The owner drew three 89x19 bar faces: **primary** (gold), **danger** (red) and **neutral** (slate).
They live in `Assets/IdleRPG/Art/UI/Buttons/` and are referenced **only** from the `UiSkin` asset
(`Assets/IdleRPG/Resources/UiSkin.asset`). That asset is the single source: art, ink colour and border scale.

| Face | Means | Used today |
|---|---|---|
| `ButtonFace.Primary` | The main action on a screen | START, RESUME, CLOSE, shop/training BUY (next step) |
| `ButtonFace.Danger` | An action that costs or ends something | QUIT RUN, END RUN |
| `ButtonFace.Neutral` | A secondary action | SOUND, RESTORE, PRIVACY, KEEP FIGHTING |
| `ButtonFace.Plate` | The old tintable plate | Cards, tabs, nodes, every button the skin has no art for |

Rules:
- `UiPlateButton.Create(..., ButtonFace face = ButtonFace.Plate)`. Callers ask for a **meaning**, never a sprite.
- **`Image.pixelsPerUnitMultiplier` DIVIDES the slice border.** The art's 1 px highlight and 3 px shade become
  7 and 21 units at `1f / 7f` (`UiSkin.buttonBorderScale`). A value of 7 made them invisible once - do not repeat it.
- Art with baked colour is drawn **white-tinted** (`Color.white`); any tint multiplies the artist's colour.
- Import preset: `Tools > Idle RPG > Art > Apply UI Art Presets` (FullRect, Point, uncompressed, border
  measured per folder, RGBA32 on Android and iPhone). Never compress these: a 1 px line dies under ASTC 4x4.
- A missing skin sprite falls back to the plate, so an empty skin never draws an invisible button.

**Bottom tabs (2026-11-22).** `FantasyTabButton` draws the same two faces: **neutral** when idle, **primary**
when selected. Icons keep their own colours in both states and carry a dark drop shadow (`0.55` at `-2`) so
gold art still reads on gold. Wide icons are capped at **96%** of the tab width, aspect kept.

Tab clean-ups (2026-11-22), after the owner saw a ghost of text on idle tabs:
- The word is **hidden outright** when a tab is not selected (`SetActive`, not alpha 0). The old fade left a
  faint line of text during the transition, and an invisible label still cost a draw.
- **Idle icons are centred, then lifted**: `(tabH - height) / 2 + height * (iconNudge + IdleIconLift)`. The common
  lift is **0.09** after the owner asked for the row higher; per-icon `TabIconVertical` values are **measured from
  a 1080x1920 capture** (bag -0.032, arrow +0.011, dummy +0.043, party 0), because each raw sprite carries its
  own offset inside its sheet cell. Result: four idle icons within 0.5 px of each other, ~9.5 px above the bar's
  box centre - about 2.5 px above the centre of the visible face (the face sits high because of the 21 px
  bottom shade). `IdleIconLift` is the one knob for the whole row.
- The tab word uses `UiType.Role.Small`; the old hard-coded `36` was off the type scale.
- The selected halo **switches its Image off** once it has faded to zero, instead of drawing a transparent quad.

**Party tab icon (2026-11-22).** Its own file: `Art/UI/Icons/icon_party.png` (75x58, owner art, third cut -
the two earlier cuts were 52x19 and 90x50; both read too small). Idle it draws at **88%** of the tab height
with a **+4% lift** (`TabIconScale` / `TabIconVertical`): the three figures stand on their bottom edge, so a
true box centre reads low. `MvpSceneBuilder` prefers this file and falls back to the old sheet sprite.

**BATTLE tab (2026-11-22).** The tab returns to the lobby centre. It no longer begins a run - only the START
button does. Before this, pressing BATTLE started a fight, which the owner reported as wrong.

**Panel face (2026-11-22).** `UiSkin.sheetSprite` holds the owner's 75x75 frame (`Art/UI/Panels/panel_pause.png`,
5 px rim, baked colour, gold top-left and brown bottom-right shading). The pause/settings sheet draws it white
at `sheetBorderScale` (`1f / 7f` = 35 units of rim). Other panels still use `ui_frame` until the owner says
which ones change.

**Pause sheet layout (2026-11-22).**
- Panel: x 0.10-0.90, y 0.20-0.80. Was y 0.14-0.86 - the empty band at the foot is gone.
- Ribbon: `UiSkin.bannerSprite` (`Art/UI/Banners/panel_banner.png`, 95x23, border 8/0/8/0, baked colour).
  It is a child of the panel, anchored to the top edge, pivot centred, height = art height x `bannerScale`
  (23 x 7 = 161 units), so half sits above the edge and half covers it. Width is **75% of the panel**
  (anchors 0.125-0.875). It carries **GAME PAUSED** in a run and **SETTINGS** in the lobby; the plain title
  label stays only as a fallback for a skin with no ribbon.
- Ribbon shadow: two stacked `UnityEngine.UI.Shadow` copies on the ribbon image (black 0.45 at -10, black
  0.22 at -24). One hard copy read as a second ribbon; two steps read as depth over the panel's top rim.
- **Ribbon word centring is measured, not guessed (2026-11-22).** The label box covers the ribbon's **red face**
  (anchors 0.30-0.96, the face spans art rows 1-16 of 23). TMP centres the full line box, so capitals sit
  high; `BannerTextDrop(font, size)` derives that gap from the font's own `capLine` / `ascentLine` /
  `descentLine` and scales it to the size (5.63 units at Abaddon 45). `BannerTextNudge` adds the measured
  remainder, **2.5**, so the capitals centre on the band. Result: 0.5 px off, measured on a 1080x1920 capture.
  Both parts scale with the font, so the font step cannot silently break it.
- Sheet buttons carry the same two-step drop shadow, stronger: black 0.38 at -5, black 0.18 at -12
  (`ButtonShadowNear` / `ButtonShadowFar`).
- Clicking **SOUND** writes no status line: the button label already says ON / OFF. The foot of the sheet is
  reserved for the lobby version and for Restore / Privacy feedback.
- No body line in a run. The status label hides when empty (`SetActive(false)`); the lobby still shows the
  version there, at the foot of the sheet.
- Rows are placed by `Place(index, count)`: run = 3 rows, lobby = 4 rows. The old hand-written anchors put
  **SOUND and PRIVACY on the same slot in the lobby**; the count now prevents that class of bug.
- Danger ink is **white** (`236,240,248`) in the skin default and in the asset. White on that red is about
  3.4:1 - fine for the large bold label, below AA for small text.

**Still on the old plate:** tabs, ability cells, training nodes, camp choice cards and card close buttons.
They are icon or multi-line components, not text action buttons, so a 19 px bar face does not fit them.

**Not converted yet:** the defeat card (REVIVE / END RUN) and the offline claim dialog are built into
`Main.unity` by `MvpSceneBuilder.Overlays.cs` and still point at the placeholder `ui_button_gold` /
`ui_button` sprites. They need a scene rebuild, so they are a separate step.


## 13. Ability strip cells: a card, picture left, words right (2026-11-15)

Each cell under the fight is one hero's special move, drawn as a small card. A 170px picture of the move fills the
card's whole height on the left (the 34px art x 5 - a whole-number zoom, so no pixel column lands half a pixel
wide). To its right is a column of words: the hero's name in one `UiType.Small` line, the move's name in
`UiType.Micro` under it, and the 6px state line under both. The line spans the words column only, so it reads as
an underline for the move rather than as a second widget.

| State | Line | Picture and words |
|---|---|---|
| Ready | full green | full colour; move's name `TextDim` |
| Resting | gold, growing left to right (length = time left) | grey art with colour returning from the left; move's name `TextMuted` |
| Press waiting | full amber | full colour |
| No ability / dead | no line | picture and move's name hidden, plate greyed |

Rules that keep it honest:
- The plate never changes colour for a cooldown; only a true lock greys it.
- Colour is a second cue, never the only one: a resting cell also loses length and dims its picture and its move
  name, so the state survives a colour weakness or a bright screen.
- The hero's name is identity, not state: it stays at `UiTheme.Text` through a whole cooldown, because it is the
  only thing that tells three cards apart at a glance. It goes away with the rest of the card when the hero is down.
- The move's name box is always two lines tall and top-aligned. A name that wraps ("Holy Slash" on a three-hero
  strip) must not push the state line, or the three state lines would stop lining up across the row.
- The line is the only clock; there are no seconds to read.
- Gaps follow one hierarchy, not one number: 2 (`Hairline`) joins the hero's name to the move's name - one block of
  words; 8 (`Small`) separates the words from the state line, and the same 8 separates the picture from the words
  and the card's edge from its content. Every number is a `UiSpace` ladder step.
- Bar length is done with anchors, never `Image.fillAmount`: these bars carry no sprite, and Unity ignores
  fillAmount without a sprite (this is why the line once sat full through every cooldown).
- The drawn length eases toward the fight's value over about 60 ms, so the fight's 0.05 s ticks read as a glide
  instead of a stutter.
- The picture's colour never fades in. It is a second, full-colour copy of the same art, pinned at the picture's
  left edge under a `RectMask2D` curtain whose width IS the countdown. Colour is therefore revealed left to right,
  and the art is cropped rather than squashed - `fillAmount` cannot do this for a sprite it must keep in proportion.

Costs, stated plainly: the card is 340 x 186 instead of 340 x 168, so the strip band grew from 0.28 to 0.27 of the
page and the battle log gives up 18 units (about one line - it scrolls). On a three-hero strip the words column is
only 146 wide, so "Holy Slash" wraps to two lines; with two or one hero the cards are wider and it does not.

