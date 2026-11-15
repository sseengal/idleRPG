# Mobile memory budget - why builds died and what changed

> Owns: the memory and disk numbers for the Android build, what was changed on 2026-11-15, and the checks that
> keep them from drifting.
> Parent: `Art-Pipeline.md`, `Mobile-Readiness.md` (the player-facing checklist).

**Last verified:** 2026-11-15. Unity 6000.6.0f1, Android / IL2CPP / ARM64.

---

## 1. The failure was disk, not code

| Check | Value on 2026-11-15 |
|---|---|
| Mac RAM | 8 GB |
| Swap used | **8.4 GB of 8.9 GB** |
| Free disk | **258 MB of 228 GB (100% full)** |
| Editor at rest | ~1 GB (main + DataStore + helpers) |
| VS Code helpers | ~1.4 GB |

A phone build needs several GB of temporary space, and macOS grows swap on disk. With 258 MB free, an allocation
fails and the build or the Editor dies. That is the "out of app memory".

### Where the disk went

| Folder | Size | Safe to delete? |
|---|---|---|
| `Library/Bee` | **8.7 GB** | Yes. Unity's build scratch cache; it rebuilds. Next build is slower. |
| `Builds/Android/*_BackUpThisFolder...` | 1.0 GB | Yes. Crash-symbol copy only. **Deleted on 2026-11-15.** |
| `Library/PackageCache` | 978 MB | Only if packages can be re-fetched. |
| `~/Library/Caches/Unity/upm` | 312 MB | Yes, a package download cache. |
| `Builds/Android/idlerpg-dev.apk` | 73 MB | No, that is the build output. |

Target: keep **20 GB free**.

## 2. Texture memory (the runtime win)

Every art texture is imported twice: a lossless Default slot (what the Editor shows) and a phone slot.

- Before: the phone slot was **RGBA32 / Uncompressed** for 549 textures, forced by `CharacterArtImporter`
  and `ArtPresetApplier` (a deliberate crispness choice, not a Unity default).
- After: `MobileTexturePolicy` rewrites the **Android + iPhone** slot of all 721 2D textures to **ASTC 4x4**.

Measured from real PNG pixel sizes, for the 339 textures that scenes and data assets actually reference:

| State | Memory |
|---|---|
| Before (104 MB of pixels, 80 MB uncompressed) | **104 MB** |
| ASTC 4x4 (1 byte/pixel) | **26 MB** |
| ASTC 6x6 (0.44 bytes/pixel) - not used | 11.6 MB |

Point filtering and "no mips" are unchanged, so geometry and crispness of the drawn pixels are unchanged. ASTC is
lossy, so greyscale ramps and 1 px outlines can shift slightly; 4x4 is the highest-quality ASTC block.

**The Editor cannot show this change.** The Default slot is still lossless on purpose. Only an Android build shows
ASTC. Rollback: `Tools > Idle RPG > Art > Revert Mobile Textures To Uncompressed`.

## 3. Other numbers found

| Item | Value | Action |
|---|---|---|
| PNG textures in `Assets` | 721 | - |
| Textures referenced by nothing | **382** (189 MB of pixels) | See `Unused-Art-Report.md` |
| `maxTextureSize` | 2048 on every texture | Leave. Resizing breaks the 100 px slicing grid. |
| Sprite atlases | none (`m_SpritePackerMode: 0`) | Later: one atlas per group, not one giant atlas |
| TMP fonts | 3 dynamic 1024x1024 atlases, 4.7 MB of assets in `Resources` | Later: 512 or static atlas |
| Editor quality level | was **PC** (4 shadow cascades, 2048 shadow map, HDR) | Now **Mobile**, to test what ships |
| Code memory hot spots | 3 `Instantiate` sites, 1 `Find`, pooling in place, incremental GC on | Nothing to fix here |

## 4. Budgets

| Budget | Limit | How to measure |
|---|---|---|
| Device texture memory | < 60 MB | Unity Profiler, Memory module, Android build |
| Device total PSS | < 300 MB | `adb shell dumpsys meminfo <package>` |
| Editor RSS during a build | < 2.5 GB | `ps -o rss,comm -p $(pgrep -f Unity.app/Contents/MacOS/Unity)` |
| Free disk | > 20 GB | `df -h .` |

## 5. Checks that keep it from drifting

- `Tools > Idle RPG > Art > Validate Art Pipeline` now fails a strip that has no ASTC 4x4 Android slot.
- Re-run `Apply Mobile Texture Compression` after adding art by any route that writes its own import settings.
- Do not add a texture to `Assets` that the game does not use. Keep source packs outside `Assets`.

## 6. Remaining steps (need a human)

1. **Free the 8.7 GB.** Quit Unity fully, then:
   ```
   rm -rf "/Users/siddharth/Idle RPG/Library/Bee"
   ```
   The next build is a cold build (slower, one time). This cannot be done while the Editor is open.
2. **Look at one hero** on an Android build. ASTC is lossy. Compare Idle + Attack01 in the battle view before
   shipping. If the outlines smear, run the revert menu and we pick ETC2 or a different block size.
3. **Move the unused art** with `Docs/move-unused-art.sh` after reading `Unused-Art-Report.md`.
4. **Measure** texture memory and PSS on the device, and fill the table in §4.
