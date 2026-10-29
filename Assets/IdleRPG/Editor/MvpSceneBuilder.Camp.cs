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
        private const float CampActorY = 0.55f;
        private const float CampFireY = 0.38f;
        private const string CampChoicePrefabPath =
            "Assets/IdleRPG/Prefabs/UI/Battle/CampChoiceCard.prefab";

        private static void BuildCampLayer(RectTransform battlePage, AbilityPanelUI abilityStrip,
            FormationBoardView board, EnemyStackView stack, ScreenController screens, GameObject hudRoot)
        {
            RectTransform viewport = battlePage.Find("Viewport") as RectTransform;

            if (viewport == null)
            {
                Debug.LogError("[MvpSceneBuilder] Camp needs the Viewport under BattlePage.");
                return;
            }

            GameObject viewRoot = UiFactory.Node("CampView", viewport);
            UiFactory.Stretch(viewRoot.GetComponent<RectTransform>());

            Image dim = UiFactory.Panel("Dim", viewRoot.transform, "ui_panel",
                new Color(0f, 0f, 0f, 0.80f), raycast: false);
            UiFactory.Stretch(dim.rectTransform);

            TextMeshProUGUI title = UiFactory.Text("Title", viewRoot.transform, "CAMP", UiType.Role.Display,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.10f, 0.86f), new Vector2(0.90f, 0.95f));

            TextMeshProUGUI token = UiFactory.Text("TokenLabel", viewRoot.transform, "1 LONG-REST TOKEN",
                UiType.Role.Small, TextAlignmentOptions.Center, UiTheme.TextDim);
            UiFactory.Anchor(token.rectTransform, new Vector2(0.10f, 0.80f), new Vector2(0.90f, 0.85f));

            TextMeshProUGUI training = UiFactory.Text("TrainingLabel", viewRoot.transform, "TRAINED +0 ATK · +0 DEF",
                UiType.Role.Micro, TextAlignmentOptions.Center, UiTheme.AccentLight);
            UiFactory.Anchor(training.rectTransform, new Vector2(0.20f, 0.75f), new Vector2(0.80f, 0.80f));

            Image fire = UiFactory.Panel("Fire", viewRoot.transform, "ui_panel_bordered",
                new Color(0.42f, 0.28f, 0.16f, 1f), raycast: false);
            UiFactory.CenterOn(fire.rectTransform, new Vector2(0.5f, CampFireY), new Vector2(240f, 110f));

            Image[] actors = new Image[3];
            TextMeshProUGUI[] hpLabels = new TextMeshProUGUI[3];
            float[] xs = { 0.22f, 0.50f, 0.78f };

            for (int i = 0; i < actors.Length; i++)
            {
                actors[i] = UiFactory.Icon("Hero" + i, viewRoot.transform, Color.white, true);
                UiFactory.CenterOn(actors[i].rectTransform, new Vector2(xs[i], CampActorY), new Vector2(160f, 160f));

                hpLabels[i] = UiFactory.Text("Hp" + i, viewRoot.transform, "—", UiType.Role.Micro,
                    TextAlignmentOptions.Center, UiTheme.Text);
                UiFactory.Anchor(hpLabels[i].rectTransform,
                    new Vector2(xs[i] - 0.15f, 0.25f), new Vector2(xs[i] + 0.15f, 0.31f));
            }

            viewRoot.SetActive(false);

            // The choices replace the ability strip, in the exact same band, so the player's hand does not move.
            Image strip = KitPanel("CampStrip", battlePage, UiTheme.Panel, raycast: true);
            UiFactory.Anchor(strip.rectTransform, new Vector2(0f, LogTop), new Vector2(1f, BattleViewportBottom),
                UiSpace.ScreenMargin, UiSpace.Snug, UiSpace.ScreenMargin, UiSpace.Snug);

            CanvasGroup stripGroup = strip.gameObject.AddComponent<CanvasGroup>();

            Button shortFree = BuildCampChoice(strip.transform, 0, 4, "SHORT REST", "10% FREE", string.Empty,
                UiTheme.PanelLight, UiTheme.Text, UiTheme.TextDim, out _, out _);
            Button shortAd = BuildCampChoice(strip.transform, 1, 4, "SHORT REST+", "20% (AD)", string.Empty,
                UiTheme.Panel, UiTheme.Text, UiTheme.TextDim, out TextMeshProUGUI shortAdLabel, out _);
            Button longRest = BuildCampChoice(strip.transform, 2, 4, "LONG REST", "30% + REVIVE", "1 TOKEN",
                UiTheme.Accent, UiTheme.Text, UiTheme.Text,
                out TextMeshProUGUI longRestLabel, out TextMeshProUGUI longNoteLabel);
            Button train = BuildCampChoice(strip.transform, 3, 4, "TRAIN", "+1 ATK", "NEXT: DEF",
                UiTheme.AccentLight, UiTheme.Text, UiTheme.Text,
                out TextMeshProUGUI trainButtonLabel, out TextMeshProUGUI trainNextLabel);

            strip.gameObject.SetActive(false);

            CampPanelUI camp = hudRoot.AddComponent<CampPanelUI>();
            SceneWiringUtility.SetField(camp, "campView", viewRoot);
            SceneWiringUtility.SetField(camp, "campStrip", strip.gameObject);
            SceneWiringUtility.SetField(camp, "abilityStrip", abilityStrip != null ? abilityStrip.gameObject : null);
            SceneWiringUtility.SetField(camp, "formationBoard", board != null ? board.gameObject : null);
            SceneWiringUtility.SetField(camp, "enemyStack", stack != null ? stack.gameObject : null);
            SceneWiringUtility.SetField(camp, "screens", screens);
            SceneWiringUtility.SetField(camp, "campStripGroup", stripGroup);
            SceneWiringUtility.SetField(camp, "actorImages", actors);
            SceneWiringUtility.SetField(camp, "hpLabels", hpLabels);
            SceneWiringUtility.SetField(camp, "tokenLabel", token);
            SceneWiringUtility.SetField(camp, "trainingLabel", training);
            SceneWiringUtility.SetField(camp, "shortFreeButton", shortFree);
            SceneWiringUtility.SetField(camp, "shortAdButton", shortAd);
            SceneWiringUtility.SetField(camp, "longRestButton", longRest);
            SceneWiringUtility.SetField(camp, "trainButton", train);
            SceneWiringUtility.SetField(camp, "shortAdLabel", shortAdLabel);
            SceneWiringUtility.SetField(camp, "longRestLabel", longRestLabel);
            SceneWiringUtility.SetField(camp, "longNoteLabel", longNoteLabel);
            SceneWiringUtility.SetField(camp, "trainButtonLabel", trainButtonLabel);
            SceneWiringUtility.SetField(camp, "trainNextLabel", trainNextLabel);
            EditorUtility.SetDirty(camp);
        }

        /// <summary>
        /// One camp choice. The prefab owns the button, title, value and note. This method places the card and
        /// supplies the four different labels and tints; the card keeps every band as a fraction of its height.
        /// </summary>
        private static Button BuildCampChoice(Transform parent, int index, int total, string title, string sub,
            string note, Color tint, Color textColor, Color noteColor,
            out TextMeshProUGUI subLabel, out TextMeshProUGUI noteLabel)
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
            cell.Apply(title, sub, note, tint, textColor, noteColor);

            UiFactory.Anchor((RectTransform)cell.transform,
                new Vector2(index / (float)total, 0f), new Vector2((index + 1) / (float)total, 1f),
                UiSpace.Small, UiSpace.Tight, UiSpace.Small, UiSpace.Tight);

            subLabel = cell.SubLabel;
            noteLabel = cell.NoteLabel;
            return cell.Button;
        }
    }
}
