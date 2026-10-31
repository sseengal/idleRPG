# Game Vision — story-led MVP (24 Oct 2026, premise updated 29 Oct 2026)

Status: **CANON** — this replaces the old "endless idle run" goal. Where another doc assumes an endless
run, this document wins. **The premise now lives in `Story-Canon.md`** (the Second Dawn, Greenwake, the
king's order). This page owns the campaign shape and the loop.

---

## 1. The one-line change

The game is no longer an endless climb. It is a **short story campaign with automatic combat**: a
classic Japanese-style role-playing game (JRPG) loop. Fights play themselves. The player chooses where to
go, what to carry, and when to push on. Idle income is not part of the core loop.

Plain words: the player does not grind forever. The player moves through a written story. Fights play
themselves. The player makes the big calls — party, gear, abilities, when to restart.

---

## 2. The core loop

```
story scene  ->  fight (automatic)  ->  reward  ->  choice (party / gear / ability)
      ^                                                        |
      |                                                        v
   retry the node (stronger)  <--  party wipe / defeat     next node
```

1. **Story scene.** A short play inside the battle view: a place, a problem, a person.
2. **Fight.** Combat runs itself, one actor at a time (see `Combat-Sequencing.md`). The player
   watches, presses ability buttons, and can speed the fight up.
3. **Reward.** Gold, gear, an ability, a new party member, or a key item that opens the next node.
4. **Choice.** Spend what was earned. Small, clear choices — not a spreadsheet.
5. **Defeat.** The party falls. The node restarts, and the player keeps permanent upgrades. The story
   explains why (a dawn shard pulls the Guard back; `Story-Canon.md` §4).

Each loop should be short. A run is measured in **nodes**, not in stage numbers.

---

## 3. What dying means

- A wipe ends the **run**, not the account.
- The player returns to the **start of the current node**, not the whole campaign.
- **Permanent upgrades are kept** (the "prestige" layer: the meta-progression that makes the next run
  faster).
- The story reason is designed: a dawn shard pulls the Guard back to the last ground they held
  (`Story-Canon.md` §4). It is not explained until the player has lost once.

Open design question for the team: is a run all chapters, or one chapter? The doc assumes **all
chapters** until the content proves too long. Decide before building the save schema for chapters.

---

## 4. What this means for existing systems

| System | Keep | Change |
|---|---|---|
| Combat sim (`Encounter`) | Everything. One lane, art-measured windows, event-driven pictures | Nothing now. Balance comes later |
| Abilities | The data pipeline from `AbilityDef` to the fight | Now a real action (see `Combat-Sequencing.md`) |
| Stages / waves | The machinery | One **node = one stage**: 9 normal fights + 1 boss |
| Endless scaling | Do not grow it | Replace with authored difficulty per node |
| Prestige / tokens | The permanent-upgrade layer | Reframed as "kept on a wipe" |
| Idle / offline payouts | Kept in code, out of the loop | May return as camp training. Never skips a node |
| Save schema | Kept | Add a story position only when a node is not a stage |

```text
Today                          Target
-----                          ------
stage 1..N, forever            node 1..N, finite
waves 1..10 per stage          one node = one stage (10 fights)
die -> drop one stage          die -> retry the node, keep upgrades
numbers tuned for scaling      numbers tuned per node (later pass)
```

---

## 5. What the player sees

- A **map or chapter view** with story nodes, not an endless ladder.
- A **battle page** that plays itself, with ability buttons the player may press.
- A **short scene inside the battle view**: actors walk in, a bubble shows a feeling, the battle log
  carries the words.
- A **defeat card** that says plainly: "The node restarts. You keep X." Never punish without saying it.

---

## 6. What this is not

- Not a visual novel. Story is short scenes inside the fight view, not long reading.
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
| **Pipeline (art)** | Wins on lead time. Map art and the prologue video start now, not at the end |
| **Delivery** | Bends. The MVP is now "one playable chapter", not "100 stages" |

Longest lead time: **the prologue video and map art**, plus **measured strike frames** for every ability
clip. All must start before the systems are "finished", or the ship date moves.

---

## 8. Next steps

1. Read `Story-Canon.md` (premise, cast, rules) and `Story-Nodes.md` §3 (`S01_Intro`).
2. Build the scene system and the one scene: `Story-Scenes.md` §11.
3. Then build the map and the node select.
4. Then add the title screen and the prologue player.
5. Then re-tune ability and enemy numbers against **one authored node**.

---

## Related docs

- `Docs/Combat-Sequencing.md` — how a fight reaches the screen today (current page).
- `Docs/Roadmap.md` — ordered backlog (needs re-ordering to this vision).
- `Docs/Progression.md` — levels and prestige (the "keep on death" layer).
- `Docs/Stage-Ladder.md` — the old 100-stage ladder (source material for beats, not the plan).
