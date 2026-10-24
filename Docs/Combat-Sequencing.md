# Combat sequencing and animations — one record

**This is the single page for "how a blow reaches the screen".** It joins the fight order, the animation
choice, the timings and the things that are still open. It replaces the sequencing notes that were
scattered through `Combat-Presentation.md` (§12), `Ability-Progress.md` and `Ability-Plan.md`.

Built and verified 24 Oct 2026. Code is the truth; if this page and the code disagree, the code wins and
this page gets fixed the same day.

---

## 1. Plain words (read this first)

A fight is a queue, not a crowd. **One fighter acts at a time.** That one sentence is why a fight reads
cleanly.

A turn is four beats:

1. **Say it** — the fight referee (`Encounter`, plain C#, no Unity) says "this unit attacks that unit".
2. **Travel** — a short pause. The pause length comes from the *drawing*, not from a guess.
3. **Land it** — the effect happens (damage, heal, guard) on the exact beat the picture reaches full reach.
4. **Step back** — the unit goes home and starts its own clock again.

The referee talks on a radio (`Encounter` events). A translator (`CombatManager`, `CombatSimulator`)
repeats each shout on the loudspeaker (`GameEvents`). The pictures — hero, enemy, numbers, log, panel —
only listen to the loudspeaker. **No picture reads the fight state and no picture measures time.**

Words used below:

- **lane** — the queue that allows one action at a time.
- **strip** — one animation row of a character (idle, attack 1, attack 2, ability, hurt, death).
- **slot** — the name of a strip (`Attack01`, `Attack02`, `Ability`).
- **window** — the time from "say it" to "land it".

---

## 2. The five layers and the one rule each keeps

| Layer | Files | Job | Rule it must keep |
|---|---|---|---|
| Data | `Data/AbilityDef.cs`, `Data/CharacterArtSet.cs`, `Content/Specs/abilities.json`, `Content/Specs/enemies.json` | What a move is; which strip it uses | Data never talks to Unity |
| Sim | `Sim/Encounter.cs`, `Encounter.Events.cs`, `Combatant.cs`, `SimAbility.cs`, `SimRules.cs` | Who acts, who is hit, how hard, when | Zero Unity types; same answer at 20×/s or 12,000×/s |
| Bridge | `Combat/CombatManager.cs`, `Combat/CombatSimulator.cs`, `Core/GameEvents*.cs` | Copy sim events to `GameEvents`, write the log | No decisions — copy and log only |
| Presentation | `UI/HeroUnitView.cs`, `UI/EnemyUnitView.cs`, `UI/CharacterAnimator.cs`, `UI/CharacterLunge.cs` | Turn an event into a picture | Never runs game logic, never owns timing |
| HUD | `UI/FloatingDamageTextPool.cs`, `UI/CombatLogUI*.cs`, `UI/AbilityPanelUI.cs` | Numbers, log, buttons | Listens to events only |

The seam that matters is `GameEvents`. Anything raised there reaches every picture for free. Anything
that bypasses it needs its own code in every picture — that is the trap the ability path fell into
before 24 Oct 2026.

---

## 3. The lane: order of work on every beat

`Encounter.Step(deltaTime)`, in this order:

1. **Tick clocks.** Every living unit's attack clock and ability clock tick down.
2. **Enemy intent.** Each enemy whose ability is ready rolls its patience (`TickAbilityIntent`). A pass
   sets a cast request; a miss costs nothing — it keeps swinging and rolls again shortly.
3. **Advance the action in flight.** The one in-flight swing loses `Remaining`; at zero it lands
   (`ApplySwing`).
4. **Recovery.** If nobody is swinging and recovery is counting, count it (`SwingRecoverySec`).
5. **Announce the next action** (`AnnounceNext`):
   - a queued cast first (`AnnounceAbility`) — the player asked for it, so it has priority;
   - otherwise the longest-waiting ready attacker, with a soft "other side next" preference.

A request is only a **flag**. It becomes an action in step 5. That is why a press during another unit's
swing can never overlap it.

```
lane free
  └─ ANNOUNCE   SwingStarted(slot, target) + AbilityCast(name, cooldown)
       ├─ view: run in (hostile target only) + play the strip
       └─ travel: window seconds (art-measured, clamped 0.1–2 s)
  └─ IMPACT
       ├─ Damage → ordinary damage event  ⇒ number, HP bar, kill, log
       ├─ Heal   → heal event             ⇒ green number, log
       └─ Guard  → guard event            ⇒ blue word, blue flash, real damage cut
  └─ RECOVERY: blink home, that unit's clock restarts
```

**One lane = one picture moving on screen.** Two units never act at the same time.

---

## 4. Step 1 — data: what a move is

A move is one row in `Content/Specs/abilities.json`:

| Field | Meaning |
|---|---|
| `id` / `displayName` | Key and the words the player reads |
| `kind` | `Damage`, `Heal` or `Guard` |
| `target` | `Self`, `Party`, `LowestAlly`, `FrontEnemy`, `FrontHero` |
| `artSlot` | Which strip to play: `Ability`, `Attack02`, … |
| `cooldownSec` | How long before it can be used again |
| `damageMultiplier` | Damage as a multiple of a plain hit |
| `healFraction` | Heal as a fraction of the target's maximum health |
| `shieldFactor` / `shieldSeconds` | Damage taken multiplier while the guard holds, and for how long |
| `impactVfx` | Optional id under `Resources/Vfx/` (a sliced sheet) played **on the target** at impact — `ice_shatter`, `heavy_smash` |
| `unlockStage` | Stage gate (heroes only; enemies ignore it) |

The row becomes an asset (`Data/Abilities/*.asset`), and the hero or enemy points at that asset.

**All hero abilities are open for the MVP.** One switch does it:
`GameManager.unlockAllAbilitiesForMvp` → `AbilityService.OpenAllForMvp`. When the story beats exist, set
the switch off and unlock each ability at its own beat. Nothing else changes, because the fight reads
whichever ability is worn.

---

## 5. Step 2 — the drawing decides when the blow lands

This is the part that makes an attack look right, and the part that broke the Slime.

- Every character owns a `CharacterArtSet` (`Data/Characters/*_Art.asset`) with one strip per slot and a
  **strike frame** per attack strip (`StrikeFrame`, `AbilityStrikeFrame`).
- `CharacterArtSet.FramesFor(slot)` returns the frames for a slot. **A missing slot returns nothing.**
- `CharacterArtSet.StrikeFrameFor(slot)` returns the measured strike frame, or **the last frame** when the
  strip has no measurement.
- `SecondsFor(slot)` turns that frame into time: frames ÷ frame rate.

`CombatSimulator.ToSimAbility` is the only place a data ability becomes a fight ability. It carries the
slot and reads `art.SecondsFor(slot)` into `SimAbility.WindowSeconds`. The referee then uses:

- `SwingWindow` — the attacker's `SwingSeconds`, else `SimRules.SwingDurationSec`;
- `AbilityWindow` — the ability's `WindowSeconds`, else the caster's `SwingWindow`;

both clamped to **0.1–2 s** so one broken strip can never stall the fight.

Consequences worth knowing:

1. **A missing slot does not fall back to another strip.** It plays the plain attack (`Attack01`) because
   the view is handed an empty frame list. This looks like "the ability works but animates wrong".
2. **An unmeasured strip lands on its last frame.** `Attack02` / `Attack03` have no measured strike frame,
   so an ability that names them lands on the final frame. A 12-frame strip at 12 fps therefore lands
   about **1.0 s** after the call-out.
3. **Measure a strip once, and every move that names it is fixed.** Measuring is art-importer work with a
   long lead time; it is not view code.

---

## 6. Step 3 — announce

`AnnounceNext` → `AnnounceAbility`:

1. Take one queued caster (party first, then enemies).
2. Resolve targets from the ability's own `target` rule.
3. Roll crit and damage with the **same** formula a swing uses (damage kind only).
4. Set `Remaining = AbilityWindow(caster)`.
5. Raise `AbilityCast`, **then** `SwingStarted` with the explicit clip slot.
6. Start the caster's ability clock **now**, at the announce — time spent queued is not charged.
7. Mark the alternation side so the other side gets the next opening.

If the caster dies while queued, the request is dropped. If the target list is empty, the press is dropped
instead of sticking forever. A wave change clears every queued request (`ResetCombatFlow`), so no stale
press can fire into the new wave.

---

## 7. Step 4 — impact

`ApplySwing` has one branch for a plain attack and one for an ability (`ApplyAbilityImpact`):

| Kind | What happens |
|---|---|
| Damage | `TakeDamage` → `Damaged` event → number, HP bar, log, threat, then `ResolveDeath` |
| Heal | `Heal(maxHealth × healFraction)` → `Healed` event → green number, log |
| Guard | Incoming damage is multiplied by `DamageTakenMultiplier` while the guard runs → `ShieldApplied` event |

Death is one path (`ResolveDeath`) for a swing, an ability, or a party wipe, so gold, kill credit and the
"party wiped" flow cannot diverge.

**On-target impact effect.** A damage ability may name an `impactVfx`. `DamageEvent.VfxId` carries it to the HUD, and
`FloatingDamageTextPool` plays the sliced sheet at the hit enemy, sized to 1.4x the target anchor, above the unit and
under the numbers. Empty id = nothing plays. Missing sheet = nothing plays (never an error).

| Move | Effect id | Sheet |
|---|---|---|
| Overhead Slash | `heavy_smash` | `Priest_Attack_effect.png` (5 frames) |
| Shatter | `ice_shatter` | `Wizard_Attack01_Effect.png` (10 frames) |

Effect sheets live in `Assets/IdleRPG/Resources/Vfx/` and are cut by `Tools > Idle RPG > Art > Slice VFX Sheets`
(a 100 px grid; the tool reads the PNG header so Unity's power-of-two rescale cannot move the cuts).

---

## 8. Step 5 — the pictures

### Unit views: one entry point

`HeroUnitView.OnSwingStarted` and `EnemyUnitView.OnSwingStarted` take
`(attackerIndex, attackerSide, targetIndex, targetSide, clipSlot)`. One handler covers an attack and an
ability:

- a named slot plays that strip (`PlayClip`), an empty slot plays `PlayAttack`;
- both finish through the same callback (`OnSwingClipFinished` → `lunge.CompleteSwing`), so a replaced
  clip can no longer drop the lunge;
- **lunge only at a hostile target.** A party heal, a party guard or an enemy self-guard stays planted: a
  guard that charges at nothing is a lie.

### The numbers

`FloatingDamageTextPool` listens to the events, not to the fight:

| Event | Show |
|---|---|
| plain hit | red number (`FF5959`), normal size |
| crit | gold number, bigger |
| enemy ability hit | orange number (`FF8C26`), **1.5× size** |
| heal | green number |
| guard | blue word `GUARD` |

The enemy case needs a small trick: the damage lands a moment after the call-out, and the damage event
carries no "ability" flag. So `AbilityCast` marks that enemy slot for `abilityHitWindowSec` (**2.5 s**),
and any damage that lands inside the window is drawn as a heavy ability hit. The same handler also spawns
the **ability name in orange over the caster**, so the player sees where the move came from.

### The log

- Hero: `<hero> casts <ability>` — written at the announce, so it always reads before the hit line.
- Enemy: `<enemy> unleashes <move>!` — orange, its own verb, its own colour.
- Then the hit, heal or guard line.

### The panel

`AbilityPanelUI` shows `READY`, `QUEUED` (amber, while the press waits for the lane), `<n>s` (cooling) or
`LOCKED`. The button is disabled while queued or cooling, so a double press cannot queue twice.

---

## 9. Enemy side: unpredictable, heavy, loud

Three things turn an enemy move from a log line into an event the player feels.

**1. Timing is random, not a metronome ("Wild Cards").**

- At wave start an armed enemy waits a random **2–6 s** before its first roll
  (`EnemyFirstCastMinSec` / `Max` in `CombatSimulator`), so a wave never opens with a move.
- After that wait it rolls **once** against a **35 %** chance (`Encounter.EnemyCastChance`).
- A pass means "cast now". A miss means it swings instead and waits a random **1–3 s** before the next roll
  (`EnemyCastRetryMinSec` / `Max`).
- **One roll per wait, never one per frame.** A per-frame roll would fire almost at once and the 35 % would
  stop meaning anything.
- The dice come from the fight's own generator, so a replay and an offline run land on the same casts.

**2. A move hits harder than a swing ("Heavier Hands").** Damage moves carry `damageMultiplier` 1.6–2.0
(most 1.8–1.9) of a plain hit, up from 1.3–1.4. It reads as a real hit and not as a wall.

**3. The move is announced loudly ("Herald").** Orange `unleashes` log line, orange ability name over the
caster, 1.5× orange damage number, plus the guard and heal colours for the other kinds.

**Enemies are armed from data on every wave** (`ArmEnemyAbilities` reads `EnemyData.Abilities`), so an
enemy move is a content decision and never a save-file or unlock decision. Each enemy uses its **first**
ability.

---

## 10. Case study — the Slime that never played its move

This is the most useful bug on this page, because it is the shape of every "the ability animates wrong"
report.

**Symptom.** The Slime cast on time, the log line appeared, damage landed — but the picture played the
plain attack.

**Cause.** The Slime's art set has **no `Ability` strip** (idle 6 frames, `Attack01` 6, `Attack02` 12,
`Ability` 0). `enemy_spike` named `artSlot: Ability`, so `FramesFor("Ability")` returned nothing and the
view fell back to `Attack01`. The 12-frame `Attack02` sat unused.

**Fix (data only, no code).**

1. Added one row, `enemy_spike_slime`, to `Content/Specs/abilities.json`: `artSlot: Attack02`,
   `kind: Damage`, `damageMultiplier: 1.8`, `cooldownSec: 10`, `target: FrontHero`.
2. Pointed `Enemy_Slime` at that row (`enemies.json` → `abilities: ["enemy_spike_slime"]`).
3. Regenerated: `Data/Abilities/EnemySpikeSlime.asset` and `Data/Enemies/Enemy_Slime.asset`.
4. **Left Lava Slime and Lava Behemoth on `enemy_spike`** — they own a real `Ability` strip.

**Verified in Play, by reading the events** (test-only listeners; they vanish when Play stops):

```
PROBE cast Enemy idx=0 id=enemy_spike_slime slot=Attack02
PROBE enemy swing idx=0 slot='Attack02'
```

**Side effect, accepted.** `Attack02` has no measured strike frame, so the blow lands on the last frame:
about **1.0 s** after the call-out. That is inside the 2.5 s orange window, so the number still reads as
the move's damage. It is the same behaviour Archer Volley already had. Shorten it by measuring the strike
frame, not by shortening the strip.

**Lesson.** When a move "animates as the wrong thing", check the strip first:
`FramesFor(slot).Length == 0` means the slot does not exist for that character.

---

## 11. Animation audit (24 Oct 2026)

Enumerated from the art sets and the specs, not typed by hand.

- **73 enemies**, all animated: idle, attack, hurt and death exist for every one.
- **16 enemies own a move.** All 16 now play a strip they actually own:

| Enemy | Move | Strip used |
|---|---|---|
| Slime | Spike | `Attack02` |
| Skeleton, Armored Orc, Orc Rider, Bone Captain, Orc Raider, Black Knight Champion | Block | `Ability` |
| Lava Slime, Lava Behemoth | Spike | `Ability` |
| Ghostfire, Eyeball Monster, Ghostfire Wraith, Eyeball Watcher, Black Knight Sovereign | Beam | `Ability` |
| Archer, Archer Captain | Volley | `Ability` |

- **57 enemies own no move.** Nine of those already own a special-move strip that no move uses:

| Enemy | Strip that sits unused | Natural move |
|---|---|---|
| Necromancer | `Summon` | Summon |
| Priest | `Heal` | Heal |
| Priest Zealot | `Heal` | Heal |
| Greatsword Skeleton | `Summon` | Summon |
| Greatsword Warden | `Summon` | Summon |
| Skeleton Archer | `Summon` | Summon |
| Skeleton Marksman | `Summon` | Summon |
| Armored Skeleton | `Summon` | Summon |
| Grand Templar | `Block` | Guard |

- **Bosses:** 40 in total, **32 have no move**. That is the biggest remaining animation gap, and the one
  the player notices most because a boss is on screen for a long time.

---

## 12. Every timing in one table

| Value | Where | Notes |
|---|---|---|
| 0.1–2 s | `SwingWindow`, `AbilityWindow` | Hard clamp, so a broken strip cannot stall the fight |
| `SwingRecoverySec` | `SimRules` (1.0 s default) | Pause after an impact |
| 2–6 s | `EnemyFirstCastMinSec` / `Max` | Wait before the first enemy roll in a wave |
| 35 % | `Encounter.EnemyCastChance` | One roll per wait |
| 1–3 s | `EnemyCastRetryMinSec` / `Max` | Wait after a miss |
| 2.5 s | `FloatingDamageTextPool.abilityHitWindowSec` | How long after a call-out damage still counts as the move's |
| 1.5× | `FloatingDamageTextView.abilityScale` | Size of an enemy ability number |
| 1.6–2.0× | `abilities.json` `damageMultiplier` | Enemy move vs a plain hit (most 1.8–1.9) |
| ~1.0 s | 12 frames ÷ 12 fps | Landing point of an unmeasured `Attack02` |

---

## 13. Adding a move later — the checklist

Must stay true, or the pipeline has drifted:

1. Add **one row** to `Content/Specs/abilities.json`.
2. Regenerate / edit the matching `Data/Abilities/*.asset`.
3. Point the hero or enemy at that id (`heroes.json` / `enemies.json`), regenerate its asset.
4. Confirm the named `artSlot` **exists** on that character's art set.
5. Nothing else. **No view code, no `switch`, no new event, no new UI.**

If step 5 is ever false, stop and fix the pipeline instead of adding the special case.

---

## 14. Manual checks in Play mode

Run these on any change that touches combat sequencing or animation. Game view must be focused — the
project sets `runInBackground = false`, so an unfocused Editor ticks at about 0.1 fps and everything looks
frozen.

1. Press CAST during another unit's swing → the press waits (`QUEUED`), then plays. Only one unit moves.
2. The blow lands when the strip reaches full reach, on the same beat as attack 1.
3. The hero goes home; the next basic attack is not delayed.
4. The number appears over the unit that was actually hit.
5. Log order: cast line, then hit line.
6. The ABILITIES row keeps the right name and clock through the queued wait.
7. Swap waves mid-cast → nobody is left away from home.
8. Guard move (Knight Shield Wall, Black Knight Block) → blue flash, `GUARD` word, and the next hit is
   visibly smaller.
9. Heal move (Priest) → green number, and the lowest hero's bar rises.
10. Enemy move (Slime Spike, Archer Volley) → its own strip plays, the enemy lunges at a hero, the orange
    name shows over it, and a bigger orange number appears on that hero.
11. Fight one wave of each enemy that owns a move, and confirm the picture matches the move name.

---

## 15. How to prove it, without guessing

- **Read the events, not the picture.** Temporary listeners that print `PROBE cast …` and
  `PROBE … swing slot=…` show exactly which slot the fight asked for and which slot the view played. If
  those two agree, the animation path is correct and any remaining oddity is art, not code.
- **Stop Play afterwards.** The probe listeners and their lines go away with the play session; nothing to
  clean up.
- **Do not use a per-swing timer as proof.** It resets every swing and will lie about whether the game is
  running.
- **Regression surfaces on every combat change:** golden numbers, content validator, save drift, and the
  battle log (attribution, one line per wave, no flooding).

---

## 16. Still open (not regressions)

| Item | Why it is not done | Size |
|---|---|---|
| Guard lands ~0.1 s after the call-out (Knight `AbilityStrikeFrame = 0`), before the Block strip reads as a shield | Timing, not balance: pick the contact frame | Small |
| Measure `Attack02` / `Attack03` strike frames | The whole 6-9 frame strip plays, so Overhead Slash and Shatter land on the last frame (0.5 s / 0.75 s) | Medium |
| Attach the nine free Summon / Heal / Block strips in §11 | Content decision, not code — each is one spec row plus one asset | Small |
| Boss moves (32 of 40 bosses have none) | Biggest visible gap; needs strips that exist plus spec rows | Medium |
| Measure `Attack02` / `Attack03` strike frames | Art-importer work, long lead time; until then those strips land on the last frame | Medium |
| HUD feel pass on the guard flash and the `GUARD` word | Functional, not final | Small |
| Ability assignments are session state, not saved | Needs persistence wiring | Small |
| Hero abilities open for everyone (MVP switch) | Deliberate stop-gap until story beats exist | Small |

Balance is a separate pass: the ordering and the effects are correct, the numbers are not tuned. **The golden
numbers stay frozen until story sequencing and the MVP are finished** — one dated re-record at the end, not one per
step (`Content.md` §10, "When the baseline is re-recorded").

---

## 17. File map

| File | What to read |
|---|---|
| `Sim/Encounter.cs` | `Step`, `TickAllTimers`, `RequestAbility`, `AdvanceActiveSwing`, `ApplySwing`, `ApplyAbilityImpact`, `SwingWindow`, `AbilityWindow`, `AnnounceNext`, `AnnounceAbility`, `PickPendingCaster`, `ResolveAbilityTargets` |
| `Sim/Encounter.Events.cs` | `PendingSwing`, `DamageEvent`, `SwingStartedEvent`, `AbilityCastEvent`, `HealEvent`, `ShieldEvent` |
| `Sim/Combatant.cs` | `SetAbility`, `RequestAbility`, `TickAbilityIntent`, `HoldAbility`, `ApplyGuard`, `DamageTakenMultiplier`, `EndSwing` |
| `Sim/SimAbility.cs` | The plain-value card a move becomes inside the fight |
| `Sim/SimRules.cs` | `SwingDurationSec`, `SwingRecoverySec` and their clamps |
| `Combat/CombatSimulator.cs` | `ToSimAbility` (the single mapping point), `ArmEnemyAbilities`, `EnemyFirstCastMinSec` |
| `Combat/CombatManager.cs` | Event copying and the log lines |
| `Core/GameEvents*.cs` | The loudspeaker: `SwingStarted`, `AbilityCast`, `Damaged`, `Healed`, `ShieldApplied` |
| `Data/CharacterArtSet.cs` | `FramesFor`, `StrikeFrameFor`, `SecondsFor` — the art-to-time bridge |
| `UI/HeroUnitView.cs` / `UI/EnemyUnitView.cs` | `OnSwingStarted`, `StartSwing`, guard and heal reactions |
| `UI/CharacterAnimator.cs` | `PlayAttack`, `PlayClip`, `Play` (one `onDone` at a time) |
| `UI/CharacterLunge.cs` | `BeginRun`, `Impact`, `CompleteSwing`, `maxHoldSeconds` |
| `UI/FloatingDamageTextPool.cs` | `OnAbilityCast` mark, `IsAbilityHit`, the 2.5 s window |
| `UI/FloatingDamageTextView.cs` | Colours and sizes per style |
| `UI/CombatLogUI.Events.cs` | `casts` / `unleashes` / hit / heal / guard lines |
| `UI/AbilityPanelUI.cs` | `READY` / `QUEUED` / clock / `LOCKED` |

---

## 18. Related docs

- `Docs/Game-Vision.md` — the story-led campaign this serves.
- `Docs/Combat-Presentation.md` — the 23 Oct research and the 24 Oct build log (record page).
- `Docs/Ability-Progress.md` — ability status, measurements and open items.
- `Docs/Ability-Plan.md` — the locked ability decisions; built behaviour is this page.
- `Docs/Ability-Map.md` — which hero and enemy owns which ability id.
- `Docs/Character-Catalog.md` — every character's strips and frame counts.
- `Docs/Sim-Core.md`, `Docs/Architecture.md` — layer rules this page builds on.
