using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty
{
    /// <summary>One language the game ships in.</summary>
    public sealed class LanguageInfo
    {
        public readonly string Code;
        /// <summary>The language's own name, shown in the picker.</summary>
        public readonly string NativeName;
        /// <summary>The column header in AudienceCapture_Strings.csv.</summary>
        public readonly string CsvColumn;

        public LanguageInfo(string code, string nativeName, string csvColumn)
        {
            Code = code;
            NativeName = nativeName;
            CsvColumn = csvColumn;
        }
    }

    /// <summary>
    /// Every player-facing string goes through here. Text lives in Unity Localization string tables (generated from
    /// Resources/Localization/AudienceCapture_Strings.csv by Deck Party > Localization > Import CSV). When a table entry
    /// is missing the CSV itself is used, then English, then the key in brackets, so the game never shows blank text.
    ///
    /// Strings are whole sentences with named placeholders: "{viewer} joins the chat." Plurals use
    /// "{count:plural:one|other}" with each language's own forms (Russian: one|few|many, Chinese: one form).
    /// </summary>
    public static class Loc
    {
        public const string English = "en";
        public const string Pseudo = "pseudo";
        public const string CsvResource = "Localization/AudienceCapture_Strings";
        const string PrefKey = "dp.language";

        public static readonly LanguageInfo[] Languages =
        {
            new LanguageInfo("en", "English", "English"),
            new LanguageInfo("zh-Hans", "简体中文", "简体中文"),
            new LanguageInfo("ru", "Русский", "Русский"),
            new LanguageInfo("es-419", "Español (Latinoamérica)", "Español (Latinoamérica)"),
            new LanguageInfo("pt-BR", "Português (Brasil)", "Português (Brasil)"),
        };

        /// <summary>Fires after the language changes (and once the first time text is ready).</summary>
        public static event Action Changed;

        /// <summary>Optional provider that reads Unity Localization string tables (set when the package is installed).</summary>
        public static Func<string, Dictionary<string, string>> TableProvider;
        /// <summary>Called when the player picks a language, so Unity Localization's selected locale follows.</summary>
        public static Action<string> LocaleSelector;

        static string s_Code;
        static Dictionary<string, string> s_Current;
        static Dictionary<string, string> s_English;
        static Dictionary<string, Dictionary<string, string>> s_Csv;
        static bool s_Initialized;

        public static string LanguageCode
        {
            get
            {
                EnsureInitialized();
                return s_Code;
            }
        }

        public static bool IsChinese => LanguageCode == "zh-Hans";

        /// <summary>The pseudo-language (stretched, accented English) for finding text that overflows. Editor and development builds only.</summary>
        public static bool PseudoAvailable => Application.isEditor || Debug.isDebugBuild;

        // ================================================================== setup

        public static void EnsureInitialized()
        {
            if (s_Initialized) return;
            s_Initialized = true;
            s_Code = PickStartupLanguage();
            Load();
        }

        static string PickStartupLanguage()
        {
            var saved = PlayerPrefs.GetString(PrefKey, string.Empty);
            if (IsSupported(saved) || (saved == Pseudo && PseudoAvailable)) return saved;

            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                    return "zh-Hans";
                case SystemLanguage.Russian: return "ru";
                case SystemLanguage.Spanish: return "es-419";
                case SystemLanguage.Portuguese: return "pt-BR";
                default: return English;
            }
        }

        public static bool IsSupported(string code)
        {
            foreach (var language in Languages)
                if (language.Code == code) return true;
            return false;
        }

        public static void SetLanguage(string code)
        {
            EnsureInitialized();
            if (!IsSupported(code) && !(code == Pseudo && PseudoAvailable)) code = English;
            PlayerPrefs.SetString(PrefKey, code);
            if (code == s_Code) return;
            s_Code = code;
            Load();
            try { LocaleSelector?.Invoke(code == Pseudo ? English : code); }
            catch (Exception e) { Debug.LogException(e); }
            RaiseChanged();
        }

        /// <summary>Re-reads the tables (after a CSV import in the editor).</summary>
        public static void Reload()
        {
            s_Csv = null;
            if (!s_Initialized) return;
            Load();
            RaiseChanged();
        }

        static void RaiseChanged()
        {
            if (Changed == null) return;
            foreach (var handler in Changed.GetInvocationList())
            {
                try { ((Action)handler)(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        static void Load()
        {
            string code = s_Code == Pseudo ? English : s_Code;
            s_English = LoadLanguage(English);
            s_Current = code == English ? s_English : LoadLanguage(code);
        }

        static Dictionary<string, string> LoadLanguage(string code)
        {
            var result = new Dictionary<string, string>(1024);

            // CSV first, so table entries (which you may have edited in Unity) win.
            var csv = Csv();
            if (csv != null && csv.TryGetValue(code, out var column))
                foreach (var kv in column)
                    if (!string.IsNullOrEmpty(kv.Value)) result[kv.Key] = Unescape(kv.Value);

            if (TableProvider != null)
            {
                try
                {
                    var tables = TableProvider(code);
                    if (tables != null)
                        foreach (var kv in tables)
                            if (!string.IsNullOrEmpty(kv.Value)) result[kv.Key] = Unescape(kv.Value);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DeckParty] Localization tables could not be read ({e.Message}). Using the CSV.");
                }
            }
            return result;
        }

        /// <summary>The CSV writes line breaks as "\n" so every string stays on one row.</summary>
        static string Unescape(string value) => value.IndexOf('\\') >= 0 ? value.Replace("\\n", "\n") : value;

        static Dictionary<string, Dictionary<string, string>> Csv()
        {
            if (s_Csv != null) return s_Csv;
            s_Csv = new Dictionary<string, Dictionary<string, string>>();
            var asset = Resources.Load<TextAsset>(CsvResource);
            if (asset == null) return s_Csv;

            var rows = CsvTable.Parse(asset.text);
            if (rows.Count == 0) return s_Csv;
            var header = rows[0];
            int keyColumn = header.IndexOf("Key");
            if (keyColumn < 0) return s_Csv;

            var columns = new List<(string code, int index)>();
            foreach (var language in Languages)
            {
                int index = header.IndexOf(language.CsvColumn);
                if (index >= 0) columns.Add((language.Code, index));
            }

            foreach (var (code, _) in columns) s_Csv[code] = new Dictionary<string, string>(1024);
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (keyColumn >= row.Count || string.IsNullOrEmpty(row[keyColumn])) continue;
                foreach (var (code, index) in columns)
                    if (index < row.Count) s_Csv[code][row[keyColumn]] = row[index];
            }
            return s_Csv;
        }

        // ================================================================== lookups

        public static bool Has(string key)
        {
            EnsureInitialized();
            return key != null && (s_Current.ContainsKey(key) || s_English.ContainsKey(key));
        }

        /// <summary>The raw string for a key in the current language (falling back to English, then "[key]").</summary>
        public static string Raw(string key)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (!s_Current.TryGetValue(key, out var value) && !s_English.TryGetValue(key, out value))
                return "[" + key + "]";
            return s_Code == Pseudo ? Pseudoize(value) : value;
        }

        /// <summary>The raw string, or <paramref name="fallback"/> when the key doesn't exist.</summary>
        public static string RawOr(string key, string fallback) => Has(key) ? Raw(key) : fallback;

        /// <summary>The English text for a key, or null when there isn't one (recognises text that was copied out of the CSV).</summary>
        public static string EnglishRaw(string key)
        {
            EnsureInitialized();
            return key != null && s_English.TryGetValue(key, out var value) ? value : null;
        }

        public static string Get(string key) => Format(Raw(key), null);

        public static string Get(string key, string name, object value) => Format(Raw(key), new[] { (name, value) });

        public static string Get(string key, string name1, object value1, string name2, object value2) =>
            Format(Raw(key), new[] { (name1, value1), (name2, value2) });

        public static string Get(string key, params (string name, object value)[] args) => Format(Raw(key), args);

        /// <summary>A rules-engine message ("key|arg|arg", see <see cref="Err"/>) in the current language.</summary>
        public static string Error(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return string.Empty;
            var parts = encoded.Split(Err.Separator);
            if (!Has(parts[0])) return encoded;
            var args = new (string, object)[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++) args[i - 1] = ((i - 1).ToString(CultureInfo.InvariantCulture), DecodeArg(parts[i]));
            return Format(Raw(parts[0]), args);
        }

        static object DecodeArg(string arg)
        {
            // Tokens: @g viewer name, @a viewer's action, @s character set, @t streamer name, @w streamer power.
            if (arg.Length > 2 && arg[0] == '@' && int.TryParse(arg.Substring(2), out int id))
            {
                var type = (GuestType)id;
                switch (arg[1])
                {
                    case 'g': if (GuestCatalog.IsValid(type)) return GuestVisuals.NameTag(type); break;
                    case 'a': if (GuestCatalog.IsValid(type)) return GuestVisuals.ActionName(type); break;
                    case 's': return ContentText.ScenarioName(id);
                    case 't': return StreamerVisuals.NameTag((StreamerType)id);
                    case 'w': return StreamerVisuals.PowerName((StreamerType)id);
                }
            }
            if (int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)) return number;
            return arg;
        }

        // ================================================================== formatting

        /// <summary>Fills "{name}" and "{name:plural:...}" placeholders.</summary>
        public static string Format(string template, (string name, object value)[] args)
        {
            if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0) return template ?? string.Empty;

            var sb = new StringBuilder(template.Length + 32);
            int i = 0;
            while (i < template.Length)
            {
                char c = template[i];
                if (c != '{')
                {
                    sb.Append(c);
                    i++;
                    continue;
                }

                int close = FindClose(template, i);
                if (close < 0)
                {
                    sb.Append(template, i, template.Length - i);
                    break;
                }

                string body = template.Substring(i + 1, close - i - 1);
                sb.Append(Resolve(body, args, template, i, close));
                i = close + 1;
            }
            return sb.ToString();
        }

        static int FindClose(string s, int open)
        {
            int depth = 0;
            for (int i = open; i < s.Length; i++)
            {
                if (s[i] == '{') depth++;
                else if (s[i] == '}' && --depth == 0) return i;
            }
            return -1;
        }

        static string Resolve(string body, (string name, object value)[] args, string template, int open, int close)
        {
            int colon = body.IndexOf(':');
            string name = colon >= 0 ? body.Substring(0, colon) : body;
            if (!TryArg(args, name, out var value)) return template.Substring(open, close - open + 1);
            if (colon < 0) return Convert.ToString(value, CultureInfo.CurrentCulture);

            string rest = body.Substring(colon + 1);
            if (rest.StartsWith("plural:", StringComparison.Ordinal))
            {
                var forms = rest.Substring(7).Split('|');
                long n = value is IConvertible convertible ? Convert.ToInt64(convertible, CultureInfo.InvariantCulture) : 0;
                return Format(forms[Mathf.Clamp(PluralIndex(n, forms.Length), 0, forms.Length - 1)], args);
            }
            return Convert.ToString(value, CultureInfo.CurrentCulture);
        }

        static bool TryArg((string name, object value)[] args, string name, out object value)
        {
            if (args != null)
                foreach (var (n, v) in args)
                    if (n == name)
                    {
                        value = v;
                        return true;
                    }
            value = null;
            return false;
        }

        /// <summary>Which plural form to use (CLDR rules for the shipped languages).</summary>
        static int PluralIndex(long n, int formCount)
        {
            if (formCount <= 1) return 0;
            n = Math.Abs(n);
            switch (s_Code)
            {
                case "zh-Hans":
                    return formCount - 1;
                case "ru":
                {
                    long mod10 = n % 10, mod100 = n % 100;
                    if (mod10 == 1 && mod100 != 11) return 0;
                    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 1;
                    return Math.Min(2, formCount - 1);
                }
                case "pt-BR":
                    return n <= 1 ? 0 : 1;
                default:
                    return n == 1 ? 0 : 1;
            }
        }

        /// <summary>Stretches and accents English so untranslated or overflowing text stands out.</summary>
        static string Pseudoize(string value)
        {
            var sb = new StringBuilder(value.Length * 2 + 4);
            sb.Append('[');
            bool tag = false;
            int braces = 0;
            int letters = 0;
            foreach (char c in value)
            {
                if (c == '<') tag = true;
                if (c == '{') braces++;
                if (tag || braces > 0)
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(Accent(c));
                    if (char.IsLetter(c) && ++letters % 3 == 0) sb.Append('~');
                }
                if (c == '>') tag = false;
                if (c == '}') braces--;
            }
            sb.Append(']');
            return sb.ToString();
        }

        static char Accent(char c)
        {
            switch (c)
            {
                case 'a': return 'á';
                case 'e': return 'é';
                case 'i': return 'î';
                case 'o': return 'ö';
                case 'u': return 'ü';
                case 'n': return 'ñ';
                case 'c': return 'ç';
                case 'A': return 'Å';
                case 'E': return 'É';
                case 'O': return 'Ø';
                case 'U': return 'Ü';
                default: return c;
            }
        }
    }

    /// <summary>A small RFC 4180 CSV reader (quoted fields, doubled quotes, newlines inside quotes).</summary>
    public static class CsvTable
    {
        public static List<List<string>> Parse(string text)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(text)) return rows;
            int start = text.Length > 0 && text[0] == '﻿' ? 1 : 0;

            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = start; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else quoted = false;
                    }
                    else field.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '"': quoted = true; break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r': break;
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(row);
                        row = new List<string>();
                        break;
                    default: field.Append(c); break;
                }
            }
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }
            return rows;
        }

        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 || value.StartsWith(" ") || value.EndsWith(" ");
            return needsQuotes ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
