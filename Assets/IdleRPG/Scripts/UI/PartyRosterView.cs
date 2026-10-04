using TMPro;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.Combat;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.Equipment;
using IdleRPG.Utils;

namespace IdleRPG.UI
{
    /// <summary>
    /// The ROSTER tab of the Party page: hero cards (idle clips), the selected hero large with its stats,
    /// and the three equipped gear slots (display only). Selecting a card refreshes the big portrait =
    /// the bound art only changes when the SELECTED hero changes, so taps never resize it. Everything is
    /// read-only: equipping/discarding lives on the INVENTORY tab (InventoryTabUI).
    /// Extracted from PartyPanelUI so the page shell and the roster stay small and testable.
    /// </summary>
    public sealed class PartyRosterView : MonoBehaviour
    {
        [SerializeField] private int abilitySlotPlaceholders = 3;

        private readonly Color cardColor = new Color(0.16f, 0.19f, 0.28f, 0.95f);
        private readonly Color cardSelectedColor = new Color(0.30f, 0.42f, 0.62f, 1f);
        private readonly Color slotColor = new Color(0.14f, 0.16f, 0.23f, 0.95f);
        private readonly Color bodyText = new Color(1f, 1f, 1f, 0.92f);
        private readonly Color dimText = new Color(1f, 1f, 1f, 0.55f);
        private readonly Color bonusColor = new Color(0.55f, 0.9f, 0.6f, 1f);
        private readonly Color accentColor = new Color(1f, 0.82f, 0.3f, 1f);

        private const int StatRowCount = 4;
        private static readonly string[] StatKeyNames = { "HP", "ATK", "DEF", "DPS" };
        private const int GearSlotCount = 3;

        private GameManager manager;

        private Image[] cardBackgrounds;
        private Image[] cardIcons;
        private CharacterAnimator[] cardAnimators;
        private TextMeshProUGUI[] cardNameLabels;

        private Image bigPortrait;
        private CharacterAnimator bigAnimator;
        private TextMeshProUGUI bigNameLabel;
        private TextMeshProUGUI roleLabel;
        private TextMeshProUGUI[] statKeys;
        private TextMeshProUGUI[] statValues;
        private TextMeshProUGUI[] statBonusLabels;

        private Image[] gearSlotBackgrounds;
        private TextMeshProUGUI[] gearSlotStatLabels;

        private int selectedHeroIndex = -1;
        private int boundBigHero = -2;   // -2 = never bound, -1 = bound to "nothing"

        /// <summary>Kicks off again (slot-change refresh etc.) after a level-up or gear edit.</summary>
        public int SelectedHeroIndex
        {
            get { return selectedHeroIndex; }
        }

        public void Build(GameManager gameManager)
        {
            manager = gameManager;
            RectTransform root = (RectTransform)transform;

            BuildHeroCards(root);
            BuildGearRow(root);
            PartyPanelUI.BuildSlotRow(root, "ABILITIES", abilitySlotPlaceholders, 0.175f, 0.255f);

            if (selectedHeroIndex < 0)
            {
                selectedHeroIndex = FirstHeroIndex();
            }

            RefreshAll();
        }

        private int FirstHeroIndex()
        {
            for (int i = 0; i < manager.Party.Heroes.Count; i++)
            {
                if (manager.Party.GetHero(i) != null)
                {
                    return i;
                }
            }

            return -1;
        }

        public void RefreshAll()
        {
            if (manager == null || manager.Party == null)
            {
                return;
            }

            RefreshCards();
            RefreshBigPortrait();
            RefreshStats();
            RefreshGear();
        }

        private void SelectHero(int heroIndex)
        {
            if (heroIndex < 0 || heroIndex == selectedHeroIndex)
            {
                return;
            }

            selectedHeroIndex = heroIndex;
            RefreshAll();
        }

        // ------------------------------------------------------------------
        // Cards + big portrait + stats
        // ------------------------------------------------------------------
        private void BuildHeroCards(RectTransform root)
        {
            int heroes = manager.Party.Heroes.Count;
            cardBackgrounds = new Image[heroes];
            cardIcons = new Image[heroes];
            cardAnimators = new CharacterAnimator[heroes];
            cardNameLabels = new TextMeshProUGUI[heroes];

            const float cardIconPx = 78f;

            for (int i = 0; i < heroes; i++)
            {
                float xMin = 0.03f + i * 0.325f;
                float xMax = xMin + 0.30f;
                int captured = i;

                Button card = UiRuntime.CreateButton(root, "Card" + i, string.Empty,
                    new Vector2(xMin, 0.79f), new Vector2(xMax, 0.99f), () => SelectHero(captured), cardColor);
                cardBackgrounds[i] = card != null ? card.GetComponent<Image>() : null;
                Transform cardRoot = card != null ? card.transform : root;

                Image icon = UiRuntime.CreateIcon("Portrait", cardRoot);
                UiRuntime.CenterOn(icon.rectTransform, new Vector2(0.5f, 0.63f), new Vector2(cardIconPx, cardIconPx));
                icon.preserveAspect = true;

                CharacterAnimator animator = icon.gameObject.AddComponent<CharacterAnimator>();
                animator.SetSlotSize(new Vector2(cardIconPx, cardIconPx));
                animator.FacingLeft = false;
                cardIcons[i] = icon;
                cardAnimators[i] = animator;

                TextMeshProUGUI name = UiRuntime.CreateText(cardRoot, "Name", string.Empty, 16f,
                    TextAlignmentOptions.Center, Color.white);
                UiRuntime.Anchor(name.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.32f), 2f, 0f, 2f, 0f);
                cardNameLabels[i] = name;
            }

            bigPortrait = UiRuntime.CreateIcon("BigPortrait", root);
            UiRuntime.CenterOn(bigPortrait.rectTransform, new Vector2(0.24f, 0.52f), new Vector2(300f, 340f));
            bigPortrait.preserveAspect = true;
            bigAnimator = bigPortrait.gameObject.AddComponent<CharacterAnimator>();
            bigAnimator.SetSlotSize(new Vector2(300f, 340f));
            bigAnimator.FacingLeft = false;

            bigNameLabel = UiRuntime.CreateText(root, "BigName", string.Empty, 22f,
                TextAlignmentOptions.Center, Color.white);
            UiRuntime.Anchor(bigNameLabel.rectTransform, new Vector2(0.02f, 0.28f), new Vector2(0.46f, 0.36f), 6f, 0f, 6f, 0f);

            Image statsPanel = UiRuntime.CreatePanel(root, null, cardColor);
            UiRuntime.Anchor(statsPanel.rectTransform, new Vector2(0.48f, 0.44f), new Vector2(0.97f, 0.795f));

            roleLabel = UiRuntime.CreateText(root, "Role", string.Empty, 15f,
                TextAlignmentOptions.MidlineLeft, accentColor);
            UiRuntime.Anchor(roleLabel.rectTransform, new Vector2(0.50f, 0.745f), new Vector2(0.95f, 0.783f), 2f, 0f, 0f, 0f);

            statKeys = new TextMeshProUGUI[StatRowCount];
            statValues = new TextMeshProUGUI[StatRowCount];
            statBonusLabels = new TextMeshProUGUI[StatRowCount];

            for (int i = 0; i < StatRowCount; i++)
            {
                float yTop = 0.68f - i * 0.055f;

                statKeys[i] = UiRuntime.CreateText(root, "Key" + StatKeyNames[i], StatKeyNames[i], 17f,
                    TextAlignmentOptions.MidlineLeft, dimText);
                UiRuntime.Anchor(statKeys[i].rectTransform, new Vector2(0.50f, yTop), new Vector2(0.62f, yTop + 0.045f), 0f, 0f, 0f, 0f);

                statValues[i] = UiRuntime.CreateText(root, "Val" + StatKeyNames[i], string.Empty, 17f,
                    TextAlignmentOptions.MidlineRight, bodyText);
                UiRuntime.Anchor(statValues[i].rectTransform, new Vector2(0.62f, yTop), new Vector2(0.79f, yTop + 0.045f), 0f, 0f, 0f, 0f);

                statBonusLabels[i] = UiRuntime.CreateText(root, "Bonus" + StatKeyNames[i], string.Empty, 13f,
                    TextAlignmentOptions.MidlineRight, bonusColor);
                UiRuntime.Anchor(statBonusLabels[i].rectTransform, new Vector2(0.79f, yTop), new Vector2(0.95f, yTop + 0.045f), 0f, 0f, 0f, 0f);
            }
        }

        // ------------------------------------------------------------------
        // Equipped gear row (view only)
        // ------------------------------------------------------------------
        private void BuildGearRow(RectTransform root)
        {
            TextMeshProUGUI caption = UiRuntime.CreateText(root, "GearCaption", "GEAR (equipped)", 15f,
                TextAlignmentOptions.MidlineLeft, dimText);
            UiRuntime.Anchor(caption.rectTransform, new Vector2(0.48f, 0.415f), new Vector2(0.72f, 0.45f), 4f, 0f, 0f, 0f);

            TextMeshProUGUI hint = UiRuntime.CreateText(root, "GearHint", "tap a slot to manage", 13f,
                TextAlignmentOptions.MidlineRight, dimText);
            UiRuntime.Anchor(hint.rectTransform, new Vector2(0.62f, 0.415f), new Vector2(0.97f, 0.45f), 0f, 0f, 4f, 0f);

            const float xMin = 0.48f;
            const float xMax = 0.97f;
            const float gap = 0.012f;
            float width = (xMax - xMin - gap * (GearSlotCount - 1)) / GearSlotCount;

            gearSlotBackgrounds = new Image[GearSlotCount];
            gearSlotStatLabels = new TextMeshProUGUI[GearSlotCount];

            for (int i = 0; i < GearSlotCount; i++)
            {
                float x0 = xMin + i * (width + gap);
                Image slot = UiRuntime.CreatePanel(root, null, slotColor);
                UiRuntime.Anchor(slot.rectTransform, new Vector2(x0, 0.335f), new Vector2(x0 + width, 0.412f));
                gearSlotBackgrounds[i] = slot;

                TextMeshProUGUI letter = UiRuntime.CreateText(slot.transform, "Letter", ItemVisuals.SlotLetter((ItemSlotType)i), 18f,
                    TextAlignmentOptions.Center, Color.white);
                UiRuntime.Anchor(letter.rectTransform, new Vector2(0.02f, 0.38f), new Vector2(0.98f, 0.95f), 0f, 0f, 0f, 0f);

                TextMeshProUGUI stat = UiRuntime.CreateText(slot.transform, "Stat", "empty", 12f,
                    TextAlignmentOptions.Center, dimText);
                UiRuntime.Anchor(stat.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.30f), 0f, 0f, 0f, 0f);
                gearSlotStatLabels[i] = stat;
            }
        }

        /// <summary>Refreshes the three equipped-slot tiles for the selected hero.</summary>
        private void RefreshGear()
        {
            if (manager == null || manager.Gear == null || gearSlotBackgrounds == null)
            {
                return;
            }

            for (int i = 0; i < GearSlotCount; i++)
            {
                ItemInstance item = selectedHeroIndex >= 0
                    ? manager.Gear.GetEquipped(selectedHeroIndex, (ItemSlotType)i)
                    : null;

                gearSlotBackgrounds[i].color = item != null ? ItemVisuals.RarityColor(item.Rarity) : slotColor;

                if (gearSlotStatLabels[i] != null)
                {
                    gearSlotStatLabels[i].color = item != null ? Color.white : dimText;
                    gearSlotStatLabels[i].SetText(item != null
                        ? "+" + item.PercentText + " " + item.SlotType.PrimaryStatLabel()
                        : "empty");
                }
            }
        }

        // ------------------------------------------------------------------
        // Refresh
        // ------------------------------------------------------------------
        /// <summary>Binds each card's idle clip once; later refreshes only touch colours and names.</summary>
        private void RefreshCards()
        {
            if (cardAnimators == null)
            {
                return;
            }

            for (int i = 0; i < cardAnimators.Length; i++)
            {
                if (cardNameLabels[i] != null)
                {
                    cardNameLabels[i].color = i == selectedHeroIndex ? new Color(1f, 0.9f, 0.5f) : Color.white;
                }

                if (cardBackgrounds[i] != null)
                {
                    cardBackgrounds[i].color = i == selectedHeroIndex ? cardSelectedColor : cardColor;
                }

                if (cardAnimators[i] == null || cardIcons[i] == null)
                {
                    continue;
                }

                HeroData hero = manager.Party.GetHero(i);

                if (hero == null)
                {
                    cardAnimators[i].Clear();
                    if (cardNameLabels[i] != null)
                    {
                        cardNameLabels[i].SetText("empty");
                    }

                    continue;
                }

                if (cardNameLabels[i] != null)
                {
                    cardNameLabels[i].SetText(hero.HeroName);
                }

                if (!cardAnimators[i].HasArt)
                {
                    if (hero.ArtSet != null)
                    {
                        cardAnimators[i].SetArt(hero.ArtSet);
                    }
                    else if (hero.HeroIcon != null)
                    {
                        cardIcons[i].sprite = hero.HeroIcon;
                    }
                }
            }
        }

        /// <summary>Rebinds the big portrait only when the SELECTED HERO changes - taps must not resize it.</summary>
        private void RefreshBigPortrait()
        {
            if (bigAnimator == null || bigPortrait == null || bigNameLabel == null)
            {
                return;
            }

            if (boundBigHero == selectedHeroIndex)
            {
                return;
            }

            boundBigHero = selectedHeroIndex;

            HeroData hero = selectedHeroIndex >= 0 ? manager.Party.GetHero(selectedHeroIndex) : null;

            if (hero == null)
            {
                bigAnimator.Clear();
                bigNameLabel.SetText(string.Empty);
                return;
            }

            bigNameLabel.SetText(hero.HeroName);

            if (hero.ArtSet != null)
            {
                bigAnimator.SetArt(hero.ArtSet);
            }
            else
            {
                bigAnimator.Clear();
                if (hero.HeroIcon != null)
                {
                    bigPortrait.sprite = hero.HeroIcon;
                }
            }
        }

        private void RefreshStats()
        {
            if (statValues == null || statKeys == null || roleLabel == null)
            {
                return;
            }

            if (selectedHeroIndex < 0 || manager.Party.GetHero(selectedHeroIndex) == null)
            {
                roleLabel.SetText("select a hero");
                for (int i = 0; i < statValues.Length; i++)
                {
                    statValues[i].SetText("-");
                }

                if (statBonusLabels != null)
                {
                    for (int i = 0; i < statBonusLabels.Length; i++)
                    {
                        statBonusLabels[i].SetText(string.Empty);
                    }
                }

                return;
            }

            HeroData hero = manager.Party.GetHero(selectedHeroIndex);
            double health = Resolve(i => manager.Resolver.GetMaxHealth(hero, i), selectedHeroIndex, hero.BaseHealth);
            double attack = Resolve(i => manager.Resolver.GetAttack(hero, i), selectedHeroIndex, hero.BaseAttack);
            double defense = Resolve(i => manager.Resolver.GetDefense(hero, i), selectedHeroIndex, hero.BaseDefense);
            double dps = hero.AttackIntervalSec > 0.05d ? attack / hero.AttackIntervalSec : attack;

            roleLabel.SetText(hero.Role.ToString().ToUpperInvariant());
            statValues[0].SetText(NumberFormatter.Format(health));
            statValues[1].SetText(NumberFormatter.Format(attack));
            statValues[2].SetText(NumberFormatter.Format(defense));
            statValues[3].SetText(NumberFormatter.Format(dps));

            if (statBonusLabels != null && statBonusLabels.Length >= 4 && manager.Gear != null)
            {
                ItemBonuses bonuses = manager.Gear.GetGearBonusFraction(selectedHeroIndex);
                double[] fractions = { bonuses.Hp, bonuses.Atk, bonuses.Def };

                for (int i = 0; i < 3 && i < statBonusLabels.Length; i++)
                {
                    if (fractions[i] > 0.0005d)
                    {
                        statBonusLabels[i].SetText("+" + (fractions[i] * 100d).ToString("0.#") + "%");
                        statBonusLabels[i].color = bonusColor;
                    }
                    else
                    {
                        statBonusLabels[i].SetText(string.Empty);
                    }
                }

                statBonusLabels[3].SetText(string.Empty);
            }
        }

        private static double Resolve(System.Func<int, double> read, int heroIndex, double fallback)
        {
            try
            {
                return read(heroIndex);
            }
            catch (System.Exception)
            {
                return fallback;
            }
        }
    }
}
