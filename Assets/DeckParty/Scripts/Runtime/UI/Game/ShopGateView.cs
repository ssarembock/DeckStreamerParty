using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Free For All: covers the shop until it opens for everyone. After your stream it says who is still streaming and holds
    /// the Go to the Shop button (you're ready); once everyone is, it shows the 3-2-1 countdown. The shop is first come, first
    /// served, so everyone goes in at the same moment.
    /// </summary>
    public sealed class ShopGateView : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text bodyLabel;
        [SerializeField] internal TMP_Text countdownLabel;
        [SerializeField] internal Button readyButton;
        [SerializeField] internal TMP_Text readyLabel;
        [SerializeField] internal TMP_Text noticeLabel;

        public event Action ReadyClicked;

        int m_Shown = -1;

        public bool IsShowing => root != null && root.activeSelf;
        public RectTransform ReadyButtonRect => readyButton != null ? (RectTransform)readyButton.transform : null;

        void Awake()
        {
            if (readyButton != null) readyButton.onClick.AddListener(() => ReadyClicked?.Invoke());
            if (root != null) root.SetActive(false);
        }

        /// <param name="buttonText">Null hides the button (during the countdown).</param>
        public void Show(string title, string body, string notice, string buttonText)
        {
            bool appearing = !IsShowing;
            if (root != null) root.SetActive(true);
            if (titleLabel != null) titleLabel.text = title;
            if (bodyLabel != null) bodyLabel.text = body;
            if (noticeLabel != null) noticeLabel.text = notice;
            bool button = !string.IsNullOrEmpty(buttonText);
            if (readyButton != null) readyButton.gameObject.SetActive(button);
            if (button && readyLabel != null) readyLabel.text = buttonText;
            if (appearing && root != null) Fx.Play(FxCue.MenuIn, root.transform);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            ClearCountdown();
        }

        /// <summary>The 3-2-1 (call every frame during the countdown). True when a new number just appeared.</summary>
        public bool SetCountdown(float seconds)
        {
            if (countdownLabel == null) return false;
            int number = Mathf.CeilToInt(seconds);
            countdownLabel.gameObject.SetActive(number > 0);
            if (number == m_Shown) return false;
            m_Shown = number;
            if (number <= 0) return false;
            countdownLabel.text = number.ToString();
            Fx.Play(FxCue.BannerSlam, countdownLabel.transform);
            return true;
        }

        public void ClearCountdown()
        {
            m_Shown = -1;
            if (countdownLabel != null) countdownLabel.gameObject.SetActive(false);
        }
    }
}
