using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace DeckParty.Juice
{
    /// <summary>
    /// Plays Deck Party's polish moments with Feel. Each cue in the FX Library points at a feedback prefab (an MMF_Player
    /// whose feedbacks target their parent). The first time a cue plays on a card or panel, the prefab is instantiated
    /// as a child of it and reused after that. Everything runs on unscaled time, scaled by Fast Animations.
    /// Remove this assembly (and Feel) and the game falls back to its built-in tweens automatically.
    /// </summary>
    public sealed class FeelFxBackend : IFxBackend
    {
        readonly Dictionary<(int target, FxCue cue), MMF_Player> m_Players = new Dictionary<(int, FxCue), MMF_Player>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register() => Fx.Backend = new FeelFxBackend();

        public bool Play(FxCue cue, Transform target, float intensity)
        {
            var entry = FxLibrary.Current.Get(cue);
            if (entry == null || entry.feedback == null || target == null) return false;

            var key = (target.GetInstanceID(), cue);
            if (!m_Players.TryGetValue(key, out var player) || player == null)
            {
                var instance = Object.Instantiate(entry.feedback, target, false);
                instance.name = entry.feedback.name;
                player = instance.GetComponent<MMF_Player>();
                if (player == null)
                {
                    Object.Destroy(instance);
                    return false;
                }
                player.ForceTimescaleMode = true;
                player.ForcedTimescaleMode = TimescaleModes.Unscaled;
                player.Initialization();
                m_Players[key] = player;
            }

            player.DurationMultiplier = Fx.DurationScale;
            player.PlayFeedbacks(target.position, Mathf.Max(0.05f, intensity));
            return true;
        }
    }
}
