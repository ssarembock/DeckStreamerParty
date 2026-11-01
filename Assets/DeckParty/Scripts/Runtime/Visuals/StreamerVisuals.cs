using System.Collections.Generic;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Presentation helpers for streamers (the characters players pick on the Select Your Streamer screen).
    /// Text keys: streamer.&lt;StreamerType&gt;.name / .title / .power / .desc. Art and quotes live in the Game Database.
    /// </summary>
    public static class StreamerVisuals
    {
        static readonly List<string> s_QuoteChoices = new List<string>();

        public static string Name(StreamerType type) => type == StreamerType.None ? Loc.Get("streamer.None.name") : Loc.Get($"streamer.{type}.name");

        public static string Title(StreamerType type) => Loc.Get($"streamer.{type}.title");

        public static string PowerName(StreamerType type) => Loc.Get($"streamer.{type}.power");

        public static string PowerDescription(StreamerType type) => Loc.Get($"streamer.{type}.desc");

        /// <summary>The CSV key of the catchphrase each streamer shipped with (copied into the Game Database as their first quote).</summary>
        public static string OriginalQuoteKey(StreamerType type) => $"streamer.{type}.quote";

        /// <summary>
        /// A random quote from the streamer's list in the Game Database, as stored (null when the list is empty). Avoids
        /// <paramref name="previous"/> when there's another to choose from. Show the result with <see cref="QuoteText"/>.
        /// </summary>
        public static string PickQuote(StreamerType type, string previous = null)
        {
            var quotes = GameDatabase.Instance.GetStreamerArt(type)?.quotes;
            if (quotes == null) return null;
            s_QuoteChoices.Clear();
            foreach (var quote in quotes)
                if (!string.IsNullOrWhiteSpace(quote)) s_QuoteChoices.Add(quote);
            if (s_QuoteChoices.Count > 1) s_QuoteChoices.Remove(previous);
            return s_QuoteChoices.Count > 0 ? s_QuoteChoices[Random.Range(0, s_QuoteChoices.Count)] : null;
        }

        /// <summary>
        /// The text to show for a stored quote: as typed, except the original catchphrase, which is translated for as long
        /// as its English text is unchanged. No Character has no database entry and always uses its built-in line.
        /// </summary>
        public static string QuoteText(StreamerType type, string quote)
        {
            string key = OriginalQuoteKey(type);
            if (type == StreamerType.None) return Loc.Get(key);
            if (string.IsNullOrEmpty(quote)) return string.Empty;
            return Loc.EnglishRaw(key) == quote ? Loc.Get(key) : quote;
        }

        public static string NameTag(StreamerType type) => GuestVisuals.Colored(Name(type), Color(type));

        public static Sprite Portrait(StreamerType type) => GameDatabase.Instance.GetStreamerArt(type)?.portrait;

        public static Color Color(StreamerType type)
        {
            var art = GameDatabase.Instance.GetStreamerArt(type);
            if (art != null && art.color.a > 0.01f) return art.color;
            return DefaultColor(type);
        }

        public static Color DefaultColor(StreamerType type)
        {
            switch (type)
            {
                case StreamerType.Fate: return new Color32(124, 108, 255, 255);
                case StreamerType.BroJogan: return new Color32(224, 122, 47, 255);
                case StreamerType.Shinobi: return new Color32(46, 197, 255, 255);
                case StreamerType.IRunFast: return new Color32(255, 70, 70, 255);
                case StreamerType.MrShark: return new Color32(63, 169, 245, 255);
                case StreamerType.EthicalFboi: return new Color32(232, 54, 79, 255);
                case StreamerType.SoftHardBodyMan: return new Color32(245, 196, 220, 255);
                case StreamerType.StrategicAbomination: return new Color32(61, 190, 107, 255);
                case StreamerType.Palmane: return new Color32(255, 122, 198, 255);
                case StreamerType.ManimMakethMan: return new Color32(76, 142, 234, 255);
                case StreamerType.RudeRadiologist: return new Color32(210, 30, 50, 255);
                default: return UIPalette.TextDim;
            }
        }

        /// <summary>Two-letter placeholder shown when no portrait is assigned.</summary>
        public static string Initials(StreamerType type)
        {
            switch (type)
            {
                case StreamerType.None: return "--";
                case StreamerType.Fate: return "F";
                case StreamerType.BroJogan: return "BJ";
                case StreamerType.Shinobi: return "SH";
                case StreamerType.IRunFast: return "IR";
                case StreamerType.MrShark: return "MS";
                case StreamerType.EthicalFboi: return "EF";
                case StreamerType.SoftHardBodyMan: return "SB";
                case StreamerType.StrategicAbomination: return "SA";
                case StreamerType.Palmane: return "PM";
                case StreamerType.ManimMakethMan: return "MM";
                case StreamerType.RudeRadiologist: return "RR";
                default: return "?";
            }
        }

        /// <summary>"Passive", "Active", "Rival (multiplayer)"...</summary>
        public static string KindLabel(StreamerType type)
        {
            switch (StreamerCatalog.Get(type).Kind)
            {
                case PowerKind.Passive: return Loc.Get("streamer.kind.passive");
                case PowerKind.Instant: return Loc.Get("streamer.kind.instant");
                case PowerKind.NextStream: return Loc.Get("streamer.kind.next_stream");
                case PowerKind.Rival: return Loc.Get("streamer.kind.rival");
                default: return string.Empty;
            }
        }

        /// <summary>The first-use cost and how it grows, e.g. "1 $ (+1 $ per use) - 2-turn cooldown". Actives are always paid in Cash.</summary>
        public static string CostRules(StreamerType type)
        {
            var info = StreamerCatalog.Get(type);
            if (!info.IsActive) return string.Empty;
            string cost = CostText(info.CashCost);
            string step = CostText(info.CashStep);
            string text = step.Length > 0 ? Loc.Get("streamer.cost_growing", "cost", cost, "step", step) : Loc.Get("streamer.cost_flat", "cost", cost);
            text += "   " + (info.Cooldown > 0 ? Loc.Get("streamer.cooldown", "count", info.Cooldown) : Loc.Get("streamer.once_per_turn"));
            return text;
        }

        public static string CostText(int cash) => cash > 0 ? cash + " " + Terms.CashSymbol : string.Empty;

        /// <summary>The background (stream setup) used while this streamer is live, or null for the player's own choice.</summary>
        public static VenueDefinition Venue(StreamerType type) => GameDatabase.Instance.GetStreamerArt(type)?.venue;
    }
}
