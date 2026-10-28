using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using IdleRPG.UI;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Builds the CRT transition on the modal canvas. It sits above the HUD, damage numbers and cards,
    /// and starts clear. The component lives on the always-active HUD root so its Start can subscribe.
    /// </summary>
    public static partial class MvpSceneBuilder
    {
        private static void BuildCrtTransition(RectTransform modalCanvas, GameObject hudRoot)
        {
            GameObject root = UiFactory.Node("CrtTransition", modalCanvas);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            UiFactory.Stretch(rootRect);
            root.transform.SetAsLastSibling();

            Image shade = KitPanel("Shade", root.transform, new Color(0f, 0f, 0f, 0f), raycast: false);
            shade.enabled = false;

            Image topBar = KitPanel("TopBar", root.transform, Color.black, raycast: false);
            topBar.rectTransform.anchorMin = new Vector2(0f, 1f);
            topBar.rectTransform.anchorMax = new Vector2(1f, 1f);
            topBar.rectTransform.pivot = new Vector2(0.5f, 1f);
            topBar.rectTransform.anchoredPosition = Vector2.zero;
            topBar.rectTransform.sizeDelta = Vector2.zero;
            topBar.enabled = false;

            Image bottomBar = KitPanel("BottomBar", root.transform, Color.black, raycast: false);
            bottomBar.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottomBar.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottomBar.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottomBar.rectTransform.anchoredPosition = Vector2.zero;
            bottomBar.rectTransform.sizeDelta = Vector2.zero;
            bottomBar.enabled = false;

            Image beam = KitPanel("Beam", root.transform, Color.white, raycast: false);
            beam.type = Image.Type.Simple;
            beam.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            beam.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            beam.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            beam.rectTransform.anchoredPosition = Vector2.zero;
            beam.rectTransform.sizeDelta = new Vector2(0f, 4f);
            beam.enabled = false;

            Image blocker = KitPanel("Blocker", root.transform, new Color(0f, 0f, 0f, 0f), raycast: true);
            blocker.gameObject.SetActive(false);

            CrtTransitionUI crt = hudRoot.AddComponent<CrtTransitionUI>();
            SceneWiringUtility.SetField(crt, "root", rootRect);
            SceneWiringUtility.SetField(crt, "topBar", topBar);
            SceneWiringUtility.SetField(crt, "bottomBar", bottomBar);
            SceneWiringUtility.SetField(crt, "beam", beam);
            SceneWiringUtility.SetField(crt, "shade", shade);
            SceneWiringUtility.SetField(crt, "inputBlocker", blocker.gameObject);
            EditorUtility.SetDirty(crt);
        }
    }
}
