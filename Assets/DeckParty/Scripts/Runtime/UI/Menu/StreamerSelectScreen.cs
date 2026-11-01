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
        }

        void ApplyStatic()
        {
            if (titleLabel != null) titleLabel.text = Loc.Get("select.title");
            if (subtitleLabel != null) subtitleLabel.text = Loc.Get(m_Mode == StreamerSelectMode.SinglePlayer ? "select.subtitle_single" : "select.subtitle_multi");
            if (hintLabel != null) hintLabel.text = Loc.Get("select.hint");
            if (vsLabel != null) vsLabel.text = Loc.Get("select.vs");
            if (confirmLabel != null) confirmLabel.text = Loc.Get("select.confirm");

            bool single = m_Mode == StreamerSelectMode.SinglePlayer;
            if (p2Name != null) p2Name.text = Loc.Get(single ? "select.opponent_algorithm" : "select.opponent_unknown");
            if (p2Sub != null) p2Sub.text = Loc.Get(single ? "select.opponent_algorithm_sub" : "select.opponent_unknown_sub");
            if (p2Initials != null) p2Initials.text = single ? "A" : "?";
            if (p2Placeholder != null) p2Placeholder.color = single ? UIPalette.Gold : UIPalette.TextDim;
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

            bool rivalWarning = !random && m_Mode == StreamerSelectMode.SinglePlayer && StreamerCatalog.Get(type).NeedsRival;
            Set(p1Warning, rivalWarning ? Loc.Get("select.rival_warning") : string.Empty);
        }

        static void Set(TMP_Text label, string text)
        {
            if (label != null) label.text = text;
        }

        void Update()
        {
            if (!IsVisible) return;

            // The decorative fighting-game clock. It never runs out: chat always gets more time.
            m_Timer -= Time.unscaledDeltaTime;
            if (m_Timer < 10f) m_Timer = 99f;
            if (timerLabel != null) timerLabel.text = Mathf.CeilToInt(m_Timer).ToString("00");

            if (m_Locked || InputHelper.IsTyping || ModalDialog.IsOpen) return;
            if (InputHelper.Pressed(HotKey.Left)) Move(-1, 0);
            else if (InputHelper.Pressed(HotKey.Right)) Move(1, 0);
            else if (InputHelper.Pressed(HotKey.Up)) Move(0, -1);
            else if (InputHelper.Pressed(HotKey.Down)) Move(0, 1);
            else if (InputHelper.Pressed(HotKey.Enter) || InputHelper.Pressed(HotKey.Space)) Confirm();
            else if (InputHelper.Pressed(HotKey.Escape)) Back();
        }

        void Move(int dx, int dy)
        {
            int count = m_Tiles.Count;
            int index = m_Index;
            if (dx != 0) index = (index + dx + count) % count;
            if (dy != 0)
            {
                index += dy * Columns;
                if (index < 0 || index >= count) index = m_Index;
            }
            Highlight(index, true);
        }

        void Confirm()
        {
            if (m_Locked || m_Tiles.Count == 0) return;
            byte pick = m_Tiles[m_Index].Pick;
            if (pick == StreamerCatalog.RandomPick)
            {
                pick = (byte)StreamerCatalog.Roster[UnityEngine.Random.Range(0, StreamerCatalog.Roster.Length)];
                ShowPick(pick);
            }
            AppSettings.LastStreamer = m_Tiles[m_Index].Pick;
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
            var callback = m_OnBack;
            m_OnBack = null;
            if (callback != null) callback();
            else m_Menu.ShowMain();
        }
    }
}
