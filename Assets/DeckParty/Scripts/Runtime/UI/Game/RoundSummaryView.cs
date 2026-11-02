using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The round summary (Free For All, where nobody can watch anyone else): what every player did this round and the one
    /// before (stream results, community payouts, recruits, Sub Cap raises, powers) and what is waiting for their next stream
    /// (rivals' raids, Doomsdayers, Bots). Opened from the bar above the board.
    /// </summary>
    public sealed class RoundSummaryView : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text bodyLabel;
        [SerializeField] internal ScrollRect scroll;
        [SerializeField] internal Button closeButton;

        Func<string> m_Provider;

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (root != null) root.SetActive(false);
        }

        /// <param name="provider">Builds the summary text (called again whenever the round changes while it's open).</param>
        public void Toggle(Func<string> provider)
        {
            if (IsOpen) Close();
            else Open(provider);
        }

        public void Open(Func<string> provider)
        {
            m_Provider = provider;
            if (root == null) return;
            root.SetActive(true);
            Refresh();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            Fx.Play(FxCue.MenuIn, root.transform);
            AudioService.Play(Sfx.Click);
        }

        public void Refresh()
        {
            if (!IsOpen || m_Provider == null) return;
            if (titleLabel != null) titleLabel.text = Loc.Get("summary.title");
            if (bodyLabel != null) bodyLabel.text = m_Provider();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
