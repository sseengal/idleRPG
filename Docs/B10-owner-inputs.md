# B10 — your start-list (exactly what to do or hand to me)

B10 swaps the fake register + fake cinema for the real ones. **Both ad sellers are baked in from the start** — Step 3
creates BOTH accounts; there is nothing optional here. **None of it can start until the pieces below exist.**
Each step is either **You do** or **Hand me**. Tick it when done. Most take 5-15 minutes; the installs take longer.

---

## Step 1 — the real names (10 minutes, you decide + hand me)

- [ ] **Company name** (shown in the stores; goes into the save folder on phones).
- [ ] **App display name** (what the store shows, e.g. "Idle RPG").
- [ ] **Bundle id / package id** — one string used by BOTH stores, e.g. `com.<yourname>.idlerpg`.
      *Rule: pick it once. It can never change after launch without becoming a different app.*
- [ ] **Commission the app icon now** (1024x1024 + a launch screen) — standalone art with the longest lead;
      the store packet (B10b) and the store screenshots need it, so it starts today, not at the art pass.

## Step 2 — the two store-build kits on this Mac (you install; the Go/No-Go menu confirms)

- [ ] Unity Hub → **Installs** → your Unity 6.0.6 install → **Add modules** → tick
      **Android Build Support** (SDK, NDK, OpenJDK) and **iOS Build Support** → install (10-30 min).
- [ ] **Xcode** — install from the Mac App Store (iOS builds + TestFlight; 30-60 min).
- [ ] Re-run `Tools > Idle RPG > Release > Go or No-Go` — it should list **no missing modules**.
      *(Android Studio is not required; Unity's module includes the SDK tools.)*

## Step 3 — the two ad-seller accounts (you create, hand me the ids)

- [ ] **AdMob** (Google) account → create an **App** for Android and one for iOS → write down the two
      **app ids** (they look like `ca-app-pub-...`) → also note the test-device option "Add test device".
- [ ] **Unity Ads** account → add the same two apps → write down the two **game ids** (one per platform) and
      create one **Rewarded** placement per platform (write its placement ids).
      *Hand me: the 2 AdMob app ids, the 2 Unity game ids, the 2 placement ids.*

## Step 4 — honest-purchase testers (you create, hand me the details)

- [ ] **Apple sandbox tester** — App Store Connect → Users & Access → Sandbox → add a test account
      (email + password). Hand me the email (never the real password store; you type it on the phone).
- [ ] **Google Play testers** — Play Console → your app → *internal testing* → add your own email to the testers
      list (or the group) so sandbox purchases work. Hand me nothing unless you want me to know who's testing.

## Step 5 — the one file I cannot download for you (you download, tell me where)

- [ ] Google's site: **"Google Mobile Ads Unity Plugin"** (.unitypackage). Download and save it somewhere easy,
      e.g. `~/Downloads/GoogleMobileAds.unitypackage`. **Hand me the full path.**
      *(Optional, only if you want the second seller bidding from day one: keep the Unity Ads org linked to the
      Unity account signed into this Editor — same one you created the Unity Ads dashboard with.)*

## Step 6 — hand it all over in one message

Paste me this block when done:

```
Company name:      ___
App display name:  ___
Bundle/package id: ___
AdMob app ids:     android=___  ios=___
Unity Ads:         gameIds=android=___,ios=___  rewardedPlacement=android=___,ios=___
Apple sandbox email: ___
Unity Ads account linked to this Editor: yes/no
Google plugin file at: ___
```

**Then I start B10:** install the purchase package + build the real register, wire the real cinema behind the
seams, build the consent question + Restore button + the tiny settings sheet, and hand you runbook §6 to test on
phones.

---

*Companion to `Docs/Manual-Tests.md` (§6) and `Checklist.md` §1p (Store Day-0, where these steps are tracked).*