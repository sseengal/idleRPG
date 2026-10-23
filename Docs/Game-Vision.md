# Game Vision — story-led MVP (24 Oct 2026)

Status: **CANON** — this replaces the old "endless idle run" goal. Where another doc assumes an endless
run, this document wins.

---

## 1. The one-line change

The game is no longer an endless climb. It is a **short story campaign with automatic combat**: a
classic Japanese-style role-playing game (JRPG) loop, made idle-friendly.

Plain words: the player does not grind forever. The player moves through a written story. Fights play
themselves. The player makes the big calls — party, gear, abilities, when to restart.

---

## 2. The core loop

```
story beat  ->  fight (automatic)  ->  reward  ->  choice (party / gear / ability)
      ^                                                        |
      |                                                        v
   restart from the beginning (stronger)  <--  party wipe / defeat
```

1. **Story beat.** A short scene pushes the plot: a place, a problem, a person.
2. **Fight.** Combat runs itself, one actor at a time (see `Combat-Presentation.md`). The player
   watches, presses ability buttons, and can speed the fight up.
3. **Reward.** Gold, gear, an ability, a new party member, or a key item that opens the next beat.
4. **Choice.** Spend what was earned. Small, clear choices — not a spreadsheet.
5. **Defeat.** The party falls. The run goes back to the start, and the player keeps permanent
   upgrades. The story explains why later.

Each loop should be short. A run is measured in **story beats**, not in stage numbers.

---

## 3. What dying means

- A wipe ends the **run**, not the account.
- The player returns to the **first story beat**.
- **Permanent upgrades are kept** (this is the "prestige" layer: the meta-progression that makes the
  next run faster).
- The story reason is **not designed yet**. Write it once the first three beats exist.

Open design question for the team: is a run all chapters, or one chapter? The doc assumes **all
chapters** until the content proves too long. Decide before building the save schema for chapters.

---

## 4. What this means for existing systems

| System | Keep | Change |
|---|---|---|
| Combat sim (`Encounter`) | Everything. One lane, art-measured windows, event-driven pictures | Nothing now. Balance comes later |
| Abilities | The data pipeline from `AbilityDef` to the fight | Now a real action (see `Combat-Presentation.md`) |
| Stages / waves | The machinery | Becomes **story beats**: one beat = one encounter (or a short chain) |
| Endless scaling | Do not grow it | Replace with authored difficulty per beat |
| Prestige / tokens | The permanent-upgrade layer | Reframed as "kept on death" |
| Idle / offline payouts | Kept, smaller | Must not skip story beats |
| Save schema | Kept | Needs a "story position" field, not a "highest stage" field |

```text
Today                          Target
-----                          ------
stage 1..N, forever            chapter -> beat 1..N, finite
waves 1..11 per stage          an encounter per beat (1..3 waves if it reads better)
die -> drop one stage          die -> restart the story, keep upgrades
numbers tuned for scaling      numbers tuned per beat (later pass)
```

---

## 5. What the player sees

- A **map or chapter view** with story beats, not an endless ladder.
- A **battle page** that plays itself, with ability buttons the player may press.
- A **story card** before and after a beat: who spoke, what changed, what was earned.
- A **defeat card** that says plainly: "The story restarts. You keep X." Never punish without saying it.

---

## 6. What this is not

- Not a visual novel. Story is short cards between fights, not long reading.
- Not an endless idle game. There is an ending.
- Not a hard tactics game. Automatic combat; the player's lever is preparation.
- Not a balance pass. Numbers stay as they are until the loop exists end to end.

---

## 7. Team view (who wins, who bends)

| Discipline | Verdict |
|---|---|
| **Design / narrative** | Wins. Story gates content, so beats must land before more systems are built |
| **Balance** | Bends. Authored beats replace scaling; numbers are re-tuned after the first playable beat |
| **Monetisation** | Bends. Fewer "endless" sinks, more chapter/ability unlocks. Fairness guard stays |
| **Compliance / privacy** | No change yet. Consent and store rules are untouched |
| **QA** | Wins early. A restart-on-death loop must be tested from a real save, not a debug flag |
| **Pipeline (art)** | Wins on lead time. Beat art (maps, story cards) and per-clip strike frames start now, not at the end |
| **Delivery** | Bends. The MVP is now "one playable chapter", not "100 stages" |

Longest lead time: **art for beats** (map, portraits, story cards) and **measured strike frames** for
every ability clip. Both must start before the systems are "finished", or the ship date moves.

---

## 8. Next steps (once approved)

1. Write the first chapter outline: 3 beats, one fight each, one choice each.
2. Define the story-position save field (replaces "highest stage" as the run anchor).
3. Build one beat end to end: story card → fight → reward → next beat.
4. Then make death restart the chapter and keep upgrades.
5. Then re-tune ability and enemy numbers against **one authored fight**.

---

## Related docs

- `Docs/Combat-Presentation.md` — how a fight reaches the screen (built contract).
- `Docs/Roadmap.md` — ordered backlog (needs re-ordering to this vision).
- `Docs/Progression.md` — levels and prestige (the "keep on death" layer).
- `Docs/Stage-Ladder.md` — the old 100-stage ladder (source material for beats, not the plan).
