# Launch Plan — art, audio, and the road to both stores

Sequenced **2026-10-11** (owner + AI). This document adds no new work: it **orders** work that
already exists in `Roadmap.md` (B10 / B10b / B1a / B1b), `Checklist.md` §1e / §1p, `B10-owner-inputs.md`,
`Mobile-Readiness.md` and `Art-Pipeline.md` §9 / §17 / §541.

**Why it exists.** The project's own plan assumes parallel lanes — art runs *while* B10b and B1a
happen. This is a **one man + one AI** show, so the work is written here as a **single ordered lane**.
Only genuinely wall-clock items overlap, and every one of those is marked **⏱**.

---

## 0. The one fact that sets the date

```
ship date = max( code done , art + audio done , ⏱ closed-test gate elapsed )
```

`Checklist.md` §1p: a new **personal** Play account needs **12 testers × 14 continuous days** before
production access, and *"Store Day-0 is on the critical path — start it before writing code, not after."*

Fourteen days cannot be compressed. They can only be **started**. That is why this plan begins with
clock-starting chores rather than with art.

**Open decision 1 — is the Play account personal or organisation?** If personal, the 14-day gate is
live and starts today, and it may well become the thing that sets the ship date.

---

## 1. The two lanes

| Lane | Who | Scope |
|---|---|---|
| **Owner** | you | decisions, store + ad accounts, commissioning art, downloads, everything that happens on a real phone |
| **Builder** | the AI | the audio plan and its import, the design-token remainder, B10 wiring and consent, the settings sheet |

Work runs **one step at a time**. A step is finished when its gate passes, not when it is started.
Items marked **⏱** keep running by themselves once started — during those waits the next step proceeds.

---

## Phase 0 — Start the clocks  ·  owner  ·  today, 1–2 hours each

The tick-box version of this list is `B10-owner-inputs.md`. Every item either starts a wall clock or
unblocks B10.

| # | Step | Why it sits here | Done when |
|---|---|---|---|
| 0.1 | Decide and record the **Play account type** | decides whether ⏱ 14 days is on the critical path | the answer is written in `Checklist.md` §1p |
| 0.2 | **Start the Play closed-test track** (if personal) | the longest lead in the whole project | ⏱ the track is live and testers are invited |
| 0.3 | Fix the **names**: company, app display name, bundle/package id | `Mobile-Readiness.md` flags the current id as a **PLACEHOLDER** — *"the one field that is painful to change later"* | the three strings are final |
| 0.4 | Create the **Apple + Play developer accounts** | needed by the test track, the product records, and every phone build | both accounts exist |
| 0.5 | Create **both ad accounts**: AdMob (app ids) + Unity Ads (game ids, rewarded placements) | B10 cannot start without the ids | ids written into `B10-owner-inputs.md` §6 |
| 0.6 | **Commission the app icon (1024) + launch screen** | standalone art, longest lead, and the store packet needs it — `Checklist.md` decision 5 says "now" | the artist is booked and dated |
| 0.7 | Host the **privacy policy + support URL** | both stores require the URLs; the closed test needs them | both URLs resolve |
| 0.8 | Create the **5 product records** (no-ads, 3 gem packs, starter pack) with prices | must match `IapCatalog`; sandbox tests need them | records exist in both consoles |

**Gate:** every clock is running and B10 has every id it needs.

---

## Phase 1 — Get the game onto phones  ·  owner  ·  half a day, mostly waiting

From `Mobile-Readiness.md` §0 and §96.

1. **Install Android + iOS build modules** in Unity Hub, and **Xcode** from the Mac App Store.
   Both are long downloads — start them, then do Phase 0 while they run. **⏱**
2. **Re-run `Tools > Idle RPG > Release > Go or No-Go`.** It must report **no missing modules**.
3. **Android:** switch target → full reimport → first IL2CPP build (dev build, script debugging off)
   → install on a phone → boot-to-battle, crash-free 10 minutes, no exceptions in Logs.
4. **iOS:** the same, once the Apple account from 0.4 exists. The first build is slow (IL2CPP + signing).

**Do your B8' proof lap in the same phone session** — the battle log and the shop beat. One walk, two jobs.

**Gate:** the game runs on both phones. This is the prerequisite for the device passes *and* for the
art pass's texture-memory measurement.

---

## Phase 2 — Audio  ·  builder + owner  ·  do it now

`Checklist.md` decision 5: *"**Audio** can run in parallel any time — it never touches layout."*
That makes it the safest thing to start while the screens are still moving.

There is **no audio plan document** — that is the gap. The seam already exists: `IAudioService` +
`PlaceholderAudioService` (procedural tones, no files) + `AudioDirector`, the only class that maps
events to cues, with 9 cues: **hit, crit, kill, boss, level-up, claim, purchase, ascend, defeat**.

| # | Step | Lane | Notes |
|---|---|---|---|
| 2.1 | ~~Write `Docs/Audio-Plan.md`~~ **DONE 24 Oct 2026** - cue table, music plan, licence fields and the build order (A1-A6) are written | builder | next: pick authored vs licensed |
| 2.2 | Choose sources and licences (CC0 or paid) and **record the licence** | owner | same discipline already used for art |
| 2.3 | Import and swap the clips behind `AudioDirector` | builder | **zero gameplay code changes** — the seam was built for this |
| 2.4 | Add the **mute** row | builder | it lands inside B10's settings sheet, so the two ship together |
| 2.5 | Device check: sound balance and mute, on the phone from Phase 1 | owner | part of B1b |

**Gate:** every cue plays a real clip, mute works, every licence is recorded.

---

## Phase 3 — The look  ·  owner + builder

`Checklist.md` decision 5: art starts **when the screens stop moving — the end of B10**. Two pieces are
independent of that freeze and have long leads, so they start now.

**Now — owner**

1. **Commission the currency icons**: gold, gem, token, scroll, ad. `Art-Pipeline.md` §541 is explicit —
   *"The kit has none."* Small, bounded, and every screen needs them.
2. **Pick one of the 3 looks** from the UI Lab, and commit the **style anchor**: one key-art frame every
   later asset references (§9.6 — *"the cheapest defence against style drift"*).

**At the freeze — builder, then owner**

3. **Finish the design tokens.** ~109 hard-coded colours across 21 files, already begun in `UiTheme.cs`;
   the remainder is Shop / Upgrades / Ascend and their runtime views. Mostly mechanical.
4. **Reskin the remaining pages**, then re-check the **mobile texture-memory** item (§541, last bullet):
   uncompressed UI textures and the heavy post effects need a measurement on a device.
5. **Empty formation seats** that currently draw red HP bars (from the loop-walk leftovers).

**A gate that needs closing, not opening:** `Art-Pipeline.md` §9 still lists four unchecked **HD-2D**
decisions (backdrop strategy, prop source mix, battle-page integration, style anchor). Those belong to
the HD-2D program that §15 / §17 / §18 re-scoped down to pixel/retro. Either mark §9 **superseded**, or
decide you still want that program — but do not leave it sitting as an ambiguous open gate.

**Gate:** every screen reads from the theme or the skin, so a new label cannot invent a new literal.

---

## Phase 4 — B10: the store wiring  ·  builder, with owner inputs

`Checklist.md`: *"the largest remaining step."* This is where the screens stop moving — which is exactly
why it gates the art freeze in Phase 3.

| # | Step | Lane | Notes |
|---|---|---|---|
| 4.1 | Install the **Google Mobile Ads** plugin (the `.unitypackage` only you can download) | owner → builder | AdMob is the marketplace; it hosts the auction |
| 4.2 | Wire **Unity Ads as the second seller** (bidding, from day one) | builder | one config change if it misbehaves — fallback is AdMob alone, and gameplay never changes |
| 4.3 | Real **IAP** via `com.unity.purchasing` behind `IIapService` | builder | the mock swaps for the real one behind the same seam |
| 4.4 | **Restore Purchases** | builder | Apple hard-blocks a non-consumable without it — `IIapService.RestorePurchases` exists but is never called today |
| 4.5 | **Consent and privacy**: iOS ATT + usage string, EEA consent, `PrivacyInfo.xcprivacy`, Play Data Safety, both sellers' SDK manifests | builder | G15 is MVP-mandatory here |
| 4.6 | The **minimal settings sheet**: Restore + privacy-policy link + mute | builder | the MVP slice of G4; the mute row comes from Phase 2 |
| 4.7 | Verify decision 7: a **No Ads owner never sees the consent prompt** | builder | if ads are off, asking would be wrong |

**Gate:** a real rewarded ad and a real purchase both work in sandbox on both platforms.

---

## Phase 5 — Store packet + legal (B10b + G15)  ·  owner + builder

1. **Screenshots** — needs the icon (0.6) and the final look (Phase 3), at both aspect ratios.
2. **Listing copy**, **age-rating questionnaire**, **Data Safety form**, **ad disclosure**.
3. **Sandbox testers** (Apple) and **Play internal testers**, then run the purchase tests on a phone.

**Compliance wins this phase.** `Checklist.md` §1p: *"a mismatched Data Safety / privacy manifest is
worse than a missing feature."*

**Gate:** both store listings are complete and the sandbox tests pass on real devices.

---

## Phase 6 — Device passes, then submit (B1a, B1b)  ·  owner

1. **B1a — logic/perf pass.** The device matrix from `Mobile-Readiness.md` §2: one low-end Android
   (the performance floor), one small iPhone (the notch floor), one flagship (the feel floor).
   Start it as soon as Phase 1 gives you a build — it never touches layout, so the art freeze cannot
   invalidate it.
2. **B1b — looks pass, last.** Layout clean at both aspect ratios, art legibility, sound balance and
   mute, and **no F3 dev overlay**. Then submit both builds.

**Rejection risk to respect:** `Checklist.md` §1p — *"placeholder-looking builds (Apple 2.2 / 4.2)"*.
That is the reason art and audio must be **in** before submission, not after.

**Gate:** both stores accept the builds.

---

## What the builder does next, in order

| Order | Task | Needs from you first |
|---|---|---|
| 1 | ~~Write `Docs/Audio-Plan.md` (cue table + the fields to fill)~~ **DONE 24 Oct 2026** | nothing |
| 2 | Design tokens: Shop / Upgrades / Ascend onto `UiTheme` / `UiSkin` | nothing |
| 3 | B10 wiring: ads + IAP + Restore behind the existing seams | the ad ids (0.5) + the Google plugin file |
| 4 | The settings sheet: Restore + privacy link + mute | the privacy URL (0.7) + B10 started |
| 5 | Store-packet assets that are code-generated | the icon (0.6) + the look (Phase 3) |

Items 1 and 2 need no decisions from you and can start immediately.

---

## Decisions waiting on you

| # | Decision | Blocks | Recorded in |
|---|---|---|---|
| 1 | Play account type (personal vs organisation) | the whole schedule (⏱ the 14-day gate) | `Checklist.md` §1p |
| 2 | Company name, app display name, bundle/package id | Phase 0.4, both stores, every phone build | `B10-owner-inputs.md` §6 |
| 3 | The look: one of the 3 UI Lab options + the style anchor | Phase 3.3-3.5 and the screenshots | `Art-Pipeline.md` §17 |
| 4 | Audio sources and licences | Phase 2.2 | `Docs/Audio-Plan.md` §4 and §6 |
| 5 | `Art-Pipeline.md` §9: mark the HD-2D questions **superseded**, or keep that program | nothing today — but it must not stay ambiguous | `Art-Pipeline.md` §9 |

---

## Not in this plan

- **No refactoring.** Phase D closed on 2026-10-11 (`Refactor-Plan.md`): `GameManager` 1520 → 1367 lines,
  four jobs in their own files, every step verified. Nothing on the launch path needs another structural
  change.
- **The v1.1 cuts stay cut** (`Checklist.md` §1p): localisation, notifications, the roster picker,
  achievements, cloud save, tablet layout, a second orientation, analytics, seasons, haptics, font scale,
  and **new monsters** — those ship post-launch, drawn by the B8' kit.

---

## The whole plan, one line per phase

| Phase | What | Lane | Gate |
|---|---|---|---|
| 0 | Start the clocks | owner | every clock running, every id handed over |
| 1 | Get it onto phones | owner | runs on both phones |
| 2 | Audio | builder + owner | real clips, mute works, licences recorded |
| 3 | The look | owner + builder | every screen reads from the theme or the skin |
| 4 | B10 store wiring | builder | a real ad and a real purchase work in sandbox |
| 5 | Packet + legal | owner + builder | listings complete, sandbox passes on devices |
| 6 | Device passes, submit | owner | both stores accept the builds |

