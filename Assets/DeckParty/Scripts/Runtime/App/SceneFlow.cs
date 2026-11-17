using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeckParty
{
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Game = "Game";
    }

    /// <summary>Everything the Game scene needs to start a single-player season.</summary>
    public sealed class SinglePlayerLaunch
    {
        public int ScenarioId;
        public uint Seed;
        public string VenueChoice = AppSettings.VenueAuto;
        public bool ContinueSave;
        /// <summary>The streamer picked on the select screen (a StreamerType; the Random pick is resolved when the game is created).</summary>
        public byte Streamer;
        /// <summary>Classic (the character sets) or Endless.</summary>
        public Core.GameMode Mode = Core.GameMode.Classic;
        /// <summary>Versus the AI: a simple AI plays its own season alongside yours.</summary>
        public bool VersusAi;
        /// <summary>Versus the AI: the AI's streamer (a StreamerType or the Random pick).</summary>
        public byte OpponentStreamer;
        /// <summary>
        /// Couch Competitive (2026-11-17): two players on this computer take turns (1v1). <see cref="Streamer"/> is Player 1's,
        /// <see cref="OpponentStreamer"/> Player 2's. Not saved.
        /// </summary>
        public bool Couch;
    }

    /// <summary>Local (non-networked) scene transitions plus hand-off data between scenes.</summary>
    public static class SceneFlow
    {
        /// <summary>Launch request consumed by the Game scene. Null means "quick start" (pressing Play in the Game scene).</summary>
        public static SinglePlayerLaunch PendingLaunch;

        /// <summary>Message the main menu shows on arrival (e.g. "Host left the game").</summary>
        public static string PendingMessage;

        /// <summary>When true the main menu opens straight on the multiplayer screen.</summary>
        public static bool OpenMultiplayerOnMenu;

        public static void StartSinglePlayer(SinglePlayerLaunch launch)
        {
            if (!CanLoad(SceneNames.Game)) return;
            PendingLaunch = launch;
            Platforms.OnlinePlatforms.SetPresence(Platforms.PresenceState.SinglePlayer);
            SceneManager.LoadScene(SceneNames.Game);
        }

        /// <summary>True if the scene is in the build list; otherwise explains how to fix it instead of failing cryptically.</summary>
        public static bool CanLoad(string sceneName)
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName)) return true;

            const string fix = "In the Unity editor run Deck Party > Setup > Build Everything. It generates the scenes and adds MainMenu and Game to File > Build Profiles > Scene List."; // developer-only message, not localized
            Debug.LogError($"[DeckParty] Scene '{sceneName}' isn't in the build scene list. {fix}");
            UI.ModalDialog.Show("Scene missing", $"The '{sceneName}' scene isn't in the build list yet.\n\n{fix}", null, "OK", null);
            return false;
        }

        public static void GoToMainMenu(string message = null, bool openMultiplayer = false)
        {
            if (!string.IsNullOrEmpty(message)) PendingMessage = message;
            OpenMultiplayerOnMenu = openMultiplayer;
            bool inSession = Networking.NetworkService.HasInstance && Networking.NetworkService.Instance.IsActive;
            if (!inSession) Platforms.OnlinePlatforms.SetPresence(Platforms.PresenceState.Menu);
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        public static string TakePendingMessage()
        {
            var message = PendingMessage;
            PendingMessage = null;
            return message;
        }
    }
}
