# Motion — one language for every animation

> Owns: how things move, flash and glow. Status: **built 2026-11-11** (dot glow, step flash).
> Parents: `Journey.md` (the band), `UI-UX.md` (screens), `Art-Pipeline.md` (asset side).
> Nothing here changes gameplay. It changes how the game feels when it changes.

---

## 1. Two different things (do not mix them)

| Want | What it is | Tool |
|---|---|---|
| **Glow** | A halo drawn behind a shape | A sprite. Not a tween, not a shader (yet) |
| **Motion** | Breathe, flash, pop, slide | A tween library |

A tween library cannot make a glow. A sprite cannot animate itself. The two are separate on purpose.

---

## 2. The library decision

**LitMotion 2.0.2** (`com.annulusgames.lit-motion`, MIT).

| Candidate | Verdict |
|---|---|
| **LitMotion** | **Chosen.** MIT, one UPM git line, zero allocation, sequences and delays, uGUI bindings (`BindToColorA`, `BindToAnchoredPosition`) |
| PrimeTween | Runner-up. Free, excellent API, but the licence needs a careful read and it installs from the Asset Store |
| DOTween | Rejected. Older, paid PRO tier, licence is stricter about source distribution |
| Timeline | Rejected. Authoring tool, wrong weight for a 180 ms flash |

Install record (in `Packages/manifest.json`):

```
"com.annulusgames.lit-motion": "https://github.com/annulusgames/LitMotion.git?path=src/LitMotion/Assets/LitMotion"
```

**Current state (2026-11-11):** the package is in the manifest, but this editor session never registered its
assemblies - the git import happened while the project was in a compile error, and a reinstall did not
fix it. A restart of the Unity Editor should register it. Until then, `GlowView` uses a small built-in
pulser (one `Update`, one sin, no allocation) behind the same API. Swapping to LitMotion is confined to
that one file.

Rule: **only `GlowView` may reference a motion library.** If the library changes, one file changes.


---

## 3. The glow system

Three pieces, so every widget glows the same way:

| Piece | What it does |
|---|---|
| `GlowStyle` (asset) | Colour, peak/min alpha, halo scale, pulse seconds, flash seconds |
| `GlowView` (component) | Owns one halo image and plays its state |
| `Juice.Glow(view, state)` | The only call a screen makes |

States:

| State | The player sees | Motion |
|---|---|---|
| `Off` | nothing | alpha 0, motion cancelled |
| `Steady` | a soft aura | constant mid alpha |
| `Pulse` | a slow breath | min -> peak -> min, 1.6 s, loops forever |
| `Flash` | one bright pop | peak -> 0 in 180 ms, once |

Rules:

1. **Only the current step breathes.** Nothing else loops in the band.
2. **One flash at a time.** A step clearing flashes that mark once.
3. **Alpha caps**: halo peak 0.55, steady 0.30. It must never wash out a neighbour.
4. **Halo size is 2.6x the mark.** A 20 px dot gets a 52 px halo. The band is 107 tall, so it fits.
5. **Reduced motion**: `Pulse` becomes `Steady`, `Flash` becomes `Off`. State stays readable.

Why a sprite and not a shader: at 20 px the halo is 10 px wide. A shader costs a material and a draw
call and looks identical. Shaders earn their keep on cards, later.

---

## 4. The timing ladder

Every cue picks a duration from this list. No new numbers.

| Duration | Use |
|---|---|
| **120 ms** | press feedback, micro nudge |
| **180 ms** | flash, tick, mark clear |
| **250 ms** | pop, card open |
| **400 ms** | slide, reward reveal |
| **1.6 s** | idle pulse (loop) |

Easing: `OutQuad` for anything arriving, `InOutSine` for loops. Nothing else, until a cue needs it.

---

## 5. Loudness tiers

| Tier | Examples | Rule |
|---|---|---|
| 1 Chrome | step glow, chips | small, slow, never above 0.3 alpha |
| 2 Reward | level-up flash, affordable button | one shot, under 400 ms |
| 3 Arena | crit flash, shake, boss | one at a time, never on chrome |

Only one tier-3 cue may run at once. The band owns at most one loop.

---

## 6. Budgets and traps

| Rule | Why |
|---|---|
| Animate **alpha and scale**, never width/height | A size change forces a canvas layout rebuild during a fight |
| One halo image per glowed mark | One live glow = one extra image |
| Cancel before starting | Two pulses on one mark read as a bug |
| Filter mode per sprite: point for stepped, bilinear for soft | Wrong filter turns a pixel halo to mush |
| Halo lives on a child larger than the mark | A UI Image clips its own material at its rect |
| No glow on inactive pages | Tweens cost frame time whether or not the player is looking |

---

## 7. Where it is used today

| Element | State |
|---|---|
| Current step | `Pulse` |
| Step that just cleared | `Flash` once |
| Camp due | `Steady` amber |
| Boss live | `Pulse` |
| Everything else | `Off` |


---

## 8. Acceptance checks

1. The current step breathes at about 1.6 s. No other mark moves.
2. Clearing a step flashes it once, then it stays lit.
3. Two marks never flash together.
4. With reduced motion on, nothing breathes and nothing flashes.
5. Console is clean for a whole run.
6. No GC allocation while the band is idle (the pulse is one bound motion, not a coroutine per frame).
7. The glow never covers a neighbouring dot's face.
