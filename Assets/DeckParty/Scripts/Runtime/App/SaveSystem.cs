using System;
using System.IO;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty
{
    /// <summary>Single-player autosave. One slot, written after every action.</summary>
    public static class SaveSystem
    {
        [Serializable]
        sealed class SaveEnvelope
        {
            public int version = 1;
            public string venueChoice;
            public string state;
            public string savedAt;
        }

        static string FilePath => Path.Combine(Application.persistentDataPath, "deckparty_save.json");

        public static bool HasSave => File.Exists(FilePath);

        public static void Save(PlayerState state, string venueChoice)
        {
            try
            {
                var envelope = new SaveEnvelope
                {
                    venueChoice = venueChoice,
                    state = Convert.ToBase64String(StateSerializer.Serialize(state)),
                    savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                };
                File.WriteAllText(FilePath, JsonUtility.ToJson(envelope));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeckParty] Could not write save: {e.Message}");
            }
        }

        public static bool TryLoad(out PlayerState state, out string venueChoice)
        {
            state = null;
            venueChoice = AppSettings.VenueAuto;
            if (!HasSave) return false;

            try
            {
                var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(FilePath));
                if (envelope == null || string.IsNullOrEmpty(envelope.state)) return false;
                state = StateSerializer.Deserialize(Convert.FromBase64String(envelope.state));
                venueChoice = string.IsNullOrEmpty(envelope.venueChoice) ? AppSettings.VenueAuto : envelope.venueChoice;
                return !state.IsGameOver;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeckParty] Save file unreadable, ignoring it: {e.Message}");
                return false;
            }
        }

        /// <summary>Short human description for the Continue button, or null.</summary>
        public static string Describe()
        {
            if (!TryLoad(out var state, out _)) return null;
            string text = Loc.Get("menu.save_desc", ("set", ContentText.ScenarioName(state.ScenarioId)), ("night", state.Night), ("total", state.TotalNights));
            return state.Streamer != StreamerType.None ? StreamerVisuals.Name(state.Streamer) + " - " + text : text;
        }

        public static void Delete()
        {
            try
            {
                if (HasSave) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DeckParty] Could not delete save: {e.Message}");
            }
        }
    }
}
