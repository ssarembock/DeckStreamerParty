namespace DeckParty
{
    /// <summary>
    /// UI Skin > Text Colours ids of the texts the game makes in code while playing (the scene builders list the rest). Each
    /// one is listed in <see cref="All"/> so the UI Skin shows it even though no scene holds it.
    /// </summary>
    public static class TextColourIds
    {
        public const string AnxietyTitle = "Code/AnxiousStreamer/Title";
        public const string AnxietySubtitle = "Code/AnxiousStreamer/Subtitle";
        public const string TimerSeconds = "Code/TurnTimer/Seconds";
        public const string FacecamLive = "Code/StreamOverlay/Live";
        public const string FacecamTime = "Code/StreamOverlay/Timer";

        /// <summary>Every code-made text: its id, readable name, group (as in the scene lists) and a sample of what it says.</summary>
        public static readonly (string id, string label, string group, string sample)[] All =
        {
            (AnxietyTitle, "Near-deplatform warning: title", "Stream", "ONE MORE AND WE'RE DEPLATFORMED!"),
            (AnxietySubtitle, "Near-deplatform warning: line under the title", "Stream", "{streamer} is sweating..."),
            (TimerSeconds, "Countdown timer: seconds", "Stream", "30"),
            (FacecamLive, "Facecam: LIVE badge", "Stream", "LIVE"),
            (FacecamTime, "Facecam: stream time beside LIVE", "Stream", "00:42"),
        };
    }
}
