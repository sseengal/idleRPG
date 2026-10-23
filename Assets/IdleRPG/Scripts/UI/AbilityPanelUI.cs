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
    /// The battle page's ability strip, one button per hero (Ability-Plan).
    ///
    /// ELI5: three buttons under the fight. Each one shows a hero's face, the special move that hero is wearing,
    /// whether it is ready or still resting, and pressing it makes that hero use that move.
    ///
    /// A press queues that hero's cast. The cast is announced when the action lane is free - one actor, one target,
    /// one travel window - and the button says so: "QUEUED" (amber) while it waits, then the cooldown clock. Everything
    /// a button shows comes from the fight itself (whose clock pauses with the game), so the strip cannot drift from
    /// the sim.
    /// </summary>
    public sealed class AbilityPanelUI : MonoBehaviour
    {
        /// <summary>One hero's button. Built by the scene builder and wired once. The whole cell is the button.</summary>
        [System.Serializable]
        public sealed class Row
        {
            public int heroIndex;
            public Image portrait;
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI abilityLabel;
            public TextMeshProUGUI clockLabel;
            public Image clockFill;
            public Button castButton;
        }

        [SerializeField] private Row[] rows = new Row[0];
        [SerializeField] private Color readyTint = new Color(0.30f, 0.68f, 0.42f, 1f);
        [SerializeField] private Color coolingTint = new Color(0.30f, 0.33f, 0.40f, 1f);

        /// <summary>Amber wash while a press waits for the action lane. The wait is real, so it is shown.</summary>
        [SerializeField] private Color queuedTint = new Color(0.72f, 0.62f, 0.28f, 1f);

        /// <summary>Grey wash for a hero with nothing to cast. Nothing is hidden, so the row keeps its width.</summary>
        [SerializeField] private Color lockedTint = new Color(0.22f, 0.24f, 0.29f, 1f);

        private GameManager manager;

        /// <summary>True once the pictures have a sprite. The party data is not readable on the first enable.</summary>
        private bool portraitsBound;

        /// <summary>True once each button has its press listener. Those listeners do not survive the editor.</summary>
        private bool wired;

        /// <summary>Wires the panel from the scene builder, the same way the unit views are wired.</summary>
        public void ConfigureRuntime(Row[] rowViews)
        {
            rows = rowViews ?? new Row[0];
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
        /// assigned yet, so binding is deferred to first enable and re-checked every frame.
        /// </summary>
        private void OnEnable()
        {
            Bind();
            WireButtons();
            portraitsBound = RefreshPortraits();
        }

        private void Update()
        {
            Bind();

            // A frame or two after scene start the HUD finally hands over the manager. Until then there is no card
            // data to read, so the pictures stay empty - keep asking instead of giving up on the first enable.
            if (!portraitsBound)
            {
                portraitsBound = RefreshPortraits();
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
                row.abilityLabel.SetText(hasAbility ? hero.AbilityName : "No ability");
            }

            if (row.clockLabel != null)
            {
                row.clockLabel.SetText(!hasAbility
                    ? "LOCKED"
                    : queued
                        ? "QUEUED"
                        : cooling ? Mathf.CeilToInt((float)hero.AbilityCooldownRemaining) + "s" : "READY");
                row.clockLabel.color = queued ? queuedTint : hasAbility && !cooling && alive ? readyTint : coolingTint;
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
                row.castButton.interactable = hasAbility && alive && !cooling && !queued;

                // The cell never disappears: a missing button would make the strip jump and leave a dead gap.
                Image face = row.castButton.targetGraphic as Image;
                if (face != null)
                {
                    face.color = hasAbility ? (queued ? queuedTint : Color.white) : lockedTint;
                }
            }
        }

        /// <summary>
        /// Points each button's picture at the art it shows. Returns false when the party data is not readable yet,
        /// so the caller can try again on the next frame.
        /// </summary>
        private bool RefreshPortraits()
        {
            if (rows == null || HeroCard(0) == null)
            {
                return false;
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

            return true;
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
