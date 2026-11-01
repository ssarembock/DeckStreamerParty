using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Top bar: night, Gifted Subs, Cash, Sub Cap, Swatting Risk meter and Diamond meter.</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text scenarioLabel;
        [SerializeField] internal TMP_Text nightLabel;
        [SerializeField] internal TMP_Text popLabel;
        [SerializeField] internal TMP_Text cashLabel;
        [SerializeField] internal TMP_Text capacityLabel;
        [SerializeField] internal TMP_Text troubleLabel;
        [SerializeField] internal Image[] troublePips;
        [SerializeField] internal TMP_Text starsLabel;
        [SerializeField] internal Image[] starPips;
        [SerializeField] internal TooltipTrigger troubleTooltip;
        [SerializeField] internal TooltipTrigger starsTooltip;
        [SerializeField] internal Button rolodexButton;
        [SerializeField] internal Button menuButton;

        public event Action RolodexClicked;
        public event Action MenuClicked;

        int m_LastPop = int.MinValue;
        int m_LastCash = int.MinValue;
        int m_LastTrouble = -1;
        int m_LastDiamonds = -1;
        PlayerState m_State;

        void Awake()
        {
            rolodexButton.onClick.AddListener(() => RolodexClicked?.Invoke());
            menuButton.onClick.AddListener(() => MenuClicked?.Invoke());
            if (troubleTooltip != null) troubleTooltip.Provider = TroubleTooltip;
            if (starsTooltip != null) starsTooltip.Provider = DiamondsTooltip;
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
            SetCaption("Pop", Terms.GiftedSubs, Loc.Get("hud.tip.gifted_subs"));
            SetCaption("Cash", Terms.Cash, Loc.Get("hud.tip.cash"));
            SetCaption("House", Terms.SubCap, Loc.Get("hud.tip.sub_cap"));
            var audienceLabel = rolodexButton != null ? rolodexButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (audienceLabel != null) audienceLabel.text = Loc.Get("hud.audience_button");
        }

        void SetCaption(string block, string caption, string tooltip)
        {
            var root = transform.Find(block);
            if (root == null) return;
            var label = root.Find("Texts/Caption");
            if (label != null && label.TryGetComponent<TMP_Text>(out var text)) text.text = caption;
            if (root.TryGetComponent<TooltipTrigger>(out var trigger)) trigger.Text = tooltip;
        }

        public void Render(PlayerState s)
        {
            m_State = s;
            scenarioLabel.text = ContentText.ScenarioName(s.ScenarioId);
            nightLabel.text = Loc.Get("hud.night", "night", s.Night, "total", s.TotalNights);

            popLabel.text = s.Pop.ToString();
            cashLabel.text = s.Cash.ToString();
            if (m_LastPop != int.MinValue && s.Pop != m_LastPop) UITween.Punch(this, popLabel.transform, 0.25f);
            if (m_LastCash != int.MinValue && s.Cash != m_LastCash) UITween.Punch(this, cashLabel.transform, 0.25f);
            m_LastPop = s.Pop;
            m_LastCash = s.Cash;

            bool party = s.Phase == GamePhase.Party;
            capacityLabel.text = party ? $"{s.Occupancy}<size=70%>/{s.Capacity}</size>" : Loc.Get("hud.cap_only", "cap", s.Capacity);

            int trouble = party ? PartyEngine.NetTrouble(s) : 0;
            int diamonds = party ? PartyEngine.DiamondsPresent(s) : 0;

            for (int i = 0; i < troublePips.Length; i++)
            {
                bool lit = i < trouble;
                troublePips[i].color = lit ? (i >= GameRules.RaidTroubleThreshold - 1 ? UIPalette.Red : UIPalette.Orange) : UIPalette.Border;
            }
            troubleLabel.enableAutoSizing = true;
            troubleLabel.fontSizeMin = 14;
            string riskText = Loc.Get("hud.risk", "risk", trouble, "max", GameRules.RaidTroubleThreshold);
            troubleLabel.text = trouble >= GameRules.RaidTroubleThreshold - 1 && party ? $"<color=#FF5A5A>{riskText}</color>" : riskText;

            for (int i = 0; i < starPips.Length; i++)
                starPips[i].color = i < diamonds ? UIPalette.Gold : UIPalette.Border;
            starsLabel.text = Loc.Get("hud.diamonds", "count", diamonds, "max", GameRules.DiamondsToWin);

            if (m_LastTrouble >= 0 && trouble > m_LastTrouble) UITween.Punch(this, troubleLabel.transform, 0.3f);
            if (m_LastDiamonds >= 0 && diamonds > m_LastDiamonds) UITween.Punch(this, starsLabel.transform, 0.3f);
            m_LastTrouble = trouble;
            m_LastDiamonds = diamonds;
        }

        string TroubleTooltip()
        {
            if (m_State == null || m_State.Phase != GamePhase.Party)
                return Loc.Get("hud.tip.risk_idle", "max", GameRules.RaidTroubleThreshold);
            string text = Loc.Get("hud.tip.risk", ("added", PartyEngine.TroubleTotal(m_State) - m_State.PartyHeat), ("cancelled", PartyEngine.CancelCount(m_State) - m_State.PartyShield),
                ("net", PartyEngine.NetTrouble(m_State)), ("max", GameRules.RaidTroubleThreshold));
            if (m_State.PartyHeat > 0) text += "\n" + Loc.Get("hud.tip.risk_heat", "count", m_State.PartyHeat);
            if (m_State.PartyShield > 0) text += "\n" + Loc.Get("hud.tip.risk_shield", "count", m_State.PartyShield);
            return text;
        }

        string DiamondsTooltip()
        {
            if (m_State == null) return null;
            return Loc.Get("hud.tip.diamonds", ("now", m_State.Phase == GamePhase.Party ? PartyEngine.DiamondsPresent(m_State) : 0),
                ("owned", m_State.DiamondsOwned()), ("best", m_State.BestDiamonds), ("goal", GameRules.DiamondsToWin));
        }
    }
}
