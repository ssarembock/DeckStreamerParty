// Steam code only exists when Steamworks.NET is installed (DECKPARTY_STEAMWORKS, a version define in DeckParty.Steam.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_STEAMWORKS && UNITY_STANDALONE
using Steamworks;
using UnityEngine;

namespace DeckParty.Platforms.Steam
{
    /// <summary>
    /// Runs SteamAPI.RunCallbacks every frame (Steam delivers lobby results, invites, avatars and connection changes
    /// only from inside it). Created by <see cref="SteamPlatform"/> once Steam starts; lives for the whole run.
    /// The transport also pumps once at the start of Netcode's EarlyUpdate so connection changes land before messages.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class SteamCallbackPump : MonoBehaviour
    {
        SteamPlatform m_Platform;

        internal static SteamCallbackPump Create(SteamPlatform platform)
        {
            var go = new GameObject("[Steam]");
            DontDestroyOnLoad(go);
            var pump = go.AddComponent<SteamCallbackPump>();
            pump.m_Platform = platform;
            return pump;
        }

        void Update()
        {
            if (!SteamPlatform.Running) return;
            SteamAPI.RunCallbacks();
            m_Platform?.Tick();
        }
    }
}
#endif
