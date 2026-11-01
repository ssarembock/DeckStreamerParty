using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>"Add Viewer to Chat" and "End the Stream" buttons, score projection, the risky-viewers count and the waiting-viewer panel.</summary>
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
            if (endButton.TryGetComponent<TooltipTrigger>(out m_EndTooltip)) m_EndTooltip.Text = Loc.Get("controls.end_tip");
            Loc.Changed += OnLanguageChanged;
        }

        void OnDestroy() => Loc.Changed -= OnLanguageChanged;

        TooltipTrigger m_EndTooltip;
        PlayerState m_State;
        bool m_Targeting;
        bool m_Interactive = true;

        void OnLanguageChanged()
        {
            if (m_EndTooltip != null) m_EndTooltip.Text = Loc.Get("controls.end_tip");
            if (m_State != null) Render(m_State, m_Targeting, m_Interactive);
        }

        public bool WinReady => m_WinReady;

        void Update()
        {
            if (m_WinReady && endButtonImage != null)
                endButtonImage.color = Color.Lerp(UIPalette.Gold, UIPalette.Pink, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
        }

        /// <param name="interactive">False while watching someone else's party (or waiting for your turn): everything is shown, nothing is clickable.</param>
        public void Render(PlayerState s, bool targeting, bool interactive = true)
        {
            m_State = s;
            m_Targeting = targeting;
            m_Interactive = interactive;
            bool peeking = s.PeekedId >= 0;
            var doorBlocker = PartyEngine.GetDoorBlocker(s);
            string doorBlockerText = doorBlocker != null ? Loc.Error(doorBlocker) : null;
            doorButton.interactable = doorBlocker == null && !targeting && interactive;
            doorLabel.text = Loc.Get("controls.add_viewer");
            var next = NextGuestPreview(s);
            doorSubLabel.text = doorBlockerText ?? (next ?? Loc.Get("controls.queue_count", "count", s.Pool.Count)) + (interactive ? "  <color=#B9AED6>[Space]</color>" : string.Empty);
            if (doorTooltip != null) doorTooltip.Text = doorBlockerText ?? Loc.Get("controls.add_viewer_tip");

            var endBlocker = PartyEngine.GetEndPartyBlocker(s);
            endButton.interactable = endBlocker == null && !targeting && interactive;
            PartyEngine.ProjectScore(s, out int pop, out int cash);
            int penalty = PartyEngine.ProjectPenalty(s, cash);
            int diamonds = PartyEngine.DiamondsPresent(s);
            m_WinReady = diamonds >= GameRules.DiamondsToWin && endBlocker == null;
            endLabel.text = Loc.Get(m_WinReady ? "controls.end_win" : "controls.end");
            var sub = $"{GuestVisuals.PopText(pop)}  {GuestVisuals.CashText(cash)}";
            if (penalty > 0) sub += $"  <color=#FF5A5A>{Loc.Get("controls.fees", "count", penalty)}</color>";
            endSubLabel.text = sub + (interactive ? "  <color=#B9AED6>[E]</color>" : string.Empty);
            if (!m_WinReady && endButtonImage != null) endButtonImage.color = m_EndBaseColor;

            riskLabel.text = RiskText(s);

            peekPanel.SetActive(peeking);
            if (peeking)
            {
                var g = s.Find(s.PeekedId);
                if (g != null) peekCard.ShowGuest(g, s, false);
                peekTitle.text = Loc.Get("controls.waiting_viewer");
                admitButton.interactable = !s.HouseFull && interactive;
                rejectButton.interactable = interactive;
            }
        }

        /// <summary>Side Eye Chloe shows who's next in the queue; the Rude Radiologist's X-Ray shows the next three.</summary>
        static string NextGuestPreview(PlayerState s)
        {
            var xray = PartyEngine.XRayQueue(s);
            if (xray.Count > 0)
            {
                var names = new System.Text.StringBuilder();
                foreach (var id in xray)
                {
                    var g = s.Find(id);
                    if (g == null) continue;
                    if (names.Length > 0) names.Append(", ");
                    names.Append(GuestVisuals.NameTag(g.Kind));
                }
                return Loc.Get("controls.xray", "viewers", names.ToString());
            }
            if (s.Pool.Count == 0 || s.CountInHouse(GuestType.SideEyeChloe) == 0) return null;
            var next = s.Find(s.Pool[0]);
            return next != null ? Loc.Get("controls.next_up", "viewer", GuestVisuals.NameTag(next.Kind)) : null;
        }

        /// <summary>
        /// How many viewers with Swatting Risk are still in the queue: a fact, not the odds. Players work out their chances
        /// from what they can see. Red when one more Swatting Risk would get the stream swatted.
        /// </summary>
        static string RiskText(PlayerState s)
        {
            int troubleWaiting = 0;
            for (int i = 0; i < s.Pool.Count; i++)
            {
                var g = s.Find(s.Pool[i]);
                if (g != null && g.Info.Trouble) troubleWaiting++;
            }
            if (troubleWaiting == 0) return string.Empty;

            bool oneAway = PartyEngine.NetTrouble(s) >= GameRules.RaidTroubleThreshold - 1;
            return GuestVisuals.Colored(Loc.Get("controls.risky_waiting", "count", troubleWaiting), oneAway ? UIPalette.Red : UIPalette.TextDim);
        }
    }
}
