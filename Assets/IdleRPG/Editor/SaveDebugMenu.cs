using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Save;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Save-file utilities for development.
    /// Menu: Tools > Idle RPG > Save > ...
    /// </summary>
    public static class SaveDebugMenu
    {
        [MenuItem("Tools/Idle RPG/Save/Open Save Folder", priority = 60)]
        public static void OpenSaveFolder()
        {
            string path = Application.persistentDataPath;
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"[SaveDebugMenu] Save folder: {path}");
        }

        [MenuItem("Tools/Idle RPG/Save/Log Save Contents", priority = 61)]
        public static void LogSaveContents()
        {
            SaveSystem system = new SaveSystem();

            if (!File.Exists(system.SavePath))
            {
                Debug.LogWarning($"[SaveDebugMenu] No save file at {system.SavePath}");
                return;
            }

            string json = File.ReadAllText(system.SavePath);
            SaveData parsed = JsonUtility.FromJson<SaveData>(json);

            Debug.Log($"[SaveDebugMenu] {system.SavePath}\n" +
                      $"  size={json.Length} bytes\n" +
                      $"  {(parsed != null ? parsed.ToString() : "unparseable")}\n" +
                      $"  backupPresent={File.Exists(system.BackupPath)}");
        }

        [MenuItem("Tools/Idle RPG/Save/Delete Save", priority = 62)]
        public static void DeleteSave()
        {
            if (Application.isPlaying)
            {
                GameManager manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();

                if (manager != null)
                {
                    manager.DeleteSave();
                    Debug.Log("[SaveDebugMenu] Deleted the save through the running game.");
                    return;
                }
            }

            if (!EditorUtility.DisplayDialog("Delete save?", "This removes savegame.json and its backup.", "Delete", "Cancel"))
            {
                return;
            }

            SaveSystem system = new SaveSystem();
            system.Delete();
            PlayerPrefs.DeleteKey(SaveManager.LogoutPlayerPrefsKey);
            PlayerPrefs.Save();
            Debug.Log("[SaveDebugMenu] Deleted the save files.");
        }

        [MenuItem("Tools/Idle RPG/Save/Fake 3h Offline (next launch)", priority = 63)]
        public static void FakeOffline()
        {
            double binary = GameClock.UtcNow.AddHours(-3).ToBinary();
            SaveManager.SetPlayerPrefsLogoutBinary(binary);

            if (Application.isPlaying)
            {
                GameManager manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();

                if (manager != null)
                {
                    manager.Save?.DebugSetLastLogoutBinary(binary);
                    OfflineRewardResult result = manager.EvaluateOffline();

                    Debug.Log(result.HasReward
                        ? $"[SaveDebugMenu] Offline claims offered: {result.CappedSeconds:0}s paid -> {result.Gold:0.#} gold."
                        : "[SaveDebugMenu] Offline evaluated: nothing to claim.");
                    return;
                }
            }

            Debug.Log("[SaveDebugMenu] Logout time set to 3h ago. Re-enter Play mode to see offline earnings.");
        }
    }
}