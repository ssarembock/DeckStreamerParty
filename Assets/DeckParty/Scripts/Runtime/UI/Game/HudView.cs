using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The site's menu bar (top of the stream and shop): night, Clout, Cash, Sub Cap, the Deplatform Risk pips and the Plays
    /// pips, then the Audience and Menu icon buttons. Everything is an icon and a number; hovering any of it says what it is
    /// (the night's tooltip also names the character set).
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] internal TMP_Text scenarioLabel;
        [SerializeField] internal TMP_Text nightLabel;
        [SerializeField] internal TMP_Text popLabel;
        [SerializeField] internal TMP_Text cashLabel;
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
        [SerializeField] internal Button menuButton;

        [Header("Website bar (optional)")]
        [SerializeField] internal Image popIcon;
        [SerializeField] internal Image cashIcon;
        [SerializeField] internal Image nightIcon;
        [SerializeField] internal Image capacityIcon;
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
            SetCaption("Pop", null, Loc.Get("hud.tip.gifted_subs"));
            SetCaption("Cash", null, Loc.Get("hud.tip.cash"));
            SetCaption("House", null, Loc.Get("hud.tip.sub_cap"));
            // Icon buttons say what they do on hover; text buttons (older scenes) keep their caption.
            SetButton(rolodexButton, Loc.Get("hud.audience_button"));
            SetButton(menuButton, Loc.Get("hud.menu") + "  [Esc]");
            if (popIcon != null) popIcon.color = GameIcons.Tint(ResourceIcon.Subs);
            if (cashIcon != null) cashIcon.color = GameIcons.Tint(ResourceIcon.Cash);
            // Your own art keeps its colours; the white placeholder shapes take the site's grey.
            if (nightIcon != null) nightIcon.color = GameIcons.SlotTint(SkinSlot.IconNight, SitePalette.Text);
            if (capacityIcon != null) capacityIcon.color = GameIcons.SlotTint(SkinSlot.IconDoor, SitePalette.Text);
        }

        static void SetButton(Button button, string text)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && button.GetComponent<TooltipTrigger>() == null) label.text = text;
            else if (button.TryGetComponent<TooltipTrigger>(out var trigger)) trigger.Text = text;
        }

        /// <param name="caption">Null hides the caption (resources show only their icon and number).</param>
        void SetCaption(string block, string caption, string tooltip)
        {
            var root = transform.Find(block);
            if (root == null) root = FindDeep(transform, block);
            if (root == null) return;
            var label = root.Find("Texts/Caption");
            if (label != null)
            {
                label.gameObject.SetActive(caption != null);
                if (caption != null && label.TryGetComponent<TMP_Text>(out var text)) text.text = caption;
            }
            if (root.TryGetComponent<TooltipTrigger>(out var trigger)) trigger.Text = tooltip;
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

        public void Render(PlayerState s)
        {
            m_State = s;
            if (scenarioLabel != null) scenarioLabel.text = ContentText.ScenarioName(s.ScenarioId);
            if (nightLabel != null)
                nightLabel.text = nightTooltip != null ? $"{s.Night}<size=70%><color=#9A9AA6>/{s.TotalNights}</color></size>" : Loc.Get("hud.night", "night", s.Night, "total", s.TotalNights);

            popLabel.text = s.Pop.ToString();
            cashLabel.text = s.Cash.ToString();
            if (m_LastPop != int.MinValue && s.Pop != m_LastPop) UITween.Punch(this, popLabel.transform, 0.25f);
            if (m_LastCash != int.MinValue && s.Cash != m_LastCash) UITween.Punch(this, cashLabel.transform, 0.25f);
            m_LastPop = s.Pop;
            m_LastCash = s.Cash;

            bool party = s.Phase == GamePhase.Party;
            capacityLabel.text = party ? $"{s.Occupancy}<size=70%><color=#9A9AA6>/{s.Capacity}</color></size>" : s.Capacity.ToString();

            int trouble = party ? PartyEngine.NetTrouble(s) : 0;
            int diamonds = party ? PartyEngine.DiamondsPresent(s) : 0;

            for (int i = 0; i < troublePips.Length; i++)
            {
                bool lit = i < trouble;
                var hot = i >= GameRules.RaidTroubleThreshold - 1 ? SitePalette.Danger : SitePalette.Warning;
                troublePips[i].color = GameIcons.PipColor(ResourceIcon.Risk, lit, hot, SitePalette.Off);
            }
            if (troubleLabel != null)
            {
                troubleLabel.enableAutoSizing = true;
                troubleLabel.fontSizeMin = 14;
                string riskText = Loc.Get("hud.risk", "risk", trouble, "max", GameRules.RaidTroubleThreshold);
                troubleLabel.text = trouble >= GameRules.RaidTroubleThreshold - 1 && party ? $"<color=#FF5A5A>{riskText}</color>" : riskText;
            }

            for (int i = 0; i < starPips.Length; i++)
                starPips[i].color = GameIcons.PipColor(ResourceIcon.Plays, i < diamonds, SitePalette.Gold, SitePalette.Off);
            if (starsLabel != null) starsLabel.text = Loc.Get("hud.diamonds", "count", diamonds, "max", GameRules.DiamondsToWin);

            if (m_LastTrouble >= 0 && trouble > m_LastTrouble) UITween.Punch(this, RiskAnchor, 0.3f);
            if (m_LastDiamonds >= 0 && diamonds > m_LastDiamonds) UITween.Punch(this, PlaysAnchor, 0.3f);
            m_LastTrouble = trouble;
            m_LastDiamonds = diamonds;
        }

        string NightTooltip()
        {
            if (m_State == null) return null;
            return Loc.Get("hud.night", "night", m_State.Night, "total", m_State.TotalNights) + "\n" +
                   GuestVisuals.Colored(ContentText.ScenarioName(m_State.ScenarioId), UIPalette.TextDim);
        }

        string TroubleTooltip()
        {
            if (m_State == null || m_State.Phase != GamePhase.Party)
                return Loc.Get("hud.tip.risk_idle", "max", GameRules.RaidTroubleThreshold);
            string text = Loc.Get("hud.tip.risk", ("added", PartyEngine.TroubleTotal(m_State) - m_State.PartyHeat), ("cancelled", PartyEngine.CancelCount(m_State) - m_State.PartyShield),
                ("net", PartyEngine.NetTrouble(m_State)), ("max", GameRules.RaidTroubleThreshold));
            if (m_State.PartyHeat > 0) text += "\n" + Loc.Get("hud.tip.risk_heat", "count", m_State.PartyHeat);
            int fanbase = PartyEngine.FanbaseCancels(m_State);
            if (fanbase > 0) text += "\n" + Loc.Get("hud.tip.risk_fanbase", "count", fanbase);
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
