using System.Text;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty
{
    /// <summary>Presentation helpers for guests: names, colours, portraits and rich-text stat lines.</summary>
    public static class GuestVisuals
    {
        public static string DisplayName(GuestType type)
        {
            var art = GameDatabase.Instance.GetGuestArt(type);
            if (art != null && !string.IsNullOrWhiteSpace(art.displayNameOverride)) return art.displayNameOverride;
            return GuestCatalog.Get(type).Name;
        }

        public static Sprite Portrait(GuestType type) => GameDatabase.Instance.GetGuestArt(type)?.portrait;

        public static Color Tint(GuestType type)
        {
            var art = GameDatabase.Instance.GetGuestArt(type);
            if (art != null && art.tint.a > 0.01f) return art.tint;
            return FamilyColor(GuestCatalog.Get(type).Category);
        }

        public static Color FamilyColor(GuestCategory category)
        {
            switch (category)
            {
                case GuestCategory.Community: return UIPalette.Blue;
                case GuestCategory.Animal: return UIPalette.Orange;
                case GuestCategory.Wojak: return UIPalette.Teal;
                case GuestCategory.RageComic: return UIPalette.Pink;
                case GuestCategory.Reaction: return UIPalette.Purple;
                case GuestCategory.Brainrot: return UIPalette.Green;
                default: return UIPalette.Gold;
            }
        }

        public static string FamilyName(GuestCategory category)
        {
            switch (category)
            {
                case GuestCategory.RageComic: return "Rage Comic";
                case GuestCategory.Diamond: return Terms.Diamond;
                default: return category.ToString();
            }
        }

        public static string ActionTypeName(ActionCategory category) => category == ActionCategory.None ? string.Empty : category.ToString();

        public static string Initials(GuestType type)
        {
            var name = DisplayName(type).Replace(".", string.Empty);
            var parts = name.Split(new[] { ' ', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpperInvariant();
            if (name.Length >= 2) return name.Substring(0, 1).ToUpperInvariant() + name.Substring(1, 1).ToLowerInvariant();
            return name.ToUpperInvariant();
        }

        public static string Colored(string text, Color color) => $"<color={UIPalette.Tag(color)}>{text}</color>";

        public static string NameTag(GuestType type) => Colored(DisplayName(type), Tint(type));

        public static string PopText(int value) => Colored((value >= 0 ? "+" : "") + value + " " + Terms.Subs, UIPalette.Gold);

        public static string CashText(int value) => Colored((value >= 0 ? "+" : "") + value + " " + Terms.CashSymbol, UIPalette.Green);

        public static string RiskText(int value) => Colored((value >= 0 ? "+" : "") + value + " " + Terms.Risk, UIPalette.Red);

        public static string GiftedSubsText(int value) => Colored(value + " " + Terms.GiftedSubs, UIPalette.Gold);

        /// <summary>The always-true summary of what a guest scores, ignoring live conditions.</summary>
        public static string BaseStats(GuestInfo info)
        {
            var sb = new StringBuilder();
            switch (info.Type)
            {
                case GuestType.NpcWojak:
                    sb.Append(Colored("Copies the guest before him", UIPalette.Gold));
                    break;
                case GuestType.PopCat:
                case GuestType.CryingCat:
                    sb.Append(Colored("Grows during the party", UIPalette.Gold));
                    break;
                default:
                    if (info.Pop != 0) sb.Append(PopText(info.Pop));
                    break;
            }

            if (info.Cash != 0)
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(CashText(info.Cash));
            }

            if (sb.Length == 0) sb.Append(Colored(info.HasAbility ? "Action guest" : "No base " + Terms.Subs, UIPalette.TextDim));
            return sb.ToString();
        }

        /// <summary>What this exact guest would score right now at this party.</summary>
        public static string LiveStats(PlayerState state, GuestInstance g)
        {
            PartyEngine.ScoreGuest(state, g, out int pop, out int cash);
            var sb = new StringBuilder();
            if (pop != 0 || cash == 0) sb.Append(PopText(pop));
            if (cash != 0)
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(CashText(cash));
            }
            return sb.ToString();
        }

        public static string Tags(GuestInfo info)
        {
            var sb = new StringBuilder();
            if (info.IsDiamond) sb.Append(Colored("DIAMOND ", UIPalette.Gold));
            if (info.Trouble) sb.Append(Colored("SWATTING RISK ", UIPalette.Red));
            if (info.CancelsTrouble) sb.Append(Colored("CANCELS RISK ", UIPalette.Teal));
            if (info.Brings > 0) sb.Append(Colored($"BRINGS {info.Brings} ", UIPalette.Orange));
            if (info.Size > 1) sb.Append(Colored($"TAKES {info.Size} SPACES ", UIPalette.Orange));
            if (info.HasAbility) sb.Append(Colored($"ACTION: {info.AbilityName.ToUpperInvariant()} ", UIPalette.Purple));
            return sb.ToString().TrimEnd();
        }

        public static string Tooltip(GuestType type, GuestInstance instance = null, PlayerState state = null)
        {
            var info = GuestCatalog.Get(type);
            var sb = new StringBuilder();
            sb.Append("<b><size=120%>").Append(DisplayName(type)).Append("</size></b>");
            if (info.Purchasable) sb.Append("   ").Append(Colored(info.Cost + " " + Terms.GiftedSubs, UIPalette.TextDim));
            sb.Append('\n');
            var tags = Tags(info);
            if (tags.Length > 0) sb.Append("<size=85%>").Append(tags).Append("</size>\n");
            sb.Append(info.Description);

            var meta = new StringBuilder();
            meta.Append(Colored(FamilyName(info.Category), FamilyColor(info.Category))).Append(" family");
            if (info.HasAbility) meta.Append("   ").Append(Colored(ActionTypeName(info.ActionCategory) + " action", UIPalette.Purple));
            sb.Append("\n<size=85%>").Append(meta).Append("</size>");

            if (instance != null)
            {
                if (instance.IsDisguised)
                    sb.Append('\n').Append(Colored($"Rickrolled! Really {DisplayName(instance.Type)}.", UIPalette.Pink));
                if (instance.BonusPop != 0)
                    sb.Append('\n').Append(Colored($"Permanent: {(instance.BonusPop > 0 ? "+" : "")}{instance.BonusPop} {Terms.Subs}", UIPalette.Pink));
                if (instance.StrikeCount > 0)
                    sb.Append('\n').Append(Colored($"Tung Tung strikes: {instance.StrikeCount}/3", UIPalette.Orange));
                if (instance.TrolledCount > 0)
                    sb.Append('\n').Append(Colored($"Trolled {instance.TrolledCount} time{(instance.TrolledCount == 1 ? "" : "s")}", UIPalette.TextDim));
                if (instance.Temporary)
                    sb.Append('\n').Append(Colored("Temporary copy: gone after tonight.", UIPalette.TextDim));

                if (state != null && state.House.Contains(instance.Id))
                {
                    sb.Append("\n<b>Now:</b> ").Append(LiveStats(state, instance));
                    int risk = PartyEngine.GuestTrouble(state, instance);
                    if (risk > 0) sb.Append("  ").Append(RiskText(risk));
                    var status = StatusLine(state, instance);
                    if (status.Length > 0) sb.Append('\n').Append(status);
                }
                if (state != null && state.BannedThisParty == instance.Id)
                    sb.Append('\n').Append(Colored($"{Terms.Ban}: sitting out tonight.", UIPalette.Orange));
            }
            return sb.ToString();
        }

        /// <summary>Tonight's modifiers on a guest at the party, as one line.</summary>
        public static string StatusLine(PlayerState state, GuestInstance g)
        {
            var sb = new StringBuilder();
            void Add(string text, Color color)
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(Colored(text, color));
            }

            if (g.Infected) Add("Infected", UIPalette.Green);
            if (g.Immune) Add("Immune", UIPalette.Teal);
            if (g.Zeroed) Add("Zeroed", UIPalette.Red);
            if (g.DoubleMode == 1) Add($"Double {Terms.Subs}", UIPalette.Gold);
            if (g.DoubleMode == 2) Add("Double $", UIPalette.Green);
            if (g.ConvertMode == 1) Add($"{Terms.Subs} to $", UIPalette.Green);
            if (g.ConvertMode == 2) Add($"$ to {Terms.Subs}", UIPalette.Gold);
            if (g.HasOverride) Add($"Printed {Terms.Subs} {g.PrintedOverride}", UIPalette.Pink);
            if (g.HasCopy) Add($"Copied {g.CopyPop} {Terms.Subs} / {g.CopyCash} $", UIPalette.Teal);
            if (g.TroubleCleared) Add("Risk cleared", UIPalette.Teal);
            if (g.Mood != PepeMood.None) Add(g.Mood.ToString(), UIPalette.Purple);
            if (g.ShieldUsed) Add("Bad luck used", UIPalette.TextDim);
            if (PartyEngine.IsProtected(state, g)) Add("Attached (can't be removed)", UIPalette.Pink);
            var partner = PartyEngine.AttachedPartner(state, g);
            if (partner != null) Add("Attached to " + DisplayName(partner.Kind), UIPalette.Pink);
            else if (g.AttachedTo == GuestInstance.AttachPending) Add("Waiting for someone to attach to", UIPalette.Pink);
            return sb.ToString();
        }
    }
}
