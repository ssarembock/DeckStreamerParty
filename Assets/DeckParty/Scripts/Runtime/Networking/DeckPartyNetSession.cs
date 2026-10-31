using System;
using System.Collections;
using System.Collections.Generic;
using DeckParty.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeckParty.Networking
{
    /// <summary>
    /// The networked heart of a multiplayer match. Spawned by the host when a party is created and kept
    /// alive across scene loads. The host runs every player's PartyEngine; clients send commands and
    /// receive authoritative snapshots of every player's season (it's an open-information game), plus a
    /// public summary of everyone in the player list.
    /// </summary>
    [DisallowMultipleComponent]
    public class DeckPartyNetSession : NetworkBehaviour
    {
        public const ulong NoPlayer = ulong.MaxValue;

        /// <summary>Take Turns: how long a finished season (win or last night) stays on screen before the turn passes.</summary>
        const float GameOverTurnDelay = 2.5f;

        public static DeckPartyNetSession Instance { get; private set; }
        public static event Action<DeckPartyNetSession> Spawned;
        public static event Action Despawned;

        NetworkList<NetPlayer> m_Players;
        readonly NetworkVariable<MatchSettings> m_Settings = new NetworkVariable<MatchSettings>(MatchSettings.Default);
        readonly NetworkVariable<MatchPhase> m_Phase = new NetworkVariable<MatchPhase>(MatchPhase.Lobby);
        readonly NetworkVariable<ulong> m_ActivePlayer = new NetworkVariable<ulong>(NoPlayer);

        // Client-side view: the latest snapshot of every player's season.
        readonly Dictionary<ulong, PlayerState> m_States = new Dictionary<ulong, PlayerState>();
        public readonly List<ChatMessage> ChatHistory = new List<ChatMessage>();

        // Server-side simulation
        readonly Dictionary<ulong, PlayerState> m_Games = new Dictionary<ulong, PlayerState>();
        // Synchronized Nights: ready for the next night. Take Turns: finished their turn this round.
        readonly HashSet<ulong> m_ReadyNextNight = new HashSet<ulong>();
        readonly List<GameEvent> m_Scratch = new List<GameEvent>();
        Coroutine m_TurnRoutine;
        bool m_TurnEnding;

        public event Action PlayersChanged;
        public event Action SettingsChanged;
        public event Action<MatchPhase> PhaseChanged;
        /// <summary>Any player's new snapshot (owner client id, state, the events that produced it).</summary>
        public event Action<ulong, PlayerState, IReadOnlyList<GameEvent>> PlayerStateReceived;
        /// <summary>The local player's new snapshot.</summary>
        public event Action<PlayerState, IReadOnlyList<GameEvent>> StateReceived;
        /// <summary>Take Turns: whose turn it is changed (<see cref="NoPlayer"/> when nobody's).</summary>
        public event Action<ulong> ActivePlayerChanged;
        public event Action<string> CommandRejected;
        public event Action<ChatMessage> ChatReceived;
        public event Action<string> HeadlineReceived;

        public NetworkList<NetPlayer> Players => m_Players;
        public MatchSettings Settings => m_Settings.Value;
        public MatchPhase Phase => m_Phase.Value;
        public ulong ActivePlayer => m_ActivePlayer.Value;
        public bool TakingTurns => Settings.Mode == MatchMode.Turns;
        public PlayerState LocalState => GetState(LocalClientId);
        public ulong LocalClientId => NetworkManager != null ? NetworkManager.LocalClientId : 0;

        public PlayerState GetState(ulong clientId) => m_States.TryGetValue(clientId, out var state) ? state : null;

        void Awake()
        {
            m_Players = new NetworkList<NetPlayer>();
        }

        public override void OnNetworkSpawn()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            m_Players.OnListChanged += OnPlayersListChanged;
            m_Settings.OnValueChanged += OnSettingsValueChanged;
            m_Phase.OnValueChanged += OnPhaseValueChanged;
            m_ActivePlayer.OnValueChanged += OnActivePlayerValueChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += ServerOnClientConnected;
                NetworkManager.OnClientDisconnectCallback += ServerOnClientDisconnected;
                foreach (var id in NetworkManager.ConnectedClientsIds) ServerAddPlayer(id);
            }

            Raise(Spawned, this);
        }

        public override void OnNetworkDespawn()
        {
            m_Players.OnListChanged -= OnPlayersListChanged;
            m_Settings.OnValueChanged -= OnSettingsValueChanged;
            m_Phase.OnValueChanged -= OnPhaseValueChanged;
            m_ActivePlayer.OnValueChanged -= OnActivePlayerValueChanged;
            StopTurnTimer();

            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= ServerOnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= ServerOnClientDisconnected;
            }

            if (Instance == this) Instance = null;
            Raise(Despawned);
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Raise(Despawned);
            }
            base.OnDestroy();
        }

        void OnPlayersListChanged(NetworkListEvent<NetPlayer> change) => Raise(PlayersChanged);
        void OnSettingsValueChanged(MatchSettings previous, MatchSettings current) => Raise(SettingsChanged);
        void OnActivePlayerValueChanged(ulong previous, ulong current) => Raise(ActivePlayerChanged, current);

        void OnPhaseValueChanged(MatchPhase previous, MatchPhase current)
        {
            if (current == MatchPhase.Lobby) m_States.Clear();
            Raise(PhaseChanged, current);
        }

        // ------------------------------------------------------------------ safe event raising

        // These events fire from inside network callbacks (RPC handlers, NetworkList updates) that also run the
        // host's simulation. A throwing UI listener must never abort that work, so each listener runs in isolation.

        static void Raise(Action handlers)
        {
            if (handlers == null) return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try { ((Action)handler)(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        static void Raise<T>(Action<T> handlers, T arg)
        {
            if (handlers == null) return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try { ((Action<T>)handler)(arg); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        static void Raise<T1, T2>(Action<T1, T2> handlers, T1 a, T2 b)
        {
            if (handlers == null) return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try { ((Action<T1, T2>)handler)(a, b); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        static void Raise<T1, T2, T3>(Action<T1, T2, T3> handlers, T1 a, T2 b, T3 c)
        {
            if (handlers == null) return;
            foreach (var handler in handlers.GetInvocationList())
            {
                try { ((Action<T1, T2, T3>)handler)(a, b, c); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        // ================================================================== queries

        public int IndexOfPlayer(ulong clientId)
        {
            for (int i = 0; i < m_Players.Count; i++)
                if (m_Players[i].ClientId == clientId) return i;
            return -1;
        }

        public bool TryGetPlayer(ulong clientId, out NetPlayer player)
        {
            int index = IndexOfPlayer(clientId);
            player = index >= 0 ? m_Players[index] : default;
            return index >= 0;
        }

        public bool TryGetLocalPlayer(out NetPlayer player) => TryGetPlayer(LocalClientId, out player);

        public List<NetPlayer> PlayersSnapshot()
        {
            var list = new List<NetPlayer>(m_Players.Count);
            for (int i = 0; i < m_Players.Count; i++) list.Add(m_Players[i]);
            return list;
        }

        public bool IsHostPlayer(ulong clientId) => clientId == NetworkManager.ServerClientId;

        public string PlayerName(ulong clientId) => TryGetPlayer(clientId, out var p) ? p.DisplayName : "Someone";

        /// <summary>Take Turns: who plays after the current player this round (null when the round ends with them).</summary>
        public string NextTurnName()
        {
            ulong active = ActivePlayer;
            int start = IndexOfPlayer(active);
            for (int i = start + 1; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (p.Connected && !p.Finished && !p.ReadyNextNight) return p.DisplayName;
            }
            return null;
        }

        // ================================================================== client API

        public void SetLobbyReady(bool ready) => SetLobbyReadyRpc(ready);
        public void SubmitCommand(GameCommand command) => SubmitCommandRpc(NetCommand.From(command));
        public void RequestState() => RequestStateRpc();
        public void CancelNextNightReady() => CancelNextNightRpc();

        public void SendChat(string text)
        {
            text = (text ?? string.Empty).Trim();
            if (text.Length == 0) return;
            if (text.Length > 100) text = text.Substring(0, 100);
            SendChatRpc(NetStrings.Fs128(text));
        }

        // ================================================================== server API

        public void ServerSetSettings(MatchSettings settings)
        {
            if (!IsServer || Phase != MatchPhase.Lobby) return;
            settings.Nights = (byte)Mathf.Clamp(settings.Nights, 5, 40);
            if (!settings.Equals(m_Settings.Value)) m_Settings.Value = settings;
        }

        public bool ServerCanStart(out string reason)
        {
            reason = null;
            if (!IsServer) { reason = "Only the host can start."; return false; }
            if (Phase != MatchPhase.Lobby) { reason = "The match already started."; return false; }

            int waiting = 0;
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (!p.Connected || IsHostPlayer(p.ClientId)) continue;
                if (!p.LobbyReady) waiting++;
            }
            if (waiting > 0)
            {
                reason = waiting == 1 ? "Waiting for 1 player to ready up." : $"Waiting for {waiting} players to ready up.";
                return false;
            }
            return true;
        }

        public void ServerStartMatch()
        {
            if (!ServerCanStart(out _)) return;

            var settings = Settings;
            uint seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
            StopTurnTimer();
            m_Games.Clear();
            m_ReadyNextNight.Clear();

            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (!p.Connected) continue;
                m_Games[p.ClientId] = PartyEngine.CreateGame(settings.ScenarioId, seed, settings.Nights);
                p.Rank = 0;
                p.ReadyNextNight = false;
                m_Players[i] = p;
                ServerUpdateSummary(p.ClientId);
            }

            m_Phase.Value = MatchPhase.InGame;
            foreach (var id in m_Games.Keys) ServerBroadcastState(id, null);
            // Everyone starts on night 1 with the doors open, so the first player's turn needs no setup.
            m_ActivePlayer.Value = settings.Mode == MatchMode.Turns ? NextTurnPlayer() : NoPlayer;

            var scenario = ScenarioCatalog.Get(settings.ScenarioId);
            ServerHeadline($"The season begins: {scenario.Name}, {settings.Nights} nights. First {GameRules.DiamondsToWin}-{Terms.Diamond} party wins!");
            NetworkManager.SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
        }

        public void ServerReturnToLobby()
        {
            if (!IsServer) return;

            StopTurnTimer();
            m_Games.Clear();
            m_ReadyNextNight.Clear();
            m_ActivePlayer.Value = NoPlayer;
            for (int i = m_Players.Count - 1; i >= 0; i--)
            {
                var p = m_Players[i];
                if (!p.Connected)
                {
                    m_Players.RemoveAt(i);
                    continue;
                }
                m_Players[i] = NetPlayer.Create(p.ClientId, p.DisplayName, p.ColorIndex);
            }

            m_Phase.Value = MatchPhase.Lobby;
            NetworkManager.SceneManager.LoadScene(SceneNames.MainMenu, LoadSceneMode.Single);
        }

        // ================================================================== RPCs: client -> server

        [Rpc(SendTo.Server)]
        void SetLobbyReadyRpc(bool ready, RpcParams rpcParams = default)
        {
            if (Phase != MatchPhase.Lobby) return;
            int index = IndexOfPlayer(rpcParams.Receive.SenderClientId);
            if (index < 0) return;
            var p = m_Players[index];
            if (p.LobbyReady == ready) return;
            p.LobbyReady = ready;
            m_Players[index] = p;
        }

        [Rpc(SendTo.Server)]
        void SubmitCommandRpc(NetCommand command, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (Phase != MatchPhase.InGame || !m_Games.TryGetValue(sender, out var state)) return;

            var cmd = command.ToCommand();

            if (Settings.Mode == MatchMode.Turns)
            {
                if (sender != ActivePlayer || m_TurnEnding)
                {
                    ServerReject(sender, sender == ActivePlayer ? "Your turn is ending."
                        : ActivePlayer != NoPlayer ? $"It's {PlayerName(ActivePlayer)}'s turn." : "Wait for your turn.");
                    return;
                }
                if (cmd.Type == CommandType.StartNextNight)
                {
                    // In Take Turns, "next night" ends your turn; the night itself starts when your next turn does.
                    if (state.Phase != GamePhase.Planning)
                    {
                        ServerReject(sender, "Finish tonight's party first.");
                        return;
                    }
                    ServerEndTurn(sender, 0f);
                    return;
                }
            }
            else if (cmd.Type == CommandType.StartNextNight && Settings.Mode == MatchMode.SyncNights)
            {
                if (state.Phase != GamePhase.Planning || state.IsFinalNight)
                {
                    ServerReject(sender, "Finish tonight's party first.");
                    return;
                }
                m_ReadyNextNight.Add(sender);
                ServerUpdateSummary(sender);
                ServerEvaluateProgress();
                return;
            }

            if (Settings.Mode == MatchMode.SyncNights && m_ReadyNextNight.Contains(sender) &&
                (cmd.Type == CommandType.BuyGuest || cmd.Type == CommandType.ExpandHouse))
            {
                ServerReject(sender, "You're marked ready. Cancel ready to keep shopping.");
                return;
            }

            m_Scratch.Clear();
            var result = PartyEngine.Execute(state, cmd, m_Scratch);
            if (!result.Ok)
            {
                ServerReject(sender, result.Error);
                return;
            }

            ServerBroadcastState(sender, m_Scratch);
            ServerAnnounce(sender, m_Scratch);
            ServerUpdateSummary(sender);
            if (Settings.Mode == MatchMode.Turns && state.IsGameOver) ServerEndTurn(sender, GameOverTurnDelay);
            ServerEvaluateProgress();
        }

        [Rpc(SendTo.Server)]
        void RequestStateRpc(RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            foreach (var owner in m_Games.Keys) ServerSendState(owner, null, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server)]
        void CancelNextNightRpc(RpcParams rpcParams = default)
        {
            if (Settings.Mode != MatchMode.SyncNights) return;
            ulong sender = rpcParams.Receive.SenderClientId;
            if (m_ReadyNextNight.Remove(sender)) ServerUpdateSummary(sender);
        }

        [Rpc(SendTo.Server)]
        void SendChatRpc(FixedString128Bytes text, RpcParams rpcParams = default)
        {
            if (!TryGetPlayer(rpcParams.Receive.SenderClientId, out var player)) return;
            ChatRpc(player.Name, text, player.ColorIndex);
        }

        // ================================================================== RPCs: server -> clients

        [Rpc(SendTo.SpecifiedInParams)]
        void ReceiveStateRpc(ulong owner, StatePacket packet, RpcParams rpcParams)
        {
            var events = new List<GameEvent>();
            PlayerState state;
            try
            {
                state = StateSerializer.Deserialize(packet.Data, events);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DeckParty] Could not read state from host: {e.Message}");
                return;
            }

            m_States[owner] = state;
            Raise(PlayerStateReceived, owner, state, (IReadOnlyList<GameEvent>)events);
            if (owner == LocalClientId) Raise(StateReceived, state, (IReadOnlyList<GameEvent>)events);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void CommandRejectedRpc(FixedString128Bytes reason, RpcParams rpcParams)
        {
            Raise(CommandRejected, reason.ToString());
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ChatRpc(FixedString64Bytes sender, FixedString128Bytes text, byte colorIndex)
        {
            AddChat(new ChatMessage { Sender = sender.ToString(), Text = text.ToString(), ColorIndex = colorIndex });
        }

        [Rpc(SendTo.ClientsAndHost)]
        void HeadlineRpc(FixedString128Bytes text)
        {
            var message = text.ToString();
            AddChat(new ChatMessage { Sender = string.Empty, Text = message, IsSystem = true });
            Raise(HeadlineReceived, message);
        }

        void AddChat(ChatMessage message)
        {
            ChatHistory.Add(message);
            if (ChatHistory.Count > 60) ChatHistory.RemoveAt(0);
            Raise(ChatReceived, message);
        }

        // ================================================================== server internals

        void ServerOnClientConnected(ulong clientId) => ServerAddPlayer(clientId);

        void ServerAddPlayer(ulong clientId)
        {
            if (!IsServer || IndexOfPlayer(clientId) >= 0) return;

            string name = NetworkService.HasInstance ? NetworkService.Instance.TakePendingName(clientId) : null;
            if (string.IsNullOrEmpty(name)) name = "Player " + (m_Players.Count + 1);
            name = UniqueName(name);

            m_Players.Add(NetPlayer.Create(clientId, name, NextColorIndex()));
            if (!IsHostPlayer(clientId)) ServerHeadline($"{name} joined the party.");
        }

        void ServerOnClientDisconnected(ulong clientId)
        {
            if (!IsServer || !IsSpawned || NetworkManager.ShutdownInProgress) return;
            int index = IndexOfPlayer(clientId);
            if (index < 0) return;

            var p = m_Players[index];
            string name = p.DisplayName;
            if (Phase == MatchPhase.Lobby)
            {
                m_Players.RemoveAt(index);
            }
            else
            {
                p.Connected = false;
                p.ReadyNextNight = false;
                m_Players[index] = p;
                m_ReadyNextNight.Remove(clientId);
            }

            ServerHeadline($"{name} left the party.");
            ServerEvaluateProgress();
        }

        string UniqueName(string name)
        {
            string candidate = name;
            int suffix = 2;
            while (NameTaken(candidate)) candidate = name + " " + suffix++;
            return candidate;
        }

        bool NameTaken(string name)
        {
            for (int i = 0; i < m_Players.Count; i++)
                if (string.Equals(m_Players[i].DisplayName, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        byte NextColorIndex()
        {
            for (byte c = 0; c < UIPalette.PlayerColors.Length; c++)
            {
                bool used = false;
                for (int i = 0; i < m_Players.Count; i++)
                    if (m_Players[i].ColorIndex == c) { used = true; break; }
                if (!used) return c;
            }
            return (byte)(m_Players.Count % UIPalette.PlayerColors.Length);
        }

        bool IsConnectedPlayer(ulong clientId) => TryGetPlayer(clientId, out var p) && p.Connected;

        void ServerReject(ulong clientId, string reason) =>
            CommandRejectedRpc(NetStrings.Fs128(reason), RpcTarget.Single(clientId, RpcTargetUse.Temp));

        /// <summary>Sends a player's season to everyone: all players can watch every party.</summary>
        void ServerBroadcastState(ulong owner, List<GameEvent> events) => ServerSendState(owner, events, RpcTarget.ClientsAndHost);

        void ServerSendState(ulong owner, List<GameEvent> events, RpcParams target)
        {
            if (!m_Games.TryGetValue(owner, out var state)) return;
            var packet = new StatePacket { Data = StateSerializer.Serialize(state, events) };
            ReceiveStateRpc(owner, packet, target);
        }

        void ServerHeadline(string text) => HeadlineRpc(NetStrings.Fs128(text));

        void ServerUpdateSummary(ulong clientId)
        {
            int index = IndexOfPlayer(clientId);
            if (index < 0) return;

            var p = m_Players[index];
            if (m_Games.TryGetValue(clientId, out var s))
            {
                p.Night = (byte)Mathf.Clamp(s.Night, 0, 255);
                p.Phase = (byte)s.Phase;
                p.Pop = s.Pop;
                p.Cash = s.Cash;
                p.Capacity = (byte)Mathf.Clamp(s.Capacity, 0, 255);
                p.Guests = (byte)Mathf.Clamp(s.House.Count, 0, 255);
                p.DiamondsOwned = (byte)Mathf.Clamp(s.DiamondsOwned(), 0, 255);
                p.DiamondsInHouse = (byte)Mathf.Clamp(PartyEngine.DiamondsPresent(s), 0, 255);
                p.BestDiamonds = (byte)Mathf.Clamp(s.BestDiamonds, 0, 255);
                p.Trouble = (byte)Mathf.Clamp(PartyEngine.NetTrouble(s), 0, 255);
                p.TotalPop = s.TotalPopEarned;
                p.WinNight = (byte)Mathf.Clamp(s.WinNight, 0, 255);
                p.LastOutcome = (byte)s.LastOutcome;
            }
            p.ReadyNextNight = m_ReadyNextNight.Contains(clientId);

            if (!p.Equals(m_Players[index])) m_Players[index] = p;
        }

        void ServerAnnounce(ulong clientId, List<GameEvent> events)
        {
            string name = PlayerName(clientId);
            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case GameEventType.PoliceRaid:
                        ServerHeadline($"{name} got swatted!");
                        break;
                    case GameEventType.FireMarshal:
                        ServerHeadline($"{name} blew past their {Terms.SubCap} and got shut down!");
                        break;
                    case GameEventType.PartyEnded:
                        if (e.C == GameRules.DiamondsToWin - 1) ServerHeadline($"{name} had {e.C} {Terms.Diamonds} at one party. One more and they win!");
                        break;
                    case GameEventType.GuestPurchased:
                        if (GuestCatalog.Get(e.Guest).IsDiamond) ServerHeadline($"{name} signed a {GuestVisuals.DisplayName(e.Guest)}!");
                        break;
                    case GameEventType.Victory:
                        ServerHeadline(Settings.Mode == MatchMode.Race
                            ? $"{name} threw a {GameRules.DiamondsToWin}-{Terms.Diamond} party and wins!"
                            : $"{name} threw a {GameRules.DiamondsToWin}-{Terms.Diamond} party! Finishing night {e.A}...");
                        break;
                }
            }
        }

        void ServerEvaluateProgress()
        {
            if (!IsServer || Phase != MatchPhase.InGame) return;

            if (Settings.Mode == MatchMode.Turns)
            {
                // The active player left (or is somehow done) without their turn being handed on.
                ulong active = ActivePlayer;
                bool stuck = !IsConnectedPlayer(active) || (m_Games.TryGetValue(active, out var current) && current.IsGameOver);
                if (!m_TurnEnding && stuck) ServerAdvanceTurn();
                return;
            }

            bool anyVictory = false;
            bool allFinishedNight = true;
            bool allReady = true;
            int activePlayers = 0;

            foreach (var kv in m_Games)
            {
                if (!IsConnectedPlayer(kv.Key)) continue;
                var s = kv.Value;
                if (s.Phase == GamePhase.Victory) { anyVictory = true; continue; }
                if (s.Phase == GamePhase.Defeat) continue;

                activePlayers++;
                if (s.Phase != GamePhase.Planning) allFinishedNight = false;
                if (s.Phase != GamePhase.Planning || !m_ReadyNextNight.Contains(kv.Key)) allReady = false;
            }

            if (Settings.Mode == MatchMode.Race)
            {
                if (anyVictory || activePlayers == 0) ServerEndMatch();
                return;
            }

            if (activePlayers == 0) { ServerEndMatch(); return; }
            if (anyVictory)
            {
                if (allFinishedNight) ServerEndMatch();
                return;
            }
            if (allReady) ServerAdvanceNight();
        }

        void ServerAdvanceNight()
        {
            int night = 0;
            var ids = new List<ulong>(m_Games.Keys);
            foreach (var id in ids)
            {
                if (!IsConnectedPlayer(id)) continue;
                var s = m_Games[id];
                if (s.Phase != GamePhase.Planning) continue;

                m_Scratch.Clear();
                if (PartyEngine.Execute(s, GameCommand.StartNextNight(), m_Scratch).Ok)
                {
                    night = s.Night;
                    ServerBroadcastState(id, m_Scratch);
                }
            }

            m_ReadyNextNight.Clear();
            foreach (var id in ids) ServerUpdateSummary(id);
            if (night > 0) ServerHeadline($"Night {night} begins!");
        }

        // ------------------------------------------------------------------ Take Turns

        /// <summary>Marks a player's turn as done for this round and hands it on (after a pause, if their season just ended).</summary>
        void ServerEndTurn(ulong clientId, float delay)
        {
            m_ReadyNextNight.Add(clientId);
            ServerUpdateSummary(clientId);

            if (delay <= 0f)
            {
                ServerAdvanceTurn();
                return;
            }
            StopTurnTimer();
            m_TurnEnding = true;
            m_TurnRoutine = StartCoroutine(AdvanceTurnAfter(delay));
        }

        IEnumerator AdvanceTurnAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            m_TurnRoutine = null;
            m_TurnEnding = false;
            if (IsServer && Phase == MatchPhase.InGame) ServerAdvanceTurn();
        }

        void StopTurnTimer()
        {
            if (m_TurnRoutine != null) StopCoroutine(m_TurnRoutine);
            m_TurnRoutine = null;
            m_TurnEnding = false;
        }

        /// <summary>The next connected player (in lobby order) who's still in the season and hasn't played this round.</summary>
        ulong NextTurnPlayer()
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (!p.Connected || m_ReadyNextNight.Contains(p.ClientId)) continue;
                if (!m_Games.TryGetValue(p.ClientId, out var s) || s.IsGameOver) continue;
                return p.ClientId;
            }
            return NoPlayer;
        }

        void ServerAdvanceTurn()
        {
            if (!IsServer || Phase != MatchPhase.InGame) return;

            ulong next = NextTurnPlayer();
            if (next == NoPlayer)
            {
                // Everyone has played tonight. A win (or nobody left to play) ends the season; otherwise a new round starts.
                bool anyVictory = false;
                foreach (var kv in m_Games)
                    if (IsConnectedPlayer(kv.Key) && kv.Value.Phase == GamePhase.Victory) anyVictory = true;

                m_ReadyNextNight.Clear();
                foreach (var id in m_Games.Keys) ServerUpdateSummary(id);
                next = anyVictory ? NoPlayer : NextTurnPlayer();
                if (next == NoPlayer)
                {
                    ServerEndMatch();
                    return;
                }
            }
            ServerBeginTurn(next);
        }

        void ServerBeginTurn(ulong clientId)
        {
            var s = m_Games[clientId];
            if (s.Phase == GamePhase.Planning && !s.IsFinalNight)
            {
                m_Scratch.Clear();
                if (PartyEngine.Execute(s, GameCommand.StartNextNight(), m_Scratch).Ok)
                {
                    ServerBroadcastState(clientId, m_Scratch);
                    ServerUpdateSummary(clientId);
                }
            }
            // Set after the state goes out, so clients already have the new night when they switch to this board.
            m_ActivePlayer.Value = clientId;
        }

        void ServerEndMatch()
        {
            StopTurnTimer();
            m_ActivePlayer.Value = NoPlayer;
            foreach (var id in m_Games.Keys) ServerUpdateSummary(id);

            var order = new List<int>();
            for (int i = 0; i < m_Players.Count; i++) order.Add(i);
            var snapshot = PlayersSnapshot();
            order.Sort((a, b) => ComparePlayers(snapshot[a], snapshot[b]));

            for (int rank = 0; rank < order.Count; rank++)
            {
                var p = m_Players[order[rank]];
                p.Rank = (byte)(rank + 1);
                m_Players[order[rank]] = p;
            }

            m_Phase.Value = MatchPhase.Results;

            if (order.Count > 0)
            {
                var winner = snapshot[order[0]];
                ServerHeadline(winner.Won
                    ? $"{winner.DisplayName} wins the season with a {GameRules.DiamondsToWin}-{Terms.Diamond} party on night {winner.WinNight}!"
                    : $"Nobody hit {GameRules.DiamondsToWin} {Terms.Diamonds}. {winner.DisplayName} takes it with a best of {Terms.DiamondCount(winner.BestDiamonds)}.");
            }
        }

        static int ComparePlayers(NetPlayer a, NetPlayer b)
        {
            if (a.Connected != b.Connected) return a.Connected ? -1 : 1;
            if (a.Won != b.Won) return a.Won ? -1 : 1;
            if (a.Won && a.WinNight != b.WinNight) return a.WinNight.CompareTo(b.WinNight);
            if (a.BestDiamonds != b.BestDiamonds) return b.BestDiamonds.CompareTo(a.BestDiamonds);
            if (a.TotalPop != b.TotalPop) return b.TotalPop.CompareTo(a.TotalPop);
            return a.ClientId.CompareTo(b.ClientId);
        }
    }
}
