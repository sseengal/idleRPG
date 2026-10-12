# Third-party asset provenance

> Required by `Docs/Art-Pipeline.md` §5: every shipped asset must be traceable to a tool, a licence and a date,
> for the store/legal pass and for replacing generated art later.
> Keep this file next to the art it describes. One entry per pack, added the day it is imported.

---

## Kenney Pixel UI Pack

| Field | Value |
|---|---|
| id | `ui_kenney_pixel` |
| source | https://kenney.nl/assets/pixel-ui-pack |
| author | Kenney Vleugels (with Lynn Evers) |
| licence | **CC0 1.0** (public domain dedication) - commercial use allowed, credit not required |
| licence text | `Kenney_PixelUI/License.txt` (shipped in the pack, kept verbatim) |
| downloaded | 2026-09-29 |
| contents as imported | 35 PNGs: 9-slice panels/buttons in 3 families (Ancient, Colored, Outline), 48x48; one 16x16 tile sheet |
| used for | UI panels, buttons, tabs, nav (battle screen first) |
| not in it | currency icons (gold/gem/token) - the pack has no icon set, those need a separate source |
| import policy | `Tools > Idle RPG > Art > Apply UI-Art Presets` (Point, no mips, uncompressed, PPU 100, 12px 9-slice border) |

## Pixel fonts (Google Fonts, SIL Open Font License 1.1)

| Field | Value |
|---|---|
| licence | **OFL 1.1** - free to use, embed and ship commercially; the licence text must travel with the font |
| licence texts | `PixelFonts/OFL-Silkscreen.txt`, `OFL-VT323.txt`, `OFL-Jersey10.txt` |
| downloaded | 2026-09-29 |
| usage rule | TMP **SDF** font assets sampled at 64px: legible at any screen scale (see `Docs/Art-Pipeline.md` §17) |

| Font | Source | TMP asset (in Resources) |
|---|---|---|
| Silkscreen | github.com/google/fonts/ofl/silkscreen | `Assets/IdleRPG/Resources/Fonts/Silkscreen Pixel.asset` |
| VT323 | github.com/google/fonts/ofl/vt323 | `Assets/IdleRPG/Resources/Fonts/VT323 Pixel.asset` |
| Jersey 10 | github.com/google/fonts/ofl/jersey10 | `Assets/IdleRPG/Resources/Fonts/Jersey10 Pixel.asset` |

OFL note: the fonts may be shipped inside the game; the licence text must be included in the build's
attributions (the store packet pass, B10b, owns that).

---

## Tiny RPG Character Asset Pack 01 v2.0 — Full (22 characters)

| Field | Value |
|---|---|
| id | `tinyrpg_pack01_full` |
| source | `~/Downloads/Tiny RPG Character Asset Pack 01 v2.0 -Full 22 Characters.zip` |
| drop location | `TinyRPGAssets/` — **project root, outside `Assets/`**, so Unity and the build cannot see it yet |
| author / licence | **UNRESOLVED.** No licence, readme, `.txt` or `.pdf` exists anywhere in the zip — verified by listing the archive |
| grid | 100 px cells; a plain set and a `with shadows` set; Aseprite sources included |
| contents | 22 characters + `Arrow01-03` and `Magic` projectile art |
| used for | heroes, bosses and normal enemies — see `Docs/Character-Catalog.md` and `Docs/Stage-Ladder.md` |
| action needed | **find the store receipt or product page** and record the terms here. This gates the entire art plan |

## Tiny RPG Character Asset Pack 02 v1.01

| Field | Value |
|---|---|
| id | `tinyrpg_pack02` |
| source | `~/Downloads/Tiny RPG Character Asset Pack 02 v1.01-Free Demon_A&Blood Monster_A.zip` |
| drop location | `TinyRPGAssets2/` — **project root, outside `Assets/`** |
| author / licence | **UNRESOLVED.** No licence file in the zip |
| grid | 100 px cells; plain + shadowed sets; Aseprite sources |
| contents | 20 enemy-side characters (demons, hell, undead, Black Knights) |
| used for | bands 4–10 of the stage ladder |

---

## Known gaps (must be closed before submission)

| Pack | Gap |
|---|---|
| `TinyRPG_01_SoldierOrc` | **No licence file in the pack.** This art is **live on the battle page** — all three heroes, the boss, and the goblin slot — so it must be licensed before release, or replaced. |
| `tinyrpg_pack01_full` | **No licence file.** The store receipt is the only source. |
| `tinyrpg_pack02` | **No licence file.** The store receipt is the only source. |
| `PixelLab` (generated goblin) | Generated with the trial account; the account terms and the generation ledger need recording before shipping. **Not used in the game** — lab benchmark only. |


---

## Tiny RPG Character set (Soldier / Orc / Demon_A / Blood Monster_A)

| Field | Value |
|---|---|
| id | `tinyrpg_characters` |
| source | user-provided pack drop (`Assets/IdleRPG/Art/Characters`); README/LICENSE text **not present in the drop** |
| author/licence | **UNRESOLVED - TESTING ONLY.** No licence/readme file shipped with the pack. Store/legal pass must
  confirm terms before release (`Assets/IdleRPG/Art/Characters/TinyRPG/_Source` holds the raw drop for traceability). |
| imported | 2026-09-30 |
| contents | 4 characters x 100px-tile strips: Soldier (Idle/Walk/Attack01-03/Hurt/Death), Orc, Demon_A,
  Blood Monster_A (Idle/Walk/Attack01/Attack02/Hurt/Death), each with artist-baked shadow variants |
| layout | `Assets/IdleRPG/Art/Characters/TinyRPG/<Char>/<Char>_<Action>.png` (canonical, shadowed strips);
  original drop + aseprite sources under `TinyRPG/_Source/` |
| import policy | `Tools > Idle RPG > Art > Import TinyRPG Characters` (Point, no mips, uncompressed, PPU 100,
  grid-sliced on 100px colums; generates `Assets/IdleRPG/Data/Characters/<Char>_Art.asset`) |
| used for | heroes (Soldier), boss (Orc), goblin-slot rotation (Demon_A / Blood Monster_A by stage parity) |
