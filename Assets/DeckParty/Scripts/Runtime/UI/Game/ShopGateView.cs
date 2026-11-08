using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Free For All: covers the shop until it opens for everyone. After your stream it says who is still streaming ("Waiting
    /// for other streamers"); once every stream is over it shows every player's stream summary (one column each) and holds
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

        [Header("Stream summary (once every stream of the round is over)")]
        [Tooltip("Holds one column per player and the grey lines between them (a horizontal layout).")]
        [SerializeField] internal RectTransform columnsRoot;
        [SerializeField] internal RecapColumnView columnTemplate;
        [SerializeField] internal GameObject dividerTemplate;

        readonly System.Collections.Generic.List<RecapColumnView> m_Columns = new System.Collections.Generic.List<RecapColumnView>();
        readonly System.Collections.Generic.List<GameObject> m_Dividers = new System.Collections.Generic.List<GameObject>();

        public event Action ReadyClicked;

        int m_Shown = -1;

        public bool IsShowing => root != null && root.activeSelf;
        public RectTransform ReadyButtonRect => readyButton != null ? (RectTransform)readyButton.transform : null;

        void Awake()
        {
            if (readyButton != null) readyButton.onClick.AddListener(() => ReadyClicked?.Invoke());
            if (columnTemplate != null) columnTemplate.gameObject.SetActive(false);
            if (dividerTemplate != null) dividerTemplate.SetActive(false);
            if (root != null) root.SetActive(false);
        }

        /// <param name="buttonText">Null hides the button (while others still stream, and during the countdown).</param>
        /// <param name="streams">Every player's stream summary, once all streams of the round are over (null hides the columns).</param>
        public void Show(string title, string body, string notice, string buttonText, System.Collections.Generic.IList<RecapEntry> streams = null)
        {
            if (columnsRoot != null)
            {
                bool columns = streams != null && streams.Count > 0;
                columnsRoot.gameObject.SetActive(columns);
                if (columns) RecapColumns.Fill(columnsRoot, columnTemplate, dividerTemplate, m_Columns, m_Dividers, streams);
            }
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
