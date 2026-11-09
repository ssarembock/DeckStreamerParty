using System;
using UnityEngine;

namespace DeckParty
{
    /// <summary>Player preferences persisted in PlayerPrefs.</summary>
    public static class AppSettings
    {
        const string KeyMaster = "dp.volume.master";
        const string KeyMusic = "dp.volume.music";
        const string KeySfx = "dp.volume.sfx";
        const string KeyFastAnim = "dp.fastAnimations";
        const string KeyName = "dp.playerName";
        const string KeyVenue = "dp.venue";
        const string KeyScenario = "dp.lastScenario";
        const string KeyJoinCode = "dp.lastJoinCode";
        const string KeyIp = "dp.lastIp";
        const string KeyPort = "dp.lastPort";
        const string KeyShake = "dp.screenShake";
        const string KeyReduceFlashing = "dp.reduceFlashing";
        const string KeyActivityLog = "dp.activityLog";
        const string KeyStreamer = "dp.streamer";
        const string KeyTurnTimer = "dp.turnTimer";
        const string KeyLobbyVisibility = "dp.lobbyVisibility";

        public const string VenueAuto = "auto";
        public const string VenueEvolving = "evolving";

        public static event Action Changed;

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(KeyMaster, 0.8f);
            set { PlayerPrefs.SetFloat(KeyMaster, Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); Notify(); }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(KeyMusic, 0.6f);
            set { PlayerPrefs.SetFloat(KeyMusic, Mathf.Clamp01(value)); Notify(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(KeySfx, 0.8f);
            set { PlayerPrefs.SetFloat(KeySfx, Mathf.Clamp01(value)); Notify(); }
        }

        public static bool FastAnimations
        {
            get => PlayerPrefs.GetInt(KeyFastAnim, 0) == 1;
            set { PlayerPrefs.SetInt(KeyFastAnim, value ? 1 : 0); Notify(); }
        }

        public static bool Fullscreen
        {
            get => Screen.fullScreen;
            set { Screen.fullScreen = value; Notify(); }
        }

        /// <summary>Multiplier applied to every UI animation duration.</summary>
        public static float AnimationTimeScale => FastAnimations ? 0.5f : 1f;

        /// <summary>Screen shake strength, 0 (off) to 1 (full).</summary>
        public static float ScreenShake
        {
            get => PlayerPrefs.GetFloat(KeyShake, 1f);
            set { PlayerPrefs.SetFloat(KeyShake, Mathf.Clamp01(value)); Notify(); }
        }

        /// <summary>Tones down flashes, strobes and glitches.</summary>
        public static bool ReduceFlashing
        {
            get => PlayerPrefs.GetInt(KeyReduceFlashing, 0) == 1;
            set { PlayerPrefs.SetInt(KeyReduceFlashing, value ? 1 : 0); Notify(); }
        }

        /// <summary>The Activity log (what happened, line by line) in the side panel during streams and in the shop. Off by default.</summary>
        public static bool ShowActivityLog
        {
            get => PlayerPrefs.GetInt(KeyActivityLog, 0) == 1;
            set { PlayerPrefs.SetInt(KeyActivityLog, value ? 1 : 0); Notify(); }
        }

        /// <summary>The last streamer picked on the select screen (a StreamerType, or 255 for Random).</summary>
        public static byte LastStreamer
        {
            get => (byte)Mathf.Clamp(PlayerPrefs.GetInt(KeyStreamer, 0), 0, 255);
            set => PlayerPrefs.SetInt(KeyStreamer, value);
        }

        /// <summary>The host's last turn timer choice in seconds (0 = off).</summary>
        public static int LastTurnTimer
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KeyTurnTimer, 0), 0, 3600);
            set => PlayerPrefs.SetInt(KeyTurnTimer, value);
        }

        public static string PlayerName
        {
            get
            {
                var name = PlayerPrefs.GetString(KeyName, string.Empty);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "Player" + UnityEngine.Random.Range(100, 999);
                    PlayerPrefs.SetString(KeyName, name);
                }
                return name;
            }
            set { PlayerPrefs.SetString(KeyName, SanitizeName(value)); Notify(); }
        }

        public static string VenueId
        {
            get => PlayerPrefs.GetString(KeyVenue, VenueAuto);
            set { PlayerPrefs.SetString(KeyVenue, string.IsNullOrEmpty(value) ? VenueAuto : value); Notify(); }
        }

        public static int LastScenario
        {
            get => PlayerPrefs.GetInt(KeyScenario, 0);
            set => PlayerPrefs.SetInt(KeyScenario, value);
        }

        public static string LastJoinCode
        {
            get => PlayerPrefs.GetString(KeyJoinCode, string.Empty);
            set => PlayerPrefs.SetString(KeyJoinCode, value ?? string.Empty);
        }

        /// <summary>Who could join the last platform lobby this player hosted (a Platforms.LobbyVisibility; -1 = never hosted).</summary>
        public static int LastLobbyVisibility
        {
            get => PlayerPrefs.GetInt(KeyLobbyVisibility, -1);
            set => PlayerPrefs.SetInt(KeyLobbyVisibility, value);
        }

        public static string LastIp
        {
            get => PlayerPrefs.GetString(KeyIp, "127.0.0.1");
            set => PlayerPrefs.SetString(KeyIp, value ?? string.Empty);
        }

        public static ushort LastPort
        {
            get => (ushort)Mathf.Clamp(PlayerPrefs.GetInt(KeyPort, 7777), 1, 65535);
            set => PlayerPrefs.SetInt(KeyPort, value);
        }

        public static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Player";
            name = name.Replace("|", string.Empty).Trim();
            if (name.Length > 16) name = name.Substring(0, 16);
            return name.Length == 0 ? "Player" : name;
        }

        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyOnBoot()
        {
            AudioListener.volume = MasterVolume;
            Application.targetFrameRate = 60;
        }

        static void Notify() => Changed?.Invoke();
    }
}
