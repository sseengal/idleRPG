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
    /// ELI5: three buttons under the fight. Each shows the hero's name, the special move that hero wears as a
    /// picture, and whether the move is ready, waiting or resting. Press one and that hero uses the move.
    ///
    /// The cell's LOOK lives in <see cref="AbilityCellView.Build"/> - one owner for the layout. This class only
    /// drives the STATE: the picture, the name, the state word and the cooldown line. A press queues that hero's
    /// cast; everything shown comes from the fight itself, so the strip cannot drift from the sim.
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
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI abilityLabel;
            public TextMeshProUGUI clockLabel;
            public Image clockTrack;
            public Image clockFill;
            public Button castButton;
        }

        [SerializeField] private Row[] rows = new Row[0];
        [SerializeField] private Color readyTint = UiTheme.Ready;
        [SerializeField] private Color coolingTint = UiTheme.Cooling;

        /// <summary>Amber state word while a press waits for the action lane. The wait is real, so it is shown.</summary>
        [SerializeField] private Color queuedTint = UiTheme.Queued;

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
        /// Drives one cell. Each band has ONE owner: the icon shows the move (and dims while resting), the state word
        /// says ready/queued/seconds, the line fills while resting, and the plate only greys for a true lock. The
        /// cell face itself stays steady, so a cooling cell does not flash a different colour every frame.
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

            if (row.nameLabel != null && card != null)
            {
                row.nameLabel.SetText(card.HeroName);
            }

            if (row.abilityLabel != null)
            {
                row.abilityLabel.SetText(hasAbility ? hero.AbilityName : "—");
            }

            AbilityDef equipped = card != null && manager != null && manager.Abilities != null
                ? manager.Abilities.EquippedFor(card)
                : null;

            if (row.abilityIcon != null)
            {
                Sprite icon = equipped != null ? equipped.Icon : null;
                row.abilityIcon.sprite = icon;
                row.abilityIcon.enabled = icon != null;
                row.abilityIcon.color = cooling ? UiTheme.TextMuted : Color.white;
            }

            if (row.clockLabel != null)
            {
                row.clockLabel.SetText(!hasAbility
                    ? "—"
                    : hero.IsCoverGuardActive
                        ? "GUARD " + hero.CoverGuardHitsRemaining
                        : queued
                            ? "QUEUED"
                            : cooling ? Mathf.CeilToInt((float)hero.AbilityCooldownRemaining) + "s" : "READY");
                row.clockLabel.color = queued
                    ? queuedTint
                    : !hasAbility
                        ? UiTheme.InkDisabled
                        : cooling ? UiTheme.TextDim : readyTint;
            }

            if (row.clockFill != null && row.clockTrack != null)
            {
                double total = hero != null ? hero.AbilityCooldownSec : 0d;
                double left = hero != null ? hero.AbilityCooldownRemaining : 0d;
                bool showBar = cooling && total > 0d;

                row.clockTrack.enabled = showBar;
                row.clockFill.enabled = showBar;

                if (showBar)
                {
                    row.clockFill.fillAmount = Mathf.Clamp01(1f - (float)(left / total));
                    row.clockFill.color = coolingTint;
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
