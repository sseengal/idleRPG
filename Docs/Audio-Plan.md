# Audio plan — cues, music, licences

> Owns: every sound the game makes, where it comes from, and what licence it ships under.
> Needed before the final art + audio pass (`Launch-Plan.md` §2.1). **Nothing here exists as a file yet.**

**State at 24 Oct 2026:** the wiring exists, the sounds do not. A placeholder service hums generated tones, so the
game is already audible and mixable. No `.wav`/`.ogg` file ships today, and there is no music.

---

## 1. Plain words

The game already shouts about everything that happens. One listener (`AudioDirector`) turns those shouts into
sound. Adding real audio = drop files in, point the cue table at them, tune volume. No gameplay code changes.

---

## 2. What exists today

| Piece | Where | What it does |
|---|---|---|
| `IAudioService` + `SfxCue` | `Scripts/Services/` | 9 cues: `Hit`, `Crit`, `Kill`, `Boss`, `LevelUp`, `Claim`, `Purchase`, `Ascend`, `Defeat` |
| `AudioDirector` | `Scripts/Services/` | One listener. Maps game events to cues, throttles noisy ones (hit 0.07 s, kill 0.05 s) |
| `PlaceholderAudioService` | `Scripts/Services/` | Generated tones, one per cue. 2D source, cached clips, mute wins over volume |
| Volume + mute | `PlayerPrefs` `IdleRPG.audioVolume`, `IdleRPG.audioMuted` | Survive relaunch; the full reset wipes them like a fresh install |

Current event → cue mapping:

| Game event | Cue | Notes |
|---|---|---|
| enemy damaged | `Hit` | throttled |
| enemy damaged, critical | `Crit` | always plays |
| enemy killed | `Kill` | throttled |
| boss spawned | `Boss` | once per boss wave |
| hero stat level up | `LevelUp` | |
| upgrade bought | `Purchase` | |
| offline rewards claimed | `Claim` | |
| ascension completed | `Ascend` | |
| party wiped / boss failed | `Defeat` | |

---

## 3. What is missing

| Gap | Why it matters | Size |
|---|---|---|
| **No clips** | The game ships as beeps | one licensed or authored set |
| **No music** | A 30-minute session has no bed | 2-3 loops (battle, menu, boss) |
| **New combat events have no cue** | `SwingStarted`, `AbilityCast`, `Healed`, `ShieldApplied` arrived 24 Oct and are silent | 4 cue rows |
| **No UI cue** | Taps, tab changes and sheet opens are silent | 1-2 cues |
| **No settings row for audio** | Volume/mute exist in code, but no screen exposes them | 1 row on the settings sheet |

`AbilityCast` is the important one: the ability feature is the loudest thing on screen and the only silent one.

---

## 4. Cue table — fill the licence columns before sourcing

Add rows; do not rename `SfxCue` values (they are the code contract).

| Cue | Should sound like | Length | File | Source | Licence | Attribution needed | Status |
|---|---|---|---|---|---|---|---|
| `Hit` | dull impact, low mid | 60-90 ms | | | | no | placeholder |
| `Crit` | brighter, short ring | 90-140 ms | | | | no | placeholder |
| `Kill` | short body fall / puff | 120-180 ms | | | | no | placeholder |
| `Boss` | low swell, 0.4 s | 400-600 ms | | | | no | placeholder |
| `LevelUp` | rising two-note | 200-300 ms | | | | no | placeholder |
| `Claim` | coin shower | 300-400 ms | | | | no | placeholder |
| `Purchase` | small click + chime | 100-150 ms | | | | no | placeholder |
| `Ascend` | long rising sweep | 600-900 ms | | | | no | placeholder |
| `Defeat` | falling minor | 500-700 ms | | | | no | placeholder |
| *(new)* `AbilityCast` | whoosh + charge | 200-400 ms | | | | no | **not wired** |
| *(new)* `Heal` | soft rising shimmer | 200-300 ms | | | | no | **not wired** |
| *(new)* `Guard` | metallic block | 100-150 ms | | | | no | **not wired** |
| *(new)* `UiTap` | dry click | 40-60 ms | | | | no | **not wired** |

Rules:
- **One file per cue.** No layering at runtime; bake the layer in the file.
- **Normalise to −16 LUFS** for one-shots, peak ≤ −1 dBFS. Phone speakers are the target, not headphones.
- **Mono.** The source is 2D; stereo costs size and buys nothing.
- **Ogg Vorbis**, quality ~0.4, mono. Keep the whole SFX set under ~1 MB.

---

## 5. Music plan

| Track | Where | Length | Loop |
|---|---|---|---|
| `Battle` | battle page, normal waves | 60-90 s | seamless |
| `Boss` | boss waves | 30-45 s | seamless |
| `Menu` | party / upgrades / ascend / shop | 60-90 s | seamless |

Rules: fade in/out over 0.5 s, never restart on wave change, duck music by ~3 dB while a boss cue plays.
Two tracks are enough for the MVP; a third is a stretch, not a requirement.

---

## 6. Licences and provenance

Every file needs a row before it is committed:

| Field | Example |
|---|---|
| File | `sfx_hit_01.ogg` |
| Source | `kenney.nl/impact-sounds` or "authored by …" |
| Licence | CC0 / CC-BY 4.0 / OFL / paid licence id |
| Attribution text | exact string required by the licence, or "none" |
| Commercial use | yes / no / restricted |
| Modified? | yes / no |

The store packet needs this (`B10b`). A cue with no provenance row does not ship.

Free, safe starting points: Kenney (CC0), OpenGameArt CC0 sets, Freesound CC0-filtered. **Do not** pull
"free" sounds from a search engine; the licence is usually unknown.

---

## 7. Build order

| Step | Work | Acceptance |
|---|---|---|
| A1 | Add the 4 missing cues to `SfxCue` + `AudioDirector` (`AbilityCast`, `Heal`, `Guard`, `UiTap`) | each plays on its event; throttling keeps a burst audible but not stacked |
| A2 | `ClipAudioService`: one clip per cue from `Resources/Audio/` (or a `SfxCatalog` SO), falls back to the placeholder tone when a file is missing | dropping one file in changes one sound; a missing file never throws |
| A3 | Source + licence the 9 existing cues | cue table complete, provenance rows filled |
| A4 | Music: one `AudioSource` for the bed, crossfade on page/boss change, mute follows the same prefs | boss track starts on a boss wave and stops after it |
| A5 | Settings row: Volume slider + Mute toggle on the settings sheet | value survives relaunch; mute silences music and SFX |
| A6 | Device pass at 50% volume | no clipping, no clicks, audible on a phone speaker |

A1 and A2 are code (~half a day). A3 is the long pole: sourcing and clearing licences.

---

## 8. Open decisions

1. **Authored or licensed?** Licensed CC0 is faster; authored matches the pixel look. Decide before A3.
2. **Settings surface:** a 3-row settings sheet was already planned (`Launch-Plan.md`); audio needs one of them.
3. **Music at MVP?** Two loops are cheap and make the game feel finished. Recommend yes.
4. **Ability cue variety:** one cue for every cast, or per ability kind (damage / heal / guard)? Recommend per kind —
   three files, and the player hears what happened without reading the log.

