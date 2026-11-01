using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
#if DECKPARTY_LOCALIZATION
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
#endif

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Localization setup. The CSV (Resources/Localization/AudienceCapture_Strings.csv) is the source of truth you edit in
    /// Excel or Google Sheets; Import CSV turns it into Unity Localization string tables (one collection per "Table"
    /// column value), Export CSV goes the other way after editing in Unity's Localization Tables window. Also creates the
    /// five locales, the multi-language TextMeshPro fonts and makes Addressables (which the tables live in) build with the player.
    /// </summary>
    internal static class LocalizationSetup
    {
        public const string Folder = "Assets/DeckParty/Localization";
        public const string FontsFolder = "Assets/DeckParty/Fonts";
        static readonly string[] s_ExtraColumns = { "Table", "Key", "Context" };

        [MenuItem("Deck Party/Localization/Import CSV into String Tables", priority = 100)]
        public static void ImportMenu()
        {
            int count = ImportCsv();
            EditorUtility.DisplayDialog("Deck Party", count >= 0 ? $"Imported {count} strings into the string tables." : "Import failed: see the Console.", "OK");
        }

        [MenuItem("Deck Party/Localization/Export String Tables to CSV", priority = 101)]
        public static void ExportMenu()
        {
            int count = ExportCsv();
            EditorUtility.DisplayDialog("Deck Party", count >= 0 ? $"Exported {count} strings to {EditorLoc.CsvPath}." : "Export failed: see the Console.", "OK");
        }

        [MenuItem("Deck Party/Localization/Select Strings CSV", priority = 102)]
        public static void SelectCsv()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(EditorLoc.CsvPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        [MenuItem("Deck Party/Localization/Rebuild Fonts", priority = 110)]
        public static void RebuildFonts() => EnsureFonts(true);

        /// <summary>Everything localization needs. Called by Deck Party > Setup > Build Everything.</summary>
        public static void EnsureAll()
        {
            EnsureFonts(false);
#if DECKPARTY_LOCALIZATION
            EnsureLocales();
            ImportCsv();
            ConfigureAddressables();
#else
            Debug.LogWarning("[DeckParty] The Localization package isn't installed yet (Unity is still importing it?). The game uses the CSV directly until it is; run Deck Party > Localization > Import CSV afterwards.");
#endif
            Loc.Reload();
        }

        // ------------------------------------------------------------------ CSV

        static List<List<string>> ReadCsv(out List<string> header)
        {
            header = null;
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(EditorLoc.CsvPath);
            if (asset == null)
            {
                Debug.LogError($"[DeckParty] {EditorLoc.CsvPath} not found.");
                return null;
            }
            var rows = CsvTable.Parse(asset.text);
            if (rows.Count == 0) return null;
            header = rows[0];
            return rows;
        }

        /// <summary>Returns how many rows were imported, or -1 on failure.</summary>
        public static int ImportCsv()
        {
#if DECKPARTY_LOCALIZATION
            var rows = ReadCsv(out var header);
            if (rows == null) return -1;
            int tableColumn = header.IndexOf("Table"), keyColumn = header.IndexOf("Key");
            if (tableColumn < 0 || keyColumn < 0)
            {
                Debug.LogError("[DeckParty] The CSV needs Table and Key columns.");
                return -1;
            }

            var locales = EnsureLocales();
            var columns = new List<(Locale locale, int column)>();
            foreach (var language in Loc.Languages)
            {
                int column = header.IndexOf(language.CsvColumn);
                var locale = locales.Find(l => l.Identifier.Code == language.Code);
                if (column >= 0 && locale != null) columns.Add((locale, column));
            }

            var collections = new Dictionary<string, StringTableCollection>();
            int imported = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count <= keyColumn || string.IsNullOrEmpty(row[keyColumn])) continue;
                string tableName = row.Count > tableColumn && !string.IsNullOrEmpty(row[tableColumn]) ? row[tableColumn] : "UI";
                if (!collections.TryGetValue(tableName, out var collection)) collections[tableName] = collection = Collection(tableName, locales);

                string key = row[keyColumn];
                if (collection.SharedData.GetEntry(key) == null) collection.SharedData.AddKey(key);
                foreach (var (locale, column) in columns)
                {
                    var table = collection.GetTable(locale.Identifier) as StringTable;
                    if (table == null) continue;
                    string value = column < row.Count ? row[column] : string.Empty;
                    var entry = table.GetEntry(key);
                    if (string.IsNullOrEmpty(value))
                    {
                        if (entry != null) table.RemoveEntry(key);
                        continue;
                    }
                    if (entry == null) table.AddEntry(key, value);
                    else entry.Value = value;
                }
                imported++;
            }

            foreach (var collection in collections.Values)
            {
                EditorUtility.SetDirty(collection);
                EditorUtility.SetDirty(collection.SharedData);
                foreach (var (locale, _) in columns)
                {
                    var table = collection.GetTable(locale.Identifier);
                    if (table != null) EditorUtility.SetDirty(table);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[DeckParty] Imported {imported} strings into {collections.Count} string table collections.");
            return imported;
#else
            Debug.LogWarning("[DeckParty] The Localization package isn't installed: the game reads the CSV directly.");
            return 0;
#endif
        }

        /// <summary>Writes the string tables back to the CSV (keeping each row's Context and Status). Returns rows written, or -1.</summary>
        public static int ExportCsv()
        {
#if DECKPARTY_LOCALIZATION
            var existing = ReadCsv(out var header) ?? new List<List<string>>();
            int keyColumn = header != null ? header.IndexOf("Key") : -1;
            int contextColumn = header != null ? header.IndexOf("Context") : -1;
            int statusColumn = header != null ? header.IndexOf("Status") : -1;
            var context = new Dictionary<string, string>();
            var status = new Dictionary<string, string>();
            for (int r = 1; r < existing.Count && keyColumn >= 0; r++)
            {
                var row = existing[r];
                if (row.Count <= keyColumn) continue;
                if (contextColumn >= 0 && contextColumn < row.Count) context[row[keyColumn]] = row[contextColumn];
                if (statusColumn >= 0 && statusColumn < row.Count) status[row[keyColumn]] = row[statusColumn];
            }

            var sb = new StringBuilder();
            var columns = new List<string>(s_ExtraColumns);
            foreach (var language in Loc.Languages) columns.Add(language.CsvColumn);
            columns.Add("Status");
            sb.AppendLine(string.Join(",", columns.Select(CsvTable.Escape)));

            int written = 0;
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections().OrderBy(c => System.Array.IndexOf(TableOrder, c.TableCollectionName)))
            {
                foreach (var shared in collection.SharedData.Entries)
                {
                    var cells = new List<string> { collection.TableCollectionName, shared.Key, context.TryGetValue(shared.Key, out var c) ? c : string.Empty };
                    foreach (var language in Loc.Languages)
                    {
                        var table = collection.GetTable(new LocaleIdentifier(language.Code)) as StringTable;
                        var entry = table != null ? table.GetEntry(shared.Key) : null;
                        cells.Add(entry != null ? entry.Value : string.Empty);
                    }
                    cells.Add(status.TryGetValue(shared.Key, out var s) ? s : "draft");
                    sb.AppendLine(string.Join(",", cells.Select(CsvTable.Escape)));
                    written++;
                }
            }

            File.WriteAllText(EditorLoc.CsvPath, sb.ToString(), new UTF8Encoding(true));
            AssetDatabase.ImportAsset(EditorLoc.CsvPath);
            EditorLoc.Reset();
            return written;
#else
            Debug.LogWarning("[DeckParty] The Localization package isn't installed: edit the CSV directly.");
            return 0;
#endif
        }

        /// <summary>Collection order used when exporting (matches the runtime table list).</summary>
        static readonly string[] TableOrder = { "UI", "Gameplay", "Characters", "Streamers", "Content", "Multiplayer" };

#if DECKPARTY_LOCALIZATION
        static StringTableCollection Collection(string name, List<Locale> locales)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(name);
            if (collection == null)
            {
                EnsureFolder(Folder + "/Tables");
                collection = LocalizationEditorSettings.CreateStringTableCollection(name, Folder + "/Tables", locales);
            }
            foreach (var locale in locales)
                if (collection.GetTable(locale.Identifier) == null) collection.AddNewTable(locale.Identifier);
            return collection;
        }

        /// <summary>The settings asset plus one Locale per shipped language.</summary>
        public static List<Locale> EnsureLocales()
        {
            EnsureFolder(Folder + "/Locales");
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                string path = Folder + "/LocalizationSettings.asset";
                var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(path);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    AssetDatabase.CreateAsset(settings, path);
                }
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            var result = new List<Locale>();
            var known = LocalizationEditorSettings.GetLocales();
            foreach (var language in Loc.Languages)
            {
                var locale = known.FirstOrDefault(l => l != null && l.Identifier.Code == language.Code);
                if (locale == null)
                {
                    string path = $"{Folder}/Locales/{language.Code}.asset";
                    locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
                    if (locale == null)
                    {
                        locale = Locale.CreateLocale(new LocaleIdentifier(language.Code));
                        locale.LocaleName = language.NativeName;
                        AssetDatabase.CreateAsset(locale, path);
                    }
                    LocalizationEditorSettings.AddLocale(locale);
                }
                result.Add(locale);
            }
            return result;
        }

        /// <summary>The string tables are Addressables content: build it with every player build so builds just work.</summary>
        static void ConfigureAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            if (settings.BuildAddressablesWithPlayerBuild != AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer)
            {
                settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
                EditorUtility.SetDirty(settings);
            }
        }
#endif

        // ------------------------------------------------------------------ fonts

        /// <summary>
        /// Dynamic TextMeshPro font assets for every language: Noto Sans (Latin + Cyrillic) with Noto Sans SC as the Chinese
        /// fallback for body text, and Rubik with ZCOOL KuaiLe / Noto Sans SC for titles. All SIL Open Font License.
        /// </summary>
        public static void EnsureFonts(bool overwrite)
        {
            var body = FontAsset("NotoSans-Regular.ttf", "NotoSans SDF", overwrite);
            var bodyBold = FontAsset("NotoSans-Bold.ttf", "NotoSans Bold SDF", overwrite);
            var chinese = FontAsset("NotoSansSC.ttf", "NotoSansSC SDF", overwrite);
            var header = FontAsset("Rubik.ttf", "Rubik SDF", overwrite);
            var headerChinese = FontAsset("ZCOOLKuaiLe-Regular.ttf", "ZCOOLKuaiLe SDF", overwrite);
            if (body == null || chinese == null)
            {
                Debug.LogWarning($"[DeckParty] Font files missing from {FontsFolder}: text keeps the default font (no Chinese glyphs).");
                return;
            }

            SetFallbacks(body, chinese);
            if (bodyBold != null)
            {
                SetFallbacks(bodyBold, chinese);
                var weights = body.fontWeightTable;
                if (weights != null && weights.Length > 7)
                {
                    weights[7].regularTypeface = bodyBold;

                }
            }
            if (header != null) SetFallbacks(header, headerChinese, chinese, body);
            if (headerChinese != null) SetFallbacks(headerChinese, chinese);

            // Every text (including the TMP default font) can fall back to Chinese.
            var tmpSettings = Resources.Load<TMP_Settings>("TMP Settings");
            if (tmpSettings != null)
            {
                var so = new SerializedObject(tmpSettings);
                var list = so.FindProperty("m_fallbackFontAssets");
                bool present = false;
                for (int i = 0; list != null && i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == chinese) present = true;
                if (list != null && !present)
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = chinese;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var skin = AssetDatabase.LoadAssetAtPath<UISkin>(DeckPartyPaths.Skin);
            if (skin != null)
            {
                if (skin.bodyFont == null || overwrite) skin.bodyFont = body;
                if ((skin.headerFont == null || overwrite) && header != null) skin.headerFont = header;
                EditorUtility.SetDirty(skin);
            }
            AssetDatabase.SaveAssets();
        }

        static void SetFallbacks(TMP_FontAsset asset, params TMP_FontAsset[] fallbacks)
        {
            if (asset == null) return;
            var list = asset.fallbackFontAssetTable ?? new List<TMP_FontAsset>();
            foreach (var fallback in fallbacks)
                if (fallback != null && fallback != asset && !list.Contains(fallback)) list.Add(fallback);
            asset.fallbackFontAssetTable = list;
            EditorUtility.SetDirty(asset);
        }

        static TMP_FontAsset FontAsset(string fileName, string assetName, bool overwrite)
        {
            string assetPath = $"{FontsFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null && !overwrite) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontsFolder}/{fileName}");
            if (font == null) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(assetPath);

            // Dynamic with multi-atlas: glyphs (including thousands of Chinese characters) are added as they're needed.
            var asset = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null) return null;
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, assetPath);
            if (asset.atlasTextures != null && asset.atlasTextures.Length > 0 && asset.atlasTextures[0] != null)
            {
                asset.atlasTextures[0].name = assetName + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            }
            if (asset.material != null)
            {
                asset.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
