using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>Project Settings > Deck Party > Online Platform, and the menu item that opens it.</summary>
    internal static class OnlinePlatformSettingsProvider
    {
        public const string SettingsPath = "Project/Deck Party/Online Platform";

        static Editor s_Editor;

        [SettingsProvider]
        static SettingsProvider Create()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Online Platform",
                guiHandler = _ =>
                {
                    var settings = OnlinePlatformSetup.GetOrCreate();
                    if (settings == null)
                    {
                        EditorGUILayout.HelpBox("The settings asset is created once Unity finishes compiling.", MessageType.Info);
                        return;
                    }
                    Editor.CreateCachedEditor(settings, typeof(OnlinePlatformSettingsEditor), ref s_Editor);
                    EditorGUILayout.Space(6);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(10);
                        using (new EditorGUILayout.VerticalScope())
                            s_Editor.OnInspectorGUI();
                        GUILayout.Space(10);
                    }
                },
                keywords = new HashSet<string> { "Steam", "Steamworks", "Epic", "Game Center", "Google Play", "multiplayer", "lobby", "invite", "platform", "App ID" },
            };
        }

        [MenuItem("Deck Party/Online Platform...", priority = 30)]
        static void Open() => SettingsService.OpenProjectSettings(SettingsPath);
    }
}
