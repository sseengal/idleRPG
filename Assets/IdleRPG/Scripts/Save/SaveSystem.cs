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

        /// <summary>How many rolling backups to keep (G6: idle saves live for months).</summary>
        public const int BackupCount = 3;

        public string BackupPathFor(int index)
        {
            return index <= 0 ? BackupPath : BackupPath + index;
        }

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

            for (int i = 0; i < BackupCount; i++)
            {
                if (TryRead(BackupPathFor(i), out data))
                {
                    RecoveredFromBackup = true;
                    Debug.LogWarning($"[SaveSystem] Primary save unusable; recovered backup {i}.");
                    return true;
                }
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

                for (int i = 0; i < BackupCount; i++)
                {
                    if (File.Exists(BackupPathFor(i)))
                    {
                        File.Delete(BackupPathFor(i));
                    }
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

        /// <summary>
        /// Writes the temp file into place, rolling the old files down: .bak -> .bak2 -> .bak3.
        ///
        /// ELI5: instead of keeping one spare copy, keep three - each one generation older. An idle save is
        /// played for months, so being able to step back two saves is worth a few extra kilobytes.
        /// </summary>
        private void ReplaceWithBackup(string tempPath)
        {
            if (File.Exists(BackupPathFor(BackupCount - 1)))
            {
                File.Delete(BackupPathFor(BackupCount - 1));
            }

            for (int i = BackupCount - 2; i >= 1; i--)
            {
                if (File.Exists(BackupPathFor(i)))
                {
                    File.Move(BackupPathFor(i), BackupPathFor(i + 1));
                }
            }

            if (File.Exists(BackupPath))
            {
                File.Move(BackupPath, BackupPathFor(1));
            }

            if (File.Exists(SavePath))
            {
                File.Move(SavePath, BackupPath);
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
