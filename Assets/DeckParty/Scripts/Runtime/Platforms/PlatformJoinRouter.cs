using DeckParty.Networking;
using DeckParty.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeckParty.Platforms
{
    /// <summary>
    /// Handles a Steam invite (or Join Game in the friends list) that arrives while the player is busy: in a match, in
    /// another lobby, or streaming in single player. It asks first, then leaves and lets the main menu join the friend's
    /// lobby (<see cref="MainMenuController"/> picks up <see cref="OnlinePlatforms.PendingJoin"/>). In the menus with
    /// nothing running, the menu joins straight away and this stays quiet.
    /// </summary>
    public static class PlatformJoinRouter
    {
        static bool s_Asking;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            s_Asking = false;
            OnlinePlatforms.PendingJoinChanged -= OnPendingJoin;
            OnlinePlatforms.PendingJoinChanged += OnPendingJoin;
        }

        static void OnPendingJoin()
        {
            string lobby = OnlinePlatforms.PendingJoin;
            if (string.IsNullOrEmpty(lobby) || s_Asking) return;

            bool inSession = NetworkService.HasInstance && NetworkService.Instance.IsActive;
            if (inSession && NetworkService.Instance.PlatformLobbyId == lobby)
            {
                // Already in that lobby.
                OnlinePlatforms.ClearPendingJoin();
                return;
            }

            bool inGameScene = SceneManager.GetActiveScene().name == SceneNames.Game;
            // In the menus with nothing running: the main menu joins right away.
            if (!inSession && !inGameScene) return;

            s_Asking = true;
            string body = Loc.Get(inSession ? "platform.join_leave_match" : "platform.join_leave_stream");
            ModalDialog.Show(Loc.Get("platform.join_title"), body, () =>
            {
                s_Asking = false;
                if (NetworkService.HasInstance && NetworkService.Instance.IsActive) NetworkService.Instance.Leave();
                // The main menu joins the waiting lobby when it opens (or when the old session has closed).
                if (SceneManager.GetActiveScene().name != SceneNames.MainMenu) SceneFlow.GoToMainMenu(null, openMultiplayer: true);
            }, Loc.Get("platform.join_confirm"), Loc.Get("lobby.stay"), () =>
            {
                s_Asking = false;
                OnlinePlatforms.ClearPendingJoin();
            });
        }
    }
}
