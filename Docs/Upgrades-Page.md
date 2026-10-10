# Upgrades Page - design + mobile layout pass (Phase 1)

*Started 2026-10-10. This is the design record for the UPGRADES page, the same way `Party-Page.md` is the record
for PARTY.*

Status: **Phase 1 built** (one row per upgrade, finger-sized buttons). Phases 2 and 3 are proposals (see §5).

---

## 1. Why this pass happened

The page was built for a mouse. Every control was smaller than a fingertip:

| Control (before) | Size (reference units) | In centimetres on a 1080p phone |
|---|---|---|
| Hero stat `+1` / `+10` | about 139 x 28 | about 1.0 x 0.2 cm |
| Automation toggle / buy | about 198 x 52 | about 1.4 x 0.4 cm |
| Ascend buy (BUY) | about 196 x 81 | about 1.4 x 0.6 cm |

The canvas is 1080 x 1920 (`match 0.5`), so one unit is about a third of a device pixel: **1 cm is roughly 114
units**. The smallest comfortable target is about 1 cm, or 48 dp, which is **about 144 units**. The old hero
buttons were a quarter of that height.

The cause was the layout, not the art: five stat tiles - each with a `+1` and a `+10` button - were crammed into
one 320-unit row.

## 2. The rule this page now follows

**One upgrade = one row, with two big buttons on the right.** `+1` buys one level, `x10` buys ten. The row face is
decoration, **not** a button (see the note below the table).

All sizes live in one place: `Assets/IdleRPG/Scripts/UI/UiTouch.cs`.

| Name | Value | Meaning |
|---|---|---|
| `UiTouch.MinTarget` | 144 | No tappable control may be smaller than this, in units |
| `UiTouch.RowHeight` | 180 | One upgrade row |
| `UiTouch.HeaderHeight` | 90 | A section header (a hero's name) |
| `UiTouch.ListHeight(n)` | n x 180 + gaps | Height of a stack of rows |

A row is laid out as: name (top left), effect (bottom left), level and cost (middle), then `+1` and `x10` on the
right. Both buttons are 0.15 of the row's width: 147 x 158 units on a 979-unit row, so both clear 144.

**Why the row face is not a button** (this changed on the same day - see §3a): uGUI fires a Button click on release
**even when the finger dragged**, as long as the press and the release land on the same button. Unity's input
module never clears that (`StandaloneInputModule` only compares the press/release handler). So a full-width row
button inside a scrolling list buys a level whenever a flick starts and ends on the same row. Two explicit buttons
keep a scroll gesture from spending gold.

## 3. What Phase 1 changed (as built)

**Hero sections** (`HeroUpgradeRowUI`, built by `MvpSceneBuilder.Panels.cs`)
- Five tiles became **five stacked rows**: ATK, HP, DEF, CRIT, CRIT DMG.
- Each row carries two finger-sized buttons: `+1` and `x10`. The row face is not clickable (see §2).
- Measured in Play: row **979 x 180**, each button **147 x 158**. Both pass `UiTouch.MinTarget`.

**Automation section** (`AutomationPanelUI`)
- It used to be a pinned strip across the bottom 24% of the page. Its buttons cannot reach 144 units inside a
  400-unit strip, so the section **moved inside the scroll** as the last section.
- The component now sizes itself: a heading row, one row per automation card, and one extra row for the auto-buy
  reserve dial. Measured in Play: section **1020 x 720** (four rows); the BUY button **182 x 148**.
- The dial's `-5%` / `+5%` buttons have a row of their own, so they are finger-sized too.
- Downside, stated plainly: automation is no longer always visible - the player scrolls to it.

**Ascend rows** (`PrestigeUpgradeRowUI`, built by the scene builder)
- Rows grew from 116 to **180** units and the BUY button fills the row height (about 162 units).

**Also touched**
- `UiTouch.cs` - new (the numbers above, with the reasoning in the header comment).
- `Docs` updated: `Checklist.md` §1aa, `Manual-Tests.md` §2g, `Party-Page.md` §7.4.

### Consequence: the page is taller

Content height is now about **4087 units** against a visible viewport of about 1430 - roughly three screens of
scrolling, because three heroes x five stats = fifteen rows. That trade is deliberate: hittable buttons beat a
short page. If the scroll feels long, the cheap fix is a **hero selector** (show one hero's five rows at a time);
that is Phase 3 in §5 and is *not* built.

### Scene rebuild required

Rows are **baked into the scene** by `Tools > Idle RPG > Build MVP Scene`. Changing row heights or adding rows
means rebuilding the scene, not just recompiling. (The 2026-10-10 rebuild failed silently once because the Editor
was in Play Mode - the builder refuses to run there.)

---

## 3a. Three bugs found on the same day (all fixed)

Reported after the first pass: "scrolling doesn't work", plus a `NullReferenceException` flood.

**1. `AutomationPanelUI.Refresh` crashed on every gold change.** `Refresh` touched `row.minusButton` /
`row.plusButton` without a null check. Only the auto-buy card gets a reserve dial, so every other card row had no
dial buttons at all. Damage was bigger than a red log line: the panel's `Build` threw before it marked itself
built, and the crash re-fired inside `CurrencyChanged` - which runs inside the combat tick that pays the gold - so
the whole tick was aborted ("Combat tick threw; skipping it"). Fixed with a null-guarded `Show(Button, bool)`
helper, the same shape as the file's existing `Label` helper.

**2. The lists could not be scrolled.** `UiFactory.CreateScrollView` puts a `ScrollRect` on a root that has no
graphic, and every graphic inside a list is non-raycast (row faces and labels are decoration). A press over a row
therefore hit **nothing**, so the EventSystem never delivered pointer-down and the `ScrollRect` never received a
drag. The machinery was right (content and viewport wired, vertical, elastic, sensitivity 25) - only hit-testing
was missing. It looked fine until now because the old content was about as tall as the window, so there was nothing
to scroll.
- **Fixed** in one place: `CreateScrollView` now adds a transparent, raycastable hit surface (`ScrollHit`) behind
  the rows. The `ScrollRect` is on that same GameObject, so a press anywhere in the list resolves to it. This fixes
  the UPGRADES, ASCEND and SHOP lists at once.
- **Side effect worth knowing:** the battle log's `LogScrollDragRelay` ("a real drag pauses auto-follow") was dead
  for the same reason - its `CombatLog` strip is the **parent** of the scroll, and uGUI looks for a drag handler
  going **up** from the pressed object. It now gets a press and works.

**3. The UPGRADES page never bound, so it showed placeholders and its buttons did nothing.**
`UpgradePanelUI.EnsureBound` accepted "the GameManager exists" as "ready". The panel can be enabled while the
manager is still loading a save, where `Upgrade`/`Resolver` are still null - it bound the rows to null and the old
`if (manager != null) return;` guard blocked every later attempt. Result: `effect` empty, cost `0`, and every tap
ignored (the row's `Buy` returns early while its manager is null). Fixed: the bind now requires `Upgrade` and
`Resolver`, and `RefreshAll` retries the bind (it is the method the live gold/purchase events call, so the page
comes alive on its own).

### Gotcha: do not trust layout numbers taken while the Game View is not rendering

Measuring this page from a script while the Editor sits unfocused gives **stale** rectangles: `Screen` reported
544x937 and later 1655x937 while the canvas stayed at 1080x1920, nested layout groups kept a pre-resize width
(38.4 instead of 979), and the automation section read 0 tall although its runtime `LayoutElement` said 720.
`Canvas.ForceUpdateCanvases` did not clear it, and manually calling `LayoutRebuilder.ForceRebuildLayoutImmediate`
on an outer group can leave the nested groups laid out at an intermediate width *and* clear their dirty flag.
Practical rule: **check layout with the Game View visible**, or force a rebuild on the specific group you want to
read - never trust a forced outer rebuild as proof that the inner groups are correct.

---

## 4. Currency rules (recorded, apply to every future upgrade here)

| Currency | Buys | Why this line |
|---|---|---|
| **Gold** | Power and comfort: hero stat levels, and future account/offline upgrades | Gold is the run-long resource. Every gold sink competes with hero stats - that is the interesting choice |
| **Rebirth tokens** | Multipliers (the automation cards) | Tokens survive a rebirth, so a token buys something permanent, not a number for this run |
| **Gems** | Shortcuts and cosmetics | Never required to progress; the game must be completable without them |

Rule of thumb for adding a track: **a global gold track costs one spec row, one effect type and one consumer.**
`PrestigeUpgradeData.CostCurrency` already carries `gold` (or `tokens`), and `TrackService.TryBuy` is the single
checkout, so no new purchase code is needed.

## 5. Backlog (proposals, not decisions)

**Phase 2 - account/global gold upgrades (the page's own content)**
This page is "UPGRADES", not "hero upgrades". With hero rows left as the only content, moving them later would
empty the page - so account tracks should land first.

| # | Track | Effect | Size |
|---|---|---|---|
| A1 | Offline window | More hours of gold banked while away | S |
| A2 | Offline efficiency | A larger share of run gold while away | S |
| A3 | Auto-buy speed | The auto-buy manager buys more often | S |

**Phase 3 - split hero stats away**
- Move the hero stat rows to the PARTY roster sheet (a chevron on the stat row, or a per-hero sheet).
- **Note an existing rule this reverses:** `Party-Page.md` §2.1 says the roster is *informational only and buying
  stays in Grow*. Moving buying there needs that line rewritten in the same commit, not quietly broken.
- **Prerequisite:** split `PartyRosterView` first (`Party-Page.md` §7.4 item S1) - it is one 500+ line view.
- Alternative if the scroll is still too long for one screen: a **hero selector** on UPGRADES (one hero's five rows
  at a time), which keeps the buying here and is smaller than a full move.

**Deferred by design**
- No opaque "power" number on this page: `UI-UX.md` §5 (C7) forbids it. Numbers must say what they do.

## 6. Open questions

- **Balance:** Stage 1 now takes about 330 s (target band 90-180 s) after the turn-based combat change. Unrelated
  to this page, but it changes how much gold a player has to spend here, so it is tracked next to this work.
- Do account upgrades read as *comfort* (quality of life) or as *power*? If they add power, they compete with hero
  stats for the same gold and the first purchase becomes a coin flip for a new player.

