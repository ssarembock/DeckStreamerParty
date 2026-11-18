// Epic code only exists when the EOS Plugin for Unity is installed (DECKPARTY_EOS, a version define in DeckParty.Epic.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_EOS && UNITY_STANDALONE
using UnityEngine;

namespace DeckParty.Platforms.Epic
{
    /// <summary>
    /// Gives <see cref="EpicPlatform"/> a frame tick (batched lobby updates, throttled friends-list events). The EOS SDK
    /// itself is ticked by the plugin's EOSManager.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class EpicPump : MonoBehaviour
    {
        EpicPlatform m_Platform;

        internal static EpicPump Create(EpicPlatform platform)
        {
            var go = new GameObject("[EpicPump]");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            var pump = go.AddComponent<EpicPump>();
            pump.m_Platform = platform;
            return pump;
        }

        void Update() => m_Platform?.Tick();
    }
}
#endif
