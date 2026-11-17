using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>What a multiplayer game is (picked on <see cref="MultiplayerModeScreen"/>).</summary>
    public enum MultiplayerMode
    {
        /// <summary>Online against other players: Take Turns, Free For All or 2v2.</summary>
        OnlineCompetitive = 0,
        /// <summary>Online, two players in one Endless run.</summary>
        OnlineCoop = 1,
        /// <summary>Two players on this computer taking turns (keyboard and mouse, controllers, or both).</summary>
        Couch = 2,
    }

    /// <summary>
    /// Multiplayer's first screen (2026-11-17): three big buttons, Online Competitive, Online Co-Op Endless and Couch
    /// Competitive, styled like the single-player mode screen. Each button's background is the UI Skin's Mode Button entry
    /// and its icon has its own entry (Online Competitive / Online Co-Op / Couch icons).
    /// </summary>
    public sealed class MultiplayerModeScreen : UIScreen
    {
        [SerializeField] internal Button competitiveButton;
        [SerializeField] internal Button coopButton;
        [SerializeField] internal Button couchButton;
        [SerializeField] internal Button backButton;

        MainMenuController m_Menu;

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            competitiveButton.onClick.AddListener(() => m_Menu.StartMultiplayerMode(MultiplayerMode.OnlineCompetitive));
            coopButton.onClick.AddListener(() => m_Menu.StartMultiplayerMode(MultiplayerMode.OnlineCoop));
            couchButton.onClick.AddListener(() => m_Menu.StartMultiplayerMode(MultiplayerMode.Couch));
            backButton.onClick.AddListener(() => m_Menu.ShowMain());
            PadDefaultFocus.Mark(competitiveButton, 1);
        }

        void Update()
        {
            if (!IsVisible || InputHelper.IsTyping || ModalDialog.IsOpen) return;
            if (InputHelper.MenuPressed(HotKey.Escape)) m_Menu.ShowMain();
        }
    }
}
