# Ability Map — the special animations, and what they become

The packs ship **named special actions**, not just attacks. That means abilities can be **animation-backed from day
one** instead of reusing attack frames. This document maps each special action to the ability it backs, its owner, and
how it is unlocked.

**The rule that makes this cheap:** `Attack01` is always the **base attack**. Everything else a character can do is
backed by a *named* strip. Nothing is invented.

---

## The six heroes

Three play at a time; the other three are art-ready reserves. **No Necromancer — it is a villain.**

| Hero | Base attack | Special action in the pack | Ability (proposal) |
|---|---|---|---|
| **Knight** | Attack01:7 | `Block:4` | Shield Wall — reduces incoming damage |
| **Archer** | Attack01:9 | Attack02:12, Attack03 — *none* | Volley — a heavier shot |
| **Wizard** | Attack01:6 (+`Attack01_Effect`) | `Attack02_Effect`, `Attack01_Effect` | Fireball — ranged spell with its own VFX strip |
| **Priest** *(reserve)* | `Attack:9` | **`Heal:6`** (+`Heal_effect`) | **Heal** — restores a party member. Your idea, and the art exists |
| **Knight Templar** *(reserve)* | Attack01:7 | `Block:4` | Bulwark — party-wide defence |
| **Lancer** *(reserve)* | Attack01:6 | Attack02:9, Attack03:8 | Impale — a piercing strike |

**Archer is the one gap:** it has two attacks and no special action. Its second attack becomes its ability.

---

## Enemy abilities — the same vocabulary, no new art

| Character | Action | Becomes |
|---|---|---|
| **Necromancer** | `Summon:10` (+ effect strip) | summons **Skeleton Archers** into the fight |
| **Skeleton Archer** | `Summon:5` | the summoned unit itself |
| Skeleton, Armored Skeleton, Greatsword Skeleton | `Summon:5` | low-tier summoners (optional) |
| Knight, Knight Templar, Armored Orc, Orc rider, Skeleton, Black Knight_B | `Block:4` | a defensive stance |
| Black Knight_C, Eyeball Monster, Ghostfire | `Beam:7 / Beam:3` | a ranged beam attack |
| Lava Slime | `Spike:5` | a ground spike attack |

---

## The unlock plan — and the version that needs no save change

Heroes and abilities are **unlocked**, so the plan needs three things: a rule, a place to store it, and a cost.

**Storage — the cheap version.** The save already carries a **keyed level list** (schema v5/v6). An unlock can ride it:
**level 0 = locked, level 1+ = unlocked.** That means **no schema change** and no migration risk. A separate
"unlocked heroes" flag would force a schema bump — avoid it unless a level genuinely cannot express the state.

**Cost — the cheap version.** Ability upgrades can ride the existing **progression tracks** (`TrackService`), which
already own levels, costs and the purchase path. A new ability is a row in the track spec, not a new system.

**Rule — proposal.**

| Unlock | Rule |
|---|---|
| Knight, Archer, Wizard | playable from the start |
| Priest | unlocked at a stage milestone |
| Knight Templar, Lancer | unlocked at later stage milestones |
| Each hero's ability | bought as a track row, per hero |

**What does not exist yet:** the **combat effect** — what an ability actually does. That is `Roadmap.md` Step 13,
parked for v1.1. So the honest split is: **art and data now, the effect later.**

---

## The animation contract

| Slot | Source |
|---|---|
| Idle | `Idle` — or `Flying` frame 0 for the four flying creatures |
| Base attack | `Attack01` (or `Attack` for Priest / Skeleton Archer) |
| Ability 1 | the named special strip (`Heal`, `Summon`, `Block`, `Beam`, `Spike`) |
| Ability 2 | `Attack02` / `Attack03` where no special exists |
| VFX | the `*_Effect` strip, or the `(With magic effects)` composited variant |
| Hurt / Death | `Hurt`, `Death` |

**Frame-rate caveat:** attacks run from 6 to 18 frames and the art set holds **one fps per character**. Ability timing
will feel uneven until fps is stored per clip, or the variance is accepted.

---

## Verification

1. Every ability names a strip that **exists** in `Character-Catalog.md` — no invented animation.
2. Each hero's base attack and ability are observed live, at 1:1, on a device.
3. Unlock state survives a save/reload round-trip (the existing `SaveRoundTripTests` cover the levels list).
4. No ability ships before its effect exists — otherwise the animation plays and nothing happens.
