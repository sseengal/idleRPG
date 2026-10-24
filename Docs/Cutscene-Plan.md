# Cutscenes - the plan and the ground work

> Status: **ground work shipped (2026-10-24)**. `CutsceneRunner` + `CutsceneDef` + one test shot on the `N` dev key.
> No content uses it yet. This page is the design record.

---

## 1. What a cutscene is (and is not)

A cutscene is a **fixed play**: bars, a line of text, a unit walking in, a clip playing. Its length comes from its own
animations, not from the fight, and nothing in it can change the game - no damage, no loot, no formation.

Two clocks, and they never mix:

| | Who owns time | Length | Can it be compressed? |
|---|---|---|---|
| **Fight beat** | the sim | whatever the fight needs | yes - the picture must never slow the fight |
| **Cutscene** | the cutscene's own animations | fixed ("animation-locked") | no - skipping is the only escape |

## 2. The vocabulary

- **Step** - one thing at one time: `0.30 s: move the boss to the middle`.
- **Verb** - what a step does: `Wait`, `BarsIn`, `BarsOut`, `Text`, `MoveUnit`, `PlayClip`.
- **Sequence** - the ordered list of steps (`CutsceneDef.steps`).

That is the whole model. A fight beat is a short sequence the `BeatDirector` builds from a sim event; a cutscene is a
long sequence read from an asset. Same verbs, different clock.

## 3. Rules that keep it safe

1. **One at a time.** `Play` is ignored while another cutscene runs.
2. **Pause is explicit.** `CutsceneDef.pauseBattle` stops `CombatManager` (a flag on `Tick`); nothing else is frozen.
3. **Skippable or not** - `skippable` ends the play on SPACE / a click.
4. **Hard cap.** `hardCapSeconds` finishes the play even if a step never arrives. A broken step cannot trap a player.
5. **No game state.** The runner can only move views and set text. If a cutscene ever needs to grant something, that
   is a game rule and belongs in the sim, fired before or after the play.
6. **Real time.** The runner uses `Time.unscaledDeltaTime`; if a frame is long (or the app was backgrounded), the
   play jumps to the correct step instead of stretching.

## 4. How to add a cutscene

1. Right click in the Project window -> `Create > Idle RPG > Cutscene`.
2. Fill `steps`: a time, a verb, and its arguments. `MoveUnit.spot` is a **screen fraction** (0..1), so the shot
   survives any resolution.
3. Play it from code: `CutsceneRunner.Instance.Play(myDef)`.

The test shot lives in code (`CutsceneDef.CreateSample`, dev key `N`) because it is a probe, not content.

## 5. What is deliberately not built yet

- **Data authoring comfort** - an inspector preview or a timeline editor. Worth it only when someone other than a
  programmer writes shots.
- **Camera, audio, shake, fades** - new verbs, added when the first real shot needs them.
- **Unity Timeline** - it is built for this, but our units are runtime-built UI images driven by a frame swapper, so it
  would need a custom track anyway. The step/verb shape here maps cleanly onto it if we ever switch.

## 6. QA list for any cutscene

- Plays once, ends, and the fight resumes with the same stage, wave and formation.
- SPACE / click skips; a second press does nothing.
- The hard cap ends it even if the last step is wrong.
- Backgrounding the app and returning does not replay or strand it.
- No numbers, bars or bobbleheads keep moving behind the bars (combat is paused).
- On a 4:3 and a 19.5:9 screen the bars still cover the HUD edges.
