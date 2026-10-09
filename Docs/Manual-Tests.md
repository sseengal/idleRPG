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

- [ ] `Tools > Idle RPG > Run All Checks (regression)` → ends **PASS** (drift + validation + golden numbers).
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
- [ ] **Fast attackers aren't lost**: at x1.6 a fast hero still gets its ~0.85 s quota (swing + recovery) — it is
      sequenced, never skipped.

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