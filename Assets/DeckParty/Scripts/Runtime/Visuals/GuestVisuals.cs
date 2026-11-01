using System.Text;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Presentation helpers for viewers: localized names, colours, portraits and rich-text stat lines.
    /// Text keys: char.&lt;GuestType&gt;.name / .desc / .action, ability.&lt;AbilityKind&gt;.opt&lt;n&gt;, family.*, card.*.
    /// </summary>
    public static class GuestVisuals
    {
        public static string DisplayName(GuestType type) => Loc.RawOr($"char.{type}.name", GuestCatalog.Get(type).Name);

        public static string Description(GuestType type) => Loc.RawOr($"char.{type}.desc", GuestCatalog.Get(type).Description);

        public static string ActionName(GuestType type) => Loc.RawOr($"char.{type}.action", GuestCatalog.Get(type).AbilityName);

        /// <summary>The localized labels of an action's choices (Pepe's moods, Stonks' investments...), or null.</summary>
        public static string[] Options(GuestType type)
        {
            var info = GuestCatalog.Get(type);
            var options = info.Options;
            if (options == null) return null;
            var result = new string[options.Length];
            for (int i = 0; i < options.Length; i++) result[i] = Loc.RawOr($"ability.{info.Ability}.opt{i}", options[i]);
            return result;
        }

        public static string OptionLabel(GuestType type, int option)
        {
            var options = Options(type);
            return options != null && option >= 0 && option < options.Length ? options[option] : string.Empty;
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
                case GuestCategory.Tbd: return UIPalette.Blue;
                default: return UIPalette.Gold;
            }
        }

        public static string FamilyName(GuestCategory category) => Loc.Get($"family.{category}");

        /// <summary>The character group's icon from the Game Database (Viewer Groups section), or null while none is assigned.</summary>
        public static Sprite FamilyIcon(GuestCategory category) => GameDatabase.Instance.GetGroupIcon(category);

        public static string ActionTypeName(ActionCategory category) => category == ActionCategory.None ? string.Empty : Loc.Get($"actiontype.{category}");

        public static string Initials(GuestType type)
        {
            var name = GuestCatalog.Get(type).Name.Replace(".", string.Empty);
            var parts = name.Split(new[] { ' ', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpperInvariant();
            if (name.Length >= 2) return name.Substring(0, 1).ToUpperInvariant() + name.Substring(1, 1).ToLowerInvariant();
            return name.ToUpperInvariant();
        }

        public static string Colored(string text, Color color) => $"<color={UIPalette.Tag(color)}>{text}</color>";

        public static string NameTag(GuestType type) => Colored(DisplayName(type), Tint(type));

        static string Signed(int value) => (value >= 0 ? "+" : "") + value;

        public static string PopText(int value) => Colored(Loc.Get("fmt.subs", "value", Signed(value), "n", value), UIPalette.Gold);

        public static string CashText(int value) => Colored(Signed(value) + " " + Terms.CashSymbol, UIPalette.Green);

        public static string RiskText(int value) => Colored(Loc.Get("fmt.risk", "value", Signed(value), "n", value), UIPalette.Red);

        public static string GiftedSubsText(int value) => Colored(Loc.Get("fmt.gifted_subs", "value", value, "n", value), UIPalette.Gold);

        /// <summary>The always-true summary of what a viewer scores, ignoring live conditions.</summary>
        public static string BaseStats(GuestInfo info)
        {
            var sb = new StringBuilder();
            switch (info.Type)
            {
                case GuestType.NpcWojak:
                    sb.Append(Colored(Loc.Get("card.copies_previous"), UIPalette.Gold));
                    break;
                case GuestType.PopCat:
                case GuestType.CryingCat:
                    sb.Append(Colored(Loc.Get("card.grows"), UIPalette.Gold));
                    break;
                case GuestType.ComedianWithAPodcast:
                    sb.Append(Colored(Loc.Get("card.if_full", "value", PopText(GameRules.ComedianFullBonus)), UIPalette.Gold));
                    break;
                case GuestType.ExecutivePr:
                    sb.Append(Colored(Loc.Get("card.per_risky", "value", PopText(GameRules.PerRiskyViewerBonus)), UIPalette.Gold));
                    break;
                case GuestType.GenZWithJob:
                    sb.Append(Colored(Loc.Get("card.grows_cash"), UIPalette.Green));
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

            if (sb.Length == 0) sb.Append(Colored(Loc.Get(info.HasAbility ? "card.action_viewer" : "card.no_base"), UIPalette.TextDim));
            return sb.ToString();
        }

        /// <summary>What this exact viewer would score right now in this stream.</summary>
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
            void Tag(string text, Color color) => sb.Append(Colored(text, color)).Append(' ');
            if (info.IsDiamond) Tag(Loc.Get("card.tag.diamond"), UIPalette.Gold);
            if (info.Trouble) Tag(Loc.Get("card.tag.risk"), UIPalette.Red);
            else if (info.Type == GuestType.HateWatcher) Tag(Loc.Get("card.tag.risk_sometimes"), UIPalette.Red);
            if (info.CancelsTrouble) Tag(Loc.Get("card.tag.cancels"), UIPalette.Teal);
            if (info.Brings > 0) Tag(Loc.Get("card.tag.brings", "count", info.Brings), UIPalette.Orange);
            if (info.Size > 1) Tag(Loc.Get("card.tag.size", "count", info.Size), UIPalette.Orange);
            if (info.HasAbility) Tag(Loc.Get("card.tag.action", "action", ActionName(info.Type)), UIPalette.Purple);
            return sb.ToString().TrimEnd();
        }

        /// <param name="shopState">In the shop: the player whose shop it is, for their owned and copies-left numbers.</param>
        public static string Tooltip(GuestType type, GuestInstance instance = null, PlayerState state = null, PlayerState shopState = null)
        {
            var info = GuestCatalog.Get(type);
            var sb = new StringBuilder();
            sb.Append("<b><size=120%>").Append(DisplayName(type)).Append("</size></b>");
            if (info.Purchasable) sb.Append("   ").Append(Colored(Loc.Get("fmt.gifted_subs", "value", info.Cost, "n", info.Cost), UIPalette.TextDim));
            sb.Append('\n');
            var tags = Tags(info);
            if (tags.Length > 0) sb.Append("<size=85%>").Append(tags).Append("</size>\n");
            sb.Append(Description(type));

            var meta = new StringBuilder();
            meta.Append(Loc.Get("card.family", "family", Colored(FamilyName(info.Category), FamilyColor(info.Category))));
            if (info.HasAbility) meta.Append("   ").Append(Colored(Loc.Get("card.action_type", "type", ActionTypeName(info.ActionCategory)), UIPalette.Purple));
            sb.Append("\n<size=85%>").Append(meta).Append("</size>");

            // The base stats and, in the shop, how many copies are left (kept off the shop tile itself).
            if (shopState != null && info.Purchasable)
            {
                sb.Append("\n<size=90%>").Append(Loc.Get("card.base_stats", "stats", BaseStats(info))).Append("</size>");
                int owned = shopState.CountOwned(type);
                int left = PartyEngine.ShopLeft(shopState, type);
                sb.Append("\n<size=90%>").Append(Loc.Get("shop.tip_owned", "count", owned));
                if (left != int.MaxValue) sb.Append("   ").Append(Loc.Get("shop.tip_left", "count", left));
                sb.Append("</size>");
            }

            if (instance != null)
            {
                if (instance.IsDisguised)
                    sb.Append('\n').Append(Colored(Loc.Get("card.rickrolled", "viewer", DisplayName(instance.Type)), UIPalette.Pink));
                if (instance.BonusPop != 0)
                    sb.Append('\n').Append(Colored(Loc.Get("card.permanent", "value", PopText(instance.BonusPop)), UIPalette.Pink));
                if (instance.BonusCash != 0)
                    sb.Append('\n').Append(Colored(Loc.Get("card.permanent", "value", CashText(instance.BonusCash)), UIPalette.Green));
                if (instance.Type == GuestType.HateWatcher)
                    sb.Append('\n').Append(Colored(Loc.Get(instance.Wolf ? "card.hate_watching" : "card.next_hate_watch"), UIPalette.Red));
                if (instance.StrikeCount > 0)
                    sb.Append('\n').Append(Colored(Loc.Get("card.strikes", "count", instance.StrikeCount), UIPalette.Orange));
                if (instance.TrolledCount > 0)
                    sb.Append('\n').Append(Colored(Loc.Get("card.trolled", "count", instance.TrolledCount), UIPalette.TextDim));
                if (instance.Temporary)
                    sb.Append('\n').Append(Colored(Loc.Get("card.temporary"), UIPalette.TextDim));

                if (state != null && state.House.Contains(instance.Id))
                {
                    sb.Append('\n').Append(Loc.Get("card.now", "stats", LiveStats(state, instance)));
                    int risk = PartyEngine.GuestTrouble(state, instance);
                    if (risk > 0) sb.Append("  ").Append(RiskText(risk));
                    var status = StatusLine(state, instance);
                    if (status.Length > 0) sb.Append('\n').Append(status);
                }
                if (state != null && state.BannedThisParty == instance.Id)
                    sb.Append('\n').Append(Colored(Loc.Get("card.banned_tonight"), UIPalette.Orange));
            }
            return sb.ToString();
        }

        /// <summary>Tonight's modifiers on a viewer in chat, as one line.</summary>
        public static string StatusLine(PlayerState state, GuestInstance g)
        {
            var sb = new StringBuilder();
            void Add(string text, Color color)
            {
                if (sb.Length > 0) sb.Append("  ");
                sb.Append(Colored(text, color));
            }

            if (g.Infected) Add(Loc.Get("status.infected"), UIPalette.Green);
            if (g.Immune) Add(Loc.Get("status.immune"), UIPalette.Teal);
            if (g.Zeroed) Add(Loc.Get("status.zeroed"), UIPalette.Red);
            if (g.SubsMuted) Add(Loc.Get("status.no_subs"), UIPalette.Red);
            if (g.DoubleMode == 1) Add(Loc.Get("status.double_subs"), UIPalette.Gold);
            if (g.DoubleMode == 2) Add(Loc.Get("status.double_cash"), UIPalette.Green);
            if (g.ConvertMode == 1) Add(Loc.Get("status.subs_to_cash"), UIPalette.Green);
            if (g.ConvertMode == 2) Add(Loc.Get("status.cash_to_subs"), UIPalette.Gold);
            if (g.HasOverride) Add(Loc.Get("status.printed", "value", g.PrintedOverride), UIPalette.Pink);
            if (g.HasCopy) Add(Loc.Get("status.copied", "subs", g.CopyPop, "cash", g.CopyCash), UIPalette.Teal);
            if (g.TroubleCleared) Add(Loc.Get("status.risk_cleared"), UIPalette.Teal);
            if (g.Mood != PepeMood.None) Add(MoodName(g.Mood), UIPalette.Purple);
            if (g.ShieldUsed) Add(Loc.Get("status.bad_luck_used"), UIPalette.TextDim);
            if (PartyEngine.IsProtected(state, g)) Add(Loc.Get("status.protected"), UIPalette.Pink);
            var partner = PartyEngine.AttachedPartner(state, g);
            if (partner != null) Add(Loc.Get("status.attached_to", "viewer", DisplayName(partner.Kind)), UIPalette.Pink);
            else if (g.AttachedTo == GuestInstance.AttachPending) Add(Loc.Get("status.attach_pending"), UIPalette.Pink);
            return sb.ToString();
        }

        public static string MoodName(PepeMood mood) => Loc.Get($"status.mood.{mood}");
    }
}
