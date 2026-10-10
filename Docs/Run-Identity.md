# Run-Identity — the defeat modal, run stats, and a run you can see

*Started 2026-10-10. This is the design record for giving the run a visible edge without changing the core loop.*

Status: **built** (Phases 0-2). Phase 3 (content beats) is a recorded, deferred plan - see §6.

---

## 1. What this pass is, in plain words

The game already had the right loop: heroes fight, wipe, fall back one stage, fight on; press ASCEND to cash the run
in for permanent power. What it did not have was any *visible* run: death was a silent 0.75 s pause, and a run had no
boundary.

This pass adds the boundary. On a wipe you now see a short **defeat card**: what you lost, what this run has earned so
far, and your two choices - keep bouncing (it restarts on its own) or **end the run right there** by ascending. When
you ascend, a one-line summary closes the run out. Nothing is ever deleted that was not already deleted by ascension.

## 2. The three pieces

| Piece | What it does |
|---|---|
| **Run stats** (Phase 0) | `RunGoldEarned`, `RunKills`, `RunStagesCleared`, `RunElapsedSeconds` - reset to zero on ascension, persisted in the save. |
| **Defeat modal** (Phase 1) | On a wipe, a card shows the run so far + a countdown + "CONTINUE" (restart now) + "ASCEND" (end the run). It auto-restarts when the countdown ends. |
| **Run-ended summary** (Phase 2) | Ascending (from the card or the ASCEND panel) shows a toast: "Run ended - stage 47 · +14 tokens · 12.3 K gold". |

## 3. Decisions, and why

### The defeat card is a modal, not a toast
The user's call, and it is the right one: a toast cannot carry the ASCEND choice. The card's **auto-restart timer**
keeps the idle promise ("the loop never waits for input") intact - an idle player ignores it and the run resumes.

### The card does not appear on every wipe (cooldown)
A hard wall bounces over and over: fail stage 47, fall to 46, clear it, push 47, fail again. A card on every bounce
would spam the player and slow the idle bounce. So the full card only opens on a **new defeated stage** or after a
**60 s cooldown**; faster repeat wipes use the old 0.75 s beat. (`defeatModalSeconds` **10**, `defeatModalCooldownSeconds`
60, both `BalanceConfig` knobs.) The timer is 10 s on purpose: a first pass at 4 s was too short to read the card or
make the ascend choice (owner's call, 2026-10-10).

### No ad on the defeat card (explicitly rejected)
"Watch an ad to double your gold" was considered and cut, for three reasons recorded in `Monetisation.md` §7: it
would re-pay already-granted gold (breaks the one-measured-rate rule), its value is wrong at both ends (the failed
wave pays pennies; the whole run is a balance bomb), and it is a consolation ad (you lost, here is pity gold). If a
run-boundary ad is wanted later, it is **"double your ascension tokens"** on the ASCEND action - aspirational, one
clean grant point - and it belongs to B7/B10, not here.

### Run stats are additive (no save version bump)
The four run fields default to 0 and re-seed the clock on load, so an older save reads them safely - the same
"additive in schema v5" convention the daily-streak and ad-cap fields already use. No migration step.

## 4. What must not break (and did not)

- **"Never waits for input"** - the card auto-restarts; CONTINUE just skips the wait.
- **"Farmed income is never zero"** - death still costs only the one-stage fallback; no gold is taken or re-paid.
- **The bounce is still the game** - a failed push still drops one stage and re-clears.
- **The robot player / golden numbers** - untouched by this pass; the extra 10 s beat only lands on new stages, so the
  climb cadence is effectively unchanged (verify with `Run All Checks`).

## 5. Files

- `Scripts/UI/DefeatModalUI.cs` (new) - the card; opened by `GameEvents.DefeatShown`, hides when state leaves `Defeat`.
- `Scripts/Core/GameManager.cs` + `.Stages.cs` + `.Snapshot.cs` - run counters, `ResumeAfterDefeat()`, the cooldown,
  the run-ended toast, save capture/apply.
- `Scripts/Core/GameEvents.cs` + `.Raise.cs` - `DefeatShown(defeatedStage, resumeStage)`.
- `Scripts/Save/SaveData.cs` - the four run fields.
- `Scripts/Data/BalanceConfig.cs` - `defeatModalSeconds`, `defeatModalCooldownSeconds`.
- `Editor/MvpSceneBuilder.cs` + `.Overlays.cs` - builds and wires the card.

## 6. Phase 3 (future) - content beats, recorded for later

The second half of "each stage needs weight" is content identity, and it is **deferred to Step 15 (v1.1)**:

| Beat | What it adds | Where it lives today |
|---|---|---|
| Walls (every 5 local stages) | a visible "this stage is hard" spike | `Content.md` §2 (`wallEveryStages` 5), not built |
| Zone boss (every ~25 stages) | a "I finished a thing" milestone | `Content.md` §2 (`stagesPerZone` 25), not built |
| Enemy archetypes (6 x variants) | visual/mechanical variety | `Content.md` §1, 4 enemies exist |
| Affixes (12-20) | modifiers that change *how* a stage reads | `Content.md` §1, not built |
| Milestone gems (every 5th stage) | already live, but only a log line | `BalanceConfig.gemsPerMilestone` = 5 |

**Decision gate:** evaluate this only after the run-identity pass ships and the robot climb re-baselines, because
walls change the very bounce the defeat card is attached to. It is not a day of work; it is a content + difficulty
re-tune.

## 7. Verification

- [x] compiles clean; scene rebuilt; card opens on wipe (stage rolls back, state → Defeat, scrim shows)
- [x] `ResumeAfterDefeat()` restarts the run (state → Combat, card hides) - the CONTINUE button and the timer both call it
- [x] ascend gating correct (button disabled below stage 10, shows "reach stage 10 to ascend")
- [ ] frame-driven check by hand (the tooling cannot run frames while the editor is unfocused): the 10 s countdown
      decrements and auto-resumes; run counters climb on kill/stage-clear; the "Run ended" toast fires on ascend
      (`Manual-Tests.md` §2h)
