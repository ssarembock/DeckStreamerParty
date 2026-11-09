using System;
using System.Collections.Generic;
using DeckParty.Networking;
using DeckParty.Platforms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Find Games (online platforms only): public lobbies that run this version and haven't started, best ping first,
    /// plus a box to join by lobby id. Lives on the Multiplayer screen; raises <see cref="JoinRequested"/> and lets the
    /// screen do the joining.
    /// </summary>
    public sealed class LobbyBrowserView : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text statusLabel;
        [SerializeField] internal RectTransform list;
        [Tooltip("An inactive row under the list, copied for each game.")]
        [SerializeField] internal LobbyBrowserRowView rowTemplate;
        [SerializeField] internal TMP_InputField lobbyIdInput;
        [SerializeField] internal Button joinIdButton;
        [SerializeField] internal Button refreshButton;
        [SerializeField] internal Button closeButton;

        readonly List<LobbyBrowserRowView> m_Rows = new List<LobbyBrowserRowView>();
        bool m_Searching;
        bool m_Interactable = true;

        /// <summary>The player picked a game (or typed a lobby id).</summary>
        public event Action<string> JoinRequested;

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (root != null) root.SetActive(false);
            refreshButton.onClick.AddListener(Refresh);
            closeButton.onClick.AddListener(Close);
            joinIdButton.onClick.AddListener(JoinById);
            lobbyIdInput.onSubmit.AddListener(_ => JoinById());
        }

        public void Open()
        {
            root.SetActive(true);
            lobbyIdInput.text = string.Empty;
            Refresh();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }

        /// <summary>Off while a join is running.</summary>
        public void SetInteractable(bool interactable)
        {
            m_Interactable = interactable;
            joinIdButton.interactable = interactable;
            refreshButton.interactable = interactable && !m_Searching;
            foreach (var row in m_Rows) row.Interactable = interactable;
        }

        public async void Refresh()
        {
            if (m_Searching) return;
            m_Searching = true;
            refreshButton.interactable = false;
            statusLabel.text = Loc.Get("platform.searching");

            var result = await NetworkService.Instance.FindPlatformLobbiesAsync();
            if (this == null) return;
            m_Searching = false;
            refreshButton.interactable = m_Interactable;

            if (!result.Ok)
            {
                Fill(null);
                statusLabel.text = $"<color=#FF5A5A>{Loc.Error(result.Error)}</color>";
                return;
            }

            var lobbies = result.Value;
            lobbies.Sort((a, b) =>
            {
                int pingA = a.PingMs < 0 ? int.MaxValue : a.PingMs, pingB = b.PingMs < 0 ? int.MaxValue : b.PingMs;
                return pingA != pingB ? pingA.CompareTo(pingB) : b.Members.CompareTo(a.Members);
            });
            Fill(lobbies);
            statusLabel.text = lobbies.Count == 0 ? Loc.Get("platform.no_games") : Loc.Get("platform.games_found", "count", lobbies.Count);
        }

        void Fill(List<LobbyListing> lobbies)
        {
            int count = lobbies?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                if (i >= m_Rows.Count)
                {
                    var row = Instantiate(rowTemplate, list);
                    row.JoinClicked += id => JoinRequested?.Invoke(id);
                    m_Rows.Add(row);
                }
                m_Rows[i].gameObject.SetActive(true);
                m_Rows[i].Set(lobbies[i]);
                m_Rows[i].Interactable = m_Interactable;
            }
            for (int i = count; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);
        }

        void JoinById()
        {
            string id = lobbyIdInput.text.Trim();
            if (id.Length == 0)
            {
                statusLabel.text = $"<color=#FF5A5A>{Loc.Error("platform.enter_lobby")}</color>";
                return;
            }
            JoinRequested?.Invoke(id);
        }
    }
}
