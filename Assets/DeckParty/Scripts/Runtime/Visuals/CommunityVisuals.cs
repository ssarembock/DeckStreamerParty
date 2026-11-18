using System.Text;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Presentation for viewer communities: names, what they do and how they scale (keys "community.&lt;Community&gt;.*"),
    /// tooltips and the short effect shown on the Community Beats chips.
    /// </summary>
    public static class CommunityVisuals
    {
        /// <summary>The stream on screen, so icon tooltips can show live scores (set by the game screen; null elsewhere).</summary>
        public static PlayerState LiveState;

        public static string Name(Community community) => Loc.Get($"community.{community}.name");

        /// <summary>What the community does and how it scales, with the numbers from <see cref="Communities"/>.</summary>
        public static string Description(Community community)
        {
            int max = Communities.MaxPoints(community);
            int per;
            switch (community)
            {
                case Community.CoreFanbase: per = Communities.CoreFanbasePercentPerPoint; break;
                case Community.Staff: per = Communities.StaffInfluencePerPoint; break;
                case Community.Trolls: per = Communities.TrollArmyCashPerRisk; break;
                case Community.Content: per = Communities.ContentPercentPerPoint; break;
                case Community.HypeMen: per = Communities.HypeMenSeatsPerPoint; break;
                case Community.Lurkers: per = Communities.LurkersCashPerSpace; break;
                case Community.Millennial: per = Communities.InheritanceCashPerPoint; break;
                // Retired.
                case Community.FinanceBros: per = Communities.FinancePercentPerPoint; break;
                case Community.Doomsdayers: per = Communities.DoomPercentPerPoint; break;
                case Community.ArmchairActivists: per = Communities.ArmchairPercentPerPoint; break;
                case Community.Clout: per = Communities.CloutPercentPerPoint; break;
                case Community.MemeticsDivision: per = Communities.MemeticsPercentPerPoint; break;
                case Community.Bots: per = Communities.BotsPercentPerPoint; break;
                case Community.Zoomer: per = Communities.ZoomerPercentPerPoint; break;
                default: per = 1; break;
            }
            return Loc.Get($"community.{community}.desc",
                ("every", Communities.CoreFanbasePointsPerCancel),
                ("per", per),
                ("max", max),
                // Different viewers in chat for the top bonus (the descriptions count viewers, as the chips do).
                ("maxv", MembersForMax(community)),
                ("first", Communities.MembersForFirstPoint),
                ("top", Communities.Effect(community, max)),
                ("retire", Communities.BoomerRetirePercent),
                // Boomers (2026-11-18): Early Retirement's chance to retire the Boomer at 1 point. Millennials: the inheritance with no points.
                ("r1", Communities.BoomerRetireChance(1)),
                ("base", Communities.InheritanceCash),
                ("t1", Communities.TechspertiseChance(1)),
                ("t2", Communities.TechspertiseChance(2)),
                ("t3", Communities.TechspertiseChance(3)),
                ("cash", community == Community.Zoomer ? Communities.ZoomerCash : Communities.CloutCash),
                ("influence", Communities.ZoomerInfluenceCost),
                // Boomers: different Boomers for level 1 and level 2. Gatekeepers: viewers sent back at 0 and 1 points.
                ("l1", Communities.MembersForPoint(community, 1)),
                ("l2", Communities.MembersForPoint(community, 2)),
                ("g0", Communities.GatekeeperSendBack(0)),
                ("g1", Communities.GatekeeperSendBack(1)));
        }

        public static Color Color(Community community) => GameIcons.CommunityColor(community);

        /// <summary>What the community does and how it scales, plus its score and current effect during a stream.</summary>
        public static string Tooltip(Community community, PlayerState state = null)
        {
            // During a stream the title shows how many different viewers from it are in chat (out of what its top bonus
            // needs), and the body ends with what it gives right now.
            bool live = state != null && state.Phase == GamePhase.Party;
            int members = live ? PartyEngine.CommunityMembers(state)[(int)community] : 0;
            int points = Communities.PointsFromMembers(community, members);
            var tip = TipText.Titled(GameIcons.Tag(community) + " " + Name(community), live ? MembersText(community, members) : null);
            tip.Line(Description(community));
            if (!live) return tip;

            string now = null;
            // Gatekeepers act from 0 points (3 viewers); the others need a point first.
            if (community == Community.Gatekeepers)
            {
                int sent = Communities.GatekeeperSendBack(points);
                now = sent == int.MaxValue ? Loc.Get("community.Gatekeepers.now_all") : Loc.Get("community.Gatekeepers.now", ("value", sent));
            }
            else if (points > 0)
            {
                int value = Communities.Effect(community, points);
                // Extra action uses: how many are left right now.
                if (community == Community.Mods || community == Community.Marketers || community == Community.Voyeurs)
                    value = PartyEngine.ExtraUsesLeft(state, community);
                // Troll Army: the Cash this stream's Risk would bring right now.
                else if (community == Community.Trolls)
                    value = PartyEngine.TrollArmyCash(state);
                // Lurkers: the Cash this stream's empty spaces would bring right now.
                else if (community == Community.Lurkers)
                    value = PartyEngine.LurkersCash(state);
                // Hype Men: spare seats still free.
                else if (community == Community.HypeMen)
                    value = PartyEngine.FreeHypeSeats(state);
                now = Loc.Get($"community.{community}.now", ("value", value));
            }
            return tip.Section().Good(now);
        }

        /// <summary>
        /// Different viewers needed in chat for a community's top bonus (its last point or Boomers level), or 0 for a
        /// community with no cap. One more than the top point: 2 viewers make the first point.
        /// </summary>
        public static int MembersForMax(Community community)
        {
            int max = Communities.MaxPoints(community);
            return max > 0 ? Communities.MembersForPoint(community, max) : 0;
        }

        /// <summary>
        /// A community's progress as different viewers in chat: "2/4 in chat" (out of what its top bonus needs), or "3 in chat"
        /// with no cap. The bonus starts at 2 different viewers, so the first step already reads 2.
        /// </summary>
        public static string MembersText(Community community, int members)
        {
            int needed = MembersForMax(community);
            return needed > 0 ? Loc.Get("community.members_max", "count", members, "max", needed) : Loc.Get("community.members", "count", members);
        }

        /// <summary>
        /// The compact effect under a Community Beats chip: "30%", "+2", "+1 [cash]"... Boomers show the chance that Early
        /// Retirement retires the Boomer (100% / 50% / 25%).
        /// </summary>
        public static string ChipEffect(Community community, int points)
        {
            int effect = Communities.Effect(community, points);
            if (Communities.IsChance(community)) return effect + "%";
            string cash = GameIcons.Tag(ResourceIcon.Cash);
            string influence = GameIcons.Tag(ResourceIcon.Influence);
            switch (community)
            {
                // Per point while nobody's here, the stream's total once there are points.
                case Community.Staff: return $"+{Communities.StaffInfluence(Mathf.Max(1, points))}{influence}";
                // Cash per point of Risk.
                case Community.Trolls: return $"+{Communities.TrollArmyCashPerRisk}{cash}/{GameIcons.Tag(ResourceIcon.Risk)}";
                // Cash per empty space (per point while nobody's here).
                case Community.Lurkers: return $"+{Communities.LurkersPerSpace(Mathf.Max(1, points))}{cash}" + Loc.Get("beats.per_space");
                case Community.ShopControl: return $"-{Communities.ShopControlDiscount(Mathf.Max(1, points))}{cash}";
                case Community.Saboteurs: return $"+{Communities.SabotageAmount(Mathf.Max(1, points))}{cash}";
                // The Cash a retiring Boomer leaves a Millennial (+1, +2, +3).
                case Community.Millennial: return $"+{Communities.InheritanceAmount(points)}{cash}";
                // Viewers a Gatekeeper sends back now: 3, 6 or everyone.
                case Community.Gatekeepers:
                {
                    int sent = Communities.GatekeeperSendBack(points);
                    return sent == int.MaxValue ? Loc.Get("beats.gatekeepers_all") : "x" + sent;
                }
                // Spare seats (per point while nobody's here).
                case Community.HypeMen: return "+" + Loc.Get("beats.seats", "count", Communities.HypeSeats(Mathf.Max(1, points)));
                case Community.FullStream: return $"-{effect}";
                default: return "+" + effect;
            }
        }

        /// <summary>A viewer's communities as icons with names (for tooltips), or "No community".</summary>
        public static string Inline(GuestType type)
        {
            var mask = GuestCatalog.CommunitiesOf(type);
            if (mask == CommunityMask.None) return GuestVisuals.Colored(Loc.Get("card.no_community"), UIPalette.TextDim);
            var sb = new StringBuilder();
            foreach (var community in Communities.All)
            {
                if (!Communities.Has(mask, community)) continue;
                if (sb.Length > 0) sb.Append("   ");
                sb.Append(GameIcons.Tag(community)).Append(' ').Append(GuestVisuals.Colored(Name(community), Color(community)));
            }
            return sb.ToString();
        }

        /// <summary>Just the icons, for tight spaces (Audience rows).</summary>
        public static string Icons(GuestType type)
        {
            var mask = GuestCatalog.CommunitiesOf(type);
            var sb = new StringBuilder();
            foreach (var community in Communities.All)
                if (Communities.Has(mask, community)) sb.Append(GameIcons.Tag(community));
            return sb.ToString();
        }
    }
}
