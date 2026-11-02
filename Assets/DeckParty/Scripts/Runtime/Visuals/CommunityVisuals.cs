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
            int per, subs = 0, cash = 0;
            switch (community)
            {
                case Community.Staff: per = Communities.StaffPercentPerPoint; break;
                case Community.Content: per = Communities.ContentPercentPerPoint; break;
                case Community.FinanceBros: per = Communities.FinancePercentPerPoint; break;
                case Community.Doomsdayers: per = Communities.DoomPercentPerPoint; break;
                case Community.ArmchairActivists: per = Communities.ArmchairPercentPerPoint; break;
                case Community.Clout:
                    per = Communities.CloutPercentPerPoint;
                    subs = Communities.CloutSubs;
                    break;
                case Community.MemeticsDivision: per = Communities.MemeticsPercentPerPoint; break;
                case Community.Bots: per = Communities.BotsPercentPerPoint; break;
                case Community.Zoomer:
                    per = Communities.ZoomerPercentPerPoint;
                    subs = Communities.ZoomerSubs;
                    cash = Communities.ZoomerCashCost;
                    break;
                case Community.Boomer:
                    per = 0;
                    subs = Communities.BoomerSubsCostPerPoint;
                    cash = Communities.BoomerCashPerPoint;
                    break;
                case Community.Millennial:
                    per = 0;
                    subs = Communities.MillennialSubsPerPoint;
                    cash = Communities.MillennialCashCostPerPoint;
                    break;
                default: per = 0; break;
            }
            return Loc.Get($"community.{community}.desc",
                ("every", Communities.CoreFanbasePointsPerCancel),
                ("per", per),
                ("max", max),
                ("top", Communities.Effect(community, max)),
                ("t1", Communities.TechspertiseChance(1)),
                ("t2", Communities.TechspertiseChance(2)),
                ("t3", Communities.TechspertiseChance(3)),
                ("subs", subs),
                ("cash", cash));
        }

        public static Color Color(Community community) => GameIcons.CommunityColor(community);

        /// <summary>What the community does and how it scales, plus its score and current effect during a stream.</summary>
        public static string Tooltip(Community community, PlayerState state = null)
        {
            var sb = new StringBuilder();
            sb.Append("<b><size=115%>").Append(GameIcons.Tag(community)).Append(' ')
              .Append(GuestVisuals.Colored(Name(community), Color(community))).Append("</size></b>\n");
            sb.Append(Description(community));

            if (state != null && state.Phase == GamePhase.Party)
            {
                int points = PartyEngine.CommunityScore(state, community);
                sb.Append("\n\n").Append(GuestVisuals.Colored(PointsText(community, points), UIPalette.Gold));
                if (points > 0)
                {
                    // Boomer / Millennial name both amounts (always shown as positive numbers in the text).
                    Communities.FixedReward(community, points, out int subs, out int cash);
                    sb.Append("\n").Append(GuestVisuals.Colored(Loc.Get($"community.{community}.now",
                        ("value", Communities.Effect(community, points)), ("subs", System.Math.Abs(subs)), ("cash", System.Math.Abs(cash))), UIPalette.Gold));
                }
            }
            return sb.ToString();
        }

        /// <summary>"3 points" or, for capped communities, "3/9 points".</summary>
        public static string PointsText(Community community, int points)
        {
            int max = Communities.MaxPoints(community);
            return max > 0 ? Loc.Get("community.points_max", "count", points, "max", max) : Loc.Get("community.points", "count", points);
        }

        /// <summary>The compact effect under a Community Beats chip: "-1 [risk]", "30%", "+2"...</summary>
        public static string ChipEffect(Community community, int points)
        {
            int effect = Communities.Effect(community, points);
            switch (community)
            {
                case Community.CoreFanbase:
                    return effect > 0
                        ? $"-{effect} {GameIcons.Tag(ResourceIcon.Risk)}"
                        : $"{points}/{Communities.CoreFanbasePointsPerCancel}";
                case Community.Trolls:
                    return "+" + effect;
                case Community.Boomer:
                case Community.Millennial:
                {
                    // Per point while nobody's here, the stream's total once there are points.
                    Communities.FixedReward(community, System.Math.Max(1, points), out int subs, out int cash);
                    string subsText = (subs >= 0 ? "+" : "") + subs + GameIcons.Tag(ResourceIcon.Subs);
                    string cashText = (cash >= 0 ? "+" : "") + cash + GameIcons.Tag(ResourceIcon.Cash);
                    return community == Community.Boomer ? cashText + " " + subsText : subsText + " " + cashText;
                }
                default:
                    return effect + "%";
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
