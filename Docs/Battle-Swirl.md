# Battle Swirl — the arena whirl that starts a fight

The old-school transition that hands a finished story scene over to the wave it was holding: the
**arena picture winds into a whirlpool**, drains to black, the fight **fades in**, and combat starts
only when the fade is done.

ELI5: the battle viewport is a live picture. This effect twists that picture into a spiral until it is
black, then the new fight fades back in like an old JRPG.

Status: **built 2026-11-04**, in use at the story → wave hand-off.

---

## 1. Where it runs

| Piece | File | Job |
|---|---|---|
| Shader | `Assets/IdleRPG/Art/Shaders/UI_Whirl.shader` | Polar twist + pull to centre + drain to black. UI pass, so it obeys the viewport's `RectMask2D` clip. |
| Material | `Assets/IdleRPG/Resources/UI_Whirl.mat` | The knobs. Loaded by name, so the shader is always in the build. |
| Driver | `Assets/IdleRPG/Scripts/UI/BattleSwirl.cs` | Puts the material on the arena display and drives `_Progress` 0 → 1 over `coverSeconds`. |
| Arena hook | `Assets/IdleRPG/Scripts/UI/BattleStageSurface.cs` | `Display` (the RawImage that shows the stage render texture) and `SetDisplayMaterial(...)`. |
| Trigger | `Assets/IdleRPG/Scripts/UI/Story/StoryPlayer.cs` | Decides when the whirl plays and runs the hand-off while the arena is black. |
| Gate signal | `Assets/IdleRPG/Scripts/Core/StoryService.cs` | `IsHolding` (a wave is waiting) and `HasQueuedScenes` (another scene follows). |

There is **no screen capture**. The arena is already a RenderTexture (`BattleStageSurface` renders the
stage through its own camera), so the "captured frame" the technique normally needs is the live
picture itself. That is why there is no hitch and the fight can be swapped behind the black.

## 2. The beat

| Time | What happens | Code |
|---|---|---|
| 0.00 – 2.00 s | The arena winds into a spiral, then drains to black. Full brightness for most of the beat; the dim runs `0.85 → 1.0` of progress. | `BattleSwirl.Update` → `_Progress` |
| at 2.00 s | Black. The stage curtain snaps up (`SnapCovered`) and the picture is restored behind it (`Release`). | `StoryPlayer.OnCoveredForExit` |
| same frame | The scene is dropped and the wave is released, so the enemies spawn **behind the black**. | `StoryPlayer` hand-off |
| 0.00 – 0.45 s after | The fight fades in from black. The enemies stand still. | `StageTransition.Reveal` |
| at 0.45 s | Combat unpauses. First swing, first damage number. | `StoryPlayer.ResumeFighting` |

The HUD (header, battle log, buttons) **never moves and never dims** — the material is only on the
arena display, which is clipped by the viewport mask.

## 3. When it plays, and when it must not

The whirl belongs to **"a scene ends and its wave begins"**. Both must hold:

1. `StoryService.IsHolding` — a wave is waiting behind the story.
2. `StoryService.HasQueuedScenes` is false — no other scene follows.

Never plays:

- **between scene 1 and scene 2** — a scene is still queued;
- **on a debug deck jump** — `HudController.DebugStepStory` calls `SuppressNextWhirl()`, because that
  jump releases the wave itself and the replay then runs over a live fight;
- **when the story player has no arena surface or no material** — it logs an error and stays off.

The plain fade stays for every other exit.

## 4. Knobs

| Knob | Where | Default | Effect |
|---|---|---|---|
| `coverSeconds` | `BattleSwirl` (serialized) | **2.0** | Length of the whirl, before the black hold. |
| `_Twist` | `UI_Whirl.mat` | **28** | How hard the picture winds. Higher = tighter spiral. |
| `_Arms` | `UI_Whirl.mat` | **3** | How many spiral arms the wind breaks into. |
| `_Radius` | `UI_Whirl.mat` | **0.85** | Radius where the wind starts to weaken toward the rim. |
| fade-in | `StoryPlayer.BattleRevealSeconds` | **0.45** | Fade from black to the fight. |

Drain-to-black window: `smoothstep(0.85, 1.0, progress)` in the shader.

## 5. Testing

- **`Y`** in Play mode: replays the whirl alone (no story, no fade). Logs `Battle whirl (Y): playing.`
  It is a temporary hotkey and is listed in the F3 overlay.
- **Real flow**: fresh boot, play both scenes without the arrow keys. The whirl must play at the end of
  scene 2 only.
- If it does not play, the console says why on that frame:
  `[StoryPlayer] '<scene>' faded out: no wave was waiting, so no battle whirl.`
- Shader check without playing: blit the material to a RenderTexture at `_Progress` 0 / 0.5 / 0.9 / 1.
  Expect a whole picture at 0 and 0.5, a dimmed picture at 0.9, black at 1.

## 6. Rules that keep it correct

- **One owner.** Only `BattleSwirl` writes `_Progress`; nothing else touches the display material.
- **Always restore.** `Release()` is called on every path, including `StoryPlayer.OnDestroy`.
- **No pop.** The picture is restored only while the curtain is fully black.
- **Battle waits.** `Combat.Paused` stays true through the whirl and the fade; the wave is spawned but
  never ticks. Unpausing happens in the fade callback, not before it.
- **No un-whirl.** The reveal is a fade, never a reverse swirl.

## 7. Reusing it

`BattleSwirl` is arena-only and independent of stories. Any future "enter a fight" beat can call:

```csharp
swirl.Play(() =>
{
    transition.SnapCovered();   // hide the arena
    swirl.Release();            // restore the picture behind the curtain
    // swap what is on stage here
    transition.Reveal(0.45f, OnFadedIn);
});
```

Only one whirl can run at a time; `Play` returns false while another is running.

## 8. Limits

- It covers the arena only, by design. A camera post effect cannot reach the uGUI HUD, and a screen
  capture would add a frame hitch for no gain.
- The wave is composed before the fade, so the fade shows it standing still. That is intended.
- Known unrelated noise in the Console: `IndexOutOfRangeException` in `UnityEngine.UI.Selectable.OnEnable`
  (a uGUI fault when many runtime buttons are enabled). It is not part of this effect.
