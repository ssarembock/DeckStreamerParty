// Steam code only exists when Steamworks.NET is installed (DECKPARTY_STEAMWORKS, a version define in DeckParty.Steam.asmdef)
// and the build target is Windows, macOS or Linux. Otherwise this file compiles to nothing and the game uses the Default multiplayer.
#if DECKPARTY_STEAMWORKS && UNITY_STANDALONE
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace DeckParty.Platforms.Steam
{
    /// <summary>
    /// Turns a Steam async call (SteamAPICall_t + CallResult) into an awaitable Task. Results arrive inside
    /// SteamAPI.RunCallbacks on the main thread; continuations are queued instead of running inside Steam's dispatcher.
    /// A call Steam never answers gives up after <see cref="TimeoutSeconds"/>.
    /// </summary>
    internal static class SteamCall
    {
        const float TimeoutSeconds = 20f;

        // Keeps each CallResult alive until Steam answers (a collected CallResult never fires).
        static readonly HashSet<object> s_Pending = new HashSet<object>();

        public static async Task<(bool ok, T result)> Run<T>(SteamAPICall_t call) where T : struct
        {
            if (call == SteamAPICall_t.Invalid) return (false, default);

            var done = new TaskCompletionSource<(bool, T)>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callResult = CallResult<T>.Create();
            s_Pending.Add(callResult);
            callResult.Set(call, (value, ioFailure) =>
            {
                s_Pending.Remove(callResult);
                done.TrySetResult((!ioFailure, value));
            });

            var timeout = Task.Delay((int)(TimeoutSeconds * 1000));
            if (await Task.WhenAny(done.Task, timeout) == done.Task) return await done.Task;

            Debug.LogWarning($"[DeckParty] Steam didn't answer {typeof(T).Name} within {TimeoutSeconds} s.");
            callResult.Cancel();
            s_Pending.Remove(callResult);
            return (false, default);
        }
    }
}
#endif
