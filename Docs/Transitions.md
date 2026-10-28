# Transitions — the CRT beat

> **Status: built, 28 Oct 2026.** Branch `feature/story-scenes`.
> Normal waves get a thin white-line flicker. Stage changes and camp entry/exit get the full
> CRT close/open. The effect is visual only; combat keeps its own timing.

---

## 1. The two effects

```
normal wave
  thin white line + 12% dark wash, 0.10s
  no blackout, taps stay live

stage or camp
  close  0.12s   black bars grow from top and bottom
  hold   0.03s   bright white line at the seam
  open   0.18s   bars shrink, line fades
  taps blocked while the bars cover the screen
```

No shader, no screen capture, no new art. Two black Images, one white line, one coroutine.

---

## 2. When it plays

```
normal wave last enemy dies
  -> quick flicker
  -> the normal 0.6s wave gap continues
  -> next wave appears

wave before a camp last enemy dies
  -> full close (the next wave is a camp, so the screen holds)
  -> camp opens behind black
  -> full open reveals the camp

camp choice made
  -> full close/open
  -> battle returns

boss dies
  -> full close
  -> stage changes behind black
  -> full open reveals the new stage
```

The effect does **not** pause the game. The existing `waveTransitionDelaySec` is the pause.
Camp already pauses combat for as long as the player takes.

---

## 3. Seams

| Moment | Event | Effect |
|---|---|---|
| Last enemy dies | `CombatDirector.WaveTransitionStarted` -> `CombatManager` | choose full or flicker |
| Next wave/stage spawns | `GameEvents.StageChanged` | open if closed |
| Camp opens | `CampService.Opened` | open if closed; otherwise blink in |
| Camp closes | `CampService.Closed` | full close/open |
| Camp is next? | `CampService.WouldHold(stage, nextWave)` | pure query; makes the wave before camp use the full close |

`WouldHold` checks the same rules as `ShouldHold`, but does not open anything.
The boss check uses `CombatManager.IsBossWave`.

---

## 4. Knobs (BalanceConfig)

| Field | Default | What it does |
|---|---|---|
| `crtEnabled` | on | Master switch |
| `crtWaveStyle` | Flicker | `Off`, `Flicker`, `Full` for normal waves |
| `crtCloseSeconds` | 0.12 | Full close |
| `crtOpenSeconds` | 0.18 | Full open |
| `crtFlickerSeconds` | 0.10 | Normal-wave line |
| `crtFlickerDim` | 0.12 | Peak darkness of the flicker wash |
| `waveTransitionDelaySec` | 0.6 | The actual pause between waves |

- Edit `BalanceConfig.asset` in the Inspector. No code change and no scene rebuild.
- If you regenerate the data assets, these values go back to the generator defaults.
- Keep `crtCloseSeconds + crtOpenSeconds <= waveTransitionDelaySec` for wave and stage.
  If the effect is longer, the next wave starts behind black.
- Camp has no such limit; the camp waits for the player.

### Longer pause

Raise `waveTransitionDelaySec`. The black hold grows and the next wave waits. This slows every
wave and live gold rate; offline gold uses a separate estimate. For a stage-only pause, do not
raise the global knob; add a stage gate later.

### Change the style

Combat only raises `WaveTransitionStarted`. It does not know the word CRT. To replace the look,
edit `CrtTransitionUI` (animation) and `MvpSceneBuilder.Crt.cs` (images/layout). Combat, camp,
save and balance logic are untouched.

---

## 5. Risks

- Normal waves flicker 9 times per stage. Set `crtWaveStyle` to `Off` or `Full` if that feels busy.
- A full transition can cover a modal if a wave clears while one is open. Rare; the overlay is
  above cards by design.
- Fast overlapping events are guarded. A busy transition drops the second request; it never
  leaves the screen black.
- Wipe and story-scene transitions are not part of this pass.

---

## 6. Code

| Piece | Job |
|---|---|
| `UI/CrtTransitionUI.cs` | State, events, coroutines, input blocker |
| `Editor/MvpSceneBuilder.Crt.cs` | Builds the overlay on `ModalCanvas`, above HUD and cards |
| `Combat/CombatDirector.cs` | Raises `WaveTransitionStarted` at the last kill |
| `Combat/CombatManager.cs` | Forwards it to the HUD |
| `Progression/CampService.cs` | `WouldHold` pure query |
| `Data/BalanceConfig.cs` | Timings and mode |

---

## 7. Manual checks

1. Normal wave clear: thin line flicker, no blackout. Next wave visible.
2. Wave 3 clear: full close; camp opens; full open.
3. Camp choice: full close/open; battle returns.
4. Boss clear: full close; stage number changes behind black; full open.
5. F7 camp: full close/open into camp.
6. Full transition blocks taps; flicker does not.
7. Pause or `Time.timeScale = 0`: transitions still finish.
8. `crtEnabled = false`: no overlay; game behaves as before.
9. `crtWaveStyle = Full`: normal waves use the full close/open.
10. `crtWaveStyle = Off`: normal waves show nothing; stage and camp still use the full effect.
