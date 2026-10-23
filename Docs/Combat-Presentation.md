# Combat Presentation — what exists, what is missing, what to change

Researched from code on 23 Oct 2026. **No code was changed for this document.**

> **Update (24 Oct 2026): S1–S5 are BUILT.** The ability path now runs in the same lane as a basic
> attack, with damage, heal and guard effects, floating numbers, log lines and enemy parity. See §12.
> The game's goal also changed: it is now a **story-led mini campaign with automatic combat**
> (`Game-Vision.md`). Nothing in this document is invalidated — the contract below is still the one to
> keep — but "stage" now means "story beat" in the wider plan.

This is the "why does the second attack look wrong" document. It records how a fight reaches the
screen today, where the ability path breaks the pattern, and the smallest set of changes that makes
every new ability behave like the basic attack without new UI code per ability.

---

## 1. In one paragraph (plain words)

The fight referee (`Encounter`, plain C#, no Unity) shouts results on a radio. A translator
(`CombatManager`) repeats each shout on the game's loudspeaker (`GameEvents`). The pictures — hero,
enemy, floating numbers, battle log — only ever listen to the loudspeaker. They never read the fight
state and never invent timing.

Two rules hold today:

1. The referee lets **one fighter act at a time**: announce → travel → hit → step back.
2. Basic attacks obey that rule. **Abilities do not.** A cast fires on any beat (even while someone
   else is mid-swing), has no target, no travel window, no hit, no number — and an enemy cast has no
   picture at all, only a log line.

Everything that "looks different" between attack 1 and the ability follows from rule 2. There is no
missing animation code; there is a second, weaker path.

---

## 2. Layer map

| Layer | Files | Job | Rule it must keep |
|---|---|---|---|
| Data | `Data/AbilityDef.cs`, `Data/AbilityKind.cs`, `Data/AbilityTarget.cs`, `Data/CharacterArtSet.cs`, `Content/Specs/abilities.json` | What an ability is and which art strip it uses | Data never talks to Unity |
| Sim | `Sim/Encounter.cs`, `Sim/Encounter.Events.cs`, `Sim/Combatant.cs`, `Sim/SimRules.cs` | Who acts, who is hit, how hard; raises events | Zero Unity types. Same answer stepped 20×/s or 12,000×/s |
| Bridge | `Combat/CombatManager.cs`, `Combat/CombatSimulator.cs`, `Core/GameEvents*.cs` | Copies sim events onto `GameEvents`, writes the log | No decisions here — copy + log only |
| Presentation | `UI/HeroUnitView.cs`, `UI/EnemyUnitView.cs`, `UI/CharacterAnimator.cs`, `UI/CharacterLunge.cs` | Turn an event into a picture | Never runs game logic, never owns timing |
| HUD | `UI/FloatingDamageTextPool.cs`, `UI/CombatLogUI*.cs`, `UI/AbilityPanelUI.cs`, `UI/HudController.cs` | Numbers, log, buttons | Listens to events only |

The seam that matters: **`GameEvents`**. Anything that raises a sim event reaches every picture for
free. Anything that bypasses it needs per-picture code — which is exactly the trap the ability path
fell into.

---

## 3. The action lane (as built)

`Encounter.Step()` (`Sim/Encounter.cs:192`) per beat, in this order:

1. `TickAllTimers` — every living unit's cooldown ticks, **and** every unit's pending cast is drained
   (`Encounter.cs:226-239`).
2. If a swing is in flight → advance it; damage lands at the end (`AdvanceActiveSwing`, `:284`).
3. Else if recovery is counting down → count it (`SwingRecoverySec`).
4. Else `AnnounceNext()` — picks the longest-waiting ready attacker, with a soft "other side next"
   preference (`:395`).

A swing is `PendingSwing` (`Encounter.Events.cs`): attacker, target, damage, crit flag, and
`Remaining` = the travel window. The window is **measured from the art**: `Combatant.SwingSeconds`
(frames up to the strike frame) falling back to `SimRules.SwingDurationSec`, clamped 0.1–2 s
(`SwingWindow`, `:370`). That is why the hit lands on the exact frame the sword is at full reach.

`ApplySwing` (`:309`) then does every consequence in one place: damage, threat, `Damaged` event,
`EndSwing()` (restarts that unit's attack clock), `Died`, `EnemyKilled`, `PartyWiped`.

```
announce ──────────► travel window ──────────► IMPACT ──────────► recovery
SwingStarted          (art-measured)            Damaged           SwingRecoverySec
  view: run in                                   view: plant       view: blink home
  view: play strip                               numbers, HP, log
```

**One lane = one picture on screen at a time.** That single sentence is why the basic attack reads
cleanly. Abilities are outside it.

---

## 4. The two paths, side by side

| | Basic attack | Ability cast (today) |
|---|---|---|
| Trigger | `AnnounceNext` when the lane is free | `DrainAbilityCast` on **any** beat (`:260`) |
| Waits for the lane | Yes | **No** — can start while a swing travels |
| Target | Picked by rule, travels in the event | **None** — event has side, index, name, clip, cooldown only |
| Travel window | Art-measured (`SwingSeconds`) | **None** — fires and is finished on the same beat |
| Clip | `PlayAttack()` (attack strip) | `PlayClip(artSlot)` in place (`HeroUnitView.cs:187-210`) |
| Lunge | `BeginRun` → `Impact` → `CompleteSwing` | **None** — view comment says "no target yet, so the hero stays in place" |
| Hit / numbers | `Damaged` → `FloatingDamageTextPool` subscribes | **Nothing** — no damage is computed at all |
| Log | Hit line | `"<hero> casts <ability>"` (`CombatManager.cs:264`) |
| Enemy side | `EnemyUnitView` handles `SwingStarted` (`:246`) | **No `AbilityCast` handler** — enemy casts are invisible |

---

## 5. Findings (with evidence)

1. **Cast fires mid-swing.** `TickAllTimers` drains casts before the `hasActiveSwing` check, so a
   press during someone's swing starts immediately (`Encounter.cs:199-224`). Two units "act" at once.
2. **A cast can land on the caster's own swing**, replacing the attack clip with the ability clip.
3. **That replacement drops the swing's completion callback.** `CharacterAnimator.Play()` (`:247`)
   sets `onDone` to the new callback (or idle), so `lunge.CompleteSwing()` from the interrupted
   attack never runs. The lunge's `maxHoldSeconds` cap (0.7 s, `CharacterLunge.cs:44`) stops it
   hanging forever — so the result is a lost beat, not a stuck unit.
4. **Floating numbers are already event-driven.** `FloatingDamageTextPool` subscribes to
   `GameEvents.EnemyDamaged` / `HeroDamaged` (`:53-63`) and spawns from those (`:85-113`). It is
   wired in the scene (`HudController` → boards, `:65,70`). Consequence: **any ability that raises a
   damage event gets numbers for free.** No pool work is needed for the damage case.
5. **Effect data is dead on arrival.** `Kind`, `Target`, `ShieldFactor`, `HealFraction`, ability
   `DamageMultiplier` are carried by `AbilityDef` and `abilities.json` and read by **no** code.
   `Combatant.SetAbility(id, displayName, clipSlot, cooldownSec)` (`Combatant.cs:147`, called from
   `CombatSimulator.cs:356`) throws the rest away at the sim boundary. The sim cannot even know
   whether a cast hits or guards.
6. **Cooldown starts at the drain tick**, and the panel reads it; there is no visible "queued" state.
7. **Docs drifted.** `Docs/Ability-Progress.md` says sequencing is done; it is not. `Ability-Plan.md`
   describes the intended design, not the built one.

---

## 6. Target design — one contract

**Law 1.** One actor, one action at a time. Nothing that takes time may start while the lane is busy.
**Law 2.** Every picture comes from an event. If the player can see it, the sim announced it.
**Law 3.** The view never measures time. The sim says when; the view only renders.
**Law 4.** A new ability adds data, not code. No new `if (abilityId == ...)` anywhere.

An ability cast then becomes "an action whose damage comes from the ability instead of the swing
formula":

```
lane free
  └─ announce: SwingStarted(index, target, clipSlot) + AbilityCast(name, cooldown)
       ├─ view: lunge to target (hostile targets only) + play clipSlot
       └─ travel: AbilityWindowSeconds (art-measured, same clamp as swings)
  └─ impact: apply the ability's effect
       ├─ Damage → ordinary damage event  ⇒ number, HP bar, kill, log  (already works)
       ├─ Heal   → heal event            ⇒ green number + log line
       └─ Shield → shield event          ⇒ blue number + log line
  └─ recovery: same beat as a swing
```

Presentation table — the only thing a view may know:

| Sim event | Hero view | Enemy view | Log | Numbers |
|---|---|---|---|---|
| `SwingStarted` (+ clip slot) | lunge + play clip | lunge + play clip | — | — |
| `Damaged` | flash + HP | flash + HP + death | hit line | yes (in/out colours) |
| `Healed` | green flash + HP | same | heal line | green |
| `ShieldApplied` | tint | same | guard line | blue |
| `AbilityCast` | cooldown clock only | same | cast line | — |

---

## 7. Changes needed to scale

Stages are ordered so each is playable on its own. S1 + S2 is the minimum that makes a cast "look
like attack 1".

### S1 — Put casts in the lane (sim only)

- `DrainAbilityCast` stops raising; it parks **one** pending cast per encounter.
- When the lane is free, the pending cast is *announced* like a swing: resolve a target from the
  ability's target rule, set `Remaining = AbilityWindow(caster)`, raise `AbilityCast` **and**
  `SwingStarted` (with the ability's clip slot).
- Drop a pending cast if the caster dies first. Set `lastActorSide` as a swing would.
- Files: `Sim/Encounter.cs`, `Sim/Encounter.Events.cs`, `Sim/Combatant.cs`.
- Accept: press CAST during a swing → the cast waits, then plays; never two actions at once.

### S2 — One view entry point

- `SwingStartedEvent` carries `ClipSlot` (empty = ordinary attack). Flow it through
  `GameEvents.RaiseSwingStarted` and both unit views.
- `HeroUnitView.OnSwingStarted` plays `clipSlot` when set, else `PlayAttack`, and keeps the single
  completion → `lunge.CompleteSwing()` wiring. Delete the in-place `OnAbilityCast` playback for
  party heroes (keep the event for the cooldown clock).
- Lunge only for hostile targets (`FrontEnemy` / `FrontHero`); self/party abilities stay planted.
- Files: `Sim/Encounter.Events.cs`, `Core/GameEvents*.cs`, `Combat/CombatManager.cs`,
  `UI/HeroUnitView.cs`.
- Accept: hero runs in, plays the ability strip, plants on the impact frame, blinks home; next basic
  attack is not delayed.

### S3 — Effects: damage (the "2nd attack" case)

- Replace `SetAbility(4 args)` with a small `SimAbility` struct carrying kind, target, cooldown,
  window and the effect numbers. One mapping function `AbilityDef → SimAbility` in `CombatSimulator`
  is the single place new abilities become real.
- `Damage` kind: `attack × abilityMultiplier` through the existing crit/defence path, applied in the
  same impact step as a swing (`ApplySwing`), so numbers / HP / kill / log are free.
- Files: `Sim/Combatant.cs`, `Sim/Encounter.cs`, `Combat/CombatSimulator.cs`.
- Accept: ability damage shows a number on the correct enemy and a log line, identical in style to
  attack 1.

### S4 — Effects: heal and shield

- Two new sim events (`Healed`, `ShieldApplied`), two `GameEvents`, two pool styles, two log lines.
- Shield must actually cut incoming damage for N seconds — a damage-taken modifier on `Combatant`.
- Files: Sim + `Core/GameEvents*` + `UI/FloatingDamageTextPool.cs` + `UI/CombatLogUI.Events.cs`.

### S5 — Enemy side parity

- `EnemyUnitView` subscribes to `AbilityCast` (and honours `ClipSlot`), so enemy casts animate.
- Enemy casts start on cooldown from wave start (today they never fire).
- Files: `UI/EnemyUnitView.cs`, `Sim/Encounter.cs`.

### S6 — Guardrails

- This document becomes the contract; `Ability-Plan.md` / `Ability-Progress.md` get corrected, not
  duplicated.
- Optional (ask first, per team rules): one EditMode test for the sequencing invariant — never two
  actions in flight, impact order equals announce order.

---

## 8. Checklist: adding a new ability later

Must be true after S3:

1. Add one row to `Content/Specs/abilities.json` (id, displayName, kind, artSlot, cooldownSec,
   effect numbers, target, unlockStage).
2. Regenerate / edit the matching `AbilityDef` asset.
3. Point the hero (or enemy) at that ability id.
4. Nothing else. No view code, no `switch`, no new event.

If step 4 is ever false, the pipeline has drifted — that is the tripwire this document exists for.

---

## 9. Trade-offs and open decisions

| Decision | Option A | Option B | Note |
|---|---|---|---|
| Press while the lane is busy | Buffer **one** cast, fire when free | Refuse (button feels dead) | A needs a visible "queued" state, or players press twice |
| Cooldown start | On the drain tick (today) | On the actual announce | Max wait is one swing (~0.4–1 s); pick one and write it down |
| Self / party abilities | Stay in place, no lunge | Lunge at nothing | In place is correct; a guard that charges is a lie |
| Clip slot in the event | Explicit field on `SwingStarted` | Reuse the "`AbilityCast` then `SwingStarted`" ordering | Explicit costs a signature change in 2–3 subscribers; ordering is free but implicit |
| Enemy abilities | Also gated by the lane | Separate enemy clock | Same lane keeps the one-picture rule intact |

Known downside of the whole plan: gating casts to the lane **delays** the player's press by up to one
swing. That is the price of correct sequencing. It must be shown (queued state), not hidden.

Second downside: a long ability animation lengthens the window for *every* player, since the lane is
shared. `AbilityWindow` is clamped like swings (0.1–2 s) so one bad clip cannot stall the fight, but a
1.8 s ability still reads slower than a 0.35 s sword. Measure it before shipping.

---

## 10. Manual checks in Play mode

1. Press CAST mid-swing → no overlap; only one unit moves at a time.
2. Cast lands on the frame the strip reaches full reach (compare with attack 1's beat).
3. Hero returns home; the following basic attack is not delayed.
4. Numbers appear on the enemy that was actually hit.
5. Log order: cast line, then hit line.
6. ABILITIES tab: label and cooldown clock stay correct through the queued wait.
7. Swap waves mid-cast → nobody is left away from home.

---

## 11. File map (line anchors used above)

> Line numbers moved on 24 Oct 2026 when S1–S5 were built. Use the symbol names in this table, not the
> numbers; §12 lists what changed file by file.

| File | What to read |
|---|---|
| `Assets/IdleRPG/Scripts/Sim/Encounter.cs` | `Step` 192, `TickAllTimers` 226, `RequestAbility` 246, `DrainAbilityCast` 260, `AdvanceActiveSwing` 284, `ApplySwing` 309, `SwingWindow` 370, `AnnounceNext` 395 |
| `Assets/IdleRPG/Scripts/Sim/Encounter.Events.cs` | `PendingSwing`, `DamageEvent`, `SwingStartedEvent`, `AbilityCastEvent` |
| `Assets/IdleRPG/Scripts/Sim/Combatant.cs` | `SetAbility` 147, `ConsumeAbilityCast`, `EndSwing` |
| `Assets/IdleRPG/Scripts/Combat/CombatManager.cs` | `OnSimAbilityCast` 262, `OnSimSwingStarted`, `LogCombat` |
| `Assets/IdleRPG/Scripts/Combat/CombatSimulator.cs` | `SetAbility` call 356 — the mapping point |
| `Assets/IdleRPG/Scripts/UI/HeroUnitView.cs` | `OnAbilityCast` 187, `OnSwingStarted` 213, `StartSwing` 238 |
| `Assets/IdleRPG/Scripts/UI/EnemyUnitView.cs` | `OnSwingStarted` 246 (no `AbilityCast` handler) |
| `Assets/IdleRPG/Scripts/UI/CharacterAnimator.cs` | `PlayAttack` 166, `PlayClip` 192, `Play` 220 (`onDone` overwrite at 247) |
| `Assets/IdleRPG/Scripts/UI/CharacterLunge.cs` | `BeginRun`, `Impact`, `CompleteSwing`, `ResetToHome`, `maxHoldSeconds` 44 |
| `Assets/IdleRPG/Scripts/UI/FloatingDamageTextPool.cs` | subscribe 53, `Spawn` 115 |

## 12. Change log — what was built (24 Oct 2026)

### Sim

- **New `Sim/SimAbility.cs`** — plain-value card (`SimAbilityKind`, `SimAbilityTarget`, `SimAbility`):
  id, name, clip slot, cooldown, kind, target, shield factor/seconds, heal fraction, damage multiplier,
  and the art-measured `WindowSeconds`.
- **`Combatant`** now holds a `SimAbility` (not four loose strings). `SetAbility(SimAbility)` replaces
  the old four-argument version. The cooldown starts at the **announce**, so a queued press is not
  charged for waiting. `HasCastRequest` is the queued flag the button reads.
- **Guard is real:** `ApplyGuard(factor, seconds)` + `DamageTakenMultiplier`; `TakeDamage` multiplies
  the whole hit while the guard is up. `RestoreFullHealth` clears it.
- **`Encounter`:** casts no longer drain on any tick. A press parks a request; `AnnounceNext` first
  tries `AnnounceAbility()`, which:
  1. takes one queued caster (party first, then enemies),
  2. resolves targets from the ability's own rule (`Self`, `Party`, `LowestAlly`, `FrontEnemy`, `FrontHero`),
  3. rolls crit/damage through the same formula a swing uses (Damage kind only),
  4. sets the travel window from the art strip (`AbilityWindow`, clamped 0.1–2 s),
  5. raises `AbilityCast` **then** `SwingStarted` with an explicit `ClipSlot`,
  6. starts the caster's clock and marks the alternation side.
- `ApplySwing` now branches: a basic attack, or an ability effect (`ApplyAbilityImpact`).
  Death handling is one path (`ResolveDeath`), so an ability kill, its reward, and a party wipe are
  identical to a swing kill. New events: `Healed`, `ShieldApplied`.
- `ResetCombatFlow` cancels every queued request, so a wave change cannot fire a stale press.

### Bridge

- `CombatSimulator.ToSimAbility` is **the single mapping point** (`AbilityDef` → `SimAbility`, with the
  clip window read from that unit's art set).
- Enemies are armed from `EnemyData.Abilities` at the start of every wave (`ArmEnemyAbilities`), so
  enemy casts now exist and animate.
- `SwingStarted` carries the clip slot through `CombatSimulator` → `GameEvents`.
- `CombatManager` logs casts with their target, and forwards heal/guard events.

### Presentation

- `HeroUnitView` / `EnemyUnitView`: **one** entry point, `OnSwingStarted(..., clipSlot)`. A named slot
  plays that strip (`PlayClip`), an empty slot plays `PlayAttack`. Both end in one callback
  (`OnSwingClipFinished` → `lunge.CompleteSwing`), so a replaced clip can no longer drop the lunge.
- The old in-place `HeroUnitView.OnAbilityCast` playback is deleted.
- Lunge only for a hostile target: a hero ability aimed at the party, or an enemy guard, stays planted.
- Guard = short blue flash; heal = bar refresh. Both read from the new events.
- `FloatingDamageTextPool`: green number on heal, blue `GUARD` word on a guard.
- `CombatLogUI`: cast line, heal line, guard line.
- `AbilityPanelUI`: amber **QUEUED** state while a press waits for the lane (the button is disabled
  until the cast is announced, so a double press cannot queue twice).

### Known limits (not regressions)

- `Attack02` / `Attack03` strips have no measured strike frame, so an ability that names them lands on
  the clip's **last** frame. Measuring those frames is art-importer work with a long lead time.
- HUD tuning is not done: the guard flash and the `GUARD` word are functional, not final.
- Numbers are untouched (see `Game-Vision.md` §6): the ordering and the effects are correct; balance is
  a later, separate pass.

### Manual checks that now cover the whole path

Use §10 as written, plus:

8. A shield ability (Knight Shield Wall) → blue flash, `GUARD` word, and the next hit is visibly
   smaller.
9. A heal ability (Priest) → green number and the lowest hero's bar rises.
10. An enemy with a Beam ability → it plays its strip, lunges at the hero, and a number appears on that
    hero.

### Related docs

- `Docs/Game-Vision.md` — the story-led MVP this work now serves.

- `Docs/Ability-Plan.md` — intended ability design (not built).
- `Docs/Ability-Progress.md` — status list (stale on sequencing).
- `Docs/Ability-Map.md` — which hero/enemy owns which ability id.
- `Docs/Architecture.md`, `Docs/Sim-Core.md` — layer rules this document builds on.
