# Ability Plan — the animation-backed specials, end to end

Companion to `Ability-Map.md` (which maps the pack's named strips to abilities). This file is the *build* plan:
decisions, data, sim, UI, unlock, order. Everything here is agreed as of 2026-10-12; change it here, not in the code.

> **Status: DECISIONS PAGE.** The decisions below still hold, but most of this plan is now built.
> For what the game does today — cast in the lane, which strip plays, impact timing, enemy timing — read
> **`Combat-Sequencing.md`**. For "what works / what is open" read **`Ability-Progress.md`**.

## 1. The locked decisions

| # | Decision | Choice |
|---|---|---|
| 1 | Cast model | **Revised 24 Oct 2026: a cast is an ACTION IN THE LANE**, not an instant off-turn burst. Any cast --> the request waits (button shows QUEUED) --> it is announced like a swing --> the effect lands on its own art-measured window. **`Combat-Sequencing.md` is the built behaviour**; this row is kept so the change is visible. |
| 2 | Combat cost | **Cooldown only.** No mana/energy. Unlock is the "limited" part; casts cost time. |
| 3 | Targeting | **Automatic.** One tap, no aiming: Shield self, Heal lowest-health ally, damage the front enemy. |
| 4 | Enemy cadence | **Superseded 24 Oct 2026.** Auto-cast stays, but the roll is random: first cast waits 2-6 s, then 35% per 1-3 s roll (`Combat-Sequencing.md` §9). A metronome read as unfair. |
| 5 | Summon | **Deferred.** It needs mid-fight combatants, which the sim does not do today. Built last, as a temporary ally. |
| 6 | Auto-trigger | **Deferred to an ascension unlock**, per hero (not a global toggle). |
| 7 | Enemy v1 set | **Block / Beam / Spike.** Enemy Summon waits for the villain band. |
| 8 | Per-clip fps | **Stored per clip** from day one (the art set already measures each strip for the strike frame). |

## 2. The v1 ability catalogue

Heroes (3 playable now, 3 art-ready reserves unlocked later):

| Hero | Ability | Strip | Effect (v1) | Status |
|---|---|---|---|---|
| Knight | Shield Wall | `Block:4` | self shield: damage taken x0.5 for N s | **v1** |
| Soldier | Charge | `Attack02:6` (its spare attack) | heavy blow on the front enemy | **v1** |
| Wizard | Shatter | `Attack02:9` (VFX overlay deferred) | ranged hit on the front enemy | **v1** |
| Priest | Heal | `Heal:6` | restore HP to the lowest-health ally | v1 effect, gated by hero unlock |
| Knight Templar | Bulwark | `Block:4` | party shield (the Shield buff applied to all allies) | v1 effect, gated by hero unlock |
| Lancer | Impale | `Attack02:9` | piercing hit on the front enemy | v1 effect, gated by hero unlock |

**Roster swap (v8).** The Archer left the party and became an enemy; the Soldier took its place and wears `enemy_volley`
from the enemy side. The reason is the art: the Soldier's sheet was already on two enemies, and a hero that looks
exactly like a common wave enemy is unreadable in a battle made of stacked sprites. Swapping the two roles gives every
sheet exactly ONE job. The hero ids moved with it (`hero_archer` -> `hero_soldier`), so a save's keyed stat levels are
renamed on load rather than stranded.

**One ability active at a time, per hero.** A hero may own several clips (see below) but only one is switched on. That
is what keeps the choice meaningful, and it is why the save holds one ability id per hero rather than a set.

**Every clip beyond the base attack is ability material.** The pack gives some characters more than one spare action.
Counting the composited casts, the Knight owns 3 clips (Attack02, Attack03, Block), the Soldier 2, the Wizard 3
(Attack02 plus both composited casts).

**Two clips per hero, one equipped, no unequip.**

| Hero | Ability 1 | Ability 2 |
|---|---|---|
| Knight | Shield Wall (`Block`) | Heavy Strike (`Attack02`) |
| Soldier | Charge (`Attack02`) | War Cry (`Attack03`, a party shield played from a swing clip - it has no block art) |
| Wizard | Shatter (`Attack02`) | Arcane Burst (`Attack03`, the composited cast) |

A hero equips ONE of the two and can never clear it: switching to the other is allowed, unequipping is not, because a
hero with no ability is only ever worse and there is no decision in it. Buying the second ability does NOT switch to
it - the player chooses. Buying the first equips it, since there is nothing to switch from.

**Composited strips earn a spare attack slot.** A `(With magic effects)` strip is the same wind-up with the spell
already drawn into the frames. The importer may place one in an attack slot the character has NO plain strip for -
never Attack01, so the primary swing is never replaced - which is how the Wizard's second ability gets a distinct cast
instead of repeating its basic attack. Four characters claimed one: the Wizard and Necromancer (Attack03), the Warlock
(Attack03) and the Priest (Attack02). The rest of their composited and overlay strips stay recognised but unused.

**The battle panel is a tab: `[ BATTLE LOG | ABILITIES ]`.** The second tab shows one column per hero - portrait, name,
HP bar, equipped ability, a cooldown clock and a CAST button. Casting plays the clip, starts the cooldown and writes a
log line. What an ability DOES is built after that plumbing, so the casts can be watched first.

Enemies (auto-cast, same vocabulary):

| Type | Characters | Effect | Status |
|---|---|---|---|
| Block | Knight, Knight Templar, Armored Orc, Orc rider, Skeleton, Black Knight_B | defensive stance: damage taken x0.5 briefly | **v1** |
| Beam | Black Knight_C, Eyeball Monster, Ghostfire | ranged hit on a hero | **v1** |
| Spike | Lava Slime | ground spike: hit on a hero | **v1** |
| Summon | Necromancer, Skeletons | spawn a Skeleton Archer | **deferred** |

## 3. Architecture — where each piece lives

- **Art** (`CharacterArtSet`): the ability strips are *already recognised* by `CharacterActionResolver`
  (`Summon`/`Block`/`Heal`/`Beam`/`Spike`) but not imported. S1 imports them into new named slots, plus a
  per-clip fps, using the exact slice/crop the base strips use.
- **Data**: one `AbilityDef` asset per ability (id, display name, kind, art slot, fps, cooldown, effect params,
  unlock stage + token cost). Authored in a spec (`abilities.json`) and generated, like heroes/enemies.
- **Sim** (`Encounter`): abilities are **sim features first** — deterministic and engine-free, so the log,
  cooldowns, enemy auto-cast and offline all share one truth. An ability is an *off-turn side channel*: the sim
  can resolve one alongside whatever swing is running, rather than blocking the single-actor ATB.
- **Unlock** (`TrackService` keyed levels): level 0 = locked, 1 = unlocked — no save-schema change. Unlock =
  a **token purchase** gated behind a **stage milestone**. Assignment = a per-hero ability id, saved per hero.
- **Party tab**: the existing 3 "ABILITIES" placeholder slots become the assign row (tap -> pick that hero's
  unlocked ability).
- **Battle tab**: the log header strip becomes a 2-tab bar **`[ BATTLE LOG | ABILITIES ]`**; the ABILITIES tab
  is the 3 hero buttons, each with the radial cooldown sweep. Tapping casts the ability.
## 4. The sim effect model (what "cast" actually does)

1. A cast is announced: `AbilityAnnounced(attacker, ability, target)` -> the view plays the strip.
2. After the strip's strike-frame time (the same measured frame the attacks use), the effect resolves:
   `AbilityResolved(attacker, ability, target, …)`.
3. Effects are three kinds for v1:
   - **Shield** — a timed buff: damage taken x a factor for N seconds (self, or party for Bulwark).
   - **Heal** — add HP to the lowest-health ally, clamped to max.
   - **Damage** — a hit on the front enemy (or front hero for enemy abilities), rolling crit like a normal swing.
4. **Cooldown** is a per-ability seconds value, ticking on the sim clock, exposed to the UI as remaining time.
5. **Enemy auto-cast** is the same path without a tap: the sim casts on the enemy's cooldown from wave start.

## 5. Build order (each step lands and can be played)

| Step | What | Acceptance |
|---|---|---|
| **S1** | Import the ability strips into the art sets (new slots + per-clip fps + a measured strike frame). | **DONE 2026-10-13** — 15 of the 42 characters carry a special (`Block`/`Heal`/`Beam`/`Spike`/`Summon`) with its own fps and strike frame; the art validator reports "42 sets checked, 15 with a special strip, PASS". Playback arrives with S3. |
| **S2** | `AbilityDef` + `abilities.json` spec + generator; unlock (token cost + stage gate) riding the keyed level list; the Party-tab assign row. | Assign an unlocked ability to a hero; it survives save/reload. Split in three: **S2a** data spine (done), **S2b** unlock + assignment service, **S2c** Party-tab row. **The assignment needed one additive save field and so moved the schema to v7** (the unlock itself still needs none). |
| **S3** | The sim effect layer + **Knight's Shield Wall end-to-end**: battle tab, tap-to-cast, cooldown clock, log line, the shield actually reduces damage. | Tap -> Knight plays Block -> incoming damage is halved for N s, all visible and logged. |
| **S4** | Heal, Volley, Shatter, Impale, Bulwark; enemy Block/Beam/Spike auto-cast. | Every v1 ability has a working effect, its own animation and cooldown, and is logged. |
| **S5** | The three reserve heroes (Priest, Knight Templar, Lancer) and their abilities behind stage-milestone unlocks. | Unlock a reserve hero mid-run and assign/use its ability. |
| **S6** | Auto-cast ascension unlock (per hero). | With the ascension upgrade, the hero casts on cooldown with no tap. |

Deferred (in order of return): enemy Summon / the Necromancer villain ability, the Wizard's VFX overlay,
ability recolours, and the auto-cast polish once ascension is felt.

## 6. Risks, named now

- **Per-clip fps**: a 6-frame Block vs an 18-frame Summon cannot share one fps. Stored per clip (decision 8).
- **Summon**: real sim work (mid-fight combatants). Parked, so it cannot stall S1–S4.
- **Off-turn resolution** — resolved by making a cast an action in the lane (decision 1, `Architecture.md` B39). The
  sim needed no change to its action model; the window comes from the strip.
- **Balance**: cooldowns and effect strengths are guesses until played. Each ability carries its numbers in the
  spec, so they are one edit each, like the ladder's band table.

## 7. State (24 Oct 2026)

S1-S4 are built: per-clip fps + strike frames, `AbilityDef` + `abilities.json`, unlock/assign (save v7), cast in the
lane with real effects, enemy auto-cast with random timing. **S5** (reserve heroes) and **S6** (hero auto-cast) remain.
Built behaviour: `Combat-Sequencing.md`. Status: `Ability-Progress.md`.

