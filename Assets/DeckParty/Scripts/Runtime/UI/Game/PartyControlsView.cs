using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The chat's typing area: the Add Viewer and End the Stream icon buttons (their words, hotkeys and blockers are in the
    /// hover tooltips), what ending now would bank (icons and numbers), how many risky viewers are still in the queue (an
    /// icon and a number), who's next when an effect reveals it (their faces), and the waiting-viewer panel.
    /// Older scenes with text buttons still work: every label is optional.
    /// </summary>
    public sealed class PartyControlsView : MonoBehaviour
    {
        [SerializeField] internal Button doorButton;
        [SerializeField] internal TMP_Text doorLabel;
        [SerializeField] internal TMP_Text doorSubLabel;
        [SerializeField] internal TooltipTrigger doorTooltip;
        [SerializeField] internal Button endButton;
        [SerializeField] internal Image endButtonImage;
        [SerializeField] internal TMP_Text endLabel;
        [Tooltip("What ending the stream now would bank (icons and numbers).")]
        [SerializeField] internal TMP_Text endSubLabel;
        [Tooltip("Risky viewers still in the queue (an icon and a number).")]
        [SerializeField] internal TMP_Text riskLabel;
        [Tooltip("Optional pill behind the risky-queue count (hidden when there are none). Scenes built since 2026-11-10 have none.")]
        [SerializeField] internal GameObject riskBadge;

        [Tooltip("The rounded rectangles under the two main buttons (UI Skin: Add Viewer base, End the Stream base). They fade " +
                 "with their button while it can't be pressed. Optional.")]
        [SerializeField] internal CanvasGroup doorBase;
        [SerializeField] internal CanvasGroup endBase;

        [Header("Who's next (X-Ray, Side Eye Chloe)")]
        [SerializeField] internal GameObject queueRoot;
        [SerializeField] internal Image[] queueBacks;
        [SerializeField] internal Image[] queueFaces;
        [SerializeField] internal TooltipTrigger[] queueTips;

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

        static readonly List<int> s_Next = new List<int>();

        /// <summary>How see-through a button's base gets while the button can't be pressed (like the button itself).</summary>
        const float DisabledBaseAlpha = 0.45f;

        Color m_EndBaseColor;
        bool m_WinReady;
        TooltipTrigger m_EndTooltip;
        PlayerState m_State;
        bool m_Targeting;
        bool m_Interactive = true;
        int m_Member;

        void Awake()
        {
            doorButton.onClick.AddListener(() => DoorClicked?.Invoke());
            endButton.onClick.AddListener(() => EndClicked?.Invoke());
            admitButton.onClick.AddListener(() => AdmitClicked?.Invoke());
            rejectButton.onClick.AddListener(() => RejectClicked?.Invoke());
            // Add Viewer is always green and End the Stream always red, on every platform (scenes built before this had
            // platform colours on them: this fixes those too until they're rebuilt).
            Paint(doorButton, SitePalette.Go);
            Paint(endButton, SitePalette.Stop);
            m_EndBaseColor = SitePalette.Stop;
            endButton.TryGetComponent(out m_EndTooltip);
            SetTip(admitButton, Loc.Get("controls.approve"));
            SetTip(rejectButton, Loc.Get("controls.deny"));
            Loc.Changed += OnLanguageChanged;
        }

        void OnDestroy() => Loc.Changed -= OnLanguageChanged;

        void OnLanguageChanged()
        {
            SetTip(admitButton, Loc.Get("controls.approve"));
            SetTip(rejectButton, Loc.Get("controls.deny"));
            if (m_State != null) Render(m_State, m_Targeting, m_Interactive, m_Member);
        }

        static void SetTip(Component target, string text)
        {
            if (target != null && target.TryGetComponent<TooltipTrigger>(out var trigger)) trigger.Text = text;
        }

        /// <summary>Gives a main button its fixed colour (and stops the site accent from recolouring it).</summary>
        static void Paint(Button button, Color color)
        {
            if (button == null || !button.TryGetComponent<Image>(out var image)) return;
            if (image.TryGetComponent<SiteAccent>(out var accent)) accent.enabled = false;
            image.color = color;
        }

        public bool WinReady => m_WinReady;

        void Update()
        {
            // Four Trophies in chat: the End button pulses (it stays red).
            if (m_WinReady && endButtonImage != null)
                endButtonImage.color = Color.Lerp(SitePalette.Stop, Color.Lerp(SitePalette.Stop, Color.white, 0.4f),
                    0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f));
        }

        /// <param name="interactive">False while watching someone else's party (or waiting for your turn): everything is shown, nothing is clickable.</param>
        /// <param name="member">Teams (2v2, co-op): the member these controls act for (their queue, Sub Cap and side of the stream).</param>
        public void Render(PlayerState s, bool targeting, bool interactive = true, int member = 0)
        {
            m_State = s;
            m_Targeting = targeting;
            m_Interactive = interactive;
            m_Member = member;
            bool peeking = s.PeekedId >= 0;
            var doorBlocker = PartyEngine.GetDoorBlocker(s, member);
            string doorBlockerText = doorBlocker != null ? Loc.Error(doorBlocker) : null;
            doorButton.interactable = doorBlocker == null && !targeting && interactive;
            if (doorLabel != null) doorLabel.text = Loc.Get("controls.add_viewer");
            string next = NextGuestPreview(s, member);
            int queued = s.PoolCountOf(member);
            if (doorSubLabel != null)
                doorSubLabel.text = doorBlockerText ?? (next ?? Loc.Get("controls.queue_count", "count", queued)) + (interactive ? "  <color=#B9AED6>[Space]</color>" : string.Empty);
            if (doorTooltip != null)
            {
                doorTooltip.Text = TipText.Titled(Loc.Get("controls.add_viewer"), TipText.Key("Space"))
                    .Line(Loc.Get("controls.add_viewer_tip"))
                    .Line(next)
                    .Detail(Loc.Get("controls.queue_count", "count", queued))
                    .Warn(doorBlockerText);
            }
            RenderQueue(s, member);

            var endBlocker = PartyEngine.GetEndPartyBlocker(s, member);
            endButton.interactable = endBlocker == null && !targeting && interactive;
            PartyEngine.ProjectScore(s, out int cash, out int influence);
            int penalty = PartyEngine.ProjectPenalty(s, influence);
            int diamonds = PartyEngine.DiamondsPresent(s);
            m_WinReady = diamonds >= GameRules.DiamondsToWin && endBlocker == null;
            if (endLabel != null) endLabel.text = Loc.Get(m_WinReady ? "controls.end_win" : "controls.end");
            var sub = $"{GuestVisuals.CashText(cash)}  {GuestVisuals.InfluenceText(influence)}";
            if (penalty > 0) sub += $"  <color=#FF5A5A>-{penalty}</color>";
            if (endSubLabel != null) endSubLabel.text = endLabel != null && interactive ? sub + "  <color=#B9AED6>[E]</color>" : sub;
            if (m_EndTooltip != null)
            {
                string payout = $"{GuestVisuals.CashText(cash)}  {GuestVisuals.InfluenceText(influence)}";
                if (penalty > 0) payout += "  " + GuestVisuals.Colored(Loc.Get("controls.fees", "count", penalty), UIPalette.Red);
                var tip = TipText.Titled(Loc.Get(m_WinReady ? "controls.end_win" : "controls.end"), TipText.Key("E"))
                    .Line(Loc.Get("controls.end_tip", "payout", payout));
                // Teams: the stream is scored once both of you have ended your side.
                if (s.IsTeam) tip.Detail(TeamStatus(s, member));
                m_EndTooltip.Text = tip.Warn(endBlocker != null ? Loc.Error(endBlocker) : null);
            }
            if (!m_WinReady && endButtonImage != null) endButtonImage.color = m_EndBaseColor;
            if (doorBase != null) doorBase.alpha = doorButton.interactable ? 1f : DisabledBaseAlpha;
            if (endBase != null) endBase.alpha = endButton.interactable ? 1f : DisabledBaseAlpha;

            RenderRisk(s);

            peekPanel.SetActive(peeking);
            if (peeking)
            {
                var g = s.Find(s.PeekedId);
                if (g != null) peekCard.ShowGuest(g, s, false);
                if (peekTitle != null) peekTitle.text = Loc.Get("controls.waiting_viewer");
                admitButton.interactable = (g == null || !s.SideFull(g.Owner)) && interactive;
                rejectButton.interactable = interactive;
            }
        }

        /// <summary>The faces of the viewers an effect has revealed as next in the queue (names on hover).</summary>
        void RenderQueue(PlayerState s, int member)
        {
            if (queueRoot == null || queueFaces == null) return;
            NextGuests(s, s_Next, member);
            queueRoot.SetActive(s_Next.Count > 0);
            for (int i = 0; i < queueFaces.Length; i++)
            {
                bool show = i < s_Next.Count;
                var back = queueBacks != null && i < queueBacks.Length ? queueBacks[i] : null;
                var host = back != null ? back.gameObject : queueFaces[i] != null ? queueFaces[i].gameObject : null;
                if (host != null) host.SetActive(show);
                if (!show) continue;
                var g = s.Find(s_Next[i]);
                if (g == null) continue;
                var art = GameDatabase.Instance.GetGuestArt(g.Kind);
                var sprite = art != null && art.avatar != null ? art.avatar : GuestVisuals.Portrait(g.Kind);
                if (queueFaces[i] != null)
                {
                    queueFaces[i].sprite = sprite;
                    queueFaces[i].enabled = sprite != null;
                    queueFaces[i].preserveAspect = true;
                }
                if (back != null) back.color = Color.Lerp(SiteColours.Get(SiteColourRole.SoftFill, back.transform), GuestVisuals.Tint(g.Kind), sprite != null ? 0.2f : 0.85f);
                var tip = queueTips != null && i < queueTips.Length ? queueTips[i] : null;
                if (tip != null)
                {
                    var kind = g.Kind;
                    tip.Provider = () => GuestVisuals.Tooltip(kind);
                }
            }
        }

        static void NextGuests(PlayerState s, List<int> into, int member)
        {
            into.Clear();
            var xray = PartyEngine.XRayQueue(s, member);
            if (xray.Count > 0)
            {
                into.AddRange(xray);
                return;
            }
            int first = s.NextInPool(member);
            if (first >= 0 && s.CountInHouse(GuestType.SideEyeChloe) > 0) into.Add(first);
        }

        /// <summary>Side Eye Chloe shows who's next in the queue; the Rude Radiologist's X-Ray shows the next three (tooltip text).</summary>
        static string NextGuestPreview(PlayerState s, int member)
        {
            var xray = PartyEngine.XRayQueue(s, member);
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
            if (s.NextInPool(member) < 0 || s.CountInHouse(GuestType.SideEyeChloe) == 0) return null;
            var next = s.Find(s.NextInPool(member));
            return next != null ? Loc.Get("controls.next_up", "viewer", GuestVisuals.NameTag(next.Kind)) : null;
        }

        /// <summary>Teams: whether you and your partner are still streaming or have ended your sides.</summary>
        static string TeamStatus(PlayerState s, int member)
        {
            int partner = member == 0 ? 1 : 0;
            return Loc.Get("controls.team_status",
                "you", Loc.Get(s.StreamDoneFor(member) ? "controls.side_ended" : "controls.side_live"),
                "partner", Loc.Get(s.StreamDoneFor(partner) ? "controls.side_ended" : "controls.side_live"));
        }

        /// <summary>Hides the risky-queue badge (it sits in the menu bar, so it would otherwise stay up in the shop).</summary>
        public void HideRisk()
        {
            if (riskBadge != null && riskBadge.activeSelf) riskBadge.SetActive(false);
        }

        /// <summary>
        /// How many viewers with Deplatform Risk are still in the queue: a fact, not the odds. Players work out their chances
        /// from what they can see. Red when one more Deplatform Risk would get the stream banned.
        /// </summary>
        void RenderRisk(PlayerState s)
        {
            int troubleWaiting = 0;
            for (int i = 0; i < s.Pool.Count; i++)
            {
                var g = s.Find(s.Pool[i]);
                if (PartyEngine.WouldArriveRisky(g)) troubleWaiting++;
            }
            bool oneAway = PartyEngine.NetTrouble(s) >= GameRules.RaidTroubleThreshold - 1;
            if (riskBadge != null)
            {
                // Website style: an icon and the number in a pill; the sentence is in the tooltip.
                riskBadge.SetActive(troubleWaiting > 0);
                if (riskLabel != null) riskLabel.text = $"{GameIcons.Tag(ResourceIcon.Risk)} " + (oneAway ? $"<color=#FF6B6B>{troubleWaiting}</color>" : troubleWaiting.ToString());
                if (riskBadge.TryGetComponent<TooltipTrigger>(out var tip)) tip.Text = Loc.Get("controls.risky_waiting", "count", troubleWaiting);
                return;
            }
            if (riskLabel == null) return;
            riskLabel.text = troubleWaiting == 0 ? string.Empty
                : GuestVisuals.Colored(Loc.Get("controls.risky_waiting", "count", troubleWaiting), oneAway ? UIPalette.Red : UIPalette.TextDim);
        }
    }
}
