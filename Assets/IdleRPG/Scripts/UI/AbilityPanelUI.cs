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
    /// The ABILITIES tab of the battle page (Ability-Plan).
    ///
    /// ELI5: one column per hero - its picture, its name, its health, the ability it is wearing, a clock that says
    /// when it can be cast again, and the button that casts it.
    ///
    /// An ability does NOTHING yet on purpose: pressing the button plays that hero's own animation and writes a line
    /// in the battle log, so the plumbing can be watched before it is made meaningful. Everything a column shows comes
    /// from the fight itself (whose clock pauses with the game), so the panel cannot drift from the sim.
    /// </summary>
    public sealed class AbilityPanelUI : MonoBehaviour
    {
        /// <summary>One hero's column. Built by the scene builder and wired once.</summary>
        [System.Serializable]
        public sealed class Row
        {
            public int heroIndex;
            public Image portrait;
            public TextMeshProUGUI nameLabel;
            public HpBarView health;
            public TextMeshProUGUI abilityLabel;
            public TextMeshProUGUI clockLabel;
            public Image clockFill;
            public Button castButton;
        }

        [SerializeField] private Row[] rows = new Row[0];
        [SerializeField] private Color readyTint = new Color(0.30f, 0.68f, 0.42f, 1f);
        [SerializeField] private Color coolingTint = new Color(0.30f, 0.33f, 0.40f, 1f);

        private GameManager manager;

        /// <summary>Wires the panel from the scene builder, the same way the unit views are wired.</summary>
        public void ConfigureRuntime(Row[] rowViews)
        {
            rows = rowViews ?? new Row[0];

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
        /// The panel sits on a tab that starts hidden, so binding happens when it is first shown: the HUD may not have
        /// a manager at scene start.
        /// </summary>
        private void OnEnable()
        {
            Bind();
            RefreshPortraits();
        }

        private void Update()
        {
            Bind();

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

            if (row.nameLabel != null && card != null)
            {
                row.nameLabel.SetText(card.HeroName);
            }

            if (row.health != null)
            {
                row.health.SetVisible(hero != null);

                if (hero != null)
                {
                    row.health.SetFill((float)hero.HealthPercent);
                    row.health.SetValueLabel(hero.CurrentHealth, hero.MaxHealth);
                }
            }

            if (row.abilityLabel != null)
            {
                row.abilityLabel.SetText(hasAbility ? hero.AbilityName : "No ability");
            }

            if (row.clockLabel != null)
            {
                row.clockLabel.SetText(!hasAbility
                    ? "-"
                    : cooling ? Mathf.CeilToInt((float)hero.AbilityCooldownRemaining) + "s" : "READY");
            }

            if (row.clockFill != null)
            {
                double total = hero != null ? hero.AbilityCooldownSec : 0d;
                double left = hero != null ? hero.AbilityCooldownRemaining : 0d;

                row.clockFill.fillAmount = hasAbility && total > 0d
                    ? Mathf.Clamp01(1f - (float)(left / total))
                    : 0f;
                row.clockFill.color = cooling ? coolingTint : readyTint;
            }

            if (row.castButton != null)
            {
                row.castButton.gameObject.SetActive(hasAbility);
                row.castButton.interactable = hasAbility && alive && !cooling;
            }
        }

        /// <summary>Points each column's picture at the art it shows, once, when the tab is first opened.</summary>
        private void RefreshPortraits()
        {
            if (rows == null)
            {
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                if (row == null || row.portrait == null)
                {
                    continue;
                }

                HeroData card = HeroCard(row.heroIndex);
                if (card == null)
                {
                    continue;
                }

                row.portrait.sprite = card.ArtSet != null && card.ArtSet.Poster != null
                    ? card.ArtSet.Poster
                    : card.HeroIcon;
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
