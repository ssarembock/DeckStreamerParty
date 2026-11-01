using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// English text for the scene builders. A builder caption written as a key ("menu.single_player") gets that key's
    /// English text plus a LocalizedLabel, so every static caption follows the in-game language.
    /// </summary>
    internal static class EditorLoc
    {
        public const string CsvPath = "Assets/DeckParty/Resources/Localization/AudienceCapture_Strings.csv";
        static readonly Regex s_KeyPattern = new Regex(@"^[a-z][a-z0-9_]*(\.[A-Za-z0-9_]+)+$");
        static Dictionary<string, string> s_English;

        public static void Reset() => s_English = null;

        static Dictionary<string, string> English
        {
            get
            {
                if (s_English != null) return s_English;
                s_English = new Dictionary<string, string>();
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CsvPath);
                if (asset == null) return s_English;
                var rows = CsvTable.Parse(asset.text);
                if (rows.Count == 0) return s_English;
                int key = rows[0].IndexOf("Key"), english = rows[0].IndexOf("English");
                if (key < 0 || english < 0) return s_English;
                for (int i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    if (row.Count > english && !string.IsNullOrEmpty(row[key])) s_English[row[key]] = row[english].Replace("\\n", "\n");
                }
                return s_English;
            }
        }

        /// <summary>True when <paramref name="text"/> is a key that exists in the CSV.</summary>
        public static bool IsKey(string text) => !string.IsNullOrEmpty(text) && s_KeyPattern.IsMatch(text) && English.ContainsKey(text);

        public static string Get(string key) => English.TryGetValue(key, out var value) ? value : key;
    }
}
