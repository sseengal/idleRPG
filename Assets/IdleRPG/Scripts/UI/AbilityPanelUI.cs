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
    /// ELI5: three buttons under the fight. Each one is that hero's special move: a big picture on the left, and on
    /// the right the hero's name, the hero's health numbers and health bar. Press one and that hero uses the move.
    ///
    /// The cell's LOOK lives in <see cref="AbilityCellView.Build"/> - one owner for the layout. This class only
    /// drives the STATE: the picture, the health bar and the words. A press queues that hero's cast; everything
    /// shown comes from the fight itself, so the strip cannot drift from the sim.
    /// </summary>
    public sealed class AbilityPanelUI : MonoBehaviour
    {
        /// <summary>One hero's cell. Built in code by the panel (or by the scene builder, same method).</summary>
        [System.Serializable]
        public sealed class Row
        {
            public int heroIndex;

            /// <summary>The press owner: transparent face, so it squashes on touch and locks without drawing a box.</summary>
            public UiPlateButton plate;

            /// <summary>The grey base copy of the picture. Always drawn; the colour copy covers it as it opens.</summary>
            public Image abilityIcon;

            /// <summary>The full-colour copy of the same art, revealed from the left by <see cref="abilityIconWipe"/>.</summary>
            public Image abilityIconColour;

            /// <summary>The curtain: width IS the countdown, so the picture fills with colour left to right.</summary>
            public RectTransform abilityIconWipe;

            /// <summary>The hero's own name. Identity, not state: it stays bright through a cooldown.</summary>
            public TextMeshProUGUI heroName;

            /// <summary>The hero's health, numbers included: the same widget the arena units wear.</summary>
            public HpBarView hpBar;

            public Button castButton;

            /// <summary>
            /// The countdown length actually on screen, eased so the colour creeping back over the picture is a glide
            /// rather than twenty small jumps a second. A cast takes it to zero at once - see <see cref="DrawCooldown"/>.
            /// </summary>
            public float shownFill;
        }

        [SerializeField] private Row[] rows = new Row[0];

        /// <summary>
        /// How fast the picture's colour curtain eases back over the art as the move comes back. A cooldown tick
        /// arrives as one step, so copying it would snap; 16 spreads each step over roughly 60 ms, which the eye reads
        /// as a sweep. The value always converges on the fight's own number, so the picture cannot lie about readiness.
        /// (The health bar in each cell owns its own ease - see <see cref="HpBarView"/>.)
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
            strip.spacing = UiSpace.Space8;
            strip.padding = UiSpace.Padding(UiSpace.Space8);
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
        /// Drives one cell. Each part has ONE owner: the picture shows the move (and dims while resting or dead), the
        /// bar and its numbers show the hero's life, and the name says whose button it is. The plate greys only when
        /// nothing can be cast, so a cooling cell cannot flash a different colour every frame.
        ///
        /// The picture is the only clock: it is whole and bright when the move is ready, turns grey in the same frame
        /// the move is cast, and fills back in as the wait runs out.
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

            // The press is in the lane, waiting for its turn. Amber says "waiting": it cannot be confused with gold
            // (value), green (ready) or the grey of a resting move.
            bool queued = hasAbility && alive && hero.HasCastRequest;

            AbilityDef equipped = card != null && manager != null && manager.Abilities != null
                ? manager.Abilities.EquippedFor(card)
                : null;

            // A hero with nothing to cast, or a hero on the floor, shows NO picture, words or bar at all: a bare
            // cell already says "nothing here", and an empty bar would read as "nearly dead".
            bool showState = hasAbility && alive;

            if (row.abilityIcon != null)
            {
                Sprite icon = equipped != null ? equipped.Icon : null;

                // Only on a change. Re-assigning the same sprite every frame would rebuild the canvas every frame.
                if (row.abilityIcon.sprite != icon)
                {
                    row.abilityIcon.sprite = icon;

                    if (row.abilityIconColour != null)
                    {
                        row.abilityIconColour.sprite = icon;
                    }
                }

                // The base keeps the old rule: dim while the move rests, dimmer still when the hero is down. It reads
                // grey in every state, because its UIEffect drained it at build. While the press waits for its turn
                // it turns amber instead: the queue is a state of the MOVE, and the art is where the player looks.
                row.abilityIcon.enabled = icon != null;
                row.abilityIcon.color = queued
                    ? UiTheme.StatusWarn
                    : (cooling || !alive ? UiTheme.TextMuted : Color.white);

                // The colour copy takes the same amber, so a queued move cannot end up half-tinted: while queued the
                // curtain is still open, so this is the copy the player actually sees.
                if (row.abilityIconColour != null)
                {
                    row.abilityIconColour.enabled = icon != null && showState;
                    row.abilityIconColour.color = queued ? UiTheme.StatusWarn : Color.white;
                }
            }

            if (row.heroName != null)
            {
                // Whose card this is: the hero's own name, the same one the party page shows. Identity, not state, so
                // it neither dims on a cooldown nor waits for the move to be ready - the player must be able to tell
                // three cards apart at any moment. It goes away only with the rest of the card, when the hero is down.
                string owner = hero != null && !string.IsNullOrEmpty(hero.DisplayName)
                    ? hero.DisplayName
                    : (card != null ? card.HeroName : string.Empty);

                // Only on a change. Writing the same string every frame would rebuild the canvas every frame.
                if (row.heroName.text != owner)
                {
                    row.heroName.text = owner;
                }

                row.heroName.enabled = alive && !string.IsNullOrEmpty(owner);
            }

            if (row.hpBar != null)
            {
                row.hpBar.SetVisible(showState);
            }

            if (showState)
            {
                double total = hero.AbilityCooldownSec;
                double left = hero.AbilityCooldownRemaining;
                bool resting = cooling && total > 0d;

                // The length IS the countdown: nothing at the cast, whole when the move comes back.
                float target = resting ? Mathf.Clamp01(1f - (float)(left / total)) : 1f;

                // A CAST takes the colour away in the same frame: snapping down is what makes the art read grey at
                // once. Only the way BACK is eased, so the colour sweeps in as the wait runs out. Copying the target
                // both ways made the cast look like the colour sliding off the picture over a third of a second.
                row.shownFill = target < row.shownFill
                    ? target
                    : Mathf.Lerp(row.shownFill, target, 1f - Mathf.Exp(-FillEasePerSecond * Time.deltaTime));

                DrawCooldown(row);

                // Health goes to the shared bar widget, which eases its own fill and guards its own label.
                row.hpBar.SetFill((float)hero.HealthPercent);
                row.hpBar.SetValueLabel(hero.CurrentHealth, hero.MaxHealth);
            }

            // The transparent face is the one owner of the press: it squashes on touch, and only its lock changes.
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
        /// Draws the countdown onto the picture: the colour copy is revealed from the left as the move comes back, so
        /// the art itself is the clock. A cast drops the drawn value to zero in the SAME frame - snapping down is what
        /// makes the art read grey at once - and only the climb back is eased, so the colour sweeps in as the wait
        /// runs out. (Easing both ways made a cast look like the colour sliding off the picture.)
        /// </summary>
        private static void DrawCooldown(Row row)
        {
            if (row.abilityIconWipe != null)
            {
                // The curtain opens from the picture's LEFT edge, so the colour copy is cropped, never squashed.
                SetWidthFraction(row.abilityIconWipe, row.shownFill);
            }
        }

        /// <summary>
        /// Draws <paramref name="amount"/> of a rect by moving its right edge, from the left edge. Anchors, not
        /// <see cref="Image.fillAmount"/>: a Filled image with no sprite ignores fillAmount and draws the whole rect.
        /// The picture's curtain uses the same trick for the opposite reason: its child DOES carry a sprite, and a
        /// mask the width of the countdown crops that art rather than stretching it.
        /// </summary>
        private static void SetWidthFraction(RectTransform rect, float amount)
        {
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
