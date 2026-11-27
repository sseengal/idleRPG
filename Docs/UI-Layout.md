# UI-Layout — containers, spacing, and mobile scaling

> Owns: the structure under the UI (layers, safe boxes, bands) and every gap number.
> Parent: `UI-UX.md` (screens, patterns, type scale) and `Architecture.md`.
> **Location:** `Docs/` (folder index: `README.md`). **Last verified:** 2026-11-25 (live scene, measured).
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

**The main safe box holds four children:** `Header`, `BattlePage`, `Toast` (and the header is borrowed by the
lobby, which puts it in its own safe box). The old `ManagementPage` and `NavBar` are retired — the lobby owns
navigation since 2026-11-12.

### 2a. The chrome bands are fixed heights (re-measured 2026-11-25)

| Band | Anchor | Band (units) | Rect height |
|---|---|---|---|
| Header | pinned to the top, 32-unit gap (`PageMargin`) | 128 | **80** |
| Page | stretched from the header band to the bottom margin | — | **1760** on 1080 × 1920 |
| NavBar | retired (the lobby owns navigation) | — | — |

**The bar draws nothing.** The light-blue header container is gone: `Header` is a bare rect node with no `Image`,
so the currency plates are the only surfaces on it. The bar is exactly one plate tall, which is why the air around
the plates is even - **32 above, 32 beside, 16 below** - instead of the 48 / 64 / 16 sandwich the old container
produced (a page margin stacked on a bar inset).

**The header is one size on every screen.** The lobby borrows the same `HudHeaderUI`, and it used to re-stretch
it to 0.03–0.97 × 0.90–0.985 of the screen — which made the same object **163 units tall in the lobby and 83 in
battle**. It is now reparented without touching the rect, into its own safe box.

The header's own numbers, all from `UiSpace`:

| Part | Value | Source |
|---|---|---|
| ICON (the anchor) | 64 (≈26 dp), in an **80-wide slot** | `ChipIconSize`, `ChipIconSlot` — a HUD glyph reads at 24–26 dp; 48 was 19 dp |
| Plate height | 80 (≈32 dp) | `ChipHeight` = icon + 8 above and below |
| Bar height | 80 | `HeaderBarHeight` = the plate, no extra inset |
| Plate art | `CurrencyContainer.png`, 30 × 30, 9-slice border 4, drawn at 2× (`pixelsPerUnitMultiplier` 0.5) | cropped from the owner's 100 × 100 export; the rim stays 1 art pixel at any chip width |
| Icons | cropped to their visible art: gold 30 × 30, gem 36 × 31, token 36 × 39 | `Assets/IdleRPG/Art/UI/Icons/currency_*.png` — one height, so a round coin no longer reads smaller than a tall key |
| Chip value | Display 56, **centred** in the space beside the icon, auto-shrinks to 26 | `UiType.ApplyAuto` |
| Menu | **three bars, no plate**; icon at the bar's right edge, so it sits halfway between the last plate and the screen edge | `UiRuntime.HamburgerSprite` |
| Menu tap square | 144 × 144, right edge on the screen edge | `UiTouch.IconButtonHitSize` |
| Row | 920 wide (stops one icon + one page margin short of the bar's right edge); 8 gaps | `MvpSceneBuilder.Battle.cs` |
| Boost badge | a third row item; hidden until an ad boost runs | `HudHeaderUI.OnGoldBoostChanged` |

**One rule now: extra height belongs to the page, the bars never move.** `PinBar` in `MvpSceneBuilder.cs`
builds the header from `UiSpace.HeaderBandHeight`, so it cannot change size with the screen.

### 2b. Bands inside the battle page

Fractions of the page (so they follow whatever height the page gets): arena **0.373–top**, party XP strip
**0.346–0.373**, ability strip **0.226–0.346**, log **0–0.226**.
Source: `LogTop = 0.226f`, `BattleViewportBottom = 0.346f`, `XpBandTop = 0.373f` in `MvpSceneBuilder.cs`.

**The stage band is a fixed 110-unit strip, not a fraction (2026-11-26).** Top of the page: the land name on its
own line (46), one `Space8` above the trail (32), then `SectionGap` + `Space8` of air under the dots. The header
band already supplies a `SectionGap` above the name, so the air above "GREENWAKE" and the air under the dots read
equal (measured before: 46 above the name against 19 under the dots). The arena's top follows the band, so a taller
phone grows the arena instead of the band.

**The stage row draws no plate (2026-11-25).** The land name and the journey trail sit straight on the page, so
the header and this band are the two plate-less surfaces at the top of the fight. A grey bar there was a second
frame that carried no meaning, and it forced the trail's own dim state to fight a lighter background. The header
check asserts the row carries no `Image`, so the bar cannot creep back.

**The log pays for the arena nameplates (2026-11-15).** Each arena slot is 160 × 260: a 95-unit nameplate band at
the bottom (name, health numbers, health bar), the sprite above it, and a 5-unit top margin. The sprite keeps the
size it had at 176; the extra height came from the log band (0.27 → 0.226), not from the art. The feed shows about
one and a half fewer lines at once; the 100-line scrollback is unchanged.

### 2c. The header is a row, not hand-placed chips (2026-10-14)

The currency chips and a three-bar (hamburger) menu button live in one `ChipRow` under the header, driven by a
`HorizontalLayoutGroup`. The **two** chips (gold, gems) carry `LayoutElement.flexibleWidth = 1` and divide the
width; the menu button takes a fixed slice at the end. **2026-11-27:** the key/prestige chip is removed; Perk
Points live on ASCEND. The boost chip is a row item, shown only while an ad boost runs.

Why: they used to be placed by hand — the chips at 0.03 / 0.35 / 0.65 of the header, and the runtime button
at 0.88 — and an earlier token chip and the button **overlapped by about 95 units**. That is how a long currency
value ended up sliding under the button. Two numbers, tuned separately, never compared.

Gold and Gems share the row; the menu takes its fixed slice. No overlap.

The boost chip is a row item, hidden until an ad boost runs; it takes the third block while active.

**Lobby centre (2026-11-12).** The centre panel is the chapter illustration (`Art/Greenwake1-clean 1.png`),
sized to its own 1400x764 aspect so there is no letterbox. The chapter name is a pixel wordmark image
(`Art/Greenwake title 1.png`) with a small "CHAPTER 1" text kicker above it; both hug the panel top. Behind the
content sits a subtle atmosphere pass — a vertical gradient, a corner vignette, and two low-alpha soft halos
(green behind the art, gold behind START). No new assets: all three sprites are generated in code and cached; the
halo alpha is held at 0.10 so it reads as rim light, not a blob.

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
| Header | `Tools > Idle RPG > UI > Check Header` | the top bar's golden numbers, measured in the built scene: bar 1016 × 80 (one plate tall, no bar image), plate 80, icon 64 in an 80 slot inset 16, value 56 centred (auto to 26), plates 9-sliced on `CurrencyContainer`, row 920 wide, gear 144 × 144 tap square; and the lobby must not re-anchor the borrowed header |

All three are registered in **`Run All Checks (regression)`**, so a fault fails the run instead of shipping
quietly. The spacing guard was seen to fire: a temporary probe with `9f` as an inset produced
`SPACING: FAIL - 1 hand-written gap(s) found`. The header guard was seen to fire too: its first run caught the
menu button's hit area at 128 units against the 144 touch minimum.

**Gotcha — run the type-scale check OUT of Play Mode.** It reads the live scene, which in Play Mode contains
the runtime-built `DevOverlayCanvas` (the F3 debug overlay), so it reports false errors (size 13, Liberation
Sans) that do not exist in the saved scene.

## 7. Known exceptions and open items

| Item | State |
|---|---|
| `InventoryTabUI.Grid.cs` | allow-listed: 56 and 18 are layout offsets, −5 is the selection ring's overhang. Not gaps |
| `UiLabSceneBuilder.cs`, `DevOverlay.cs` | allow-listed: the lab compares looks; the overlay is developer UI |
| Header row | **fixed 2026-10-14** — the chips and the menu button share one layout-group row (§2c), so they divide the width and cannot overlap |
| Page bands | named (§2d): ASCEND + SHOP share one set; UPGRADES has its own. Change one constant, not four screens |
| Tablets | bands compress on a short, wide screen (see §3). Fix = cap the canvas width, if tablets matter |
| Inventory 56 / 18 / −5 | should become named constants (or new roles) if the inventory tab is un-parked |
| Toast | sits inside the main safe box at 0.32–0.40 of the safe rect (it used to be on the raw canvas) |

## 8. How to check a layout change

1. **Game View at 1080 × 1920** — the battle page, party, upgrades, ascend and shop look as designed.
2. **Game View at a tall aspect (e.g. 1080 × 2400)** — the two bars stay the same size and the **page grows**.
   That is the whole point of §2a.
3. **Device Simulator, notched phone** — open the hamburger menu and trigger the offline / defeat popup; cards and the
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
| 7 | the header row (`ChipRow`) — fixes the chip / RANKS overlap (§2c); RANKS itself was later replaced by the three-bar menu (2026-11-12) |
| 8 | named management page bands — a pure rename (§2d) |

