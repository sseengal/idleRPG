# Release Checklist — the one list that gates the stores

> Owns: every step between "the loop works" and "both stores accept the build".
> Parents: `Launch-Plan.md` (the order), `Checklist.md` §1p (MVP = store-submittable), `B10-owner-inputs.md` (owner hand-offs).
> Created 2026-11-21. **This page is living: tick a box only when the gate passes, not when the work starts.**

---

## 0. How to use this page

- One row is one check. A check is done when its **gate** passes.
- Owner is **B** (builder: code, docs, editor) or **O** (owner: accounts, money, devices, art, stores).
- `[x]` verified against the code or the store console · `[~]` partly done · `[ ]` open · `[!]` blocked.
- Every `[x]` must name its evidence in the same line. If it has no evidence, it is not done.
- The list is ordered by dependency, not by comfort. Do not start a later box to avoid an earlier one.

**Definitions**

| Term | Means |
|---|---|
| MVP | One loop a stranger can play for 30 minutes on a phone, from boot to run end to restart, with no dead end and no console error. |
| Release | Both stores accept a build that can earn money honestly and asks for every consent the law requires. |
| Gate | A human check that either passes or fails. "Looks fine" is not a gate. |

**Current state, 2026-11-21**

- Code is clean at `ba4a4cf`; Unity compiles with **0 errors** (18 obsolete-API warnings, editor-only).
- Save: **v12** with **3 rolling backups**; `runActive` decides boot resume vs lobby.
- Build target **Android / IL2CPP**; portrait lock and 60 fps bootstrap are live in code.
- **Lifecycle 2026-11-21:** the lobby is between runs only; a live fight has no menu; QUIT RUN ends the run; app close resumes the fight.
- **No** ads SDK, **no** billing SDK, **no** settings sheet, **no** app icon.
- Known route defects are listed in `Routes-and-Flows.md` §8 (R1–R18).

---

## 1. Scope lock (do first, or the date moves)

| Done | Item | Owner | Gate |
|---|---|---|---|
| [~] | v1.0 feature list frozen in `Checklist.md` | B | decisions taken 2026-11-21; the ordered list is in `Routes-and-Flows.md` + this page |
| [~] | v1.1 cut list written | B | recommended list in §11; owner to confirm |
| [x] | Story scope decided: **opening scenes + four flavour lines**; intro film **skipped** for v1.0 | O | `Intro-Sequence.md` (deferred) |
| [x] | Gold rule recorded: **permanent**, Training is the only gold sink | B | `Run-Loop.md` §3 correction, 2026-11-21 |
| [~] | App name, company, bundle id, version final | O | title **Kingsguard: Tiny RPG**; company `sseengal`; bundle id `com.sseengal.idlerpg` (confirm before records); version `0.9.0` |
| [x] | **Monetisation is required at v1.0** | O | decision 2026-11-21; minimal shop + IAP + ads are in scope, not optional |
| [x] | Current art may be used commercially without attribution | O | decision 2026-11-21; record the licence files anyway (§7) |
| [!] | Device work deferred to the end | O | decision 2026-11-21; §4 stays blocked until a phone + Mac/Xcode exist. **Risk:** the Play closed-test clock (12 testers × 14 days) cannot be compressed — start the account and the track as early as possible, even while device work waits. |

---

## 2. Code gates

| Done | Item | Owner | Gate |
|---|---|---|---|
| [x] | Save round-trip: v11, 3 rolling backups | B | `SaveSystem.cs:27`, `SaveData.cs:32` |
| [x] | Portrait lock + 60 fps target on device | B | `MobileRuntimeBootstrap.cs:34-41` |
| [x] | Android back button saves, then leaves | B | `AndroidBackButton.cs`, created in `GameManager.Awake` |
| [x] | Boot + Main in Build Settings, Boot at index 0 | B | `get_build_settings` 2026-11-21 |
| [x] | Android scripting backend IL2CPP | B | `get_player_settings` 2026-11-21 |
| [x] | No analytics SDK ships | B | `Packages/manifest.json` (decision §1p) |
| [ ] | `Run All Checks (regression)` PASS | B | full run, zero unexpected FAIL |
| [ ] | EditMode tests PASS | B | test runner, all green |
| [ ] | Console clean on 4 boot paths: fresh, resume, wipe, run end | B | zero errors in all four |
| [ ] | Save migration tested from the previous shipped version | B | real v10 file loads as v11 |
| [ ] | Crash log installed and writes a file | B | `CrashLog` is currently unused (0 call sites) |
| [ ] | Version stamp visible in the UI | B | settings sheet shows it |
| [ ] | Build from a clean checkout succeeds | B | clone → build, no local files needed |
| [ ] | Frame budget on device: 60 fps idle; TRAIN first open within 33 ms worst frame | B/O | phone measurement, recorded in `Mobile-Memory-Budget.md` §4 |

---

## 3. Gameplay acceptance

| Done | Item | Owner | Gate |
|---|---|---|---|
| [ ] | Boot with an active run resumes the **same fight**, never the lobby | B | kill app mid-fight, reopen, fight continues (no upgrade window) |
| [ ] | START begins a run from the lobby; no fight runs behind the lobby | B | boot between runs → START → stage 1 |
| [ ] | **No upgrade screen is reachable during a fight** | B | the pause menu offers resume/sound/quit only |
| [ ] | **Gear in the lobby opens SETTINGS** (sound, restore, privacy, version) | B | all four rows work; privacy URL may be a stub |
| [ ] | **Gear in a fight opens PAUSE**; resume restores time | B | time stops, resumes at the same wave |
| [ ] | QUIT RUN asks a second confirm, then ends and banks | B | KEEP FIGHTING returns; END RUN opens the lobby |
| [ ] | **BATTLE tab** begins the run from the lobby | B | same result as START |
| [ ] | Party wipe → RUN OVER card → REVIVE once (**Dawn Ward**) → same stage | B | no ad; second wipe hides the button |
| [ ] | Dawn Ward bought in **ASCEND** (20 tokens, max 1) | B | fourth row; revive available next run |
| [ ] | Party wipe → **END RUN** banks Perk Points → lobby | B | token count grows by the preview value |
| [ ] | Run-over card reads **END RUN** and returns to the lobby | B | label matches what happens |
| [ ] | Revive used, app killed mid-fight, reopen → revive still used | B | flag saved; no free second revive |
| [ ] | Party wipe on the run-over card + app close boots to the lobby, not a free revive | B | `runActive` is cleared on the wipe |
| [ ] | Perk Points, gold and training nodes persist; boons and camp reset | B | two runs in a row |
| [ ] | Level-up card pauses the fight; a pick applies; reroll works | B | pick changes the numbers |
| [ ] | Ability tap + cooldown + Guard + camp choices | B | each does what its label says |
| [ ] | TRAIN: 1,357 nodes scroll; affordable-only halo; purchase applies | B | first node and last unlock |
| [ ] | PARTY roster reads correct stats; gear slots inert (bag parked) | B | stats match the fight |
| [~] | SHOP: No Ads, 30-gem pack, Starter (50 gems + 30-min boost), Golden Foundry | B | page built; mock purchase verified; real prices with the store SDK |
| [ ] | No-Ads owner sees no ad buttons and no consent prompt | B | purchase no-ads, restart, confirm silence |
| [ ] | Daily streak pays once per local day | B | change the device date, no double pay |
| [ ] | Offline popup: shipped **or** removed, never hidden | B | no unreachable screen (defect R9) |
| [ ] | F8 full reset → clean boot | B | splash → lobby → new save |

---

## 4. Device matrix

> **Deferred by owner decision, 2026-11-21** until a test phone and a Mac/Xcode setup exist. The boxes stay
> unticked. This is the last phase, but the **store accounts and the Play closed-test clock start now**, because
> they are wall-clock work that needs no phone.

| Done | Item | Owner | Gate |
|---|---|---|---|
| [ ] | Low-end Android (the performance floor) | O | one full session, no crash, frame budget held |
| [ ] | Small iPhone (the notch floor) | O | safe area clean, no clipped labels |
| [ ] | Flagship (the feel floor) | O | motion and sound balanced |
| [ ] | Notch and punch-hole safe area | O | every bar and card clears the cut-out |
| [ ] | Background → foreground; kill app mid-fight → reopen | O | nothing lost, no double reward |
| [ ] | 15-minute memory read | O | no upward creep past the budget |
| [ ] | Low Power Mode | O | no softlock, frame rate honest |
| [ ] | Android back button | O | saves, then leaves |
| [ ] | Portrait lock holds; rotation refused | O | rotate the phone both ways |

---

## 5. Accounts and products (owner lane)

| Done | Item | Owner | Gate |
|---|---|---|---|
| [ ] | Apple + Google Play developer accounts live | O | both consoles open |
| [ ] | Play account type recorded (personal vs organisation) | O | written in `Checklist.md` §1p |
| [ ] | Play closed-test track running (12 testers × 14 days if personal) | O | track live, testers invited |
| [ ] | Store records created; bundle id reserved | O | id matches `ProjectSettings` |
| [ ] | 3 product records match `IapCatalog`; prices set | O | No Ads, 30 Gems, Starter Pack |
| [ ] | AdMob app ids + Unity Ads game ids recorded | O | written in `B10-owner-inputs.md` §6 |
| [ ] | Privacy policy + support URLs resolve | O | both open in a browser |
| [ ] | Play signing key + Apple provisioning ready | O | a signed build installs |

**Open question that sets the date:** a personal Play account needs **12 testers for 14 continuous days**.
Start that track before the code is finished. It cannot be compressed.

---

## 6. Legal and privacy

| Done | Item | Owner | Gate |
|---|---|---|---|
| [ ] | Privacy policy text matches what ships | O | no listed SDK that is not in the build |
| [ ] | Play Data Safety form | O | answers match the manifest |
| [ ] | Apple `PrivacyInfo.xcprivacy` | B | file in the Xcode project |
| [ ] | iOS ATT usage string + prompt | B | prompt shows once, refusal respected |
| [ ] | EEA consent message (Google UMP) | B | message shows, choice stored |
| [ ] | No-Ads owners skip consent entirely | B | owner never sees the prompt |
| [ ] | Age / content rating questionnaire | O | rating issued |
| [ ] | Ad disclosure in the listing | O | listing says ads exist |
| [ ] | "No analytics SDK" declared honestly | O | both stores say no data collected |

**Compliance wins this phase.** A mismatched Data Safety form is worse than a missing feature.

---

## 7. Art and audio

| Done | Item | Owner | Gate |
|---|---|---|---|
| [ ] | App icon 1024 (plus Android adaptive icon) | O | icon appears on both phones |
| [ ] | Launch screen | O | splash matches the icon |
| [ ] | Real SFX + music replace the procedural placeholders | O/B | every cue in `Audio-Plan.md` has a clip |
| [ ] | Licences recorded for every art and audio asset | O | one list, one row per file |
| [ ] | Mute works and survives a relaunch | B | `PlaceholderAudioService.MutedPrefsKey` |
| [ ] | Final art pass: no placeholder-looking screen | O | three screens reviewed at arm's length |
| [ ] | TRAIN node icons and ability badges final | O | no letter fallback on screen |
| [ ] | Screenshots: 6.7" iPhone, Play phone, 1024×500 feature graphic | O | screenshots match the shipped build |

**Art starts when the screens stop moving.** An icon drawn for a screen that changes is money burned.

**Licence status (owner, 2026-11-21):** all art currently in the project may be used commercially **without
attribution**. Keep the licence files/links anyway — a store reviewer or a future artist may ask, and a missing
record is expensive to rebuild.

---

## 8. Store packet

| Done | Item | Owner | Gate |
|---|---|---|---|
| [ ] | Title, short description, full description | O | fits both stores' limits |
| [ ] | Category, keywords, support link | O | listing saves without error |
| [ ] | Review notes (no login required?) | O | reviewer can reach the fight in one tap |
| [ ] | Sandbox purchase on both platforms | O | product grants, save survives restart |
| [ ] | Restore purchase on both platforms | O | reinstall → restore → no-ads returns |
| [ ] | Ad no-fill or skip burns no cap and no reward | B | forced no-fill test |
| [ ] | Age rating issued | O | both consoles show it |

---

## 9. Submission and launch

| Done | Item | Owner | Gate |
|---|---|---|---|
| [ ] | Signed AAB with version code | O | installs from the Play console |
| [ ] | Signed IPA with build number | O | installs from TestFlight |
| [ ] | Internal testing / TestFlight pass | O | one full loop on a real phone |
| [ ] | Closed test pass | O | no new P0 in 14 days |
| [ ] | Staged rollout % chosen | O | written in this page |
| [ ] | Rollback plan written | B | previous AAB/IPA kept and installable |
| [ ] | Day-0 monitoring owner named | O | Play vitals + Xcode crashes checked daily |

---

## 10. Post-launch

| Done | Item | Owner | Gate |
|---|---|---|---|
| [ ] | First-patch branch ready | B | branch cut before launch |
| [ ] | Crash triage owner named | O | one person, one rota |
| [ ] | Save-compatibility promise published | B | written rule in `Architecture.md` |
| [ ] | v1.1 list handed over | B | the cut list below becomes the backlog |

---

## 11. v1.0 cut list (recommended, owner decides)

| Cut | Why it can wait |
|---|---|
| Offline popup + idle payouts UI | The pivot removes idle from the loop; the screen is unreachable today. |
| Gear + bag UI | Drop chance is already **0**; the bag is parked. No player-visible loss. |
| Automation / speed / auto-buy | The panels are retired; manual play is the loop. |
| Leaderboard | Retired 2026-11-14; mock only. |
| 4th hero | Needs the roster picker (v1.1). |
| Story beyond the opening two scenes | Story is flavour, not a gate. |
| Localisation, notifications, cloud save, achievements, analytics, seasons, haptics | Not needed to submit. |

**Do not cut:** the minimal SHOP and Restore Purchases. Without them the game cannot earn, and Apple
rejects a non-consumable IAP that has no restore path.

---

## 12. Known route defects (fix before the boxes above can pass)

Full detail and evidence: `Routes-and-Flows.md` §8.

| # | Defect | Blocks |
|---|---|---|
| R1 | ~~START resets the run to stage 1~~ **fixed** (lobby has no live run) | — |
| R2 | ~~BATTLE tab does nothing~~ **fixed** (same path as START) | — |
| R3 | ~~SHOP tab is a dead end~~ **fixed**: lean shelf built | — |
| R4 | No settings sheet (mute / Restore / privacy) | §3, §6, §8 |
| R5 | No ads or billing SDK | §8, money path |
| R6 | First-run hint says "Tap UPGRADE" (page removed) | §3 first run |
| R7 | ~~No mid-run END RUN~~ **fixed** (QUIT RUN, two taps) | — |
| R8 | Level-up card is silent | §7 audio |
| R9 | Offline popup is unreachable | §3 |
| R14 | App name / id / version not final | §1, §5 |

---

## Change log

| Date | Change |
|---|---|
| 2026-11-21 | Created. Code synced to `ba4a4cf`; 0 compile errors. Two new pages: this checklist and `Routes-and-Flows.md`. |


