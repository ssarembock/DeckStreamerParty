using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>The Audience: every viewer you own, with permanent upgrades and tonight's availability.</summary>
    public sealed class RolodexView : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text summaryLabel;
        [SerializeField] internal RectTransform grid;
        [SerializeField] internal GuestCardView cardPrefab;
        [SerializeField] internal Button closeButton;
        [Tooltip("Optional: the communities in this Audience (how many different viewers each has and what that can score).")]
        [SerializeField] internal TMP_Text communitiesLabel;

        const float CardScale = 0.62f;

        readonly List<GuestCardView> m_Cards = new List<GuestCardView>();

        public bool IsOpen => root.activeSelf;

        void Awake()
        {
            closeButton.onClick.AddListener(Close);
            root.SetActive(false);
        }

        /// <param name="owner">Whose Audience this is when watching another player; null for your own.</param>
        public void Toggle(PlayerState s, string owner = null)
        {
            if (IsOpen) Close();
            else Open(s, owner);
        }

        public void Open(PlayerState s, string owner = null)
        {
            bool wasOpen = IsOpen;
            Refresh(s, owner);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (!wasOpen) UITween.PopIn(this, root.transform, 0.2f, 0f, 0.9f);
        }

        public void Refresh(PlayerState s, string owner = null)
        {
            var guests = new List<GuestInstance>(s.Rolodex);
            guests.Sort((a, b) =>
            {
                if (a.BaseInfo.IsDiamond != b.BaseInfo.IsDiamond) return a.BaseInfo.IsDiamond ? -1 : 1;
                if (a.Temporary != b.Temporary) return a.Temporary ? 1 : -1;
                int c = string.CompareOrdinal(a.BaseInfo.Name, b.BaseInfo.Name);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });

            bool party = s.Phase == GamePhase.Party;
            // Teams (2026-11-18): each player's own viewer cards in their own column, their name on top.
            bool split = s.IsTeam && SetTeamLayout(true);
            if (!s.IsTeam) SetTeamLayout(false);
            if (split)
            {
                var left = guests.FindAll(g => g.Owner == 0);
                var right = guests.FindAll(g => g.Owner != 0);
                FillGrid(grid, m_Cards, left, s, party);
                FillGrid(m_Grid2, m_Cards2, right, s, party);
                for (int member = 0; member < 2; member++)
                {
                    string name = MemberName != null ? MemberName(member) : null;
                    if (string.IsNullOrEmpty(name)) name = Loc.Get("couch.player", "number", member + 1);
                    string owned = $"<size=75%><color=#53535F>{Loc.Get("audience.count", "count", s.PermanentCountOf(member))}</color></size>";
                    m_Heads[member].text = LightText.AdaptOn(m_Heads[member].transform, $"<b>{name}</b>  {owned}");
                }
            }
            else FillGrid(grid, m_Cards, guests, s, party);

            string count = $"<size=70%><color=#B9AED6>{Loc.Get("audience.count", "count", s.PermanentCount)}</color></size>";
            titleLabel.text = (owner == null ? Loc.Get("audience.title_you") : Loc.Get("audience.title_other", "player", owner)) + "  " + count;
            var summary = Loc.Get("audience.summary", "owned", $"<color=#FFC94A>{s.DiamondsOwned()}</color>", "best", $"<color=#FFC94A>{s.BestDiamonds}</color>");
            if (party) summary += "    " + Loc.Get("audience.summary_live", "queue", s.Pool.Count, "chat", s.House.Count);
            var banned = s.BannedThisParty >= 0 ? s.Find(s.BannedThisParty) : null;
            if (banned != null) summary += "\n" + Loc.Get("audience.banned_now", "viewer", GuestVisuals.NameTag(banned.Type));
            else if (s.BannedNextParty >= 0 && s.Find(s.BannedNextParty) != null) summary += "\n" + Loc.Get("audience.banned_next", "viewer", GuestVisuals.NameTag(s.Find(s.BannedNextParty).Type));
            // Teams: each player's own streamer.
            for (int member = 0; member < (s.IsTeam ? 2 : 1); member++)
            {
                var streamer = s.StreamerOf(member);
                if (streamer != StreamerType.None) summary += "\n" + Loc.Get("audience.streamer", "streamer", StreamerVisuals.NameTag(streamer), "power", StreamerVisuals.PowerName(streamer));
            }
            summaryLabel.text = summary + $"\n<color=#B9AED6>{Loc.Get("audience.hint")}</color>";
            if (communitiesLabel != null) communitiesLabel.text = CommunitiesText(s);
        }

        /// <summary>
        /// Every community someone in the Audience belongs to: its icon and name, how many different viewer cards you own
        /// from it, the best score they could make together in one chat (different viewers - 1, up to the cap) and the bonus at
        /// that score. Hover an icon for the full rule. A community needs two different viewers to score at all.
        /// </summary>
        static string CommunitiesText(PlayerState s)
        {
            var sb = new System.Text.StringBuilder();
            var seen = new HashSet<GuestType>();
            var types = new int[Communities.Count];
            foreach (var g in s.Rolodex)
            {
                if (g.Temporary || !seen.Add(g.Type)) continue;
                var mask = GuestCatalog.CommunitiesOf(g.Type);
                foreach (var community in Communities.All)
                    if (Communities.Has(mask, community)) types[(int)community]++;
            }

            foreach (var community in Communities.All)
            {
                int count = types[(int)community];
                if (count == 0) continue;
                int best = Communities.Capped(community, Communities.PointsFromMembers(community, count));
                sb.Append(GameIcons.Tag(community)).Append(" <b>").Append(GuestVisuals.Colored(CommunityVisuals.Name(community), CommunityVisuals.Color(community)))
                  .Append("</b>\n<size=85%>");
                sb.Append(Loc.Get("audience.community_types", "count", count));
                if (best > 0)
                    sb.Append("  ·  ").Append(Loc.Get("audience.community_best", "points", best, "effect", CommunityVisuals.ChipEffect(community, best)));
                else
                    sb.Append("  ·  <color=#53535F>").Append(Loc.Get("audience.community_needs", "count", Communities.MembersForPoint(community, 1))).Append("</color>");
                sb.Append("</size>\n\n");
            }
            if (sb.Length == 0) return $"<color=#53535F>{Loc.Get("audience.no_communities")}</color>";
            return sb.ToString().TrimEnd('\n');
        }

        public void Close() => root.SetActive(false);

        static void FillGrid(RectTransform target, List<GuestCardView> cards, List<GuestInstance> guests, PlayerState s, bool party)
        {
            GuestCardView.FitGrid(target, CardScale);
            for (int i = 0; i < guests.Count; i++)
            {
                if (i >= cards.Count) cards.Add(Instantiate(s_CardPrefab, target));
                var card = cards[i];
                var g = guests[i];
                card.gameObject.SetActive(true);
                card.ShowGuest(g, s, party && s.House.Contains(g.Id));
                card.SetClickable(false);
                card.SetScale(CardScale);

                bool unavailable = g.Id == s.BannedThisParty || (party && !s.Pool.Contains(g.Id) && !s.House.Contains(g.Id));
                card.SetHighlight(unavailable ? CardHighlight.Dimmed : CardHighlight.None);
            }
            for (int i = guests.Count; i < cards.Count; i++) cards[i].gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ teams: one column per player (2026-11-18)

        /// <summary>Teams: a member's name for their column (0 = left, 1 = right), set by the game screen.</summary>
        public Func<int, string> MemberName;

        const float HeadBand = 46f;
        const float ColumnGap = 28f;

        static GuestCardView s_CardPrefab;
        readonly List<GuestCardView> m_Cards2 = new List<GuestCardView>();
        readonly TMP_Text[] m_Heads = new TMP_Text[2];
        RectTransform m_Scroll;
        RectTransform m_Scroll2;
        RectTransform m_Grid2;
        Image m_Split;
        Vector2 m_ScrollMin, m_ScrollMax;
        bool m_Split2Made;
        bool m_TeamLayout;

        /// <summary>
        /// Lays the cards out in two columns (teams) or one. The second column is a copy of the scroll view, made the first
        /// time a team's Audience opens, so older scenes need no Rebuild. False when the scene's grid isn't in a scroll view.
        /// </summary>
        bool SetTeamLayout(bool team)
        {
            s_CardPrefab = cardPrefab;
            if (team == m_TeamLayout) return team && m_Split2Made;
            if (team && !EnsureTeamColumns()) return false;
            m_TeamLayout = team;
            if (m_Scroll == null) return false;

            if (!team)
            {
                m_Scroll.offsetMin = m_ScrollMin;
                m_Scroll.offsetMax = m_ScrollMax;
                if (m_Scroll2 != null) m_Scroll2.gameObject.SetActive(false);
                foreach (var head in m_Heads) if (head != null) head.gameObject.SetActive(false);
                if (m_Split != null) m_Split.gameObject.SetActive(false);
                return false;
            }

            var parent = (RectTransform)m_Scroll.parent;
            float width = parent.rect.width;
            float leftInset = m_ScrollMin.x;
            float rightInset = -m_ScrollMax.x;
            float column = Mathf.Max(100f, (width - leftInset - rightInset - ColumnGap) * 0.5f);
            float top = m_ScrollMax.y - HeadBand;

            m_Scroll.offsetMin = new Vector2(leftInset, m_ScrollMin.y);
            m_Scroll.offsetMax = new Vector2(-(rightInset + column + ColumnGap), top);
            m_Scroll2.offsetMin = new Vector2(leftInset + column + ColumnGap, m_ScrollMin.y);
            m_Scroll2.offsetMax = new Vector2(m_ScrollMax.x, top);
            m_Scroll2.gameObject.SetActive(true);

            PlaceHead(m_Heads[0], leftInset, -(rightInset + column + ColumnGap), top);
            PlaceHead(m_Heads[1], leftInset + column + ColumnGap, m_ScrollMax.x, top);

            float middle = leftInset + column + ColumnGap * 0.5f;
            var line = m_Split.rectTransform;
            line.anchorMin = new Vector2(0f, 0f);
            line.anchorMax = new Vector2(0f, 1f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.offsetMin = new Vector2(middle - 1.5f, m_ScrollMin.y);
            line.offsetMax = new Vector2(middle + 1.5f, m_ScrollMax.y);
            m_Split.gameObject.SetActive(true);
            return true;
        }

        /// <summary>A column's name band: across the column, from <paramref name="bottom"/> up (offsets from the top edge).</summary>
        static void PlaceHead(TMP_Text head, float left, float right, float bottom)
        {
            var rect = head.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left + 10f, bottom);
            rect.offsetMax = new Vector2(right - 10f, bottom + HeadBand);
            head.gameObject.SetActive(true);
        }

        bool EnsureTeamColumns()
        {
            if (m_Split2Made) return true;
            var scroll = grid != null ? grid.GetComponentInParent<ScrollRect>(true) : null;
            if (scroll == null) return false;
            m_Scroll = (RectTransform)scroll.transform;
            m_ScrollMin = m_Scroll.offsetMin;
            m_ScrollMax = m_Scroll.offsetMax;

            var copy = Instantiate(scroll.gameObject, m_Scroll.parent);
            copy.name = scroll.gameObject.name + "Right";
            m_Scroll2 = (RectTransform)copy.transform;
            m_Grid2 = copy.GetComponent<ScrollRect>().content;
            // The copy may hold copies of cards already shown: start it empty.
            if (m_Grid2 != null)
                for (int i = m_Grid2.childCount - 1; i >= 0; i--)
                {
                    var child = m_Grid2.GetChild(i).gameObject;
                    child.SetActive(false);
                    Destroy(child);
                }
            if (m_Grid2 == null) m_Grid2 = (RectTransform)copy.transform;

            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "LeftPlayer" : "RightPlayer", typeof(RectTransform));
                go.transform.SetParent(m_Scroll.parent, false);
                var head = go.AddComponent<TextMeshProUGUI>();
                head.font = titleLabel.font;
                head.fontSize = 26f;
                head.enableAutoSizing = true;
                head.fontSizeMin = 16f;
                head.fontSizeMax = 26f;
                head.alignment = TextAlignmentOptions.MidlineLeft;
                head.color = SiteColours.Get(SiteColourRole.Text, m_Scroll.parent);
                head.raycastTarget = false;
                head.richText = true;
                m_Heads[i] = head;
            }

            var split = new GameObject("PlayersDivider", typeof(RectTransform));
            split.transform.SetParent(m_Scroll.parent, false);
            m_Split = split.AddComponent<Image>();
            m_Split.color = SiteColours.Get(SiteColourRole.Divider, m_Scroll.parent);
            m_Split.raycastTarget = false;
            m_Split2Made = true;
            return true;
        }
    }
}
