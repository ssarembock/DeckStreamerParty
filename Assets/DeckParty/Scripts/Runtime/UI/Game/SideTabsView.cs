using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The side panel's feed, above the facecam: an Activity tab (the Stream Log) and a Chat tab (multiplayer messages and
    /// headlines). The tab you're not looking at counts the lines that arrived since. Single player only has Activity.
    /// </summary>
    public sealed class SideTabsView : MonoBehaviour
    {
        public enum Tab
        {
            Activity,
            Chat,
        }

        [Header("Activity")]
        [SerializeField] internal Button activityTab;
        [SerializeField] internal TMP_Text activityLabel;
        [SerializeField] internal GameObject activityUnderline;
        [SerializeField] internal GameObject activityPage;
        [SerializeField] internal EventLogView activityLog;

        [Header("Chat")]
        [SerializeField] internal Button chatTab;
        [SerializeField] internal TMP_Text chatLabel;
        [SerializeField] internal GameObject chatUnderline;
        [SerializeField] internal GameObject chatPage;
        [SerializeField] internal EventLogView chatLog;

        Tab m_Tab;
        bool m_ChatAvailable = true;
        int m_UnreadActivity;
        int m_UnreadChat;
        bool m_Hooked;

        public Tab Current => m_Tab;

        void Awake()
        {
            Hook();
            Select(m_Tab, false);
        }

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            if (activityTab != null) activityTab.onClick.AddListener(() => Select(Tab.Activity, true));
            if (chatTab != null) chatTab.onClick.AddListener(() => Select(Tab.Chat, true));
            if (activityLog != null) activityLog.LineAdded += OnActivityLine;
            if (chatLog != null) chatLog.LineAdded += OnChatLine;
            Loc.Changed += Refresh;
        }

        void OnDestroy()
        {
            Loc.Changed -= Refresh;
            if (activityLog != null) activityLog.LineAdded -= OnActivityLine;
            if (chatLog != null) chatLog.LineAdded -= OnChatLine;
        }

        /// <summary>Shows or hides the Chat tab (multiplayer only).</summary>
        public void SetChatAvailable(bool available)
        {
            Hook();
            m_ChatAvailable = available;
            if (chatTab != null) chatTab.gameObject.SetActive(available);
            Select(available ? m_Tab : Tab.Activity, false);
        }

        public void Select(Tab tab, bool feedback)
        {
            Hook();
            if (tab == Tab.Chat && !m_ChatAvailable) tab = Tab.Activity;
            bool changed = tab != m_Tab;
            m_Tab = tab;
            if (activityPage != null) activityPage.SetActive(tab == Tab.Activity);
            if (chatPage != null) chatPage.SetActive(tab == Tab.Chat);
            MarkRead(tab);
            if (feedback && changed)
            {
                AudioService.Play(Sfx.Click);
                var page = tab == Tab.Activity ? activityPage : chatPage;
                if (page != null) Fx.Play(FxCue.MenuIn, page.transform, 0.5f);
            }
        }

        /// <summary>Clears a tab's unread count (also used after a log is refilled, which isn't news).</summary>
        public void MarkRead(Tab tab)
        {
            if (tab == Tab.Activity) m_UnreadActivity = 0;
            else m_UnreadChat = 0;
            Refresh();
        }

        void OnActivityLine()
        {
            if (m_Tab == Tab.Activity) return;
            m_UnreadActivity++;
            Refresh();
        }

        void OnChatLine()
        {
            if (m_Tab == Tab.Chat || !m_ChatAvailable) return;
            m_UnreadChat++;
            Refresh();
            if (chatTab != null) Fx.Play(FxCue.CounterPunch, chatTab.transform, 0.6f);
        }

        void Refresh()
        {
            Style(activityTab, activityLabel, activityUnderline, Loc.Get("tab.activity"), m_Tab == Tab.Activity, m_UnreadActivity);
            Style(chatTab, chatLabel, chatUnderline, Loc.Get("tab.chat"), m_Tab == Tab.Chat, m_UnreadChat);
        }

        static void Style(Button tab, TMP_Text label, GameObject underline, string caption, bool selected, int unread)
        {
            if (tab != null && tab.targetGraphic != null) tab.targetGraphic.color = selected ? UIPalette.PanelAlt : UIPalette.PanelDeep;
            if (underline != null) underline.SetActive(selected);
            if (label == null) return;
            label.color = selected ? UIPalette.Text : UIPalette.TextDim;
            string count = unread > 99 ? "99+" : unread.ToString();
            label.text = unread > 0 && !selected ? $"{caption}  <color={UIPalette.Tag(UIPalette.Pink)}>{count}</color>" : caption;
        }
    }
}
