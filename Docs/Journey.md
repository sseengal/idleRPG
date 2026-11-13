# Journey — the words on the battle screen

> Owns: what the player reads above the fight, and the marks under it.
> Parents: `Story-Greenwake.md` (the chapter), `UI-UX.md` (screens), `UI-Layout.md` (bands).
> Status: **built 2026-11-11** — title band, resolver, trail model.
> Nothing here changes combat. The words change; the fight does not.

---

## 1. Why this page exists

The battle screen used to say `Stage 12   wave 3`. Those are engine words. They break the fiction, and
they will break again the moment the world grows, because a "stage" is not a place.

Plain words: the player is walking through Greenwake. The screen should name the land and show how far
along the road the party stands. No counters.

---

## 2. The four words

| Word | Means | Shown to the player? | Code word |
|---|---|---|---|
| **Land** | The named country the party is in | **Yes**, above the trail | `stage` (unchanged) |
| **Place** | Where the party stands inside the land | Not yet - reserved | `wave` range |
| **Step** | One fight | Only as a mark | `wave` (unchanged) |
| **Camp** | A rest offered at a fixed cadence | As a flame mark | `campEveryWaves` |
| **Leg** | One full pass through a land: steps, camps, boss | Never | `stage` |

Rules:

1. The player never reads **stage** or **wave**.
2. Numbers stay in the save, the debug overlay and the tooling.
3. A word that is not in this table does not go on the battle screen.

---

## 3. What the band shows

The title band is 100 units tall, between the header and the arena.

```
+------------------------------------------+
|                GREENWAKE                 |   Land, centred
|        [] [] []  ^  [] []  ^  <>         |   Trail, centred
+------------------------------------------+
```

| Part | Size | Rule |
|---|---|---|
| Land name | 40 (Section) | Centred, one line, upper case |
| Trail | 20 px step marks, 12 px gaps (20 px beside a camp) | Centred under the name |
| Band frame | 3 px lighter edge (`PanelLight`) | A dark ink edge on a dark page is invisible - use a lighter edge |
| Boss step | The name slot swaps to the boss name | Same font, same place |


There is no separate "boss" banner. On the boss step the land name is replaced by the boss name. One
slot, two states. That is the cheapest story the band can carry.

---

## 4. The trail

Marks appear in fight order, left to right:

| Mark | Shape | Size | Off | On |
|---|---|---|---|---|
| Step, cleared | filled circle | 20 | - | amber |
| Step, ahead | filled circle | 20 | muted | - |
| Step, current | circle + ring behind | 20 (ring 26) | ring | amber ring |
| Camp (placeholder) | hollow circle | 24 | muted | amber |
| Boss (placeholder) | diamond (rotated square) | 30 | muted | amber + ring |



Rules:

1. **At most two lit marks, and only this pair.** The current step (ring) and an open camp (ring)
   may show together, because a camp opens on the step that was just cleared. Nothing else lights up.
2. Shape carries the meaning, not colour: filled, empty, hollow ring, diamond.
3. The trail is **not a button**. It never takes a tap.
4. The trail is built from the stage plan and `campEveryWaves`. Never from a fixed count of ten.

Placeholders: the camp is a hollow circle and the boss is a rotated square. Both are drawn from one
white dot sprite and plain rectangles, so **no art is needed** to run the band. Real 16 x 16 pixel
icons replace them later: the mark is a builder call, not an asset.

---

## 5. Motion

| Moment | Motion | Time |
|---|---|---|
| Step cleared | mark scales 0.7 -> 1.15 -> 1, flashes amber | 0.18 s |
| Camp becomes available | flame brightens, one soft ring | 0.25 s, then still |
| Camp used | flame settles to dim | 0.20 s |
| Boss arrives | diamond pulses once; name swaps | 0.25 s |
| Leg starts | marks draw left to right | 0.30 s |
| Idle | **nothing** | - |

Rules:

1. Scale and colour only. Never animate a width: that forces a layout rebuild during a fight.
2. One pulse at a time.
3. The **reduced motion** setting turns every scale and ring off. State changes still land, instantly.
4. The arena owns the loud motion. The band owns at most one beat.

---

## 6. Data

`JourneyCatalog` (ScriptableObject, `Assets/IdleRPG/Data/Config/JourneyCatalog.asset`):

| Field | Meaning |
|---|---|
| `stagesPerLand` | How many stages one land covers (default 10) |
| `lands[].name` | The land name shown on the band |
| `lands[].bossName` | Shown on the boss step |
| `lands[].places[]` | **Optional.** `{ name, fromStep, toStep }`. Empty = land name only |

`JourneyResolver` is pure:

- `Resolve(catalog, stage, step)` -> `JourneyLabel { Land, Place, Boss }`
- `BuildTrail(normalSteps, currentStep, campEverySteps)` -> ordered list of marks

A missing catalog falls back to `Greenwake`, so the band is never blank during development.

---

## 7. Greenwake today

One land, no place names, no numbers. The band reads `GREENWAKE` for every step of every stage.

Places are **reserved, not written**. The boss is at the Hollow of the old tree
(`Story-Greenwake.md` §4), and the four place names wait for the chapter pass. When they arrive they
are data rows: no code changes, no rebuild.

Later lands copy the shape, not the names: one land name, three or four plain places, boss in the last.

---

## 8. Player-facing strings that changed

| Where | Before | After |
|---|---|---|
| Title band | `Stage 5 BOSS` | `GREENWAKE` + diamond |
| Feed, wave line | `-- Wave 3: Bat x2 --` | `-- Bat x2 --` |
| Feed, clear line | `Stage 1 wave 1 cleared` | `Path clear` |
| Defeat card | `RUN OVER - stage 5` | `RUN OVER - Greenwake` |
| Run-ended toast | `Run ended - stage 5` | `Run ended at Greenwake` |
| Ascension line | `This run: stage 5` | `This run: Greenwake` |
| Leaderboard column | `Stage` | `Depth` (number kept, for ranking) |

Debug overlay and logs keep the engine words. That is developer ground.

---

## 9. Manual checks

1. Boot, play a leg. The band shows `GREENWAKE`, and the trail matches the stage plan.
2. Clear a step. One mark fills; nothing else pulses.
3. Reach the camp step. The flame brightens; the ability strip is replaced by the camp choices.
4. Reach the boss step. The name swaps to the boss name; the diamond lights.
5. Die. The run-ended card reads `Greenwake`, not a stage number.
6. Reduced motion on. No pulses; states still change.
7. A stage with 6 normal steps and one with 9 both draw a correct trail.
