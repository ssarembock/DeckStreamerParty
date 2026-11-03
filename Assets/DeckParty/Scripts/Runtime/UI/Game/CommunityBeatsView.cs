using System;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Community Beats: one chip per community during a stream (top-right of the chat bubble). Each shows the community's
    /// icon, its live score (different viewers in chat from that community) and its current bonus; hovering explains the
    /// community and how it scales. Only communities someone in the player's Audience belongs to are shown, so the row
    /// grows as they recruit. Chips light up and pop as their score rises. A red chip warns when rival Doomsdayers cursed
    /// this stream. The chat header and hint shrink to leave room for the row.
    /// </summary>
    public sealed class CommunityBeatsView : MonoBehaviour
    {
        [Serializable]
        internal sealed class Chip
        {
            public Community community;
            public RectTransform root;
            public CanvasGroup group;
            public Image background;
            public Image icon;
            public TMP_Text pointsLabel;
            public TMP_Text effectLabel;
            public TooltipTrigger tooltip;
            [Tooltip("The halo, rays and shader shine shown while this community's score is at its maximum.")]
            public CommunityMaxFx maxFx;
            [NonSerialized] public int last;
        }

        [SerializeField] internal Chip[] chips;
        [Tooltip("Shown while rival Doomsdayers cursed this stream (each viewer may score nothing).")]
        [SerializeField] internal GameObject cursed;
        [SerializeField] internal TMP_Text cursedLabel;
        [SerializeField] internal TooltipTrigger cursedTooltip;
        [Tooltip("Rects (the chat header and hint) whose right edge moves left to make room for the visible chips.")]
        [SerializeField] internal RectTransform[] avoid;
        [Tooltip("Space kept between the avoided text and the chips, plus the row's own margin from the bubble edge.")]
        [SerializeField] internal float avoidPadding = 48f;
        [Tooltip("Light (website) chip colours: the chips sit on a white panel.")]
        [SerializeField] internal bool siteStyle;

        PlayerState m_State;
        bool m_Hooked;
        float m_AvoidWidth = -1f;

        void Hook()
        {
            if (m_Hooked) return;
            m_Hooked = true;
            if (chips != null)
            {
                foreach (var chip in chips)
                {
                    if (chip == null) continue;
                    chip.last = -1;
                    var community = chip.community;
                    if (chip.tooltip != null) chip.tooltip.Provider = () => CommunityVisuals.Tooltip(community, m_State);
                }
            }
            if (cursedTooltip != null)
                cursedTooltip.Provider = () => m_State == null ? null : Loc.Get("community.cursed_tip", "value", Communities.DoomChance(m_State.PartyDoom));
        }

        public void Render(PlayerState s)
        {
            Hook();
            m_State = s;
            bool live = s != null && s.Phase == GamePhase.Party;
            var scores = live ? PartyEngine.CommunityScores(s) : new int[Communities.Count];
            var relevant = RelevantCommunities(s);
            float width = 0f;
            int visible = 0;

            if (chips != null)
            {
                foreach (var chip in chips)
                {
                    if (chip == null || chip.root == null) continue;
                    int points = scores[(int)chip.community];
                    bool show = points > 0 || Communities.Has(relevant, chip.community);
                    if (chip.root.gameObject.activeSelf != show) chip.root.gameObject.SetActive(show);
                    if (!show)
                    {
                        chip.last = -1;
                        if (chip.maxFx != null) chip.maxFx.SetMaxed(false, Color.white);
                        continue;
                    }
                    width += ChipWidth(chip.root);
                    visible++;
                    int max = Communities.MaxPoints(chip.community);

                    if (chip.icon != null)
                    {
                        chip.icon.sprite = GameIcons.Sprite(chip.community);
                        chip.icon.preserveAspect = true;
                    }
                    if (chip.background != null)
                        chip.background.color = points > 0
                            ? CommunityVisuals.Color(chip.community).WithAlpha(siteStyle ? 0.28f : 0.35f)
                            : siteStyle ? SitePalette.SurfaceAlt : UIPalette.PanelDeep.WithAlpha(0.7f);
                    string dim = siteStyle ? "#9A9AA6" : "#B9AED6";
                    if (chip.pointsLabel != null)
                        chip.pointsLabel.text = max > 0 ? $"{points}<size=60%><color={dim}>/{max}</color></size>" : points.ToString();
                    if (chip.effectLabel != null)
                    {
                        chip.effectLabel.text = CommunityVisuals.ChipEffect(chip.community, points);
                        chip.effectLabel.color = points > 0 ? (siteStyle ? SitePalette.Text : UIPalette.Text) : siteStyle ? SitePalette.TextFaint : UIPalette.TextDim;
                    }
                    if (chip.group != null) chip.group.alpha = points > 0 ? 1f : siteStyle ? 0.6f : 0.5f;

                    // Every point bestows a bigger bonus: make the rise felt.
                    if (chip.last >= 0 && points > chip.last && isActiveAndEnabled)
                    {
                        UITween.Punch(this, chip.root, 0.22f, 0.35f);
                        AudioService.PlayPitched(Sfx.Coin, 1.1f + 0.05f * points, 0.5f);
                    }
                    chip.last = live ? points : -1;

                    // At the maximum (5/5, 9/9...) the icon shines (CommunityMaxFx: halo, rays, shader glow).
                    if (chip.maxFx != null) chip.maxFx.SetMaxed(live && max > 0 && points >= max, CommunityVisuals.Color(chip.community));
                }
            }

            bool cursedNow = live && s.PartyDoom > 0;
            if (cursed != null)
            {
                cursed.SetActive(cursedNow);
                if (cursedNow)
                {
                    width += ChipWidth((RectTransform)cursed.transform);
                    visible++;
                }
            }
            if (cursedNow && cursedLabel != null) cursedLabel.text = $"{GameIcons.Tag(Community.Doomsdayers)} {Communities.DoomChance(s.PartyDoom)}%";

            var layout = GetComponent<HorizontalLayoutGroup>();
            if (visible > 1 && layout != null) width += layout.spacing * (visible - 1);
            MakeRoom(width);
        }

        /// <summary>Where a community's chip is on screen (for flying rewards), or null while it's hidden.</summary>
        public Vector3? ChipPosition(Community community)
        {
            if (chips == null) return null;
            foreach (var chip in chips)
                if (chip != null && chip.community == community && chip.root != null && chip.root.gameObject.activeInHierarchy) return chip.root.position;
            return null;
        }

        /// <summary>Communities anyone in the player's Audience (or in chat, disguised) belongs to.</summary>
        static CommunityMask RelevantCommunities(PlayerState s)
        {
            var mask = CommunityMask.None;
            if (s == null) return mask;
            foreach (var g in s.Rolodex)
                if (g != null) mask |= GuestCatalog.CommunitiesOf(g.Type);
            foreach (var id in s.House)
            {
                var g = s.Find(id);
                if (g != null) mask |= GuestCatalog.CommunitiesOf(g.Kind);
            }
            return mask;
        }

        static float ChipWidth(RectTransform rect)
        {
            var element = rect.GetComponent<LayoutElement>();
            return element != null && element.preferredWidth > 0f ? element.preferredWidth : rect.rect.width;
        }

        /// <summary>Pulls the chat header and hint in from the right so they never run under the chips.</summary>
        void MakeRoom(float width)
        {
            if (avoid == null || Mathf.Approximately(width, m_AvoidWidth)) return;
            m_AvoidWidth = width;
            float inset = width > 0f ? avoidPadding + width : 30f;
            foreach (var rect in avoid)
            {
                if (rect == null) continue;
                var max = rect.offsetMax;
                max.x = -inset;
                rect.offsetMax = max;
            }
        }
    }
}
