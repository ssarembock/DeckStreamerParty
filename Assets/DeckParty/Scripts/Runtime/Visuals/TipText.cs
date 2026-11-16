using System.Text;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// The one layout for every hover pop-up: a bold title (with an optional faint note beside it, such as a price or a key),
    /// a divider, then the body: short plain lines, faint details, a green status and an orange "why not" line.
    /// Colours are the game's usual ones for dark panels; the pop-up darkens them on its white card (see LightText).
    /// </summary>
    public sealed class TipText
    {
        /// <summary>Between the title and the body. The tooltip draws its divider there (older scenes show a line break).</summary>
        public const char Split = '\u001E';

        /// <summary>The space between two sections of the body: a short blank line.</summary>
        const string Gap = "\n<size=40%>\n</size>";

        readonly StringBuilder m_Body = new StringBuilder();
        readonly string m_Title;
        bool m_GapNext;

        TipText(string title) => m_Title = title;

        /// <summary>A pop-up with a title row. <paramref name="note"/> sits beside the title, small and faint.</summary>
        public static TipText Titled(string title, string note = null) =>
            new TipText(string.IsNullOrEmpty(note) ? Bold(title) : Bold(title) + "  " + Note(note));

        /// <summary>A pop-up with no title row (a short label, or a body only).</summary>
        public static TipText Plain() => new TipText(null);

        /// <summary>A plain line of the body (skipped when empty).</summary>
        public TipText Line(string text)
        {
            if (string.IsNullOrEmpty(text)) return this;
            if (m_Body.Length > 0) m_Body.Append(m_GapNext ? Gap : "\n");
            m_GapNext = false;
            m_Body.Append(text);
            return this;
        }

        /// <summary>A secondary line: smaller and faint.</summary>
        public TipText Detail(string text) => string.IsNullOrEmpty(text) ? this : Line(Small(Faint(text)));

        /// <summary>A good state ("Ready", what a community gives right now).</summary>
        public TipText Good(string text) => string.IsNullOrEmpty(text) ? this : Line(Colour(text, UIPalette.Green));

        /// <summary>A highlighted number line (scores, points).</summary>
        public TipText Highlight(string text) => string.IsNullOrEmpty(text) ? this : Line(Colour(text, UIPalette.Gold));

        /// <summary>Why something can't be done right now.</summary>
        public TipText Warn(string text) => string.IsNullOrEmpty(text) ? this : Line(Colour(text, UIPalette.Orange));

        /// <summary>The next line starts a new section, after a short gap.</summary>
        public TipText Section()
        {
            if (m_Body.Length > 0) m_GapNext = true;
            return this;
        }

        public override string ToString()
        {
            if (m_Title == null) return m_Body.ToString();
            return m_Body.Length == 0 ? m_Title + Split : m_Title + Split + m_Body;
        }

        public static implicit operator string(TipText tip) => tip?.ToString();

        /// <summary>
        /// Splits a pop-up text into its title row and body: at <see cref="Split"/>, or else (text from the strings file or
        /// written by hand) a first line that starts in bold is the title. False when it has no title.
        /// </summary>
        public static bool TrySplit(string text, out string title, out string body)
        {
            title = null;
            body = text;
            if (string.IsNullOrEmpty(text)) return false;
            int split = text.IndexOf(Split);
            if (split < 0)
            {
                if (!text.StartsWith("<b>", System.StringComparison.Ordinal)) return false;
                split = text.IndexOf('\n');
                if (split < 0) split = text.Length;
            }
            title = text.Substring(0, split);
            body = split < text.Length ? text.Substring(split + 1) : string.Empty;
            return true;
        }

        // ------------------------------------------------------------------ inline pieces

        public static string Bold(string text) => $"<b>{text}</b>";

        public static string Small(string text) => $"<size=88%>{text}</size>";

        public static string Faint(string text) => Colour(text, UIPalette.TextDim);

        /// <summary>A small faint note beside a title: a price, a count, a key.</summary>
        public static string Note(string text) => $"<size=82%>{Faint(text)}</size>";

        /// <summary>A keyboard key, as a note: "Space", "E", "Esc".</summary>
        public static string Key(string key) => Note($"[{key}]");

        public static string Colour(string text, Color colour) => $"<color={UIPalette.Tag(colour)}>{text}</color>";

        /// <summary>The non-empty parts joined with a faint middle dot.</summary>
        public static string Join(params string[] parts)
        {
            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                if (sb.Length > 0) sb.Append(Faint("  ·  "));
                sb.Append(part);
            }
            return sb.ToString();
        }
    }
}
