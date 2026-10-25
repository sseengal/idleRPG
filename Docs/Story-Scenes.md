# Story Scenes — stage 1 demo (high-level plan)

> **Status: plan only, 25 Oct 2026. No code yet.** Branch: `feature/story-scenes`.
> Scope: **stage 1 only**. Intro scene, mid scene, final scene. No map, no save changes, no new art.

---

## 1. The demo the player sees

```
stage 1 starts
  INTRO SCENE      hero walks in · bubble … · log line · 3 enemies appear · bubble !
  waves 1-5        normal automatic fight
  MID SCENE        short exchange, then the fight resumes
  waves 6-10       normal automatic fight
  BOSS INTRO       optional; before the boss wave
  boss fight
  FINAL SCENE      short close, then stage 2 starts
```

"Next stage" is stage 2, because there is no map yet. The map replaces that hand-off later.

**Stage count note.** You said 10 stages, 10 waves each. The code and `Stage-Ladder.md` currently say 100 stages, and the director uses **10 normal waves + 1 boss = 11**. The demo does not depend on either answer. Decide before stage 2 content:

- 10 stages total, or 100?
- 10 waves **including** the boss, or 10 normal + boss?

Mid scene is planned after **wave 5**. Boss intro is planned before **wave 11** (or wave 10 if the total becomes 10).

---

## 2. The two gates

The fight must stop **before** a wave spawns, not after. Today `CombatDirector` clears a wave and spawns the next one in the same call.
Two small seams fix that:

```
Gate 1 — before a wave
  BeginWave(stage, wave)
    story due?  yes -> wait. scene plays. ResumeAfterStory() -> spawn the wave
                no  -> spawn now
  Covers: stage intro (wave 1), mid scene (wave 5), boss intro (wave 11)

Gate 2 — before the stage advances
  boss dies -> GameManager.OnStageCleared
    story due?  yes -> wait. final scene plays. then advance
                no  -> advance now
```

One seam for every in-stage scene. One seam for the end. Nothing else in combat changes.

---

## 3. The scene layer

Inside the existing battle viewport, so the **battle log stays visible**:

```
Viewport
  backdrop
  formation board        hidden while a scene plays
  enemy stack            hidden while a scene plays
  StoryStage             NEW
    StoryActor           Image + CharacterAnimator (idle / walk / attack)
    SpeechBubble         9-slice panel: text, …, !, ?
  damage canvas          above, unchanged
Combat log               below, visible, fed by BattleLog.Append
```

- The log carries **words**: `Bran: We hold here.`
- Bubbles carry **feeling**: `…`, `!`, `?`, or one short word.
- Scenes pause combat. They never damage, heal, or grant loot.
- When the scene ends, the wave spawns in the real enemy slots and the board returns.

Why not the old `CutsceneRunner`: it only moves the current fight's units and plays attack clips. It cannot walk a hero in, show a bubble, or spawn a story actor. The old runner stays for fight-bound plays.

---

## 4. Data, not code

**`StorySceneDef`** (ScriptableObject) — one scene: actors + timed steps.
```text
0.4s  Bran enters from the left
2.0s  Bran bubble: …
2.6s  log: "Bran: So the stone remembers us."
3.4s  Slime A / B / C enter from the right
5.0s  Bran bubble: !
5.6s  end
```

**`StoryPlan`** (ScriptableObject) — rules that decide when a scene plays.
```text
StageStart, stage 1        -> st1_intro
WaveStart 5, stage 1       -> st1_mid
BossStart, stage 1         -> st1_boss   (optional)
StageEnd, stage 1          -> st1_final
```

- No rule = **no scene**. That is the "may or may not happen" switch.
- A per-stage override is just a narrower rule. No code per scene.
- The plan is data, so stage 2+ needs rules and scenes, not a new system.

**Stage 1 content:** 3 scenes (intro, mid, final) + one optional boss intro. All actors use existing art sets (Knight, Slime). No new art.

---

## 5. Text and idle rules

- **Budget:** intro 8-15 s, mid 5-8 s, final 8-10 s. Maximum 6 log lines per scene. Bubble text 1-3 words.
- **Skippable:** one tap ends the scene. A second tap does nothing.
- **Hard cap:** every scene has a maximum length even if a step is wrong.
- **Auto-advance:** steps advance on their own timers. The idle player is never stuck waiting for a tap.
- **Story setting (later):** `Full / Short / Off`. For the demo, scenes always play; skipping is the escape.

The scene is a picture, never a rule. If a scene should grant something, that grant belongs before or after it in game code.

---

## 6. Build order

| Phase | What | Done when |
|---|---|---|
| **P0** | This doc + decisions on stage count and wave count | You approve |
| **P1** | Scene layer only: `StoryActor`, `SpeechBubble`, `StoryRunner`, debug key to play a test scene over the current fight. No wave gate. | Hero walks in, bubble, log line, 3 enemies appear, combat pauses and resumes |
| **P2** | Gate 1 + `StoryPlan`; stage 1 intro before wave 1 | Play → intro → wave 1 |
| **P3** | Same gate at wave 5 (mid scene) | Wave 5 clears → scene → wave 6 |
| **P4** | Boss intro + Gate 2 (final scene before stage advance) | Boss dies → final → stage 2 |
| **P5** | Stage 1 polish + phone pass | Skips, hard cap, backgrounding, no stuck waves |

Each phase is one session or less, and each ends with a manual test. Do not start the next phase before the last one is proven.

---

## 7. Non-goals for this branch

- World map, walker, fog, zone art.
- Save fields for seen scenes.
- Death reset / chapter restart.
- Scenes for stages 2-10.
- Voice, portraits, localisation, timeline editor.
- Changing combat balance or wave composition.

---

## 8. Open decisions

| # | Decision | Recommendation |
|---|---|---|
| 1 | 10 stages or 100? | 10 for the demo; resolve before stage 2 |
| 2 | 10 waves total or 10 normal + boss? | Keep the code: 10 normal + boss |
| 3 | Enemy actors in a scene: authored, or the real upcoming wave? | Authored for stage 1; real wave later |
| 4 | Replay scenes every stage start, or first clear only? | Every time, skippable (no save fields) |
| 5 | Bubbles with text, or symbols only? | Symbols + 1-3 words; the log carries sentences |

