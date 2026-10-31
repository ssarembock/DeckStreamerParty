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
