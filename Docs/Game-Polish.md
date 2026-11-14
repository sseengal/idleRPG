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
