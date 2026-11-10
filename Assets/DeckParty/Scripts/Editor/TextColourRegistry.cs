using System.Collections.Generic;
using System.Text.RegularExpressions;
using DeckParty.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Lists the texts of the stream and shop in UI Skin > Text Colours. The scene builders call it as they build: each text gets
    /// an id (its place in the scene or prefab, so it survives rebuilds) on its <see cref="SkinnedText"/>, and an entry in the
    /// UI Skin with a readable name, the part of the game it's in and what it said when it was built. Colours set in the skin
    /// are kept; entries the build no longer makes are dropped unless they have a colour of their own.
    /// </summary>
    internal static class TextColourRegistry
    {
        public const string GameSceneSource = "Game scene";
        public const string CodeSource = "Game code";

        /// <summary>The order the UI Skin lists the groups in (any other group follows, in the order it was found).</summary>
        public static readonly string[] GroupOrder =
        {
            "Menu bar", "Stream", "Shop", "Side panel", "Deplatformed screen", "Chat avatars", "Shop tiles", "Viewer cards",
            "Viewer rows", "Player rows", "Activity log lines",
        };

        /// <summary>Tags every text in the Game scene (texts inside prefab copies keep the prefab's ids).</summary>
        public static void TagScene(Scene scene)
        {
            var found = new List<TextColourEntry>();
            var used = new HashSet<string>();
            foreach (var root in scene.GetRootGameObjects())
                Collect(root.transform, "Game/", GroupInGameScene, true, found, used);
            Commit(GameSceneSource, found);
        }

        /// <summary>Tags every text in a UI prefab being built (before it's saved).</summary>
        public static void TagPrefab(GameObject root, string source, string group)
        {
            var found = new List<TextColourEntry>();
            var used = new HashSet<string>();
            Collect(root.transform, string.Empty, _ => group, false, found, used);
            Commit(source, found);
        }

        static void Collect(Transform root, string prefix, System.Func<Transform, string> groupOf, bool skipPrefabCopies,
            List<TextColourEntry> found, HashSet<string> used)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (skipPrefabCopies && PrefabUtility.IsPartOfPrefabInstance(text)) continue;
                string path = PathOf(text.transform, root);
                string id = prefix + path;
                for (int n = 2; !used.Add(id); n++) id = $"{prefix}{path}#{n}";

                if (!text.TryGetComponent<SkinnedText>(out var skinned))
                {
                    // Made without EditorUI.Text: it keeps its own font, only the colour comes from the skin.
                    skinned = text.gameObject.AddComponent<SkinnedText>();
                    skinned.colourOnly = true;
                }
                skinned.colourId = id;
                skinned.builtColour = text.color;
                skinned.hasBuiltColour = true;

                found.Add(new TextColourEntry
                {
                    id = id,
                    label = LabelOf(text.transform, root),
                    group = groupOf(text.transform),
                    sample = SampleOf(text),
                    color = Opaque(text.color),
                });
            }
        }

        /// <summary>Merges what a build found into the UI Skin's list, keeping your colours.</summary>
        static void Commit(string source, List<TextColourEntry> found)
        {
            var skin = Skin();
            if (skin == null) return;
            skin.EnsureAllSlots();
            var old = new Dictionary<string, TextColourEntry>();
            foreach (var entry in skin.textColours)
                if (entry != null && !string.IsNullOrEmpty(entry.id)) old[entry.id] = entry;

            // This source's entries are rebuilt in the order the build made them; colours you set carry over.
            var kept = new List<TextColourEntry>();
            var seen = new HashSet<string>();
            foreach (var entry in found)
            {
                entry.source = source;
                if (old.TryGetValue(entry.id, out var before))
                {
                    entry.overrideColor = before.overrideColor;
                    if (before.overrideColor) entry.color = before.color;
                }
                seen.Add(entry.id);
                kept.Add(entry);
            }

            var result = new List<TextColourEntry>();
            int insertAt = -1;
            foreach (var entry in skin.textColours)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id)) continue;
                if (entry.source != source && !seen.Contains(entry.id))
                {
                    result.Add(entry);
                    continue;
                }
                if (insertAt < 0) insertAt = result.Count;
                // Gone from this build: kept (marked) only when it has a colour of its own.
                if (!seen.Contains(entry.id) && entry.overrideColor)
                {
                    entry.missing = true;
                    kept.Add(entry);
                }
            }
            result.InsertRange(insertAt < 0 ? result.Count : insertAt, kept);
            skin.textColours = result;
            AddCodeTexts(skin);

            EditorUtility.SetDirty(skin);
            skin.NotifyChanged();
        }

        /// <summary>The texts the game makes in code while playing (TextColourIds): always listed.</summary>
        static void AddCodeTexts(UISkin skin)
        {
            foreach (var (id, label, group, sample) in TextColourIds.All)
            {
                var entry = skin.textColours.Find(e => e != null && e.id == id);
                if (entry == null) skin.textColours.Add(entry = new TextColourEntry { id = id, color = Color.black });
                entry.label = label;
                entry.group = group;
                entry.sample = sample;
                entry.source = CodeSource;
                entry.missing = false;
            }
        }

        /// <summary>Adds the code-made texts to the skin's list (the UI Skin inspector calls this, so they show before a rebuild).</summary>
        public static void EnsureCodeTexts(UISkin skin)
        {
            if (skin == null) return;
            if (skin.textColours == null) skin.textColours = new List<TextColourEntry>();
            int before = skin.textColours.Count;
            AddCodeTexts(skin);
            if (skin.textColours.Count != before) EditorUtility.SetDirty(skin);
        }

        static UISkin Skin()
        {
            var skin = GameDatabase.Instance != null ? GameDatabase.Instance.skin : null;
            return skin != null ? skin : AssetDatabase.LoadAssetAtPath<UISkin>(DeckPartyPaths.Skin);
        }

        /// <summary>Which part of the Game scene a text is in, from the objects above it.</summary>
        static string GroupInGameScene(Transform text)
        {
            Transform top = null;
            for (var t = text; t != null; t = t.parent)
            {
                switch (t.name)
                {
                    case "TopBar": return "Menu bar";
                    case "Planning": return "Shop";
                    case "SidePanel": return "Side panel";
                    case "RaidBlame": return "Deplatformed screen";
                    case "Party": return "Stream";
                }
                if (t.parent != null && t.parent.parent == null) top = t;
                if (t.parent == null && t.name == "OverlayCanvas")
                    return top != null ? "Pop-ups: " + ObjectNames.NicifyVariableName(top.name) : "Pop-ups";
            }
            return "Other";
        }

        /// <summary>The text's place under the root, root included ("GameCanvas/TopBar/Fill/Centre/Pop/Value").</summary>
        static string PathOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var x = t; x != null; x = x.parent)
            {
                parts.Insert(0, x.name);
                if (x == root) break;
            }
            return string.Join("/", parts);
        }

        /// <summary>The last few levels of its place, readable ("Centre > Pop > Value"), skipping the panels' inner fills.</summary>
        static string LabelOf(Transform t, Transform root)
        {
            var parts = new List<string>();
            for (var x = t; x != null && parts.Count < 3; x = x.parent)
            {
                if (x.name != "Fill" && x.name != "Viewport" && x.name != "Content") parts.Insert(0, ObjectNames.NicifyVariableName(x.name));
                if (x == root) break;
            }
            return string.Join(" > ", parts);
        }

        static readonly Regex s_Tags = new Regex("<[^>]*>");

        static string SampleOf(TMP_Text text)
        {
            string sample = s_Tags.Replace(text.text ?? string.Empty, string.Empty).Replace('\n', ' ').Trim();
            return sample.Length > 60 ? sample.Substring(0, 57) + "..." : sample;
        }

        static Color Opaque(Color c) => new Color(c.r, c.g, c.b, 1f);
    }
}
