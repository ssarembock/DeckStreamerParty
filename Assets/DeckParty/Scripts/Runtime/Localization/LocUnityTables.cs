#if DECKPARTY_LOCALIZATION
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DeckParty
{
    /// <summary>
    /// Connects <see cref="Loc"/> to the Unity Localization package: reads the string tables for a language and keeps
    /// the package's selected locale in step with the in-game language picker.
    /// </summary>
    static class LocUnityTables
    {
        /// <summary>The string table collections Deck Party > Localization > Import CSV creates (one per "Table" column value).</summary>
        public static readonly string[] TableNames = { "UI", "Gameplay", "Characters", "Streamers", "Content", "Multiplayer" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            Loc.TableProvider = ReadTables;
            Loc.LocaleSelector = SelectLocale;
        }

        static bool Ready()
        {
            // No settings asset until Deck Party > Localization > Import CSV has run once: the CSV covers that case.
            if (!LocalizationSettings.HasSettings) return false;
            var init = LocalizationSettings.InitializationOperation;
            if (!init.IsDone) init.WaitForCompletion();
            return LocalizationSettings.AvailableLocales != null && LocalizationSettings.AvailableLocales.Locales.Count > 0;
        }

        static Dictionary<string, string> ReadTables(string code)
        {
            if (!Ready()) return null;
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale == null) return null;

            var result = new Dictionary<string, string>(1024);
            foreach (var name in TableNames)
            {
                var table = LocalizationSettings.StringDatabase.GetTable(name, locale);
                if (table == null) continue;
                foreach (var entry in table.Values)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.Key)) continue;
                    result[entry.Key] = entry.Value;
                }
            }
            return result;
        }

        static void SelectLocale(string code)
        {
            if (!Ready()) return;
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale != null && LocalizationSettings.SelectedLocale != locale) LocalizationSettings.SelectedLocale = locale;
        }
    }
}
#endif
