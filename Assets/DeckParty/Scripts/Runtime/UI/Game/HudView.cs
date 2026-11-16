using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The site's menu bar (top of the stream and shop): the stream count, then Cash, Influence and Trophies (in chat / needed to
    /// win) in the middle, then the Audience and Menu icon buttons. It also lights the Deplatform Risk pips, which sit in the
    /// Stream Chat's title band. Everything is an icon and a number; hovering any of it says what it is
    /// (the night's tooltip also names the character set).
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text scenarioLabel;
        [SerializeField] internal TMP_Text nightLabel;
        [SerializeField] internal TMP_Text cashLabel;
        [SerializeField] internal TMP_Text influenceLabel;
        [SerializeField] internal TMP_Text capacityLabel;
        [Tooltip("Optional text next to the risk pips (older scenes). The pips alone show the count.")]
        [SerializeField] internal TMP_Text troubleLabel;
        [SerializeField] internal Image[] troublePips;
        [Tooltip("Optional text next to the Plays pips (older scenes).")]
        [SerializeField] internal TMP_Text starsLabel;
        [SerializeField] internal Image[] starPips;
        [SerializeField] internal TooltipTrigger troubleTooltip;
        [SerializeField] internal TooltipTrigger starsTooltip;
        [SerializeField] internal Button rolodexButton;
        [Tooltip("The number on the Audience button: viewers still in the queue during a stream, your whole Audience between " +
                 "streams. Optional.")]
        [SerializeField] internal TMP_Text queueLabel;
        [SerializeField] internal Button menuButton;

        [Header("Website bar (optional)")]
        [SerializeField] internal Image cashIcon;
        [SerializeField] internal Image influenceIcon;
        [SerializeField] internal Image nightIcon;
        [SerializeField] internal Image capacityIcon;
        [Tooltip("The Trophy icon next to the Trophies count (in chat / needed to win).")]
        [SerializeField] internal Image starsIcon;
        [SerializeField] internal TooltipTrigger nightTooltip;
        [Tooltip("The risk pips' block: what feedbacks punch when risk changes.")]
        [SerializeField] internal RectTransform troubleMeter;
        [Tooltip("The Plays pips' block.")]
        [SerializeField] internal RectTransform starsMeter;

        public event Action RolodexClicked;
        public event Action MenuClicked;

        /// <summary>Where Deplatform Risk feedbacks play (the pips, or the old label).</summary>
        public Transform RiskAnchor => troubleMeter != null ? troubleMeter : troubleLabel != null ? troubleLabel.transform : transform;
        /// <summary>Where Plays feedbacks play.</summary>
        public Transform PlaysAnchor => starsMeter != null ? starsMeter : starsLabel != null ? starsLabel.transform : transform;

        int m_LastCash = int.MinValue;
        int m_LastInfluence = int.MinValue;
        int m_LastTrouble = -1;
        int m_LastDiamonds = -1;
        /// <summary>Endless: the Trophies banked at the last render (the counter punches when they change).</summary>
        int m_LastBanked = -1;
        /// <summary>The Deplatform Risk the pips show (0 outside a stream).</summary>
        int m_PipTrouble;
        PlayerState m_State;

        void Awake()
        {
            rolodexButton.onClick.AddListener(() => RolodexClicked?.Invoke());
            menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
            if (troubleTooltip != null) troubleTooltip.Provider = TroubleTooltip;
            if (starsTooltip != null) starsTooltip.Provider = DiamondsTooltip;
            if (nightTooltip != null) nightTooltip.Provider = NightTooltip;
            ApplyCaptions();
            Loc.Changed += OnLanguageChanged;
        }

        void OnDestroy() => Loc.Changed -= OnLanguageChanged;

        void OnLanguageChanged()
        {
            ApplyCaptions();
            if (m_State != null) Render(m_State);
        }

        /// <summary>Static captions and their tooltips (captions are upper-cased by the font style, not in code).</summary>
        void ApplyCaptions()
        {
            // Scenes built since 2026-11-15 call these blocks Cash and Influence; older ones Pop (now Cash) and Cash (now Influence).
            var influenceBlock = transform.Find("Influence");
            bool renamed = influenceBlock != null || FindDeep(transform, "Influence") != null;
            SetCaption(renamed ? "Cash" : "Pop", null, Loc.Get("hud.tip.cash_banked"));
            SetCaption(renamed ? "Influence" : "Cash", null, Loc.Get("hud.tip.influence"));
            SetCaption("House", null, Loc.Get("hud.tip.sub_cap"));
            // Icon buttons say what they do on hover; text buttons (older scenes) keep their caption.
            SetButton(rolodexButton, Loc.Get("hud.audience"), "R");
            SetButton(menuButton, Loc.Get("hud.menu"), "Esc");
            if (cashIcon != null) cashIcon.color = GameIcons.Tint(ResourceIcon.Cash);
            if (influenceIcon != null) influenceIcon.color = GameIcons.Tint(ResourceIcon.Influence);
            if (starsIcon != null) starsIcon.color = GameIcons.Tint(ResourceIcon.Plays);
            // Your own art keeps its colours; the white placeholder shapes take the menu bar's text colour (UI Skin > Site Colours).
            if (nightIcon != null) nightIcon.color = GameIcons.SlotTint(SkinSlot.IconNight, SiteColours.Get(SiteColourRole.Text, nightIcon.transform));
            if (capacityIcon != null) capacityIcon.color = GameIcons.SlotTint(SkinSlot.IconDoor, SiteColours.Get(SiteColourRole.Text, capacityIcon.transform));
        }

        /// <summary>
        /// The Audience button's number and tooltip: during a stream the viewers still waiting in the queue, between streams
        /// your whole Audience (they all queue up for the next stream).
        /// </summary>
        void RenderQueue(PlayerState s, bool party, int member)
        {
            // Teams: your own queue and Audience.
            int count = party ? s.PoolCountOf(member) : s.IsTeam ? s.PermanentCountOf(member) : s.Rolodex.Count;
            if (queueLabel != null) queueLabel.text = count.ToString();
            if (queueLabel == null || rolodexButton == null || !rolodexButton.TryGetComponent<TooltipTrigger>(out var trigger)) return;
            string line = party ? Loc.Get("controls.queue_count", "count", count) : Loc.Get("hud.audience_count", "count", count);
            trigger.Text = TipText.Titled(Loc.Get("hud.audience"), TipText.Key("R")).Line(line);
        }

        /// <summary>Icon buttons say what they do (and their key) on hover; text buttons (older scenes) show it as their caption.</summary>
        static void SetButton(Button button, string text, string key)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && button.GetComponent<TooltipTrigger>() == null) label.text = $"{text}  [{key}]";
            else if (button.TryGetComponent<TooltipTrigger>(out var trigger)) trigger.Text = TipText.Titled(text, TipText.Key(key));
        }

        /// <param name="caption">Null hides the caption (resources show only their icon and number).</param>
        /// <returns>False when there is no such block.</returns>
        bool SetCaption(string block, string caption, string tooltip)
        {
            var root = transform.Find(block);
            if (root == null) root = FindDeep(transform, block);
            if (root == null) return false;
            var label = root.Find("Texts/Caption");
            if (label != null)
            {
                label.gameObject.SetActive(caption != null);
                if (caption != null && label.TryGetComponent<TMP_Text>(out var text)) text.text = caption;
            }
            if (root.TryGetComponent<TooltipTrigger>(out var trigger)) trigger.Text = tooltip;
            return true;
        }

        static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <param name="member">Teams (2v2, co-op): the member on this screen (their queue and Sub Cap); 0 otherwise.</param>
        public void Render(PlayerState s, int member = 0)
        {
            m_State = s;
            if (scenarioLabel != null) scenarioLabel.text = ContentText.ScenarioName(s.ScenarioId);
            // The "/25" and "/4" parts in the menu bar's faint text colour (UI Skin > Site Colours).
            string faint = "#" + ColorUtility.ToHtmlStringRGB(SiteColours.Get(SiteColourRole.FaintText, transform));
            if (nightLabel != null)
                nightLabel.text = nightTooltip != null ? $"{s.Night}<size=70%><color={faint}>/{s.TotalNights}</color></size>" : Loc.Get("hud.night", "night", s.Night, "total", s.TotalNights);

            cashLabel.text = s.Cash.ToString();
            influenceLabel.text = s.Influence.ToString();
            if (m_LastCash != int.MinValue && s.Cash != m_LastCash) UITween.Punch(this, cashLabel.transform, 0.25f);
            if (m_LastInfluence != int.MinValue && s.Influence != m_LastInfluence) UITween.Punch(this, influenceLabel.transform, 0.25f);
            m_LastCash = s.Cash;
            m_LastInfluence = s.Influence;

            bool party = s.Phase == GamePhase.Party;
            RenderQueue(s, party, member);
            // Older scenes only: the menu bar no longer shows the Sub Cap (the chat's seats do).
            if (capacityLabel != null)
                capacityLabel.text = party ? $"{s.OccupancyOf(member)}<size=70%><color={faint}>/{s.CapacityOf(member)}</color></size>" : s.CapacityOf(member).ToString();

            int trouble = party ? PartyEngine.NetTrouble(s) : 0;
            int diamonds = party ? PartyEngine.DiamondsPresent(s) : 0;

            m_PipTrouble = trouble;
            ApplyRiskPips();
            if (troubleLabel != null)
            {
                troubleLabel.enableAutoSizing = true;
                troubleLabel.fontSizeMin = 14;
                string riskText = Loc.Get("hud.risk", "risk", trouble, "max", GameRules.RaidTroubleThreshold);
                troubleLabel.text = trouble >= GameRules.RaidTroubleThreshold - 1 && party ? $"<color=#FF5A5A>{riskText}</color>" : riskText;
            }

            // Older scenes: a pip per Trophy. Now: one Trophy icon and "in chat / needed to win", counting up as Trophy viewers join.
            if (starPips != null)
                for (int i = 0; i < starPips.Length; i++)
                    if (starPips[i] != null) starPips[i].color = GameIcons.PipColor(ResourceIcon.Plays, i < diamonds, SitePalette.Gold, SitePalette.Off);
            // Endless: Trophies are banked like the other resources (no "/4": there's no win), with the ones in chat beside them.
            if (starsLabel != null && s.IsEndless)
                starsLabel.text = diamonds > 0 ? $"{s.Trophies}<size=70%><color={faint}> +{diamonds}</color></size>" : s.Trophies.ToString();
            else if (starsLabel != null)
                starsLabel.text = starsIcon != null
                    ? $"{diamonds}<size=70%><color={faint}>/{GameRules.DiamondsToWin}</color></size>"
                    : Loc.Get("hud.diamonds", "count", diamonds, "max", GameRules.DiamondsToWin);

            if (m_LastTrouble >= 0 && trouble > m_LastTrouble) UITween.Punch(this, RiskAnchor, 0.3f);
            if (m_LastDiamonds >= 0 && diamonds > m_LastDiamonds) UITween.Punch(this, PlaysAnchor, 0.3f);
            if (s.IsEndless && m_LastBanked >= 0 && s.Trophies != m_LastBanked) UITween.Punch(this, PlaysAnchor, 0.35f);
            m_LastBanked = s.IsEndless ? s.Trophies : -1;
            m_LastTrouble = trouble;
            m_LastDiamonds = diamonds;
        }

        /// <summary>
        /// The pips sit in the Stream Chat window, which is switched off between streams. Switching it back on makes each pip's
        /// SkinnedImage re-apply the UI Skin's colour (full colour for your own art), which made them look lit until the next
        /// render. So they're re-coloured here every frame, after anything else has touched them (unchanged colours cost nothing).
        /// </summary>
        void LateUpdate() => ApplyRiskPips();

        /// <summary>Lights the Deplatform Risk pips for the risk last rendered (red for the one that would get you deplatformed).</summary>
        void ApplyRiskPips()
        {
            if (troublePips == null) return;
            for (int i = 0; i < troublePips.Length; i++)
            {
                if (troublePips[i] == null) continue;
                bool lit = i < m_PipTrouble;
                var hot = i >= GameRules.RaidTroubleThreshold - 1 ? SitePalette.Danger : SitePalette.Warning;
                troublePips[i].color = GameIcons.PipColor(ResourceIcon.Risk, lit, hot, SitePalette.Off);
            }
        }

        string NightTooltip()
        {
            if (m_State == null) return null;
            string title = m_State.IsEndless
                ? Loc.Get("hud.night_endless", ("night", m_State.Night), ("total", m_State.TotalNights), ("left", m_State.NightsRemaining))
                : Loc.Get("hud.night", "night", m_State.Night, "total", m_State.TotalNights);
            return TipText.Titled(title).Detail(ContentText.ScenarioName(m_State.ScenarioId));
        }

        /// <summary>Outside a stream: what Risk is. During one: where this stream's Risk comes from, against the limit.</summary>
        string TroubleTooltip()
        {
            int max = GameRules.RaidTroubleThreshold;
            if (m_State == null || m_State.Phase != GamePhase.Party)
                return TipText.Titled(Loc.Get("hud.tip.risk_title")).Line(Loc.Get("hud.tip.risk_rule", "max", max));
            int net = PartyEngine.NetTrouble(m_State);
            var tip = TipText.Titled(Loc.Get("hud.tip.risk_title"), $"{net}/{max}");
            tip.Line(Loc.Get("hud.tip.risk_added", "count", PartyEngine.TroubleTotal(m_State) - m_State.PartyHeat));
            if (m_State.PartyHeat > 0) tip.Line(Loc.Get("hud.tip.risk_heat", "count", m_State.PartyHeat));
            int cancelled = PartyEngine.CancelCount(m_State) - m_State.PartyShield;
            if (cancelled > 0) tip.Line(Loc.Get("hud.tip.risk_cancelled", "count", cancelled));
            int fanbase = PartyEngine.FanbaseCancels(m_State);
            if (fanbase > 0) tip.Line(Loc.Get("hud.tip.risk_fanbase", "count", fanbase));
            if (m_State.PartyShield > 0) tip.Line(Loc.Get("hud.tip.risk_shield", "count", m_State.PartyShield));
            return tip.Section().Detail(Loc.Get("hud.tip.risk_rule", "max", max));
        }

        string DiamondsTooltip()
        {
            if (m_State == null) return null;
            int now = m_State.Phase == GamePhase.Party ? PartyEngine.DiamondsPresent(m_State) : 0;
            var tip = TipText.Titled(Loc.Get("hud.tip.trophies_title"));
            if (m_State.IsEndless)
            {
                // Endless: Trophies are banked and spent in the shop; there's no win.
                tip.Line(Loc.Get("hud.tip.trophies_banked", "count", m_State.Trophies));
                if (m_State.Phase == GamePhase.Party) tip.Line(Loc.Get("hud.tip.trophies_in_chat", "count", now));
                return tip.Section().Detail(Loc.Get("hud.tip.trophies_endless_rule", "stream", PartyEngine.ExtraStreamCost(m_State), "viewer", PartyEngine.ShopViewerCost(m_State)));
            }
            if (m_State.Phase == GamePhase.Party) tip.Line(Loc.Get("hud.tip.trophies_in_chat_goal", "count", now, "goal", GameRules.DiamondsToWin));
            tip.Line(Loc.Get("hud.tip.trophies_owned", "count", m_State.DiamondsOwned()));
            if (m_State.BestDiamonds > 0) tip.Line(Loc.Get("hud.tip.trophies_best", "count", m_State.BestDiamonds));
            return tip.Section().Detail(Loc.Get("hud.tip.trophies_rule", "goal", GameRules.DiamondsToWin));
        }
    }
}
