using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Camp panel construction: a modal still scene with the party, a placeholder fire and three choices.
    /// The panel stays active and is hidden through its CanvasGroup, so its Start can subscribe to the service.
    /// </summary>
    public static partial class MvpSceneBuilder
    {
        private static void BuildCampPanel(RectTransform modalRoot)
        {
            GameObject root = UiFactory.Node("CampModal", modalRoot);
            UiFactory.Stretch(root.GetComponent<RectTransform>());

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image scrim = UiFactory.Panel("Scrim", root.transform, "ui_panel", new Color(0f, 0f, 0f, 0.88f), raycast: true);
            UiFactory.Stretch(scrim.rectTransform);

            Image face = KitPanel("CampFace", root.transform, UiTheme.PanelDeep, raycast: true);
            UiFactory.Anchor(face.rectTransform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f));

            TextMeshProUGUI title = UiFactory.Text("Title", face.transform, "CAMP", UiType.Role.Display,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.10f, 0.88f), new Vector2(0.90f, 0.96f));

            TextMeshProUGUI token = UiFactory.Text("TokenLabel", face.transform, "1 LONG-REST TOKEN", UiType.Role.Small,
                TextAlignmentOptions.Center, UiTheme.TextDim);
            UiFactory.Anchor(token.rectTransform, new Vector2(0.10f, 0.83f), new Vector2(0.90f, 0.88f));

            TextMeshProUGUI training = UiFactory.Text("TrainingLabel", face.transform, "TRAINED +0 ATK · +0 DEF",
                UiType.Role.Micro, TextAlignmentOptions.Center, UiTheme.AccentLight);
            UiFactory.Anchor(training.rectTransform, new Vector2(0.20f, 0.79f), new Vector2(0.80f, 0.83f));

            Image fire = UiFactory.Panel("Fire", face.transform, "ui_panel_bordered",
                new Color(0.42f, 0.28f, 0.16f, 1f), raycast: false);
            UiFactory.CenterOn(fire.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(240f, 110f));

            Image[] actors = new Image[3];
            TextMeshProUGUI[] hpLabels = new TextMeshProUGUI[3];
            float[] xs = { 0.22f, 0.50f, 0.78f };

            for (int i = 0; i < actors.Length; i++)
            {
                actors[i] = UiFactory.Icon("Hero" + i, face.transform, Color.white, true);
                UiFactory.CenterOn(actors[i].rectTransform, new Vector2(xs[i], 0.71f), new Vector2(160f, 160f));

                hpLabels[i] = UiFactory.Text("Hp" + i, face.transform, "—", UiType.Role.Micro,
                    TextAlignmentOptions.Center, UiTheme.Text);
                UiFactory.Anchor(hpLabels[i].rectTransform,
                    new Vector2(xs[i] - 0.15f, 0.55f), new Vector2(xs[i] + 0.15f, 0.60f));
            }

            Image shortCard = BuildCampCard(face.transform, "ShortCard", 0.03f, 0.335f,
                "SHORT REST", "10% free · 20% ad");
            Image longCard = BuildCampCard(face.transform, "LongCard", 0.35f, 0.655f,
                "LONG REST", "Token · revive the fallen");
            Image trainCard = BuildCampCard(face.transform, "TrainCard", 0.67f, 0.975f,
                "TRAIN", "Party-wide · ATK / DEF");

            Button shortFree = UiFactory.Button("ShortFree", shortCard.transform, "10%", "ui_panel_light",
                UiType.Role.Section, UiTheme.Text, null);
            shortFree.targetGraphic.color = UiTheme.PanelLight;
            UiFactory.CenterOn(shortFree.GetComponent<RectTransform>(), new Vector2(0.5f, 0.40f), new Vector2(260f, 140f));

            Button shortAd = UiFactory.Button("ShortAd", shortCard.transform, "20% (AD)", "ui_panel_light",
                UiType.Role.Small, UiTheme.TextDim, null);
            shortAd.targetGraphic.color = UiTheme.Panel;
            UiFactory.CenterOn(shortAd.GetComponent<RectTransform>(), new Vector2(0.5f, 0.15f), new Vector2(260f, 140f));

            Button longRest = UiFactory.Button("LongRest", longCard.transform, "30% + REVIVE", "ui_panel_light",
                UiType.Role.Small, UiTheme.InkOnAccent, null);
            longRest.targetGraphic.color = UiTheme.Accent;
            UiFactory.CenterOn(longRest.GetComponent<RectTransform>(), new Vector2(0.5f, 0.30f), new Vector2(280f, 144f));

            Button train = UiFactory.Button("Train", trainCard.transform, "+1 ATK", "ui_panel_light",
                UiType.Role.Section, UiTheme.InkOnAccent, null);
            train.targetGraphic.color = UiTheme.AccentLight;
            UiFactory.CenterOn(train.GetComponent<RectTransform>(), new Vector2(0.5f, 0.36f), new Vector2(280f, 144f));

            TextMeshProUGUI trainNext = UiFactory.Text("TrainNext", trainCard.transform, "next: DEF", UiType.Role.Small,
                TextAlignmentOptions.Center, UiTheme.TextDim);
            UiFactory.Anchor(trainNext.rectTransform, new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.22f));

            TextMeshProUGUI auto = UiFactory.Text("AutoLabel", face.transform, "auto in 10…", UiType.Role.Small,
                TextAlignmentOptions.Center, UiTheme.TextDim);
            UiFactory.Anchor(auto.rectTransform, new Vector2(0.20f, 0.01f), new Vector2(0.80f, 0.06f));

            CampPanelUI camp = root.AddComponent<CampPanelUI>();
            SceneWiringUtility.SetField(camp, "group", group);
            SceneWiringUtility.SetField(camp, "actorImages", actors);
            SceneWiringUtility.SetField(camp, "hpLabels", hpLabels);
            SceneWiringUtility.SetField(camp, "tokenLabel", token);
            SceneWiringUtility.SetField(camp, "trainingLabel", training);
            SceneWiringUtility.SetField(camp, "autoLabel", auto);
            SceneWiringUtility.SetField(camp, "shortAdLabel", shortAd.GetComponentInChildren<TextMeshProUGUI>());
            SceneWiringUtility.SetField(camp, "longRestLabel", longRest.GetComponentInChildren<TextMeshProUGUI>());
            SceneWiringUtility.SetField(camp, "trainButtonLabel", train.GetComponentInChildren<TextMeshProUGUI>());
            SceneWiringUtility.SetField(camp, "trainNextLabel", trainNext);
            SceneWiringUtility.SetField(camp, "shortFreeButton", shortFree);
            SceneWiringUtility.SetField(camp, "shortAdButton", shortAd);
            SceneWiringUtility.SetField(camp, "longRestButton", longRest);
            SceneWiringUtility.SetField(camp, "trainButton", train);
            EditorUtility.SetDirty(camp);
        }

        private static Image BuildCampCard(Transform parent, string name, float xMin, float xMax,
            string title, string description)
        {
            Image card = KitPanel(name, parent, UiTheme.Panel, raycast: true);
            UiFactory.Anchor(card.rectTransform, new Vector2(xMin, 0.08f), new Vector2(xMax, 0.40f));

            TextMeshProUGUI titleLabel = UiFactory.Text("Title", card.transform, title, UiType.Role.Section,
                TextAlignmentOptions.Center, UiTheme.Text);
            UiFactory.Anchor(titleLabel.rectTransform, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.94f));

            TextMeshProUGUI desc = UiFactory.Text("Desc", card.transform, description, UiType.Role.Micro,
                TextAlignmentOptions.Center, UiTheme.TextDim);
            desc.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.Anchor(desc.rectTransform, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.78f));

            return card;
        }
    }
}
