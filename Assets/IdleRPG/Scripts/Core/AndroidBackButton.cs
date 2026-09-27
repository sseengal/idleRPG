using UnityEngine;
using UnityEngine.InputSystem;

namespace IdleRPG.Core
{
    /// <summary>
    /// Android back button behaviour (B9').
    ///
    /// Plain words: on an Android phone the back gesture is how a player leaves an app, and it must not drop them
    /// out of the game mid-fight and lose their progress. Pressing it here saves the game first, then leaves. The
    /// game already saves by itself, but "save, then go" makes it certain even if the phone kills the app right
    /// after. Only does anything on a real Android phone.
    /// </summary>
    public sealed class AndroidBackButton : MonoBehaviour
    {
        private GameManager manager;

        /// <summary>Creates the handler and points it at the game manager. No scene wiring needed.</summary>
        public static AndroidBackButton Create(GameManager manager)
        {
            GameObject host = new GameObject("AndroidBackButton");
            Object.DontDestroyOnLoad(host);

            AndroidBackButton handler = host.AddComponent<AndroidBackButton>();
            handler.manager = manager;
            return handler;
        }

        private void Update()
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            if (!backPressedThisFrame())
            {
                return;
            }

            if (manager != null)
            {
                manager.SaveNow();
            }

            Application.Quit();
        }

        /// <summary>
        /// Reads the Android back key whichever input backend is active: the new Input System reports it as "escape"
        /// on the keyboard device; the legacy backend reports KeyCode.Escape. One of them answers.
        /// </summary>
        private static bool backPressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            try
            {
                return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
            }
            catch
            {
                // Legacy input is disabled in this build - the new backend already had its chance above.
                return false;
            }
        }
    }
}