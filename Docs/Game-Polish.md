# Game Polish - the plan (next feature)

> Owns: making what already exists feel finished. Status: **plan, 2026-11-09**. Nothing here is built.
> Parents: `Manual-Tests.md` (how to check each item), `UI-UX.md` and `UI-Layout.md` (the rules polish must obey).
> Related: `Boons.md` (the feature before this one), `Roadmap.md` (where polish sits in the sequence).

---

## 1. What "polish" means here (and what it does not)

Polish is the difference between "the system works" and "the player feels it work". It is **not** new systems and
**not** balance.

| In scope | Out of scope |
|---|---|
| Feel: timing, motion, feedback, sound | new mechanics or cards |
| Clarity: can the player read what just happened? | changing any damage, gold or XP number |
| Consistency: one type scale, one spacing ladder, safe areas | new screens or tabs |
| Robustness: no missing references, stable frame time, no silent failures | store, accounts, monetisation wiring |
| First run: the first 60 seconds teach themselves | art replacement for the whole game |

**Two rules for every item below:**

1. **It states the player-visible change** in one line, not the code change.
2. **It has an acceptance check** a human can do in the Play editor (or an automated test where the thing is a rule).
   If an item cannot be checked, it is not polish - it is a wish.

**Polish never changes balance.** If an item looks like a number, it belongs in a balance pass with the robot.

---

## 2. Known defects found while building boons

Seen in play, or read straight out of the code. These come first because they are already real.

| # | Defect | Severity | How found | Plain description |
|---|---|---|---|---|
| D1 | The level-up card is **silent** | high | code: `SfxCue.LevelUp` fires on the *retired* hero-stat level event, never on a party level | a screen that stops the fight arrives with no sound at all |
| D2 | Camp shows a **dark empty box** mid-field | medium | seen in the Play editor | an unexplained rectangle sits in the middle of the camp view |
| D3 | **Duplicate wave line** in the feed after a resume | low | seen in the Play editor | the feed repeats itself when a run is restored |
| D4 | XP bar **does not react** when a level lands | medium | by design today: the fill jumps | the reward moment is the quietest thing on screen |
| D5 | **Thorned Mail reads as broken** at low stages | medium | measured: a 1-damage hit reflected at 20% rounds to nothing | the player takes a card, sees nothing, and concludes it is broken |
| D6 | `MENU` sits exactly over the `RANKS` slot | low | seen in play | two buttons pretend to be the same button |
| D7 | Card **letter badges** instead of icons | medium | by design (placeholder) | the most important new screen looks unfinished |
| D8 | No **welcome-back moment** since away payouts were switched off | product | decision 2026-11-09 | the game used to greet a returning player; now it says nothing |

D1, D4 and D5 hurt most: all three sit on the **reward moment**, the one the player is meant to enjoy.

---

## 3. Workstreams

### W1 - Feel: the reward must land (highest value)

| Item | Player-visible change | Acceptance |
|---|---|---|
| Card opens with motion | the card scales in from 92% and fades, ~0.18 s, fight already stopped | no jarring pop; the fight stays frozen while it animates |
| Rarity reveal | the tag and frame flash their rarity colour once as the card lands | a legendary reads as different before you read a word |
| XP bar pulses on level (D4) | the bar flashes gold and the level label scales once | a level-up is visible without reading a number |
| Card tap feedback | the tapped card lifts and brightens, then the next hand deals | no tap feels ignored, even when the next deal is instant |
| Sound on open / pick / reveal (D1) | open plays the level chime, a pick plays the purchase sound, a legendary adds one extra note | three distinct cues, checked with sound on |

### W2 - Clarity: the player must be able to read the fight

| Item | Player-visible change | Acceptance |
|---|---|---|
| Mechanic hits are labelled | a cleave, echo or reflect hit says **which boon** it came from, in its own colour | a new player can answer "why did that enemy lose health" |
| D5 clarity half | a card whose effect rounds to nothing says so: `reflects 20% (min 1)` | Thorned Mail at stage 1 still shows a number |
| Camp box (D2) | the middle of the camp view has a purpose, or it is gone | no unexplained rectangle |
| Feed de-duplication (D3) | a resumed run prints its wave line once | relaunch, resume, count the lines |
| Boon count on the bar | `LV 7 - 4 boons`, so the run's build is readable at a glance | no list page needed; the count is always in sight |

### W3 - Consistency: one kit, one ruler

| Item | Player-visible change | Acceptance |
|---|---|---|
| D6 fix | `MENU` and `RANKS` never occupy the same slot at once | press MENU in a run: no overlap, no button under a button |
| Safe-area sweep | nothing important sits under a notch or a gesture bar | check the corners of every page on a tall simulated screen |
| Spacing guard | `Tools > Idle RPG > UI > Check Spacing` passes on the new pages | the automated guard is green |
| Rebuild parity | rebuilding the scene changes nothing the player can see | rebuild, play, compare with a screenshot |

### W4 - Robustness: it must not break quietly

| Item | Player-visible change | Acceptance |
|---|---|---|
| Frame time on device | the fight holds its framerate with 3 enemies, damage numbers and a card opening | measure it, do not eyeball it |
| No missing references | no console errors on a fresh boot, a resumed run, a wipe, an ascension | console clean in all four paths |
| Failure paths | a save that cannot load still starts a run instead of hanging | corrupt the save on purpose, then boot |
| Boon safety net | a card with bad data is skipped, never crashes | already covered by the deck guard test |

### W5 - First run: the first 60 seconds

| Item | Player-visible change | Acceptance |
|---|---|---|
| Silent-tutorial check | with no text at all, a new player can find: start a run, open a card, pick one, read the bar | watch someone who has never seen it |
| Level-up is the hook | the first card arrives within the first two waves | matches the Boons pacing target |
| D8 decision | either a welcome-back moment returns, or the game never pretends to have one | product decision, recorded in `Idle-Economy.md` |

---

## 4. Phases

| Phase | Contents | Size | Ends when |
|---|---|---|---|
| **Q1 - Quick wins** (no new art, no new systems) | D1, D3, D4, D5, D6, mechanic labels, feed de-duplication | small each | every reward moment has motion and a sound |
| **Q2 - Reading and safety** | rest of W1, the boon count, W3 safe areas and guards, W4 frame time and failure paths | medium | a stranger can read a fight, and nothing breaks quietly |
| **Q3 - Art and the finish** | D7 icons, rarity frames, card background, impact effects for the mechanics | art-led | the card screen looks shipped, not scaffolded |

Q1 and Q2 can interleave. **Q3 cannot start until the deck is frozen** (`Boons.md` section 13.8): an icon for a card
that gets deleted is money burned.

---

## 5. What to measure (so polish is not opinion either)

| Signal | How | Target |
|---|---|---|
| Frame time while a card opens | profiler on device, 3 enemies plus damage numbers | no spike past the frame budget |
| Console cleanliness | four boot paths: fresh, resume, wipe, ascend | zero errors |
| Time to the first card | the Play editor clock | under ~30 s from run start |
| "Why did that happen?" count | watch one new player for five minutes | no unanswered questions about the fight |

---

## 6. The order I would actually work in

1. **D1 (sound on the card) + D4 (the bar pulse).** Cheapest possible change, biggest felt difference: they fix the
   reward moment.
2. **D5 (Thorned Mail looks broken).** A card that appears to do nothing poisons trust in the whole feature.
3. **D3 + D6** (feed duplicate, MENU/RANKS overlap): small, visible, embarrassing.
4. **Mechanic labels** - the first clarity win, now that boons change the fight and nothing says so.
5. **D2 (the camp box)** - needs a short investigation, not a guess.
6. Then Q2 as a block, then Q3 with art.

---

## 7. Explicit non-goals for this feature

- No new cards, no new mechanics, no rarity work. Those are `Boons.md` P1/P2/P3.
- No balance tuning. That is the robot's job and a separate pass.
- No new screens. If an item needs a new page, it is a feature, not polish.
- No full art replacement. Icons and frames only (Q3).

---

*Prepared at the boons regroup, 2026-11-09. Defects D1-D8 were found in this session - in play, or by reading the
code. Nothing here is speculative.*




---

## Lobby, header and tab polish (2026-11-12)

- **Header:** the shared bar wears the chunky riveted `ui_frame` plate. RANKS (leaderboard) is switched off; a
  three-bar menu button takes its slot, with the tap action intentionally left for later.
- **Lobby centre:** the chapter illustration (`Greenwake1-clean 1.png`) sits in the centre panel, sized to its
  1400x764 aspect (no letterbox). The name is a pixel wordmark (`Greenwake title 1.png`) with a small "CHAPTER 1"
  kicker above; both hug the panel top.
- **Atmosphere:** a vertical gradient, a corner vignette and two soft halos (green behind the art, gold behind
  START), all generated in code and cached. Halo alpha held at 0.10 so they read as rim light, not a blob.
- **Bottom tabs:** icons sliced from `Button Icons.png` (Multiple sprites). Idle tabs show only their icon; the
  selected tab shows icon + label centred beneath it, easing in. Per-tab size and vertical nudges live in
  `StartScreenUI` (`TabIconScale`, `TabIconSelectedScale`, `TabIconVertical`, `TabIconSelectedVertical`); the base
  fractions live in `FantasyTabButton.Update`.


---

## UI consistency programme - Phase 1: the named palette (2026-11-14)

- Every colour the UI draws is now a name in UiTheme (80 new tokens: nav blues, golds, rarity,
  Ready/Cooling/Queued/Locked ability states, HP states, log colours, item tints, atmosphere stops).
  158 hand-written colours across 35 files were replaced; near-duplicates (within 0.05 in one channel)
  were merged so one job has one name.
- New guard UiColourCheck (Tools > Idle RPG > UI > Check Colours), registered in Run All Checks, fails
  the run on any new hand-written constant colour. Procedural textures (computed channel) are exempt.
- Zero visual change intended: values are the exact ones the screens already used, except the
  documented merges above. Consolidating the remaining near-twins (the golds) is a later, screenshot-led
  design decision, not a refactor.


---

## UI consistency programme - Phases 2-3: one button, one plate (2026-11-14)

- UiPlateButton (Common/UiPlateButton.cs): the shared button - chunky ui_frame face, smooth idle/selected
  colour hand-off, press squash (95.5%), and a true grayscale when disabled via UIEffect. Creates itself with
  UiPlateButton.Create(parent, name, anchors, text, role, idle, active, onClick).
- UiRuntime.Plate + UiRuntime.PlateSprite: one runtime plate factory (9-sliced ui_frame). Rule: big surfaces
  wear the plate; small chips stay flat so the rivets do not turn tiny things into noise.
- Migrated: the lobby Btn helper (START, MENU, Perks Buy, Training popup Buy) now builds through
  UiPlateButton, so those buttons gained the press squash and the grayscale disable for free.
- Level-up card: plate shell + plate rows (rows carry the same press/disabled behaviour) + a plate REFRESH
  button that truly grayscales when no re-roll is left. Tabs keep FantasyTabButton (lift/halo/pose).


---

## UI consistency programme - Phase 4: Training page (2026-11-14)

- Surfaces: the Training page panel, the column header strip and the node popup card now wear the chunky
  ui_frame plate (UiRuntime.PlateSprite, 9-sliced). Inner scroll face and the small diamond nodes stay flat
  on purpose - rivets on tiny elements read as noise.
- Buttons: hero header cells, tree diamonds and the scroll buttons now carry the shared UiPlateButton
  behaviour (press squash + disabled grayscale). Nodes keep their diamond art; SetFaces() keeps their
  state colour (owned / next / locked) easing correctly instead of fighting the button lerp.
- No layout or gameplay change: colours, anchors and the tree math are untouched.


---

## UI consistency programme - Phase 5: Party and Perks pages (2026-11-14)

- The shared lobby page shell (CreateLobbyPanel) now wears the ui_frame plate, so BOTH the Party and the
  Perks pages gained the chunky frame with one change.
- Perk rows are plated cards; the Party hero stats panel is plated. Small chips stay flat by design:
  gear slots, roster cards, formation pads and tab faces remain the flat theme fills.
- Buy buttons on the Perks page were already UiPlateButtons from Phase 2, so they squash and grayscale
  when unaffordable.


---

## UI consistency programme - Phases 6-7: battle chrome and one menu (2026-11-14)

- Shop rows and BUY buttons use the chunky plate at runtime (row/button sprites overridden to
  UiRuntime.PlateSprite in ShopPanelUI.EnsureBound); the BUY face is the Orange token, not white.
- Ability strip cells and camp choice buttons are re-skinned to the plate at runtime (RestylePlates), so
  the battle chrome matches the lobby without a scene rebuild. The old kit sprite stays only as a fallback
  until the scene is rebuilt.
- One menu affordance: the header three-bar button now returns to the lobby (StartScreenUI.ShowLobby), and
  the old overlapping top-right MENU button (MiniMenuCanvas) is retired. It was runtime-built, so no scene
  rebuild is needed.
- Phase 8 (boon/ability icon sheets) is BLOCKED on icon art: the letter badges remain placeholders until a
  sheet exists, then it takes the same Multiple-sprite pipeline as Button Icons.png.


---

## UI consistency programme - clean-up pass (2026-11-14)

- Dead code: LeaderboardSheetUI removed (RANKS was retired and nothing referenced it). Legacy
  ui_panel/ui_button sprite fields removed from ShopPanelUI and slotSprite from FormationBoardView; the scene
  builder no longer wires them. Every surface now resolves UiRuntime.PlateSprite.
- Plated: party hero cards and formation pads (they were still flat kit faces).
- Behaviour: camp choice buttons now carry UiPlateButton - press squash + true grayscale. LONG REST greys
  out when it cannot be afforded instead of only refusing the tap.
- Deliberately NOT changed: the combat log Dialogue size (a visual decision that needs a screenshot
  session), and the ability cells keep their state tints (they repaint by design, so the shared colour
  lerp would fight them).


---

## Training node icons - first real art (2026-11-14)

- Source: FantasyRPGIcons_Free24 (free for commercial use). Only the 128px set is imported: Sprite,
  Bilinear, no mips, uncompressed. 256/512 are not in the build.
- Data: new TrainingIconSet asset (Assets/IdleRPG/Data/Training/TrainingIconSet.asset), one sprite per
  TrainingEffect: 0 sword_long, 1 shield_round, 2 meat_leg, 3 potion_health, 4 coin_gold, 5 chest_treasure,
  6 spellbook_red, 7 scroll_sealed, 8 spell_heal, 9 wand_crystal.
- UI: each diamond draws the icon at 0.17-0.83 of the node (95px of 144); the old letter stays as the
  fallback so a missing sprite never leaves a blank node. Icons are full colour; the diamond still carries
  the owned/next/locked state colour.
- Verified in Play: all 10 effects resolve to distinct sprites; UiColourCheck 0 offenders; UiSpacingCheck PASS.
- Note: this pack draws each icon as a medallion (stone disc + gold rim), so the diamond reads as a frame
  around a badge. Judged acceptable; if it feels heavy, the fix is to hide the diamond face when art exists.


---

## Training medallions + boon/ability art (2026-11-14)

- Training nodes: the diamond and the pale-gold ring are gone. When a node has art the MEDALLION is the
  node - it is the click target; the diamond and letter survive only as the fallback for a missing sprite.
  Art at 0.10-0.90 of the 144 node (115px). State: full colour when owned or next; dimmed (InkGreyMid)
  when locked, LineSoft when deferred; the green arrow badge still marks the next purchase.
- Boons: all 13 PerkData assets now carry a Tavmir icon (sword_long sword, chest, coin, aimed_shot, axe,
  potion_red, shield_wood, staff, berserk, multi_shot, war_slash, poison, life_drain). The level-up card
  draws the icon over the rarity-tinted well and keeps the letter as the fallback.
- Abilities: AbilityDef gained an icon field; 11 assets assigned (6 player + 5 enemy). The strip draws a
  small badge in the cell top-right, created at runtime. The current scene has no AbilityPanelUI instance,
  so it is wired but not visible until the battle strip is present.
- Fix: AbilityPanelUI.RestylePlates now runs from OnEnable - it was only reachable from ConfigureRuntime,
  which nothing calls at runtime, so the plate face and the badge never applied before.


---

## Icon system pass: one skin, shared glow, medallion polish (2026-11-14)

- UiSkin is now the SINGLE reskin home for icon art too: goldIcon, gemIcon, tokenIcon, weaponIcon,
  armorIcon, trinketIcon (Resources asset, so runtime UI can read it). A reskin is still one asset.
- Header: HudHeaderUI.RestyleCurrencyIcons swaps the three chip icons from the skin at start (the scene keeps
  its placeholder icons, so a scene rebuild cannot undo it).
- Items: ItemVisuals.SlotIcon(slot) resolves the skin icon; the roster gear slots and the inventory tiles
  draw it and hide the letter, which stays as the fallback.
- Glow: UiRuntime.SoftGlowSprite is one shared, cached radial texture; StartScreenUI atmosphere now reuses
  it instead of building a second copy.
- Training: medallions at 127px (0.06-0.94). A soft gold halo shows only when the node is NEXT and
  CanAffordNode() is true - refresh-driven, no per-frame polling. Connectors stay behind the nodes.
- Ability strip: real scene object confirmed; its OnEnable now restyles the plate and creates the icon badge
  (3 badges at runtime), so ability icons appear once a hero has an ability equipped.
- Verified in Play: chips = pxi_coin/pxi_gem_ruby/pxi_key; 1357 nodes all 127px with glow objects, 4 glowing
  (one affordable next-node per column); UiColourCheck PASS, UiSpacingCheck PASS.


---

## Ability strip: one layout owner, icon-led cells (2026-11-14)

Why: the cells came from AbilityCell.prefab, so the layout could not be reviewed - a runtime badge landed
on top of the name and ability text (x 0.66-0.96, y 0.60-0.96 vs name at 0.70-0.94). Two owners also fought
over the cell colour: the panel washed the whole face while UiPlateButton lerped the same Image.

Now: AbilityCellView.Build(parent) owns the layout, and both the scene builder and the running game call it.
- Three bands: hero name (Micro, dim) top-left; state word top-right; ability icon (72) left, vertically
  centred; ability name (Small) beside it; 4px cooldown line along the bottom, visible only while cooling.
- Portrait removed: the hero faces are already in the formation row, so repeating them was noise.
- Equal cells: HorizontalLayoutGroup on the strip (spacing Small, padding Small) and flexibleWidth = 1,
  replacing fractional anchors + per-cell negative sizeDelta.
- One owner per concern: the plate stays steady; state lives in icon tint + state word + line. The plate
  only greys for a true lock (no ability / dead), so a cooling cell never flashes another colour.
- State word ink: cooling uses TextDim (readable), not Cooling (a bar-fill token, near-invisible as text).
- Runtime: AbilityPanelUI.EnsureCells builds from the party once, clears the old Hero* cells, then drives
  state every frame. A scene rebuild follows the same Build path.

Verified in Play: 3 cells at 340x168, identical icon Y (580.6-652.6), name baseline 668.6, insets 12/8 on
every cell; READY green, 12s in TextDim with the gold line filling; guards PASS. Screenshot:
Assets/Screenshots/ability-cells-v2.png.

Follow-up: AbilityCell.prefab is no longer used by the build path. The scene still carries its instances
until the next rebuild; delete the asset after a rebuild confirms the code-built cells.


---

## Ability strip: the picture IS the cell (2026-11-14)

Why: the cells still carried four pieces of text (hero name, ability name, state word, and later a GUARD count)
around a 72px picture. Three cells of small text under a fight asked the player to read instead of look, and the
state word duplicated what the line under it could say on its own.

Now: each cell is one picture and one line, nothing else.
- Ability picture at 136 = the art's own 34px x 4. A whole-number zoom, so no pixel column lands half a pixel
  wide. It is centred in the room between the plate's top inset and the bar, which puts it BarHeight/2 above
  the true centre.
- State line along the bottom: 316 x 6, inset 12 at the sides and 8 off the bottom. One graphic is driven; the
  rail behind it is a fixed slate line that a full fill covers completely.
  - ready = full green (UiTheme.Ready)
  - resting = gold fill growing left to right (UiTheme.GoldSoft); its LENGTH is the countdown
  - press waiting = full amber (UiTheme.Queued), which is the only "your tap landed" cue
  - no ability, or dead = no line, no picture, greyed plate
- Removed: both text labels, the state word, the GUARD count, the three serialized tint fields, and the old
  4px/`Tight` bar. Bar colours now come from UiTheme at the point of use, so no saved scene value can repaint
  the line (the stale `coolingTint` was why the sliver once looked gold while the code claimed dark slate).
- The icon sprite is only re-assigned when it changes; assigning the same sprite every frame rebuilt the
  canvas every frame.

Losses, stated plainly: no names means the three cells are told apart by picture and left-to-right party order
(the same order as the fighters above); GUARD is no longer visible in the strip (the fight still floats
"GUARD UP" over the knight); and there are no seconds, only a proportion.

Verified in Play: cells 340 x 168, icon 136 with identical Y, rail and fill both 316 x 6 at y 8, green full when
ready, gold partial while resting; UiColourCheck PASS, UiSpacingCheck PASS. Screenshots:
Assets/Screenshots/ability-icons-hero.png and Assets/Screenshots/ability-bar-cooling.png.

---

## Ability strip: the move's name back, and a line that really fills (2026-11-15)

Supersedes the section above.

Why two changes in one pass:
1. With no text, the three cells could only be told apart by picture and party order. The player asked for the
   move's name back, so he can see WHICH move he is pressing.
2. The gold line never grew. `Image.fillAmount` is ignored by Unity when the image carries no sprite, and every
   state line in the game is a sprite-less colour quad - so the line sat full for the whole cooldown, then flipped
   to green. The fight also moves its clock in 0.05 s steps, so even a working bar would arrive as small jumps.

Now:
- The move's name sits under the picture: `UiType.Micro` (26), centred, one line, `UiTheme.TextDim` when ready and
  `UiTheme.TextMuted` while resting - the same dimming rule as the picture. Text = `AbilityDef.DisplayName`, with
  `Combatant.AbilityName` as the fallback. It is written only when the string changes, so no canvas rebuild per
  frame. Hidden together with the picture and the line (no ability, or hero down).
- The picture is 34 x 3 = 102, anchored to the cell's bottom edge. Numbers: line 8..14, group gap 8, name
  22..55.3, caption gap 2, picture 57.3..159.3, with 8.6 units of air above the picture - the same air as below
  the line, so the picture does not crowd the plate's top rim. A whole-number zoom, so it stays crisp; the old 136
  no longer fits beside a name.
- The cell has a two-step gap hierarchy, not two equal gaps: 8 between the picture-and-name group and the state
  line (different things: what the move is, versus when it is free), and 2 between the picture and its own name
  (a caption belongs to the picture). Both come from the `UiSpace` ladder (`Small`, `Hairline`); no number is
  written in the layout. Stated plainly: 8 is as large as this cell can give - the gap can only grow by eating the
  air above the picture, and the picture is already as big as the 34px art allows without a blurry zoom.
- The line's length now comes from the fill's right ANCHOR, not from `fillAmount`: geometry always clips, and it
  needs no sprite. The fill is a child of the rail, so one rect still owns the bar's place on the cell.
- The drawn length eases toward the fight's value (`1 - exp(-16 * dt)`, about 60 ms): each 0.05 s step of the fight
  becomes a short glide, and the value always lands on the fight's own number, so the line cannot drift or claim
  "ready" early. Amber when a press waits for its turn, gold while resting, green at full.

Losses, stated plainly: the picture is a quarter smaller than before; the name needs a move whose name fits one
316-wide line (the longest today is "Holy Slash", 10 characters); and the drawn line trails the fight by about
60 ms, which is invisible at rest but real.

Found while doing this, NOT fixed here: the hero and enemy health bars use the same sprite-less Filled images, so
they also ignore fillAmount and always draw full length. Same root cause, different owner (`HpBarView`). It needs
its own small change.

Verified in Play: cells 340 x 168, picture 102 at y 57.3, name 316 x 33.3 at y 22, rail 316 x 6 at y 8, gap to the
name 8, gap to the picture 2, 8.6 air above the picture (measured from the live rects, not by eye; the drawn gap
reads a few pixels larger because the text line box has padding). Bars: full green when ready, full amber while a
press waits, gold and partial while resting; UiColourCheck PASS, UiSpacingCheck PASS, UiTypeScaleCheck reports only
its three old Boot-splash errors and no ability-strip issue. Screenshots:
Assets/Screenshots/ability-name-cooling.png, Assets/Screenshots/ability-name-ready.png and
Assets/Screenshots/ability-gap.png (the wider gap).
