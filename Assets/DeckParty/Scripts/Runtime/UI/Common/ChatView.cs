using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Multiplayer chat box (lobby and in-game). Also shows system headlines.</summary>
    public sealed class ChatView : MonoBehaviour
    {
        [SerializeField] internal EventLogView log;
        [SerializeField] internal TMP_InputField input;
        [SerializeField] internal Button sendButton;

        DeckPartyNetSession m_Session;
        bool m_Hooked;

        void Awake() => Hook();

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            sendButton.onClick.AddListener(Send);
            input.onSubmit.AddListener(_ => Send());
        }

        public void Bind(DeckPartyNetSession session)
        {
            Hook();
            Unbind();
            m_Session = session;
            log.Clear();
            if (m_Session == null) return;
            foreach (var message in m_Session.ChatHistory) log.Add(Format(message));
            m_Session.ChatReceived += OnChat;
        }

        public void Unbind()
        {
            if (m_Session != null) m_Session.ChatReceived -= OnChat;
            m_Session = null;
        }

        void OnDestroy() => Unbind();

        void OnChat(ChatMessage message) => log.Add(Format(message));

        static string Format(ChatMessage message)
        {
            if (message.IsSystem) return $"<color={UIPalette.Tag(UIPalette.TextDim)}><i>{Escape(message.Text)}</i></color>";
            return $"<b><color={UIPalette.Tag(UIPalette.PlayerColor(message.ColorIndex))}>{Escape(message.Sender)}:</color></b> {Escape(message.Text)}";
        }

        static string Escape(string text) =>
            "<noparse>" + (text ?? string.Empty).Replace("noparse", "no parse") + "</noparse>";

        void Send()
        {
            if (m_Session == null || !m_Session.IsSpawned) return;
            var text = input.text;
            if (string.IsNullOrWhiteSpace(text)) return;
            m_Session.SendChat(text);
            input.text = string.Empty;
            input.ActivateInputField();
        }
    }
}
