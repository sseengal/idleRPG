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
| Stage band | **110 fixed** at the top of the page | name line 46 + `Space8` + trail 32 + `SectionGap` + `Space8` under the dots; the arena starts at the band's bottom |
| Trail | 20 px step marks, one 16 px gap everywhere | Centred under the name |
| Band | **no plate** (2026-11-25) | The name and the trail sit on the page. A grey bar here was a second frame that meant nothing |
| Boss step | The band still names the **land** (2026-11-26) | The old swap to the boss name was wrong: the party is still in Greenwake |


There is no separate "boss" banner, and no boss name in the band. The land name is one fact and it stays put;
the boss is marked by the last mark on the trail, not by renaming the place.

---

## 4. The trail

Marks appear in fight order, left to right:

| Mark | Shape | Size | Passed | Here | Ahead |
|---|---|---|---|---|---|
| Step | filled circle | 20 | amber, still | **amber, 1.25x, breathing** | muted dot (default) |
| Camp (placeholder) | hollow circle | 24 | amber, still | amber, breathing while its panel is open | muted |
| Boss (placeholder) | diamond (rotated square) | 30 | amber, still | amber, 1.25x, breathing | muted |

Rules:

1. **Exactly one mark breathes, and it is where you are** (2026-11-25). In a fight that is the current
   step. While the camp panel owns the screen it is that camp, and the current step goes still (it stays
   amber - the wave is finished). The old rule allowed two lit marks; that is what made a camp look like
   the next step had been reached.
2. A camp is amber only once it is **passed**. Being at its wave does not light it.
3. Shape carries the meaning, colour answers "have I done it": filled, hollow ring, diamond.
4. The trail is **not a button**. It never takes a tap.
5. The trail is built from the stage plan and `campEveryWaves`. Never from a fixed count of ten.

Placeholders: the camp is a hollow circle and the boss is a rotated square. Both are drawn from one
white dot sprite and plain rectangles, so **no art is needed** to run the band. Real 16 x 16 pixel
icons replace them later: the mark is a builder call, not an asset.

**The glow is its own recipe** (2026-11-25): `GlowStyle_Trail.asset` runs at full strength because the
halo art peaks at 30 % opacity. The shared dot style (peak 0.55) made the breath invisible - 0.30 x 0.55
is 17 % on screen. Only the current mark's halo is enabled, so a 13-mark trail costs 13 draws, not 26.

---

## 5. Motion

| Moment | Motion | Time |
|---|---|---|
| Step cleared | mark scales 0.7 -> 1.15 -> 1, flashes amber | 0.18 s |
| Camp reaches its wave | nothing - it stays muted until the camp is **passed** | - |
| Camp panel opens | that camp ring breathes; the current step stops breathing | 1.6 s loop |
| Camp used | ring settles to still amber | - |
| Boss arrives | diamond breathes; name swaps | 0.25 s + 1.6 s loop |
| Leg starts | marks draw left to right | 0.30 s |
| Idle | only the mark you are on breathes | 1.6 s loop |

Rules:

1. Alpha and scale only. Never animate a width: that forces a layout rebuild during a fight.
2. One pulse at a time.
3. The **reduced motion** setting turns every pulse into a still state. State changes still land, instantly.
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
