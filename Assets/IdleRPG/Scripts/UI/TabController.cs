using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Control dock tabs (Upgrades / Ascension / Shop). Simple buttons + SetActive panels:
    /// no ToggleGroup overhead, and panels are only touched when the tab changes.
    /// </summary>
    public sealed class TabController : MonoBehaviour
    {
        /// <summary>One tab: its button, its panel and the visuals to swap.</summary>
        [System.Serializable]
        public sealed class TabDefinition
        {
            public string tabName = "Tab";
            public Button button;
            public Image buttonBackground;
            public TextMeshProUGUI buttonLabel;
            public GameObject panel;
        }

        [SerializeField] private List<TabDefinition> tabs = new List<TabDefinition>();
        [SerializeField] private Sprite activeTabSprite;
        [SerializeField] private Sprite inactiveTabSprite;
        [SerializeField] private Color activeTabColor = new Color(0.20f, 0.28f, 0.44f, 1f);
        [SerializeField] private Color inactiveTabColor = new Color(0.10f, 0.12f, 0.18f, 1f);
        [SerializeField] private int defaultTabIndex;

        public int ActiveIndex { get; private set; } = -1;

        private void Start()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i;
                TabDefinition tab = tabs[i];

                if (tab.button != null)
                {
                    tab.button.onClick.AddListener(() => Show(index));
                }
            }

            if (tabs.Count > 0)
            {
                Show(Mathf.Clamp(defaultTabIndex, 0, tabs.Count - 1));
            }
        }

        /// <summary>Activates a tab by index (ignored when out of range).</summary>
        public void Show(int index)
        {
            if (index < 0 || index >= tabs.Count)
            {
                Debug.LogWarning($"[TabController] Tab index {index} is out of range (0..{tabs.Count - 1}).");
                return;
            }

            ActiveIndex = index;

            for (int i = 0; i < tabs.Count; i++)
            {
                TabDefinition tab = tabs[i];
                bool isActive = i == index;

                if (tab.panel != null && tab.panel.activeSelf != isActive)
                {
                    tab.panel.SetActive(isActive);
                }

                if (tab.buttonBackground != null)
                {
                    tab.buttonBackground.color = isActive ? activeTabColor : inactiveTabColor;

                    if (isActive && activeTabSprite != null)
                    {
                        tab.buttonBackground.sprite = activeTabSprite;
                    }
                    else if (!isActive && inactiveTabSprite != null)
                    {
                        tab.buttonBackground.sprite = inactiveTabSprite;
                    }
                }

                if (tab.buttonLabel != null)
                {
                    tab.buttonLabel.color = isActive ? Color.white : new Color(1f, 1f, 1f, 0.65f);
                }
            }
        }
    }
}