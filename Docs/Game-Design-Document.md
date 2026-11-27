# Game Design Document — Kingsguard: Tiny RPG

> **This is the top page.** It holds the product decisions and the rules. It does not hold detail.
> Detail stays in the topic page it links to (§15). When this page and the code disagree, the code wins
> and this page is fixed the same day.
>
> Created 2026-11-22 from an owner design session. Owner decisions are dated in §14.
> A decision is not real until it is in §14.

---

## 0. How to use this page

- Read §1 to §3 to know what the game is.
- Read §4 to §7 to know how a run works.
- Read §12 to know when a piece of work is finished.
- Read §13 for the work that is out of scope.
- Every open question lives in one place: §11. Do not answer it in code first.

---

## 1. The game in one line

**Kingsguard: Tiny RPG** — a small dark-fantasy auto-battler. The king sends three guards to stop a
knight who has already won once. The player fights short runs, dies, grows, and goes deeper.

| Field | Value |
|---|---|
| Genre | Auto-battler with run-based meta-progression. Anchor: *Soda Dungeon* |
| Fantasy | Three guards against a war they already lost. Small people, big odds |
| Platform | Portrait phone. Android first, iPhone after (§10) |
| Price | Free. Rewarded ads + a small shop (§9) |
| Session | 5-10 minutes. One run is 5-8 minutes |
| Build target | Unity 6000.6.0f1, URP 2D, portrait |

---

## 2. Who plays it

**A busy adult who likes basic role-playing games.** They play on the go, one hand, in short bursts.
They will read a little. They will not read a lot.

What this forces:

| Rule | Why |
|---|---|
| Fights play themselves | No tactics skill needed on a bus |
| Few, clear choices | Party, spend, push, quit. Not a spreadsheet |
| Short text lines | 12 words or fewer per line (`Story-Canon.md` §7) |
| Visible progress in one run | A new stage, a new row, a new perk |
| No homework between runs | The lobby is a carousel, START, and the shop |

---

## 3. The pillars

| # | Pillar | Plain words | What it forbids |
|---|---|---|---|
| P1 | **Watch, do not steer** | The fight runs. The player presses an ability when it suits, and chooses when to push or quit | A tactics grid, manual targeting, twitch input |
| P2 | **Die and return** | Death ends the run, not the account. Bank a little, come back stronger | A hard game-over, a paywall on progress |
| P3 | **A place, not a ladder** | One location = one look, one problem, one boss, one cost | A faceless 100-stage grind with no reason to be there |

**Anti-pillars.** This game is not: a visual novel, an idle cash collector, a PvP game, a 100-hour
campaign, or a deck-builder. If a feature needs one of those, it is the wrong feature.

---

## 4. The core loop

```
LOBBY  (carousel: pick any unlocked location)
   |
   v
RUN START (stage 1 of that location, power = permanent upgrades only)
   |
   v
auto-battle waves -> location boss -> next location (if unlocked) .....
   |                                                                  .
   v                                                                  .
spend GOLD on TRAINING (permanent) and on run choices                  .
   |                                                                  .
   v                                                                  .
PARTY WIPE or END RUN  --(run over, bank Perk Points)-----------------'
   |
   v
LOBBY (pick the next location)
```

- A **run** is the unit of play. It starts in the lobby and ends at a wipe or at END RUN.
- A **run** crosses locations. If the player is strong enough, one run can pass more than one location.
- A **location** (the owner calls it a "chapter") is one stage. It has 6 normal waves and one boss wave
  (built: `Stage01_WavePlan.asset`, `normalWaveCount: 6`, boss on wave 7).
- A **wipe** does not delete story progress. Unlocked locations stay unlocked.

Target run length: **5-8 minutes**. A strong player can clear one location in one run.

---

## 5. Death, unlock, return

| Step | Rule | Built today? |
|---|---|---|
| Wipe | Party falls. RUN OVER card. No bounce, no auto-restart | Yes (`GameManager.EndRunOnWipe`) |
| Revive | **Revive** perk: one revive per run, full heal, same stage. Never an ad, never an IAP | Yes (`Prestige_Revive.asset`, 20 Perk Points, max 1) |
| Bank | END RUN banks **Perk Points** (scaled by depth). A run that cleared no stage pays nothing | Yes (`BankRunReward`) |
| Return | Lobby. No fight runs behind it | Yes (`GameState.Lobby`) |
| Pick | **Carousel of unlocked locations.** Any unlocked location may be started | **No.** `StartScreenUI` has a *placeholder* carousel and calls `StartRun(1)` — always stage 1 |
| Unlock | Beat a location boss -> the next location opens | Not yet (only one location exists) |

**Rule:** the player never fights backwards by force. If they want to farm an easy location, they pick it.
If they want to push, they pick the deepest one.

---

## 6. Currencies and what a run end wipes

| Currency | On screen name | Earned | Spent on | After a run end |
|---|---|---|---|---|
| Gold | Gold | Every kill | **TRAINING** (permanent) | **Kept** — `GameManager.Stages.cs:224` |
| Perk Points | Perk Points | Banked at run end, scaled by depth | **ASCEND** perks (War, Gold, Vitality, Revive) | Kept, added |
| Gems | Gems | Milestones, streak, IAP | Shop shortcuts and cosmetics | Kept |

What a run end **does** reset (`BankRunReward`): hero stat levels, party progression, camp state, run
counters (`CurrentStage`, kills, stages cleared), and `runActive`.

What it **does not** reset: gold, gems, Perk Points, Training nodes, and unlocked locations (when they
exist). The Revive "used this run" flag clears for the next run.

> **DECIDED 2026-11-27 — gold is permanent; TRAINING is the only gold sink.**
> 1. **TRAINING** in the lobby: permanent nodes. The long game.
> 2. **Camp Train** is **free**: +1 ATK / +1 DEF, run only. The camp choice is heal HP (Short/Long Rest) or raise
>    party stats (Train). The earlier "camp Train paid in gold" plan is cancelled.

---

## 7. Progression

| Layer | Lifetime | Page | Notes |
|---|---|---|---|
| Hero stat levels | **One run** | `Progression.md` | Reset at run end. Bought with gold |
| TRAINING tree | **Permanent** | `Training.md` | Gold sink. The long-term growth |
| ASCEND perks | **Permanent** | `Progression.md` | Bought with Perk Points. Four rows |
| Boons | **One run** | `Boons.md` | Offered during the run. Reset at run end |
| Camp Train | **One run** | `Camp.md` | +1 ATK / +1 DEF per camp. **Free** (heal vs stats choice) |
| Party XP | **One run** | `Run-Loop.md` | Reset at run end |

Design rule: **the player must always see one upgrade they can buy.** Training is the lobby answer (gold);
camp Train is the mid-run answer (free). The camp choice is heal HP (Short/Long Rest) or raise party stats.

---

## 8. Content scope

### 8.1 The MVP: 3 locations

| # | Location | Story job | Boss | Status |
|---|---|---|---|---|
| 1 | **Greenwake** | The shard, the bloom, and the cost of taking it | Slime King | Story locked, 2 of 6 scenes built, balance open |
| 2 | Location 2 | Not written | Not chosen | Not started |
| 3 | Location 3 | Not written | Not chosen | Not started |

- Each location has: one backdrop, its own enemy mix, its own boss, and a **mini arc** (open, turn,
  close + one boss line).
- **Finish location 1 to the full bar before location 2 is written.** One finished location proves the
  loop. Three unfinished locations prove nothing.
- After the third location: a **"Chapter 2 coming soon"** beat, then endless mode.

### 8.2 The full story (after MVP)

- More locations. A real ending at the final boss: the Black Knight is beaten, the shards are resolved,
  the Guard's cost is paid.
- Then an **endless mode** opens. Nothing in it is required to finish the story.
- **OPEN (§11.4):** how many locations the full story needs. Do not pick a number until location 1
  proves its cost and length.

### 8.3 Story rules that hold for every location

- Canon and voice rules: `Story-Canon.md`.
- The story never blocks the loop. A scene plays, then the fight resumes (or the run ends).
- No scene explains a shard, time, or the plot. Show, do not lecture.
- A wipe may show a defeat line. It must not repeat a scene the player has already seen.
- Text budget per location: **OPEN (§11.1)**. Recommended: 12 lines (open, turn, close, boss line).

---

## 9. Money

Model: **free, rewarded ads, small shop.** The game must be fully playable without paying.

| Product | SKU (built) | Type | Gives |
|---|---|---|---|
| No Ads | `kingsguard.no_ads` | Non-consumable | Removes ads. **Must** have Restore Purchases |
| 30 Gems | `kingsguard.gems_30` | Consumable | 30 gems |
| Starter Pack | `kingsguard.starter_pack` | Consumable | 50 gems + 30 minutes of gold boost |

Rules:

- **Never sell raw power.** No product may grant a stat the free player cannot reach.
- **A rewarded ad is always optional.** It never gates a stage, a revive, or progress.
- **Revive is a perk, not an ad, not an IAP** (Revive, 20 Perk Points, one per run). Decision
  2026-11-21, built.
- Ads and billing must sit behind the existing seams (`IAdService`, `IIapService`), so a mock and a real
  store can never disagree.
- Ads need consent, a cap per session, and a no-fill path that burns no reward.
- **OPEN (§11.3):** where the rewarded ad slot goes. Recommended: bonus gold at the run-over card, not a
  revive.

Source of truth: `Monetisation.md` §3a and `IapCatalog.cs`.

---

## 10. Look, sound, platform, release

### 10.1 Look

- Pixel/retro identity, portrait, one fixed camera (`Art-Pipeline.md` §17).
- v1.0 art plan: **one coherent paid pixel pack** for heroes, enemies and bosses, plus **custom key art**
  (icon, title screen, store images). Budget $300-800.
- The free Tiny RPG packs stay as placeholders until the paid set lands. Do not ship them as final art.
- **Licence rule:** the paid pack needs a written commercial licence. Record it in
  `Assets/ThirdParty/PROVENANCE.md` before any build goes to a store.
- Store rule to respect: a build that looks like a placeholder can be rejected (Apple 2.2 / 4.2).

### 10.2 Sound

- **One paid music pack + a matched sound set.** Budget $50-150.
- Mute must work. Sound settings live in the settings sheet.
- Every menu action, hit, death, level-up and boss entrance has a sound (`Audio-Plan.md` cue table).

### 10.3 Platform and release path

| Step | What | Gate |
|---|---|---|
| 1 | **Android soft launch** on Google Play | One full loop on a real phone; store packet complete; closed test passed |
| 2 | Live Android build, watch crashes and reviews | No P0 for 14 days |
| 3 | **iPhone** build, TestFlight, then App Store | Same packet, iOS purchases tested |

- Start the Play account and the closed test **now**. Play needs 12 testers for 14 days before a new
  personal account can publish. That clock cannot be compressed (`Launch-Plan.md` §0).
- Portrait only. One orientation. No tablet layout for v1.0.
- English only for v1.0.

---

## 11. Open questions

| # | Question | Why it matters | Recommended answer |
|---|---|---|---|
| 11.1 | How many story lines per location? | Sets writing, voice and localisation cost | 12 lines: open, turn, close, boss line |
| 11.2 | ~~Does gold survive a run end?~~ **DECIDED 2026-11-27:** gold is permanent; Camp Train is free; Training is the only gold sink | Closed | See §6 |
| 11.3 | What does the rewarded ad give? | Consent, fairness, store rules | Bonus gold on the run-over card. Never a revive |
| 11.4 | How many locations does the full story need? | Sets the real ending's cost | Decide after location 1 ships |
| 11.5 | Can the player farm an easy location forever? | Economy runaway, or a boring safety net | Yes, with a reduced gold rate after 3 clears |
| 11.6 | Does the carousel show locked locations? | Hype vs dead space | Show the next one as a silhouette ("coming soon") |

No code may answer an open question by accident. If a build needs the answer, ask the owner first.

---

## 12. Definition of done

Three gates. A gate passes on evidence, not on opinion. `Release-Checklist.md` owns the launch boxes.

### 12.1 Gate 1 — a location is done (Greenwake first)

- [ ] Story plays end to end: open, turn, close, boss line, outro. No repeated scene on a retry.
- [ ] Boss reads as a story event (the Slime King as the knot, not a bigger slime).
- [ ] Balance: a fresh save loses the first attempts and wins by run 2-3.
- [ ] Look: paid pack art for the location's units, one backdrop, key art.
- [ ] Sound: music for the location, hits, death, boss entrance, mute works.
- [ ] `Tools > Idle RPG > Run All Checks` passes. No console errors.
- [ ] One full run from boot to run end on a real phone.

### 12.2 Gate 2 — the MVP is done

- [ ] 3 locations, each at Gate 1.
- [ ] Real carousel: unlocked locations can be picked; the next one is teased.
- [ ] Location unlock on boss kill. Progress saved and restored after a relaunch.
- [ ] Run length measured at 5-8 minutes for a mid-power player.
- [ ] Shop (3 products) + Restore Purchases + rewarded ads + consent, tested in sandbox.
- [ ] "Chapter 2 coming soon" beat after the third boss, then endless mode.
- [ ] Save v12 -> v13 migration tested on a real save.

### 12.3 Gate 3 — the launch is done

- [ ] Store packet complete: icon, screenshots, text, privacy URL, data safety.
- [ ] Play closed test passed (12 testers, 14 days).
- [ ] No P0 defects in `Routes-and-Flows.md` §8.
- [ ] Signed AAB installs from the console. Rollback build kept.
- [ ] Day-0 monitoring owner named.

---

## 13. Out of scope for v1.0

These are **not** bugs. They are cuts. They ship later, if the game earns it.

| Cut | Why |
|---|---|
| Locations 4+ and the real ending | MVP proves 3 locations first |
| Endless mode tuning beyond a simple loop | It is the reward, not the product |
| 4th hero, roster picker | Needs new UI, no proven demand |
| Gear and bag UI | Drop chance is 0; the bag is parked |
| Leaderboard, seasons, achievements | No backend, no proven need |
| Cloud save, notifications, analytics, haptics | Not needed to submit |
| Localisation, tablet layout, landscape | English portrait only |
| Offline/ idle payouts UI | Out of the loop (see `Run-Loop.md`) |
| HD-2D pipeline (voxel props, lit sprites) | Too long a lead for v1.0 |
| PvP, clans, chat | Wrong game |

**One-in, one-out rule:** to add something here, name what leaves the same list, or the date moves.

---

## 14. Decision log

| Date | Decision | Owner | Record |
|---|---|---|---|
| 2026-11-21 | Name: **Kingsguard: Tiny RPG** | Owner | `Run-Loop.md` §9 |
| 2026-11-21 | Monetisation is required at v1.0 (lean shop + IAP + ads) | Owner | `Checklist.md` §0 |
| 2026-11-21 | The run-over revive is the **Revive** perk (Perk Points). Never an ad or an IAP | Owner | `Checklist.md` §0, built |
| 2026-11-21 | Story scope: opening scenes + flavour lines. Intro film skipped | Owner | `Intro-Sequence.md` |
| 2026-11-22 | Shape: run-based battler, story locations on top. A run ends at death | Owner | §4 |
| 2026-11-22 | Death returns to the lobby. A carousel picks any unlocked location | Owner | §5 |
| 2026-11-27 | **Gold stays permanent. One sink: permanent TRAINING.** Camp Train is free (heal vs stats choice) | Owner | §6, `Camp.md` §2 |
| 2026-11-22 | Ending: real ending at the final boss, then endless. MVP = 3 locations + "coming soon" | Owner | §8 |
| 2026-11-22 | **Chapter = one location = one stage.** Own boss, own mini arc. Greenwake first | Owner | §8.1 |
| 2026-11-22 | "Done" bar: story + balance + final look and sound + clean checks + real-phone run | Owner | §12.1 |
| 2026-11-22 | Money: free + rewarded ads + small shop | Owner | §9 |
| 2026-11-22 | Art: one paid pixel pack + custom key art ($300-800) | Owner | §10.1 |
| 2026-11-22 | Audience: busy adult who likes basic RPGs, plays on the go | Owner | §2 |
| 2026-11-22 | Run length: 5-8 minutes | Owner | §4 |
| 2026-11-22 | Release: Android soft launch first, iPhone after | Owner | §10.3 |
| 2026-11-22 | Sound: one paid music pack + matched sound set ($50-150) | Owner | §10.2 |

Change rule: edit the row, keep the date. Do not delete a row; strike it and add the new one, so the
reasoning survives.

---

## 15. Where the detail lives

| Question | Page |
|---|---|
| How does a run start and end in code? | `Routes-and-Flows.md`, `Run-Loop.md` |
| What happens in a fight, beat by beat? | `Combat-Sequencing.md` |
| What are the story rules and words? | `Story-Canon.md`, `Story-Greenwake.md`, `Story-Nodes.md` |
| How is a story scene played? | `Story-Scenes.md` |
| What does TRAINING contain? | `Training.md` |
| What does ASCEND contain? | `Progression.md` |
| What does the shop sell, and what are the ad rules? | `Monetisation.md` |
| What is left to build? | `Checklist.md` |
| What gates the stores? | `Release-Checklist.md` |
| What is the art pipeline? | `Art-Pipeline.md` |
| What is the audio plan? | `Audio-Plan.md` |
| What is the launch order? | `Launch-Plan.md` |

---

## 16. Next work, in order

One step at a time. Each step ends with a manual test in Unity, per `.clinerules`.

1. **Answer §11.1.** It blocks the story writing for locations 2 and 3.
2. **Greenwake story:** build `S03_Wall`, `S04_King`, `S05_Outro`, `S06_Defeat`.
3. **Camp choices:** three neutral cards; Short Rest 10%, Long Rest 20% for a rewarded ad, Train free. Camp heals revive the fallen at the healed fraction.
4. **Boss feel:** make the Slime King read as the knot around the shard.
5. **Balance:** first runs lose, run 2-3 wins, run 5-8 minutes.
6. **Look:** paid pack swap, Greenwake backdrop, key art booked.
7. **Sound:** music and matched set, mute works.
8. **Gate 1:** clean checks, no console errors, one full run on a phone.
9. Then locations 2 and 3, the real carousel, the shop and ads, and the Android soft launch.

### Risks, named

| Risk | Plain words | First defence |
|---|---|---|
| Balance | 7 waves is about 8 minutes. The target is 5-8 for a whole run. The stage may be too long | Measure first. Shorten or speed up before adding content |
| Art licence | A paid pack with an unclear licence can stop a store release | Get the licence in writing before payment |
| Scope | A 4th location is always "one more thing" | §13 is a cut list, not a wish list |
| Play clock | 12 testers for 14 days cannot be rushed | Start the account before the game is done |
| Store rejection | A placeholder-looking build gets rejected | Final art and sound in before submission |

