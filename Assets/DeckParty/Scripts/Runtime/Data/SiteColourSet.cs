using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>What a graphic of the website look is, so the UI Skin's Site Colours can recolour it (see <see cref="UI.SiteColour"/>).</summary>
    public enum SiteColourRole
    {
        /// <summary>The page behind every panel.</summary>
        Page = 0,
        /// <summary>The inside of a panel.</summary>
        PanelFill = 1,
        /// <summary>A panel's border (and the normal ring around chat avatars).</summary>
        PanelBorder = 2,
        /// <summary>The thin lines between sections.</summary>
        Divider = 3,
        /// <summary>Soft secondary fills: empty chat seats, the circle behind portraits, scrollbar tracks.</summary>
        SoftFill = 4,
        /// <summary>Main text, and the placeholder icons that sit on panels.</summary>
        Text = 5,
        /// <summary>Secondary text: hints, counts, subtitles.</summary>
        SecondaryText = 6,
        /// <summary>Faint text: empty notices, Community Beats that haven't scored.</summary>
        FaintText = 7,
    }

    /// <summary>
    /// The named panels of the stream and shop. Each can have its own inside, border and text colours (UI Skin > Site Colours);
    /// everything under a <see cref="UI.SiteAreaTag"/> takes that panel's colours. The numbers are stored: only append.
    /// </summary>
    public enum SiteArea
    {
        /// <summary>Not in a named panel: the shared colours.</summary>
        None = 0,
        MenuBar = 1,
        StreamChat = 2,
        WaitingViewer = 3,
        CommunityBeats = 4,
        Shop = 5,
        ShopTiles = 6,
        SidePanel = 7,
        PopUps = 8,
        Tooltips = 9,
        /// <summary>The Players card on the left (multiplayer). Unset colours come from <see cref="SidePanel"/> (2026-11-18).</summary>
        Players = 10,
        /// <summary>The emotes card on the left (multiplayer). Unset colours come from <see cref="SidePanel"/> (2026-11-18).</summary>
        Emotes = 11,
    }

    /// <summary>One named panel's own colours. Unticked colours use the shared ones.</summary>
    [Serializable]
    public sealed class SiteAreaColours
    {
        [HideInInspector] public string name;
        public SiteArea area;
        public bool overrideFill;
        public Color fill = SitePalette.Surface;
        public bool overrideBorder;
        public Color border = SitePalette.Outline;
        public bool overrideText;
        public Color text = SitePalette.Text;
    }

    /// <summary>
    /// The colours of the website look (the stream, the shop and the light pop-ups), kept in the UI Skin so they survive
    /// Rebuild. The defaults are the built-in look (<see cref="SitePalette"/>). A panel that overrides its inside or text
    /// colour gets dividers, soft fills and secondary text worked out from those two, so one change keeps it readable.
    /// </summary>
    [Serializable]
    public sealed class SiteColourSet
    {
        public Color page = SitePalette.Page;
        public Color panelFill = SitePalette.Surface;
        public Color panelBorder = SitePalette.Outline;
        public Color divider = SitePalette.Divider;
        public Color softFill = SitePalette.SurfaceAlt;
        public Color text = SitePalette.Text;
        public Color secondaryText = SitePalette.TextDim;
        public Color faintText = SitePalette.TextFaint;
        public List<SiteAreaColours> areas = new List<SiteAreaColours>();

        /// <summary>
        /// A panel inside another named panel: the Players and emotes cards are part of the side panel, so a colour they don't
        /// set themselves comes from the Side panel entry (then the shared colours). None for the others.
        /// </summary>
        public static SiteArea ParentOf(SiteArea area) =>
            area == SiteArea.Players || area == SiteArea.Emotes ? SiteArea.SidePanel : SiteArea.None;

        enum Part { Fill, Border, Text }

        /// <summary>The nearest entry (this panel, then its parent panel) that sets its own <paramref name="part"/>, or null.</summary>
        SiteAreaColours Setting(SiteArea area, Part part)
        {
            for (var current = area; current != SiteArea.None; current = ParentOf(current))
            {
                var entry = Find(current);
                if (entry == null) continue;
                bool sets = part == Part.Fill ? entry.overrideFill : part == Part.Border ? entry.overrideBorder : entry.overrideText;
                if (sets) return entry;
            }
            return null;
        }

        /// <summary>
        /// The colour for a role inside a named panel: its own colour when it overrides it, else its parent panel's (Players
        /// and Emotes: the Side panel), else the shared one.
        /// </summary>
        public Color Get(SiteColourRole role, SiteArea area)
        {
            var fillFrom = Setting(area, Part.Fill);
            var textFrom = Setting(area, Part.Text);
            var borderFrom = Setting(area, Part.Border);
            bool ownFill = fillFrom != null;
            bool ownText = textFrom != null;
            var fill = ownFill ? fillFrom.fill : panelFill;
            var ink = ownText ? textFrom.text : text;
            switch (role)
            {
                case SiteColourRole.Page: return page;
                case SiteColourRole.PanelFill: return fill;
                case SiteColourRole.PanelBorder: return borderFrom != null ? borderFrom.border : panelBorder;
                case SiteColourRole.Divider: return ownFill ? Mix(fill, ink, 0.11f) : divider;
                case SiteColourRole.SoftFill: return ownFill ? Mix(fill, ink, 0.065f) : softFill;
                case SiteColourRole.SecondaryText: return ownText ? Mix(ink, fill, 0.3f) : secondaryText;
                case SiteColourRole.FaintText: return ownText || ownFill ? Mix(ownText ? ink : secondaryText, fill, ownText ? 0.55f : 0.45f) : faintText;
                default: return ink;
            }
        }

        public SiteAreaColours Find(SiteArea area)
        {
            if (area == SiteArea.None || areas == null) return null;
            foreach (var entry in areas)
                if (entry != null && entry.area == area) return entry;
            return null;
        }

        /// <summary>One entry per named panel (in order), so the inspector always lists every panel.</summary>
        public void EnsureAllAreas()
        {
            if (areas == null) areas = new List<SiteAreaColours>();
            foreach (SiteArea area in Enum.GetValues(typeof(SiteArea)))
            {
                if (area == SiteArea.None) continue;
                var entry = Find(area);
                if (entry == null) areas.Add(entry = new SiteAreaColours { area = area });
                entry.name = area.ToString();
            }
            areas.RemoveAll(entry => entry == null || entry.area == SiteArea.None);
            areas.Sort((a, b) => a.area.CompareTo(b.area));
        }

        /// <summary>Back to the built-in look: the default shared colours and no panel overrides.</summary>
        public void ResetToDefaults()
        {
            var defaults = new SiteColourSet();
            page = defaults.page;
            panelFill = defaults.panelFill;
            panelBorder = defaults.panelBorder;
            divider = defaults.divider;
            softFill = defaults.softFill;
            text = defaults.text;
            secondaryText = defaults.secondaryText;
            faintText = defaults.faintText;
            areas = new List<SiteAreaColours>();
            EnsureAllAreas();
        }

        static Color Mix(Color from, Color to, float t)
        {
            var mixed = Color.Lerp(from, to, t);
            mixed.a = from.a;
            return mixed;
        }
    }
}
