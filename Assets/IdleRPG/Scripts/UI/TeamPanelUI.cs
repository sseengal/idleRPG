using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.UI
{
    /// <summary>
    /// The TEAM tab: the party board at full size, the roster with roles, tap-to-swap and two one-tap presets.
    ///
    /// ELI5: the changing room. The board is the same one the battle screen uses (so what you see is what fights),
    /// the list tells you who each hero is, and the two buttons are "everyone normal" and "hide the soft one
    /// behind the tank" - which is also the fastest way to prove that rows actually matter.
    /// </summary>
    public sealed class TeamPanelUI : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Where the board is built. Empty means \"this object\".")]
        [SerializeField] private RectTransform boardRoot;

        private GameManager manager;
        private FormationStripUI board;
        private TextMeshProUGUI rosterLabel;
        private TextMeshProUGUI statusLabel;

        private void Start()
        {
            Build();
        }

        private void OnEnable()
        {
            // Opening the tab re-reads the party: hero levels may have changed while it was closed.
            if (board != null)
            {
                board.RefreshAll();
                RefreshText();
            }
        }

        private void OnDestroy()
        {
            if (manager != null && manager.Formation != null)
            {
                manager.Formation.Changed -= RefreshText;
            }
        }

        /// <summary>Builds the panel in code (placeholder layout; Step 19 restyles it).</summary>
        private void Build()
        {
            HudController hud = HudController.Instance;
            manager = hud != null ? hud.GameManager : null;

            if (manager == null || manager.Formation == null)
            {
                Debug.LogWarning("[TeamPanelUI] GameManager not ready yet; will bind on next open.");
                return;
            }

            RectTransform root = boardRoot != null ? boardRoot : (RectTransform)transform;

            GameObject boardObject = UiRuntime.CreateNode("FormationBoard", root);
            RectTransform boardRect = boardObject.GetComponent<RectTransform>();
            boardRect.anchorMin = new Vector2(0f, 1f);
            boardRect.anchorMax = new Vector2(0f, 1f);
            boardRect.pivot = new Vector2(0f, 1f);
            boardRect.anchoredPosition = new Vector2(14f, -70f);

            board = boardObject.AddComponent<FormationStripUI>();

            // Team board flavour: roles instead of health bars, and no damage pool (only the battle strip owns the
            // floating numbers, otherwise this board would hijack their anchors on every formation change).
            board.SetDisplayOptions(health: false, roles: true);
            board.Build(manager, null);

            rosterLabel = UiRuntime.CreateText(root, "Roster", string.Empty, 19f,
                TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.9f));
            UiRuntime.Anchor(rosterLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0.60f, 0.30f), 16f, 92f, 0f, 0f);

            UiRuntime.CreateButton(root, "AutoArrange", "TANK FRONT", new Vector2(0.63f, 0.17f), new Vector2(0.98f, 0.29f), OnAutoArrange);
            UiRuntime.CreateButton(root, "TankBack", "HIDE THE TANK (BACK ROW)", new Vector2(0.63f, 0.03f), new Vector2(0.98f, 0.15f), OnTankBack);

            statusLabel = UiRuntime.CreateText(root, "Status", string.Empty, 17f,
                TextAlignmentOptions.MidlineLeft, new Color(1f, 0.92f, 0.7f));
            UiRuntime.Anchor(statusLabel.rectTransform, new Vector2(0f, 0.88f), new Vector2(1f, 0.99f), 16f, 0f, 12f, 0f);

            manager.Formation.Changed += RefreshText;
            RefreshText();
        }

        private void OnAutoArrange()
        {
            ApplyPreset(tankFirst: true, "Heaviest heroes to the front row");
        }

        private void OnTankBack()
        {
            ApplyPreset(tankFirst: false, "Heaviest hero moved to the back row");
        }

        private void ApplyPreset(bool tankFirst, string message)
        {
            if (manager == null || manager.Formation == null)
            {
                return;
            }

            int heroes = manager.Party != null ? manager.Party.ValidHeroCount : 0;

            if (tankFirst)
            {
                manager.Formation.AutoArrange(heroes, WeightOf);
            }
            else
            {
                manager.Formation.ArrangeTankInBack(heroes, WeightOf);
            }

            board?.RefreshAll();
            RefreshText();
            GameEvents.RaiseToast(message);
        }

        /// <summary>How much punishment a hero can take - the sorting key for both presets.</summary>
        private double WeightOf(int heroIndex)
        {
            HeroData hero = manager != null && manager.Party != null ? manager.Party.GetHero(heroIndex) : null;

            if (hero == null)
            {
                return 0d;
            }

            var resolver = manager.Resolver;

            // Effective HP is the honest "who should stand in front" number: health scaled by defence.
            double health = resolver != null ? resolver.GetMaxHealth(hero, heroIndex) : hero.BaseHealth;
            double defense = resolver != null ? resolver.GetDefense(hero, heroIndex) : hero.BaseDefense;
            return health * (1d + defense / 100d);
        }

        private void RefreshText()
        {
            if (manager == null)
            {
                return;
            }

            if (rosterLabel != null)
            {
                rosterLabel.SetText(BuildRoster());
            }

            if (statusLabel != null && manager.Formation != null)
            {
                int heroes = manager.Party != null ? manager.Party.ValidHeroCount : 0;
                statusLabel.SetText($"board {manager.Formation.Describe()}  heroes {heroes}  " +
                                    "(tap a hero, then a slot)");
            }
        }

        private string BuildRoster()
        {
            PartyConfig party = manager.Party;

            if (party == null)
            {
                return "No party assigned.";
            }

            var resolver = manager.Resolver;
            System.Text.StringBuilder builder = new System.Text.StringBuilder(240);

            for (int i = 0; i < party.Heroes.Count; i++)
            {
                HeroData hero = party.GetHero(i);

                if (hero == null)
                {
                    continue;
                }

                int slot = manager.Formation != null ? manager.Formation.SlotOfHero(i) : -1;
                string position = slot >= 0 ? "slot " + slot : "benched";

                double health = resolver != null ? resolver.GetMaxHealth(hero, i) : hero.BaseHealth;
                double attack = resolver != null ? resolver.GetAttack(hero, i) : hero.BaseAttack;

                builder.AppendLine($"{i}: {hero.HeroName}  [{hero.Role}]  {position}  hp {health:0}  atk {attack:0}");
            }

            return builder.ToString().TrimEnd();
        }
    }
}
