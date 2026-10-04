# Mobile Readiness + Polish / Test / Playtest Plan

Status: MVP code is feature-complete. This is the hardening track BEFORE assets and monetization
are added - the goal is "the app is ready" on real devices, with a repeatable test loop.

## 0. Build line (in-flight)

| Item | State | Notes |
|---|---|---|
| iOS + Android build modules | installing via Unity Hub CLI (ios, android + child modules) | then: switch target, verify import, first device build |
| Scripting backend | IL2CPP on Android + iOS (set) | Mono/standalone untouched |
| Identity | company `sseengal`, product `Idle RPG`, version `0.9.0` | bundle id `com.sseengal.idlerpg` both platforms - **PLACEHOLDER: confirm before the first store record; the id is the one field that is painful to change later** |
| App icon / splash / screenshots | DEFERRED with visual assets | Checklist G4/G15 |

First-device build checklist (after modules):
1. Switch to Android, full reimport clean, IL2CPP build to .apk (dev build, script debugging off).
2. Install on one device; run boot-to-battle; crash-free 10 min; no exceptions in Logs.
3. Repeat for iOS (needs a Mac + free Apple dev account; first build is slow - IL2CPP + signing).
4. Only then enable Development Build for profiling sessions; ship Release elsewhere.

## 1. Regression gate (existing, extend to devices)

- EditMode suites (save round-trip, drop routing, formulas) - run before any commit touching the
  pure-logic folders (Docs/Checklist.md section 7).
- Manual smoke on EDITOR per step (.clinerules): boot, battle ticks, 3 tabs, dropdowns/sheet,
  leaderboard, overlay.
- DEVICE smoke superset (new): same loop on a phone, PLUS: portrait lock holds, notch safe-area
  (Dynamic Island + punch-hole), rotation rejected, app background/foreground, offline reward
  popup, Low Power Mode, mute button, memory over 15 min.

## 2. Playtest program

Principle: small, structured, repeated sessions - not one big QA week. Every session has a job.

### Device matrix (start with 2-3, not a lab)
- 1 low-end Android (2019-2021 midrange, 4 GB RAM) - the performance floor.
- 1 small iPhone (SE or mini) - the notch/iOS floor.
- 1 modern flagship - the show floor; feel testing.

### Phase A - build reads (1 week)
Ship a dev-build each day; the only task is PLAY IT for 10-15 min and file anything broken.
Target: 3 consecutive daily builds with zero crash/softlock. Do NOT touch balance yet.

### Phase B - feature-box pass (2-3 days)
Walk the feature map (Party, Inventory, Save, Leaderboard, Shop, Offline, Prestige) ON DEVICE
with the existing Manual-Tests.md as a checklist. Judgment calls go to playtest sessions instead.

### Phase C - polish reads (1 week)
One specific look/feel per session:
- first-session pacing: taps to first hero kill, first upgrade, first stage clear
- numbers legibility at arm's length (real phone)
- touch feel: dropdown dismiss, sheet retarget, two-tap discard arm/disarm
- save/reload anxiety: kill app mid-battle, reopen, nothing lost
- idle loop: 5 min away returns believable income; OfflineRewardsPopup numbers sane

### Phase D - balance read (uses the tooling already built)
BalanceLabMenu + ClimbSimulation + DevOverlay are the instruments; sessions feed them. Rebalance
only what the sessions flag - RENUMBER the SOs, never rewrite code. Check first 3 stages, first
prestige, gold curve against the Idle-Economy doc as the spec.

### Session template (any test user)
```
Session: <date> | build <version> | device <model>
Job of this session: <one thing>
Played: <min>
1. boot -> first boss in: <s>
2. first upgrade felt: yes/no - why
3. anything read twice? note it (legibility)
4. anything confusing? note it
5. anything annoying? note it
6. crash / stuck: <if any> -> reproduce steps
```

### Release to real testers (later)
- Android: Play Console > internal testing track (Google Play dev account, $25).
- iOS: TestFlight (free with the Apple dev account).
- Both come AFTER balance/polish reads; first impressions are precious - don't waste them.

## 3. Known-fragile list (the failure brain-map)

| Area | What breaks first | Watch |
|---|---|---|
| Save v6 | item dedupe across fill; worn-vs-bag on trim | EditMode SaveRoundTripTests before any save change |
| Transient UI | tap-swallow/fall-through when a widget closes | close via UiTransient (instant SetActive(false) + Destroy); never raw Destroy alone |
| Offline rewards | reward vs soak arithmetic | values on real clocks (device clock skew) |
| Leaderboard | mock is deterministic; real backend swap must keep Refresh/Ranked | Docs/Leaderboard-MVP.md |
| Fonts | silently-missing glyphs render as boxes | only UiGlyphs constants in UI strings |

## 4. Store submission gate (runs in parallel, blocks release)

Checklist G15 remains the gate: privacy policy URL, data-safety form (Apple+Google), age rating
questionnaire, ad disclosure, ATT copy (if ads), screenshot set (needs the deferred visual assets
- so assets gate the STORE page, not the build). First real device build needs none of this.
TestFlight / Play-internal-test is the milestone that does (privacy + export-compliance answers).

## Next automated step
1. Confirm modules finished -> switch to Android -> first IL2CPP build (dev, no run-tests).
2. Fix whatever the build flags (likely nothing; settings-only so far).
3. Commit ProjectSettings changes + this doc. iOS switch + build as a follow-up session.
