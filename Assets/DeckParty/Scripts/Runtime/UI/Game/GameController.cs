using System.Collections;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Runs the Game scene. Picks a local or networked session, renders the state it publishes, turns engine events into
    /// feedback, and converts clicks / hotkeys into GameCommands.
    /// In multiplayer any player's board can be on screen (it's an open-information game): your own board is interactive
    /// when it's your move, everyone else's is shown live and read-only, with the active player's presence signals.
    /// Split into partial files: this one (flow, rendering, actions), .Presence (presence, emotes, turn timer) and
    /// .Juice (event effects, scoring sequence, link lines).
    /// </summary>
    public sealed partial class GameController : MonoBehaviour
    {
        [Header("Main views")]
        [SerializeField] internal HudView hud;
        [SerializeField] internal GameObject partyRoot;
        [SerializeField] internal HouseView house;
        [SerializeField] internal PartyControlsView partyControls;
        [SerializeField] internal PlanningView planning;
        [SerializeField] internal RaidBlameView raidBlame;
        [SerializeField] internal TargetingBannerView targetingBanner;

        [Header("Side panel")]
        [SerializeField] internal EventLogView log;
        [SerializeField] internal OpponentsView opponents;
        [Tooltip("Multiplayer text chat. Hidden while FX Library > Text Chat is off (players use emotes); kept for future modes.")]
        [SerializeField] internal ChatView chat;
        [Tooltip("The Activity / Chat tabs holding the log and the chat. Hidden unless the Activity log is on (menu) or text chat is.")]
        [SerializeField] internal SideTabsView feed;
        [SerializeField] internal FacecamView facecam;

        [Header("Stream chat")]
        [Tooltip("The chat bubble the viewers sit in, with its tail pointing at the scene above.")]
        [SerializeField] internal ChatBubbleView chatBubble;
        [Tooltip("Emojis floating up from the viewers in chat.")]
        [SerializeField] internal ViewerEmojiView viewerEmojis;
        [Tooltip("Community Beats: each community's live score and bonus, top-right of the chat bubble.")]
        [SerializeField] internal CommunityBeatsView communityBeats;

        [Header("Overlays")]
        [SerializeField] internal GuestPickerView picker;
        [SerializeField] internal RolodexView rolodex;
        [SerializeField] internal GameOverView gameOver;
        [SerializeField] internal PauseMenuView pauseMenu;
        [SerializeField] internal BannerView banner;
        [SerializeField] internal EffectsView effects;
        [SerializeField] internal VenueBackgroundView background;
        [Tooltip("Full-screen Deplatform / Server Down images (Game Database > Stream Endings).")]
        [SerializeField] internal OutcomeSplashView outcomeSplash;
        [Tooltip("Free For All: what every player did this round (opened from the bar above the board).")]
        [SerializeField] internal RoundSummaryView roundSummary;

        [Header("Polish (optional)")]
        [SerializeField] internal StreamerAnxietyView anxiety;
        [SerializeField] internal PresenceView presence;
        [SerializeField] internal EmoteView emotes;
        [SerializeField] internal StreamOverlayView overlay;
        [SerializeField] internal TurnTimerView turnTimer;
        [SerializeField] internal LinkLinesView links;

        const int HistoryLimit = 150;

        IGameSession m_Session;
        DeckPartyNetSession m_Net;
        ViewBarView m_ViewBar;
        string m_VenueChoice;
        string m_IntroLine;
        bool m_GameOverScheduled;
        bool m_GameOverShown;
        bool m_ResultsDismissed;
        readonly List<int> m_Arrivals = new List<int>();
        readonly List<int> m_Pulses = new List<int>();

        // Whose board is on screen (multiplayer), plus every player's log lines.
        ulong m_ViewId;
        readonly Dictionary<ulong, List<string>> m_History = new Dictionary<ulong, List<string>>();

        // Action targeting: the acting viewer, the option chosen in the choice dialog and the viewers picked so far.
        int m_TargetActor = -1;
        int m_Option;
        readonly List<int> m_Picks = new List<int>();

        // Distracted Boyfriend: the revealed viewer chosen, while the player picks who they replace.
        int m_AttractionChoice = -1;
        bool m_AttractionSubmitted;

        /// <summary>The local player's state.</summary>
        PlayerState State => m_Session != null ? m_Session.State : null;
        /// <summary>The state on screen: yours, or the player you're watching.</summary>
        PlayerState View => ViewingOwn ? State : m_Net.GetState(m_ViewId) ?? State;
        ulong LocalId => m_Net != null ? m_Net.LocalClientId : 0UL;
        bool ViewingOwn => m_Net == null || m_ViewId == LocalId;
        bool TakingTurns => m_Net != null && m_Net.TakingTurns;
        bool MyTurn => !TakingTurns || m_Net.ActivePlayer == LocalId;
        /// <summary>True when clicks and hotkeys may act: your own board, and (in Take Turns) your turn.</summary>
        bool CanAct => ViewingOwn && MyTurn;
        string ViewOwnerName => ViewingOwn ? null : NameOf(m_ViewId);
        bool Targeting => m_TargetActor >= 0 || m_AttractionChoice >= 0;

        // ================================================================== lifecycle

        void Start()
        {
            HookViews();

            bool networked = NetworkService.HasInstance && NetworkService.Instance.IsActive && DeckPartyNetSession.Instance != null;
            if (networked) StartNetwork(DeckPartyNetSession.Instance);
            else StartLocal();

            // Multiplayer listens to every player's snapshots instead (see StartNetwork).
            if (m_Net == null) m_Session.StateChanged += OnStateChanged;
            m_Session.CommandRejected += OnCommandRejected;
            Loc.Changed += OnLanguageChanged;
            AppSettings.Changed += UpdateFeedVisibility;
            UpdateFeedVisibility();

            // Re-read the platform colours each game (they may have been edited in the Game Database since the last one).
            SiteTheme.Apply(State != null ? StreamerVisuals.Platform(State.Streamer) : SiteTheme.Platform, true);

            if (State != null)
            {
                TrackRecap(LocalId, State, null);
                ApplyVenue(true);
                RenderAll();
                IntroBanner();
            }
            else
            {
                banner.Show(Loc.Get("game.connecting"), Loc.Get("game.waiting_host"), UIPalette.Teal, 2f);
            }
        }

        void StartLocal()
        {
            var launch = SceneFlow.PendingLaunch;
            SceneFlow.PendingLaunch = null;

            PlayerState state = null;
            string venue = AppSettings.VenueId;

            if (launch != null && launch.ContinueSave && SaveSystem.TryLoad(out var saved, out var savedVenue))
            {
                state = saved;
                venue = savedVenue;
                log.Add(Loc.Get("game.welcome_back", "set", $"<b>{ContentText.ScenarioName(state.ScenarioId)}</b>", "night", state.Night));
            }

            if (state == null)
            {
                int scenarioId = launch != null ? launch.ScenarioId : AppSettings.LastScenario;
                uint seed = launch != null && launch.Seed != 0 ? launch.Seed : NewSeed();
                if (launch != null && !string.IsNullOrEmpty(launch.VenueChoice)) venue = launch.VenueChoice;
                byte pick = launch != null ? launch.Streamer : AppSettings.LastStreamer;
                var streamer = StreamerCatalog.Resolve(pick, new System.Random((int)seed));
                state = PartyEngine.CreateGame(scenarioId, seed, GameRules.DefaultNights, streamer);
                log.Add(Loc.Get("game.welcome", ("set", $"<b>{ContentText.ScenarioName(scenarioId)}</b>"), ("count", GameRules.DiamondsToWin), ("nights", state.TotalNights)));
                if (streamer != StreamerType.None)
                    log.Add(Loc.Get("game.streamer_intro", "streamer", StreamerVisuals.NameTag(streamer), "power", StreamerVisuals.PowerName(streamer)));
            }

            m_VenueChoice = venue;
            m_Session = new LocalGameSession(state, venue);
            opponents.gameObject.SetActive(false);
            if (emotes != null) SetEmotesVisible(false);
        }

        void StartNetwork(DeckPartyNetSession net)
        {
            m_Net = net;
            m_Session = new NetworkGameSession(net);
            m_VenueChoice = AppSettings.VenueId;
            m_ViewId = net.LocalClientId;
            m_RecapActive = net.ActivePlayer;
            if (net.TakingTurns && net.ActivePlayer != DeckPartyNetSession.NoPlayer) m_ViewId = net.ActivePlayer;

            opponents.gameObject.SetActive(true);
            // Text chat is switched off (players talk with emotes); it still works if FX Library > Text Chat is turned on.
            if (TextChatOn) chat.Bind(net);
            if (feed != null) feed.MarkRead(SideTabsView.Tab.Chat);
            opponents.PlayerClicked += ShowBoard;
            opponents.PresenceLine = PresenceLine;
            EventText.PlayerName = index => ColoredName(net.ClientAtIndex(index));
            EventText.Multiplayer = true;

            var panel = targetingBanner != null && targetingBanner.root != null ? targetingBanner.root.GetComponent<Image>() : null;
            m_ViewBar = ViewBarView.Create(partyRoot.transform.parent as RectTransform, hud.menuButton, hud.nightLabel, panel);

            net.PlayersChanged += OnPlayersChanged;
            net.PhaseChanged += OnMatchPhaseChanged;
            net.HeadlineReceived += OnHeadline;
            net.PlayerStateReceived += OnPlayerState;
            net.ActivePlayerChanged += OnActivePlayerChanged;
            net.PresenceReceived += OnPresenceReceived;
            net.EmoteReceived += OnEmoteReceived;
            net.RivalPowerUsed += OnRivalPowerUsed;
            net.StageChanged += OnStageChanged;
            net.RequestState();
            net.ReportViewing(m_ViewId);

            if (emotes != null)
            {
                SetEmotesVisible(FxLibrary.Current.emotes);
                emotes.EmoteClicked += index => net.SendEmote(index);
            }

            m_IntroLine = MultiplayerIntro();
            log.Add(m_IntroLine);
            opponents.Render(net, m_ViewId);
            UpdateViewBar();
        }

        string MultiplayerIntro()
        {
            var settings = m_Net.Settings;
            return Loc.Get("game.mp_intro", ("set", $"<b>{ContentText.ScenarioName(settings.ScenarioId)}</b>"), ("nights", settings.Nights), ("mode", MatchModes.ShortName(settings.Mode)));
        }

        bool TextChatOn => m_Net != null && FxLibrary.Current.textChat;

        /// <summary>The side panel's Activity log is off unless switched on in the menu; the Chat tab only exists with text chat on.</summary>
        void UpdateFeedVisibility()
        {
            bool activity = AppSettings.ShowActivityLog;
            bool chatOn = TextChatOn;
            if (feed != null) feed.SetAvailable(activity, chatOn);
            else
            {
                if (log != null) log.gameObject.SetActive(activity);
                if (chat != null) chat.gameObject.SetActive(chatOn);
            }
        }

        /// <summary>The emote buttons sit in their own panel in the side panel (multiplayer only).</summary>
        void SetEmotesVisible(bool visible)
        {
            emotes.gameObject.SetActive(visible);
            // The panel is the buttons' parent, or its parent when the panel has a border (bordered panels hold a "Fill").
            var panel = emotes.transform.parent;
            if (panel != null && panel.name != EmoteBarName) panel = panel.parent;
            if (panel != null && panel.name == EmoteBarName) panel.gameObject.SetActive(visible);
        }

        /// <summary>Name of the side panel's emote panel (GameSceneBuilder).</summary>
        public const string EmoteBarName = "EmoteBar";

        void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            AppSettings.Changed -= UpdateFeedVisibility;
            EventText.PlayerName = null;
            EventText.Multiplayer = false;
            CommunityVisuals.LiveState = null;
            if (m_Session != null)
            {
                m_Session.StateChanged -= OnStateChanged;
                m_Session.CommandRejected -= OnCommandRejected;
                m_Session.Dispose();
            }
            if (m_Net != null)
            {
                m_Net.PlayersChanged -= OnPlayersChanged;
                m_Net.PhaseChanged -= OnMatchPhaseChanged;
                m_Net.HeadlineReceived -= OnHeadline;
                m_Net.PlayerStateReceived -= OnPlayerState;
                m_Net.ActivePlayerChanged -= OnActivePlayerChanged;
                m_Net.PresenceReceived -= OnPresenceReceived;
                m_Net.EmoteReceived -= OnEmoteReceived;
                m_Net.RivalPowerUsed -= OnRivalPowerUsed;
                m_Net.StageChanged -= OnStageChanged;
            }
            AudioService.SetMuffled(false);
        }

        void HookViews()
        {
            hud.RolodexClicked += ToggleRolodex;
            hud.MenuClicked += OpenPause;

            house.AbilityClicked += card => BeginAbility(card.GuestId);
            house.CardClicked += OnHouseCardClicked;

            partyControls.DoorClicked += () => Submit(GameCommand.OpenDoor());
            partyControls.EndClicked += EndParty;
            partyControls.AdmitClicked += () => Submit(GameCommand.AdmitPeeked());
            partyControls.RejectClicked += () => Submit(GameCommand.RejectPeeked());

            planning.BuyClicked += type => Submit(GameCommand.Buy(type));
            planning.ExpandClicked += () => Submit(GameCommand.ExpandHouse());
            planning.RolodexClicked += ToggleRolodex;
            planning.NextNightClicked += NextNight;
            planning.PowerClicked += UseStreamerPower;
            planning.ShopReadyClicked += ToggleShopReady;

            raidBlame.GuestChosen += id => Submit(GameCommand.Blame(id));
            targetingBanner.CancelClicked += () => CancelTargeting(true);
            targetingBanner.ConfirmClicked += ConfirmTargets;

            pauseMenu.QuitClicked += QuitGame;
            pauseMenu.VenueChosen += ChangeVenue;
            pauseMenu.OpenChanged += open => AudioService.SetMuffled(open);

            HookPresenceSources();
        }

        static uint NewSeed() => (uint)Random.Range(1, int.MaxValue);

        void IntroBanner()
        {
            if (TakingTurns)
            {
                AnnounceTurn(m_Net.ActivePlayer);
                return;
            }
            var s = State;
            if (s == null || s.Phase != GamePhase.Party || s.House.Count > 0) return;
            banner.Show(Loc.Get("game.night_banner", "night", s.Night),
                Loc.Get("game.night_sub", "set", ContentText.ScenarioName(s.ScenarioId), "count", s.NightsRemaining), UIPalette.Purple, 1.2f);
        }

        void OnLanguageChanged()
        {
            if (m_Net != null)
            {
                m_IntroLine = MultiplayerIntro();
                RefillLog();
            }
            RenderAll();
            if (Targeting) UpdateTargetingBanner();
        }

        // ================================================================== state updates

        void OnStateChanged(PlayerState s, IReadOnlyList<GameEvent> events) => OnPlayerState(LocalId, s, events);

        void OnPlayerState(ulong owner, PlayerState s, IReadOnlyList<GameEvent> events)
        {
            bool own = owner == LocalId;
            RecordRound(owner, s, events);
            TrackRecap(owner, s, events);
            var lines = Describe(s, events, own ? null : NameOf(owner));
            Remember(owner, lines);
            bool switched = false;

            if (own)
            {
                m_AttractionSubmitted = false;
                if (m_TargetActor >= 0 && (s.Phase != GamePhase.Party || PartyEngine.GetAbilityBlocker(s, m_TargetActor) != null)) CancelTargeting(false);
                if (m_AttractionChoice >= 0 && (!s.AttractionPending || !s.AttractionCandidates.Contains(m_AttractionChoice))) CancelTargeting(false);
                if (picker.IsOpen && s.Phase != GamePhase.Party) picker.Close();

                // Synchronized / Race: when your next night starts, come back from watching someone else.
                if (!ViewingOwn && !TakingTurns && HasEvent(events, GameEventType.NightStarted))
                {
                    ShowBoard(owner);
                    switched = true;
                }
            }

            if (owner != m_ViewId)
            {
                // Not on screen: the side panel updates from the player list. Your own changes can still end your season.
                if (own) RenderAll();
                return;
            }

            if (!switched)
                foreach (var line in lines) log.Add(line);

            // A successful stream ends with the scoring sequence before the shop appears.
            if (TryStartScoring(s, events)) return;

            ApplyVenue(false);
            ProcessEvents(s, events, own ? null : NameOf(owner));
            RenderAll();
            PlayCardAnimations(s, events);
        }

        void OnCommandRejected(string reason)
        {
            m_AttractionSubmitted = false;
            ToastView.Show(Loc.Error(reason), ToastKind.Warning);
            AudioService.Play(Sfx.Error);
        }

        static bool HasEvent(IReadOnlyList<GameEvent> events, GameEventType type)
        {
            if (events == null) return false;
            foreach (var e in events)
                if (e.Type == type) return true;
            return false;
        }

        static List<string> Describe(PlayerState s, IReadOnlyList<GameEvent> events, string owner)
        {
            var lines = new List<string>();
            if (events == null) return lines;
            foreach (var e in events)
            {
                var line = EventText.Describe(e, s, owner);
                if (line != null) lines.Add(line);
            }
            return lines;
        }

        void Remember(ulong owner, List<string> lines)
        {
            if (m_Net == null || lines.Count == 0) return;
            if (!m_History.TryGetValue(owner, out var history)) m_History[owner] = history = new List<string>();
            history.AddRange(lines);
            if (history.Count > HistoryLimit) history.RemoveRange(0, history.Count - HistoryLimit);
        }

        // ================================================================== watching other players

        string NameOf(ulong clientId) => m_Net != null ? m_Net.PlayerName(clientId) : Loc.Get("common.you");

        Color ColorOf(ulong clientId) =>
            m_Net != null && m_Net.TryGetPlayer(clientId, out var p) ? UIPalette.PlayerColor(p.ColorIndex) : UIPalette.Teal;

        string ColoredName(ulong clientId) => GuestVisuals.Colored(NameOf(clientId), ColorOf(clientId));

        /// <summary>Puts a player's board on screen (multiplayer). Your own board is interactive when it's your move.</summary>
        void ShowBoard(ulong clientId)
        {
            if (m_Net == null) return;
            // Free For All: everyone streams at once and nobody watches anyone else (the round summary tells what happened).
            if (FreeForAll && clientId != LocalId)
            {
                ToastView.Show(Loc.Get("game.ffa_no_watching"), ToastKind.Info);
                return;
            }
            if (clientId != LocalId && m_Net.GetState(clientId) == null)
            {
                ToastView.Show(Loc.Get("game.board_loading", "player", NameOf(clientId)), ToastKind.Info);
                return;
            }

            if (clientId != m_ViewId)
            {
                CancelTargeting(false);
                if (picker.IsOpen) picker.Close();
                m_ViewId = clientId;
                m_LastLive = null;
                m_Arrivals.Clear();
                m_Pulses.Clear();
                RefillLog();
                if (presence != null) presence.Clear();
                // Nothing visual from the previous board may carry over: these objects are shared by every board.
                if (viewerEmojis != null) viewerEmojis.Clear();
                if (planning.power != null) planning.power.ResetPose();
                if (outcomeSplash != null) outcomeSplash.Hide();
                m_Net.ReportViewing(clientId);
                Fx.Play(FxCue.BoardSwitch, partyRoot.transform);
                ApplyVenue(true);
            }
            RenderAll();
            if (m_Presence.TryGetValue(clientId, out var msg)) RenderPresence(clientId, msg);
        }

        void RefillLog()
        {
            log.Clear();
            if (!string.IsNullOrEmpty(m_IntroLine)) log.Add(m_IntroLine);
            if (m_History.TryGetValue(m_ViewId, out var history))
                foreach (var line in history) log.Add(line);
            // Another player's history isn't news.
            if (feed != null) feed.MarkRead(SideTabsView.Tab.Activity);
        }

        void OnActivePlayerChanged(ulong active)
        {
            if (!TakingTurns) return;
            TrackRecapTurn(active);
            m_Presence.Clear();
            if (presence != null) presence.Clear();
            if (active == DeckPartyNetSession.NoPlayer)
            {
                RenderAll();
                return;
            }
            // Everyone follows the player whose turn it is.
            ShowBoard(active);
            AnnounceTurn(active);
        }

        void AnnounceTurn(ulong active)
        {
            if (m_Net == null || active == DeckPartyNetSession.NoPlayer || m_Net.Phase != MatchPhase.InGame) return;
            var s = m_Net.GetState(active);
            string night = s != null ? Loc.Get("game.night_of", "night", s.Night, "total", s.TotalNights) : string.Empty;
            if (active == LocalId)
            {
                banner.Show(Loc.Get("game.your_turn"), night + "  -  " + Loc.Get("game.everyone_watching"), UIPalette.Gold, 1.4f);
                AudioService.Play(Sfx.Notify);
            }
            else
            {
                banner.Show(Loc.Get("game.their_turn", "player", NameOf(active)), night + "  -  " + Loc.Get("game.watching_live"), ColorOf(active), 1.2f);
            }
            Fx.Play(FxCue.TurnBanner, banner.transform);
        }

        void UpdateViewBar()
        {
            if (m_ViewBar == null || m_Net == null) return;
            if (UpdateFreeForAllBar()) return;
            ulong active = m_Net.ActivePlayer;
            string watchers = WatchersText();

            if (TakingTurns && m_Net.Phase == MatchPhase.InGame && active != DeckPartyNetSession.NoPlayer)
            {
                if (active == LocalId)
                {
                    if (ViewingOwn) m_ViewBar.Set(Loc.Get("bar.your_turn_watching") + watchers, UIPalette.Gold, null, null);
                    else m_ViewBar.Set(Loc.Get("bar.your_turn_looking", "player", ColoredName(m_ViewId)), UIPalette.Gold, Loc.Get("bar.back_to_turn"), () => ShowBoard(LocalId));
                }
                else if (m_ViewId == active)
                {
                    m_ViewBar.Set(Loc.Get("bar.their_turn", "player", ColoredName(active)), ColorOf(active), Loc.Get("bar.my_stream"), () => ShowBoard(LocalId));
                }
                else
                {
                    string whose = ViewingOwn ? Loc.Get("bar.your_stream") : Loc.Get("bar.players_stream", "player", ColoredName(m_ViewId));
                    m_ViewBar.Set(Loc.Get("bar.elsewhere", "whose", whose, "player", ColoredName(active)), ColorOf(m_ViewId), Loc.Get("bar.watch", "player", NameOf(active)), () => ShowBoard(active));
                }
                return;
            }

            if (ViewingOwn) m_ViewBar.Set(Loc.Get("bar.own") + watchers, UIPalette.Teal, null, null);
            else m_ViewBar.Set(Loc.Get("bar.watching", "player", ColoredName(m_ViewId)), ColorOf(m_ViewId), Loc.Get("bar.back_to_stream"), () => ShowBoard(LocalId));
        }

        /// <summary>"   👁 2 watching" for your own stream (presence P5).</summary>
        string WatchersText()
        {
            if (m_Net == null || !m_Net.TryGetLocalPlayer(out var me) || me.Watchers == 0) return string.Empty;
            return $"   <color=#FF9AD5>{Loc.Get("bar.watchers", "count", me.Watchers)}</color>";
        }

        // ================================================================== rendering

        void RenderAll()
        {
            if (m_Scoring) return;
            var s = View;
            if (s == null) return;
            bool act = CanAct;
            // Community icons show live scores for the stream on screen.
            CommunityVisuals.LiveState = s;
            // The site takes the look of the on-screen streamer's platform (logo and button colour).
            SiteTheme.Apply(StreamerVisuals.Platform(s.Streamer));

            hud.Render(s);

            bool party = s.Phase == GamePhase.Party;
            bool planningPhase = s.Phase == GamePhase.Planning;
            bool blame = s.Phase == GamePhase.RaidBlame;

            partyRoot.SetActive(party || s.IsGameOver);
            planning.gameObject.SetActive(planningPhase);
            raidBlame.gameObject.SetActive(blame);

            if (party || s.IsGameOver)
            {
                house.Render(s);
                ApplyCardStates(s, act);
                partyControls.Render(s, Targeting, act);
                if (communityBeats != null) communityBeats.Render(s);
                RenderLinks(s);
                if (party) m_LastLive = s.Clone();
            }
            // The risky-queue badge lives in the menu bar: only during a stream.
            if (!party) partyControls.HideRisk();
            if (planningPhase) planning.Render(s, PlanningCtx());
            if (blame) raidBlame.Render(s, act ? null : Loc.Get("ban.watching", "player", ViewingOwn ? Loc.Get("common.you") : NameOf(m_ViewId)));

            int trouble = party ? PartyEngine.NetTrouble(s) : 0;
            bool anxious = party && trouble >= GameRules.RaidTroubleThreshold - 1;
            background.SetEnergy(party ? s.Occupancy / (float)Mathf.Max(1, s.Capacity) : 0.2f);
            background.SetRisk(party ? trouble / (float)(GameRules.RaidTroubleThreshold - 1) : 0f);
            effects.SetDanger(anxious ? 1f : 0f);
            if (anxiety != null) anxiety.Set(anxious, s.Streamer);
            if (overlay != null) overlay.Render(s);
            if (facecam != null) facecam.Render(s, m_Net != null ? NameOf(m_ViewId) : null, ColorOf(m_ViewId), anxious);
            if (viewerEmojis != null) viewerEmojis.Render(s, party && !m_Scoring);

            if (rolodex.IsOpen) rolodex.Refresh(s, ViewOwnerName);
            if (m_Net != null)
            {
                opponents.Render(m_Net, m_ViewId);
                UpdateViewBar();
            }

            UpdateMusic(s);
            var own = State;
            if (own != null)
            {
                CheckGameOver(own);
                if (act) PromptAttraction(own);
            }
        }

        void ApplyCardStates(PlayerState s, bool act)
        {
            string watchHint = act ? null : ViewingOwn ? Loc.Get("game.wait_turn_hint", "player", NameOf(m_Net.ActivePlayer)) : Loc.Get("game.watching_hint", "player", NameOf(m_ViewId));
            foreach (var card in house.Cards)
            {
                if (!card.gameObject.activeSelf) continue;
                if (card.IsEmpty)
                {
                    card.SetHighlight(Targeting ? CardHighlight.Dimmed : CardHighlight.None);
                    continue;
                }

                var g = s.Find(card.GuestId);
                if (g == null) continue;

                if (Targeting && act)
                {
                    bool picked = m_Picks.Contains(g.Id);
                    bool valid = m_AttractionChoice >= 0
                        ? PartyEngine.IsValidAttractionTarget(s, g.Id)
                        : PartyEngine.IsValidTarget(s, m_TargetActor, g.Id, m_Picks);
                    card.SetHighlight(picked ? CardHighlight.Selected : valid ? CardHighlight.Selectable : CardHighlight.Dimmed);
                    card.SetClickable(valid || picked);
                    card.HideAbility();
                    if (card.Fx != null) card.Fx.SetStatus(s, g, true, valid && !picked, picked, false);
                }
                else
                {
                    card.SetHighlight(CardHighlight.None);
                    card.SetClickable(false);
                    if (g.Info.HasAbility && s.Phase == GamePhase.Party)
                    {
                        bool used = s.UsedAbilities.Contains(g.Id);
                        var blocker = act ? PartyEngine.GetAbilityBlocker(s, g.Id) : null;
                        string reason = act ? (blocker != null ? Loc.Error(blocker) : null) : watchHint;
                        card.ShowAbility(GuestVisuals.ActionName(g.Kind), act && blocker == null && !used, reason, used);
                    }
                    else
                    {
                        card.HideAbility();
                    }
                }
            }
        }

        PlanningContext PlanningCtx()
        {
            var ctx = new PlanningContext();
            if (m_Net == null) return ctx;

            ctx.Multiplayer = true;
            ctx.SyncNights = m_Net.Settings.Mode == MatchMode.SyncNights;
            ctx.Turns = TakingTurns;
            ctx.MyTurn = MyTurn;
            ctx.Owner = ViewOwnerName;
            ctx.RivalsAvailable = m_Net.HasRival(ViewingOwn ? LocalId : m_ViewId);
            if (TakingTurns && m_Net.ActivePlayer != DeckPartyNetSession.NoPlayer)
            {
                ctx.ActiveName = NameOf(m_Net.ActivePlayer);
                ctx.NextName = m_Net.NextTurnName();
            }
            if (m_Net.TryGetLocalPlayer(out var me)) ctx.LocalReady = me.ReadyNextNight;
            FillShopGate(ref ctx);

            var waiting = new List<string>();
            foreach (var p in m_Net.PlayersSnapshot())
            {
                if (p.ClientId == m_Net.LocalClientId || !p.Connected || p.Finished) continue;
                if (!(p.GamePhase == GamePhase.Planning && p.ReadyNextNight)) waiting.Add(p.DisplayName);
            }
            ctx.WaitingFor = string.Join(", ", waiting);
            return ctx;
        }

        /// <summary>
        /// The background follows the board on screen: your own setup choice on your board (a streamer brings theirs on
        /// "Automatic"), and the other player's streamer setup when you watch them.
        /// </summary>
        void ApplyVenue(bool force)
        {
            var s = View;
            string choice = ViewingOwn ? m_VenueChoice : AppSettings.VenueAuto;
            var venue = GameDatabase.Instance.ResolveVenue(choice, s);
            if (venue != null) background.Apply(venue, force);
            if (chatBubble != null) chatBubble.Apply(venue);
        }

        void UpdateMusic(PlayerState s)
        {
            AudioClip clip = AudioService.PartyMusic(background.Current);
            if (s.Phase != GamePhase.Party)
            {
                var planningClip = AudioService.PlanningMusic();
                if (planningClip != null) clip = planningClip;
            }
            AudioService.PlayMusic(clip);
        }

        // ================================================================== game over

        void CheckGameOver(PlayerState s)
        {
            bool over = s.IsGameOver || (m_Net != null && m_Net.Phase == MatchPhase.Results);
            if (!over) return;

            if (m_GameOverShown)
            {
                if (m_Net != null) ShowMultiplayerResults();
                return;
            }
            if (m_GameOverScheduled) return;
            m_GameOverScheduled = true;
            StartCoroutine(ShowGameOverAfter(s.IsGameOver ? 2.0f : 0.4f));
        }

        IEnumerator ShowGameOverAfter(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            while (m_Scoring) yield return null;
            m_GameOverShown = true;
            CancelTargeting(false);
            picker.Close();
            rolodex.Close();
            pauseMenu.Close();
            if (roundSummary != null) roundSummary.Close();

            if (m_Net == null) gameOver.ShowSingle(State, PlayAgain, () => SceneFlow.GoToMainMenu());
            else ShowMultiplayerResults();
        }

        void ShowMultiplayerResults()
        {
            if (m_Net == null || !m_Net.IsSpawned) return;
            bool matchOver = m_Net.Phase == MatchPhase.Results;
            // Your season can end before everyone else's; you may close the scoreboard and keep watching.
            if (!matchOver && m_ResultsDismissed) return;

            if (matchOver && m_Net.IsServer)
                gameOver.ShowMultiplayer(m_Net, State, true, () => m_Net.ServerReturnToLobby(), Loc.Get("over.back_to_lobby"), LeaveGame, Loc.Get("over.close_lobby"), null);
            else if (matchOver)
                gameOver.ShowMultiplayer(m_Net, State, true, null, null, LeaveGame, Loc.Get("over.leave"), Loc.Get("over.waiting_host"));
            else
                gameOver.ShowMultiplayer(m_Net, State, false, KeepWatching, Loc.Get("over.keep_watching"), LeaveGame, Loc.Get("over.leave"), Loc.Get("over.scoreboard_live"));
        }

        void KeepWatching()
        {
            m_ResultsDismissed = true;
            gameOver.Hide();
        }

        void PlayAgain()
        {
            var s = State;
            SceneFlow.StartSinglePlayer(new SinglePlayerLaunch
            {
                ScenarioId = s != null ? s.ScenarioId : 0,
                Seed = NewSeed(),
                VenueChoice = m_VenueChoice,
                Streamer = s != null ? (byte)s.Streamer : AppSettings.LastStreamer,
            });
        }

        // ================================================================== multiplayer callbacks

        void OnPlayersChanged()
        {
            if (m_Net == null) return;
            opponents.Render(m_Net, m_ViewId);
            UpdateViewBar();
            var s = View;
            if (s != null && s.Phase == GamePhase.Planning && !m_Scoring) planning.Render(s, PlanningCtx());
            if (m_GameOverShown) ShowMultiplayerResults();
        }

        void OnMatchPhaseChanged(MatchPhase phase)
        {
            if (State != null) CheckGameOver(State);
            UpdateViewBar();
        }

        void OnHeadline(string text) => ToastView.Show(Loc.Error(text), ToastKind.Info);

        // ================================================================== player actions

        void Submit(GameCommand command)
        {
            if (State == null || !CanAct || m_Scoring) return;
            m_Session.Submit(command);
        }

        void EndParty()
        {
            var s = State;
            if (s == null || s.Phase != GamePhase.Party || !CanAct) return;
            if (s.House.Count == 0)
            {
                ModalDialog.Show(Loc.Get("game.end_empty_title"), Loc.Get("game.end_empty_body"), () => Submit(GameCommand.EndParty()),
                    Loc.Get("game.end_it"), Loc.Get("game.keep_streaming"));
                return;
            }
            Submit(GameCommand.EndParty());
        }

        void NextNight()
        {
            var s = State;
            if (s == null || s.Phase != GamePhase.Planning || !CanAct) return;

            // Free For All, before the shop opens: Space / the button marks you ready for the shop.
            if (ShopGated)
            {
                ToggleShopReady();
                return;
            }

            if (m_Net != null && m_Net.Settings.Mode == MatchMode.SyncNights)
            {
                m_Net.TryGetLocalPlayer(out var me);
                if (me.ReadyNextNight) m_Net.CancelNextNightReady();
                else Submit(GameCommand.StartNextNight());
                return;
            }
            // Take Turns: this ends your turn.
            Submit(GameCommand.StartNextNight());
        }

        // ------------------------------------------------------------------ streamer power

        void UseStreamerPower()
        {
            var s = State;
            if (s == null || !CanAct) return;
            bool rivals = m_Net != null && m_Net.HasRival(LocalId);
            var blocker = PartyEngine.GetPowerBlocker(s, rivals);
            if (blocker != null)
            {
                OnCommandRejected(blocker);
                return;
            }

            if (!s.StreamerInfo.NeedsRival || m_Net == null)
            {
                Submit(GameCommand.UsePower());
                return;
            }

            // Rival powers: choose who to hit when there's more than one rival.
            var options = new List<string>();
            var indexes = new List<int>();
            var players = m_Net.PlayersSnapshot();
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p.ClientId == LocalId || !p.Connected || p.Finished) continue;
                options.Add(Loc.Get("power.rival_option", "player", p.DisplayName, "cash", p.Cash));
                indexes.Add(i);
            }
            if (options.Count <= 1)
            {
                Submit(GameCommand.UsePower(indexes.Count == 1 ? indexes[0] : -1));
                return;
            }
            m_ChoosingRival = true;
            ModalDialog.ShowChoice(Loc.Get("power.pick_rival_title", "power", StreamerVisuals.PowerName(s.Streamer)), Loc.Get("power.pick_rival_body"), options,
                pick =>
                {
                    m_ChoosingRival = false;
                    Submit(GameCommand.UsePower(indexes[Mathf.Clamp(pick, 0, indexes.Count - 1)]));
                },
                () => m_ChoosingRival = false);
        }

        // ------------------------------------------------------------------ actions: option -> targets -> submit

        void BeginAbility(int actorId)
        {
            var s = State;
            if (s == null || !CanAct) return;
            if (Targeting) CancelTargeting(false);

            var blocker = PartyEngine.GetAbilityBlocker(s, actorId);
            if (blocker != null)
            {
                OnCommandRejected(blocker);
                RenderAll();
                return;
            }

            var guest = s.Find(actorId);
            var info = guest.Info;
            var options = GuestVisuals.Options(guest.Kind);
            if (options == null)
            {
                ContinueAbility(actorId, 0);
                return;
            }

            m_ChoosingActor = actorId;
            ModalDialog.ShowChoice($"{GuestVisuals.DisplayName(guest.Kind)}: {GuestVisuals.ActionName(guest.Kind)}", OptionHelp(s, info.Ability), options,
                option =>
                {
                    m_ChoosingActor = -1;
                    ContinueAbility(actorId, option);
                },
                () => m_ChoosingActor = -1, OptionsEnabled(s, info.Ability, options.Length));
        }

        static string OptionHelp(PlayerState s, AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.Mood:
                    return Loc.Get("help.mood");
                case AbilityKind.Invest:
                    return Loc.Get("help.invest", ("subs", GameRules.StonksInvestPop), ("cash", GameRules.StonksInvestCash), ("mult", GameRules.StonksMultiplier),
                        ("have_subs", s.Pop), ("have_cash", s.Cash));
                case AbilityKind.Reroll:
                {
                    var last = PartyEngine.CanReroll(s) ? s.Find(s.LastRandomArrival) : null;
                    var reroll = last != null ? Loc.Get("help.reroll", "viewer", GuestVisuals.NameTag(last.Kind)) : Loc.Get("help.reroll_none");
                    return reroll + "\n" + Loc.Get("help.peek");
                }
                case AbilityKind.Drake:
                    return Loc.Get("help.drake");
                case AbilityKind.Convert:
                    return Loc.Get("help.convert");
                default:
                    return string.Empty;
            }
        }

        static bool[] OptionsEnabled(PlayerState s, AbilityKind kind, int count)
        {
            var enabled = new bool[count];
            for (int i = 0; i < count; i++) enabled[i] = true;
            if (kind == AbilityKind.Invest && count >= 2)
            {
                enabled[0] = s.Pop >= GameRules.StonksInvestPop;
                enabled[1] = s.Cash >= GameRules.StonksInvestCash;
            }
            else if (kind == AbilityKind.Reroll && count >= 2)
            {
                enabled[0] = PartyEngine.CanReroll(s);
                enabled[1] = s.Pool.Count > 0;
            }
            return enabled;
        }

        void ContinueAbility(int actorId, int option)
        {
            var s = State;
            if (s == null || !CanAct) return;
            var blocker = PartyEngine.GetAbilityBlocker(s, actorId);
            if (blocker != null)
            {
                OnCommandRejected(blocker);
                return;
            }

            var guest = s.Find(actorId);
            var info = guest.Info;
            string title = $"{GuestVisuals.DisplayName(guest.Kind)}: {GuestVisuals.ActionName(guest.Kind)}";

            switch (info.Target)
            {
                case TargetMode.None:
                    Submit(GameCommand.UseAbility(actorId, option: option));
                    break;

                case TargetMode.WaitingGuest:
                    m_PickingActor = actorId;
                    picker.Open(title, Loc.Get("pick.fetch"), s, s.Pool,
                        pick =>
                        {
                            m_PickingActor = -1;
                            Submit(GameCommand.UseAbility(actorId, pick, option: option));
                        }, () => m_PickingActor = -1);
                    break;

                case TargetMode.Inspect:
                    BeginInspect(actorId, title);
                    break;

                default:
                    m_TargetActor = actorId;
                    m_Option = option;
                    m_Picks.Clear();
                    UpdateTargetingBanner();
                    RenderAll();
                    break;
            }
        }

        /// <summary>Grumpy Cat: deny one of the next viewers, then choose who comes next.</summary>
        void BeginInspect(int actorId, string title)
        {
            var s = State;
            var looked = s.Pool.GetRange(0, Mathf.Min(GameRules.GrumpyInspectCount, s.Pool.Count));
            m_PickingActor = actorId;
            picker.Open(title, Loc.Get("pick.inspect_reject"), s, looked, rejected =>
            {
                var rest = new List<int>(looked);
                rest.Remove(rejected);
                if (rest.Count <= 1)
                {
                    m_PickingActor = -1;
                    Submit(GameCommand.UseAbility(actorId, rejected, rest.Count == 1 ? rest[0] : -1));
                    return;
                }
                picker.Open(title, Loc.Get("pick.inspect_next"), State, rest,
                    first =>
                    {
                        m_PickingActor = -1;
                        Submit(GameCommand.UseAbility(actorId, rejected, first));
                    }, () => m_PickingActor = -1, keepOrder: true);
            }, () => m_PickingActor = -1, keepOrder: true);
        }

        void UpdateTargetingBanner()
        {
            var s = State;
            if (m_AttractionChoice >= 0)
            {
                var chosen = s?.Find(m_AttractionChoice);
                string who = chosen != null ? GuestVisuals.NameTag(chosen.Kind) : Loc.Get("common.someone");
                targetingBanner.Show(Loc.Get("target.attraction", "viewer", who));
                targetingBanner.SetConfirm(false);
                return;
            }

            var actor = s?.Find(m_TargetActor);
            if (actor == null) return;
            GuestInfo.TargetCount(actor.Info.Ability, out int min, out int max);
            targetingBanner.Show(TargetPrompt(s, actor));
            targetingBanner.SetConfirm(max > min, m_Picks.Count >= min, Loc.Get("target.confirm_count", "count", m_Picks.Count, "max", max));
        }

        string TargetPrompt(PlayerState s, GuestInstance actor)
        {
            var info = actor.Info;
            string who = GuestVisuals.NameTag(actor.Kind);
            string first = m_Picks.Count > 0 && s.Find(m_Picks[0]) != null ? GuestVisuals.NameTag(s.Find(m_Picks[0]).Kind) : string.Empty;
            switch (info.Ability)
            {
                case AbilityKind.Boot: return Loc.Get("target.boot", "actor", who, "self", GuestVisuals.DisplayName(actor.Kind));
                case AbilityKind.Troll: return Loc.Get("target.troll", "actor", who);
                case AbilityKind.PlayOff: return Loc.Get("target.playoff", "actor", who);
                case AbilityKind.Refund: return Loc.Get("target.refund", "actor", who);
                case AbilityKind.Mog: return Loc.Get("target.mog", "actor", who);
                case AbilityKind.Arson: return Loc.Get("target.arson", "actor", who);
                case AbilityKind.Strike: return Loc.Get("target.strike", "actor", who);
                case AbilityKind.Rickroll: return Loc.Get("target.rickroll", "actor", who);
                case AbilityKind.Convert: return Loc.Get("target.convert", "actor", who, "option", GuestVisuals.OptionLabel(actor.Kind, m_Option));
                case AbilityKind.Immunity: return Loc.Get("target.immunity", "actor", who);
                case AbilityKind.SwapOrder:
                    return m_Picks.Count == 0 ? Loc.Get("target.swap_first", "actor", who) : Loc.Get("target.swap_second", "actor", who, "first", first);
                case AbilityKind.Pirouette:
                    return m_Picks.Count == 0 ? Loc.Get("target.pirouette_first", "actor", who) : Loc.Get("target.pirouette_second", "actor", who, "first", first);
                case AbilityKind.Drake:
                    return m_Picks.Count == 0 ? Loc.Get("target.drake_first", "actor", who) : Loc.Get(m_Option == 0 ? "target.drake_second_subs" : "target.drake_second_cash", "actor", who);
                case AbilityKind.Bombard:
                    return Loc.Get("target.bombard", "actor", who);
                case AbilityKind.Photograph: return Loc.Get("target.photograph", "actor", who);
                case AbilityKind.Style: return Loc.Get("target.style", "actor", who, "cap", GameRules.PermanentCashCap);
                case AbilityKind.BootPair:
                    return m_Picks.Count == 0 ? Loc.Get("target.bot_pair_first", "actor", who) : Loc.Get("target.bot_pair_second", "actor", who, "first", first);
                default: return Loc.Get("target.default", "actor", who);
            }
        }

        void OnHouseCardClicked(GuestCardView card)
        {
            var s = State;
            if (!Targeting || !CanAct || s == null || card.IsEmpty) return;
            int id = card.GuestId;

            if (m_AttractionChoice >= 0)
            {
                if (!PartyEngine.IsValidAttractionTarget(s, id)) return;
                int chosen = m_AttractionChoice;
                CancelTargeting(false);
                m_AttractionSubmitted = true;
                Submit(GameCommand.ResolveAttraction(id, chosen));
                return;
            }

            var actor = s.Find(m_TargetActor);
            if (actor == null)
            {
                CancelTargeting(true);
                return;
            }

            if (m_Picks.Remove(id))
            {
                UpdateTargetingBanner();
                RenderAll();
                return;
            }
            if (!PartyEngine.IsValidTarget(s, m_TargetActor, id, m_Picks)) return;

            m_Picks.Add(id);
            Fx.Play(FxCue.CardHover, card.AnimRoot, 2f);
            GuestInfo.TargetCount(actor.Info.Ability, out _, out int max);
            if (m_Picks.Count >= max)
            {
                ConfirmTargets();
                return;
            }
            UpdateTargetingBanner();
            RenderAll();
        }

        void ConfirmTargets()
        {
            var s = State;
            if (s == null || m_TargetActor < 0 || !CanAct) return;
            var actor = s.Find(m_TargetActor);
            if (actor == null) return;
            GuestInfo.TargetCount(actor.Info.Ability, out int min, out _);
            if (m_Picks.Count < min) return;

            var command = GameCommand.UseAbility(m_TargetActor,
                m_Picks.Count > 0 ? m_Picks[0] : -1,
                m_Picks.Count > 1 ? m_Picks[1] : -1,
                m_Picks.Count > 2 ? m_Picks[2] : -1,
                m_Option);
            CancelTargeting(false);
            Submit(command);
        }

        void CancelTargeting(bool render)
        {
            if (!Targeting) return;
            m_TargetActor = -1;
            m_Option = 0;
            m_Picks.Clear();
            m_AttractionChoice = -1;
            targetingBanner.Hide();
            if (render) RenderAll();
        }

        // ------------------------------------------------------------------ Distracted Boyfriend

        void PromptAttraction(PlayerState s)
        {
            if (!s.AttractionPending || m_AttractionSubmitted || Targeting || picker.IsOpen || ModalDialog.IsOpen || m_GameOverShown) return;
            var boyfriend = s.Find(s.AttractionActor);
            string title = Loc.Get("pick.attraction_title", "viewer", boyfriend != null ? GuestVisuals.DisplayName(boyfriend.Kind) : GuestVisuals.DisplayName(GuestType.DistractedBoyfriend));
            m_PickingActor = s.AttractionActor;
            picker.Open(title, Loc.Get("pick.attraction"), s, s.AttractionCandidates,
                pick =>
                {
                    m_PickingActor = -1;
                    m_AttractionChoice = pick;
                    UpdateTargetingBanner();
                    RenderAll();
                },
                () =>
                {
                    m_PickingActor = -1;
                    m_AttractionSubmitted = true;
                    Submit(GameCommand.ResolveAttraction(-1, -1));
                },
                Loc.Get("pick.skip"), true);
        }

        // ------------------------------------------------------------------ misc

        void ToggleRolodex()
        {
            var s = View;
            if (s != null) rolodex.Toggle(s, ViewOwnerName);
        }

        void OpenPause()
        {
            if (gameOver.IsOpen) return;
            pauseMenu.Open(m_VenueChoice, m_Net != null);
        }

        void ChangeVenue(string choice)
        {
            m_VenueChoice = choice;
            AppSettings.VenueId = choice;
            if (m_Session is LocalGameSession local) local.VenueChoice = choice;
            ApplyVenue(true);
            if (View != null) UpdateMusic(View);
        }

        void QuitGame()
        {
            if (m_Net != null)
            {
                bool host = m_Net.IsServer;
                ModalDialog.Show(Loc.Get("game.leave_title"), Loc.Get(host ? "game.leave_host" : "game.leave_client"), LeaveGame, Loc.Get("over.leave"), Loc.Get("lobby.stay"));
                return;
            }
            pauseMenu.Close();
            SceneFlow.GoToMainMenu();
        }

        void LeaveGame()
        {
            if (NetworkService.HasInstance) NetworkService.Instance.Leave();
            SceneFlow.GoToMainMenu(null, openMultiplayer: true);
        }

        // ================================================================== input

        void Update()
        {
            ClearButtonSelection();
            UpdatePresence();
            UpdateTurnTimer();
            UpdateShopGate();
            UpdateRecap();
            if (State == null || InputHelper.IsTyping) return;

            if (m_Scoring)
            {
                if (InputHelper.Pressed(HotKey.Space) || InputHelper.Pressed(HotKey.Enter) || InputHelper.Pressed(HotKey.Escape) || PointerClicked()) SkipScoring();
                return;
            }

            // The stream / shop summary: Space, Enter or Esc closes it (nothing else reacts meanwhile).
            if (RecapOpen)
            {
                if (InputHelper.Pressed(HotKey.Space) || InputHelper.Pressed(HotKey.Enter) || InputHelper.Pressed(HotKey.Escape)) recap.Close();
                return;
            }

            if (InputHelper.Pressed(HotKey.Escape))
            {
                HandleEscape();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Testing aid: F9 grants Gifted Subs and Cash in single player (never in release builds or multiplayer).
            if (InputHelper.Pressed(HotKey.F9) && m_Session is LocalGameSession debugSession)
            {
                debugSession.DebugGrant(50, 10);
                ToastView.Show($"Debug: +50 {GameIcons.Tag(ResourceIcon.Subs)}, +10 {GameIcons.Tag(ResourceIcon.Cash)}", ToastKind.Info);
                return;
            }
#endif

            if (ModalDialog.IsOpen || pauseMenu.IsOpen || gameOver.IsOpen || picker.IsOpen || (roundSummary != null && roundSummary.IsOpen)) return;

            if (InputHelper.Pressed(HotKey.R))
            {
                ToggleRolodex();
                return;
            }
            if (!CanAct) return;
            var s = State;

            if (InputHelper.Pressed(HotKey.Space) || InputHelper.Pressed(HotKey.Enter))
            {
                if (s.Phase == GamePhase.Party && Targeting)
                {
                    ConfirmTargets();
                }
                else if (s.Phase == GamePhase.Party)
                {
                    if (s.PeekedId >= 0)
                    {
                        if (!s.HouseFull) Submit(GameCommand.AdmitPeeked());
                    }
                    else if (PartyEngine.GetDoorBlocker(s) == null)
                    {
                        Submit(GameCommand.OpenDoor());
                    }
                }
                else if (s.Phase == GamePhase.Planning)
                {
                    NextNight();
                }
            }
            else if (InputHelper.Pressed(HotKey.E))
            {
                if (s.Phase == GamePhase.Party && !Targeting && PartyEngine.GetEndPartyBlocker(s) == null) EndParty();
            }
        }

        static bool PointerClicked()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        void HandleEscape()
        {
            if (ModalDialog.IsOpen) ModalDialog.CloseActive();
            else if (picker.IsOpen) picker.Cancel();
            else if (rolodex.IsOpen) rolodex.Close();
            else if (roundSummary != null && roundSummary.IsOpen) roundSummary.Close();
            else if (Targeting) CancelTargeting(true);
            else if (pauseMenu.IsOpen) pauseMenu.Close();
            else if (!gameOver.IsOpen) OpenPause();
        }

        /// <summary>Clicked buttons must not keep keyboard focus, or Space/Enter would "click" them again.</summary>
        static void ClearButtonSelection()
        {
            var system = EventSystem.current;
            if (system == null) return;
            var selected = system.currentSelectedGameObject;
            if (selected != null && selected.GetComponent<TMP_InputField>() == null) system.SetSelectedGameObject(null);
        }
    }
}
