using UnityEngine;
using IdleRPG.Core;
using IdleRPG.Data;

namespace IdleRPG.Services
{
    /// <summary>
    /// Translates game events into sounds. This is the only class that knows "an enemy was killed" should make a
    /// noise, so gameplay code never calls the audio service directly.
    ///
    /// ELI5: the game already shouts about everything that happens (events). This is the one listener that turns
    /// shouts into beeps, and it throttles the noisy ones - a hit every frame would be a buzz, not a sound effect.
    ///
    /// On the same object as <see cref="GameManager"/> (created by it), so lifetime is automatic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour
    {
        [Tooltip("Minimum gap between two 'hit' cues. Combat fires many hits per second.")]
        [SerializeField] private float hitMinGapSec = 0.07f;

        [Tooltip("Minimum gap between two 'kill' cues.")]
        [SerializeField] private float killMinGapSec = 0.05f;

        private IAudioService audio;
        private float lastHitPlayTime = -10f;
        private float lastKillPlayTime = -10f;

        /// <summary>Creates (or reuses) the director on a host object. Called by GameManager.</summary>
        public static AudioDirector Create(GameObject host, IAudioService service)
        {
            if (host == null || service == null)
            {
                return null;
            }

            AudioDirector director = host.GetComponent<AudioDirector>();

            if (director == null)
            {
                director = host.AddComponent<AudioDirector>();
            }

            director.audio = service;
            return director;
        }

        private void OnEnable()
        {
            GameEvents.EnemyDamaged += OnEnemyDamaged;
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.HeroLevelChanged += OnHeroLevelChanged;
            GameEvents.UpgradePurchased += OnUpgradePurchased;
            GameEvents.OfflineRewardsClaimed += OnOfflineRewardsClaimed;
            GameEvents.AscensionCompleted += OnAscensionCompleted;
            GameEvents.PartyWiped += OnPartyWiped;
            GameEvents.BossFailed += OnBossFailed;
        }

        private void OnDisable()
        {
            GameEvents.EnemyDamaged -= OnEnemyDamaged;
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.HeroLevelChanged -= OnHeroLevelChanged;
            GameEvents.UpgradePurchased -= OnUpgradePurchased;
            GameEvents.OfflineRewardsClaimed -= OnOfflineRewardsClaimed;
            GameEvents.AscensionCompleted -= OnAscensionCompleted;
            GameEvents.PartyWiped -= OnPartyWiped;
            GameEvents.BossFailed -= OnBossFailed;
        }

        private void OnEnemyDamaged(EnemyDamagedInfo info)
        {
            if (info.Damage <= 0d)
            {
                return;
            }

            // Crits always play (they are rare and worth hearing); normal hits are throttled.
            if (info.IsCritical)
            {
                Play(SfxCue.Crit);
                return;
            }

            PlayThrottled(SfxCue.Hit, ref lastHitPlayTime, hitMinGapSec);
        }

        private void OnEnemyKilled(string enemyName, double goldReward)
        {
            PlayThrottled(SfxCue.Kill, ref lastKillPlayTime, killMinGapSec);
        }

        private void OnEnemySpawned(string enemyName, double maxHealth, bool isBoss)
        {
            if (isBoss)
            {
                Play(SfxCue.Boss);
            }
        }

        private void OnHeroLevelChanged(int heroIndex, HeroStatType statType, int newLevel)
        {
            Play(SfxCue.LevelUp);
        }

        private void OnUpgradePurchased(int heroIndex, HeroStatType statType, int levels, double goldCost)
        {
            Play(SfxCue.Purchase);
        }

        private void OnOfflineRewardsClaimed(double gold)
        {
            Play(SfxCue.Claim);
        }

        private void OnAscensionCompleted(double tokensEarned, int newHighestStage)
        {
            Play(SfxCue.Ascend);
        }

        private void OnPartyWiped()
        {
            Play(SfxCue.Defeat);
        }

        private void OnBossFailed()
        {
            Play(SfxCue.Defeat);
        }

        private void Play(SfxCue cue)
        {
            audio?.Play(cue);
        }

        /// <summary>Plays a cue at most once per gap, so a burst of combat does not turn into one long buzz.</summary>
        private void PlayThrottled(SfxCue cue, ref float lastPlayTime, float minGapSec)
        {
            float now = Time.unscaledTime;

            if (now - lastPlayTime < minGapSec)
            {
                return;
            }

            lastPlayTime = now;
            Play(cue);
        }
    }
}
