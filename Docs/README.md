# Docs — index

> **Everything that describes this game lives here.** One folder, one source of truth per topic.
> Root `README.md` is the quick start (clone → open → play); everything deeper is in this folder.
> Last restructured: Step 11e (2026-09-21); Art-Pipeline.md added 2026-09-26.

---

## Where to look

| Question | Document |
|---|---|
| What is left to finish, and where are we right now? | **`Checklist.md`** (master record - start here) |
| What is planned next, in what order, with what acceptance? | `Roadmap.md` |
| Why is the code shaped this way? Layers, decisions, rules | `Architecture.md` |
| How does the simulation work (combat, formation, enemies)? | `Sim-Core.md` |
| Content pipeline: specs, generator, validator, enemies/zones | `Content.md` |
| Rates, offline payouts, currencies, time-based monetisation | `Idle-Economy.md` |
| Monetisation plan: sources, sinks, IAP mock, guards | `Monetisation.md` |
| Levelling, prestige, tracks, roster | `Progression.md` |
| Screens, navigation, log contract, accessibility | `UI-UX.md` |
| Art pipeline: HD-2D look, asset volume, tooling, perf budgets (G11/G12) | `Art-Pipeline.md` |

## Status legend (used in every doc)

`DONE` · `IN PROGRESS` · `TODO` · `PARKED` (deliberately deferred) · `OBSOLETE` (kept only until deleted)

## Standing rules (apply to every change)

1. **One sub-step at a time** - implement, recompile, verify, tick the box, hand off for a manual test.
2. **Run the regression surfaces** on every combat-affecting change: golden numbers, content validator, save drift,
   **battle log** (attribution, one line per wave, no mid-line renaming, no flooding).
3. **Parity lever** - force the wave recipe to `1x 100%` and the golden numbers must match the single-enemy
   baseline byte for byte; that proves a change is additive.
4. **Nothing is stored that can be derived** - wave size comes from `hash(stage, wave)` + the recipe, so saves stay
   small and a resumed stage replays the same fights.
5. **The sim never touches Unity** - `Sim/` has no engine references; runtime glue lives in `Combat/`, data in
   `Data/`, presentation in `UI/`.

## Layout

```
Docs/
  README.md          <- this index
  Checklist.md       <- master progress + status
  Roadmap.md         <- ordered backlog with acceptance tests
  Architecture.md    <- decisions (A/B/AD log), layers, data catalog
  Sim-Core.md        <- pure simulation design
  Content.md         <- specs + pipeline + enemies/zones/affixes
  Progression.md     <- currencies, tracks, prestige, roster
  Idle-Economy.md    <- rates, offline, time payouts
  Monetisation.md    <- sources, sinks, IAP mock, guards (B7 done; live-store wiring = B10, see its §6b)
  UI-UX.md           <- screens, navigation, battle log contract
  Art-Pipeline.md    <- HD-2D art pipeline, asset volume, perf budgets (G11/G12)
  REVIEW.md          <- deletion candidates / resolved removals
```
