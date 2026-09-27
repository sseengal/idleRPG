using System;
using System.IO;
using UnityEngine;

namespace IdleRPG.Core
{
    /// <summary>
    /// Crash diary (B9').
    ///
    /// Plain words: the game can fall over on a phone when you are not holding it, and you need to know why. This
    /// writes every error line to a plain text file next to the save, so afterwards you can open it and read what
    /// happened. It attaches itself when the game starts, needs no scene wiring, and its own file-writing can never
    /// crash the game (that is the whole point of the try/catch).
    /// </summary>
    public static class CrashLog
    {
        private const long MaxBytes = 512 * 1024;
        private static string path;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Attach()
        {
            path = Path.Combine(Application.persistentDataPath, "crash-log.txt");
            Application.logMessageReceived += OnLog;
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            string line = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " [" + type + "] " + message +
                          (string.IsNullOrEmpty(stackTrace) ? "" : "\n" + stackTrace);

            try
            {
                TrimIfHuge();
                File.AppendAllText(path, line + "\n");
            }
            catch
            {
                // The diary must never take the game down with it.
            }
        }

        /// <summary>Keeps the diary small: when it passes the cap, keep only the newest 64KB (the latest failure).</summary>
        private static void TrimIfHuge()
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            FileInfo info = new FileInfo(path);
            if (info.Length <= MaxBytes)
            {
                return;
            }

            const long keep = 64 * 1024;
            long start = Math.Max(0, info.Length - keep);

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                stream.Seek(start, SeekOrigin.Begin);
                byte[] tail = new byte[info.Length - start];
                stream.Read(tail, 0, tail.Length);
                stream.SetLength(0);
                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(tail, 0, tail.Length);
            }
        }
    }
}