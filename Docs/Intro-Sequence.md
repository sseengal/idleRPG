# Intro Sequence — the premise, then repeat

> Owns: the first-play intro that explains why the fight repeats, and the short lines that keep the loop in fiction.
> Parents: `Story-Canon.md` (facts and rules), `Story-Scenes.md` (how a scene plays), `Run-Loop.md` (the loop).
> Status: **plan, 2026-11-21.** Nothing built. The two opening scenes (`S01_Intro`, `S02_Rupture`) already play at
> stage 1 wave 1; this page adds the **premise film** in front of them.
>
> **Word note:** in this page a "card" means a **full-screen slide** — one picture and one sentence. It is **not**
> a boon card and not anything the player picks.

---

## 1. The job

In **25–30 seconds**, tell the player four facts and nothing more:

1. The kingdom fell.
2. The king broke time to buy one more day.
3. One shard fell into Greenwake.
4. The King's Guard must take the road and stop the Black Knight early.

Then the game repeats battles without more story. The player does not need to understand the shard yet.

**Canon guard (important):** `Story-Canon.md` §4 rule 5 says a shard *"can pull the Guard back to the last
ground they held… It is not explained until the player has lost once."*
So the intro sells **the world repeating**, not the personal revive. The first wipe explains the revive.
Do not put "you will respawn" in the intro.

---

## 2. Where it plays

| Moment | Plays? | Why |
|---|---|---|
| Fresh install, first boot | **Yes**, once, skippable | sets the premise before the first fight |
| Resume from a save | **No** | the player already knows |
| After a wipe / NEXT RUN | **No** | the loop is the game |
| "Replay intro" in settings | Yes, later | cheap, and good for the store build |

Flow: `Boot` → spline → **INTRO** → `Main` lobby → START → `S01_Intro` → battle.

Implementation choice (see §5): either a small UI sequence on the lobby canvas or the existing
`CutsceneRunner`. The intro must not depend on the fight being alive.

---

## 3. The script (6 shots, ~25 s, text on screen, no voice)

All lines are short enough for one card each. `SKIP` is visible from shot 1.

| # | Picture | Text on screen | Time |
|---|---|---|---|
| 1 | Black. Gold king's seal (`ui_frame` plate, `UiTheme.Gold`) | **The Black Knight took the kingdom.** | 4 s |
| 2 | The seal cracks; gold light | **The king broke the Second Dawn. Time went back.** | 5 s |
| 3 | `Greenwake1-clean 1.png`, slow push in | **One shard fell into the Greenwake.** | 5 s |
| 4 | Three guard sprites at the tree line (`Knight`, `Wizard`, `Soldier`) | **He sent only his Guard.** | 4 s |
| 5 | A thin light passes over the three | **The light marked them.** | 4 s |
| 6 | Title plate | **KINGSGUARD** · `TAP TO BEGIN` | hold |

Then the existing `S01_Intro` plays inside the battle view.

**Word budget:** 33 words. No character names beyond the cast (`Story-Canon.md` §3). No lecture.

**Optional last card for the store build:** `DIE. REMEMBER. RETURN.` — only if the first-wipe reveal is still
kept for the mechanic. Owner decision (see §7).

---

## 4. The repeatable arc after the intro

No more scenes are needed for v1.0. The loop stays in fiction with four lines, all data:

| Moment | Line shape | Source |
|---|---|---|
| Run start | one line about the road | `StoryLayer`, per band |
| New stage band (1, 10, 20…) | one line about the place | `JourneyCatalog` |
| Boss step | the boss name replaces the land name | `JourneyBandUI` |
| Run over | one epitaph line | `DefeatModalUI` |

Rule: **one line, one moment, never a gate.** If a line would need two sentences, it is a scene, not a line.

Suggested v1.0 lines (owner may change the words):

- Run start: `The road is long. Go quietly.`
- Band 1 (Greenwake): `The wood is growing wrong.`
- Boss: `Slime King` (on the band)
- Run over (first time): `The shard pulls you back. It remembers.`
- Run over (after): `The road is still there.`

---

## 5. Two build paths

| Path | What it is | Size | Risk | Verdict |
|---|---|---|---|---|
| **A. In-engine motion graphic** | UI images, slow push/pull, fades, text cards, optional placeholder tone. Reuses `Greenwake` art. Played by a small `IntroSequence` component or `CutsceneRunner`. | S | none new; skip/replay easy; no file size | **Recommended for v1.0** |
| B. Real MP4 + `VideoPlayer` | a rendered clip imported into `StreamingAssets` or as a VideoClip | M | file size (10–40 MB), encoder, audio sync, platform codecs, store build size | do it only if the look demands it |

Path A also gives the store a clean screenshot: the title card. If the intro later becomes an MP4, the
script above does not change.

### 5a. Placeholder now, film later (owner decision 2026-11-21)

**Build the placeholder. The owner supplies the real film later.** The placeholder must not block the loop and
must not need a new save version.

| Part | Decision |
|---|---|
| What plays now | the six cards above, built in-engine (UI images, fades, text). No video file, no codec. |
| Where it sits | one root object above the lobby (`IntroSequence`). It is the only thing on screen until skip/end. |
| "Already seen" flag | **PlayerPrefs** key `IdleRPG.introSeen` (the same pattern as the mute key). No save-schema bump. A full reset (F8) wipes PlayerPrefs, so a reset replays the intro — which is correct. |
| Skip | visible from shot 1. Skip sets the flag and opens the lobby. Never leaves a paused or stacked canvas. |
| First play | only when the flag is absent. A resumed save never plays it. |
| Swap seam | when the real film exists, add a `VideoPlayer` to the **same root** and play the clip there. Keep the cards as the fallback when no clip is assigned, so a missing file can never softlock boot. |
| Sound | optional placeholder tone now; real audio with the audio pass. Mute must silence it. |
| Store build | the intro may be replayed from settings later. Not required for v1.0. |

The placeholder is **not** a throwaway: the six cards are the storyboard for the film, and the timing and skip
rules are the same either way.

---

## 6. Acceptance

- [ ] Fresh install: the intro plays once, is skippable from the first shot, and lasts ≤30 s.
- [ ] `SKIP` lands in the lobby or in `S01_Intro`; never in a stuck or double-paused state.
- [ ] Resume from a save never plays the intro.
- [ ] Every word uses `UiGlyphs` (no missing-glyph boxes).
- [ ] Mute silences the intro tone; the visual still reads.
- [ ] The intro can be replayed from settings (later, not v1.0 critical).
- [ ] The first wipe still explains the shard revive (canon rule 5), even if the intro used the optional card.
- [ ] 0 console errors on fresh boot and on skip.
- [ ] Placeholder only: `IdleRPG.introSeen` is absent on a fresh install, written by skip and by the last card, and
      wiped by F8 so the intro replays after a reset. No save-schema bump.

---

## 7. Open decisions

1. **Optional last card** `DIE. REMEMBER. RETURN.` — keep the shard reveal for the first wipe (canon), or
   show this line in the intro and weaken canon rule 5? Recommendation: keep canon; use the card only after the
   first wipe.
2. ~~Path A or B~~ **Decided 2026-11-21:** placeholder cards now (§5a); the owner supplies the real film later,
   played from the same root.
3. **Exact store title** — the script uses **KINGSGUARD**. If the store name is "Kingsguard Tiny RPG", the title
   card may carry a small "TINY RPG" kicker, or not.
4. **Sound** — placeholder tone now, real music with the audio pass.

---

## 8. Not in this slice

- Voice acting, lip sync, cutscene actors walking in 3D.
- A second film for later lands or the Black Knight.
- Localisation of the intro (string table is v1.1).
- Any story that gates content. The loop never waits for a scene.
