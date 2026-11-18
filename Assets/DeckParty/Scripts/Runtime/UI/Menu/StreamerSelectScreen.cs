using System;
using System.Collections;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    public enum StreamerSelectMode
    {
        SinglePlayer,
        Multiplayer,
        /// <summary>Changing your streamer from the lobby.</summary>
        Lobby,
        /// <summary>Versus the AI: your own streamer (rival powers work, there's a rival).</summary>
        VersusAi,
        /// <summary>Versus the AI: the streamer the AI plays as.</summary>
        Opponent,
        /// <summary>
        /// Multiplayer, once everyone is ready and the host starts: every player picks at once against a real countdown, then
        /// waits for the others. There's no going back (unless someone leaves and the pick is called off).
        /// </summary>
        MatchPick,
    }

    /// <summary>
    /// "SELECT YOUR STREAMER": a fighting-game style character select. The roster grid sits at the bottom, your pick
    /// fills the left side (name, power, a random quote from the Game Database) and your opponent the right (THE
    /// ALGORITHM in single player, a mystery challenger online). Arrow keys or the mouse to choose, Enter to go live,
    /// Esc to go back.
    /// </summary>
    public sealed class StreamerSelectScreen : UIScreen
    {
        const int Columns = 7;

        [Header("Header")]
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text subtitleLabel;
        [SerializeField] internal TMP_Text timerLabel;
        [SerializeField] internal TMP_Text hintLabel;

        [Header("Roster")]
        [SerializeField] internal RectTransform grid;
        [SerializeField] internal StreamerTileView tilePrefab;

        [Header("Player 1")]
        [SerializeField] internal RectTransform p1Root;
        [SerializeField] internal Image p1Portrait;
        [SerializeField] internal Image p1Placeholder;
        [SerializeField] internal TMP_Text p1Initials;
        [SerializeField] internal TMP_Text p1Name;
        [SerializeField] internal TMP_Text p1Title;
        [SerializeField] internal TMP_Text p1Kind;
        [SerializeField] internal TMP_Text p1Power;
        [SerializeField] internal TMP_Text p1Description;
        [SerializeField] internal TMP_Text p1Cost;
        [SerializeField] internal TMP_Text p1Quote;
        [SerializeField] internal TMP_Text p1Warning;

        [Header("Opponent")]
        [SerializeField] internal RectTransform p2Root;
        [SerializeField] internal Image p2Placeholder;
        [SerializeField] internal TMP_Text p2Initials;
        [SerializeField] internal TMP_Text p2Name;
        [SerializeField] internal TMP_Text p2Sub;
        [SerializeField] internal TMP_Text vsLabel;

        [Header("Buttons and effects")]
        [SerializeField] internal Button confirmButton;
        [SerializeField] internal TMP_Text confirmLabel;
        [SerializeField] internal Button backButton;
        [SerializeField] internal TMP_Text announcerLabel;
        [SerializeField] internal Image flash;

        readonly List<StreamerTileView> m_Tiles = new List<StreamerTileView>();
        MainMenuController m_Menu;
        StreamerSelectMode m_Mode;
        Action<byte> m_OnConfirmed;
        Action m_OnBack;
        int m_Index;
        bool m_Locked;
        float m_Timer = 99f;

        // The quote is re-rolled each time a streamer is highlighted (avoiding the one they said last) and kept
        // through language changes.
        int m_QuotePick = -1;
        string m_Quote;
        readonly Dictionary<byte, string> m_LastQuotes = new Dictionary<byte, string>();

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
            if (backButton != null) backButton.onClick.AddListener(Back);
            // The grid moves with the D-pad itself (MenuPressed), so the general selector stands aside here.
            if (GetComponent<PadSelfNavigation>() == null) gameObject.AddComponent<PadSelfNavigation>();
            Loc.Changed += OnLanguageChanged;
        }

        protected override void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            base.OnDestroy();
        }

        void OnLanguageChanged()
        {
            if (!IsVisible) return;
            foreach (var tile in m_Tiles) tile.Bind(tile.Pick);
            ApplyStatic();
            Highlight(m_Index, false);
        }

        /// <summary>Versus the AI: the streamer you picked, shown on the right while you pick the AI's.</summary>
        public byte PlayerPick { get; set; }

        /// <summary>
        /// Couch Competitive (2026-11-17): the Versus the AI picks become Player 1's (<see cref="StreamerSelectMode.VersusAi"/>)
        /// and Player 2's (<see cref="StreamerSelectMode.Opponent"/>), with titles to match.
        /// </summary>
        public bool Couch { get; set; }

        /// <summary>Opens the select screen. <paramref name="onConfirmed"/> receives the pick (a real streamer or None).</summary>
        public void Open(StreamerSelectMode mode, Action<byte> onConfirmed, Action onBack)
        {
            m_Mode = mode;
            m_OnConfirmed = onConfirmed;
            m_OnBack = onBack;
            Show();
        }

        protected override void OnShow()
        {
            m_Locked = false;
            m_QuotePick = -1;
            BuildTiles();
            RefreshLocks();
            ApplyStatic();
            if (announcerLabel != null) announcerLabel.gameObject.SetActive(false);
            if (flash != null) flash.gameObject.SetActive(false);

            byte last = AppSettings.LastStreamer;
            int start = m_Tiles.FindIndex(t => t.Pick == last);
            Highlight(start >= 0 ? start : 0, false);
            AudioService.Play(Sfx.Notify);
            Fx.Play(FxCue.MenuIn, p1Root);
            Fx.Play(FxCue.MenuIn, p2Root);
        }

        void BuildTiles()
        {
            if (m_Tiles.Count > 0) return;
            var picks = new List<byte>();
            foreach (var type in StreamerCatalog.Roster) picks.Add((byte)type);
            picks.Add(StreamerCatalog.RandomPick);
            picks.Add((byte)StreamerType.None);

            foreach (var pick in picks)
            {
                var tile = Instantiate(tilePrefab, grid);
                tile.name = "Tile " + pick;
                tile.gameObject.SetActive(true);
                tile.Bind(pick);
                tile.Hovered += t => Highlight(m_Tiles.IndexOf(t), true);
                tile.Clicked += t =>
                {
                    int index = m_Tiles.IndexOf(t);
                    if (index == m_Index) Confirm();
                    else Highlight(index, true);
                };
                m_Tiles.Add(tile);
            }
            FitGrid();
        }

        /// <summary>
        /// Two rows, however many streamers there are: scenes built for 13 tiles (7 a row) widen to 8 a row for the 15 of
        /// 2026-11-18, narrowing the tiles to fit.
        /// </summary>
        void FitGrid()
        {
            var layout = grid != null ? grid.GetComponent<GridLayoutGroup>() : null;
            if (layout == null || layout.constraint != GridLayoutGroup.Constraint.FixedColumnCount) return;
            int needed = Mathf.CeilToInt(m_Tiles.Count / 2f);
            if (layout.constraintCount >= needed) return;
            float width = grid.rect.width - layout.padding.horizontal;
            if (width <= 0f) width = layout.constraintCount * layout.cellSize.x + (layout.constraintCount - 1) * layout.spacing.x;
            float cell = Mathf.Floor((width - (needed - 1) * layout.spacing.x) / needed);
            layout.constraintCount = needed;
            layout.cellSize = new Vector2(Mathf.Min(layout.cellSize.x, cell), layout.cellSize.y);
        }

        /// <summary>Columns in the roster grid (for moving up and down with the arrows).</summary>
        int GridColumns
        {
            get
            {
                var layout = grid != null ? grid.GetComponent<GridLayoutGroup>() : null;
                return layout != null && layout.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? Mathf.Max(1, layout.constraintCount) : Columns;
            }
        }

        /// <summary>Streamers still locked (their unlock achievement not earned) show a lock; Random and No Streamer never do.</summary>
        void RefreshLocks()
        {
            foreach (var tile in m_Tiles)
            {
                bool streamer = tile.Pick != StreamerCatalog.RandomPick && tile.Pick != (byte)StreamerType.None;
                tile.SetLocked(streamer && !Unlocks.IsStreamerUnlocked((StreamerType)tile.Pick));
            }
        }

        static bool IsLockedPick(byte pick) =>
            pick != StreamerCatalog.RandomPick && pick != (byte)StreamerType.None && !Unlocks.IsStreamerUnlocked((StreamerType)pick);

        void ApplyStatic()
        {
            if (Couch && (m_Mode == StreamerSelectMode.VersusAi || m_Mode == StreamerSelectMode.Opponent))
            {
                ApplyCouch(m_Mode == StreamerSelectMode.Opponent);
                return;
            }
            bool rivalPick = m_Mode == StreamerSelectMode.Opponent;
            if (titleLabel != null) titleLabel.text = Loc.Get(rivalPick ? "select.title_rival" : "select.title");
            string subtitle = m_Mode == StreamerSelectMode.SinglePlayer ? "select.subtitle_single"
                : m_Mode == StreamerSelectMode.VersusAi ? "select.subtitle_versus"
                : m_Mode == StreamerSelectMode.MatchPick ? (TeamPick ? "select.subtitle_team" : "select.subtitle_match")
                : rivalPick ? "select.subtitle_rival" : "select.subtitle_multi";
            if (subtitleLabel != null) subtitleLabel.text = Loc.Get(subtitle);
            if (hintLabel != null) hintLabel.text = Loc.Get(m_Mode == StreamerSelectMode.MatchPick ? "select.hint_match" : "select.hint");
            if (vsLabel != null) vsLabel.text = Loc.Get("select.vs");
            if (backButton != null) backButton.gameObject.SetActive(m_Mode != StreamerSelectMode.MatchPick);
            if (confirmLabel != null) confirmLabel.text = Loc.Get(rivalPick ? "select.confirm_rival" : "select.confirm");

            if (rivalPick)
            {
                // Picking the AI's streamer: you (already picked) stand on the right.
                bool random = PlayerPick == StreamerCatalog.RandomPick;
                var you = random ? StreamerType.None : (StreamerType)PlayerPick;
                if (p2Name != null) p2Name.text = Loc.Get("select.you", "streamer", random ? Loc.Get("streamer.Random.name") : StreamerVisuals.Name(you));
                if (p2Sub != null) p2Sub.text = Loc.Get("select.you_sub");
                if (p2Initials != null) p2Initials.text = random ? "?" : StreamerVisuals.Initials(you);
                if (p2Placeholder != null) p2Placeholder.color = random ? UIPalette.Purple : StreamerVisuals.Color(you);
                return;
            }

            bool single = m_Mode == StreamerSelectMode.SinglePlayer;
            bool versus = m_Mode == StreamerSelectMode.VersusAi;
            if (p2Name != null) p2Name.text = Loc.Get(single || versus ? "select.opponent_algorithm" : "select.opponent_unknown");
            if (p2Sub != null) p2Sub.text = Loc.Get(versus ? "select.opponent_versus_sub" : single ? "select.opponent_algorithm_sub" : "select.opponent_unknown_sub");
            if (p2Initials != null) p2Initials.text = single || versus ? "A" : "?";
            if (p2Placeholder != null) p2Placeholder.color = single || versus ? UIPalette.Gold : UIPalette.TextDim;
        }

        /// <summary>Couch play: "PLAYER 1 / PLAYER 2: SELECT YOUR STREAMER", the other player on the right.</summary>
        void ApplyCouch(bool second)
        {
            if (titleLabel != null) titleLabel.text = Loc.Get("select.title_couch", "player", second ? 2 : 1);
            if (subtitleLabel != null) subtitleLabel.text = Loc.Get(second ? "select.subtitle_couch_second" : "select.subtitle_couch");
            if (hintLabel != null) hintLabel.text = Loc.Get("select.hint");
            if (vsLabel != null) vsLabel.text = Loc.Get("select.vs");
            if (backButton != null) backButton.gameObject.SetActive(true);
            if (confirmLabel != null) confirmLabel.text = Loc.Get("select.confirm");

            if (second)
            {
                // Player 2 picks: Player 1 (already picked) stands on the right.
                bool random = PlayerPick == StreamerCatalog.RandomPick;
                var first = random ? StreamerType.None : (StreamerType)PlayerPick;
                string name = random ? Loc.Get("streamer.Random.name") : StreamerVisuals.Name(first);
                if (p2Name != null) p2Name.text = Loc.Get("couch.player_streamer", "player", Loc.Get("couch.player", "number", 1), "streamer", name);
                if (p2Sub != null) p2Sub.text = Loc.Get("select.couch_p1_sub");
                if (p2Initials != null) p2Initials.text = random ? "?" : StreamerVisuals.Initials(first);
                if (p2Placeholder != null) p2Placeholder.color = random ? UIPalette.Purple : StreamerVisuals.Color(first);
                return;
            }
            if (p2Name != null) p2Name.text = Loc.Get("couch.player", "number", 2);
            if (p2Sub != null) p2Sub.text = Loc.Get("select.couch_p2_sub");
            if (p2Initials != null) p2Initials.text = "2";
            if (p2Placeholder != null) p2Placeholder.color = UIPalette.PlayerColor(1);
        }

        void Highlight(int index, bool feedback)
        {
            if (m_Locked || index < 0 || index >= m_Tiles.Count) return;
            bool changed = index != m_Index;
            m_Index = index;
            for (int i = 0; i < m_Tiles.Count; i++) m_Tiles[i].SetSelected(i == m_Index);
            ShowPick(m_Tiles[m_Index].Pick);
            if (feedback && changed)
            {
                AudioService.Play(Sfx.Hover);
                Fx.Play(FxCue.CardHover, m_Tiles[m_Index].animRoot != null ? m_Tiles[m_Index].animRoot : (RectTransform)m_Tiles[m_Index].transform);
                Fx.Play(FxCue.CounterPunch, p1Root);
            }
        }

        void ShowPick(byte pick)
        {
            bool random = pick == StreamerCatalog.RandomPick;
            var type = random ? StreamerType.None : (StreamerType)pick;
            var sprite = random ? null : StreamerVisuals.Portrait(type);

            if (p1Portrait != null)
            {
                p1Portrait.enabled = sprite != null;
                p1Portrait.sprite = sprite;
            }
            if (p1Placeholder != null)
            {
                p1Placeholder.gameObject.SetActive(sprite == null);
                p1Placeholder.color = random ? UIPalette.Purple : StreamerVisuals.Color(type);
            }
            if (p1Initials != null) p1Initials.text = random ? "?" : StreamerVisuals.Initials(type);

            if (pick != m_QuotePick)
            {
                m_QuotePick = pick;
                m_LastQuotes.TryGetValue(pick, out var previous);
                m_Quote = random ? null : StreamerVisuals.PickQuote(type, previous);
                if (m_Quote != null) m_LastQuotes[pick] = m_Quote;
            }

            if (random)
            {
                Set(p1Name, Loc.Get("streamer.Random.name"));
                Set(p1Title, Loc.Get("streamer.Random.title"));
                Set(p1Kind, string.Empty);
                Set(p1Power, string.Empty);
                Set(p1Description, Loc.Get("streamer.Random.desc"));
                Set(p1Cost, string.Empty);
                Set(p1Quote, string.Empty);
            }
            else
            {
                Set(p1Name, GuestVisuals.Colored(StreamerVisuals.Name(type), StreamerVisuals.Color(type)));
                Set(p1Title, Loc.Get($"streamer.{type}.title"));
                Set(p1Kind, StreamerVisuals.KindLabel(type));
                Set(p1Power, type == StreamerType.None ? string.Empty : Loc.Get("select.power") + ": " + StreamerVisuals.PowerName(type));
                Set(p1Description, StreamerVisuals.PowerDescription(type));
                Set(p1Cost, StreamerVisuals.CostRules(type));
                string quote = StreamerVisuals.QuoteText(type, m_Quote);
                Set(p1Quote, quote.Length > 0 ? $"<i>\"{quote}\"</i>" : string.Empty);
            }

            // Rival powers need a rival: none in a solo season, and none in co-op (both players are one team).
            bool noRival = m_Mode == StreamerSelectMode.SinglePlayer ||
                           (m_Mode == StreamerSelectMode.MatchPick && Net != null && Net.Settings.Mode == Networking.MatchMode.CoopEndless);
            bool rivalWarning = !random && noRival && StreamerCatalog.Get(type).NeedsRival;
            string warning = rivalWarning ? Loc.Get("select.rival_warning") : string.Empty;
            // Locked: what unlocks them (the power above still shows, as a teaser).
            if (IsLockedPick(pick)) warning = Loc.Get("select.locked_requirement", "requirement", Achievements.RequirementText(Unlocks.StreamerCondition(type)));
            Set(p1Warning, warning);
            if (confirmButton != null) confirmButton.interactable = !IsLockedPick(pick);
        }

        static void Set(TMP_Text label, string text)
        {
            if (label != null) label.text = text;
        }

        void Update()
        {
            if (!IsVisible) return;

            if (m_Mode == StreamerSelectMode.MatchPick)
            {
                UpdateMatchPick();
                return;
            }

            // The decorative fighting-game clock. It never runs out: chat always gets more time.
            m_Timer -= Time.unscaledDeltaTime;
            if (m_Timer < 10f) m_Timer = 99f;
            if (timerLabel != null) timerLabel.text = Mathf.CeilToInt(m_Timer).ToString("00");

            if (m_Locked || InputHelper.IsTyping || ModalDialog.IsOpen) return;
            if (InputHelper.MenuPressed(HotKey.Left)) Move(-1, 0);
            else if (InputHelper.MenuPressed(HotKey.Right)) Move(1, 0);
            else if (InputHelper.MenuPressed(HotKey.Up)) Move(0, -1);
            else if (InputHelper.MenuPressed(HotKey.Down)) Move(0, 1);
            else if (InputHelper.MenuPressed(HotKey.Enter) || InputHelper.MenuPressed(HotKey.Space)) Confirm();
            else if (InputHelper.MenuPressed(HotKey.Escape)) Back();
        }

        static Networking.DeckPartyNetSession Net => Networking.DeckPartyNetSession.Instance;

        /// <summary>Team modes: this player picks the team's streamer (their teammate waits).</summary>
        bool TeamPick => Net != null && Net.IsTeamMode;

        /// <summary>
        /// Multiplayer pick: the clock is the real pick timer (anyone not locked in when it runs out plays a random streamer);
        /// once you've locked in, the hint says how many players are still choosing. If the pick is called off (someone
        /// left), back to the lobby.
        /// </summary>
        void UpdateMatchPick()
        {
            var net = Net;
            if (net == null || !net.IsSpawned || net.Phase == Networking.MatchPhase.Lobby)
            {
                if (!m_Locked) Back();
                else if (m_OnBack != null) m_OnBack();
                m_OnBack = null;
                return;
            }
            if (timerLabel != null) timerLabel.text = Mathf.CeilToInt(net.PickSecondsLeft).ToString("00");
            if (m_Locked)
            {
                if (hintLabel != null) hintLabel.text = Loc.Get("select.waiting_players", "count", net.PlayersStillPicking);
                return;
            }
            if (InputHelper.IsTyping || ModalDialog.IsOpen) return;
            if (InputHelper.MenuPressed(HotKey.Left)) Move(-1, 0);
            else if (InputHelper.MenuPressed(HotKey.Right)) Move(1, 0);
            else if (InputHelper.MenuPressed(HotKey.Up)) Move(0, -1);
            else if (InputHelper.MenuPressed(HotKey.Down)) Move(0, 1);
            else if (InputHelper.MenuPressed(HotKey.Enter) || InputHelper.MenuPressed(HotKey.Space)) Confirm();
            // No Esc: the match is starting.
        }

        void Move(int dx, int dy)
        {
            int count = m_Tiles.Count;
            int index = m_Index;
            if (dx != 0) index = (index + dx + count) % count;
            if (dy != 0)
            {
                index += dy * GridColumns;
                if (index < 0 || index >= count) index = m_Index;
            }
            Highlight(index, true);
        }

        void Confirm()
        {
            if (m_Locked || m_Tiles.Count == 0) return;
            byte pick = m_Tiles[m_Index].Pick;
            if (IsLockedPick(pick))
            {
                // Locked: say what unlocks them instead.
                AudioService.Play(Sfx.Error);
                Fx.Play(FxCue.CounterPunch, p1Root);
                return;
            }
            if (pick == StreamerCatalog.RandomPick)
            {
                // Random only lands on streamers you've unlocked.
                var pool = Unlocks.UnlockedRoster();
                pick = pool.Count > 0 ? (byte)pool[UnityEngine.Random.Range(0, pool.Count)] : (byte)StreamerType.None;
                ShowPick(pick);
            }
            // The AI's pick isn't remembered as yours.
            if (m_Mode != StreamerSelectMode.Opponent) AppSettings.LastStreamer = m_Tiles[m_Index].Pick;
            StartCoroutine(ConfirmRoutine(pick));
        }

        IEnumerator ConfirmRoutine(byte pick)
        {
            m_Locked = true;
            var type = (StreamerType)pick;
            AudioService.Play(Sfx.StarArrive);
            Fx.Play(FxCue.SelectConfirm, p1Root);
            Fx.Shake(10f);

            if (flash != null)
            {
                flash.gameObject.SetActive(true);
                flash.color = AppSettings.ReduceFlashing ? Color.white.WithAlpha(0.15f) : Color.white.WithAlpha(0.7f);
                StartCoroutine(FadeFlash());
            }
            if (announcerLabel != null)
            {
                announcerLabel.gameObject.SetActive(true);
                announcerLabel.text = type == StreamerType.None ? Loc.Get("select.confirm") : Loc.Get("select.announce", "streamer", StreamerVisuals.Name(type));
                announcerLabel.color = type == StreamerType.None ? UIPalette.Text : StreamerVisuals.Color(type);
                UITween.PopIn(this, announcerLabel.transform, 0.3f, 0f, 1.6f);
            }

            yield return new WaitForSecondsRealtime(1.1f * AppSettings.AnimationTimeScale);
            var callback = m_OnConfirmed;
            m_OnConfirmed = null;
            callback?.Invoke(pick);
        }

        IEnumerator FadeFlash()
        {
            float start = flash.color.a;
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                flash.color = flash.color.WithAlpha(Mathf.Lerp(start, 0f, t / 0.35f));
                yield return null;
            }
            flash.gameObject.SetActive(false);
        }

        void Back()
        {
            if (m_Locked) return;
            // The multiplayer pick has no way back while it's on (the match is starting).
            if (m_Mode == StreamerSelectMode.MatchPick && Net != null && Net.PickingStreamers) return;
            var callback = m_OnBack;
            m_OnBack = null;
            if (callback != null) callback();
            else m_Menu.ShowMain();
        }
    }
}
