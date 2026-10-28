# UI-Layout — containers, spacing, and mobile scaling

> Owns: the structure under the UI (layers, safe boxes, bands) and every gap number.
> Parent: `UI-UX.md` (screens, patterns, type scale) and `Architecture.md`.
> **Location:** `Docs/` (folder index: `README.md`). **Last verified:** 2026-10-28 (live scene, measured).
> Companions: `UiType.cs` (text), `UiTouch.cs` (fingers), `UiSpace.cs` (gaps).

---

## 1. Why this file exists

Three lists already kept the UI honest: **sizes** (`UiType`), **finger targets** (`UiTouch`) and **look**
(`UiTheme` + `UiSkin`). Gaps had no list. Numbers were picked by hand in about 60 places — eight different
values (0, 2, 4, 6, 8, 10, 12, 14) — and two panels doing the same job padded **14/6** and **10/10**, so nothing
lined up.

Two other faults came out of the same audit:

1. **Popups and cards were not safe-area fitted.** The main canvas stepped around the notch; the modal canvas
   did not. Cards sat mid-screen, so nothing was clipped *yet* — it was latent, and it would have bitten the
   first tall card.
2. **The two bars were screen fractions** (header 6%, nav 12%). On a tall phone the extra height was split
   between the bars and the page; on a short screen the top bar could fall below the 56-unit currency numbers.

## 2. The container model (as built)

Three stacked layers. All three use the same pretend screen, 1080 × 1920.

| Layer | Canvas | Order | Safe box | Takes taps |
|---|---|---|---|---|
| Main | `/HUD/Canvas` | 0 | `/HUD/Canvas/SafeArea` | yes |
| Damage | `/HUD/DamageCanvas` | 10 | **none, on purpose** | no |
| Modal | `/HUD/ModalCanvas` | 100 | `/HUD/ModalCanvas/SafeArea` | yes |

Every safe box is a `SafeAreaFitter` node, authored **stretched** so Edit Mode reads as the no-notch case.
At runtime the fitter tightens it to `Screen.safeArea` (and only re-applies when the safe area changes).

**The damage layer needs no safe box.** `FloatingDamageTextPool.Spawn()` reparents each number to
`anchor.parent` — the battle viewport, which is already inside the main safe box. Only the idle pool sits on
the damage canvas, and nobody sees it. Adding a box there would change the coordinate space for no gain.

**The main safe box holds five children:** `Header`, `BattlePage`, `ManagementPage`, `NavBar`, `Toast`.

### 2a. The two chrome bars are fixed heights

| Band | Anchor | Height (units) | Rect height |
|---|---|---|---|
| Header | pinned to the top, 14-unit gap | 115 | 101 |
| Page | stretched **between** the bars | — | 1575 |
| NavBar | pinned to the bottom, 10-unit gap | 230 | 220 |

The values reproduce the 1080 × 1920 design, so nothing shifted on a 9:16 screen (measured before/after:
header 101.2 → 101, nav 214.4 → 220, page 1574.4 → 1575). The nav gained 6 units because its odd 6-unit top
inset was dropped.

**One rule now: extra height belongs to the page, the bars never move.** `PinBar` in `MvpSceneBuilder.cs`
builds both, so neither can change size with the screen.

### 2b. Bands inside the battle page

Fractions of the page (so they follow whatever height the page gets): stage/wave row **0.93–0.99**, viewport
**0.39–0.93**, log/abilities tab strip **0.28–0.39**, log or abilities panel **0–0.28**.
Source: `LogTop = 0.28f`, `BattleViewportBottom = 0.39f` in `MvpSceneBuilder.cs`.

### 2c. The header is a row, not hand-placed chips (2026-10-14)

The currency chips and the RANKS button live in one `ChipRow` under the header, driven by a
`HorizontalLayoutGroup`. The three chips carry `LayoutElement.flexibleWidth = 1` and divide the width; the RANKS
button takes a fixed 110-unit slice at the end.

Why: they used to be placed by hand — the chips at 0.03 / 0.35 / 0.65 of the header, and the runtime RANKS button
at 0.88 — and the token chip and the button **overlapped by about 95 units**. That is how a long currency value
ended up sliding under the button. Two numbers, tuned separately, never compared.

Measured in Play (row width 1004): Gold 38–328, Gems 336–626, Tokens 634–924, RANKS 932–1042 — all 8 units
apart, 77 tall, no overlap.

The boost chip stays on the **header**, not in the row: it is an overlay badge, not a fourth currency, so the
layout group must not size it.

### 2d. Page bands have names (2026-10-14)

The management panels used to place their bands with bare fractions. They are now named once, at the top of
`MvpSceneBuilder.Panels.cs`:

| Constant | Value | Used by |
|---|---|---|
| `BandTop` | 1.0 | all |
| `BandTitleBottom` | 0.88 | ASCEND + SHOP |
| `BandSubLineBottom` | 0.79 | ASCEND + SHOP |
| `BandActionTop` / `BandActionBottom` | 0.77 / 0.60 | ASCEND + SHOP |
| `BandListTop` | 0.57 | ASCEND + SHOP |
| `BandUpgradesTitleBottom` / `BandUpgradesListTop` | 0.945 / 0.93 | UPGRADES (it has no action band) |

A band change is one edit, and the two mirrored screens cannot drift apart. It was a **pure rename** — no pixel
moved.

**Bands stay fractions on purpose.** A band is a proportion of a flexible page, not a gap in canvas units, so it
is not on the `UiSpace` ladder. On phones this is safe: match-width scaling means the page is **never shorter
than its 1575-unit design height** (every phone is at least 16:9 tall), so a fraction band is never smaller than
designed. It would bite on a short, wide screen — a tablet — see §3. If the v1.1 font-scale or localization pass
ever overflows a band, convert *that band* to content-sizing. Do not build a container system for it.

## 3. Scaling: match WIDTH

All three canvases (and the UI lab) use `ScaleWithScreenSize`, reference **1080 × 1920**,
`matchWidthOrHeight = 0`.

**Why.** Every fixed size in the UI is calibrated to the 1080-wide reference, and every band is flexible.
Matching width therefore keeps taps and text the same on every phone, and a taller phone shows **more content**
instead of bigger bars. The alternative — a width/height blend (`0.5`) — does half of each bad thing and
guarantees neither dimension.

| Screen | Match width (chosen) | Match height (rejected) |
|---|---|---|
| Tall phone 20:9 | canvas 1080 × 2400 — bars fixed, page grows | canvas 864 × 1920 — width shrinks 20%, header/buttons crowd |
| Tablet 4:3 | canvas 1080 × 1440 — bands shorter, page scrolls more | canvas 1440 × 1920 — comfortable |

**Downside, stated plainly:** on a short, wide screen (a tablet in portrait) the bands get less height than
designed, so pages scroll more. Phones are the target. If tablets ever matter, the fix is to cap the canvas
width at the design width — a separate job.

## 4. The three lists — pick a role, never a number

| List | Owns | File |
|---|---|---|
| `UiType` | text sizes + fonts (7 roles: Display 56 … Micro 26) | `Scripts/UI/UiType.cs` |
| `UiTouch` | finger sizes (`MinTarget` 144, `RowHeight` 180) | `Scripts/UI/UiTouch.cs` |
| `UiSpace` | gaps (the ladder + the roles) | `Scripts/UI/UiSpace.cs` |

`UiTouch.RowGap` points at `UiSpace.RowGap`, so the ladder owns the one row-gap number.

### 4a. The ladder

| Step | None | Hairline | Tight | Snug | Small | Base | Medium | Gutter | Large |
|---|---|---|---|---|---|---|---|---|---|
| Units | 0 | 2 | 4 | 6 | 8 | 10 | 12 | 14 | 24 |

The ladder **contains every value that was already in use**, so moving a call site onto a role is numerically
identical and cannot move a pixel.

### 4b. The roles

| Role | Value | What it is for |
|---|---|---|
| `ScreenMargin` | 14 | the gap from the safe-area edge to a bar — every bar shares one edge |
| `SectionGap` | 8 | between two big sections on a page |
| `RowGap` | 8 | between two rows in a list |
| `CardPadding` | 12 | from a card / dialog edge to its content |
| `LabelPadX` / `LabelPadY` | 8 / 4 | a label inside a button |
| `ChipPadX` / `ChipPadY` | 4 / 2 | a label inside a small chip (currency, boost) |

Helpers: `UiSpace.Padding(all)` and `UiSpace.Padding(horizontal, vertical)` build the `RectOffset` a scroll
view wants, so no screen writes `new RectOffset(...)` by hand.

## 5. Rules (apply to every change)

1. **Pick a role, never a number.** A screen says what a gap is *for*.
2. **Chrome is fixed height; content flexes.** A bar never changes size with the screen.
3. **Every overlay hangs off a safe box.** Anything that floats over the game is a `UiCard` on the modal canvas.
4. **A zero inset is allowed.** "No gap" is not a spacing choice, so `0f` is not a magic number.
5. **Look changes live in `UiTheme` / `UiSkin` only.** Swapping UI art is one asset.
6. **One 9-slice border, from the import preset** (12 px, `Art-Pipeline.md` §17) — never set in code.

## 6. The guards

| Guard | Menu | Checks |
|---|---|---|
| Type scale | `Tools > Idle RPG > UI > Check Type Scale` | every label's size and font; no hand-written `fontSize` |
| Spacing | `Tools > Idle RPG > UI > Check Spacing` | no hand-written gap: a non-zero inset, a hand-built `RectOffset`, or a numeric scroll / stack spacing |

Both are registered in **`Run All Checks (regression)`**, so either fault fails the run instead of shipping
quietly. The spacing guard was seen to fire: a temporary probe with `9f` as an inset produced
`SPACING: FAIL - 1 hand-written gap(s) found`.

**Gotcha — run the type-scale check OUT of Play Mode.** It reads the live scene, which in Play Mode contains
the runtime-built `DevOverlayCanvas` (the F3 debug overlay), so it reports false errors (size 13, Liberation
Sans) that do not exist in the saved scene.

## 7. Known exceptions and open items

| Item | State |
|---|---|
| `InventoryTabUI.Grid.cs` | allow-listed: 56 and 18 are layout offsets, −5 is the selection ring's overhang. Not gaps |
| `UiLabSceneBuilder.cs`, `DevOverlay.cs` | allow-listed: the lab compares looks; the overlay is developer UI |
| Header row | **fixed 2026-10-14** — the chips and RANKS share one layout-group row (§2c), so they divide the width and cannot overlap |
| Page bands | named (§2d): ASCEND + SHOP share one set; UPGRADES has its own. Change one constant, not four screens |
| Tablets | bands compress on a short, wide screen (see §3). Fix = cap the canvas width, if tablets matter |
| Inventory 56 / 18 / −5 | should become named constants (or new roles) if the inventory tab is un-parked |
| Toast | sits inside the main safe box at 0.32–0.40 of the safe rect (it used to be on the raw canvas) |

## 8. How to check a layout change

1. **Game View at 1080 × 1920** — the battle page, party, upgrades, ascend and shop look as designed.
2. **Game View at a tall aspect (e.g. 1080 × 2400)** — the two bars stay the same size and the **page grows**.
   That is the whole point of §2a.
3. **Device Simulator, notched phone** — open RANKS and trigger the offline / defeat popup; cards and the
   toast must clear the notch and the home bar.
4. Run both guards (§6).

## 9. Where the numbers came from

| Step | What landed |
|---|---|
| 1 | `UiSpace` — the ladder + roles |
| 2 | a safe box on the modal canvas; the toast moved inside the main one; safe nodes authored stretched |
| 3 | fixed-height chrome bars; `matchWidthOrHeight` 0 on all canvases (and the lab) |
| 4 | the battle page, the management panels, the shared builders, the header, party and shop onto roles — each sweep verified **pixel-lossless** (`Main.unity` byte-identical) |
| 5 | `UiSpacingCheck`, registered in `Run All Checks` |
| 6 | this document |
| 7 | the header row (`ChipRow`) — fixes the chip / RANKS overlap (§2c) |
| 8 | named management page bands — a pure rename (§2d) |

