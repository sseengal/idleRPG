using System;
using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.Save
{
    /// <summary>
    /// Owns when to save: autosave cadence, debounced "something important happened" saves and the
    /// app lifecycle hooks. The payload comes from the caller's capture delegate, so this class never
    /// needs to know about combat, upgrades or the economy.
    /// </summary>
    public sealed class SaveManager
    {
        public const string LogoutPlayerPrefsKey = "IdleRPG.lastLogout";

        /// <summary>Minimum gap between debounced saves, so a burst of events writes once.</summary>
        private const float MinDirtyGapSec = 5f;

        private readonly SaveSystem saveSystem;
        private readonly BalanceConfig balanceConfig;
        private readonly Func<SaveData> capture;

        private float autosaveTimer;
        private float dirtyTimer;

        public SaveManager(SaveSystem saveSystem, BalanceConfig balanceConfig, Func<SaveData> capture)
        {
            this.saveSystem = saveSystem;
            this.balanceConfig = balanceConfig;
            this.capture = capture;

            if (this.saveSystem == null || this.capture == null)
            {
                Debug.LogError("[SaveManager] Needs a SaveSystem and a capture delegate.");
            }
        }

        /// <summary>Raised after a successful write (mirrors GameEvents.SaveWritten).</summary>
        public event Action<SaveData> Saved;

        public bool HasSave => saveSystem != null && saveSystem.HasSave;

        /// <summary>True when the load had to use the backup file (caller should re-save).</summary>
        public bool RecoveredFromBackup => saveSystem != null && saveSystem.RecoveredFromBackup;

        /// <summary>Seconds of play time in this save file (accrued by the tick).</summary>
        public double PlayTimeSeconds { get; private set; }

        public int SaveCount { get; private set; }

        /// <summary>True when something changed since the last write.</summary>
        public bool IsDirty { get; private set; }

        public double SecondsSinceLastSave { get; private set; }

        /// <summary>Where the last payload came from: "file", "fresh" or "none" (debug read-out).</summary>
        public string LastLoadSource { get; private set; } = "none";

        /// <summary>Timestamp of the previous session end; the offline calculator consumes it.</summary>
        public double LastLogoutBinary { get; private set; }

        /// <summary>Loads the file if present and adopts its meta data (play time, save count).</summary>
        public bool TryLoad(out SaveData data)
        {
            data = null;

            if (saveSystem == null || !saveSystem.TryLoad(out SaveData loaded))
            {
                LastLoadSource = "fresh";
                return false;
            }

            data = loaded;
            PlayTimeSeconds = loaded.playTimeSeconds;
            SaveCount = loaded.saveCount;

            if (double.TryParse(loaded.lastLogoutTimestampBinary, out double logoutBinary))
            {
                LastLogoutBinary = logoutBinary;
            }

            LastLoadSource = "file";
            return true;
        }

        /// <summary>Writes immediately. Safe to call on quit; failures are logged, never thrown.</summary>
        public bool SaveNow(string reason)
        {
            if (saveSystem == null || capture == null)
            {
                return false;
            }

            SaveData data = capture();
            if (data == null)
            {
                Debug.LogError("[SaveManager] Capture returned null; nothing saved.");
                return false;
            }

            double nowBinary = GameClock.NowBinary;
            data.lastSaveTimestampBinary = nowBinary.ToString("R");
            data.lastLogoutTimestampBinary = nowBinary.ToString("R");
            data.playTimeSeconds = PlayTimeSeconds;
            data.saveCount = SaveCount + 1;

            if (!saveSystem.Save(data))
            {
                return false;
            }

            LastLogoutBinary = nowBinary;
            SaveCount = data.saveCount;
            SecondsSinceLastSave = 0f;
            autosaveTimer = 0f;
            dirtyTimer = 0f;
            IsDirty = false;

            // Spec: the logout timestamp also lives in PlayerPrefs so a hard process kill still counts.
            PlayerPrefs.SetString(LogoutPlayerPrefsKey, nowBinary.ToString("R"));
            PlayerPrefs.Save();

            Saved?.Invoke(data);
            GameEvents.RaiseSaveWritten();

            return true;
        }

        /// <summary>Flags a change; the next tick writes it (debounced so bursts do not thrash the disk).</summary>
        public void MarkDirty(string reason = null)
        {
            IsDirty = true;
            dirtyTimer = 0f;
        }

        /// <summary>Called from the game's one-second tick: autosave cadence and play time.</summary>
        public void Tick(float deltaTime)
        {
            PlayTimeSeconds += deltaTime;
            autosaveTimer += deltaTime;
            dirtyTimer += deltaTime;
            SecondsSinceLastSave += deltaTime;

            float interval = balanceConfig != null ? balanceConfig.AutosaveIntervalSec : 15f;

            if (autosaveTimer >= interval)
            {
                SaveNow("autosave");
                return;
            }

            if (IsDirty && dirtyTimer >= MinDirtyGapSec)
            {
                SaveNow("dirty");
            }
        }

        /// <summary>Wipes the save file and the PlayerPrefs timestamp (debug / future reset button).</summary>
        public void DeleteSave()
        {
            if (saveSystem != null)
            {
                saveSystem.Delete();
            }

            PlayerPrefs.DeleteKey(LogoutPlayerPrefsKey);
            PlayerPrefs.Save();

            PlayTimeSeconds = 0d;
            SaveCount = 0;
            IsDirty = false;

            Debug.Log("[SaveManager] Save deleted.");
        }

        /// <summary>Reads the PlayerPrefs timestamp written at the end of the last session.</summary>
        public static bool TryReadPlayerPrefsLogout(out double binary)
        {
            binary = 0d;

            if (!PlayerPrefs.HasKey(LogoutPlayerPrefsKey))
            {
                return false;
            }

            return double.TryParse(PlayerPrefs.GetString(LogoutPlayerPrefsKey), out binary);
        }

        /// <summary>Debug hook: rewinds the logout timestamp so offline progress can be tested live.</summary>
        public void DebugSetLastLogoutBinary(double binary)
        {
            LastLogoutBinary = binary;
            SetPlayerPrefsLogoutBinary(binary);
        }

        /// <summary>Debug helper: makes the next launch see an older logout time.</summary>
        public static void SetPlayerPrefsLogoutBinary(double binary)
        {
            PlayerPrefs.SetString(LogoutPlayerPrefsKey, binary.ToString("R"));
            PlayerPrefs.Save();
        }
    }
}