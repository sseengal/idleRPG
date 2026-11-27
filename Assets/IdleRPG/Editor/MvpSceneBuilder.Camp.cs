using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Camp construction: a still scene inside the battle viewport and four choices in the band where the
    /// ability buttons normally sit. The battle log is never covered.
    ///
    /// The camp view and strip start hidden. <see cref="CampPanelUI"/> lives on the always-active HUD root,
    /// so it still hears the camp open while the player is on another page.
    /// </summary>
    public static partial class MvpSceneBuilder
    {
        private const float CampMiddleActorY = 0.55f;
        private const float CampSideActorY = 0.42f;
        private const float CampFireY = 0.31f;

        /// <summary>The slot the camp actor art is sized against; the HP plate uses the same width.</summary>
        private const float CampActorSize = 160f;

        /// <summary>The name line's centre, in canvas units below the actor's centre.</summary>
        private static float CampNameOffset =>
            -(CampActorSize * 0.5f + UiSpace.Space8 + UiType.LineHeight(UiType.Role.Micro) * 0.5f);

        /// <summary>The HP bar's centre, in canvas units below the actor's centre.</summary>
        private static float CampBarOffset =>
            -(CampActorSize * 0.5f + UiSpace.Space8 + UiType.LineHeight(UiType.Role.Micro)
              + UiSpace.Space4 + UiSpace.HealthBarHeight * 0.5f);

        /// <summary>The camp fire's box: the 36x48 art at 3.5x, so the flame reads at phone size.</summary>
        private const float CampFireWidth = 126f;
        private const float CampFireHeight = 168f;

        /// <summary>Frames per second for the fire. Ten reads as a flicker, not a strobe.</summary>
        private const float CampFireFps = 10f;
        private const string CampChoicePrefabPath =
            "Assets/IdleRPG/Prefabs/UI/Battle/CampChoiceCard.prefab";

        private static void BuildCampLayer(RectTransform battlePage, AbilityPanelUI abilityStrip,
            FormationBoardView board, EnemyStackView stack, GameObject hudRoot)
        {
            RectTransform viewport = battlePage.Find("Viewport") as RectTransform;

            if (viewport == null)
            {
                Debug.LogError("[MvpSceneBuilder] Camp needs the Viewport under BattlePage.");
                return;
            }

            GameObject viewRoot = UiFactory.Node("CampView", viewport);
            UiFactory.Stretch(viewRoot.GetComponent<RectTransform>());

            Image dim = UiFactory.Panel("Dim", viewRoot.transform, (string)null,
                UiTheme.VeilDeep, raycast: false);
            UiFactory.Stretch(dim.rectTransform);

            TextMeshProUGUI title = UiFactory.Text("Title", viewRoot.transform, "CAMP", UiType.Role.Display,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.10f, 0.86f), new Vector2(0.90f, 0.95f));

            // The camp fire: six frames of the owner's sheet, flipped at 10 fps. The sheet's cells are cut without
            // trimming, so the flame flickers instead of bobbing (owner art, 2026-11-23).
            Image fire = UiFactory.Icon("Campfire", viewRoot.transform, Color.white, true);
            UiFactory.CenterOn(fire.rectTransform, new Vector2(0.5f, CampFireY), new Vector2(CampFireWidth, CampFireHeight));

            Sprite[] campfireFrames = CampfireSlicer.LoadFrames();

            if (campfireFrames.Length > 0)
            {
                fire.sprite = campfireFrames[0];
                fire.gameObject.AddComponent<UiSpriteSequence>().Bind(fire, campfireFrames, CampFireFps, loopFrames: true);
            }
            else
            {
                // Not sliced yet: keep the flat marker so the camp never draws an empty hole.
                fire.color = UiTheme.CampFire;
                Debug.LogWarning("[MvpSceneBuilder] Camp fire sheet is not sliced - run Tools > Idle RPG > Art > Slice Campfire.");
            }

            Image[] actors = new Image[3];
            TextMeshProUGUI[] hpLabels = new TextMeshProUGUI[3];
            HpBarView[] hpBars = new HpBarView[3];
            float[] xs = { 0.30f, 0.50f, 0.70f };

            // The plate hangs off the actor's own anchor, in canvas units: the gap never depends on a rect that the
            // scene builder has not laid out yet. Name, then the green bar, exactly under the feet.
            for (int i = 0; i < actors.Length; i++)
            {
                float actorY = i == 1 ? CampMiddleActorY : CampSideActorY;
                actors[i] = UiFactory.Icon("Hero" + i, viewRoot.transform, Color.white, true);
                UiFactory.CenterOn(actors[i].rectTransform, new Vector2(xs[i], actorY),
                    new Vector2(CampActorSize, CampActorSize));

                hpLabels[i] = UiFactory.Text("HpName" + i, viewRoot.transform, "—", UiType.Role.Micro,
                    TextAlignmentOptions.Center, UiTheme.Text);
                RectTransform nameRect = hpLabels[i].rectTransform;
                nameRect.anchorMin = nameRect.anchorMax = new Vector2(xs[i], actorY);
                nameRect.pivot = new Vector2(0.5f, 0.5f);
                nameRect.sizeDelta = new Vector2(CampActorSize, UiType.LineHeight(UiType.Role.Micro));
                nameRect.anchoredPosition = new Vector2(0f, CampNameOffset);

                hpBars[i] = CreateHpBar(viewRoot.transform, "HpBar" + i);
                RectTransform barRect = (RectTransform)hpBars[i].transform;
                barRect.anchorMin = barRect.anchorMax = new Vector2(xs[i], actorY);
                barRect.pivot = new Vector2(0.5f, 0.5f);
                barRect.sizeDelta = new Vector2(CampActorSize, UiSpace.HealthBarHeight);
                barRect.anchoredPosition = new Vector2(0f, CampBarOffset);
            }

            viewRoot.SetActive(false);

            // The choices replace the ability strip, in the exact same band, so the player's hand does not move.
            Image strip = ContainerPanel("CampStrip", battlePage, raycast: true);
            UiFactory.Anchor(strip.rectTransform, new Vector2(0f, LogTop), new Vector2(1f, BattleViewportBottom),
                UiSpace.PageMargin, UiSpace.Space8, UiSpace.PageMargin, UiSpace.Space8);

            CanvasGroup stripGroup = strip.gameObject.AddComponent<CanvasGroup>();

            Button shortFree = BuildCampChoice(strip.transform, 0, 3, "SHORT REST", "+10% HP", string.Empty,
                out _, out _);
            Button longRest = BuildCampChoice(strip.transform, 1, 3, "LONG REST", "+20% HP (Ad)", string.Empty,
                out TextMeshProUGUI longRestLabel, out _);
            Button train = BuildCampChoice(strip.transform, 2, 3, "TRAIN", "+1 ATK", string.Empty,
                out TextMeshProUGUI trainButtonLabel, out _);

            strip.gameObject.SetActive(false);

            CampPanelUI camp = hudRoot.AddComponent<CampPanelUI>();
            SceneWiringUtility.SetField(camp, "campView", viewRoot);
            SceneWiringUtility.SetField(camp, "campStrip", strip.gameObject);
            SceneWiringUtility.SetField(camp, "abilityStrip", abilityStrip != null ? abilityStrip.gameObject : null);
            SceneWiringUtility.SetField(camp, "formationBoard", board != null ? board.gameObject : null);
            SceneWiringUtility.SetField(camp, "enemyStack", stack != null ? stack.gameObject : null);
            SceneWiringUtility.SetField(camp, "campStripGroup", stripGroup);
            SceneWiringUtility.SetField(camp, "actorImages", actors);
            SceneWiringUtility.SetField(camp, "hpLabels", hpLabels);
            SceneWiringUtility.SetField(camp, "hpBars", hpBars);
            SceneWiringUtility.SetField(camp, "shortFreeButton", shortFree);
            SceneWiringUtility.SetField(camp, "longRestButton", longRest);
            SceneWiringUtility.SetField(camp, "trainButton", train);
            SceneWiringUtility.SetField(camp, "longRestLabel", longRestLabel);
            SceneWiringUtility.SetField(camp, "trainButtonLabel", trainButtonLabel);
            EditorUtility.SetDirty(camp);
        }

        /// <summary>
        /// One camp choice. The prefab owns the button; <see cref="CampChoiceView"/> owns the neutral face, the type
        /// roles and the tokenised stack. This method places the card and supplies its three strings.
        /// </summary>
        private static Button BuildCampChoice(Transform parent, int index, int total, string title, string sub,
            string note, out TextMeshProUGUI subLabel, out TextMeshProUGUI noteLabel)
        {
            CampChoiceView prefab = AssetDatabase.LoadAssetAtPath<CampChoiceView>(CampChoicePrefabPath);

            if (prefab == null)
            {
                Debug.LogError($"[MvpSceneBuilder] Missing camp choice prefab at {CampChoicePrefabPath}.");
                subLabel = null;
                noteLabel = null;
                return null;
            }

            CampChoiceView cell = PrefabUtility.InstantiatePrefab(prefab, parent) as CampChoiceView;

            if (cell == null)
            {
                Debug.LogError("[MvpSceneBuilder] Could not instantiate the camp choice prefab.");
                subLabel = null;
                noteLabel = null;
                return null;
            }

            cell.name = "CampChoice" + index;
            cell.Apply(title, sub, note);

            UiFactory.Anchor((RectTransform)cell.transform,
                new Vector2(index / (float)total, 0f), new Vector2((index + 1) / (float)total, 1f),
                UiSpace.Space8, UiSpace.Space4, UiSpace.Space8, UiSpace.Space4);

            subLabel = cell.SubLabel;
            noteLabel = cell.NoteLabel;
            return cell.Button;
        }
    }
}
