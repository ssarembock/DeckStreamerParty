using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// A dropdown for [AchievementId] string fields (unlock conditions in the Game Database): None, then every achievement in
    /// the Achievement Library by name and id. An id that no longer exists shows in red; such a condition counts as met.
    /// </summary>
    [CustomPropertyDrawer(typeof(AchievementIdAttribute))]
    internal sealed class AchievementIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "[AchievementId] needs a string field.");
                return;
            }
            var none = ((AchievementIdAttribute)attribute).noneLabel;
            Popup(position, property, label, none);
        }

        /// <summary>Draws the dropdown for an achievement id property (also used by the Game Database's emote list).</summary>
        public static void Popup(Rect position, SerializedProperty property, GUIContent label, string noneLabel = "None (unlocked from the start)")
        {
            var ids = new List<string> { string.Empty };
            var names = new List<string> { noneLabel };
            foreach (var definition in Achievements.All)
            {
                if (definition == null || string.IsNullOrEmpty(definition.id)) continue;
                ids.Add(definition.id);
                names.Add($"{Achievements.Name(definition)}  ({definition.id})");
            }

            string current = property.stringValue ?? string.Empty;
            int index = ids.IndexOf(current);
            bool missing = index < 0;
            if (missing)
            {
                ids.Add(current);
                names.Add($"MISSING: {current} (counts as met)");
                index = ids.Count - 1;
            }

            var previous = GUI.color;
            if (missing) GUI.color = new Color(1f, 0.55f, 0.55f);
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(position, label.text, index, names.ToArray());
            if (EditorGUI.EndChangeCheck()) property.stringValue = ids[picked];
            EditorGUI.EndProperty();
            GUI.color = previous;
        }
    }
}
