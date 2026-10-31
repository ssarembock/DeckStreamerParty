using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Door and End Party buttons, score projection, risk hints and the peek decision panel.</summary>
    public sealed class PartyControlsView : MonoBehaviour
    {
        [SerializeField] internal Button doorButton;
        [SerializeField] internal TMP_Text doorLabel;
        [SerializeField] internal TMP_Text doorSubLabel;
        [SerializeField] internal TooltipTrigger doorTooltip;
        [SerializeField] internal Button endButton;
        [SerializeField] internal Image endButtonImage;
        [SerializeField] internal TMP_Text endLabel;
        [SerializeField] internal TMP_Text endSubLabel;
        [SerializeField] internal TMP_Text riskLabel;

        [Header("Peek")]
        [SerializeField] internal GameObject peekPanel;
        [SerializeField] internal TMP_Text peekTitle;
        [SerializeField] internal GuestCardView peekCard;
        [SerializeField] internal Button admitButton;
        [SerializeField] internal Button rejectButton;

        public event Action DoorClicked;
        public event Action EndClicked;
        public event Action AdmitClicked;
        public event Action RejectClicked;

        Color m_EndBaseColor;
        bool m_WinReady;

        void Awake()
        {
            doorButton.onClick.AddListener(() => DoorClicked?.Invoke());
            endButton.onClick.AddListener(() => EndClicked?.Invoke());
            admitButton.onClick.AddListener(() => AdmitClicked?.Invoke());
            rejectButton.onClick.AddListener(() => RejectClicked?.Invoke());
            if (endButtonImage != null) m_EndBaseColor = endButtonImage.color;
            if (endButton.TryGetComponent<TooltipTrigger>(out var endTip))
                endTip.Text = $"Bank the {Terms.Subs} and {Terms.Cash} of everyone inside as {Terms.GiftedSubs} and $.";
        }

        void Update()
        {
            if (m_WinReady && endButtonImage != null)
                endButtonImage.color = Color.Lerp(UIPalette.Gold, UIPalette.Pink, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
        }

        /// <param name="interactive">False while watching someone else's party (or waiting for your turn): everything is shown, nothing is clickable.</param>
        public void Render(PlayerState s, bool targeting, bool interactive = true)
        {
            bool peeking = s.PeekedId >= 0;
            var doorBlocker = PartyEngine.GetDoorBlocker(s);
            doorButton.interactable = doorBlocker == null && !targeting && interactive;
            doorLabel.text = "Open the Door";
            var next = NextGuestPreview(s);
            doorSubLabel.text = doorBlocker ?? (next ?? $"{s.Pool.Count} guest{(s.Pool.Count == 1 ? "" : "s")} waiting") + (interactive ? "  <color=#B9AED6>[Space]</color>" : string.Empty);
            if (doorTooltip != null) doorTooltip.Text = doorBlocker ?? "Let the next waiting guest in.";

            var endBlocker = PartyEngine.GetEndPartyBlocker(s);
            endButton.interactable = endBlocker == null && !targeting && interactive;
            PartyEngine.ProjectScore(s, out int pop, out int cash);
            int penalty = PartyEngine.ProjectPenalty(s, cash);
            int diamonds = PartyEngine.DiamondsPresent(s);
            m_WinReady = diamonds >= GameRules.DiamondsToWin && endBlocker == null;
            endLabel.text = m_WinReady ? "End Party & WIN!" : "End Party";
            var sub = $"{GuestVisuals.PopText(pop)}  {GuestVisuals.CashText(cash)}";
            if (penalty > 0) sub += $"  <color=#FF5A5A>(-{penalty} fees)</color>";
            endSubLabel.text = sub + (interactive ? "  <color=#B9AED6>[E]</color>" : string.Empty);
            if (!m_WinReady && endButtonImage != null) endButtonImage.color = m_EndBaseColor;

            riskLabel.text = RiskText(s);

            peekPanel.SetActive(peeking);
            if (peeking)
            {
                var g = s.Find(s.PeekedId);
                if (g != null) peekCard.ShowGuest(g, s, false);
                peekTitle.text = "Waiting at the door...";
                admitButton.interactable = !s.HouseFull && interactive;
                rejectButton.interactable = interactive;
            }
        }

        /// <summary>Side Eye Chloe lets you see who's next in line.</summary>
        static string NextGuestPreview(PlayerState s)
        {
            if (s.Pool.Count == 0 || s.CountInHouse(GuestType.SideEyeChloe) == 0) return null;
            var next = s.Find(s.Pool[0]);
            return next != null ? $"Next up: {GuestVisuals.NameTag(next.Kind)}" : null;
        }

        static string RiskText(PlayerState s)
        {
            if (s.Pool.Count == 0) return string.Empty;

            int trouble = PartyEngine.NetTrouble(s);
            int troubleWaiting = 0;
            int crowdersWaiting = 0;
            for (int i = 0; i < s.Pool.Count; i++)
            {
                var g = s.Find(s.Pool[i]);
                if (g == null) continue;
                if (g.Info.Trouble) troubleWaiting++;
                if (g.Info.Size + g.Info.Brings > s.EmptySpaces) crowdersWaiting++;
            }

            var text = string.Empty;
            if (trouble >= GameRules.RaidTroubleThreshold - 1 && troubleWaiting > 0)
                text = $"<color=#FF5A5A>SWAT chance: {Mathf.RoundToInt(100f * troubleWaiting / s.Pool.Count)}%</color>";
            else if (troubleWaiting > 0)
                text = $"<color=#B9AED6>{troubleWaiting} guest{(troubleWaiting == 1 ? "" : "s")} with {Terms.Risk} waiting</color>";

            if (s.EmptySpaces > 0 && crowdersWaiting > 0)
            {
                if (text.Length > 0) text += "   ";
                text += $"<color=#FF9A3D>Over-cap chance: {Mathf.RoundToInt(100f * crowdersWaiting / s.Pool.Count)}%</color>";
            }
            return text;
        }
    }
}
