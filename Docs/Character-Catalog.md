# Character Catalogue — all 42 characters, enumerated

**Generated from the packs, not hand-typed.** Every row below comes from enumerating each character's folder and
reading each PNG's width (frames = width / 100, because every strip sits on the pack's 100 px grid).

Sources, both sitting **at the project root, outside `Assets/`**, so Unity cannot see them yet:

| Pack | Path | Contents |
|---|---|---|
| Pack 01 v2.0 Full | `TinyRPGAssets/Characters(100x100 split)/` | 22 characters |
| Pack 02 | `TinyRPGAssets2/Characters(100x100 split)/` | 20 characters |

Each character ships **twice** — a plain set and a `... with shadows` set. **The game uses the plain set**: the
existing import takes `Art/Characters/TinyRPG/<Char>/<Char>_<Action>.png`, and `Art-Pipeline.md` §13 records that the
shadow variants were deliberately skipped (they bake a fake drop shadow; the scene casts real ones).

**Roles** are assigned by `Stage-Ladder.md`: `HERO` · `NORMAL` · `BOSS` · `SUMMON` (appears only inside a fight).

---

## Pack 02 — 20 characters (all enemy-side)

| Character | Role | Actions (name : frames) |
|---|---|---|
| Black Knight_A | BOSS | Attack01:16 Attack02:15 Attack03:15 Death:4 Hurt:4 Idle:6 Walk:8 |
| Black Knight_B | BOSS | Attack01:7 Attack02:8 Attack03:9 **Block:4** Death:4 Hurt:4 Idle:6 Walk:8 |
| Black Knight_C | BOSS | Attack01:8 Attack02:9 Attack03:13 **Attack03(No beam effects):13** **Beam:7** **Death:10** Hurt:4 Idle:6 Walk:8 |
| Demon_A | NORMAL | Attack01:7 Attack02:7 Death:4 Hurt:4 Idle:6 Walk:8 |
| Demon_B | NORMAL | Attack01:9 Attack02:6 Death:4 Hurt:4 Idle:6 Walk:8 |
| Demon_C | NORMAL | Attack01:6 Attack02:6 Death:4 Hurt:4 Idle:6 Walk:6 |
| Demon_D | NORMAL | Attack01:11 Attack02:18 Attack03:12 Death:4 Hurt:4 Idle:6 Walk:8 |
| Demon_E | BOSS | Attack01:8 Attack02:13 Attack03:8 Death:4 Hurt:4 Idle:6 Walk:8 |
| Demoness_A | BOSS | Attack01:9 Attack02:15 Attack03:16 Death:4 Hurt:4 Idle:6 Walk:8 |
| Demoness_B | NORMAL | Attack01:8 Attack02:8 Death:4 Hurt:4 Idle:6 Walk:6 |
| Blood Monster_A | NORMAL | Attack01:8 Attack02:8 Death:4 Hurt:4 Idle:6 Walk:8 |
| Blood Monster_B | NORMAL | Attack01:8 Attack02:9 **Death:6** **Flying:6** Hurt:4 |
| Eyeball Monster | BOSS | Attack01:8 Attack02:8 Attack03:6 **Attack03(No beam effects):6** **Beam:3** Death:4 Hurt:4 Idle:6 Walk:6 |
| Flame Golem | BOSS | Attack01:9 Attack02:9 Attack03:9 **Death:6** Hurt:4 Idle:6 **Walk:12** |
| Ghostfire | NORMAL | Attack01:6 Attack02:7 **Attack02(No beam effects):7** **Beam:3** **Death:6** **Flying:6** Hurt:4 |
| Hellbat | NORMAL | Attack01:6 Attack02:7 Death:4 **Flying:6** Hurt:4 |
| Hellhound | NORMAL | Attack01:8 Attack02:8 Death:4 Hurt:4 Idle:6 Walk:8 |
| Lava Slime | NORMAL | Attack01:6 Attack02:9 **Attack02(No spike effects):9** Death:4 Hurt:4 Idle:6 **Spike:5** Walk:6 |
| Minotaur | BOSS | Attack01:8 Attack02:7 Attack03:7 Death:4 Hurt:4 Idle:6 Walk:8 |
| Warlock | BOSS | Attack01:7 Attack02:6 **Attack02_Effect:9** Attack02(With magic effects):14 **Death:11** Hurt:4 Idle:6 Walk:8 |

---

## The naming traps — six classes of silent failure

The importer must **map** these names, not match them. Anything unrecognised is silently dropped today, which is
exactly how a missing animation ships unnoticed.

| # | Trap | Characters | Rule |
|---|---|---|---|
| 1 | `Attack` with no digits | Priest, Skeleton Archer | maps to **Attack01** |
| 2 | `Attack3` (no zero) | Swordsman | rename the file to `Attack03` |
| 3 | `DEATH` in capitals | Necromancer | case-insensitive matching |
| 4 | `Sumon_Effect` (typo) | Necromancer | rename the file |
| 5 | `Walk01` / `Walk02` | Knight Templar, Lancer | two-variant walk; take `Walk01` as Walk |
| 6 | `(With magic effects)` and `_Effect` | Necromancer, Wizard, Warlock, Priest, Black Knight_C, Eyeball Monster, Ghostfire, Lava Slime | composited variant + a separate VFX strip — **do not** treat as actions |

**Flying creatures use `Flying` instead of `Walk`**: Bat, Blood Monster_B, Ghostfire, Hellbat. Four characters have
**no Idle** (the same four): their idle pose must come from `Flying` frame 0, or from `Attack01` frame 0.

**Frame counts are not uniform.** Idle is always 6 and Hurt is 4 (except Swordsman, 5). Death is 4 (up to 11).
Attacks run from **6 to 18 frames** — a 3× spread. The art set holds **one fps per character**, so a 6-frame attack
plays three times faster than an 18-frame one. Either accept the variance or store fps per clip.

---

## What this catalogue is for

1. **Nothing is imported until this file is complete and the validator reports no unknown action names.**
2. It is **regenerated, never hand-edited** — the validator walks the pack folders and rewrites this table.
3. The roles here are the single source for `Stage-Ladder.md`; a character with no role is a planning gap.

## Pack 01 — 22 characters

| Character | Role | Actions (name : frames) |
|---|---|---|
| Archer | HERO | Attack01:9 Attack02:12 Death:4 Hurt:4 Idle:6 Walk:8 |
| Knight | HERO | Attack01:7 Attack02:10 Attack03:11 **Block:4** Death:4 Hurt:4 Idle:6 Walk:8 |
| Wizard | HERO | Attack01:6 **Attack01_Effect:10** Attack01(With magic effects):15 Attack02:9 **Attack02_Effect:7** Attack02(With magic effects):14 Death:4 Hurt:4 Idle:6 Walk:8 |
| Priest | HERO | Attack:9 **Attack_effect:5** Attack(With magic effects):9 **Heal:6** **Heal_effect:4** Heal(With magic effects):6 Death:4 Hurt:4 Idle:6 Walk:8 |
| Knight Templar | HERO | Attack01:7 Attack02:8 Attack03:11 **Block:4** Death:4 Hurt:4 Idle:6 **Walk01:8 Walk02:8** |
| Lancer | HERO | Attack01:6 Attack02:9 Attack03:8 Death:4 Hurt:4 Idle:6 **Walk01:8 Walk02:8** |
| Slime | NORMAL | Attack01:6 Attack02:12 Death:4 Hurt:4 Idle:6 Walk:6 |
| Bat | NORMAL | Attack01:6 Attack02:7 Death:4 **Flying:6** Hurt:4 |
| Werewolf | NORMAL | Attack01:9 Attack02:13 Death:4 Hurt:4 Idle:6 Walk:8 |
| Werebear | NORMAL | Attack01:9 Attack02:13 Attack03:9 Death:4 Hurt:4 Idle:6 Walk:8 |
| Skeleton | NORMAL | Attack01:6 Attack02:7 **Block:4** Death:4 Hurt:4 Idle:6 **Summon:5** Walk:8 |
| Armored Skeleton | NORMAL | Attack01:8 Attack02:9 Death:4 Hurt:4 Idle:6 **Summon:5** Walk:8 |
| Greatsword Skeleton | NORMAL | Attack01:9 Attack02:12 Attack03:8 Death:4 Hurt:4 Idle:6 **Summon:5** Walk:9 |
| Orc | NORMAL | Attack01:6 Attack02:6 Death:4 Hurt:4 Idle:6 Walk:8 |
| Armored Orc | NORMAL | Attack01:7 Attack02:8 Attack03:9 **Block:4** Death:4 Hurt:4 Idle:6 Walk:8 |
| Elite Orc | BOSS | Attack01:7 Attack02:11 Attack03:9 Death:4 Hurt:4 Idle:6 Walk:8 |
| Orc rider | BOSS | Attack01:8 Attack02:9 Attack03:11 **Block:4** Death:4 Hurt:4 Idle:6 Walk:8 |
| Armored Axeman | NORMAL | Attack01:9 Attack02:9 Attack03:12 Death:4 Hurt:4 Idle:6 Walk:8 |
| Soldier | NORMAL | Attack01:6 Attack02:6 Attack03:9 Death:4 Hurt:4 Idle:6 Walk:8 |
| Swordsman | NORMAL | Attack01:7 Attack02:15 **Attack3:12** Death:4 **Hurt:5** Idle:6 Walk:8 |
| Necromancer | BOSS | Attack01:9 Attack02:10 **Attack02_Effect:6** Attack02(With magic effects):10 **DEATH:9** Hurt:4 Idle:6 **Summon:10** **Sumon_Effect:7** Summon(With magic effects):13 Walk:6 |
| Skeleton Archer | SUMMON | **Attack:9** Death:4 Hurt:4 Idle:6 **Summon:5** Walk:8 |
