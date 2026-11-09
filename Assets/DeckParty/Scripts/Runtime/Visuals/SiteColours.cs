using DeckParty.UI;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// The website look's colours as set in the UI Skin (Site Colours), for code that colours site graphics itself. Graphics
    /// built by the scene builders carry a <see cref="SiteColour"/> instead, which follows the skin live. Defaults to the
    /// built-in look (<see cref="SitePalette"/>) when there's no UI Skin.
    /// </summary>
    public static class SiteColours
    {
        static readonly SiteColourSet s_Defaults = new SiteColourSet();

        public static SiteColourSet Current
        {
            get
            {
                var skin = GameDatabase.Instance.skin;
                return skin != null && skin.siteColours != null ? skin.siteColours : s_Defaults;
            }
        }

        public static Color Get(SiteColourRole role, SiteArea area = SiteArea.None) => Current.Get(role, area);

        /// <summary>The colour for a role in the named panel this object sits in.</summary>
        public static Color Get(SiteColourRole role, Transform on) => Current.Get(role, AreaOf(on));

        /// <summary>The named panel an object sits in: the nearest <see cref="SiteAreaTag"/> at or above it (None outside panels).</summary>
        public static SiteArea AreaOf(Transform on)
        {
            for (var t = on; t != null; t = t.parent)
                if (t.TryGetComponent<SiteAreaTag>(out var tag) && tag.Area != SiteArea.None) return tag.Area;
            return SiteArea.None;
        }

        /// <summary>Whether an object is part of the website look (inside a named panel, or coloured by the Site Colours).</summary>
        public static bool IsSite(Transform on) => on != null && (on.GetComponent<SiteColour>() != null || AreaOf(on) != SiteArea.None);

        /// <summary>True for light colours (dark text reads on them).</summary>
        public static bool IsLight(Color color) => LightText.Luminance(color) > 0.5f;
    }
}
