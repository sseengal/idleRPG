# Manual tests — the one runbook

Every "a human must look at this" step left in the plan, in the order the plan runs them. Tick what you did,
write the expected result next to it. Found a problem? Note it in `Docs/REVIEW.md`, make one small change, re-run
the checks — that is the standing loop.

---

## 0. How to test (the basics)

| What | How |
|---|---|
| Play the game to test | Unity: press Play. It starts at your saved run (or stage 1 fresh). |
| Live numbers while playing | **F3** — the overlay with gold speed, waves, money/ads lines (press F3 again to hide). |
| Save now | **F5** |
| Full reset (fresh-player test) | **F8** — wipes save + settings + reloads the scene |
| Delete save only | **F9** |
| Sound on/off | **M** |
| Move between screens | **Tab** (and the number keys 1-4 jump to a page) |
| Developer grants while testing | **G** gold · **T** tokens · **B** ad-boost · **A** ascend now · **S** speed toggle (the F3 legend lists them all) |
| Read what happened | the console (errors) and the crash diary `crash-log.txt` next to the save |

**Dev build vs release build:** developer keys and test ads only exist in a developer build. A release build never
includes them (enforced at build time from B10).

---

## 1. Before every build (a few eyeball reads — the heavy lifting is automated)

- [ ] `Tools > Idle RPG > Run All Checks (regression)` → ends **PASS** (drift + validation + golden numbers + the
      type scale). If the only failure is the golden-number line, and the change was meant to move the numbers, that
      is the frozen baseline talking, not a bug: write the new numbers down and check the other lines passed
      (`Content.md` §10). Cleanup work gets no such allowance.
- [ ] `Tools > Idle RPG > UI > Check Type Scale` → **PASS** (every label on the scale, in a theme font, and no code
      setting a size by hand). It also runs inside the check above; this is the version with the size/font histogram.
- [ ] `Tools > Idle RPG > Mobile > Verify Settings` → nothing red.
- [ ] `Tools > Idle RPG > Release > Go or No-Go (checks + stamp)` → **GO**, version stamped, lists no missing
      modules.
- [ ] Battle-log skim (2 minutes of watching a stage): one line per wave, enemies named once, no flooding, no
      mid-line renaming (the standing contract).
- [ ] The touched screen: 30 seconds on the thing you changed, no clip, no freeze.

---

## 2. B8' proof lap (the only open B8' item)

- [ ] **Battle-log read-through**: play stage 1-3 with the monster-making tooling present — log reads clean
      (one line per wave, no flooding), all 3 monsters + the boss appear across waves.
- [ ] **Shop beat walk with F3 open**: reach a milestone stage (every 5th) → note the gem gain on the F3 money
      line → buy the fast-forward card (AUTO page) → hit x2 on a stage → let an offline window happen → the
      welcome-back pay and its cap look sane on the F3 rate line; the ad line shows caps/cool-downs moving.
- [ ] First-run whispers (fresh save via F8): exactly 3 messages, in order, once each — then never again.

---

## 2b. Juice pass — visual review (new combat feel, 2026-10-09)

> Everything below is cosmetic. Check in Play at normal **and** x1.6 pace (the fast-forward card). If any single feel
> is too much or invisible, name it — each value is one Inspector field (list in `Checklist.md` §1u).

- [ ] **Enemy hit flash**: placeholder sprites flash white and fade back on every hit; crits flash longer.
- [ ] **Enemy hit punch**: the enemy dips to ~0.94 on a hit; crits dip to ~0.88 (barely deeper, visibly different).
- [ ] **Crit shake**: on a crit the whole enemy column jitters once (~1 px) and settles; normal hits do NOT shake.
- [ ] **Wave-clear pop**: when a wave clears, the enemy column springs up 4% and settles before the new wave lays out.
- [ ] **Hero hit punch**: a hit on a hero dips that seat (icon + HP bar) to 0.94 with the red flash; scene has no
      leftovers (seats return to exact size, no squashed empties).
- [ ] No errors in the console at any point.

---

## 2c. Gold fly — coins into the counter (new, 2026-10-09)

> Check in Play at normal **and** x1.6 pace. The kill reward was already granted before the effect runs — this is
> only the visible "money travel" moment.

- [ ] **Coins fly on kill**: each kill spawns ~8 gold dots from that enemy's slot, bursting outward with their own
      scatter directions, decelerating, then being sucked into the top-bar counter **immediately** (no paused
      bounce). The gold number only starts rising when the first coin LANDs.
- [ ] **Multiple distinct coins + comet tails**: coins are clearly separate (each has its own path + 8 ghost dots
      trailing it); a kill does not look like one dot.
- [ ] **Bounce + magnet**: coins decelerate naturally and the magnet pull starts the instant the burst decays (no
      settle/bounce pause, no clipping).
- [ ] **Multi-kill waves at x1.6**: 3 kills still keep the screen readable (throttled, capped at 32 active coins).
- [ ] **Spends still snap**: buying an upgrade drops the counter instantly, then the next coin-burst rises it again.
- [ ] **Coin-less gains still show**: offline claim / ad / debug scenes surface their gold within the 0.6 s grace.
- [ ] No errors in the console; F3 money line matches the counter after coins settle.

---

## 2d. Face-to-face combat — visual review (run-in, blur-back, 2026-10-09)

> Check in Play at normal **and** x1.6. The damage events fire exactly as before — the surprise to verify is the
> *visible* choreography on top of them.

- [ ] **Heroes run in**: when a hero attacks an enemy, the hero's icon runs at the enemy's slot (walk clip) before
      the damage lands.
- [ ] **Enemies run in**: when an enemy attacks a hero, it runs at the hero's slot.
- [ ] **Damage at impact**: the damage number, HP bar drop and battle-log line all appear as the run makes contact —
      no number floating before the swing, no delayed-lie (nothing the player can "catch lying").
- [ ] **Swing pose at contact**: after the run-in, the attacker plays its attack animation at the target.
- [ ] **Blink back**: the attacker squash-fades at the target, teleports to its slot, pops back in — a readable
      "anime blink", not a slow walk home.
- [ ] **No stranded units**: after a wave ends, a death, or a fast x1.6 burst, every unit is back in its slot
      (no one frozen mid-field).
- [ ] **Console clean**: no `IndexOutOfRange` on slot lookups, no null rects during the fight. (`Run All Checks`
      goldens are intentionally NOT re-measured in this pass — see Checklist §1x, balance-deferred.)

### Turn-sequence checks (ATB-style, one unit at a time — added 2026-10-09)
- [ ] **One runner at a time**: at any moment only ONE unit is running/hitting — never two heroes or a hero+enemy
      mid-swing together.
- [ ] **Exchanges, not a brawl**: swings alternate hero → enemy → hero when both sides are ready; when the enemy is
      alone it still acts on its own cadence.
- [ ] **Reset between actions**: a unit finishes its full dance (hit → swing pose → blink home) before the next
      unit starts running.
- [ ] **Same damage truth**: HP bar, damage number and battle log still arrive together at impact — the sequencing
      changed nothing about when damage is real.
- [ ] **Fast attackers aren't lost**: at x1.6 a fast hero still gets its ~1.35 s quota (swing + recovery) — it is
      sequenced, never skipped.

### Battle log checks (store + view, added 2026-10-25)
- [ ] **Past 100 lines**: fight for 3+ minutes (over 100 lines). The oldest line leaves the top, the newest line
      arrives at the bottom, and the newest line changes with every action — the feed never freezes.
- [ ] **No blank band**: at 100 lines the newest line sits on the bottom edge with no empty strip below it.
- [ ] **Page switch keeps history**: open Upgrades for 30 seconds, go back to Battle. Lines from the hidden time are
      there, in order, and the feed is still pinned to the newest line.
- [ ] **Drag pauses, release resumes**: drag the feed up, hold. New lines do not yank it. Release — it returns to
      the newest line on the next action.
- [ ] **One line per action**: no merged, throttled or `...N more` summary lines anywhere in the scroll.
- [ ] **Long lines wrap inside the box**: a long story line (the intro's first Soldier line) wraps to two or
      three rows, stays inside the log, and its row is tall enough. No text crosses the right edge or the box bottom.
- [ ] **All log text is 48**: combat and story lines both use the 48 VT323 Dialogue role.
- [ ] **Speaker names stand out**: story names are gold; the spoken words are near-white. Names and words are easy
      to scan apart on a long line.

### Lunge-position + animation checks (v2, added 2026-10-09)
- [ ] **Side-by-side flank at body height**: a lunging unit stops BESIDE its target aligned to the target's BODY
      (the icons' `(0.5, 0.60)` centre — not the slot's top edge), with a clear gap — no diagonal overlap, no
      landing above the head, and breathing room (bosses get more).
- [ ] **No attack tail at home**: when a unit teleports back, the fade-in shows the idle pose — the swing's last
      frames never play on after the character is already back in position.
- [ ] **Full swing plays**: the attack clip runs to its LAST frame (strike + follow-through) before the blink —
      no mid-swing teleport cut, no missing final frames (a ~0.5 s pose on the pack's art).
- [ ] **Turn beat**: after the attacker pops home there is a noticeable beat of stillness before the next runner
      starts (~0.3 s) — the hit and its spoils read, then the next unit commits. Like a turn-based RPG, not a
      conveyor belt.

---

## 2e. Crit stats + party page (added 2026-10-10)

> The party page (PARTY > ROSTER) and the UPGRADES page both gained two stats. Check that the numbers agree
> everywhere and that buying is understood.

### Party page — ROSTER sheet
- [ ] Six rows read cleanly: **HP, ATK, DEF, CRIT, CRIT DMG, DPS** — no overlap with the role label above or the
      gear row below, at any hero count.
- [ ] **CRIT** shows a percent (`5%` at level 0) and **CRIT DMG** shows a multiplier (`x2`) — not raw fractions.
- [ ] **DPS changes when crit changes**: buy crit, come back, the DPS row moved (it now includes the crit factor).
- [ ] **Live update**: with the party page open, buying a stat on the UPGRADES page updates the sheet without
      re-opening the tab.
- [ ] Gear bonus % appears only next to HP/ATK/DEF (blank beside CRIT / CRIT DMG / DPS).

### Upgrades page
- [ ] Each hero section shows **five stacked rows** (ATK, HP, DEF, CRIT, CRIT DMG) under the hero's name - one row
      per stat, not tiles side by side.
- [ ] **Every row is easy to hit with a thumb**: tapping anywhere on a row buys one level; the `x10` button on the
      right buys ten. Nothing needs a careful aim.
- [ ] The effect line reads right per stat: `x1.09 / lvl`, `+10% base / lvl`, `+0.5% / lvl` (crit), `+0.05x / lvl`
      (crit damage).
- [ ] At max level the row reads `MAX`, the cost line goes blank and the row stops responding to taps.
- [ ] Buying crit raises CRIT in the battle log's upgrade line and on the roster.

### Battle log
- [ ] **Incoming crits** read `Goblin CRITS Knight for N (hp/max)` and are marked in the crit colour; normal hits
      are unchanged.
- [ ] Outgoing crits still read `Knight CRITS Goblin for N`.

### Combat
- [ ] A crit still hits harder than the same non-crit (roughly x2 at level 0).
- [ ] Crit chance caps out: past 75% no more crits appear (buy far enough to check, or trust the resolver cap).

---

## 2f. Party page — formation + bag lock (added 2026-10-10, updated 2026-10-24)

> Moving a hero is now a **battle tactic**. This section keeps the bag-lock checks; the formation checks moved to
> the battle board (2g below).

### Party sub-nav + bag lock
- [ ] PARTY shows **one** tab (`ROSTER`) — the tab bar is dropped so the roster fills the page; no INVENTORY button.
- [ ] No `Formation` panel or dead button is left behind; the page opens on ROSTER as before.

### Roster gear row
- [ ] The gear hint reads **"gear: coming soon"** (it used to say "tap a slot to manage", which was never true).
- [ ] The three gear tiles are not buttons: tapping them does nothing.

### Drops are off
- [ ] Beat a boss (or several): **no item toast, no new gear, no gold bonus from gear** — nothing drops.
- [ ] If a save already carries worn gear from before this change, it still loads (bonuses stay). That is expected.

### Regression sweep
- [ ] Upgrades still work (buy a stat, the roster sheet updates live), crit rows still read correctly, battle log
      still marks crits, and the lunge/turn-beat behaviour on the BATTLE page is untouched.

---

## 2h. Moving a hero in battle + where the numbers land (added 2026-10-24)

> Moving a hero is now a **battle tactic**: tap a hero, free pads brighten, tap a free pad. Numbers and words are born
> at the point of impact, not at the pad the hero usually stands on.

### Move a hero
- [ ] Tap the Knight's card (the hero himself, not the pad edge): nothing moves yet, and **three free pads brighten**
      (two in the back column, one more in the front column).
- [ ] Tap a brightened back pad: he **slides** there in about a fifth of a second, name and health bar with him.
- [ ] Tap him, then tap **anywhere else** (another hero, an occupied pad, empty board): the brightening disappears and
      nobody moves.
- [ ] After a move the console has no errors, and the next enemy swings follow the new row (heroes in the back rank
      are hit less often - watch the log for `back` targets).
- [ ] Move him back to the front pad: same two taps, same slide.
- [ ] Relaunch Play: the layout you left is still there.

### Numbers land on the hero, not on his pad
- [ ] With Guard up, let the Knight cover a hit on another hero: the number **and** the word `GUARD` appear next to
      the hero he covered, where he is standing at that moment - not over his empty pad.
- [ ] The number does **not** chase him when he dashes back home; it stays where it appeared and fades out.
- [ ] Every hero (top, middle, bottom) shows his own numbers when hit.
- [ ] An enemy that runs at the party shows its number where it was hit.

---


## 2m. Guard — the stance and the picture (added 2026-10-24)

> Guard is one sim rule (intercept N hits aimed at other heroes, take half) and ONE picture: the knight steps between
> the ally and the enemy that is coming. A plan may only contain a spot a unit will really occupy.

- [ ] Cast Guard: `GUARD UP` appears over the Knight and **he does not move at all** (not even a small sideways hop).
- [ ] With the stance up, let an enemy hit another hero: the Knight slides **in front of that ally**, on the side the
      enemy came from, and the enemy stops **facing him** - you can see it close a real distance.
- [ ] The Knight holds there about two thirds of a second, then blinks home; the number and `GUARD` appear where he
      actually stood.
- [ ] The ally takes nothing for the first two covered hits; the **third** hit lands on the ally (charges: `GUARD 2` ->
      `1` -> `0` on the ability button).
- [ ] Put the Knight in the **back** row and repeat: he still steps in front of the targeted ally.
- [ ] Watch one of his own attacks land while an enemy hits an ally in the same instant: he is **not** dragged mid-run;
      the blow lands on him where he is. (Deliberate: a unit already out is never moved.)
- [ ] Move a hero to another pad with Guard up: the cover still lands beside the ally, and the enemy still stops facing
      the Knight (nothing aims at a pad any more).
- [ ] Console: no errors during all of the above.

---

## 2n. Enemies close the distance (added 2026-10-24)

- [ ] Any enemy attacking the party runs **towards** its target, not just up/down its own row: the travel is clearly
      visible (roughly 180 px), and it stops roughly one body-width short, never overlapping.
- [ ] A hero attacking an enemy stops on the **near** side of that enemy (it does not cross to the far side).
- [ ] Two enemies on different rows both stop facing their own target; their bodies never stack on one another.

---

## 2g. Upgrades page — mobile layout pass, Phase 1 (added 2026-10-10)

> Every control was smaller than a fingertip. Now one upgrade is one finger-sized row with `+1` and `x10` buttons.
> Full record: `Docs/Upgrades-Page.md`. **No page content moved** - hero stats are still bought here.

### Can I hit it with a thumb?
- [ ] On UPGRADES, each stat row has two buttons on the right: **`+1`** buys one level, **`x10`** buys ten. Tap them
      on all five rows on all three heroes - neither needs a careful aim.
- [ ] Tapping the row's **face** (name, level, cost) does nothing by design - the face is decoration, so a scroll
      flick cannot buy. Only the two buttons buy.
- [ ] With 0 gold, rows are dimmed and the buttons do nothing (no sound, no purchase).
- [ ] Buy a stat to level 1: the row stops being dim, the level and cost text update immediately.
- [ ] The console shows **no** `NullReferenceException` and **no** "Combat tick threw" after opening this page and
      killing an enemy (the page used to throw on every gold change).

### Can I scroll it? (this was broken - check carefully)
- [ ] Drag the UPGRADES list **starting on top of a row**: the list moves.
- [ ] Drag it starting on a hero's name, on empty space, and on the AUTOMATION section: the list moves too.
- [ ] Flick it hard: the list keeps moving and slows down (soft landing, not a snap back).
- [ ] Drag the ASCEND list: it moves.
- [ ] On the BATTLE page, drag the combat feed: auto-follow pauses, and it resumes when you let go.
- [ ] Nothing gets bought while you drag (flick over a row, release, check gold and levels did not change).

### Nothing overlaps or clips
- [ ] Row text does not collide: stat name and effect on the left, level and cost in the middle, buttons on the
      right. Costs in the millions (`1.2 M`) still fit.
- [ ] The five rows fit inside their hero panel with even spacing; the hero's name sits above them.
- [ ] Scrolling: the page scrolls smoothly from the first hero to the bottom of AUTOMATION. Content is about three
      screens tall - that is expected, not a bug.

### Automation section (it moved)
- [ ] AUTOMATION is now the **last section inside the page**, below the last hero - not a pinned strip at the
      bottom of the screen.
- [ ] Each card has ON/OFF and BUY buttons that are comfortable to hit; unowned cards read `LOCKED` with the token
      cost.
- [ ] Auto-buy (once owned) shows its own row: the reserve text plus finger-sized `-5%` / `+5%` buttons. Before it
      is owned, that row says the reserve unlocks with the card.

### Ascend rows
- [ ] ASCEND rows are taller than before and the `BUY` button is easy to hit.

### Regression sweep
- [ ] ROSTER still updates live when a stat is bought here; DPS still moves with crit.
- [ ] The BATTLE page, the turn order and the lunge behaviour are untouched by this change.

## 2h. Run identity — the defeat card and run summary (added 2026-10-10)

> The run now has a visible edge. The loop itself is unchanged: a wipe still falls back one stage and keeps fighting,
> and ASCEND still resets the run. Full record: `Docs/Run-Identity.md`.

### The defeat card
- [ ] Lose a fight (let the party wipe): a card appears over a dimmed battlefield - "DEFEAT — stage N", the run's best
      stage and gold so far, and a countdown ("resuming in 10…").
- [ ] The run restarts on its own when the countdown ends - no tap needed.
- [ ] Tap CONTINUE: the run restarts immediately.
- [ ] Tap ASCEND (stage 10+): the run ends there; the "Run ended" toast appears; you go back to stage 1 with tokens.
- [ ] Below stage 10, the ASCEND button is greyed and reads "reach stage 10 to ascend".
- [ ] At a hard wall, the full card does NOT open on every single bounce (a 60 s cooldown shows the quick beat
      instead) - but a genuinely new stage shows it again.

### Run stats
- [ ] Kill an enemy and clear a stage: the "best this run / gold this run" numbers on the next card grow.
- [ ] Ascend: the next card (and the toast) read from the NEW run, not the old one.

### Regression
- [ ] Bounce still works: wipe -> one stage back -> auto-resume.
- [ ] No NullReferenceException in the console; no "Combat tick threw".

## 2i. Enemy sprites — blank area + death pose (fixed 2026-10-10)

> Two bugs reported from play. Both were in the enemy views; nothing else changed.

### The enemy area was blank sometimes (start-order race)
The stack lays itself out from the simulator, but the first wave can spawn *before* the HUD binds the stack. With no
manager it found no enemies, switched every slot off, and nothing refreshed it again until the next wave - so the
right side stayed empty for a whole wave.
- [ ] Start a session: enemies are visible straight away, never a blank right side.
- [ ] Press `F8` (full reset) and let it reload: enemies visible straight away again.
- [ ] Do both a few times (the race depends on Start order): visible every time.
- [ ] Force a defeat (`D`) and let the run resume: the new wave's enemies appear.

### The enemy vanished when it died (it now holds the pose, like heroes)
- [ ] Let an enemy die: it plays its death clip and **stays on screen** on the last frame (it used to pop off).
- [ ] Placeholder enemies (Slime, Bat) stay on screen as-is - they have no death strip to play.
- [ ] A dead enemy keeps its slot until the next spawn: it must not hide, float, or drift around.
- [ ] The next wave re-uses the slot cleanly: new sprite, new name, full HP bar.
- [ ] With 2-3 enemies, killing one leaves the corpse in place while the others keep fighting.

## 2j. Swing timing — the hit lands on the strike (fixed 2026-10-10)

> The blow used to be applied at the same instant the attack animation *started*. It now lands on the swing's strike
> frame. Rule recorded in `UI-UX.md` §3.

- [ ] Watch a hero attack: the swing starts as it leaves its spot, and the enemy's health/flash drops when the swing
      **connects** - not before the swing begins.
- [ ] Watch an enemy attack the party: same thing (art enemies only - Slime/Bat have no attack strip, so they only
      slide and pose).
- [ ] The unit does not stand still before swinging: the run-in and the wind-up happen together.
- [ ] After the blow, the swing's follow-through plays, then the unit blinks back to its spot.
- [ ] Two units in a row: the second attack starts cleanly (no unit stuck holding an attack pose).
- [ ] Placeholder units (Slime/Bat) still slide in, pose, and blink back - no frozen poses.

---

## 2k. Ranks card — the shared card standard (added 2026-10-11)

Open RANKS (the trophy button, top right of the header). This sheet was the test case for the shared card, so it is worth
a careful look. It is safe to open while a fight runs.

### The card fills the screen
- [ ] The card is a large panel in the middle of the screen (about 4/5 of the width) - **not** a small square. Text is as
      big as the rest of the game's text.
- [ ] The game behind the card is dimmed (compare a nav tab before and after opening it).
- [ ] Nothing behind the card can be tapped: try tapping a nav tab through the card - the card stays, the tab does not
      change.
- [ ] The card draws **above** the floating damage numbers.
- [ ] `GLOBAL RANKS` reads at the top left, `X` at the top right, `you are #N of M` under the title.

### The rows read
- [ ] 12 rows plus your own gold row at the bottom.
- [ ] The column heads (#, TAG, LVL, STAGE) sit right above row 1, not floating in the middle.
- [ ] Your row shows your **real** rank number (the same number as in `you are #N of M`) and is gold.
- [ ] Rows use the same pixel font as the rest of the game (not a smooth default font).

### It opens again (this was broken)
- [ ] Close with the `X` -> it goes away. **Open it again** -> it comes back with its rows intact. The second open used
      to show nothing at all.
- [ ] Tap the dim background -> it closes as well.
- [ ] Open and close it 3 times in a row while a fight runs: rows never double up and nothing flickers.

### Regression
- [ ] The rest of the battle page still works (damage numbers, battle log, nav tabs).
- [ ] Switch tabs, come back, open RANKS again: still fine.


## 2l. Type scale — the by-eye pass (added 2026-10-11)

> The numbers are already checked by `Tools > Idle RPG > UI > Check Type Scale`. This is the part a machine cannot
> judge: does the text actually *look* right on a phone-sized window?

- [ ] **Window shape first**: set the Game view to a phone shape (1080x1920 portrait). Everything below assumes that.
- [ ] **Battle page**: currency values, `STAGE n  WAVE n`, the hero name plates with their HP (`240/240`), the enemy
      plates, `BATTLE LOG` and its lines. Nothing clipped, nothing touching its neighbour, nothing too small to read.
- [ ] **Nav bar**: BATTLE / PARTY / UPGRADES / ASCEND / SHOP — all five the same size, `UPGRADES` clear of the tab edges.
- [ ] **Party page, ROSTER tab**: the hero card's stat table (keys, values, bonuses) reads without zooming. This was
      the worst screen before the pass.
- [ ] **Party page, FORMATION tab**: hero names over their health bars; tap a hero then a slot and check the swap reads.
- [ ] **UPGRADES page**: hero rows (name, stat cost, effect), then scroll to `AUTOMATION` — card titles and their
      descriptions fit inside their half of the card.
- [ ] **ASCEND and SHOP pages**: titles, bodies, and the shop's `BUY` button showing a price.
- [ ] **RANKS card** (header button): title, the `you are #N of M` line (same size as the column heads), the rows, and
      the pinned `YOU` row.
- [ ] **Offline popup** on a fresh start (F8, wait, relaunch): `WELCOME BACK!`, the reward number, and both buttons.
- [ ] **Inventory (parked feature)**: to see it, set `PartyPanelUI.InventoryEnabled = true`, recompile, open
      PARTY > INVENTORY. Grid tiles, the filter/sort buttons, and tapping a tile for the item sheet. Set it back to
      `false` afterwards.

If a size looks wrong, change the number in `UiType.cs` — one place, and every screen follows.

---

## 2o. Story scene — `S01_Intro` (added 2026-10-29, runs after the story slice is built)

Run on the battle page, fresh save or after F9 (delete save). Everything else is unchanged.

### Setup
- Open `Main.unity`, press Play. The fight should stop before wave 1.
- If it does not, the gate or the pending-scene bind is broken (`Story-Scenes.md` §7, boot order).

### The scene
- Three Guard actors walk in from the left: Knight, Mage, Soldier. They stop without overlapping.
- One Slime creeps in from the right and stops inside the frame.
- The first story line shows the speaker name in gold immediately, then the words type out one character at a time.
- Each later line finishes the previous line before it starts typing.
- No damage numbers, no HP changes, no swings for the whole scene.
- The battle log stays visible and readable.

### Controls
- Tap while a line is typing: the line completes immediately, then the scene ends. The wave spawns.
- Tap again: nothing happens.
- Let a scene run past its cap on purpose (temporarily set a beat time too high): it still ends.
- Background the app for 10 s, return: no stuck wave, no replay, no duplicate actors.

### After the scene
- Wave 1 spawns once. Stage 1, wave 1 in the header.
- Formation and HP are the same as before the scene.
- Camp still opens before wave 4 (it must not be skipped by the story gate).
- Press `P`: the scene plays again over the live fight (debug only).

### Layout
- Repeat on the tallest and shortest device profiles. Bubble stays inside the viewport.
- No actor leaves the frame at either extreme.

### Fail signs
- Wave 1 spawns behind the scene, or twice.
- Two scenes play at once.
- The log shows combat lines inside the scene.
- The scene plays behind the Upgrades page.
- A tap does nothing, or a second tap restarts it.

---

## 3. B9' part 2 — phone checklist (needs the first real phone build)

- [ ] **Portrait lock** — turn the phone while playing; game stays one way up; nothing clipped at the notch /
      punch-hole.
- [ ] **Android back button** — one press saves and leaves; reopening shows the progress still there.
- [ ] **Version** — the app shows **1.0.0**.
- [ ] **Crash diary** — crash it (developer build); `crash-log.txt` exists with a readable, time-stamped last line.
- [ ] **Icon + launch screen** — look right in the app drawer and on the home screen.
- [ ] **Go/No-Go green** — names no missing modules.

---

## 4. B1a — device pass 1 (does the logic feel right on a phone)

> Runs as soon as a developer phone build exists — it can run **in parallel with the art pass** (logic does not care
> about looks).

- [ ] **Fresh install** → first-run whispers lead you to the UPGRADE page (once each).
- [ ] **Resume** — force-quit, reopen: stage/wave/gold exactly as you left them.
- [ ] **Idle for 30 minutes** (screen on) → steady 60 frames; booster/extras expire on time; no slow creep.
- [ ] **Offline claim by touch** — background the app ≥ the cap window, come back, tap CLAIM: gold matches the
      F3-consistent rate; the off-cap never pays.
- [ ] **Safe area** — swipe into the notch/chin area, nothing is hidden; UI reaches both edges correctly.
- [ ] **Back button** (Android) — saves + leaves (see section 3).
- [ ] **Streak + ad caps on device** — claim the morning gift; watch an ad: counters on F3 move; a *failed* ad
      leaves the cap untouched.
- [ ] **Forced crash** → diary file has it (section 3).
- [ ] **Battery/heat** — 15 min idle: reasonable, not cooking the phone.
---

## 5. B1b — device pass 2, after art + audio (does it *look* and *sound* right) + submission

- [ ] **Both phone shapes** (iPhone + Android): layout clean at both aspect ratios, no clipped text.
- [ ] **Art legibility** — every monster/hero reads from arm's length; gold/gem icons distinct.
- [ ] **Sound balance** — hit/click/level-up audible; **M** mutes everything immediately.
- [ ] **Portrait-only** holds after the art pass.
- [ ] **The F3 overlay is gone** from the release build (developer build only).
- [ ] Then: **submission** — the store packet (B10b): screenshots, feature graphic, listing text, age/content
      rating, Data Safety answers, review-queue double-check against this runbook's notes.

---

## 6. B10/B10b — store tests (real purchases + real ads on phones)

- [ ] **Sandbox purchase** (both stores): every shop item buys, pays the right gems/minutes, saves immediately.
- [ ] **Restore Purchases** — fresh install on the same account → press Restore → no-ads returns.
- [ ] **No-ads removes ads** on the real network instantly, both sellers.
- [ ] **Both sellers' test ads** — a rewarded ad plays; reward only after the full view; skip/fail/no-fill →
      reward nothing and burn no daily cap.
- [ ] **Consent walk (EEA)** — the privacy question appears before the first ad; "refuse" still lets the game
      play and disables ads for them; the same answer is remembered after restart.
- [ ] **Test ads cannot ship** — a release build shows real ads only (a developer-build test-ad id in a release
      build fails the build).

---

## 7. Found a bug? Do this

1. Note it in `Docs/REVIEW.md` (what you saw, when, what you expected).
2. One small change, compile, `Run All Checks`, Play the exact spot again.
3. Tick it off. Never fix three things at once.

---

*Home of the checklists previously in `Checklist.md` §1q (proof lap) and §1r (B9' part 2 phone list) — those
sections now point here.*