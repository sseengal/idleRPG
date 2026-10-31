# Story Scenes — the injection system (as built 31 Oct 2026)

> **Status: current. `BeforeWave`/`AfterWave` gate built; `S01_Intro` plays before wave 1.**
> `StageCleared`, `Defeat` and `Manual` are defined but not hooked yet.
> Desk gate: `Run All Checks` PASS, **31/31** EditMode tests. Phone pass still open.
> This page owns **how** a scene is placed, timed, paused, skipped and resumed.
> It does not own the words: see `Story-Nodes.md`. It does not own the premise: see `Story-Canon.md`.
> The battle log view and typewriter are documented in `Battle-Log.md`.

---

## 1. What a scene is

A scene is a **fixed play**: a list of beats, each with its own time. It draws actors and words. It
cannot damage, heal, move the party, grant loot, unlock anything, or change the story.

Two clocks, and they never mix:

| | Who owns time | Length | Can it be compressed? |
|---|---|---|---|
| **Fight** | the simulation | whatever the fight needs | yes - the picture must never slow the fight |
| **Scene** | the scene's own beats | fixed | no - skipping is the only escape |

A scene has **no knowledge of a stage or a wave**. The schedule decides where it plays. This is the rule
that makes a scene reusable before a fight, after a fight, at a node start, at a node end, or on a
manual call.

---

## 2. The five pieces

| Piece | Type | Job | Changes when content grows? |
|---|---|---|---|
| `StorySceneDef` | asset | one play: actors + beats | yes - new asset per scene |
| `StoryScheduleDef` | asset | when a scene plays | yes - one new row per scene |
| `StoryService` | Core | holds the schedule, holds the fight, queues scenes | no |
| `StoryPlayer` | UI | walks beats on real time; skip; hard cap | no |
| `StoryLayer` | UI | actors, bubble, skip hint, hide the board | no |

Why the schedule is separate from the scene: the same scene may play in more than one place, and two
scenes may be due at the same moment. Putting the trigger inside the scene asset looks simpler today and
forces an asset edit every time pacing changes.

Supporting pieces built around those five:

| Piece | Job |
|---|---|
| `StageTransition` | Black cover/reveal inside the arena only. 0.35 s each way. |
| `BattlePageFocus` | Grows the log and fades the ability band while a scene plays. 0.30 s. |
| `BattleLog` + `CombatLogUI` | Stores the words and draws them. Story lines type; see `Battle-Log.md`. |
| `BalanceConfig.WaveSizeOverride` | Pins one wave size. Stage 1, wave 1 = one Slime. |

---

## 3. Data model

```csharp
enum StoryVerb  { Enter, WalkTo, PlayClip, Bubble, Say, Hide, End }
enum BubbleKind { None, Dots, Bang, Question, Word }
enum StoryCue   { BeforeWave, AfterWave, StageCleared, Defeat, Manual }
enum WaveMode   { Any, Exact, EveryN, Random }
enum PlayLimit  { EveryTime, OncePerStage, OncePerRun }
```

### A. Scene (`StorySceneDef`)

| Field | Type | Default | Meaning |
|---|---|---|---|
| `id` | string | required | Stable id. Used by save and random rolls. Never rename after ship |
| `pauseBattle` | bool | true | Stop the fight for the whole scene |
| `skippable` | bool | true | One tap (or Space) ends it |
| `hardCapSeconds` | float | 14 | Ends the scene even if a beat is wrong. Never 0 |
| `actors` | `StoryActorDef[]` | required | Up to 6 in v1 |
| `beats` | `StoryBeat[]` | required | Sorted by `time` on validate |

`StoryActorDef`: `id`, `displayName`, `art` (`CharacterArtSet`), `facesLeft`, `scale`.

`StoryBeat`: `time`, `verb`, `actorId`, `spot` (0..1 inside the viewport), `duration`, `text`,
`bubble`, `clip`.

Rules:

- `Enter` start side is inferred: `spot.x < 0.5` walks in from the left edge, else from the right.
- `Say` stores `displayName` as the speaker and `text` as the words. The log colours the name gold
  and types the words at 45 characters per second. The scene never writes raw log text or markup.
- `Bubble` shows one symbol, or 1-3 words when `bubble = Word`. Bubble symbols are instant; the
  typewriter applies to the log, not the bubble.
- `End` finishes early. The hard cap is the safety net.
- An unknown `actorId` is a validation error, not a silent skip.

### B. Schedule (`StoryScheduleDef`)

One rule = one place a scene can play.

| Field | Type | Default | Meaning |
|---|---|---|---|
| `cue` | `StoryCue` | required | Which moment |
| `scene` | `StorySceneDef` | required | What plays |
| `minStage` | int | 0 | 0 = any. 1-based otherwise |
| `maxStage` | int | 0 | 0 = any |
| `wave` | `StoryWaveSpec` | Any | Which fight, for wave cues |
| `priority` | int | 0 | Higher plays first |
| `playLimit` | `PlayLimit` | EveryTime | Repeat rule |
| `maxPerStage` | int | 0 | 0 = no cap. Caps `Random` rules |

`StoryWaveSpec`: `mode`, `wave` (Exact), `everyN` (EveryN), `offset` (EveryN, default 0), `chance` (Random, 0..1).

---

## 4. Injection points

A **cue** is a moment in the run. Each cue maps to one code seam. New cues are added only when a new
seam exists; a cue is never faked by a timer.

| Cue | Moment the player sees | Code seam | Status |
|---|---|---|---|
| `BeforeWave` | Before a fight starts. Wave 1 = node intro; wave 6 = mid; wave 10 = boss intro | `CombatDirector.WaveGate(stage, wave)` | **v1** |
| `AfterWave` | After a fight ends, before the next one starts | Same gate, matched against `wave - 1` | next slice (same gate) |
| `StageCleared` | After the boss dies, before the stage advances | `GameManager.OnStageCleared` hook | next slice |
| `Defeat` | After a party wipe, before the retry | `GameManager.OnPartyWiped` hook | next slice |
| `Manual` | Any time a caller asks | `StoryService.PlayNow(scene)` | debug + tutorials |

Notes:

- `BeforeWave` and `AfterWave` are the **same instant** in code: the gate is asked just before the next
  wave spawns. `AfterWave N` and `BeforeWave N+1` are two names for that moment. Both are allowed. Priority
  decides which plays first, and the queue (§6) plays both.
- `AfterWave` is **not** evaluated after the boss. The boss has no next wave, so the gate is never asked.
  Use `StageCleared` for anything after the boss.
- A `Random` rule can land on the boss wave. If that is wrong for the scene, add an exact rule, an
  `EveryN` rule, or a stage range that keeps it away.
- There is no `RunStart` cue. A node intro is `BeforeWave`, stage 1, wave 1. Fewer cues, no special path.
- `Manual` during a scene is ignored. One play at a time, always.

---

## 5. Matching rules

At a cue, a rule matches when **all** of these are true:

1. `cue` equals the current cue.
2. The stage is inside `[minStage, maxStage]`. A zero bound means "no bound".
3. The wave spec matches (wave cues only).
4. The play limit has not been reached.
5. `maxPerStage` has not been reached.

### Wave modes

| Mode | Matches when | Example use |
|---|---|---|
| `Any` | Every wave | A rare event that the play limit controls |
| `Exact` | `wave == spec.wave` | Intro at wave 1, mid at wave 6 |
| `EveryN` | `(wave - offset) % everyN == 0` | A reminder every 3 fights |
| `Random` | `Hash(...) < chance` | "Before a random wave" |

### Random must not change on reload

Do **not** use `UnityEngine.Random`. A reload would re-roll and a player could save-scum a scene in or
out. Use a stable hash:

```
h = FNV1a(scene.id + "|" + stage + "|" + wave + "|" + ruleIndex)
roll = h / 2^32            // 0..1
matched = roll < chance
```

Consequences, stated plainly:

- The same stage, wave and scene always rolls the same. Relaunching changes nothing.
- Reordering the schedule changes `ruleIndex`, so rolls change. Freeze the order before ship.
- Changing `chance` changes the outcome. That is intended.

### Priority and ties

- Higher `priority` plays first.
- Equal priority: the earlier row in the asset plays first.
- Priority never lets a non-matching rule play. It only orders the matches.

### Play limits

| Limit | Meaning | Storage now |
|---|---|---|
| `EveryTime` | Plays at every match | none |
| `OncePerStage` | Plays at most once in a stage | memory |
| `OncePerRun` | Plays at most once until the run restarts | memory |

Counters are keyed by **rule row**, not by scene. The same scene used twice on purpose counts twice. A
stage counter resets when the stage changes; a run counter resets on a new run.

Memory-only means a relaunch replays a "once" scene. Accepted for the slice. Persist in save v12 when
the run needs it.

### Worked examples

Copy these shapes. They cover the three requests: before a fight, after a fight, before a random wave.

| Intent | Cue | Stage range | Wave spec | Limit | Priority |
|---|---|---|---|---|---|
| Node 1 intro | `BeforeWave` | 1-1 | `Exact 1` | `OncePerStage` | 10 |
| Mid-node beat | `BeforeWave` | 1-1 | `Exact 6` | `OncePerStage` | 5 |
| Boss warning | `BeforeWave` | 1-1 | `Exact 10` | `OncePerStage` | 5 |
| After fight 5 | `AfterWave` | 1-1 | `Exact 5` | `OncePerStage` | 0 |
| Random ambush | `BeforeWave` | 1-10 | `Random 0.15` | `OncePerStage` | 0, `maxPerStage 1` |
| Node 1 outro | `StageCleared` | 1-1 | `Any` | `OncePerStage` | 10 |
| First-wipe line | `Defeat` | 1-10 | `Any` | `OncePerRun` | 10 |

Note on the random row: `maxPerStage 1` stops a 15% roll from firing twice in one node. Without it, a
stage can show the scene in several fights, which reads as a bug.

---

## 6. More than one scene at one moment

Two rules can match the same wave. Example: the node intro (priority 10) and a tutorial (priority 5).

The service does this:

1. Collect every matching rule.
2. Sort by priority, then by row order.
3. Play them **in order**, without resuming the fight between them.
4. Resume the held wave once, after the last scene.

Why a queue: `CombatDirector.ResumeAfterGate()` spawns the wave immediately. If the service resumed after
the first scene, every later scene at that moment would be silently lost. The queue makes the loss
impossible.

Safety cap: **3 scenes per hold**. A fourth match is skipped and logged as a content error. A bad schedule
must never trap a player behind a long chain of scenes. The cap is a guard, not a design target.

If a scene is interrupted by the hard cap or a skip, the queue continues. Player input can never strand a
wave.

---

## 7. The playback contract

These rules hold for every scene, at every cue.

| Rule | Behaviour |
|---|---|
| **Pause** | `CombatManager.Paused = true` for the whole play. No swings, no numbers, no HP change |
| **Hide** | Formation board, enemy stack and ability band hide. The battle log stays visible and readable |
| **Abilities** | `RequestAbility` returns false while paused. No cast can queue behind a scene |
| **One at a time** | A second play request is ignored or queued. Never two scenes |
| **Skip** | One tap, or Space. A second tap does nothing. Skipping loses nothing |
| **Hard cap** | Real time, not fight time. A broken beat can never trap the player |
| **Real time** | `Time.unscaledDeltaTime`. A long frame jumps to the beat it should be on |
| **Background** | Return after the cap: the scene ends. No stuck state, no replay |
| **Resume** | The held wave spawns with the same stage, wave and formation |
| **Page** | **Not forced yet.** The scene owns the battle page only if the player is already there. Forcing the page is an open gap |
| **No state** | A scene cannot grant, unlock, damage, heal or move the party |

Bubble rules:

- One bubble in the whole layer. It attaches above the speaking actor.
- x is clamped so the bubble never leaves the viewport.
- The bubble hides when its actor moves. It never follows mid-walk.
- Symbols only by default. A word bubble is 1-3 words and never a full sentence.

Log rules:

- `BattleLogKind.Dialogue`, written by `BattleLog.Say(name, line)`.
- Story lines type out at **45 characters per second** in real time. The speaker name appears at once,
  then the words reveal one character at a time. Combat lines always appear whole.
- A new line finishes the previous one first. Ending or skipping a scene finishes the current line.
- The log store keeps the full text, so a page rebuild shows complete history, never half-typed lines.
- Speaker names are gold; the words are near-white. All log lines use the 48 VT323 role.

### Transitions (built 30 Oct 2026)

The scene enters and leaves through a **fade inside the arena only**.

- `StageTransition` is a black overlay under the battle stage root. It covers actors and damage
  numbers, never the header, log, nav or popups.
- `Cover` 0.35 s -> swap the picture -> `Reveal` 0.35 s. Unscaled time.
- Scene clock and hard cap start **after** the reveal. Fades are not scene time.
- Skip works only while content runs; it starts the closing fade.
- A fade can be interrupted. A skip during the opening reveal starts the exit cover from the current alpha.
  It must never leave the scene in its exit phase.
- While the arena is black, `StoryLayer.Begin/End` swaps the board, enemy stack and actors.

`BattlePageFocus` owns the lower half while a scene plays:

- The log grows from `0.00-0.28` to `0.00-0.39` (the ability band's top), in 0.30 s.
- The ability band fades to alpha 0, then deactivates. It cannot be tapped.
- Both happen under the black. The reverse runs on the closing fade.
- The log is pinned to its newest line while the box changes height.

Still to come: force the battle page when a scene starts.

Scene length: a first-time node intro may run **30-50 s**. Mid and outro stay short. The budget is in
`Story-Nodes.md` §6.

### Boot order (must not lose the first scene)

The first scene can be requested during `GameManager.Start`, before the HUD's `Start` has created the
player. If the service raised a normal event there, the wave would stay held with nobody listening.

Required rule:

1. `StoryService` stores a **pending** scene. It does not assume a listener exists.
2. `StoryPlayer` binds in `Start` and pulls the pending scene immediately.
3. If no player binds within 1 second, the service resumes the wave and logs an error.

This is the only reason `StoryService` and `StoryPlayer` are separate. Do not replace the pending pull
with a plain `event Action` at boot.

---

## 8. Authoring

### Folder and naming

```
Assets/IdleRPG/Data/Story/
  Stage01/
    S01_Intro.asset
    S01_Mid.asset
    S01_Outro.asset
    Stage01_Schedule.asset
  Stage02/
    ...
```

- `S<node>_<Cue>.asset`. A scene that is reused in two nodes lives in `Data/Story/Common/`.
- One scene asset = one scene. Never a "story" asset with everything in it.
- A schedule holds only rules. It never holds beats or text.

### Add a scene

**Fast path (current assets):** edit `AgentScripts/SeedStoryAssets.cs`, then rerun its seeder. The seeder
deletes and recreates `S01_Intro.asset` and `Stage01_Schedule.asset`, so review the diff after it runs.

**Normal path (new scene):**

1. Create the asset: `Assets > Create > Idle RPG/Story/Story Scene`. Save it under the node folder.
2. Set a stable `id`. Never rename it after ship; random rolls and future save data use it.
3. Fill `actors`: `id`, `displayName`, art set, facing, scale. Up to six.
4. Fill `beats` in time order. Use `Enter` for the first appearance, `WalkTo` for later moves,
   `Bubble` for a symbol or short word, `Say` for a log line, `End` for an early finish.
5. Set `hardCapSeconds` to content length + 5-8 s. It is a safety net, not pacing.
6. Add one rule to the node's schedule (`Assets > Create > Idle RPG/Story/Story Schedule`, or the
   existing node schedule): cue, stage range, wave rule, scene, priority, play limit.
7. Assign the schedule to `GameManager.storySchedule` in `Main.unity` if this is a new node schedule.
8. Press Play. Check the gate, the fade, the log, the skip and the wave spawn.

If step 8 needs code, the structure is wrong. Fix the structure, not the scene.

### Current intro anatomy

`S01_Intro` is the reference scene:

- four actors: `knight`, `mage`, `soldier` on the right, `slime` on the left;
- 27 beats, 48.4 s of content, 54 s hard cap;
- 9 `Say` lines and 7 bubbles;
- party enters, pauses, talks, looks around, slime creeps in, knight calls weapons ready,
  `Attack01` flourish, `End`;
- stage 1, wave 1 is pinned to exactly one Slime so the wave matches the picture.

Use it as the shape for a new intro, but do not copy its words. The script lives in `Story-Nodes.md`.


---

## 9. Extension policy

The system grows by adding data and verbs. It must not fork.

| Need | Add | Do not |
|---|---|---|
| A new action (camera shake, fade, sound) | One `StoryVerb` value + one case in `StoryLayer` | A new runner, or code inside a scene asset |
| A condition (only if the party is wounded) | `StoryCondition[]` on the rule, checked in `StoryService` | An `if` inside the player |
| A branch or choice | A new verb that ends the scene with a result, read by the service | A second scene format |
| A one-time tutorial | A `Manual` rule + `OncePerRun` | Hard-coded checks in a UI page |
| Localisation | Text keys on the beat, resolved at play time | Language checks in the player |
| Persisted play limits | Save v12 list of played scene ids | Writing to PlayerPrefs |

Rules:

1. No scene-specific code. Ever.
2. A scene never reads game state. If it must react, the service selects a different scene.
3. A scene never changes game state. Rewards and unlocks are game rules, before or after the play.
4. Six actor slots and one bubble in v1. Raise both only when a real scene needs it, and raise them in
   one place (`StoryLayer`).

---

## 10. Integration map (as built)

### Runtime flow

```
CombatDirector wants to start a wave
  -> CombatManager wave gate
  -> Story.ShouldHold(stage, wave)  ||  Camp.ShouldHold(stage, wave)
  -> StoryService queues matching rules
  -> HudController.PlayStory -> StoryPlayer.Play
  -> StoryPlayer walks beats on real time
  -> StoryLayer draws actors and bubbles
  -> Say beats call BattleLog.Say
  -> CombatLogUI draws and types the line
  -> scene ends -> StoryService resumes the held wave once
```

The gate is one delegate in `GameManager.Wiring.cs`:

```csharp
combatManager.SetWaveGate((stage, wave) => Story.ShouldHold(stage, wave) || Camp.ShouldHold(stage, wave));
```

Short-circuit matters: if a story holds the wave, the camp must not also open. They never target the
same wave today; keep the order anyway.

### Runtime files

| File | Job |
|---|---|
| `Scripts/Data/StorySceneDef.cs` | `StoryVerb`, `BubbleKind`, `StoryActorDef`, `StoryBeat`, `StorySceneDef`. |
| `Scripts/Data/StoryScheduleDef.cs` | `StoryCue`, `WaveMode`, `PlayLimit`, `StoryWaveSpec`, `StoryRule`, schedule asset. |
| `Scripts/Core/StoryService.cs` | Matching, priority, queue, play limits, hold/resume, stable random roll. |
| `Scripts/Core/GameManager.Story.cs` | `storySchedule` field and `Story` service property. |
| `Scripts/Core/GameManager.Wiring.cs` | Creates the service; composes the wave gate. |
| `Scripts/Core/GameManager.Stages.cs` | Calls `Story.Tick` so a binding timeout cannot stall a wave. |
| `Scripts/UI/Story/StoryPlayer.cs` | Real-time clock, beat dispatch, skip, hard cap, fade hand-off. |
| `Scripts/UI/Story/StoryLayer.cs` | Six actor slots, one bubble, skip overlay, hide/restore board and enemy stack. |
| `Scripts/UI/Story/StageTransition.cs` | Arena-only black cover/reveal. Interruptible fade. |
| `Scripts/UI/BattlePageFocus.cs` | Log grows; ability band fades out and becomes inert. |
| `Scripts/UI/HudController.cs` | Creates `StoryPlayer`, binds it to `StoryService`, owns `CompleteStoryText`. |
| `Scripts/Core/BattleLog.cs` | `Dialogue` kind, `Say(speaker, line)`, `Speaker` field. |
| `Scripts/UI/CombatLogUI.cs` | Gold speaker, 48 VT323, typewriter, wrapping. See `Battle-Log.md`. |
| `Scripts/Combat/CombatManager.cs` | `RequestAbility` returns false while paused; `ResumeAfterGate`. |
| `Scripts/Debug/DebugHotkeys*.cs` | `P` replays the wired debug scene. |
| `Scripts/Data/BalanceConfig.cs` + `WaveComposition.cs` | Authored wave size override. |
| `Scripts/Tests/BattleLogTests.cs` | Store contract and speaker split. |

### Assets and scene wiring

| Asset / object | Job |
|---|---|
| `Data/Story/Stage01/S01_Intro.asset` | Current intro: 4 actors, 27 beats, 48.4 s content, 54 s cap. |
| `Data/Story/Stage01/Stage01_Schedule.asset` | One `BeforeWave` rule: stage 1, wave 1, `OncePerStage`, priority 10. |
| `Data/Config/BalanceConfig.asset` | `waveSizeOverrides`: stage 1, wave 1, count 1. |
| `HudController.debugStoryScene` | Scene used by `P`. |
| `HudController.combatLog`, `abilityStrip` | Wired by the builder. |
| `Main.unity` | Story layer is created at runtime; no prefab scene object. |

### Later cues

One call each; no new system:

| Cue | Where |
|---|---|
| `StageCleared` | `GameManager.OnStageCleared`, after rewards, before the stage advances |
| `Defeat` | `GameManager.OnPartyWiped`, before the auto-resume |
| `Manual` | Any caller: `GameManager.Story.PlayNow(scene)` |

### Known gaps in this slice

- Phone aspect and a real-device background test are open.
- The battle page is not forced when a scene starts. A scene behind a menu is still possible.
- Play limits are memory-only. A relaunch can replay a "once" scene.
- `StageCleared`, `Defeat` and `Manual` cues are defined, not hooked.
- Word bubbles are not typewritten; only log dialogue is.

---

## 11. Build order (first slice: `BeforeWave`)

| # | Step | Test |
|---|---|---|
| 1 | Write the data types and both assets | Compiles. The scene shows its beats in the inspector |
| 2 | Build `StoryLayer` + `StoryPlayer`; `P` plays the scene over a live fight | 3 heroes and 1 slime enter; bubbles and 9 log lines; wave resumes |
| 3 | Add the `Dialogue` log kind + the ability pause guard | 9 typed lines, gold names, no cast queued |
| 4 | Add `StoryService` and the `BeforeWave` gate | Press Play -> intro -> wave 1. Once. One Slime. No double spawn |
| 5 | Skip, hard cap, background, phone aspect | Tap completes the line and skips. Return from background leaves no stuck state |
| 6 | `Run All Checks` + manual QA | PASS, 31/31 EditMode; phone pass still open |

`AfterWave`, `StageCleared` and `Defeat` are specified here but **not built** in the first slice. Each is a
single hook when its scene exists.

---

## 12. QA checklist

- Scene plays at the intended cue and only once per hold.
- Fight is frozen: no swings, no numbers, no HP change.
- Board and enemy stack hide; the log stays visible.
- Six actors stay inside the frame on a tall and a short phone.
- Bubble sits above the head, never leaves the frame.
- Skip works by tap. A second tap does nothing. The current line completes first.
- Hard cap ends a scene with a broken beat and completes its line.
- Background and return: no stuck wave, no replay.
- Queue: two scenes at the same wave both play, in priority order.
- `Random` rule plays the same waves after a relaunch.
- A wave never spawns twice, and no wave is skipped.
- Story log: name gold, words near-white, 48 VT323, typed at 45 cps; combat lines whole.
- Wave 1: exactly one Slime, matching the single slime on screen.
- Zero console errors.

---

## 13. Not in this slice

Map, campaign state, title screen, node select, auto-advance removal, offline removal, ascension, New
Game+, branching choices, portraits, voice, localisation, a custom scene editor window, new art.

---

## 14. Open decisions

| # | Decision | Recommendation |
|---|---|---|
| 1 | Six actor slots, or eight? | Six. Raise only when a scene needs it |
| 2 | Queue several scenes at one moment, or play one? | Queue. Losses are silent and hard to spot |
| 3 | Persist "played" flags now? | No. Memory now; save v12 when a scene must not replay |
| 4 | Force the battle page when a scene starts? | Yes. A scene behind a menu is a bug. **Still open** |
| 5 | Bubble tail? | No tail in v1. Add a sprite later if it reads badly |
| 6 | Typewriter speed? | 45 cps. Tune only if the long Soldier line reads too fast or too slow |
