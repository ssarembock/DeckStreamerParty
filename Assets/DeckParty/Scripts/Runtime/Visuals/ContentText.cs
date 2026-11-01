using DeckParty.Core;

namespace DeckParty
{
    /// <summary>Localized names for content: character sets (keys "scenario.&lt;id&gt;.*") and stream setups ("venue.&lt;id&gt;").</summary>
    public static class ContentText
    {
        public static string ScenarioName(int id) => Loc.RawOr($"scenario.{id}.name", ScenarioCatalog.Get(id).Name);

        public static string ScenarioTagline(int id) => Loc.RawOr($"scenario.{id}.tagline", ScenarioCatalog.Get(id).Tagline);

        public static string ScenarioDescription(int id) => Loc.RawOr($"scenario.{id}.desc", ScenarioCatalog.Get(id).Description);

        public static string VenueDescription(VenueDefinition venue) =>
            venue == null ? string.Empty : Loc.RawOr($"venue.{venue.id}.desc", venue.description);
    }
}
