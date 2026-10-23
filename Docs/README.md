# Docs — index

> **Everything that describes this game lives here.** One folder, one source of truth per topic.
> Root `README.md` is the quick start (clone → open → play); everything deeper is in this folder.
> Last restructured: Step 11e (2026-09-21); Art-Pipeline.md added 2026-09-26; Launch-Plan.md added 2026-10-11; Character-Catalog.md, Stage-Ladder.md and Ability-Map.md added 2026-10-11; UI-Layout.md added 2026-10-14; Combat-Sequencing.md added 2026-10-24; doc consolidation pass 2026-10-24 (one current page per topic).

---

## One current page per topic

Each topic has **one** page that says how the game works today. Older pages stay as the record of how a
decision was reached. A replaced page must carry a status line at the top and point at the current page.

| Topic | Current page | Kept as the record |
|---|---|---|
| Fight order, strip choice, timings, enemy cast timing | **`Combat-Sequencing.md`** | `Combat-Presentation.md` (23 Oct research + 24 Oct build log) |
| Ability status: what works, what is open | **`Ability-Progress.md`** | — |
| Ability design decisions | **`Ability-Plan.md`** (decisions) | built behaviour lives in `Combat-Sequencing.md` |
| Which hero or enemy owns which ability | **`Ability-Map.md`** | — |

A status page names the date it was last checked against the code. When the page and the code disagree,
the code wins and the page is fixed the same day.

---

## Where to look

| Question | Document |
|---|---|
| What is this game now — endless, or a story? | **`Game-Vision.md`** (canon: story-led mini campaign, automatic combat, restart on death) |
| How does a fight reach the screen, and how do abilities work? | **`Combat-Sequencing.md`** (the current page: lane, art-to-time, casts, enemy timing) |
| What was the original combat research and the build log? | `Combat-Presentation.md` (23-24 Oct 2026 record; current behaviour is in `Combat-Sequencing.md`) |
| Where do abilities stand, and what is still open? | **`Ability-Progress.md`** (checked against code 24 Oct 2026) |
| What is the fight order, which strip plays, and what are the timings? | **`Combat-Sequencing.md`** (one page: lane, art-to-time, enemy timing, audit, checks) |
| What is left to finish, and where are we right now? | **`Checklist.md`** (master record - start here) |
| What is planned next, in what order, with what acceptance? | `Roadmap.md` |
| How do I get this to the stores, and in what order? | **`Launch-Plan.md`** (art, audio, store launch - sequenced for a one-man team) |
| What characters and animations do we have, exactly? | **`Character-Catalog.md`** (all 42, enumerated from the packs, never hand-typed) |
| Which enemy appears on which stage, and who is a boss? | **`Stage-Ladder.md`** (100 stages, 10 bands) |
| What are the abilities, and how are they unlocked? | **`Ability-Map.md`** (named strips -> abilities, plus the unlock rule) |
| How do I clean up the project without changing the game? | **`Cleanup-Checks.md`** (before/after gate: regression net, 12 EditMode tests, 5-minute smoke) |
| Why is the code shaped this way? Layers, decisions, rules | `Architecture.md` |
| How does the simulation work (combat, formation, enemies)? | `Sim-Core.md` |
| Content pipeline: specs, generator, validator, enemies/zones | `Content.md` (also: **when the golden numbers are re-recorded**) |
| Rates, offline payouts, currencies, time-based monetisation | `Idle-Economy.md` |
| Monetisation plan: sources, sinks, IAP mock, guards | `Monetisation.md` |
| Levelling, prestige, tracks, roster | `Progression.md` |
| Screens, navigation, log contract, accessibility | `UI-UX.md` |
| Containers, the spacing ladder, mobile scaling | **`UI-Layout.md`** |
| Art pipeline: HD-2D look, asset volume, tooling, perf budgets (G11/G12) | `Art-Pipeline.md` |

## Status legend (used in every doc)

`DONE` · `IN PROGRESS` · `TODO` · `PARKED` (deliberately deferred) · `OBSOLETE` (kept only until deleted)

## Standing rules (apply to every change)

1. **One sub-step at a time** - implement, recompile, verify, tick the box, hand off for a manual test.
2. **Run the regression surfaces** on every combat-affecting change: golden numbers, content validator, save drift,
   **battle log** (attribution, one line per wave, no mid-line renaming, no flooding). The golden numbers are
   **frozen until the MVP baseline pass** (`Content.md` §10): a FAIL is expected while content is still moving, and
   the baseline file is not edited until then.
3. **Parity lever** - force the wave recipe to `1x 100%` and the golden numbers must match the single-enemy
   baseline byte for byte; that proves a change is additive.
4. **Nothing is stored that can be derived** - wave size comes from `hash(stage, wave)` + the recipe, so saves stay
   small and a resumed stage replays the same fights.
5. **The sim never touches Unity** - `Sim/` has no engine references; runtime glue lives in `Combat/`, data in
   `Data/`, presentation in `UI/`.
6. **One current page per topic** - a replaced page gets a status line and a pointer; a status page carries the
   date it was last checked against the code.

## Layout

```
Docs/
  README.md          <- this index
  Game-Vision.md     <- CANON: story-led campaign, automatic combat, restart on death
  Combat-Sequencing.md <- CURRENT: fight order, strip choice, timings, audit and Play checks
  Combat-Presentation.md <- old record: 23 Oct research + 24 Oct build log
  Ability-Progress.md <- CURRENT: what works in the ability feature, what is open
  Ability-Plan.md     <- the locked ability decisions (built behaviour is in Combat-Sequencing.md)
  Cleanup-Checks.md  <- the before/after gate for consolidation work (no behaviour change)
  Checklist.md       <- master progress + status
  Roadmap.md         <- ordered backlog with acceptance tests
  Launch-Plan.md     <- art + audio + store launch, sequenced for a one-man team
  Character-Catalog.md <- all 42 characters, enumerated (actions + frames + traps)
  Stage-Ladder.md    <- 100 stages, 10 bands, bosses, introduction order
  Ability-Map.md     <- named animation strips -> abilities, and the unlock rule
  Architecture.md    <- decisions (A/B/AD log), layers, data catalog
  Sim-Core.md        <- pure simulation design
  Content.md         <- specs + pipeline + enemies/zones/affixes
  Progression.md     <- currencies, tracks, prestige, roster
  Idle-Economy.md    <- rates, offline, time payouts
  Monetisation.md    <- sources, sinks, IAP mock, guards (B7 done; live-store wiring = B10, see its §6b)
  Manual-Tests.md    <- the one manual-testing runbook (proof lap, phone list, device passes, store tests) - do these
  B10-owner-inputs.md<- your step-by-step list to complete BEFORE B10 can start
  UI-UX.md           <- screens, navigation, battle log contract
  UI-Layout.md       <- containers, the spacing ladder, mobile scaling
  Art-Pipeline.md    <- HD-2D art pipeline, asset volume, perf budgets (G11/G12)
  REVIEW.md          <- deletion candidates / resolved removals
```
