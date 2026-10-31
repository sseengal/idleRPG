# Cutscenes - the structure, and what it may never do

> Status: **structure ready, no effects (2026-10-24)**. `CutsceneDef` holds a list of beats; `CutsceneBuilder` writes
> one in a single readable place; `CutsceneRunner` walks the list, pauses the fight and puts it back. It draws nothing -
> there are no bars, no title, no text and no key. The old test effect (bars + title on `N`) was removed by owner
> decision: structure first, effects when a real shot asks for them.
>
> **This page is for fight-bound plays only** - moves and clips on the units already in the fight.
> Story scenes (actors walking in, bubbles, dialogue in the log) are a different system: **`Story-Scenes.md`**.

---

## 1. What a cutscene is (and is not)

A cutscene is a **fixed play**: a list of beats, each with its own time. Its length comes from its own animations, not
from the fight, and nothing in it can change the game - no damage, no loot, no formation move.

Two clocks, and they never mix:

| | Who owns time | Length | Can it be compressed? |
|---|---|---|---|
| **Fight beat** | the sim | whatever the fight needs | yes - the picture must never slow the fight |
| **Cutscene** | the cutscene's own animations | fixed ("animation-locked") | no - skipping is the only escape |

## 2. The vocabulary

- **Step** - one thing at one time: `2.10 s: move enemy 0 to the middle of the screen`.
- **Verb** - what a step does. Only two exist, because a play is a picture:
  - `MoveUnit` - send a unit to a screen fraction (0..1, so a shot survives any resolution)
  - `PlayClip` - play that unit's action strip
- **Sequence** - the ordered list of steps (`CutsceneDef.steps`, sorted by time at build).

A fight beat is a short sequence the `BeatDirector` builds from a sim event; a cutscene is a longer sequence read from
data. Same idea, different clock.

## 3. Rules that keep it safe

1. **One at a time.** `Play` is ignored while another play runs.
2. **Pause is explicit.** `CutsceneDef.pauseBattle` stops `CombatManager` through its `Paused` flag; nothing else freezes.
3. **Skippable or not** - `skippable` ends the play on SPACE / a click.
4. **Hard cap.** `hardCapSeconds` finishes the play even if a beat never arrives. A broken step cannot trap a player.
5. **No game state.** The runner can only move views and play clips. If a cutscene ever needs to grant something, that
   is a game rule and belongs in the sim, before or after the play.
6. **Real time.** The runner uses `Time.unscaledDeltaTime`; a long frame (or a backgrounded app) jumps to the beat it
   should already be on instead of stretching the shot.

## 4. How to author one

**In code - one line per beat:**

```csharp
CutsceneDef shot = new CutsceneBuilder("boss_intro")
    .PauseBattle(true)
    .HardCap(8f)
    .Move(0.30f, CombatantSide.Enemy, 0, new Vector2(0.62f, 0.55f))
    .PlayClip(0.90f, CombatantSide.Enemy, 0)
    .Build();

CutsceneRunner.Instance.Play(shot);
```

**In an asset:** `Create > Idle RPG > Cutscene`, fill the steps, then `CutsceneRunner.Instance.Play(def)`.

Positions are screen fractions of the damage canvas (1080 x 1920 reference), so `(0.5, 0.5)` is the middle on any phone.

## 5. What is deliberately not built

- **Text, bars, fades, camera, shake, audio** - new verbs, added when the first real shot needs them. They are effects,
  not structure, and the owner asked for structure first.
- **An inspector preview / timeline editor** - worth it only when someone other than a programmer writes shots.
- **Unity Timeline** - built for this, but our units are runtime-built UI images driven by a frame swapper, so it would
  need a custom track anyway. This step/verb shape maps onto it cleanly if we switch.

## 6. QA list for any cutscene

- Plays once, ends, and the fight resumes with the same stage, wave and formation.
- SPACE / click skips; a second press does nothing.
- The hard cap ends it even if the last step is wrong.
- Backgrounding the app and returning does not replay or strand it.
- Combat is paused for the whole play (no numbers, no bars moving behind it).
- A move written as `(0.5, 0.5)` lands in the middle of the screen on a 4:3 and a 19.5:9 device.
