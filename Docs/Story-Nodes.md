# Story Nodes — scripts (29 Oct 2026)

> **Status: node 1 is the first build target. `S01_Intro` is final; `S01_Mid` and `S01_Outro` are outlines.**
> This page owns the words and the beats. It does not own how a scene is played — see `Story-Scenes.md`.
> Canon, cast and rules: `Story-Canon.md`.

---

## 1. Terms

| Term | Meaning in this game |
|---|---|
| **Fight** | One wave. 1-3 enemies. |
| **Node** | One stage. 9 normal fights + 1 boss fight = 10 fights. |
| **Scene** | One short play inside the battle view. 5-14 seconds. |

A scene has no knowledge of a stage or a wave. The schedule decides where it plays. That is what makes a
scene reusable before a wave, after a wave, at a node start, at a node end, or on a manual call.

---

## 2. Node 1

| Field | Value |
|---|---|
| Id | `stage01` |
| Place | Greenwake Forest |
| Title | The Waking Wood |
| Blurb (map card) | The king's light fell here. The wood is awake. Find the shard. |
| Objective | Recover the first shard and find the way out |
| Boss | Slime King (the shard grew it as a guard) |
| Scenes | `S01_Intro`, `S01_Mid`, `S01_Outro` |

Why the enemies are slimes: the shard wakes matter. Mud, roots and water move. They are not alive and
they are not evil. They only do what the light tells them.

---

## 3. `S01_Intro` — final (30 Oct 2026)

**Plays:** before wave 1 of stage 1 (`BeforeWave`, stage 1, wave 1).
**Length:** 48.4 s of content. Hard cap 54 s. Skippable. Once per stage.
**Bubble:** symbols only. **Log:** nine lines.

### Actors

| Id | Screen name | Art set | Facing | Notes |
|---|---|---|---|---|
| `knight` | Knight | `Knight_Art` | right | Enters first |
| `mage` | Mage | `Wizard_Art` | right | Sees the forest is alive |
| `soldier` | Soldier | `Soldier_Art` | right | Speaks first and last before the order |
| `slime` | Slime | `Slime_Art` | left | Creeps in at 28.3 s |

Art sets live in `Assets/IdleRPG/Data/Characters/`. The Mage uses `Wizard_Art`; the screen name stays “Mage”.

### Beats

| Time | Verb | Actor | Spot (x/y) | Duration | Text | Bubble |
|---|---|---|---|---|---|---|
| 0.0 | — | — | — | — | Empty forest. Fade reveals it | — |
| 0.5 | Enter | knight | .20 / .40 | 1.2 | — | — |
| 1.0 | Enter | mage | .31 / .33 | 1.2 | — | — |
| 1.5 | Enter | soldier | .42 / .27 | 1.2 | — | — |
| 4.2 | Bubble | soldier | — | — | — | `?` |
| 4.8 | Say | soldier | — | — | Something's wrong here. There's not a single bird, not even an insect. | — |
| 9.0 | Bubble | knight | — | — | — | `!` |
| 9.6 | Say | knight | — | — | Stay alert. I don't like how these trees seem to be watching us. | — |
| 13.8 | Bubble | mage | — | — | — | `…` |
| 14.4 | Say | mage | — | — | That's because they are... This forest is alive. | — |
| 18.0 | Bubble | soldier | — | — | Hide the bubble before the next turn | — |
| 18.6 | Say | soldier | — | — | You could've mentioned that before we walked in! | — |
| 22.0 | Bubble | mage | — | — | — | `!` |
| 22.5 | Say | mage | — | — | Quiet. Something's moving. | — |
| 25.3 | WalkTo | knight | .24 / .40 | 1.0 | Looks right | — |
| 26.5 | WalkTo | knight | .20 / .40 | 1.0 | Looks back | — |
| 28.3 | Enter | slime | .80 / .40 | 2.8 | Slow creep | — |
| 31.7 | Bubble | slime | — | — | — | `…` |
| 32.3 | Bubble | knight | — | — | — | `!` |
| 32.9 | Say | knight | — | — | What in the blazes is that? | — |
| 36.3 | Bubble | mage | — | — | Hide the bubble before the next turn | — |
| 36.9 | Say | mage | — | — | A living slime. And it's heading straight for us. | — |
| 41.1 | Say | soldier | — | — | Well, let's give it a warm welcome! | — |
| 44.3 | Bubble | knight | — | — | — | `!` |
| 44.8 | Say | knight | — | — | Weapons ready! | — |
| 47.6 | PlayClip | knight | — | — | `Attack01` | — |
| 47.8 | PlayClip | soldier | — | — | `Attack01` | — |
| 48.4 | End | — | — | — | Wave 1 spawns | — |

The pauses are the gaps: **1.0-1.5 s** before the first line, **0.8-1.0 s** between turns. They are
deliberate. Do not tighten them without watching the whole scene.


### What the log shows

```
Soldier: Something's wrong here. There's not a single bird, not even an insect.
Knight: Stay alert. I don't like how these trees seem to be watching us.
Mage: That's because they are... This forest is alive.
Soldier: You could've mentioned that before we walked in!
Mage: Quiet. Something's moving.
Knight: What in the blazes is that?
Mage: A living slime. And it's heading straight for us.
Soldier: Well, let's give it a warm welcome!
Knight: Weapons ready!
```

No line explains the shard, the king, or time. The player already saw the video.

On screen, the speaker name appears first in gold, then the words type at 45 characters per second. The
store keeps the whole line; the typewriter is only a view effect. See `Battle-Log.md`.

### What this scene must prove

1. The party walks in, settles, and does not start talking immediately.
2. The pauses are visible. No line or bubble sneaks into a pause.
3. Seven bubbles, one at a time, above the correct speaker.
4. Knight's two small steps read as a glance, not a walk to a new place.
5. The slime takes about 2.8 s to arrive. It does not pop in.
6. Nine lines reach the log once, in order, with the right speaker.
7. Combat stays frozen; the wave spawns once after “Weapons ready!” and the fade.
8. Skip and the 54 s cap both end the scene cleanly.

Wave 1 is pinned to **one Slime** (stage 1, wave 1) so the wave matches the single slime on screen.
The pin is a `WaveSizeOverride` in `BalanceConfig`: stage 1, wave 1, count 1.

---

## 4. `S01_Mid` — outline (not final)

**Plays:** before wave 6 of stage 1 (rule: `BeforeWave`, stage 1, wave 6).
**Length:** about 5 s. Cap 8 s.
**Purpose:** raise the stakes. The light is calling the creatures. The Guard decides to follow it.

| Time | Verb | Actor | Text | Bubble |
|---|---|---|---|---|
| 0.3 | Enter | slimeA | — | — |
| 1.0 | Bubble | slimeA | — | `?` |
| 1.6 | Say | mage | The light is calling them. | — |
| 2.6 | Bubble | knight | — | `!` |
| 3.0 | Say | knight | Then we take the light. | — |
| 4.6 | End | — | — | — |

Two lines only. Do not explain the shard.

---

## 5. `S01_Outro` — outline (not final)

**Plays:** after the boss of stage 1 (rule: `StageCleared`, stage 1).
**Length:** about 6 s. Cap 10 s.
**Purpose:** confirm the shard, close the node, point to the road.

| Time | Verb | Actor | Text | Bubble |
|---|---|---|---|---|
| 0.3 | Bubble | mage | — | `…` |
| 1.0 | Say | mage | The shard. It grew a king to guard it. | — |
| 2.6 | Bubble | knight | — | `!` |
| 3.0 | Say | knight | It guarded badly. | — |
| 4.4 | Say | soldier | One piece. The rest are still out there. | — |
| 5.8 | End | — | — | — |

The scene does **not** grant the shard. The grant is a game rule outside the scene. A scene is a picture.

---

## 6. Text budget (hard rules)

| Rule | Limit |
|---|---|
| Node intro lines (first time only) | 8-12 |
| Mid-node / outro lines | 3-4 |
| Words per log line | 12 is the guide. A longer intro line is allowed if its hold is longer |
| Bubble text | One symbol, or 1-3 words |
| Intro length | 30-50 s |
| Mid length | 5-8 s |
| Outro length | 8-12 s |
| Pauses | Intro 0.8-1.6 s. Mid 0.4-0.8 s. Outro 0.6-1.0 s |
| Hard cap | Planned content + 5-8 s |
| Skip | Always on |
| Scenes that grant rewards | None. Ever |

Why: the node intro is the one place to spend time, and it plays once per stage. A player who skips loses
nothing. Mid and outro stay short so a retry never drags.

---

## 7. How to add the next scene (the 3-step test)

1. Duplicate `S01_Intro` and save it as `S01_Mid` with its own beats.
2. Add one row to `Stage01_Schedule`:
   `BeforeWave · stage 1 · wave 6 · S01_Mid`.
3. Press Play. No code change.

If step 3 needs code, the structure is wrong. Fix the structure, not the scene.

---

## 8. Nodes 2-10

One line each until they are written. Names come from `Story-Canon.md` §5 and are provisional.

| Node | One line |
|---|---|
| 2 | Ashen Road — first sign that the army is already moving |
| 3 | River Gate — the Guard must choose what to save |
| 4 | Hollow Village — people who do not believe the warning |
| 5 | The Broken Bridge — the mid-chapter turn |
| 6 | Watchtower — see the army for the first time |
| 7 | Black Camp — first contact with the Black Knight |
| 8 | The Pass — the only road left |
| 9 | Outer Wall — the last gate |
| 10 | Throne of Ash — the Black Knight |

Do not write these until node 1 plays well. Their scenes may need verbs that node 1 does not use.
