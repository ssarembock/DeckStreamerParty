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
        /// <summary>Take Turns timer: server time when the active player's turn runs out (0 = no timer running).</summary>
        readonly NetworkVariable<double> m_TurnDeadline = new NetworkVariable<double>(0);
        /// <summary>Which timer is running (<see cref="MatchTimer"/>): the stream timer or the shop timer.</summary>
        readonly NetworkVariable<byte> m_TimerKind = new NetworkVariable<byte>(0);
        /// <summary>Free For All: where the round is (a <see cref="RoundStage"/>).</summary>
        readonly NetworkVariable<byte> m_RoundStage = new NetworkVariable<byte>((byte)RoundStage.Streaming);
        /// <summary>Free For All: server time when the countdown ends and the shop opens.</summary>
        readonly NetworkVariable<double> m_ShopOpensAt = new NetworkVariable<double>(0);
        /// <summary>The streamer pick before a match: server time when it ends (anyone not locked in plays a random streamer).</summary>
        readonly NetworkVariable<double> m_PickDeadline = new NetworkVariable<double>(0);

        // Client-side view: the latest snapshot of every player's season.
        readonly Dictionary<ulong, PlayerState> m_States = new Dictionary<ulong, PlayerState>();
        public readonly List<ChatMessage> ChatHistory = new List<ChatMessage>();

        // Server-side simulation
        readonly Dictionary<ulong, PlayerState> m_Games = new Dictionary<ulong, PlayerState>();
        // Free For All: ready for the next night. Take Turns: finished their turn this round.
        readonly HashSet<ulong> m_ReadyNextNight = new HashSet<ulong>();
        // Free For All: finished their stream and clicked Go to the Shop.
        readonly HashSet<ulong> m_ReadyForShop = new HashSet<ulong>();
        readonly List<GameEvent> m_Scratch = new List<GameEvent>();
        Coroutine m_TurnRoutine;
        bool m_TurnEnding;
        /// <summary>Host: which half of the turn / round the timers are on (set even when that half has no timer).</summary>
        MatchTimer m_TimerStage;
        // Which board each player is looking at (for the "watching" count) and emote spam protection.
        readonly Dictionary<ulong, ulong> m_Viewing = new Dictionary<ulong, ulong>();
        /// <summary>Each player's recent emote times (the host's anti-spam check, <see cref="Emotes.Allowed"/>).</summary>
        readonly Dictionary<ulong, List<float>> m_EmoteTimes = new Dictionary<ulong, List<float>>();
        readonly Dictionary<ulong, float> m_LastPresence = new Dictionary<ulong, float>();

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
        /// <summary>A headline (localization key with arguments; show it with Loc.Error).</summary>
        public event Action<string> HeadlineReceived;
        /// <summary>Another player's presence signal (what they're pointing at or choosing).</summary>
        public event Action<ulong, PresenceMessage> PresenceReceived;
        /// <summary>A player sent an emote (index into <see cref="Emotes.All"/>).</summary>
        public event Action<ulong, int> EmoteReceived;
        /// <summary>A rival power hit someone: (actor, target, streamer).</summary>
        public event Action<ulong, ulong, StreamerType> RivalPowerUsed;
        /// <summary>Free For All: the round moved on (streaming, the countdown, the shop open).</summary>
        public event Action<RoundStage> StageChanged;

        public NetworkList<NetPlayer> Players => m_Players;
        public MatchSettings Settings => m_Settings.Value;
        public MatchPhase Phase => m_Phase.Value;
        public ulong ActivePlayer => m_ActivePlayer.Value;
        public bool TakingTurns => Settings.Mode == MatchMode.Turns;

        /// <summary>Free For All: streams run at once (anyone may watch anyone, read-only), and the shop opens for everyone together.</summary>
        public bool FreeForAll => MatchModes.UsesRounds(Settings.Mode);

        /// <summary>Free For All: where the round is (always Shopping in the other modes, where everyone shops at their own pace).</summary>
        public RoundStage Stage => FreeForAll ? (RoundStage)m_RoundStage.Value : RoundStage.Shopping;

        /// <summary>Whether the shop takes orders right now (Free For All only while it's open for everyone).</summary>
        public bool ShopOpen => Stage == RoundStage.Shopping;

        /// <summary>Free For All: seconds until the shop opens during the countdown (0 otherwise).</summary>
        /// <summary>The streamer pick before a match is on (everyone is on the select screen).</summary>
        public bool PickingStreamers => Phase == MatchPhase.PickingStreamers;

        /// <summary>Seconds left to pick a streamer (0 outside the pick).</summary>
        public float PickSecondsLeft
        {
            get
            {
                if (!PickingStreamers || NetworkManager == null) return 0f;
                return Mathf.Max(0f, (float)(m_PickDeadline.Value - NetworkManager.ServerTime.Time));
            }
        }

        /// <summary>Players still picking their streamer.</summary>
        public int PlayersStillPicking
        {
            get
            {
                int count = 0;
                for (int i = 0; i < m_Players.Count; i++)
                    if (m_Players[i].Connected && !m_Players[i].StreamerLocked) count++;
                return count;
            }
        }

        /// <summary>
        /// Team modes: the player who picks the team's streamer (the team's first player in the lobby). Their teammate waits.
        /// Everyone picks for themselves outside the team modes.
        /// </summary>
        public bool PicksStreamer(ulong clientId)
        {
            if (!IsTeamMode) return true;
            int team = Settings.Mode == MatchMode.CoopEndless ? -1 : TeamOf(clientId);
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (!p.Connected || (team >= 0 && p.Team != team)) continue;
                return p.ClientId == clientId;
            }
            return true;
        }

        public float ShopCountdownLeft
        {
            get
            {
                if (Stage != RoundStage.Countdown || NetworkManager == null) return 0f;
                return Mathf.Max(0f, (float)(m_ShopOpensAt.Value - NetworkManager.ServerTime.Time));
            }
        }

        /// <summary>Players still in the lobby or match (connected).</summary>
        public int ConnectedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < m_Players.Count; i++)
                    if (m_Players[i].Connected) count++;
                return count;
            }
        }

        /// <summary>The running countdown: the stream timer or the shop timer (None when neither runs).</summary>
        public MatchTimer Timer => m_TurnDeadline.Value > 0 ? (MatchTimer)m_TimerKind.Value : MatchTimer.None;

        /// <summary>Seconds left on the running timer (the stream or the shop), or -1 when there's no timer.</summary>
        public float TurnSecondsLeft
        {
            get
            {
                double deadline = m_TurnDeadline.Value;
                if (deadline <= 0 || NetworkManager == null) return -1f;
                return Mathf.Max(0f, (float)(deadline - NetworkManager.ServerTime.Time));
            }
        }

        /// <summary>A player's position in the player list, as used by rival-power events.</summary>
        public ulong ClientAtIndex(int index) => index >= 0 && index < m_Players.Count ? m_Players[index].ClientId : NoPlayer;
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
            m_RoundStage.OnValueChanged += OnRoundStageValueChanged;

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
            m_RoundStage.OnValueChanged -= OnRoundStageValueChanged;
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
        void OnRoundStageValueChanged(byte previous, byte current) => Raise(StageChanged, (RoundStage)current);

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

        public string PlayerName(ulong clientId) => TryGetPlayer(clientId, out var p) ? p.DisplayName : Loc.Get("common.someone");

        /// <summary>Whether <paramref name="clientId"/> has a rival still in the season (rival powers need one; teammates aren't rivals).</summary>
        public bool HasRival(ulong clientId)
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (p.ClientId != clientId && p.Connected && !p.Finished && !SameTeam(p.ClientId, clientId)) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ teams (2v2, co-op)

        /// <summary>Team modes: players share a season in teams of two.</summary>
        public bool IsTeamMode => MatchModes.IsTeam(Settings.Mode);

        /// <summary>A player's team (0 or 1) in a team mode; 0 otherwise.</summary>
        public int TeamOf(ulong clientId) => TryGetPlayer(clientId, out var p) ? p.Team : 0;

        /// <summary>A player's place in their team's season (member 0 or 1) in a team mode; 0 otherwise.</summary>
        public int MemberOf(ulong clientId) => IsTeamMode && TryGetPlayer(clientId, out var p) ? p.Member : 0;

        /// <summary>Two different players on the same team (always false outside the team modes).</summary>
        public bool SameTeam(ulong a, ulong b) => IsTeamMode && a != b && TryGetPlayer(a, out var pa) && TryGetPlayer(b, out var pb) && pa.Team == pb.Team;

        /// <summary>The other players on this player's team (none outside the team modes).</summary>
        public List<ulong> TeammatesOf(ulong clientId)
        {
            var mates = new List<ulong>();
            if (!IsTeamMode) return mates;
            for (int i = 0; i < m_Players.Count; i++)
                if (SameTeam(m_Players[i].ClientId, clientId)) mates.Add(m_Players[i].ClientId);
            return mates;
        }

        /// <summary>A team's players (lobby order).</summary>
        public List<NetPlayer> TeamPlayers(int team)
        {
            var list = new List<NetPlayer>();
            for (int i = 0; i < m_Players.Count; i++)
                if (m_Players[i].Team == team) list.Add(m_Players[i]);
            return list;
        }

        /// <summary>"Ana and Bo": a team's player names.</summary>
        public string TeamName(int team)
        {
            var names = new List<string>();
            foreach (var p in TeamPlayers(team)) names.Add(p.DisplayName);
            return string.Join(" & ", names);
        }

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
        /// <summary>Changes your streamer in the lobby (a StreamerType, or the Random pick).</summary>
        public void SetStreamer(byte streamer) => SetStreamerRpc(streamer);
        /// <summary>Tells the host whose stream you're looking at (for the "watching" count).</summary>
        public void ReportViewing(ulong viewed) => ViewingRpc(viewed);
        public void SendPresence(PresenceMessage message) => PresenceRpc(message);

        public void SendEmote(int index)
        {
            if (index < 0 || index >= Emotes.Count) return;
            EmoteRpc((byte)index);
        }
        public void SubmitCommand(GameCommand command) => SubmitCommandRpc(NetCommand.From(command));
        public void RequestState() => RequestStateRpc();
        public void CancelNextNightReady() => CancelNextNightRpc();
        /// <summary>Free For All: after your stream, say you're ready for the shop (or take it back while others still stream).</summary>
        public void SetReadyForShop(bool ready) => ReadyForShopRpc(ready);
        /// <summary>2v2 lobby: move to the other team (if it has room).</summary>
        public void SwitchTeam() => SwitchTeamRpc();
        /// <summary>The streamer pick before a match: lock in your streamer (a StreamerType, or the Random pick).</summary>
        public void LockStreamer(byte streamer) => LockStreamerRpc(streamer);

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
            // More than two players: Free For All only.
            if (!MatchModes.Allowed(settings.Mode, ConnectedCount)) settings.Mode = MatchMode.SyncNights;
            if (!settings.Equals(m_Settings.Value)) m_Settings.Value = settings;
        }

        /// <summary>A third player joined a Take Turns or Race lobby: switch to Free For All (and say so).</summary>
        void ServerEnforceModeForPlayerCount()
        {
            if (!IsServer || Phase != MatchPhase.Lobby) return;
            var settings = m_Settings.Value;
            if (MatchModes.Allowed(settings.Mode, ConnectedCount)) return;
            settings.Mode = MatchMode.SyncNights;
            m_Settings.Value = settings;
            ServerHeadline(Core.Err.Of("mp.ffa_forced", MatchModes.MaxPlayersForAnyMode));
        }

        public bool ServerCanStart(out string reason)
        {
            reason = null;
            if (!IsServer) { reason = "mp.host_only"; return false; }
            if (Phase != MatchPhase.Lobby) { reason = "mp.already_started"; return false; }

            int waiting = 0;
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (!p.Connected || IsHostPlayer(p.ClientId)) continue;
                if (!p.LobbyReady) waiting++;
            }
            if (waiting > 0)
            {
                reason = Core.Err.Of("mp.waiting_ready", waiting);
                return false;
            }

            // Team modes need exactly their players (2v2: four, two a side; co-op: two).
            int required = MatchModes.RequiredPlayers(Settings.Mode);
            if (required > 0 && ConnectedCount != required)
            {
                reason = Core.Err.Of("mp.needs_players", required);
                return false;
            }
            if (Settings.Mode == MatchMode.TwoVsTwo && (CountTeam(0) != GameRules.TeamSize || CountTeam(1) != GameRules.TeamSize))
            {
                reason = "mp.teams_uneven";
                return false;
            }
            return true;
        }

        /// <summary>Connected players on a team.</summary>
        int CountTeam(int team)
        {
            int count = 0;
            for (int i = 0; i < m_Players.Count; i++)
                if (m_Players[i].Connected && m_Players[i].Team == team) count++;
            return count;
        }

        /// <summary>
        /// The host pressed Start: everyone picks their streamer now, last thing before the match (2026-11-16). Team modes: the
        /// team's first player picks for the team. The match starts once everyone has locked in or the timer runs out.
        /// </summary>
        public void ServerBeginStreamerPick()
        {
            if (!ServerCanStart(out _)) return;
            ServerEnforceModeForPlayerCount();
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                // Team modes: the teammate who doesn't pick is locked in already (the team plays its picker's streamer).
                p.StreamerLocked = p.Connected && !PicksStreamer(p.ClientId);
                m_Players[i] = p;
            }
            m_PickDeadline.Value = NetworkManager.ServerTime.Time + MatchModes.StreamerPickSeconds;
            m_Phase.Value = MatchPhase.PickingStreamers;
            ServerHeadline("mp.pick_streamers");
            ServerCheckPickDone();
        }

        [Rpc(SendTo.Server)]
        void LockStreamerRpc(byte streamer, RpcParams rpcParams = default)
        {
            if (!PickingStreamers) return;
            if (streamer != StreamerCatalog.RandomPick && !StreamerCatalog.IsValid((StreamerType)streamer)) return;
            ulong sender = rpcParams.Receive.SenderClientId;
            int index = IndexOfPlayer(sender);
            if (index < 0 || !PicksStreamer(sender)) return;
            var p = m_Players[index];
            if (p.StreamerLocked) return;
            p.Streamer = streamer;
            p.StreamerLocked = true;
            m_Players[index] = p;
            ServerCheckPickDone();
        }

        /// <summary>Everyone has locked in their streamer: the match starts.</summary>
        void ServerCheckPickDone()
        {
            if (IsServer && PickingStreamers && PlayersStillPicking == 0) ServerStartMatch();
        }

        /// <summary>The pick timer ran out: anyone still picking plays a random streamer, and the match starts.</summary>
        void ServerPickTimedOut()
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (!p.Connected || p.StreamerLocked) continue;
                p.Streamer = StreamerCatalog.RandomPick;
                p.StreamerLocked = true;
                m_Players[i] = p;
            }
            ServerStartMatch();
        }

        /// <summary>Someone left during the pick: back to the lobby (the match needs everyone).</summary>
        void ServerCancelPick()
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                p.StreamerLocked = false;
                m_Players[i] = p;
            }
            m_PickDeadline.Value = 0;
            m_Phase.Value = MatchPhase.Lobby;
            ServerHeadline("mp.pick_cancelled");
        }

        /// <summary>Starts the match (after the streamer pick; Start in the lobby begins the pick, see <see cref="ServerBeginStreamerPick"/>).</summary>
        public void ServerStartMatch()
        {
            if (!IsServer || (Phase != MatchPhase.PickingStreamers && !ServerCanStart(out _))) return;
            m_PickDeadline.Value = 0;

            ServerEnforceModeForPlayerCount();
            var settings = Settings;
            uint seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
            StopTurnTimer();
            m_Games.Clear();
            m_ReadyNextNight.Clear();
            m_ReadyForShop.Clear();
            m_RoundStage.Value = (byte)RoundStage.Streaming;
            m_ShopOpensAt.Value = 0;

            var random = new System.Random((int)seed);
            if (MatchModes.IsTeam(settings.Mode))
            {
                ServerCreateTeamGames(settings, seed, random);
            }
            else
            {
                for (int i = 0; i < m_Players.Count; i++)
                {
                    var p = m_Players[i];
                    if (!p.Connected) continue;
                    var streamer = StreamerCatalog.Resolve(p.Streamer, random);
                    p.Streamer = (byte)streamer;
                    m_Games[p.ClientId] = PartyEngine.CreateGame(settings.ScenarioId, seed, settings.Nights, streamer);
                    p.Rank = 0;
                    p.ReadyNextNight = false;
                    p.Member = 0;
                    m_Players[i] = p;
                    ServerUpdateSummary(p.ClientId);
                }
            }
            // The shared shop's stacks grow with the match: 2 copies per player (6 for three players, 8 for four...).
            foreach (var game in m_Games.Values) game.MatchPlayers = m_Games.Count;

            m_Phase.Value = MatchPhase.InGame;
            foreach (var id in m_Games.Keys) ServerBroadcastState(id, null);
            // Everyone starts on night 1 with the doors open, so the first player's turn needs no setup.
            m_ActivePlayer.Value = settings.Mode == MatchMode.Turns ? NextTurnPlayer() : NoPlayer;
            ServerStartTimer(MatchTimer.Stream, FirstTimerGrace);

            // Co-op Endless has no win: its opening line says how many streams the run starts with instead.
            ServerHeadline(settings.Mode == MatchMode.CoopEndless
                ? Core.Err.Of("mp.coop_begins", "@s" + settings.ScenarioId, GameRules.EndlessStartingStreams)
                : Core.Err.Of("mp.season_begins", "@s" + settings.ScenarioId, settings.Nights) + Core.Err.Separator + GameRules.DiamondsToWin);
            NetworkManager.SceneManager.LoadScene(SceneNames.Game, LoadSceneMode.Single);
        }

        /// <summary>
        /// Team modes: one season per team, shared by its two players (Cash, Influence, the stream and the shop), each with
        /// their own Audience and Sub Cap (member 0 and 1, in lobby order). The team plays as its first player's streamer.
        /// 2v2 is a Classic season per team; co-op is one Endless run for both.
        /// </summary>
        void ServerCreateTeamGames(MatchSettings settings, uint seed, System.Random random)
        {
            bool coop = settings.Mode == MatchMode.CoopEndless;
            var mode = coop ? GameMode.Endless : GameMode.Classic;
            int nights = coop ? GameRules.EndlessStartingStreams : settings.Nights;

            var teams = new SortedDictionary<int, List<int>>();
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (!p.Connected) continue;
                int team = coop ? 0 : p.Team;
                if (!teams.TryGetValue(team, out var list)) teams[team] = list = new List<int>();
                list.Add(i);
            }

            foreach (var kv in teams)
            {
                var members = kv.Value;
                var streamer = StreamerCatalog.Resolve(m_Players[members[0]].Streamer, random);
                var state = PartyEngine.CreateGame(settings.ScenarioId, seed, nights, streamer, mode, Mathf.Min(members.Count, GameRules.TeamSize));
                for (int k = 0; k < members.Count; k++)
                {
                    var p = m_Players[members[k]];
                    p.Team = (byte)kv.Key;
                    p.Member = (byte)Mathf.Min(k, GameRules.TeamSize - 1);
                    p.Streamer = (byte)streamer;
                    p.Rank = 0;
                    p.ReadyNextNight = false;
                    m_Players[members[k]] = p;
                    m_Games[p.ClientId] = state;
                }
                foreach (var index in members) ServerUpdateSummary(m_Players[index].ClientId);
            }
        }

        public void ServerReturnToLobby()
        {
            if (!IsServer) return;

            StopTurnTimer();
            m_Games.Clear();
            m_ReadyNextNight.Clear();
            m_ReadyForShop.Clear();
            m_RoundStage.Value = (byte)RoundStage.Streaming;
            m_ShopOpensAt.Value = 0;
            m_ActivePlayer.Value = NoPlayer;
            for (int i = m_Players.Count - 1; i >= 0; i--)
            {
                var p = m_Players[i];
                if (!p.Connected)
                {
                    m_Players.RemoveAt(i);
                    continue;
                }
                var fresh = NetPlayer.Create(p.ClientId, p.DisplayName, p.ColorIndex, p.Streamer, p.PlatformUserId);
                fresh.Team = p.Team;
                m_Players[i] = fresh;
            }

            m_Phase.Value = MatchPhase.Lobby;
            NetworkManager.SceneManager.LoadScene(SceneNames.MainMenu, LoadSceneMode.Single);
        }

        // ================================================================== RPCs: client -> server

        /// <summary>2v2 lobby: the sender moves to the other team, if it has room (two a side).</summary>
        [Rpc(SendTo.Server)]
        void SwitchTeamRpc(RpcParams rpcParams = default)
        {
            if (Phase != MatchPhase.Lobby) return;
            int index = IndexOfPlayer(rpcParams.Receive.SenderClientId);
            if (index < 0) return;
            var p = m_Players[index];
            int other = p.Team == 0 ? 1 : 0;
            if (CountTeam(other) >= GameRules.TeamSize)
            {
                ServerReject(p.ClientId, "mp.team_full");
                return;
            }
            p.Team = (byte)other;
            m_Players[index] = p;
        }

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
        void SetStreamerRpc(byte streamer, RpcParams rpcParams = default)
        {
            if (Phase != MatchPhase.Lobby) return;
            if (streamer != StreamerCatalog.RandomPick && !StreamerCatalog.IsValid((StreamerType)streamer)) return;
            int index = IndexOfPlayer(rpcParams.Receive.SenderClientId);
            if (index < 0) return;
            var p = m_Players[index];
            if (p.Streamer == streamer) return;
            p.Streamer = streamer;
            m_Players[index] = p;
        }

        [Rpc(SendTo.Server)]
        void ViewingRpc(ulong viewed, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            m_Viewing[sender] = viewed;
            ServerUpdateWatchers();
        }

        [Rpc(SendTo.Server)]
        void PresenceRpc(PresenceMessage message, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (Phase != MatchPhase.InGame || !IsConnectedPlayer(sender)) return;
            // In Take Turns only the active player has anything to show; the others are watching.
            if (TakingTurns && sender != ActivePlayer && message.Kind != PresenceKind.None) return;
            // Updates up to a second late are fine (turn-based), so a generous flood guard costs nothing.
            float now = Time.unscaledTime;
            if (m_LastPresence.TryGetValue(sender, out var last) && now - last < 0.05f && message.Kind != PresenceKind.None) return;
            m_LastPresence[sender] = now;
            ReceivePresenceRpc(sender, message);
        }

        [Rpc(SendTo.Server)]
        void EmoteRpc(byte index, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (index >= Emotes.Count || !IsConnectedPlayer(sender)) return;
            float now = Time.unscaledTime;
            if (!m_EmoteTimes.TryGetValue(sender, out var recent)) m_EmoteTimes[sender] = recent = new List<float>();
            if (!Emotes.Allowed(recent, now)) return;
            recent.Add(now);
            ReceiveEmoteRpc(sender, index);
        }

        [Rpc(SendTo.Server)]
        void SubmitCommandRpc(NetCommand command, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (Phase != MatchPhase.InGame || !m_Games.TryGetValue(sender, out var state)) return;

            var cmd = command.ToCommand();
            // Teams: the sender's place in their team's season decides whose queue, Sub Cap and Audience it uses.
            cmd.Member = MemberOf(sender);

            if (Settings.Mode == MatchMode.Turns)
            {
                if (sender != ActivePlayer || m_TurnEnding)
                {
                    ServerReject(sender, sender == ActivePlayer ? "mp.turn_ending"
                        : ActivePlayer != NoPlayer ? Core.Err.Of("mp.their_turn", PlayerName(ActivePlayer)) : "mp.wait_turn");
                    return;
                }
                if (cmd.Type == CommandType.StartNextNight)
                {
                    // In Take Turns, "next night" ends your turn; the night itself starts when your next turn does.
                    if (state.Phase != GamePhase.Planning)
                    {
                        ServerReject(sender, "err.finish_stream");
                        return;
                    }
                    ServerEndTurn(sender, 0f);
                    return;
                }
            }
            else if (FreeForAll && Stage != RoundStage.Shopping && IsShopCommand(cmd.Type))
            {
                // Free For All: the shop opens for everyone at once, after every stream is over.
                ServerReject(sender, Stage == RoundStage.Countdown ? "mp.shop_countdown" : "mp.shop_closed");
                return;
            }
            else if (cmd.Type == CommandType.StartNextNight && MatchModes.UsesRounds(Settings.Mode))
            {
                if (state.Phase != GamePhase.Planning || state.IsFinalNight)
                {
                    ServerReject(sender, "err.finish_stream");
                    return;
                }
                m_ReadyNextNight.Add(sender);
                ServerUpdateSummary(sender);
                ServerEvaluateProgress();
                return;
            }

            if (MatchModes.UsesRounds(Settings.Mode) && m_ReadyNextNight.Contains(sender) &&
                (cmd.Type == CommandType.BuyGuest || cmd.Type == CommandType.ExpandHouse || cmd.Type == CommandType.UseStreamerPower || cmd.Type == CommandType.RemoveGuest ||
                 cmd.Type == CommandType.BuyExtraStream || cmd.Type == CommandType.BuyShopViewer))
            {
                ServerReject(sender, "mp.marked_ready");
                return;
            }

            // Rival powers touch two players' seasons, so the host resolves them here.
            if (cmd.Type == CommandType.UseStreamerPower && state.StreamerInfo.NeedsRival)
            {
                ServerUseRivalPower(sender, state, cmd.TargetA);
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
            ServerSendDoom(sender, state);
            ServerSendBots(sender, state);
            ServerSendSabotage(sender, state);
            // A Free For All player whose stream ended badly may have taken back their shop ready mark with it.
            if (state.Phase != GamePhase.Planning) m_ReadyForShop.Remove(sender);
            ServerUpdateSummary(sender);
            ServerSyncSharedShop();
            ServerCheckTurnStage(sender, state);
            if (Settings.Mode == MatchMode.Turns && state.IsGameOver) ServerEndTurn(sender, GameOverTurnDelay);
            ServerEvaluateProgress();
        }

        [Rpc(SendTo.Server)]
        void RequestStateRpc(RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            foreach (var owner in m_Games.Keys) ServerSendState(owner, null, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        /// <summary>Commands that belong to the shop half of a turn (gated in Free For All until the shop opens).</summary>
        static bool IsShopCommand(CommandType type) =>
            type == CommandType.BuyGuest || type == CommandType.ExpandHouse || type == CommandType.UseStreamerPower || type == CommandType.StartNextNight ||
            type == CommandType.RemoveGuest || type == CommandType.BuyExtraStream || type == CommandType.BuyShopViewer;

        [Rpc(SendTo.Server)]
        void ReadyForShopRpc(bool ready, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (Phase != MatchPhase.InGame || !FreeForAll || Stage != RoundStage.Streaming || !m_Games.TryGetValue(sender, out var state)) return;
            if (ready && state.Phase != GamePhase.Planning)
            {
                ServerReject(sender, "err.finish_stream");
                return;
            }
            bool changed = ready ? m_ReadyForShop.Add(sender) : m_ReadyForShop.Remove(sender);
            if (!changed) return;
            ServerUpdateSummary(sender);
            ServerEvaluateProgress();
        }

        [Rpc(SendTo.Server)]
        void CancelNextNightRpc(RpcParams rpcParams = default)
        {
            if (!MatchModes.UsesRounds(Settings.Mode)) return;
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
            // Teams share one season: the snapshot is every teammate's too (it comes in under whoever acted).
            var mates = TeammatesOf(owner);
            foreach (var mate in mates) m_States[mate] = state;
            Raise(PlayerStateReceived, owner, state, (IReadOnlyList<GameEvent>)events);
            if (owner == LocalClientId || mates.Contains(LocalClientId)) Raise(StateReceived, state, (IReadOnlyList<GameEvent>)events);
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

        [Rpc(SendTo.ClientsAndHost)]
        void ReceivePresenceRpc(ulong sender, PresenceMessage message)
        {
            if (sender == LocalClientId) return;
            Raise(PresenceReceived, sender, message);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ReceiveEmoteRpc(ulong sender, byte index)
        {
            Raise(EmoteReceived, sender, (int)index);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void RivalPowerRpc(ulong actor, ulong target, byte streamer)
        {
            Raise(RivalPowerUsed, actor, target, (StreamerType)streamer);
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
            byte streamer = NetworkService.HasInstance ? NetworkService.Instance.TakePendingStreamer(clientId) : (byte)0;
            ulong platformUserId = NetworkService.HasInstance ? NetworkService.Instance.TakePendingPlatformId(clientId) : 0;
            if (string.IsNullOrEmpty(name)) name = "Player " + (m_Players.Count + 1);
            name = UniqueName(name);

            var player = NetPlayer.Create(clientId, name, NextColorIndex(), streamer, platformUserId);
            // 2v2: newcomers join the smaller team (it can be changed in the lobby with Switch Team).
            player.Team = (byte)(CountTeam(0) <= CountTeam(1) ? 0 : 1);
            m_Players.Add(player);
            if (!IsHostPlayer(clientId)) ServerHeadline(Core.Err.Of("mp.joined", name));
            ServerEnforceModeForPlayerCount();
        }

        void ServerOnClientDisconnected(ulong clientId)
        {
            if (!IsServer || !IsSpawned || NetworkManager.ShutdownInProgress) return;
            int index = IndexOfPlayer(clientId);
            if (index < 0) return;

            var p = m_Players[index];
            string name = p.DisplayName;
            if (Phase == MatchPhase.PickingStreamers)
            {
                m_Players.RemoveAt(index);
                ServerCancelPick();
            }
            else if (Phase == MatchPhase.Lobby)
            {
                m_Players.RemoveAt(index);
            }
            else
            {
                p.Connected = false;
                p.ReadyNextNight = false;
                p.ReadyForShop = false;
                m_Players[index] = p;
                m_ReadyNextNight.Remove(clientId);
                m_ReadyForShop.Remove(clientId);
            }

            m_Viewing.Remove(clientId);
            ServerHeadline(Core.Err.Of("mp.left", name));
            ServerUpdateWatchers();
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

        /// <summary>Sends a headline as a localization key with arguments, so each player reads it in their own language.</summary>
        void ServerHeadline(string key) => HeadlineRpc(NetStrings.Fs128(key));

        /// <summary>
        /// The shop's stock is the one thing players share: every season is simulated separately, so after anything that can
        /// change a collection the host tells each player how many copies of each viewer the others have taken
        /// (<see cref="PlayerState.RivalTaken"/>). Only snapshots that changed are sent again.
        /// </summary>
        void ServerSyncSharedShop()
        {
            // Teammates share a season: count each season once.
            var games = DistinctGames();
            if (games.Count < 2) return;
            int types = GuestCatalog.Count;
            var taken = new Dictionary<ulong, int[]>(games.Count);
            var total = new int[types];
            foreach (var kv in games)
            {
                var counts = new int[types];
                for (int t = 0; t < types; t++)
                {
                    counts[t] = PartyEngine.TakenFromShop(kv.Value, (GuestType)t);
                    total[t] += counts[t];
                }
                taken[kv.Key] = counts;
            }

            var changed = new List<ulong>();
            foreach (var kv in games)
            {
                var own = taken[kv.Key];
                var rivals = kv.Value.RivalTaken;
                bool dirty = false;
                for (int t = 0; t < types && t < rivals.Length; t++)
                {
                    int value = total[t] - own[t];
                    if (rivals[t] == value) continue;
                    rivals[t] = value;
                    dirty = true;
                }
                if (dirty) changed.Add(kv.Key);
            }
            foreach (var id in changed) ServerBroadcastState(id, null);
        }

        /// <summary>Each season once (teammates share one), with one of its players' ids.</summary>
        List<KeyValuePair<ulong, PlayerState>> DistinctGames()
        {
            var seen = new HashSet<PlayerState>();
            var list = new List<KeyValuePair<ulong, PlayerState>>(m_Games.Count);
            foreach (var kv in m_Games)
                if (seen.Add(kv.Value)) list.Add(kv);
            return list;
        }

        /// <summary>
        /// Doomsdayers: a successful stream's Doomsdayers points curse every rival's next stream (each of their viewers may score
        /// nothing). Several curses don't add up: a rival keeps the strongest one waiting for them.
        /// </summary>
        void ServerSendDoom(ulong sender, PlayerState state)
        {
            int points = state.DoomOut;
            if (points <= 0) return;
            state.DoomOut = 0;

            int cursed = 0;
            foreach (var kv in m_Games)
            {
                if (kv.Key == sender || !IsConnectedPlayer(kv.Key) || kv.Value.IsGameOver) continue;
                cursed++;
                if (kv.Value.IncomingDoom >= points) continue;
                kv.Value.IncomingDoom = points;
                ServerBroadcastState(kv.Key, null);
            }
            if (cursed > 0) ServerHeadline(Core.Err.Of("mp.doom", PlayerName(sender), Communities.DoomChance(points)));
        }

        /// <summary>
        /// Saboteurs: a scored stream's Saboteurs points make every rival's next recruit cost that much more Cash. Several
        /// don't add up: a rival keeps the biggest surcharge until their next recruit.
        /// </summary>
        void ServerSendSabotage(ulong sender, PlayerState state)
        {
            int amount = state.SabotageOut;
            if (amount <= 0) return;
            state.SabotageOut = 0;

            int hit = 0;
            foreach (var kv in m_Games)
            {
                if (kv.Key == sender || !IsConnectedPlayer(kv.Key) || kv.Value.IsGameOver) continue;
                hit++;
                if (kv.Value.IncomingSabotage >= amount) continue;
                kv.Value.IncomingSabotage = amount;
                ServerBroadcastState(kv.Key, null);
            }
            if (hit > 0) ServerHeadline(Core.Err.Of("mp.sabotage", PlayerName(sender), amount));
        }

        /// <summary>
        /// Bots: a scored stream's Bots join the Audience of the next player (in player order) for their next stream. Bots from
        /// several rivals add up, to <see cref="Communities.BotsMaxIncoming"/>.
        /// </summary>
        void ServerSendBots(ulong sender, PlayerState state)
        {
            int bots = state.BotsOut;
            if (bots <= 0) return;
            state.BotsOut = 0;

            ulong target = NextPlayerAfter(sender);
            if (target == NoPlayer || !m_Games.TryGetValue(target, out var targetState)) return;
            int before = targetState.IncomingBots;
            targetState.IncomingBots = Mathf.Min(Communities.BotsMaxIncoming, before + bots);
            if (targetState.IncomingBots == before) return;
            ServerBroadcastState(target, null);
            ServerHeadline(Core.Err.Of("mp.bots", PlayerName(sender), targetState.IncomingBots - before) + Core.Err.Separator + PlayerName(target));
        }

        /// <summary>The next connected player after <paramref name="clientId"/> in player order who's still in the season.</summary>
        ulong NextPlayerAfter(ulong clientId)
        {
            int start = IndexOfPlayer(clientId);
            int count = m_Players.Count;
            for (int step = 1; step < count; step++)
            {
                var p = m_Players[((start < 0 ? 0 : start) + step) % count];
                if (p.ClientId == clientId || !p.Connected) continue;
                if (!m_Games.TryGetValue(p.ClientId, out var s) || s.IsGameOver) continue;
                return p.ClientId;
            }
            return NoPlayer;
        }

        /// <summary>Fate's Raid / Ethical F**kboi's Wealth Tax: picks the rival (or uses the one chosen) and applies the power to both seasons.</summary>
        void ServerUseRivalPower(ulong sender, PlayerState state, int requestedIndex)
        {
            ulong target = ClientAtIndex(requestedIndex);
            // A teammate is never a rival (2v2: the power hits the other team's season).
            if (target == sender || SameTeam(target, sender) || !IsConnectedPlayer(target) || !m_Games.ContainsKey(target)) target = AutoRivalFor(sender, state);
            if (target == NoPlayer || !m_Games.TryGetValue(target, out var targetState))
            {
                ServerReject(sender, "err.power_no_rival");
                return;
            }

            var actorEvents = new List<GameEvent>();
            var targetEvents = new List<GameEvent>();
            var result = PartyEngine.UseRivalPower(state, IndexOfPlayer(sender), targetState, IndexOfPlayer(target), actorEvents, targetEvents);
            if (!result.Ok)
            {
                ServerReject(sender, result.Error);
                return;
            }

            ServerBroadcastState(sender, actorEvents);
            ServerBroadcastState(target, targetEvents);
            ServerUpdateSummary(sender);
            ServerUpdateSummary(target);
            RivalPowerRpc(sender, target, (byte)state.Streamer);
            ServerHeadline(Core.Err.Of(state.Streamer == StreamerType.Fate ? "mp.raided" : "mp.taxed", PlayerName(sender), PlayerName(target)));
        }

        /// <summary>Raid hits whoever plays next (in Take Turns) or the leader; Wealth Tax hits the richest rival.</summary>
        ulong AutoRivalFor(ulong sender, PlayerState state)
        {
            ulong best = NoPlayer;
            int bestScore = int.MinValue;
            int senderIndex = IndexOfPlayer(sender);
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                if (p.ClientId == sender || SameTeam(p.ClientId, sender) || !p.Connected || !m_Games.TryGetValue(p.ClientId, out var s) || s.IsGameOver) continue;
                int score;
                if (state.Streamer == StreamerType.EthicalFboi) score = s.Influence * 1000 - i;
                else if (TakingTurns) score = -((i - senderIndex + m_Players.Count) % m_Players.Count);
                else score = s.BestDiamonds * 1000 + s.TotalCashEarned;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = p.ClientId;
                }
            }
            return best;
        }

        void ServerUpdateWatchers()
        {
            if (!IsServer) return;
            for (int i = 0; i < m_Players.Count; i++)
            {
                var p = m_Players[i];
                int watchers = 0;
                foreach (var kv in m_Viewing)
                    if (kv.Key != p.ClientId && kv.Value == p.ClientId && IsConnectedPlayer(kv.Key)) watchers++;
                if (p.Watchers == watchers) continue;
                p.Watchers = (byte)Mathf.Clamp(watchers, 0, 255);
                m_Players[i] = p;
            }
        }

        /// <summary>Refreshes a player's public summary (and their teammates': a team's summary is the team's season).</summary>
        void ServerUpdateSummary(ulong clientId)
        {
            ServerUpdateOneSummary(clientId);
            foreach (var mate in TeammatesOf(clientId)) ServerUpdateOneSummary(mate);
        }

        void ServerUpdateOneSummary(ulong clientId)
        {
            int index = IndexOfPlayer(clientId);
            if (index < 0) return;

            var p = m_Players[index];
            if (m_Games.TryGetValue(clientId, out var s))
            {
                p.Night = (byte)Mathf.Clamp(s.Night, 0, 255);
                p.Phase = (byte)s.Phase;
                p.Cash = s.Cash;
                p.Influence = s.Influence;
                p.Capacity = (byte)Mathf.Clamp(s.TotalCapacity, 0, 255);
                p.Guests = (byte)Mathf.Clamp(s.House.Count, 0, 255);
                p.Trophies = s.Trophies;
                p.DiamondsOwned = (byte)Mathf.Clamp(s.DiamondsOwned(), 0, 255);
                p.DiamondsInHouse = (byte)Mathf.Clamp(PartyEngine.DiamondsPresent(s), 0, 255);
                p.BestDiamonds = (byte)Mathf.Clamp(s.BestDiamonds, 0, 255);
                p.Trouble = (byte)Mathf.Clamp(PartyEngine.NetTrouble(s), 0, 255);
                p.TotalCash = s.TotalCashEarned;
                p.WinNight = (byte)Mathf.Clamp(s.WinNight, 0, 255);
                p.LastOutcome = (byte)s.LastOutcome;
            }
            p.ReadyNextNight = m_ReadyNextNight.Contains(clientId);
            p.ReadyForShop = m_ReadyForShop.Contains(clientId);

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
                        ServerHeadline(Core.Err.Of("mp.platform_ban", name));
                        break;
                    case GameEventType.FireMarshal:
                        ServerHeadline(Core.Err.Of("mp.server_down", name));
                        break;
                    case GameEventType.PartyEnded:
                        // Endless (co-op) has no win, so nobody is ever "one Trophy away".
                        if (e.C == GameRules.DiamondsToWin - 1 && Settings.Mode != MatchMode.CoopEndless) ServerHeadline(Core.Err.Of("mp.one_more", name, e.C));
                        break;
                    case GameEventType.GuestPurchased:
                        if (GuestCatalog.Get(e.Guest).IsDiamond) ServerHeadline(Core.Err.Of("mp.signed", name, Core.Err.Guest(e.Guest)));
                        break;
                    case GameEventType.PowerUsed:
                        if (!StreamerCatalog.Get((StreamerType)e.A).NeedsRival) ServerHeadline(Core.Err.Of("mp.power", name, "@t" + e.A) + Core.Err.Separator + "@w" + e.A);
                        break;
                    case GameEventType.Victory when IsTeamMode:
                        ServerHeadline(Core.Err.Of("mp.team_won", TeamName(TeamOf(clientId)), GameRules.DiamondsToWin) + Core.Err.Separator + e.A);
                        break;
                    case GameEventType.Victory:
                        ServerHeadline(Settings.Mode == MatchMode.Race
                            ? Core.Err.Of("mp.won_race", name, GameRules.DiamondsToWin)
                            : Core.Err.Of("mp.won_finishing", name, GameRules.DiamondsToWin) + Core.Err.Separator + e.A);
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
            bool allReadyForShop = true;
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
                if (s.Phase != GamePhase.Planning || !m_ReadyForShop.Contains(kv.Key)) allReadyForShop = false;
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
            // Free For All: once every stream is over and everyone clicked Go to the Shop, the countdown starts.
            if (Stage == RoundStage.Streaming)
            {
                if (allReadyForShop) ServerStartShopCountdown();
                return;
            }
            if (Stage == RoundStage.Shopping && allReady) ServerAdvanceNight();
        }

        void ServerStartShopCountdown()
        {
            m_ShopOpensAt.Value = NetworkManager.ServerTime.Time + MatchModes.ShopCountdownSeconds;
            m_RoundStage.Value = (byte)RoundStage.Countdown;
            ServerStartTimer(MatchTimer.None);
        }

        void ServerOpenShop()
        {
            m_RoundStage.Value = (byte)RoundStage.Shopping;
            m_ShopOpensAt.Value = 0;
            ServerHeadline("mp.shop_open");
            ServerStartTimer(MatchTimer.Shop);
            ServerEvaluateProgress();
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
            m_ReadyForShop.Clear();
            if (FreeForAll)
            {
                m_RoundStage.Value = (byte)RoundStage.Streaming;
                ServerStartTimer(MatchTimer.Stream);
            }
            foreach (var id in ids) ServerUpdateSummary(id);
            if (night > 0) ServerHeadline(Core.Err.Of("mp.night_begins", night));
        }

        // ------------------------------------------------------------------ Take Turns

        /// <summary>Marks a player's turn as done for this round and hands it on (after a pause, if their season just ended).</summary>
        void ServerEndTurn(ulong clientId, float delay)
        {
            m_ReadyNextNight.Add(clientId);
            m_TurnDeadline.Value = 0;
            m_TimerKind.Value = (byte)MatchTimer.None;
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
            if (!IsServer) return;
            m_TurnDeadline.Value = 0;
            m_TimerKind.Value = (byte)MatchTimer.None;
        }

        void Update()
        {
            if (IsServer && PickingStreamers && NetworkManager.ServerTime.Time >= m_PickDeadline.Value) ServerPickTimedOut();
            if (!IsServer || Phase != MatchPhase.InGame) return;
            if (FreeForAll && Stage == RoundStage.Countdown && NetworkManager.ServerTime.Time >= m_ShopOpensAt.Value) ServerOpenShop();
            if (m_TurnEnding) return;
            double deadline = m_TurnDeadline.Value;
            if (deadline <= 0 || NetworkManager.ServerTime.Time < deadline) return;
            if (TakingTurns) ServerTurnTimedOut(ActivePlayer);
            else if (FreeForAll) ServerRoundTimedOut();
        }

        /// <summary>Extra seconds on the very first timer of a match, while every player's game scene is still loading.</summary>
        const float FirstTimerGrace = 5f;

        /// <summary>
        /// Starts the stream or the shop timer (Take Turns: for the active player; Free For All: for everyone at once).
        /// The stage is remembered even when that timer is off, so the next half knows where the turn is.
        /// </summary>
        void ServerStartTimer(MatchTimer stage, float grace = 0f)
        {
            m_TimerStage = stage;
            int seconds = stage == MatchTimer.Stream ? Settings.TurnSeconds : stage == MatchTimer.Shop ? Settings.ShopSeconds : 0;
            if (!Settings.TimersApply) seconds = 0;
            m_TurnDeadline.Value = seconds > 0 ? NetworkManager.ServerTime.Time + seconds + grace : 0;
            m_TimerKind.Value = (byte)(seconds > 0 ? stage : MatchTimer.None);
        }

        /// <summary>
        /// Finishes a player's stream the safest way when their stream timer runs out: a waiting viewer is denied, a pending
        /// New Attraction is skipped, the stream ends (and is scored normally), and if they were deplatformed a random viewer
        /// gets the Temporary Ban (someone must always be banned). Returns false if the player isn't playing any more.
        /// </summary>
        bool ServerFinishStream(ulong clientId)
        {
            if (!m_Games.TryGetValue(clientId, out var s)) return false;
            for (int guard = 0; guard < 8 && !s.IsGameOver && s.Phase != GamePhase.Planning; guard++)
            {
                GameCommand cmd;
                if (s.Phase == GamePhase.RaidBlame) cmd = GameCommand.Blame(RandomSuspect(s));
                else if (s.PeekedId >= 0) cmd = GameCommand.RejectPeeked();
                else if (s.AttractionPending) cmd = GameCommand.ResolveAttraction(-1, -1);
                // Teams: the timer ends both players' sides at once.
                else cmd = GameCommand.EndParty().ForMember(-1);

                m_Scratch.Clear();
                if (!PartyEngine.Execute(s, cmd, m_Scratch).Ok) break;
                ServerBroadcastState(clientId, m_Scratch);
                ServerAnnounce(clientId, m_Scratch);
                ServerSendDoom(clientId, s);
                ServerSendBots(clientId, s);
                ServerSendSabotage(clientId, s);
            }
            ServerUpdateSummary(clientId);
            return true;
        }

        /// <summary>A random viewer from the ones who could take the Temporary Ban.</summary>
        static int RandomSuspect(PlayerState s) =>
            s.RaidSuspects.Count > 0 ? s.RaidSuspects[UnityEngine.Random.Range(0, s.RaidSuspects.Count)] : -1;

        /// <summary>
        /// Take Turns: a timer ran out. The stream timer finishes the stream (then the shop timer starts); the shop timer
        /// ends the turn. A player stuck on the Temporary Ban when time runs out gets a random viewer banned.
        /// </summary>
        void ServerTurnTimedOut(ulong clientId)
        {
            var stage = m_TimerStage;
            m_TurnDeadline.Value = 0;
            m_TimerKind.Value = (byte)MatchTimer.None;
            if (!m_Games.TryGetValue(clientId, out var s))
            {
                ServerAdvanceTurn();
                return;
            }
            ServerHeadline(Core.Err.Of(stage == MatchTimer.Shop ? "mp.timeout_shop" : "mp.timeout", PlayerName(clientId)));

            if (stage == MatchTimer.Stream || s.Phase != GamePhase.Planning) ServerFinishStream(clientId);
            if (stage == MatchTimer.Stream && !s.IsGameOver && s.Phase == GamePhase.Planning)
            {
                // The stream is over: now the shop has its own timer.
                ServerStartTimer(MatchTimer.Shop);
                ServerEvaluateProgress();
                return;
            }
            ServerEndTurn(clientId, s.IsGameOver ? GameOverTurnDelay : 0f);
            ServerEvaluateProgress();
        }

        /// <summary>
        /// Take Turns: when the active player's stream ends by itself (scored, deplatformed and banned, Server Down), their
        /// shop timer starts.
        /// </summary>
        void ServerCheckTurnStage(ulong clientId, PlayerState state)
        {
            if (!TakingTurns || clientId != ActivePlayer || m_TurnEnding) return;
            if (m_TimerStage == MatchTimer.Stream && state.Phase == GamePhase.Planning) ServerStartTimer(MatchTimer.Shop);
        }

        /// <summary>
        /// Free For All: a timer ran out for everyone. The stream timer finishes every stream still running (a random viewer
        /// gets any pending Temporary Ban) and marks everyone ready for the shop; the shop timer marks everyone ready for the
        /// next stream.
        /// </summary>
        void ServerRoundTimedOut()
        {
            var stage = m_TimerStage;
            m_TurnDeadline.Value = 0;
            m_TimerKind.Value = (byte)MatchTimer.None;
            ServerHeadline(stage == MatchTimer.Shop ? "mp.round_timeout_shop" : "mp.round_timeout_stream");
            var ids = new List<ulong>(m_Games.Keys);
            foreach (var id in ids)
            {
                if (!IsConnectedPlayer(id)) continue;
                var s = m_Games[id];
                if (s.IsGameOver) continue;
                if (stage == MatchTimer.Stream && Stage == RoundStage.Streaming)
                {
                    if (s.Phase != GamePhase.Planning) ServerFinishStream(id);
                    if (s.Phase == GamePhase.Planning) m_ReadyForShop.Add(id);
                }
                else if (stage == MatchTimer.Shop && Stage == RoundStage.Shopping && s.Phase == GamePhase.Planning && !s.IsFinalNight)
                {
                    m_ReadyNextNight.Add(id);
                }
                ServerUpdateSummary(id);
            }
            ServerEvaluateProgress();
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
            // Take Turns: the stream timer while the player streams, then the shop timer (ServerCheckTurnStage).
            ServerStartTimer(s.Phase == GamePhase.Party ? MatchTimer.Stream : MatchTimer.Shop);
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

            // Team modes: teammates share a season, so they share a rank (the team's).
            int teamRank = 0;
            int lastTeam = -1;
            for (int rank = 0; rank < order.Count; rank++)
            {
                var p = m_Players[order[rank]];
                if (IsTeamMode)
                {
                    if (p.Team != lastTeam)
                    {
                        teamRank++;
                        lastTeam = p.Team;
                    }
                    p.Rank = (byte)teamRank;
                }
                else p.Rank = (byte)(rank + 1);
                m_Players[order[rank]] = p;
            }

            m_Phase.Value = MatchPhase.Results;

            if (order.Count > 0 && Settings.Mode == MatchMode.CoopEndless)
            {
                // Co-op: no winner, just how long the run lasted.
                var team = snapshot[order[0]];
                ServerHeadline(Core.Err.Of("mp.coop_over", TeamName(team.Team), team.Night));
            }
            else if (order.Count > 0 && IsTeamMode)
            {
                var winner = snapshot[order[0]];
                ServerHeadline(winner.Won
                    ? Core.Err.Of("mp.team_won", TeamName(winner.Team), GameRules.DiamondsToWin) + Core.Err.Separator + winner.WinNight
                    : Core.Err.Of("mp.team_best", TeamName(winner.Team), GameRules.DiamondsToWin) + Core.Err.Separator + winner.BestDiamonds);
            }
            else if (order.Count > 0)
            {
                var winner = snapshot[order[0]];
                ServerHeadline(winner.Won
                    ? Core.Err.Of("mp.season_won", winner.DisplayName, GameRules.DiamondsToWin) + Core.Err.Separator + winner.WinNight
                    : Core.Err.Of("mp.season_best", winner.DisplayName, GameRules.DiamondsToWin) + Core.Err.Separator + winner.BestDiamonds);
            }
        }

        static int ComparePlayers(NetPlayer a, NetPlayer b)
        {
            if (a.Connected != b.Connected) return a.Connected ? -1 : 1;
            if (a.Won != b.Won) return a.Won ? -1 : 1;
            if (a.Won && a.WinNight != b.WinNight) return a.WinNight.CompareTo(b.WinNight);
            if (a.BestDiamonds != b.BestDiamonds) return b.BestDiamonds.CompareTo(a.BestDiamonds);
            if (a.TotalCash != b.TotalCash) return b.TotalCash.CompareTo(a.TotalCash);
            // Teammates have the same summary: keep them together.
            if (a.Team != b.Team) return a.Team.CompareTo(b.Team);
            return a.ClientId.CompareTo(b.ClientId);
        }
    }
}
