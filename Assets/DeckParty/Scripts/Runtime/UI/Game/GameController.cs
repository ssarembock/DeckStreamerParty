using System.Collections;
using System.Collections.Generic;
using System.Text;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Runs the Game scene. Picks a local or networked session, renders the state it publishes, turns
    /// engine events into feedback, and converts clicks / hotkeys into GameCommands.
    /// In multiplayer any player's board can be on screen (it's an open-information game): your own board is
    /// interactive when it's your move, everyone else's is shown live and read-only.
    /// </summary>
    public sealed class GameController : MonoBehaviour
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
        [SerializeField] internal ChatView chat;

        [Header("Overlays")]
        [SerializeField] internal GuestPickerView picker;
        [SerializeField] internal RolodexView rolodex;
        [SerializeField] internal GameOverView gameOver;
        [SerializeField] internal PauseMenuView pauseMenu;
        [SerializeField] internal BannerView banner;
        [SerializeField] internal EffectsView effects;
        [SerializeField] internal VenueBackgroundView background;

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

        // Whose board is on screen (multiplayer), plus every player's log lines and last scoring breakdown.
        ulong m_ViewId;
        readonly Dictionary<ulong, List<string>> m_History = new Dictionary<ulong, List<string>>();
        readonly Dictionary<ulong, string> m_Breakdowns = new Dictionary<ulong, string>();

        // Action targeting: the acting guest, the option chosen in the choice dialog and the guests picked so far.
        int m_TargetActor = -1;
        int m_Option;
        readonly List<int> m_Picks = new List<int>();

        // Distracted Boyfriend: the revealed guest chosen, while the player picks who they replace.
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

            if (State != null)
            {
                ApplyVenue(true);
                RenderAll();
                IntroBanner();
            }
            else
            {
                banner.Show("CONNECTING", "Waiting for the host...", UIPalette.Teal, 2f);
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
                log.Add($"Welcome back! Continuing <b>{ScenarioCatalog.Get(state.ScenarioId).Name}</b> on night {state.Night}.");
            }

            if (state == null)
            {
                int scenarioId = launch != null ? launch.ScenarioId : AppSettings.LastScenario;
                uint seed = launch != null && launch.Seed != 0 ? launch.Seed : NewSeed();
                if (launch != null && !string.IsNullOrEmpty(launch.VenueChoice)) venue = launch.VenueChoice;
                state = PartyEngine.CreateGame(scenarioId, seed);
                var scenario = ScenarioCatalog.Get(scenarioId);
                log.Add($"Welcome to <b>{scenario.Name}</b>! Get {GameRules.DiamondsToWin} {Terms.Diamonds} to one party within {state.TotalNights} nights.");
            }

            m_VenueChoice = venue;
            m_Session = new LocalGameSession(state, venue);
            opponents.gameObject.SetActive(false);
            chat.gameObject.SetActive(false);
        }

        void StartNetwork(DeckPartyNetSession net)
        {
            m_Net = net;
            m_Session = new NetworkGameSession(net);
            m_VenueChoice = AppSettings.VenueId;
            m_ViewId = net.LocalClientId;
            if (net.TakingTurns && net.ActivePlayer != DeckPartyNetSession.NoPlayer) m_ViewId = net.ActivePlayer;

            opponents.gameObject.SetActive(true);
            chat.gameObject.SetActive(true);
            chat.Bind(net);
            opponents.PlayerClicked += ShowBoard;

            var panel = targetingBanner != null && targetingBanner.root != null ? targetingBanner.root.GetComponent<Image>() : null;
            m_ViewBar = ViewBarView.Create(partyRoot.transform.parent as RectTransform, hud.menuButton, hud.nightLabel, panel);

            net.PlayersChanged += OnPlayersChanged;
            net.PhaseChanged += OnMatchPhaseChanged;
            net.HeadlineReceived += OnHeadline;
            net.PlayerStateReceived += OnPlayerState;
            net.ActivePlayerChanged += OnActivePlayerChanged;
            net.RequestState();

            var settings = net.Settings;
            m_IntroLine = $"Multiplayer season: <b>{ScenarioCatalog.Get(settings.ScenarioId).Name}</b>, {settings.Nights} nights, {MatchModes.ShortName(settings.Mode)}.";
            log.Add(m_IntroLine);
            opponents.Render(net, m_ViewId);
            UpdateViewBar();
        }

        void OnDestroy()
        {
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
            }
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

            raidBlame.GuestChosen += id => Submit(GameCommand.Blame(id));
            targetingBanner.CancelClicked += () => CancelTargeting(true);
            targetingBanner.ConfirmClicked += ConfirmTargets;

            pauseMenu.QuitClicked += QuitGame;
            pauseMenu.VenueChosen += ChangeVenue;
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
            banner.Show($"NIGHT {s.Night}", $"{ScenarioCatalog.Get(s.ScenarioId).Name}  -  {s.NightsRemaining} nights left after tonight", UIPalette.Purple, 1.2f);
        }

        // ================================================================== state updates

        void OnStateChanged(PlayerState s, IReadOnlyList<GameEvent> events) => OnPlayerState(LocalId, s, events);

        void OnPlayerState(ulong owner, PlayerState s, IReadOnlyList<GameEvent> events)
        {
            bool own = owner == LocalId;
            var lines = Describe(s, events, own ? null : NameOf(owner));
            Remember(owner, lines);
            UpdateBreakdown(owner, events);
            bool switched = false;

            if (own)
            {
                m_AttractionSubmitted = false;
                if (m_TargetActor >= 0 && (s.Phase != GamePhase.Party || PartyEngine.GetAbilityBlocker(s, m_TargetActor) != null)) CancelTargeting(false);
                if (m_AttractionChoice >= 0 && (!s.AttractionPending || !s.AttractionCandidates.Contains(m_AttractionChoice))) CancelTargeting(false);
                if (picker.IsOpen && s.Phase != GamePhase.Party) picker.Close();
                ApplyVenue(false);

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
            ProcessEvents(s, events, own ? null : NameOf(owner));
            RenderAll();
            PlayCardAnimations();
        }

        void OnCommandRejected(string reason)
        {
            m_AttractionSubmitted = false;
            ToastView.Show(reason, ToastKind.Warning);
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

        void UpdateBreakdown(ulong owner, IReadOnlyList<GameEvent> events)
        {
            if (events == null) return;
            Dictionary<GuestType, (int count, int pop, int cash)> breakdown = null;
            bool partyEnded = false;

            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case GameEventType.NightStarted:
                        m_Breakdowns.Remove(owner);
                        break;
                    case GameEventType.GuestScored when e.C != 0:
                        if (breakdown == null) breakdown = new Dictionary<GuestType, (int count, int pop, int cash)>();
                        breakdown.TryGetValue(e.Guest, out var entry);
                        breakdown[e.Guest] = (entry.count + 1, entry.pop + e.A, entry.cash + e.B);
                        break;
                    case GameEventType.PartyEnded:
                        partyEnded = true;
                        break;
                }
            }

            if (partyEnded) m_Breakdowns[owner] = FormatBreakdown(breakdown ?? new Dictionary<GuestType, (int count, int pop, int cash)>());
        }

        string BreakdownFor(ulong owner) => m_Breakdowns.TryGetValue(owner, out var text) ? text : null;

        /// <summary>Banners, sounds and effects for events on the board being shown. <paramref name="who"/> is null for your own party.</summary>
        void ProcessEvents(PlayerState s, IReadOnlyList<GameEvent> events, string who)
        {
            m_Arrivals.Clear();
            m_Pulses.Clear();
            if (events == null) return;

            Sfx sound = Sfx.Click;
            int soundPriority = -1;
            void Consider(Sfx candidate, int priority)
            {
                if (priority > soundPriority)
                {
                    sound = candidate;
                    soundPriority = priority;
                }
            }

            foreach (var e in events)
            {
                switch (e.Type)
                {
                    case GameEventType.NightStarted:
                    {
                        string left = s.NightsRemaining > 0 ? $"{s.NightsRemaining} nights left after tonight" : "The final night!";
                        banner.Show($"NIGHT {e.A}", who == null ? left : $"{who}'s party  -  {left}", UIPalette.Purple, 1.0f);
                        Consider(Sfx.Notify, 1);
                        break;
                    }
                    case GameEventType.GuestArrived:
                        m_Arrivals.Add(e.GuestId);
                        if (GuestCatalog.Get(e.Guest).IsDiamond) Consider(Sfx.StarArrive, 5);
                        else Consider(e.A < 0 ? Sfx.DoorKnock : Sfx.GuestArrive, 2);
                        break;
                    case GameEventType.GuestRemoved:
                    case GameEventType.GuestRejected:
                        Consider(Sfx.Boot, 3);
                        break;
                    case GameEventType.GuestPeeked:
                    case GameEventType.AttractionRevealed:
                        Consider(Sfx.Peek, 3);
                        break;
                    case GameEventType.GuestScored:
                        if (e.C == 0)
                        {
                            m_Pulses.Add(e.GuestId);
                            Consider(Sfx.Coin, 3);
                        }
                        break;
                    case GameEventType.GuestGained:
                    case GameEventType.PartyPotGained:
                        m_Pulses.Add(e.GuestId);
                        Consider(Sfx.Coin, 1);
                        break;
                    case GameEventType.AbilityUsed:
                        m_Pulses.Add(e.GuestId);
                        Consider(Sfx.Ability, 2);
                        break;
                    case GameEventType.EveryoneOutside:
                    case GameEventType.Transformed:
                    case GameEventType.Struck:
                    case GameEventType.Infected:
                    case GameEventType.PositionsSwapped:
                    case GameEventType.PrintedExchanged:
                    case GameEventType.ShieldBlocked:
                    case GameEventType.PermanentChange:
                        m_Pulses.Add(e.GuestId);
                        Consider(Sfx.Ability, 3);
                        break;
                    case GameEventType.ThisIsFine:
                        banner.Show("THIS IS FINE.", $"The SWAT team was called off. {Terms.Risk} cleared.", UIPalette.Orange, 1.6f);
                        effects.Flash(UIPalette.Orange);
                        Consider(Sfx.Ability, 8);
                        break;
                    case GameEventType.PoliceRaid:
                        banner.Show(Terms.Raid, who == null ? "The SWAT team is here. No payout tonight" : $"The SWAT team is at {who}'s party. No payout tonight", UIPalette.Red, 1.6f);
                        effects.Shake(24f, 0.5f);
                        effects.Flash(UIPalette.Red);
                        background.PoliceLights();
                        Consider(Sfx.Raid, 9);
                        break;
                    case GameEventType.FireMarshal:
                        banner.Show(Terms.Overload, $"{e.A} spaces filled with a {Terms.SubCap} of {e.B}", UIPalette.Orange, 1.6f);
                        effects.Shake(18f, 0.45f);
                        effects.Flash(UIPalette.Orange);
                        Consider(Sfx.FireMarshal, 9);
                        break;
                    case GameEventType.PartyEnded:
                        effects.FloatText($"{GuestVisuals.PopText(e.A)}   {GuestVisuals.CashText(e.B)}", new Vector2(0f, 60f));
                        Consider(Sfx.PartySuccess, 7);
                        break;
                    case GameEventType.CashShortfall:
                        if (who == null) ToastView.Show($"You couldn't pay {e.A} $ in fees and lost {e.B} {Terms.GiftedSubs}.", ToastKind.Error);
                        else ToastView.Show($"{who} couldn't pay {e.A} $ in fees and lost {e.B} {Terms.GiftedSubs}.", ToastKind.Info);
                        break;
                    case GameEventType.GuestPurchased:
                        Consider(Sfx.Purchase, 4);
                        break;
                    case GameEventType.HouseExpanded:
                        Consider(Sfx.Expand, 4);
                        break;
                    case GameEventType.Victory:
                        banner.Show(who == null ? "VICTORY!" : $"{who.ToUpperInvariant()} WINS!", $"{GameRules.DiamondsToWin} {Terms.Diamonds} at one party!", UIPalette.Gold, 2.2f);
                        effects.Confetti(140);
                        effects.Flash(UIPalette.Gold);
                        Consider(Sfx.Victory, 10);
                        break;
                    case GameEventType.Defeat:
                        banner.Show("SEASON OVER", who == null ? $"No {GameRules.DiamondsToWin}-{Terms.Diamond} party this time" : $"{who}'s season is over", UIPalette.TextDim, 2f);
                        Consider(Sfx.Defeat, 10);
                        break;
                }
            }

            if (soundPriority >= 0) AudioService.Play(sound);
        }

        static string FormatBreakdown(Dictionary<GuestType, (int count, int pop, int cash)> breakdown)
        {
            if (breakdown.Count == 0) return "Nobody was inside when the party ended.";
            var entries = new List<KeyValuePair<GuestType, (int count, int pop, int cash)>>(breakdown);
            entries.Sort((a, b) => (b.Value.pop + b.Value.cash).CompareTo(a.Value.pop + a.Value.cash));

            var sb = new StringBuilder("<b>Who scored:</b> ");
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) sb.Append(",  ");
                var (count, pop, cash) = entries[i].Value;
                sb.Append(GuestVisuals.NameTag(entries[i].Key));
                if (count > 1) sb.Append(" x").Append(count);
                sb.Append(' ');
                if (pop != 0 || cash == 0) sb.Append(GuestVisuals.PopText(pop));
                if (cash != 0) sb.Append(pop != 0 ? " " : string.Empty).Append(GuestVisuals.CashText(cash));
            }
            return sb.ToString();
        }

        void PlayCardAnimations()
        {
            float delay = 0f;
            foreach (var id in m_Arrivals)
            {
                var card = house.FindCard(id);
                if (card == null) continue;
                card.PlayArrive(delay);
                delay += 0.12f;
            }

            foreach (var id in m_Pulses)
            {
                if (id < 0 || m_Arrivals.Contains(id)) continue;
                var card = house.FindCard(id);
                if (card != null) card.PlayPulse();
            }
        }

        // ================================================================== watching other players

        string NameOf(ulong clientId) => m_Net != null ? m_Net.PlayerName(clientId) : "You";

        Color ColorOf(ulong clientId) =>
            m_Net != null && m_Net.TryGetPlayer(clientId, out var p) ? UIPalette.PlayerColor(p.ColorIndex) : UIPalette.Teal;

        string ColoredName(ulong clientId) => GuestVisuals.Colored(NameOf(clientId), ColorOf(clientId));

        /// <summary>Puts a player's board on screen (multiplayer). Your own board is interactive when it's your move.</summary>
        void ShowBoard(ulong clientId)
        {
            if (m_Net == null) return;
            if (clientId != LocalId && m_Net.GetState(clientId) == null)
            {
                ToastView.Show($"{NameOf(clientId)}'s party is still loading.", ToastKind.Info);
                return;
            }

            if (clientId != m_ViewId)
            {
                CancelTargeting(false);
                if (picker.IsOpen) picker.Close();
                m_ViewId = clientId;
                m_Arrivals.Clear();
                m_Pulses.Clear();
                RefillLog();
            }
            RenderAll();
        }

        void RefillLog()
        {
            log.Clear();
            if (!string.IsNullOrEmpty(m_IntroLine)) log.Add(m_IntroLine);
            if (m_History.TryGetValue(m_ViewId, out var history))
                foreach (var line in history) log.Add(line);
        }

        void OnActivePlayerChanged(ulong active)
        {
            if (!TakingTurns) return;
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
            string night = s != null ? $"Night {s.Night} of {s.TotalNights}" : string.Empty;
            if (active == LocalId)
            {
                banner.Show("YOUR TURN", night + "  -  everyone is watching!", UIPalette.Gold, 1.4f);
                AudioService.Play(Sfx.Notify);
            }
            else
            {
                banner.Show($"{NameOf(active).ToUpperInvariant()}'S TURN", night + "  -  watching live", ColorOf(active), 1.2f);
            }
        }

        void UpdateViewBar()
        {
            if (m_ViewBar == null || m_Net == null) return;
            ulong active = m_Net.ActivePlayer;

            if (TakingTurns && m_Net.Phase == MatchPhase.InGame && active != DeckPartyNetSession.NoPlayer)
            {
                if (active == LocalId)
                {
                    if (ViewingOwn) m_ViewBar.Set("<b>Your turn!</b>  Everyone is watching your party.", UIPalette.Gold, null, null);
                    else m_ViewBar.Set($"<b>Your turn!</b>  You're looking at {ColoredName(m_ViewId)}'s party.", UIPalette.Gold, "Back to My Turn", () => ShowBoard(LocalId));
                }
                else if (m_ViewId == active)
                {
                    m_ViewBar.Set($"<b>{ColoredName(active)}'s turn</b>  Watching live. You can't act until your turn.", ColorOf(active), "Look at My Party", () => ShowBoard(LocalId));
                }
                else
                {
                    string whose = ViewingOwn ? "Your party" : $"{ColoredName(m_ViewId)}'s party";
                    m_ViewBar.Set($"{whose}  -  it's {ColoredName(active)}'s turn.", ColorOf(m_ViewId), $"Watch {NameOf(active)}", () => ShowBoard(active));
                }
                return;
            }

            if (ViewingOwn) m_ViewBar.Set("<b>Your party</b>  <color=#B9AED6>Click a player on the right to watch their party live.</color>", UIPalette.Teal, null, null);
            else m_ViewBar.Set($"<b>Watching {ColoredName(m_ViewId)}'s party</b>  live and read-only.", ColorOf(m_ViewId), "Back to My Party", () => ShowBoard(LocalId));
        }

        // ================================================================== rendering

        void RenderAll()
        {
            var s = View;
            if (s == null) return;
            bool act = CanAct;

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
            }
            if (planningPhase) planning.Render(s, PlanningCtx(), BreakdownFor(m_ViewId));
            if (blame) raidBlame.Render(s, act ? null : $"{(ViewingOwn ? "You" : NameOf(m_ViewId))} must choose who gets a {Terms.Ban}.");

            background.SetEnergy(party ? s.Occupancy / (float)Mathf.Max(1, s.Capacity) : 0.2f);
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
            string watchHint = act ? null : ViewingOwn ? $"Wait for your turn. It's {NameOf(m_Net.ActivePlayer)}'s turn." : $"This is {NameOf(m_ViewId)}'s party. You're watching.";
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
                }
                else
                {
                    card.SetHighlight(CardHighlight.None);
                    card.SetClickable(false);
                    if (g.Info.HasAbility && s.Phase == GamePhase.Party)
                    {
                        bool used = s.UsedAbilities.Contains(g.Id);
                        var blocker = act ? PartyEngine.GetAbilityBlocker(s, g.Id) : watchHint;
                        card.ShowAbility(used ? "Used" : g.Info.AbilityName, blocker == null, blocker);
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
            if (TakingTurns && m_Net.ActivePlayer != DeckPartyNetSession.NoPlayer)
            {
                ctx.ActiveName = NameOf(m_Net.ActivePlayer);
                ctx.NextName = m_Net.NextTurnName();
            }
            if (m_Net.TryGetLocalPlayer(out var me)) ctx.LocalReady = me.ReadyNextNight;

            var waiting = new List<string>();
            foreach (var p in m_Net.PlayersSnapshot())
            {
                if (p.ClientId == m_Net.LocalClientId || !p.Connected || p.Finished) continue;
                if (!(p.GamePhase == GamePhase.Planning && p.ReadyNextNight)) waiting.Add(p.DisplayName);
            }
            ctx.WaitingFor = string.Join(", ", waiting);
            return ctx;
        }

        void ApplyVenue(bool force)
        {
            var venue = GameDatabase.Instance.ResolveVenue(m_VenueChoice, State);
            if (venue != null) background.Apply(venue, force);
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
            m_GameOverShown = true;
            CancelTargeting(false);
            picker.Close();
            rolodex.Close();
            pauseMenu.Close();

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
                gameOver.ShowMultiplayer(m_Net, State, true, () => m_Net.ServerReturnToLobby(), "Back to Lobby", LeaveGame, "Close Party", null);
            else if (matchOver)
                gameOver.ShowMultiplayer(m_Net, State, true, null, null, LeaveGame, "Leave", "Waiting for the host to start another round...");
            else
                gameOver.ShowMultiplayer(m_Net, State, false, KeepWatching, "Keep Watching", LeaveGame, "Leave", "The scoreboard updates live.");
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
            });
        }

        // ================================================================== multiplayer callbacks

        void OnPlayersChanged()
        {
            if (m_Net == null) return;
            opponents.Render(m_Net, m_ViewId);
            UpdateViewBar();
            var s = View;
            if (s != null && s.Phase == GamePhase.Planning) planning.Render(s, PlanningCtx(), BreakdownFor(m_ViewId));
            if (m_GameOverShown) ShowMultiplayerResults();
        }

        void OnMatchPhaseChanged(MatchPhase phase)
        {
            if (State != null) CheckGameOver(State);
            UpdateViewBar();
        }

        void OnHeadline(string text) => ToastView.Show(text, ToastKind.Info);

        // ================================================================== player actions

        void Submit(GameCommand command)
        {
            if (State == null || !CanAct) return;
            m_Session.Submit(command);
        }

        void EndParty()
        {
            var s = State;
            if (s == null || s.Phase != GamePhase.Party || !CanAct) return;
            if (s.House.Count == 0)
            {
                ModalDialog.Show("End an empty party?", "Nobody is inside, so you'll earn nothing tonight.", () => Submit(GameCommand.EndParty()), "End It", "Keep Partying");
                return;
            }
            Submit(GameCommand.EndParty());
        }

        void NextNight()
        {
            var s = State;
            if (s == null || s.Phase != GamePhase.Planning || !CanAct) return;

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
            var options = info.Options;
            if (options == null)
            {
                ContinueAbility(actorId, 0);
                return;
            }

            ModalDialog.ShowChoice($"{GuestVisuals.DisplayName(guest.Kind)}: {info.AbilityName}", OptionHelp(s, info.Ability), options,
                option => ContinueAbility(actorId, option), null, OptionsEnabled(s, info.Ability, options.Length));
        }

        static string OptionHelp(PlayerState s, AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.Mood:
                    return $"<b>Happy:</b> +3 {Terms.Subs}.   <b>Sad:</b> +3 $.   <b>Angry:</b> +6 {Terms.Subs} and +1 {Terms.Risk}.\nLasts until the party ends.";
                case AbilityKind.Invest:
                    return $"Stake {GameRules.StonksInvestPop} {Terms.GiftedSubs} or {GameRules.StonksInvestCash} $ from your bank. " +
                           $"If the party succeeds you get {GameRules.StonksMultiplier}x back; if it's swatted or shut down, it's gone.\n" +
                           $"You have {s.Pop} {Terms.GiftedSubs} and {s.Cash} $.";
                case AbilityKind.Reroll:
                {
                    var last = PartyEngine.CanReroll(s) ? s.Find(s.LastRandomArrival) : null;
                    var reroll = last != null
                        ? $"<b>Reroll:</b> {GuestVisuals.NameTag(last.Kind)} goes back outside and a new random guest takes their place."
                        : "<b>Reroll:</b> nothing to reroll right now.";
                    return reroll + "\n<b>Peek:</b> look at the next guest and decide whether to let them in.";
                }
                case AbilityKind.Drake:
                    return $"You'll pick two guests: the first earns nothing tonight, the second doubles their printed {Terms.Subs} or $.\nWhat should the second guest double?";
                case AbilityKind.Convert:
                    return $"You'll pick one guest for tonight.\n<b>{Terms.Subs} to $:</b> every 2 {Terms.Subs} become 1 $.   <b>$ to {Terms.Subs}:</b> every 1 $ becomes 2 {Terms.Subs}.";
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
            string title = $"{GuestVisuals.DisplayName(guest.Kind)}: {info.AbilityName}";

            switch (info.Target)
            {
                case TargetMode.None:
                    Submit(GameCommand.UseAbility(actorId, option: option));
                    break;

                case TargetMode.WaitingGuest:
                    picker.Open(title, "Choose a waiting guest to bring in.", s, s.Pool,
                        pick => Submit(GameCommand.UseAbility(actorId, pick, option: option)));
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

        /// <summary>Grumpy Cat: reject one of the next guests, then choose who comes next.</summary>
        void BeginInspect(int actorId, string title)
        {
            var s = State;
            var looked = s.Pool.GetRange(0, Mathf.Min(GameRules.GrumpyInspectCount, s.Pool.Count));
            picker.Open(title, "These are the next guests in line. Pick one to reject for the night.", s, looked, rejected =>
            {
                var rest = new List<int>(looked);
                rest.Remove(rejected);
                if (rest.Count <= 1)
                {
                    Submit(GameCommand.UseAbility(actorId, rejected, rest.Count == 1 ? rest[0] : -1));
                    return;
                }
                picker.Open(title, "Who should come in next?", State, rest,
                    first => Submit(GameCommand.UseAbility(actorId, rejected, first)), keepOrder: true);
            }, keepOrder: true);
        }

        void UpdateTargetingBanner()
        {
            var s = State;
            if (m_AttractionChoice >= 0)
            {
                var chosen = s?.Find(m_AttractionChoice);
                string who = chosen != null ? GuestVisuals.NameTag(chosen.Kind) : "them";
                targetingBanner.Show($"New Attraction: choose who {who} replaces. They'll sit out tonight.");
                targetingBanner.SetConfirm(false);
                return;
            }

            var actor = s?.Find(m_TargetActor);
            if (actor == null) return;
            GuestInfo.TargetCount(actor.Info.Ability, out int min, out int max);
            targetingBanner.Show(TargetPrompt(s, actor));
            targetingBanner.SetConfirm(max > min, m_Picks.Count >= min, $"Confirm ({m_Picks.Count}/{max})");
        }

        string TargetPrompt(PlayerState s, GuestInstance actor)
        {
            var info = actor.Info;
            string who = GuestVisuals.NameTag(actor.Kind);
            string first = m_Picks.Count > 0 && s.Find(m_Picks[0]) != null ? GuestVisuals.NameTag(s.Find(m_Picks[0]).Kind) : string.Empty;
            switch (info.Ability)
            {
                case AbilityKind.Boot: return $"{who}: choose a guest to remove from chat (it can be {GuestVisuals.DisplayName(actor.Kind)} too).";
                case AbilityKind.Troll: return $"{who}: choose a guest to send back outside.";
                case AbilityKind.PlayOff: return $"{who}: choose a guest to play off.";
                case AbilityKind.Refund: return $"{who}: choose a guest to remove. You get half their cost as {Terms.Subs}.";
                case AbilityKind.Mog: return $"{who}: choose a guest to compare printed {Terms.Subs} with.";
                case AbilityKind.Arson: return $"{who}: choose a guest to get +1 {Terms.Risk} and +3 {Terms.Subs}.";
                case AbilityKind.Strike: return $"{who}: choose a guest to strike.";
                case AbilityKind.Rickroll: return $"{who}: choose a non-{Terms.Diamond} guest to Rickroll.";
                case AbilityKind.Convert: return $"{who}: choose a guest to convert ({info.Options[m_Option]}).";
                case AbilityKind.Immunity: return $"{who}: choose a guest to make immune tonight.";
                case AbilityKind.SwapOrder:
                    return m_Picks.Count == 0 ? $"{who}: choose the first guest to move." : $"{who}: choose who {first} swaps places with.";
                case AbilityKind.Pirouette:
                    return m_Picks.Count == 0 ? $"{who}: choose the first dancer." : $"{who}: choose who {first} swaps printed {Terms.Subs} with.";
                case AbilityKind.Drake:
                    return m_Picks.Count == 0
                        ? $"{who}: choose the guest who earns nothing tonight."
                        : $"{who}: choose who doubles their printed {(m_Option == 0 ? Terms.Subs : "$")}.";
                case AbilityKind.Bombard:
                    return $"{who}: choose up to 3 guests to replace with random waiting guests, then Confirm.";
                default: return $"{who}: choose a target.";
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
            string title = $"{(boyfriend != null ? GuestVisuals.DisplayName(boyfriend.Kind) : "Distracted Boyfriend")}: New Attraction";
            picker.Open(title, "Pick someone to swap in, then choose who they replace. Skip to keep everyone.", s, s.AttractionCandidates,
                pick =>
                {
                    m_AttractionChoice = pick;
                    UpdateTargetingBanner();
                    RenderAll();
                },
                () =>
                {
                    m_AttractionSubmitted = true;
                    Submit(GameCommand.ResolveAttraction(-1, -1));
                },
                "Skip  [Esc]", true);
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
                ModalDialog.Show("Leave the game?",
                    host ? "You're the host: leaving ends the season for everyone." : "You won't be able to rejoin this season.",
                    LeaveGame, "Leave", "Stay");
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
            if (State == null || InputHelper.IsTyping) return;

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
                ToastView.Show($"Debug: +50 {Terms.GiftedSubs}, +10 $", ToastKind.Info);
                return;
            }
#endif

            if (ModalDialog.IsOpen || pauseMenu.IsOpen || gameOver.IsOpen || picker.IsOpen) return;

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

        void HandleEscape()
        {
            if (ModalDialog.IsOpen) ModalDialog.CloseActive();
            else if (picker.IsOpen) picker.Cancel();
            else if (rolodex.IsOpen) rolodex.Close();
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
