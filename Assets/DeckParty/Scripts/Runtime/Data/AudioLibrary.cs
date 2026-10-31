using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    public enum Sfx
    {
        Click = 0,
        Hover = 1,
        DoorKnock = 2,
        GuestArrive = 3,
        StarArrive = 4,
        Boot = 5,
        Ability = 6,
        Coin = 7,
        Purchase = 8,
        Expand = 9,
        PartySuccess = 10,
        Raid = 11,
        FireMarshal = 12,
        Victory = 13,
        Defeat = 14,
        Error = 15,
        Notify = 16,
        Peek = 17,
    }

    [Serializable]
    public sealed class SfxEntry
    {
        [HideInInspector] public string name;
        public Sfx sfx;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    /// <summary>Optional audio. Any empty slot falls back to a small synthesized placeholder sound.</summary>
    [CreateAssetMenu(menuName = "Deck Party/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        public AudioClip menuMusic;
        public AudioClip partyMusic;
        public AudioClip planningMusic;
        public List<SfxEntry> sounds = new List<SfxEntry>();

        public SfxEntry Get(Sfx sfx) => sounds.Find(s => s != null && s.sfx == sfx);

        public void EnsureAllSlots()
        {
            foreach (Sfx sfx in Enum.GetValues(typeof(Sfx)))
            {
                var existing = sounds.Find(s => s != null && s.sfx == sfx);
                if (existing == null) sounds.Add(existing = new SfxEntry { sfx = sfx });
                existing.name = sfx.ToString();
            }
            sounds.Sort((a, b) => a.sfx.CompareTo(b.sfx));
        }

        void OnValidate()
        {
            foreach (var entry in sounds)
                if (entry != null) entry.name = entry.sfx.ToString();
        }
    }
}
