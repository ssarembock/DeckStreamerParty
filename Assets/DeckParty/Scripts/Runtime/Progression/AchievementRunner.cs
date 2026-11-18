using DeckParty.Core;
using DeckParty.Platforms;
using DeckParty.UI;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Keeps achievements running in the background for the whole session: checks them once at start-up (an achievement
    /// added later may already be earned), shows a toast for every unlock (achievement, streamer, emote), syncs with the
    /// store when it signs in and retries anything that couldn't be sent.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class AchievementRunner : MonoBehaviour
    {
        const float RetrySeconds = 30f;

        float m_NextRetry;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (FindFirstObjectByType<AchievementRunner>() != null) return;
            var go = new GameObject("[Achievements]");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            go.AddComponent<AchievementRunner>();
        }

        void Awake()
        {
            Achievements.Unlocked += OnAchievementUnlocked;
            Unlocks.StreamerUnlocked += OnStreamerUnlocked;
            Unlocks.EmoteUnlocked += OnEmoteUnlocked;
            OnlinePlatforms.StatusChanged += PlatformAchievementSync.OnPlatformStatusChanged;
        }

        void Start()
        {
            Achievements.Evaluate();
            PlatformAchievementSync.OnPlatformStatusChanged();
        }

        void OnDestroy()
        {
            Achievements.Unlocked -= OnAchievementUnlocked;
            Unlocks.StreamerUnlocked -= OnStreamerUnlocked;
            Unlocks.EmoteUnlocked -= OnEmoteUnlocked;
            OnlinePlatforms.StatusChanged -= PlatformAchievementSync.OnPlatformStatusChanged;
        }

        void Update()
        {
            if (Time.unscaledTime < m_NextRetry) return;
            m_NextRetry = Time.unscaledTime + RetrySeconds;
            if (PlatformAchievementSync.PendingCount > 0) PlatformAchievementSync.Flush();
        }

        static void OnAchievementUnlocked(AchievementDefinition definition) =>
            ToastView.Show(Loc.Get("achievement.unlocked", "name", Achievements.Name(definition)), ToastKind.Success);

        static void OnStreamerUnlocked(StreamerType type) =>
            ToastView.Show(Loc.Get("unlock.streamer", "name", StreamerVisuals.Name(type)), ToastKind.Success);

        static void OnEmoteUnlocked(int index) =>
            ToastView.Show(Loc.Get("unlock.emote"), ToastKind.Success);
    }
}
