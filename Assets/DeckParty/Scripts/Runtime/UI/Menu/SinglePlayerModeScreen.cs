using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>What a single-player season is (picked on <see cref="SinglePlayerModeScreen"/>).</summary>
    public enum SinglePlayerMode
    {
        /// <summary>The character sets ("puzzles"): win with 4 Trophies in one stream.</summary>
        Puzzles = 0,
        /// <summary>Last as many streams as you can; Trophies buy more streams and more shop viewers.</summary>
        Endless = 1,
        /// <summary>A season against a simple AI streamer: the first to win wins.</summary>
        VersusAi = 2,
        /// <summary>
        /// Couch Competitive (2026-11-17, picked on the multiplayer mode screen): two players on this computer, 1v1, taking
        /// turns. It shares the character set screen; the first to win wins.
        /// </summary>
        Couch = 3,
    }

    /// <summary>
    /// Single Player's first screen (2026-11-16): three big buttons, Puzzles (the character sets), Endless and Versus the
    /// AI. Each button's background and icon come from the UI Skin (Mode Button, Puzzles / Endless / Versus the AI icons).
    /// </summary>
    public sealed class SinglePlayerModeScreen : UIScreen
    {
        [SerializeField] internal Button puzzlesButton;
        [SerializeField] internal Button endlessButton;
        [SerializeField] internal Button versusButton;
        [SerializeField] internal Button backButton;
        [Tooltip("Optional: a line under the Endless button with your longest run.")]
        [SerializeField] internal TMP_Text endlessBestLabel;
        [Tooltip("Optional: a line under the Puzzles button with the character sets cleared.")]
        [SerializeField] internal TMP_Text puzzlesProgressLabel;

        MainMenuController m_Menu;

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            puzzlesButton.onClick.AddListener(() => m_Menu.StartSinglePlayerMode(SinglePlayerMode.Puzzles));
            endlessButton.onClick.AddListener(() => m_Menu.StartSinglePlayerMode(SinglePlayerMode.Endless));
            versusButton.onClick.AddListener(() => m_Menu.StartSinglePlayerMode(SinglePlayerMode.VersusAi));
            PadDefaultFocus.Mark(puzzlesButton, 1);
            backButton.onClick.AddListener(() => m_Menu.ShowMain());
            Loc.Changed += () =>
            {
                if (IsVisible) OnShow();
            };
        }

        protected override void OnShow()
        {
            if (endlessBestLabel != null)
                endlessBestLabel.text = ProgressStore.BestEndlessRun > 0
                    ? Loc.Get("modes.endless_best", "count", ProgressStore.BestEndlessRun)
                    : Loc.Get("modes.endless_none");
            if (puzzlesProgressLabel != null)
                puzzlesProgressLabel.text = Loc.Get("modes.puzzles_progress", "count", ProgressStore.ClearedCount, "total", ScenarioCatalog.PresetCount);
        }

        void Update()
        {
            if (!IsVisible || InputHelper.IsTyping || ModalDialog.IsOpen) return;
            if (InputHelper.MenuPressed(HotKey.Escape)) m_Menu.ShowMain();
        }
    }
}
