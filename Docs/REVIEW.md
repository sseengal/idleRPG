# REVIEW — candidates for deletion

> Nothing here is deleted until you confirm. Each entry says **why** it is a candidate, **what still depends on it**,
> and **what happens if we delete it**. Reply with the ids you want removed (e.g. "delete R1") and I will remove the
> file and fix any references in the same pass.

---

## R1 — `Docs/archive/Plan.md` (711 lines) — MVP build journal  ·  status: **DELETED 2026-09-22**

- Deleted on the owner's call ("check and update/remove obsolete docs"). The MVP it described is finished; its live
  content moved into `Checklist.md` (status), `Roadmap.md` (backlog) and `Architecture.md` (decision log), and it was
  the only doc still opening with "Last updated: Step 6".
- What was lost: the per-step verification logs for Steps 0-6 (what was tested on which date) and the original locked
  spec decisions in their first phrasing. `Architecture.md` keeps every decision that still binds.
- Every reference was removed in the same pass: root `README.md`, `Docs/README.md`, `Docs/Checklist.md`.
- If the history is ever wanted, `git log -- Docs/archive/Plan.md` still has it.

---

## Asset audit — 24 Oct 2026  ·  read-only, nothing deleted

How this was measured (so the numbers can be re-run):

- **Unity reverse-dependency scan.** All 180 roots (`.unity`, `.prefab`, `.asset`, `.controller`, `.anim`, `.mat`,
  `.playable`) were opened with `AssetDatabase.GetDependencies`, and each file's **external** reference count was
  recorded. A file that is itself a root is counted as 1 for itself, so "external 0" means nothing else points at it.
- **GUID search** across `Assets/` and `ProjectSettings/`, plus a code search for name-based loads. Only three
  by-name loads exist in the whole project: `Fonts/Silkscreen Pixel`, `Fonts/VT323 Pixel` and `UiSkin` — all inside
  `Resources/`. So an asset outside `Resources/` that nothing references by GUID is not loaded at runtime.
- Sizes from `du -sh`.

| id | Candidate | Files | Size | External refs | Verdict |
|---|---|---|---|---|---|
| R2 | Nine empty folder husks | 9 folders | ~0 | 0 | remove |
| R3 | `Art/Characters/TinyRPG/_Source` | 41 | 528 K | 0 of 41 | remove |
| R4 | `Assets/ThirdParty` unused tail | 96 of 102 | ~1.5 M | 8 kept | **DONE 24 Oct 2026** |
| R5 | Orphan data assets | 11 | small | 0 | remove (one caution) |
| R6 | `Assets/Screenshots` | 42 | 10 M | untracked | move outside `Assets` |
| R7 | `SaveSamples` | 2 | small | 0 | keep (PARKED) |

### R2 — nine empty folder husks

`Data/Currencies 1` … `Currencies 7`, `Prefabs`, `Art/Animations`. Each holds no file except its own `.meta`, and
none of the nine folder GUIDs is referenced anywhere.

- **Why they exist:** `ContentGenerator.cs:47` already names the cause — inside an asset-editing batch
  `AssetDatabase.IsValidFolder` did not see a folder the batch had just made, so Unity wrote duplicate companions
  (`Currencies 1.meta`, `Currencies 2.meta`, …). The generator has since been fixed, but the husks were committed.
- **What depends on them:** nothing. `Art/Animations` is the animation builder's output root and the builder calls
  `EnsureFolder(OutputRoot)` before writing, so it recreates the folder when needed.
- **If deleted:** no change. The animation builder still recreates `Art/Animations` on its next run.

### R3 — `_Source` raw art

`Assets/IdleRPG/Art/Characters/TinyRPG/_Source` — 41 files (split frames, `.aseprite` sources, shadow variants).
0 of 41 referenced. The game reads the packed sheets in `TinyRPG/<Character>/`, not these.

- **If deleted:** no change to the game. The original packs still live outside `Assets/` (`TinyRPGAssets/`,
  `TinyRPGAssets2/`, ignored by Git) and in `Assets/ThirdParty/TinyRPG_01_SoldierOrc` for Soldier and Orc.
- **Downside:** if a character is ever re-imported by hand, the split frames are the convenient input. The `.aseprite`
  files are the real source; the PNG splits can be regenerated.

### R4 — `Assets/ThirdParty` unused tail  ·  **DONE 24 Oct 2026**

**What happened:** 94 asset files plus 8 download archives (`.zip` / `.bin`) moved from `Assets/ThirdParty/` to
`ThirdPartyAssets/` at the project root — outside `Assets/`, so Unity no longer imports them. `ThirdPartyAssets/`
was added to `.gitignore`. Git shows the old paths as deleted; nothing was lost, the files are on disk.

**Result:** `Assets/ThirdParty/` went from 102 files (1.6 M) to 13 files (420 K). Verified after the move:
`Tools > Idle RPG > Art > Validate Art Pipeline` prints **PASS**, and none of the 89 moved GUIDs is referenced
anywhere in `Assets/`.

**The 8 files kept, and who reads each one:**

| File | Read by |
|---|---|
| `Kenney_PixelUI/9-Slice/Colored/grey.png` | `Main.unity`, `UiLab.unity`, `Resources/UiSkin.asset` |
| `PixelFonts/Silkscreen-Regular.ttf` | `Resources/Fonts/Silkscreen Pixel.asset`, both scenes |
| `PixelFonts/VT323-Regular.ttf` | `Resources/Fonts/VT323 Pixel.asset`, both scenes |
| `PixelFonts/Jersey10-Regular.ttf` | `Resources/Fonts/Jersey10 Pixel.asset`, `UiLab.unity` |
| `PixelLab/goblin/Goblin_Idle_anim.png` | `ArtLab.unity` + `DataAssetGenerator.GoblinIdleSheet` |
| `…/Soldier/Soldier/Soldier_Idle.png` | `ArtLab.unity` + `DataAssetGenerator.SoldierIdleSheet` |
| `…/Soldier/Soldier/Soldier_Attack01.png` | `DataAssetGenerator.SoldierAttack01Sheet` (path, not GUID) |
| `…/Orc/Orc/Orc_Idle.png` | `DataAssetGenerator.OrcIdleSheet` (path, not GUID) |

The last two would have looked "unused" to a reference scan, because `DataAssetGenerator` reads them by **path
string**, not by GUID. That is why this review happened before the move instead of after.

**Licence records stay tracked:** `PROVENANCE.md`, `Kenney_PixelUI/License.txt` and the three `OFL-*.txt` files
remain under `Assets/ThirdParty/`. The art validator requires `PROVENANCE.md`, and the store packet needs the licences.
The packs themselves are ignored by Git, so a fresh clone has the game art but not the raw source packs.

**Two tools now see less art, on purpose:**

- `CharacterAnimationBuilder` scans `Assets/ThirdParty` for `<Character>_<Clip>_anim.png`. Only the Goblin Idle strip
  remains, so a re-run would build a one-clip Goblin controller. Adding a character later means dropping its strips
  back into `Assets/ThirdParty` first.
- `ArtPresetApplier` and `UiArtPresetApplier` scan the same folders. They now have 8 files to preset instead of 102.
  Both still run without errors.

### R5 — eleven orphan data assets

| File | Why it is orphaned |
|---|---|
| `Data/Currencies/Currency_Gold`, `_Gems`, `_Tokens`, `_Shards`, `_Materials`, `_Essence`, `_Scrolls` (7) | `CurrencyDef` is not read by any runtime script; only `DataAssetGenerator.CreateCurrency` writes them. Gold and gems are plain numbers in the balance config |
| `Data/Characters/Elite Orc_Art.asset` | No enemy or hero uses "Elite Orc"; the spec has `boss_elite_demon` / `boss_elite_demoness` instead |
| `Data/Config/Prestige_AutoEquipGear.asset` | `PrestigeEffectType.AutoEquipGear` is read from the save level; this asset is a duplicate data card nothing points at |
| `Data/Config/Prestige_AutoSalvageGear.asset` | Same for `AutoSalvageGear` |

- **Caution:** `DataAssetGenerator` would recreate the seven currency assets if it is ever run again. It has no menu
  entry today, so this does not come back by accident. If the generator is ever re-enabled, delete them again.
- **If deleted:** no change. The enum values and the save fields are untouched. Removing the seven leaves
  `Data/Currencies` empty, so that folder goes with them (the generator recreates the path if it ever runs).

### R6 — `Assets/Screenshots` (move, do not delete)

42 PNGs, 10 MB, and Git already ignores the folder (`Assets/Screenshots/` in `.gitignore`). They sit inside
`Assets/`, so Unity imports every one of them on a fresh clone and whenever they change.

- **Recommendation:** move the folder to the project root (outside `Assets/`) and keep it as the visual history.
  Deleting it loses the screenshots that `Docs/UI-Layout.md` and `Docs/Art-Pipeline.md` refer to by name.

### R7 — `SaveSamples` (keep)

`SaveSamples/README.md` and `v4_sample.json` have no external references, but they are input fixtures for manual
save-migration work, not runtime content. Keeping them costs two small files. **PARKED** — no action.

---


| Doc | Checked | Verdict |
|---|---|---|
| `Architecture.md` | 2026-09-21 (Step 11e) | current - decision log extended (B28-B33) |
| `Checklist.md` | 2026-09-21 (Step 11e) | current - master record, includes the standing regression surfaces |
| `Roadmap.md` | 2026-09-21 (Step 11e) | current - 11a/11b/11c/11e done, Step 12 next |
| `Sim-Core.md` | 2026-09-21 (Step 11e) | current - ranks removed, wave size derivation documented |
| `Content.md` | 2026-09-21 (Step 11c) | current - budget split + recipe described |
| `UI-UX.md` | 2026-09-21 (Step 11e) | current - battle-log contract added |
| `Idle-Economy.md` | 2026-09-21 (Step 11c refresh) | current - offline payout uses the wave-size mean |
| `Progression.md` | 2026-09-20 | current - untouched by Steps 10/11 (no progression changes) |
| `README.md` (root) | 2026-09-21 | current - quick start, now points into `Docs/` |
