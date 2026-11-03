# Story Nodes — scripts (2 Nov 2026)

> **Status: `S01_Intro` is built (9 lines, 32 beats). `S01_Mid` and `S01_Outro` are outlines. `S01_King` and `S01_Defeat` are planned.**
> This page owns the words and the beats. It does not own how a scene is played — see `Story-Scenes.md`.
> Canon, cast and rules: `Story-Canon.md`. Chapter story truth: `Story-Greenwake.md`.

---

## 1. Terms

| Term | Meaning in this game |
|---|---|
| **Fight** | One wave. 1-3 enemies. |
| **Node** | One stage. 9 normal fights + 1 boss fight = 10 fights. |
| **Scene** | One short play inside the battle view. Intro 30-50 s; mid and outro 5-12 s. |

A scene has no knowledge of a stage or a wave. The schedule decides where it plays. That is what makes a
scene reusable before a wave, after a wave, at a node start, at a node end, or on a manual call.

---

## 2. Node 1

| Field | Value |
|---|---|
| Id | `stage01` |
| Place | Greenwake Forest |
| Title | The Waking Wood |
| Blurb (map card) | The king's light fell here. The wood is growing wrong. Find the shard. |
| Objective | Recover the first shard and find the way out |
| Boss | Slime King (the bloom knotted around the shard) |
| Scenes | `S01_Intro`, `S01_Mid`, `S01_King`, `S01_Outro`, `S01_Defeat` |

Why the enemies are what they are: the shard leaks stored time. It speeds up living plants and churns
dead matter. The slimes are mud, roots and water moving in the leak. The bats and wolves are real
animals driven out of the bloom. Nothing here is evil. The Guard are cutting through a fever.

The chapter story is locked in `Story-Greenwake.md`. The wave structure is also the geography: waves 1-2 are the bat panic, wave 3 brings the first wolf, wave 4 is the pack, waves 5-6 are the slime wall, and wave 7 is the heart. The first slime is an intro actor, not a wave. Wave 1 must be bats.

---

## 3. `S01_Intro` — built (2 Nov 2026)

**Plays:** before wave 1 of stage 1 (`BeforeWave`, stage 1, wave 1).
**Length:** 39.0 s of automatic content. Hard cap 120 s real time. Skippable. Once per stage.
**Asset:** 32 beats; the slime enters from the left and passes between the party. The party faces it and turns as it passes. No combat.
**Interaction:** tap after each spoken line. **Bubble:** symbols only. **Log:** one line at a time.

### Actors

| Id | Screen name | Art set | Facing | Notes |
|---|---|---|---|---|
| `knight` | Knight | `Knight_Art` | right | Right/front. Orders the party aside, then decides to follow |
| `mage` | Mage | `Wizard_Art` | right | Middle. Patient and thoughtful. Hears the sound first; stops the attack |
| `soldier` | Soldier | `Soldier_Art` | right | Left/back. The audience. Speaks plain; reacts first |
| `slime` | Slime | `Slime_Art` | left | Passes between the party. Slow. Does not attack or look at them |

Art sets live in `Assets/IdleRPG/Data/Characters/`. The Mage uses `Wizard_Art`; the screen name stays “Mage”.

### Sequence

| # | Beat | Action | Dialogue |
|---|---|---|---|
| 1 | Lost | The overgrown road. No birds. No insects. The markers are buried | **Soldier:** We've been walking for hours now. The markers are gone. |
| 2 | Order | The Knight keeps the party moving | **Knight:** We must keep moving. |
| 3 | Sound | The Mage stops. The others halt | **Mage:** Wait. Do you hear that? |
| 4 | Look behind | Shuffling behind them. All three face left. Shock | **Knight:** Move aside! |
| 5 | Split | Mage moves to the same upper line as Soldier. Knight drops low. The Soldier readies his weapon | **Soldier:** Weapons ready! |
| 6 | Stop | The Mage stops the swing before it lands | **Mage:** Wait! |
| 7 | Pass | The slime enters from the left and passes through the vertical gap. The party tracks it: Soldier, then Mage, then Knight turn right as it passes | — |
| 8 | After | The Soldier lowers his blade | **Soldier:** Is that a ... Slime!? |
| 9 | Read | The Knight watches it go | **Knight:** It didn't notice us. |
| 10 | Follow | The Mage watches it move deeper in. The party decides to follow | **Mage:** Let us follow it and see where it takes us. |
| 11 | End | The party follows the slime deeper into the forest | — |

Notes:

- **“Move aside!” comes after the look behind.** All three face left first; then the Knight clears the party out of the way.
- **The split is vertical.** Mage and Soldier stand on the same upper line. Mage and Knight keep facing left and walk backwards to give the slime way. The slime passes through the gap; the party does not overlap it.
- **The party tracks the slime.** They face left when it enters. Soldier turns right as it passes him, then Mage, then Knight.
- **No combat.** The slime passes; the party does not attack. The attack swing is stopped by the Mage.
- **“Is that a ... Slime!?” is the Soldier’s first guess, not canon.** He does not know what the party is dealing with. The slime is churned matter, not a creature (`Story-Greenwake.md` §5).
- **Telling is allowed.** The narrative stage direction “The party decides to follow” is the intended level of narration. Do not replace it with extra dialogue.

### What the log shows

```
Soldier: We've been walking for hours now. The markers are gone.
Knight: We must keep moving.
Mage: Wait. Do you hear that?
Knight: Move aside!
Soldier: Weapons ready!
Mage: Wait!
Soldier: Is that a ... Slime!?
Knight: It didn't notice us.
Mage: Let us follow it and see where it takes us.
```

No line explains the shard, the king, time, or the bloom. The player already saw the video. The party does not know what a slime is yet; the Soldier names it first.

On screen, the log switches to **one-line story mode**: the normal feed hides, the speaker name appears in
gold, and the words type at 45 characters per second. `TAP TO CONTINUE` completes the line; the next tap
moves on. `SKIP SCENE` ends the whole scene. The store keeps the whole line; the typewriter is only a view
effect. See `Battle-Log.md`.

### What this scene must prove

1. The Guard is lost: the road is gone and the markers are buried.
2. The wood is wrong: no birds, no insects, too much green.
3. The Mage hears the sound before the others and stops the attack.
4. All three face left and look behind **before** the Knight says “Move aside!”
5. Mage moves to the same upper line as Soldier; Knight drops low. The Soldier readies; the Mage stops the swing.
6. The slime passes through the gap. The party tracks it: Soldier, then Mage, then Knight turn right.
7. The Soldier asks, “Is that a ... Slime!?” because he does not know better.
8. The Knight says it didn’t notice them; the Mage proposes following it.
9. The party follows the slime deeper in. No combat plays in the scene.
10. Skip and the 120 s cap both end the scene cleanly.

### Wave handoff

Wave 1 must **not** be a slime. The party has just spared one; spawning a slime immediately undercuts the scene.

The first wave is **bats**. `Stage01_WavePlan.asset` is linked to `WaveConfig`: waves 1-2 are bats, wave 3 is bats + the first werewolf, wave 4 is werewolves, wave 5 is slimes + the last werewolf, and wave 6 is slimes. Stage 1 has 6 normal waves; wave 7 is the boss from the boss pool. The old `WaveSizeOverride` that pinned stage 1, wave 1 to one slime has been removed.


---

## 4. `S01_Mid` — outline (not final)

**Plays:** before wave 5 of stage 1 (rule: `BeforeWave`, stage 1, wave 5).
**Length:** about 5 s. Cap 8 s.
**Purpose:** the slime wall. The slimes are not attacking; they are pressed into a choke at the root
lip. They block the only way through. The Guard must clear the path. Three lines only. Do not explain
the shard.

| Time | Verb | Actor | Text | Bubble |
|---|---|---|---|---|
| 0.3 | Enter | slimeA | — | — |
| 1.0 | Say | soldier | They're not fighting. | — |
| 2.0 | Say | mage | They're not. | — |
| 3.0 | Say | knight | They're in the way. Clear it. | — |
| 4.5 | End | — | — | — |

The slimes do not attack in this scene. The fight that follows is an obstacle, not a hunt. The player
should feel the difference. Do not add a line that calls them innocent.

---

## 5. `S01_Outro` — outline (not final)

**Plays:** after the boss of stage 1 (rule: `StageCleared`, stage 1).
**Length:** about 6 s. Cap 10 s.
**Purpose:** confirm the shard, show the cost, close the node, point to the road. The bloom has collapsed.
The ordinary forest survives. The Guard have taken the thing that kept a living pocket alive.

The table below is a placeholder. The final pass must land on cost, not a joke. The old “guarded badly”
line undercuts the chapter and should not survive the rewrite.

| Time | Verb | Actor | Text | Bubble |
|---|---|---|---|---|
| 0.3 | Bubble | mage | — | `…` |
| 1.0 | Say | mage | The shard. It grew a king to guard it. | — |
| 2.6 | Bubble | knight | — | `!` |
| 3.0 | Say | knight | It guarded badly. | — |
| 4.4 | Say | soldier | One piece. The rest are still out there. | — |
| 5.8 | End | — | — | — |

The scene does **not** grant the shard. The grant is a game rule outside the scene. A scene is a picture.

**Story alignment (2 Nov 2026):** “It grew a king to guard it” is the Mage’s reading, not chapter truth.
The Slime King is a knot of roots and mud around the shard; it has no intent (`Story-Greenwake.md` §6).
The image can suggest a guardian. The script must not confirm one.

### `S01_King` — planned (not written)

**Plays:** before wave 7 of stage 1 (`BeforeWave`, stage 1, wave 7).
**Length:** 4-6 s. Cap 8 s.
**Purpose:** show the heart of the bloom. The old hollow tree, the shard inside it, the Slime King wrapped
around it. The King does not speak and does not explain itself. It moves only when the Guard step
between it and the shard.

The scene must establish the boss’s behaviour before combat: it covers the shard, not the road. Keep it
to two lines at most. Do not call it a guardian in dialogue; let the image do that work.

### `S01_Defeat` — planned (not written)

**Plays:** on a party wipe in stage 1.
**Length:** 3-5 s. Cap 8 s.
**Purpose:** show the shard pulling the marked Guard back. The world does not rewind. The same road and
enemies are there. Wounds are closed. Gear is kept. The Guard do not understand why.

No lecture. No explanation of the rule. The player learns it by seeing the same ground again.

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
| Hard cap | Timed scene: content + 5-8 s. Tap mode: 120 s real-time safety net |
| Tap to continue | Optional per scene. One tap per spoken line; first tap finishes typing |
| Skip | Always on; separate `SKIP SCENE` button |
| Scenes that grant rewards | None. Ever |

Why: the node intro is the one place to spend time, and it plays once per stage. A player who skips loses
nothing. Mid and outro stay short so a retry never drags.

---

## 7. How to add the next scene (the 3-step test)

1. Duplicate `S01_Intro` and save it as `S01_Mid` with its own beats.
2. Add one row to `Stage01_Schedule`:
   `BeforeWave · stage 1 · wave 5 · S01_Mid`.
   The planned rows are `BeforeWave · stage 1 · wave 7 · S01_King` and `Defeat · stage 1 · S01_Defeat`.
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
