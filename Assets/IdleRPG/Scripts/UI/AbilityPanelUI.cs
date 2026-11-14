using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Sim;

namespace IdleRPG.UI
{
    /// <summary>
    /// The battle page's ability strip, one cell per hero (Ability-Plan).
    ///
    /// ELI5: three buttons under the fight. Each one is that hero's special move: a picture, the move's name under
    /// it, and a thin line along the bottom edge that says the state. Press one and that hero uses the move.
    ///
    /// The cell's LOOK lives in <see cref="AbilityCellView.Build"/> - one owner for the layout. This class only
    /// drives the STATE: the picture and the state line. A press queues that hero's cast; everything shown comes
    /// from the fight itself, so the strip cannot drift from the sim.
    /// </summary>
    public sealed class AbilityPanelUI : MonoBehaviour
    {
        /// <summary>One hero's cell. Built in code by the panel (or by the scene builder, same method).</summary>
        [System.Serializable]
        public sealed class Row
        {
            public int heroIndex;
            public UiPlateButton plate;
            public Image abilityIcon;
            public TextMeshProUGUI abilityName;
            public Image stateRail;
            public Image stateFill;
            public Button castButton;

            /// <summary>
            /// The line length actually on screen, which eases toward the fight's real value instead of copying it.
            /// The fight moves its clock in 0.05 s steps, so a copied value arrives as small jumps; an eased one
            /// glides, and it still ends on the fight's own number.
            /// </summary>
            public float shownFill;
        }

        [SerializeField] private Row[] rows = new Row[0];

        /// <summary>
        /// How fast the drawn line eases toward the fight's value. The fight ticks its clock 20 times a second, so
        /// its number arrives in steps of about 1 px; 16 spreads each step over roughly 60 ms, which the eye reads
        /// as a glide. The value always converges on the fight's own number, so the line cannot drift.
        /// </summary>
        private const float FillEasePerSecond = 16f;

        private GameManager manager;

        /// <summary>True once each button has its press listener. Those listeners do not survive the editor.</summary>
        private bool wired;

        /// <summary>True once this panel has built its own cells, so a page toggle does not rebuild them.</summary>
        private bool cellsBuilt;

        /// <summary>Wires the panel from the scene builder, the same way the unit views are wired.</summary>
        public void ConfigureRuntime(Row[] rowViews)
        {
            rows = rowViews ?? new Row[0];
            wired = false;
            WireButtons();
        }

        /// <summary>
        /// Builds the strip's cells from the party, once. The scene still carries the old prefab cells, so they are
        /// cleared here: one layout survives, the one in <see cref="AbilityCellView.Build"/>.
        ///
        /// The party data is not readable on the first frame, so callers retry until it is.
        /// </summary>
        private void EnsureCells()
        {
            if (cellsBuilt)
            {
                return;
            }

            Bind();

            PartyConfig party = manager != null && manager.Context != null ? manager.Context.Party : null;
            if (party == null)
            {
                return;
            }

            cellsBuilt = true;

            HorizontalLayoutGroup strip = GetComponent<HorizontalLayoutGroup>();
            if (strip == null)
            {
                strip = gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            // Equal cells, one gap size: 1, 2 or 3 heroes can never drift apart.
            strip.spacing = UiSpace.Small;
            strip.padding = UiSpace.Padding(UiSpace.Small);
            strip.childAlignment = TextAnchor.MiddleCenter;
            strip.childControlWidth = true;
            strip.childControlHeight = true;
            strip.childForceExpandWidth = true;
            strip.childForceExpandHeight = true;
            strip.childScaleWidth = false;
            strip.childScaleHeight = false;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("Hero"))
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            int count = Mathf.Max(1, party.ValidHeroCount);
            rows = new Row[count];

            for (int i = 0; i < count; i++)
            {
                rows[i] = AbilityCellView.Build(transform, i).ToRow(i);
            }

            wired = false;
            WireButtons();
        }

        /// <summary>
        /// Attaches each button's press to its hero.
        ///
        /// This must happen at RUNTIME. A listener added by the scene builder at edit time is not serialized, so the
        /// buttons arrive in play mode with an empty onClick and a press does nothing at all.
        /// </summary>
        private void WireButtons()
        {
            if (wired)
            {
                return;
            }

            wired = true;

            for (int i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                if (row == null || row.castButton == null)
                {
                    continue;
                }

                int heroIndex = row.heroIndex;
                row.castButton.onClick.RemoveAllListeners();
                row.castButton.onClick.AddListener(() => Cast(heroIndex));
            }
        }

        /// <summary>
        /// The strip lives on the battle page, which is active at scene start, but the HUD's manager may not be
        /// assigned yet, so building and binding are deferred and retried every frame until they succeed.
        /// </summary>
        private void OnEnable()
        {
            Bind();
            EnsureCells();
            WireButtons();
        }

        private void Update()
        {
            Bind();

            if (!cellsBuilt)
            {
                EnsureCells();
                return;
            }

            Encounter encounter = ActiveEncounter();
            if (encounter == null || rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                RefreshRow(rows[i], encounter);
            }
        }

        /// <summary>
        /// Drives one cell. Each part has ONE owner: the picture shows the move (and dims while resting or dead),
        /// the line carries the state, and the plate greys only for a true lock. The cell face itself stays steady,
        /// so a cooling cell does not flash a different colour every frame.
        ///
        /// The line is the only clock: it is full green when the move is ready, drains to nothing at the moment the
        /// move is cast, and grows back as the wait runs out. Nothing has to be read.
        /// </summary>
        private void RefreshRow(Row row, Encounter encounter)
        {
            if (row == null)
            {
                return;
            }

            Combatant hero = Hero(encounter, row.heroIndex);
            HeroData card = HeroCard(row.heroIndex);
            bool alive = hero != null && hero.IsAlive;
            bool hasAbility = hero != null && hero.HasAbility;
            bool cooling = hasAbility && alive && hero.AbilityCooldownRemaining > 0d;
            bool queued = hasAbility && alive && hero.HasCastRequest;

            AbilityDef equipped = card != null && manager != null && manager.Abilities != null
                ? manager.Abilities.EquippedFor(card)
                : null;

            // A hero with nothing to cast, or a hero on the floor, shows NO picture, name or line at all: a greyed
            // plate already says "nothing here", and an empty rail would read as "nearly ready".
            bool showState = hasAbility && alive;

            if (row.abilityIcon != null)
            {
                Sprite icon = equipped != null ? equipped.Icon : null;

                // Only on a change. Re-assigning the same sprite every frame would rebuild the canvas every frame.
                if (row.abilityIcon.sprite != icon)
                {
                    row.abilityIcon.sprite = icon;
                }

                row.abilityIcon.enabled = icon != null;
                row.abilityIcon.color = cooling || !alive ? UiTheme.TextMuted : Color.white;
            }

            if (row.abilityName != null)
            {
                // Same words as the party page, so one move is never called two things.
                string label = equipped != null
                    ? equipped.DisplayName
                    : (hasAbility ? hero.AbilityName : string.Empty);

                // Only on a change. Writing the same string every frame would rebuild the canvas every frame.
                if (row.abilityName.text != label)
                {
                    row.abilityName.text = label;
                }

                // The name follows the picture: shown while the hero can cast, dimmed while the move rests.
                row.abilityName.enabled = showState;
                row.abilityName.color = cooling ? UiTheme.TextMuted : UiTheme.TextDim;
            }

            if (row.stateRail != null)
            {
                row.stateRail.enabled = showState;
            }

            if (row.stateFill != null)
            {
                row.stateFill.enabled = showState;

                if (showState)
                {
                    double total = hero != null ? hero.AbilityCooldownSec : 0d;
                    double left = hero != null ? hero.AbilityCooldownRemaining : 0d;
                    bool resting = cooling && total > 0d;

                    // The length IS the countdown: nothing at the cast, full when the move comes back.
                    float target = resting ? Mathf.Clamp01(1f - (float)(left / total)) : 1f;

                    // The fight ticks its clock 20 times a second, so its truth arrives in small steps. Ease the
                    // drawn value toward it instead of copying it: each step becomes a short glide, and the value
                    // still lands on the fight's own number, so the line can never lie about readiness.
                    float ease = 1f - Mathf.Exp(-FillEasePerSecond * Time.deltaTime);
                    row.shownFill = Mathf.Lerp(row.shownFill, target, ease);
                    SetBarFill(row.stateFill, row.shownFill);

                    // Amber while the press waits for its turn, gold while the move rests, green when it is ready.
                    row.stateFill.color = queued ? UiTheme.Queued : (resting ? UiTheme.GoldSoft : UiTheme.Ready);
                }
            }

            // The plate is the one owner of the cell face: steady colour, grayscale only when nothing can be cast.
            bool canCast = hasAbility && alive;

            if (row.plate != null)
            {
                row.plate.SetInteractable(canCast);
            }
            else if (row.castButton != null)
            {
                row.castButton.interactable = canCast;
            }
        }

        /// <summary>
        /// Draws <paramref name="amount"/> of the state line by moving the fill's right edge. Unity's
        /// Image.fillAmount only works when the image carries a sprite; these lines have none, so length is done with
        /// anchors instead - geometry always clips.
        /// </summary>
        private static void SetBarFill(Image bar, float amount)
        {
            RectTransform rect = bar.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Asks the fight to cast. The fight refuses when it cannot, so a press can never force one.</summary>
        private void Cast(int heroIndex)
        {
            Bind();

            if (manager == null || manager.Context == null || manager.Context.Combat == null)
            {
                return;
            }

            manager.Context.Combat.RequestAbility(heroIndex);
        }

        private void Bind()
        {
            if (manager == null && HudController.Instance != null)
            {
                manager = HudController.Instance.GameManager;
            }
        }

        private Encounter ActiveEncounter()
        {
            CombatManager combat = manager != null && manager.Context != null ? manager.Context.Combat : null;
            CombatSimulator simulator = combat != null ? combat.Simulator : null;
            return simulator != null ? simulator.CurrentEncounter : null;
        }

        private static Combatant Hero(Encounter encounter, int index)
        {
            Combatant[] party = encounter != null ? encounter.Party : null;
            return party != null && index >= 0 && index < party.Length ? party[index] : null;
        }

        private HeroData HeroCard(int index)
        {
            PartyConfig party = manager != null && manager.Context != null ? manager.Context.Party : null;
            return party != null ? party.GetHero(index) : null;
        }
    }
}
