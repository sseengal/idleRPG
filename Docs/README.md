# Docs — index

> **Everything that describes this game lives here.** One folder, one source of truth per topic.
> Root `README.md` is the quick start (clone → open → play); everything deeper is in this folder.
> Last restructured: Step 11e (2026-09-21); Art-Pipeline.md added 2026-09-26; Launch-Plan.md added 2026-10-11; Character-Catalog.md, Stage-Ladder.md and Ability-Map.md added 2026-10-11; UI-Layout.md added 2026-10-14; Combat-Sequencing.md added 2026-10-24; doc consolidation pass 2026-10-24 (one current page per topic); Foundation.md added 2026-10-28; Story canon + node scripts + scene injection system added 2026-10-29; Battle-Log.md added 2026-10-31.

---

## One current page per topic

Each topic has **one** page that says how the game works today. Older pages stay as the record of how a
decision was reached. A replaced page must carry a status line at the top and point at the current page.

| Topic | Current page | Kept as the record |
|---|---|---|
| Fight order, strip choice, timings, enemy cast timing | **`Combat-Sequencing.md`** | `Combat-Presentation.md` (23 Oct research + 24 Oct build log) |
| Story premise, cast, artifact rules, video split | **`Story-Canon.md`** | — |
| Node scripts: beats and words | **`Story-Nodes.md`** | — |
| Story scene injection: cues, wave rules, queue, skip | **`Story-Scenes.md`** | — (old one-node plan replaced 29 Oct 2026) |
| Battle log: store, view, colours, typewriter, layout | **`Battle-Log.md`** | — |
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
| What is the story premise, cast and artifact rules? | **`Story-Canon.md`** (canon: the Second Dawn, Greenwake, video vs in-game split) |
| What happens in each node, and what are the exact words? | **`Story-Nodes.md`** (node 1 script, text budget, 3-step add-a-scene test) |
| How is a story scene injected before or after a fight? | **`Story-Scenes.md`** (system: cues, wave rules, random, priority, queue, skip, resume) |
| How does the battle log store, colour and type text? | **`Battle-Log.md`** (store, view, 48 VT323, gold names, typewriter, wrapping) |
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
| How do I clean up the project without changing the game? | **`Cleanup-Checks.md`** (before/after gate: regression net, 14 EditMode tests, 5-minute smoke) |
| What foundation are we building on? | **`Foundation.md`** (locked pipeline, arena layer, prefab and service policy) |
| Why is the code shaped this way? Layers, decisions, rules | `Architecture.md` |
| How does the simulation work (combat, formation, enemies)? | `Sim-Core.md` |
| Content pipeline: specs, generator, validator, enemies/zones | `Content.md` (also: **when the golden numbers are re-recorded**) |
| Rates, offline payouts, currencies, time-based monetisation | `Idle-Economy.md` |
| Monetisation plan: sources, sinks, IAP mock, guards | `Monetisation.md` |
| Levelling, prestige, tracks, roster | `Progression.md` |
| Screens, navigation, type scale, accessibility | `UI-UX.md` (log contract: `Battle-Log.md`) |
| Containers, the spacing ladder, mobile scaling | **`UI-Layout.md`** |
| Art pipeline: HD-2D look, asset volume, tooling, perf budgets (G11/G12) | `Art-Pipeline.md` |

## Status legend (used in every doc)

`DONE` · `IN PROGRESS` · `TODO` · `PARKED` (deliberately deferred) · `OBSOLETE` (kept only until deleted)

## Standing rules (apply to every change)

1. **One sub-step at a time** - implement, recompile, verify, tick the box, hand off for a manual test.
2. **Run the regression surfaces** on every combat-affecting change: content validator, save drift, **battle log**
   (attribution, one line per wave, no mid-line renaming, no flooding) and the golden numbers as a **printed
   reading, not a gate**. The golden baseline is **VOID until the full rebalance after feature complete**
   (`Content.md` §10): `Run All Checks` ends FAIL on those four lines by design, and the baseline file is not edited
   until then.
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
  README.md            <- this index
  -- current --
  Game-Vision.md       <- CANON: story-led campaign, automatic combat, restart on death
  Story-Canon.md       <- CANON: the Second Dawn, cast, Greenwake, video vs in-game
  Story-Nodes.md       <- node scripts: beats, words, text budget
  Story-Scenes.md      <- how a scene is injected, timed, skipped, resumed
  Battle-Log.md        <- log store, view, colours, typewriter, wrapping
  Camp.md              <- the wave interlude: Short/Long Rest, Train, token, save, risks
  Checklist.md         <- master status: what is done, what is left
  Cleanup-Checks.md    <- the before/after gate for consolidation (no behaviour change)
  Combat-Sequencing.md <- how a blow reaches the screen: lane, timings, enemy casts, the one beat planner
  Cutscene-Plan.md     <- fixed plays (structure only today): steps, rules, QA
  Ability-Progress.md  <- ability status: what works, what is open
  Ability-Plan.md      <- the locked ability decisions
  Ability-Map.md       <- hero / enemy -> ability id
  Character-Catalog.md <- all 42 characters, enumerated
  Stage-Ladder.md      <- 100 stages, 10 bands, bosses
  Architecture.md      <- layers, decisions (B/AD log), save contract
  Sim-Core.md          <- pure simulation design
  Content.md           <- specs, generator, validator, enemies/zones
  Progression.md       <- currencies, tracks, prestige, roster
  Idle-Economy.md      <- rates, offline payouts
  Monetisation.md      <- sources, sinks, IAP mock, guards
  UI-UX.md             <- screens, navigation, type scale (log contract: Battle-Log.md)
  UI-Layout.md         <- containers, the spacing ladder, mobile scaling
  Art-Pipeline.md      <- HD-2D look, asset volume, perf budgets
  Audio-Plan.md        <- cues, music, licences, the audio build order
  Manual-Tests.md      <- the one manual runbook
  Launch-Plan.md       <- art + audio + store launch, sequenced
  B10-owner-inputs.md  <- what you must do before B10 starts
  Roadmap.md           <- ordered backlog with acceptance
  REVIEW.md            <- deletion candidates and resolved removals
  -- record (kept for the reasoning; each opens with a pointer to the current page) --
  Combat-Presentation.md   -> Combat-Sequencing.md
  Refactor-Plan.md         -> Architecture.md
  Mobile-Readiness.md      -> Launch-Plan.md, Manual-Tests.md
  Party-Page.md            -> UI-UX.md, UI-Layout.md
  Upgrades-Page.md         -> UI-UX.md, Progression.md
  Equipment-MVP-Plan.md    -> Inventory-AS-BUILT.md
  Inventory-Tab-Plan.md    -> Inventory-AS-BUILT.md
  Inventory-AS-BUILT.md    -> as-built: gear + bag, save v6
  Leaderboard-MVP.md       -> as-built: mock field, no backend
  Run-Identity.md          -> as-built: defeat modal + run stats
```

A page in the record group must open with a **RECORD, not current** line and name the page that replaced it.
A new doc that describes behaviour goes in the current group, or it is not finished.
