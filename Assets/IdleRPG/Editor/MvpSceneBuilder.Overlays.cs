using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using IdleRPG.Core;
using IdleRPG.Data;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Overlay construction: the offline claim modal and the toast strip.
    /// </summary>
    public static partial class MvpSceneBuilder
    {
        /// <summary>
        /// The offline claim dialog, built as a real modal: a full-screen scrim that dims the game and blocks every
        /// click behind it, with the panel centred on top. `root` is the scrim, so a single SetActive shows the dim
        /// and the dialog together. Labels are set to ellipsis because TMP's default overflow lets a long line spill
        /// into its neighbours - which is exactly what made the popup read as "text all over each other".
        /// </summary>
        private static OfflineRewardsPopup BuildOfflinePopup(RectTransform modalRoot)
        {
            GameObject host = UiFactory.Node("OfflineRewardsPopup", modalRoot);
            UiFactory.Stretch(host.GetComponent<RectTransform>());   // full-screen host: scrim + dialog anchor against it
            OfflineRewardsPopup ui = host.AddComponent<OfflineRewardsPopup>();

            Image scrim = UiFactory.Panel("Scrim", host.transform, null, new Color(0f, 0f, 0f, 0.65f), raycast: true);
            UiFactory.Stretch(scrim.rectTransform);

            GameObject dialog = UiFactory.Node("Dialog", scrim.transform);
            UiFactory.Anchor(dialog.GetComponent<RectTransform>(), new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.74f));

            // Two images: a solid light card, then the bordered frame on top of it. A bordered-only sprite reads as
            // see-through against the dimmed battlefield.
            Image background = UiFactory.Panel("Background", dialog.transform, "ui_panel_light",
                Color.white, raycast: true);
            UiFactory.Stretch(background.rectTransform);

            Image frame = UiFactory.Panel("Frame", dialog.transform, "ui_panel_bordered", Color.white);
            UiFactory.Stretch(frame.rectTransform);

            TextMeshProUGUI title = UiFactory.Text("Title", dialog.transform, "Welcome back!", 36f,
                TextAlignmentOptions.Center, TextColor);
            UiFactory.Anchor(title.rectTransform, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.97f));

            TextMeshProUGUI timeLabel = UiFactory.Text("Time", dialog.transform, "You were away for 1h", 24f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(timeLabel.rectTransform, new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.83f));

            TextMeshProUGUI goldLabel = UiFactory.Text("Gold", dialog.transform, "+0 gold", 40f,
                TextAlignmentOptions.Center, new Color(0.98f, 0.82f, 0.30f, 1f));
            UiFactory.Anchor(goldLabel.rectTransform, new Vector2(0.06f, 0.47f), new Vector2(0.94f, 0.67f));

            TextMeshProUGUI capNote = UiFactory.Text("CapNote", dialog.transform, "", 19f,
                TextAlignmentOptions.Center, DimTextColor);
            UiFactory.Anchor(capNote.rectTransform, new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.46f));

            Button claimButton = UiFactory.Button("ClaimButton", dialog.transform, "CLAIM", "ui_button_gold", 30f, TextColor, null);
            UiFactory.Anchor(claimButton.GetComponent<RectTransform>(), new Vector2(0.22f, 0.07f), new Vector2(0.78f, 0.27f));

            title.overflowMode = TextOverflowModes.Ellipsis;
            timeLabel.overflowMode = TextOverflowModes.Ellipsis;
            goldLabel.overflowMode = TextOverflowModes.Ellipsis;
            capNote.overflowMode = TextOverflowModes.Ellipsis;
            capNote.textWrappingMode = TextWrappingModes.Normal;

            SceneWiringUtility.SetField(ui, "root", scrim.gameObject);
            SceneWiringUtility.SetField(ui, "titleLabel", title);
            SceneWiringUtility.SetField(ui, "timeLabel", timeLabel);
            SceneWiringUtility.SetField(ui, "goldLabel", goldLabel);
            SceneWiringUtility.SetField(ui, "capNoteLabel", capNote);
            SceneWiringUtility.SetField(ui, "claimButton", claimButton);

            // The scrim is the popup's root: it starts hidden and one SetActive shows dim + dialog together.
            // (The Dialog must stay ACTIVE or the panel would never render - that bug hid the whole dialog.)
            scrim.gameObject.SetActive(false);
            return ui;
        }

        private static ToastUI BuildToast(RectTransform canvasRoot)
        {
            GameObject host = UiFactory.Node("Toast", canvasRoot);
            UiFactory.Stretch(host.GetComponent<RectTransform>());   // full-screen host: the label anchors against it
            CanvasGroup group = host.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            TextMeshProUGUI label = UiFactory.Text("Message", host.transform, "", 26f,
                TextAlignmentOptions.Center, new Color(1f, 0.92f, 0.6f, 1f));
            UiFactory.Anchor(label.rectTransform, new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.40f));

            ToastUI ui = host.AddComponent<ToastUI>();
            SceneWiringUtility.SetField(ui, "canvasGroup", group);
            SceneWiringUtility.SetField(ui, "label", label);
            return ui;
        }
    }
}
