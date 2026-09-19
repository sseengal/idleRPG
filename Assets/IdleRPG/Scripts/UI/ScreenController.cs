using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleRPG.UI
{
    /// <summary>
    /// Two-page shell: the battle page (viewport + combat log) and the management page
    /// (upgrades / ascension / shop, each fully scrollable). Nav buttons swap pages and jump
    /// straight to a tab, so nothing important hides behind a scroll anymore.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenController : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] private GameObject battlePage;
        [SerializeField] private GameObject managementPage;

        [Header("Management tabs")]
        [SerializeField] private TabController tabs;

        [Tooltip("Layers that only belong to the battle page (e.g. the damage-number canvas).")]
        [SerializeField] private GameObject[] hideWhileBrowsing;

        [Header("Bottom navigation (Battle, Upgrades, Ascend, Shop)")]
        [SerializeField] private Button[] navButtons;
        [SerializeField] private Image[] navButtonBackgrounds;
        [SerializeField] private TextMeshProUGUI[] navButtonLabels;
        [SerializeField] private Color activeNavColor = new Color(0.24f, 0.34f, 0.55f, 1f);
        [SerializeField] private Color inactiveNavColor = new Color(0.11f, 0.13f, 0.19f, 1f);

        /// <summary>True while the management page is on screen.</summary>
        public bool IsManagementOpen => managementPage != null && managementPage.activeSelf;

        private void Awake()
        {
            if (battlePage == null || managementPage == null || navButtons == null || navButtons.Length == 0)
            {
                Debug.LogError("[ScreenController] Pages or nav buttons are not wired; navigation will not work.");
                enabled = false;
                return;
            }

            WireNavButton(0, ShowBattle);

            for (int i = 1; i < navButtons.Length; i++)
            {
                int tabIndex = i - 1;
                WireNavButton(i, () => ShowManagement(tabIndex));
            }

            // Start on the battle page so the management page never covers the fight.
            ShowBattle();
        }

        /// <summary>Cycles Battle -> Upgrades -> Ascend -> Shop -> Battle (keyboard/tests).</summary>
        public void CyclePage()
        {
            if (!IsManagementOpen)
            {
                ShowManagement(0);
                return;
            }

            int next = tabs != null ? tabs.ActiveIndex + 1 : 1;
            if (next > 2)
            {
                ShowBattle();
                return;
            }

            ShowManagement(next);
        }

        private void WireNavButton(int index, UnityEngine.Events.UnityAction action)
        {
            if (navButtons == null || index < 0 || index >= navButtons.Length || navButtons[index] == null)
            {
                return;
            }

            navButtons[index].onClick.RemoveAllListeners();
            navButtons[index].onClick.AddListener(action);
        }

        /// <summary>Shows the battle page (combat keeps running either way).</summary>
        public void ShowBattle()
        {
            SetPages(battle: true);

            if (tabs != null)
            {
                tabs.gameObject.SetActive(false);
            }

            RefreshNav(0);
        }

        /// <summary>Shows the management page on a specific tab (0 = upgrades, 1 = ascend, 2 = shop).</summary>
        public void ShowManagement(int tabIndex)
        {
            SetPages(battle: false);

            if (tabs != null)
            {
                tabs.gameObject.SetActive(true);
                tabs.Show(tabIndex);
            }

            RefreshNav(Mathf.Clamp(tabIndex, 0, 2) + 1);
        }

        private void SetPages(bool battle)
        {
            if (battlePage != null && battlePage.activeSelf != battle)
            {
                battlePage.SetActive(battle);
            }

            if (managementPage != null && managementPage.activeSelf == battle)
            {
                managementPage.SetActive(!battle);
            }

            if (hideWhileBrowsing == null)
            {
                return;
            }

            for (int i = 0; i < hideWhileBrowsing.Length; i++)
            {
                GameObject layer = hideWhileBrowsing[i];

                if (layer != null && layer.activeSelf != battle)
                {
                    layer.SetActive(battle);
                }
            }
        }

        private void RefreshNav(int activeIndex)
        {
            if (navButtons == null)
            {
                return;
            }

            for (int i = 0; i < navButtons.Length; i++)
            {
                bool isActive = i == activeIndex;

                if (navButtonBackgrounds != null && i < navButtonBackgrounds.Length && navButtonBackgrounds[i] != null)
                {
                    navButtonBackgrounds[i].color = isActive ? activeNavColor : inactiveNavColor;
                }

                if (navButtonLabels != null && i < navButtonLabels.Length && navButtonLabels[i] != null)
                {
                    navButtonLabels[i].color = isActive ? Color.white : new Color(1f, 1f, 1f, 0.6f);
                }
            }
        }
    }
}