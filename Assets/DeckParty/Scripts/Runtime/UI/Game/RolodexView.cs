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
            GuestCardView.FitGrid(grid, CardScale);
            for (int i = 0; i < guests.Count; i++)
            {
                if (i >= m_Cards.Count) m_Cards.Add(Instantiate(cardPrefab, grid));
                var card = m_Cards[i];
                var g = guests[i];
                card.gameObject.SetActive(true);
                card.ShowGuest(g, s, party && s.House.Contains(g.Id));
                card.SetClickable(false);
                card.SetScale(CardScale);

                bool unavailable = g.Id == s.BannedThisParty || (party && !s.Pool.Contains(g.Id) && !s.House.Contains(g.Id));
                card.SetHighlight(unavailable ? CardHighlight.Dimmed : CardHighlight.None);
            }
            for (int i = guests.Count; i < m_Cards.Count; i++) m_Cards[i].gameObject.SetActive(false);

            string count = $"<size=70%><color=#B9AED6>{Loc.Get("audience.count", "count", s.PermanentCount)}</color></size>";
            titleLabel.text = (owner == null ? Loc.Get("audience.title_you") : Loc.Get("audience.title_other", "player", owner)) + "  " + count;
            var summary = Loc.Get("audience.summary", "owned", $"<color=#FFC94A>{s.DiamondsOwned()}</color>", "best", $"<color=#FFC94A>{s.BestDiamonds}</color>");
            if (party) summary += "    " + Loc.Get("audience.summary_live", "queue", s.Pool.Count, "chat", s.House.Count);
            var banned = s.BannedThisParty >= 0 ? s.Find(s.BannedThisParty) : null;
            if (banned != null) summary += "\n" + Loc.Get("audience.banned_now", "viewer", GuestVisuals.NameTag(banned.Type));
            else if (s.BannedNextParty >= 0 && s.Find(s.BannedNextParty) != null) summary += "\n" + Loc.Get("audience.banned_next", "viewer", GuestVisuals.NameTag(s.Find(s.BannedNextParty).Type));
            if (s.Streamer != StreamerType.None) summary += "\n" + Loc.Get("audience.streamer", "streamer", StreamerVisuals.NameTag(s.Streamer), "power", StreamerVisuals.PowerName(s.Streamer));
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
                int best = Communities.Capped(community, count - Communities.MembersForFirstPoint + 1);
                sb.Append(GameIcons.Tag(community)).Append(" <b>").Append(GuestVisuals.Colored(CommunityVisuals.Name(community), CommunityVisuals.Color(community)))
                  .Append("</b>\n<size=85%>");
                sb.Append(Loc.Get("audience.community_types", "count", count));
                if (best > 0)
                    sb.Append("  ·  ").Append(Loc.Get("audience.community_best", "points", best, "effect", CommunityVisuals.ChipEffect(community, best)));
                else
                    sb.Append("  ·  <color=#53535F>").Append(Loc.Get("audience.community_needs", "count", Communities.MembersForFirstPoint)).Append("</color>");
                sb.Append("</size>\n\n");
            }
            if (sb.Length == 0) return $"<color=#53535F>{Loc.Get("audience.no_communities")}</color>";
            return sb.ToString().TrimEnd('\n');
        }

        public void Close() => root.SetActive(false);
    }
}
