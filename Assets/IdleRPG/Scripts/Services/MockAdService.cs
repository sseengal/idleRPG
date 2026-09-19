using System;
using System.Collections;
using UnityEngine;

namespace IdleRPG.Services
{
    /// <summary>
    /// Development implementation of <see cref="IAdService"/>: waits a simulated ad
    /// duration (coroutine on an injected host) and then reports success or failure.
    /// Used until a real ad SDK is integrated.
    /// </summary>
    public sealed class MockAdService : IAdService
    {
        private readonly MonoBehaviour host;
        private readonly float simulatedDurationSec;
        private readonly bool alwaysSucceed;
        private bool isPlaying;

        public MockAdService(MonoBehaviour host, float simulatedDurationSec = 3f, bool alwaysSucceed = true)
        {
            this.host = host;
            this.simulatedDurationSec = Mathf.Max(0f, simulatedDurationSec);
            this.alwaysSucceed = alwaysSucceed;

            if (this.host == null)
            {
                Debug.LogError("[MockAdService] A MonoBehaviour host is required to run the simulated ad.");
            }
        }

        public bool IsRewardedAdReady => host != null && !isPlaying;

        public void ShowRewardedAd(Action<bool> onCompleted)
        {
            if (host == null)
            {
                onCompleted?.Invoke(false);
                return;
            }

            if (isPlaying)
            {
                Debug.LogWarning("[MockAdService] An ad is already playing.");
                onCompleted?.Invoke(false);
                return;
            }

            host.StartCoroutine(PlayRoutine(onCompleted));
        }

        private IEnumerator PlayRoutine(Action<bool> onCompleted)
        {
            isPlaying = true;
            Debug.Log($"[MockAdService] Simulating a {simulatedDurationSec:0.#}s rewarded ad...");

            if (simulatedDurationSec > 0f)
            {
                yield return new WaitForSeconds(simulatedDurationSec);
            }

            isPlaying = false;
            Debug.Log($"[MockAdService] Ad finished (reward granted: {alwaysSucceed}).");
            onCompleted?.Invoke(alwaysSucceed);
        }
    }
}