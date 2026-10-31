# Battle Log — as built (31 Oct 2026)

> **Status: current. Last checked against code: 31 Oct 2026.**
> This page owns the battle log: the store, the view, the colours, the typewriter and the layout.
> It does **not** own the story script: see `Story-Nodes.md`. It does **not** own scene timing: see
> `Story-Scenes.md`. UI roles and spacing live in `UI-UX.md`.
>
> `Run All Checks` **PASS**. EditMode tests **31/31** pass. The phone pass is still open.

---

## 1. What it is

The battle log is a **history store plus one view**.

- `BattleLog` is the only writer. It keeps the last **200** lines of the session.
- `CombatLogUI` is a view only. It shows the newest **100** lines, recycles old labels, and never
  drops or merges a line.
- It stays visible while a story scene plays. The fight is frozen, so story lines never mix with
  damage lines.
- Hiding the battle page does not lose history. The view redraws from the store when it returns.

Two rules matter for every future change:

1. **One action = one entry.** No throttling, no `...N more`, no merged lines.
2. **The store owns the text. The view owns the picture.** Never parse a sentence to recover a
   speaker, a target or a colour.

---

## 2. Data model

### Entry

`BattleLogEntry` is a readonly struct:

| Field | Type | Meaning |
|---|---|---|
| `Sequence` | `long` | Monotonic id. Never resets on `Clear()`. |
| `Text` | `string` | The words only, or the whole combat line. Never includes the speaker. |
| `Kind` | `BattleLogKind` | Drives colour and special behaviour. |
| `Speaker` | `string` | Optional. Empty for combat and system lines. |

`ToString()` returns `Speaker: Text` when a speaker exists. That is for diagnostics and tests, not
for the view. The view builds its own rich-text string.

### Kinds

```
Damage, Critical, Incoming, EnemyKilled, HeroDied, Ability, EnemyAbility,
Heal, Guard, Gold, Event, Defeat, System, Dialogue
```

`Dialogue` means a story line. It is the only kind that has a speaker and the only kind that types.

### Write API

```csharp
BattleLog.Append(string text, BattleLogKind kind = Event, string speaker = null);
BattleLog.Say(string speaker, string line);
```

- `Append` is the raw path. Use it for combat, system and one-off lines.
- `Say` is the story path. It stores the speaker apart from the words and sets `Dialogue`.
- Empty text is ignored. No caller can write a blank line.
- A listener exception is caught and logged; one broken view cannot starve the next listener.

Examples:

```csharp
BattleLog.Append("Knight hits Slime for 9", BattleLogKind.Damage);
BattleLog.Say("Knight", "Weapons ready!");
```

Stored result:

| Sequence | Kind | Speaker | Text |
|---|---|---|---|
| 1 | `Damage` | empty | `Knight hits Slime for 9` |
| 2 | `Dialogue` | `Knight` | `Weapons ready!` |

### Store limits

- `BattleLog.Capacity = 200` entries.
- View window: `maxLines = 100`.
- The view pool starts at 40 labels and grows on demand up to the window size.

---

## 3. Lifecycle

| Moment | What happens |
|---|---|
| `BattleLog.Install()` | Detaches old listeners, clears history, resets sequence, subscribes game events. |
| `BattleLog.Clear()` | Clears the store and tells views. `Install` also writes `-- session resumed --`. |
| View `OnEnable` | Subscribes, clears labels, redraws every entry in the visible window. |
| New entry | `Append` adds to store, then calls every listener. |
| View `Append` | Rents a label, moves it to the end, paints it, starts typing if it is a story line. |
| View `LateUpdate` | Advances typing, rebuilds content height when needed, pins the newest line. |
| View `OnDisable` | Unsubscribes and stops typing. The store is untouched. |
| View trim | Oldest labels return to the pool. The store keeps its own 200. |

**Rebuild rule:** history redraws with `suppressTyping = true`. An old story line never replays its
typewriter effect after a page switch.

---

## 4. Visual rules

| Rule | Value |
|---|---|
| Role | `UiType.Role.Dialogue` for **every** line |
| Font | VT323 |
| Size | **48** |
| Base line height | **62.4** (`48 x 1.3`) |
| Line gap | **4** |
| Content padding | **12** |
| Speaker colour | `UiTheme.AccentLight`, gold `#F7CB6E` |
| Spoken words | `heroHitColor`, near-white `#ECF0F8` |
| Combat body | `ColorFor(kind)` |

Speaker format:

```
<color=#F7CB6E>Knight</color>: Weapons ready!
```

The speaker tag is built in `DisplayText`. `Text` never contains TMP markup. This keeps the store
plain, testable and safe to reuse.

### Colour map

`ColorFor(kind)` is the single colour table. It uses the serialized fields on `CombatLogUI`, so a
scene can tune colours without code:

| Kind | Field | Current look |
|---|---|---|
| `Damage`, default | `heroHitColor` | near-white |
| `Critical` | `criticalColor` | gold |
| `Incoming`, `Defeat` | `incomingColor` | red |
| `EnemyKilled`, `Gold` | `goldColor` | gold |
| `HeroDied` | `killColor` | green |
| `Ability`, `System` | `eventColor` | pale blue |
| `Dialogue` | `heroHitColor` | near-white body; gold name |
| `EnemyAbility` | `enemyAbilityColor` | orange |
| `Heal` | `healColor` | green |
| `Guard` | `guardColor` | blue |

The long-term suggestion is to reduce this set to neutral / danger / reward / story / system.
Do that as a separate visual pass, not while adding a kind.

### Layout rule

The content height is **measured**, not multiplied:

```
height = top padding + sum(each label's preferred height) + gaps + bottom padding
```

Each line's height is the larger of:

- the base 62.4, and
- TMP's preferred height at the current content width for the wrapped text.

This was the overflow fix on 30 Oct 2026. Do **not** replace it with `count * lineHeight`. A long
story line can be two or three rows. A fixed row height lets text spill out of the box.

The view pins itself to the bottom after the layout pass. A real drag pauses follow; a ScrollRect
clamp does not.

---

## 5. Typewriter

Story lines type; combat lines do not.

| Setting | Default | Where |
|---|---|---|
| `typewriter` | `true` | `CombatLogUI` Inspector |
| `typewriterCharsPerSecond` | `45` | `CombatLogUI` Inspector |

Behaviour:

1. The speaker name appears at once, followed by `: `.
2. The body reveals left to right in **real time** (`Time.unscaledDeltaTime`).
3. A new line finishes the previous line first.
4. Scene end, hard cap or skip calls `HudController.CompleteStoryText()` and shows the whole line.
5. Page rebuild shows full lines, not half-typed history.
6. While typing, the row height is remeasured only when the visible text needs another row.
7. The content height is marked dirty only when that height changes.

Relevant methods:

| Method | Job |
|---|---|
| `CombatLogUI.Append` | Starts or skips typing for one entry. |
| `CombatLogUI.AdvanceTyping` | Adds characters, updates the one label, marks height dirty only on a row change. |
| `CombatLogUI.CompleteTyping` | Shows the whole current line. Public. |
| `HudController.CompleteStoryText` | Called by `StoryPlayer.Finish`. |
| `StoryPlayer.Finish` | Completes the line before the closing fade. |

Typing state:

```
typingActive, typingLabel, typingEntry, typingChars, typingCarry
```

Only **one** line types at a time. `ClearLines`, `Return` and `OnDisable` stop typing safely.

Do not type the whole rich-text string blindly. Substringing `<color=...>` would break the tag. The
view keeps `Speaker` and `Text` apart and rebuilds the string each tick.


---

## 6. Add to the log

### Add a combat line

```csharp
BattleLog.Append("Knight hits Slime for 9", BattleLogKind.Damage);
```

### Add a story line

```csharp
BattleLog.Say("Knight", "Weapons ready!");
```

Never write `"Knight: Weapons ready!"` as the text. That hides the name from the colour system.

### Add a new kind

1. Append a value to `BattleLogKind` (append only; do not reorder).
2. Add its case to `CombatLogUI.ColorFor`.
3. Add a serialized colour field if the look must be tunable, then wire it in the scene builder.
4. Add a test or a manual check in `Manual-Tests.md`.

### Add a speaker

No new code. Pass the display name to `Say`. The UI colours it gold. If different speakers need
different colours, add the colour to the data source (for example `StoryActorDef.nameColor`) and
pass it through the entry. Do not hard-code names in the UI.

### Change the look

- Type role or size: `UiType.Role.Dialogue` in `Scripts/UI/UiType.cs`.
- Base row height: `UiTheme.LogLineHeight`.
- Gap/padding: `CombatLogUI` fields, builder and `Main.unity`.
- Speaker gold: `UiTheme.AccentLight` in `DisplayText`.
- Body colour: `heroHitColor` on the component.

After a builder change, rerun `Tools/Idle RPG/Build MVP Scene` **or** set the same serialized field
in `Main.unity`. Do not leave the builder and the scene disagreeing.

---

## 7. Tests and checks

### Automated

- `Run All Checks` → PASS on 31 Oct 2026.
- EditMode: **31/31**, including:
  - one entry per call, oldest-first order;
  - capped history;
  - clean reset;
  - a broken listener cannot block the next;
  - `Say` keeps `Speaker` and `Text` apart;
  - `Say` without a speaker writes the line alone.

### Manual

From `Manual-Tests.md`:

- Past 100 lines: oldest leaves, newest arrives, feed never freezes.
- No blank band under the newest line.
- Page switch keeps history and pin.
- Drag pauses; release resumes.
- One line per action.
- Long story line wraps inside the box.
- All log text is 48.
- Speaker names are gold; words are near-white.
- Story text types; a new line completes the previous one.
- Tap/skip completes the line before the fade.

### Performance notes

- Do not call `GetComponent` or `GetPreferredValues` for every line every frame.
- Typewriter only updates one label.
- `RefreshContentSize` runs when content is dirty, not every frame.
- The pool exists to avoid instantiate/destroy churn during long idle sessions.

---

## 8. File map

| File | Role |
|---|---|
| `Scripts/Core/BattleLog.cs` | Entry, kinds, store, `Append`, `Say`, events. |
| `Scripts/UI/CombatLogUI.cs` | View, colour map, speaker rich text, typewriter, follow. |
| `Scripts/UI/CombatLogUI.Pool.cs` | Rent/return/trim, content height, wrapped row height. |
| `Scripts/UI/UiType.cs` | `Dialogue` role (48 VT323). |
| `Scripts/UI/UiTheme.cs` | Palette, fonts, `LogLineHeight`. |
| `Scripts/UI/HudController.cs` | Owns the view; `CompleteStoryText`. |
| `Scripts/UI/Story/StoryPlayer.cs` | Calls `CompleteStoryText` when a scene ends. |
| `Editor/MvpSceneBuilder.Battle.cs` | Builds and wires the log. |
| `Scenes/Main.unity` | Live serialized values. |
| `Scripts/Tests/BattleLogTests.cs` | Store contract tests. |

---

## 9. Known gaps

- The phone aspect pass is open.
- The speaker gold is shared by all speakers. Per-speaker colours are not built.
- Combat attacker/target names are not separately coloured; they live inside the sentence.
- The colour count is still high. Reducing it is a separate UX pass.
- `-- session resumed --` still uses the bright event colour. Dimming it is a future polish item.
- Word bubbles in a scene are separate from the log; they do not use the log typewriter.
