using System;
using System.IO;
using UnityEngine;

namespace IdleRPG.Save
{
    /// <summary>
    /// File layer for the save game: atomic writes, a rolling backup and corrupt-file recovery.
    /// Plain C# so the game logic can be driven from tests or tooling without a scene.
    /// </summary>
    public sealed class SaveSystem
    {
        public const string FileName = "savegame.json";

        private readonly string directory;

        public SaveSystem(string directory = null)
        {
            this.directory = string.IsNullOrEmpty(directory) ? Application.persistentDataPath : directory;
        }

        public string SavePath => Path.Combine(directory, FileName);

        public string BackupPath => SavePath + ".bak";

        /// <summary>True when the last load had to fall back to the backup file.</summary>
        public bool RecoveredFromBackup { get; private set; }

        /// <summary>True when any save file (primary or backup) exists.</summary>
        public bool HasSave => File.Exists(SavePath) || File.Exists(BackupPath);

        /// <summary>
        /// Loads the save. Falls back to the backup when the primary file is missing or corrupt,
        /// and quarantines a corrupt file so it can be inspected later.
        /// </summary>
        public bool TryLoad(out SaveData data)
        {
            data = null;

            if (TryRead(SavePath, out data))
            {
                return true;
            }

            if (File.Exists(SavePath))
            {
                QuarantineCorruptFile();
            }

            if (TryRead(BackupPath, out data))
            {
                RecoveredFromBackup = true;
                Debug.LogWarning("[SaveSystem] Primary save unusable; recovered the backup.");
                return true;
            }

            data = null;
            return false;
        }

        /// <summary>
        /// Writes atomically: a temp file is flushed first, then swapped in, keeping the previous
        /// file as the backup. Returns false (and logs) when the write fails.
        /// </summary>
        public bool Save(SaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveSystem] Refusing to save a null payload.");
                return false;
            }

            try
            {
                data.schemaVersion = SaveData.CurrentVersion;
                string json = JsonUtility.ToJson(data, true);
                string tempPath = SavePath + ".tmp";

                File.WriteAllText(tempPath, json);
                ReplaceWithBackup(tempPath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSystem] Save failed: {exception.Message}");
                return false;
            }
        }

        /// <summary>Deletes the save and its backup (used by debug tools and a future "reset").</summary>
        public bool Delete()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                }

                if (File.Exists(BackupPath))
                {
                    File.Delete(BackupPath);
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSystem] Delete failed: {exception.Message}");
                return false;
            }
        }

        private bool TryRead(string path, out SaveData data)
        {
            data = null;

            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                if (parsed == null || !parsed.IsUsable)
                {
                    return false;
                }

                parsed.Sanitize();
                data = SaveMigrations.Migrate(parsed);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSystem] Could not read '{path}': {exception.Message}");
                return false;
            }
        }

        private void ReplaceWithBackup(string tempPath)
        {
            if (File.Exists(SavePath))
            {
                File.Copy(SavePath, BackupPath, true);
                File.Delete(SavePath);
            }

            File.Move(tempPath, SavePath);
        }

        private void QuarantineCorruptFile()
        {
            try
            {
                string quarantined = Path.Combine(directory, $"savegame.corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
                File.Copy(SavePath, quarantined, true);
                Debug.LogWarning($"[SaveSystem] Quarantined the unreadable save as {Path.GetFileName(quarantined)}");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SaveSystem] Could not quarantine the corrupt save: {exception.Message}");
            }
        }
    }
}
