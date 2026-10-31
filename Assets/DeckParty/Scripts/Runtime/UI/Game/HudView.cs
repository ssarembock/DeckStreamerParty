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
        }

        /// <summary>Static captions are set here too, so scenes generated before a rename still show the new terms.</summary>
        void ApplyCaptions()
        {
            SetCaption("Pop", Terms.GiftedSubs.ToUpperInvariant(), $"{Terms.GiftedSubs}: spend them to recruit new guests.");
            SetCaption("Cash", Terms.Cash.ToUpperInvariant(), $"{Terms.Cash}: spend it to raise your {Terms.SubCap}.");
            SetCaption("House", Terms.SubCap.ToUpperInvariant(), $"Spaces in use / your {Terms.SubCap}.");
            var squadLabel = rolodexButton != null ? rolodexButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (squadLabel != null) squadLabel.text = $"{Terms.Squad} [R]";
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
            var scenario = ScenarioCatalog.Get(s.ScenarioId);
            scenarioLabel.text = scenario.Name;
            nightLabel.text = $"Night <b>{s.Night}</b><size=70%>/{s.TotalNights}</size>";

            popLabel.text = s.Pop.ToString();
            cashLabel.text = s.Cash.ToString();
            if (m_LastPop != int.MinValue && s.Pop != m_LastPop) UITween.Punch(this, popLabel.transform, 0.25f);
            if (m_LastCash != int.MinValue && s.Cash != m_LastCash) UITween.Punch(this, cashLabel.transform, 0.25f);
            m_LastPop = s.Pop;
            m_LastCash = s.Cash;

            bool party = s.Phase == GamePhase.Party;
            capacityLabel.text = party ? $"{s.Occupancy}<size=70%>/{s.Capacity}</size>" : $"{s.Capacity}<size=70%> cap</size>";

            int trouble = party ? PartyEngine.NetTrouble(s) : 0;
            int diamonds = party ? PartyEngine.DiamondsPresent(s) : 0;

            for (int i = 0; i < troublePips.Length; i++)
            {
                bool lit = i < trouble;
                troublePips[i].color = lit ? (i >= GameRules.RaidTroubleThreshold - 1 ? UIPalette.Red : UIPalette.Orange) : UIPalette.Border;
            }
            troubleLabel.enableAutoSizing = true;
            troubleLabel.fontSizeMin = 14;
            troubleLabel.text = trouble >= GameRules.RaidTroubleThreshold - 1 && party
                ? $"<color=#FF5A5A>{Terms.Risk} {trouble}/{GameRules.RaidTroubleThreshold}</color>"
                : $"{Terms.Risk} {trouble}/{GameRules.RaidTroubleThreshold}";

            for (int i = 0; i < starPips.Length; i++)
                starPips[i].color = i < diamonds ? UIPalette.Gold : UIPalette.Border;
            starsLabel.text = $"{Terms.Diamonds} {diamonds}/{GameRules.DiamondsToWin}";

            if (m_LastTrouble >= 0 && trouble > m_LastTrouble) UITween.Punch(this, troubleLabel.transform, 0.3f);
            if (m_LastDiamonds >= 0 && diamonds > m_LastDiamonds) UITween.Punch(this, starsLabel.transform, 0.3f);
            m_LastTrouble = trouble;
            m_LastDiamonds = diamonds;
        }

        string TroubleTooltip()
        {
            if (m_State == null || m_State.Phase != GamePhase.Party)
                return $"<b>{Terms.Risk}</b>\n{GameRules.RaidTroubleThreshold} net {Terms.Risk} at a party and {Terms.RaidTeam} shows up.";
            return $"<b>{Terms.Risk}</b>\nAdded by guests: {PartyEngine.TroubleTotal(m_State)}\nCancelled: -{PartyEngine.CancelCount(m_State)}\n" +
                   $"Net: {PartyEngine.NetTrouble(m_State)} (SWAT team at {GameRules.RaidTroubleThreshold})";
        }

        string DiamondsTooltip()
        {
            if (m_State == null) return null;
            return $"<b>{Terms.Diamonds}</b>\nAt this party: {(m_State.Phase == GamePhase.Party ? PartyEngine.DiamondsPresent(m_State) : 0)}\n" +
                   $"In your {Terms.Squad}: {m_State.DiamondsOwned()}\nBest ever at one party: {m_State.BestDiamonds}\n" +
                   $"Get {GameRules.DiamondsToWin} at one party to win!";
        }
    }
}
