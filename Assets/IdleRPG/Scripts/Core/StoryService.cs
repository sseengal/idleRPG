using System;
using System.Collections.Generic;
using UnityEngine;
using IdleRPG.Data;

namespace IdleRPG.Core
{
    /// <summary>
    /// Decides when a story scene plays and holds the wave until it ends.
    ///
    /// ELI5: the scheduler. Before a wave spawns it asks the schedule, "is anything due here?" If yes, it
    /// stops the wave, plays the scene(s) in priority order, and then lets the wave spawn once.
    ///
    /// A scene asset never knows its stage or wave; this class owns that placement. It also owns the one
    /// safety rule that matters: the wave is released once, after the last queued scene, never between them.
    /// </summary>
    public sealed class StoryService
    {
        /// <summary>How a player is asked to show a scene. False = no screen is available right now.</summary>
        public delegate bool SceneRequest(StorySceneDef scene, Action onFinished);

        private readonly struct Candidate
        {
            public readonly StorySceneDef Scene;
            public readonly int RuleIndex;
            public readonly int Stage;
            public readonly int Priority;

            public Candidate(StorySceneDef scene, int ruleIndex, int stage, int priority)
            {
                Scene = scene;
                RuleIndex = ruleIndex;
                Stage = stage;
                Priority = priority;
            }
        }

        private readonly struct QueuedScene
        {
            public readonly StorySceneDef Scene;
            public readonly int RuleIndex;
            public readonly int Stage;

            public QueuedScene(StorySceneDef scene, int ruleIndex, int stage)
            {
                Scene = scene;
                RuleIndex = ruleIndex;
                Stage = stage;
            }
        }

        private const float PlayerBindGraceSeconds = 1f;

        private readonly GameManager manager;
        private readonly StoryScheduleDef schedule;
        private readonly Queue<QueuedScene> queue = new Queue<QueuedScene>();
        private readonly List<Candidate> candidates = new List<Candidate>();
        private readonly Dictionary<long, int> stageCounts = new Dictionary<long, int>();
        private readonly Dictionary<int, int> runCounts = new Dictionary<int, int>();

        private SceneRequest request;
        private QueuedScene? current;
        private bool holding;
        private bool waitingForPlayer;
        private float waitingSeconds;

        public StoryService(GameManager manager, StoryScheduleDef schedule)
        {
            this.manager = manager;
            this.schedule = schedule;
        }

        /// <summary>True while a scene owns the screen (playing, or waiting for the HUD to bind).</summary>
        public bool IsPlaying => current.HasValue;

        /// <summary>True while the service is holding a wave.</summary>
        public bool IsHolding => holding;

        /// <summary>The scene on screen, or null.</summary>
        public StorySceneDef CurrentScene => current.HasValue ? current.Value.Scene : null;

        /// <summary>Scenes waiting after the current one.</summary>
        public int PendingCount => queue.Count + (current.HasValue ? 1 : 0);

        /// <summary>Binds the UI that can show scenes. Flushes a scene that was requested before the HUD existed.</summary>
        public void SetPlayer(SceneRequest player)
        {
            request = player;

            if (waitingForPlayer && current.HasValue)
            {
                StartCurrent();
            }
        }

        /// <summary>
        /// Called by the wave gate before a wave spawns. True = the wave is held until the scenes finish.
        /// Matches BeforeWave against the upcoming wave and AfterWave against the cleared wave.
        /// </summary>
        public bool ShouldHold(int stage, int wave)
        {
            if (schedule == null || schedule.Count == 0)
            {
                return false;
            }

            if (current.HasValue)
            {
                holding = true;
                return true;
            }

            if (!Collect(stage, wave))
            {
                return false;
            }

            holding = true;
            StartNext();
            return true;
        }

        /// <summary>
        /// Releases a held wave if no UI ever binds. Called every frame by the game manager; costs nothing
        /// unless a scene is waiting for a player.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (!waitingForPlayer || !current.HasValue)
            {
                return;
            }

            waitingSeconds += deltaSeconds;

            if (waitingSeconds < PlayerBindGraceSeconds)
            {
                return;
            }

            Debug.LogWarning("[StoryService] No story player bound; releasing the held wave instead of stalling.");
            waitingForPlayer = false;
            waitingSeconds = 0f;
            current = null;
            StartNext();
        }

        /// <summary>New run: forget once-per-run marks. Stage marks stay keyed by stage, so a retry is quiet.</summary>
        public void ResetRun()
        {
            runCounts.Clear();
            stageCounts.Clear();
        }

        private bool Collect(int stage, int wave)
        {
            candidates.Clear();
            StoryRule[] rules = schedule.Rules;

            for (int i = 0; i < rules.Length; i++)
            {
                StoryRule rule = rules[i];

                if (rule.scene == null || !rule.MatchesStage(stage))
                {
                    continue;
                }

                int waveNumber;

                if (rule.cue == StoryCue.BeforeWave)
                {
                    waveNumber = wave;
                }
                else if (rule.cue == StoryCue.AfterWave)
                {
                    waveNumber = wave - 1;

                    if (waveNumber < 1)
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }

                if (!rule.wave.Matches(waveNumber))
                {
                    continue;
                }

                if (rule.wave.IsRandom && Roll(rule.scene, stage, waveNumber, i) >= rule.wave.chance)
                {
                    continue;
                }

                if (LimitReached(rule, i, stage))
                {
                    continue;
                }

                candidates.Add(new Candidate(rule.scene, i, stage, rule.priority));
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            candidates.Sort((a, b) => a.Priority != b.Priority
                ? b.Priority.CompareTo(a.Priority)
                : a.RuleIndex.CompareTo(b.RuleIndex));

            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                queue.Enqueue(new QueuedScene(candidate.Scene, candidate.RuleIndex, candidate.Stage));
            }

            return true;
        }

        private bool LimitReached(StoryRule rule, int ruleIndex, int stage)
        {
            int stageCount = StageCount(ruleIndex, stage);

            if (rule.playLimit == PlayLimit.OncePerStage && stageCount >= 1)
            {
                return true;
            }

            if (rule.playLimit == PlayLimit.OncePerRun && RunCount(ruleIndex) >= 1)
            {
                return true;
            }

            return rule.maxPerStage > 0 && stageCount >= rule.maxPerStage;
        }

        private void StartNext()
        {
            if (current.HasValue)
            {
                return;
            }

            if (queue.Count == 0)
            {
                if (holding)
                {
                    holding = false;
                    manager?.Combat?.ResumeAfterGate();
                }

                return;
            }

            current = queue.Dequeue();
            MarkPlayed(current.Value.RuleIndex, current.Value.Stage);
            StartCurrent();
        }

        private void StartCurrent()
        {
            if (!current.HasValue)
            {
                return;
            }

            if (request == null)
            {
                waitingForPlayer = true;
                waitingSeconds = 0f;
                return;
            }

            waitingForPlayer = false;
            waitingSeconds = 0f;

            QueuedScene scene = current.Value;
            bool shown = request(scene.Scene, OnSceneFinished);

            if (shown)
            {
                return;
            }

            Debug.LogWarning($"[StoryService] Could not show '{scene.Scene.Id}'; dropping it so the wave cannot stall.");
            current = null;
            StartNext();
        }

        private void OnSceneFinished()
        {
            if (!current.HasValue)
            {
                return;
            }

            current = null;
            StartNext();
        }

        private void MarkPlayed(int ruleIndex, int stage)
        {
            long key = StageKey(ruleIndex, stage);
            stageCounts[key] = StageCount(ruleIndex, stage) + 1;
            runCounts[ruleIndex] = RunCount(ruleIndex) + 1;
        }

        private int StageCount(int ruleIndex, int stage)
        {
            return stageCounts.TryGetValue(StageKey(ruleIndex, stage), out int count) ? count : 0;
        }

        private int RunCount(int ruleIndex)
        {
            return runCounts.TryGetValue(ruleIndex, out int count) ? count : 0;
        }

        private static long StageKey(int ruleIndex, int stage)
        {
            return ((long)ruleIndex << 32) ^ (uint)stage;
        }

        /// <summary>
        /// Stable 0..1 roll. Same scene, stage, wave and rule row always give the same answer, so a relaunch
        /// cannot re-roll a random scene in or out. Reordering the schedule changes the row index and the roll.
        /// </summary>
        private static float Roll(StorySceneDef scene, int stage, int wave, int ruleIndex)
        {
            unchecked
            {
                uint hash = 2166136261u;
                string key = (scene != null ? scene.Id : string.Empty) + "|" + stage + "|" + wave + "|" + ruleIndex;

                for (int i = 0; i < key.Length; i++)
                {
                    hash ^= key[i];
                    hash *= 16777619u;
                }

                return hash / 4294967296f;
            }
        }
    }
}
