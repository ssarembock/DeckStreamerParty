using System.Collections.Generic;
using DeckParty.UI;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// The Skinned Image component (on every UI image that takes its art from the UI Skin): shows which UI Skin entry it uses
    /// by name, with where that entry shows, and an Edit in UI Skin button that opens the skin on that entry.
    /// </summary>
    [CustomEditor(typeof(SkinnedImage)), CanEditMultipleObjects]
    internal sealed class SkinnedImageEditor : Editor
    {
        static string[] s_Names;
        static List<SkinSlot> s_Slots;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureLists();

            var slot = serializedObject.FindProperty("slot");
            int index = s_Slots.IndexOf((SkinSlot)slot.intValue);
            EditorGUI.showMixedValue = slot.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup(new GUIContent("UI Skin Entry", "The UI Skin entry this image takes its sprite from."), index, s_Names);
            if (EditorGUI.EndChangeCheck() && index >= 0) slot.intValue = (int)s_Slots[index];
            EditorGUI.showMixedValue = false;

            if (!slot.hasMultipleDifferentValues)
            {
                var info = SkinSlotGuide.Get((SkinSlot)slot.intValue);
                EditorGUILayout.HelpBox($"{info.Name} ({SkinSlotGuide.Title(info.Area)}): {info.Where}", MessageType.None);
                if (GUILayout.Button(new GUIContent("Edit in UI Skin", "Open the UI Skin on this entry to change its art.")))
                    UISkinEditor.Focus(info.Slot);
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tintPlaceholderOnly"), new GUIContent("Tint Placeholder Only",
                "Icons: only the built-in white placeholder is tinted with Placeholder Tint; your own art shows in its own colours " +
                "(unless the UI Skin entry overrides the colour)."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("placeholderTint"), new GUIContent("Placeholder Tint",
                "The colour of the built-in white placeholder shape."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("siteText"), new GUIContent("Use Panel Text Colour",
                "The placeholder takes the text colour of the panel it sits in (UI Skin > Site Colours) instead of Placeholder Tint."));
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>The entries in the UI Skin inspector's order, labelled "Section: name".</summary>
        static void EnsureLists()
        {
            if (s_Names != null) return;
            s_Slots = new List<SkinSlot>();
            var names = new List<string>();
            foreach (var info in SkinSlotGuide.All)
            {
                s_Slots.Add(info.Slot);
                names.Add($"{SkinSlotGuide.Title(info.Area)}: {info.Name}");
            }
            // Any slot the guide doesn't describe yet still shows up.
            foreach (SkinSlot value in System.Enum.GetValues(typeof(SkinSlot)))
            {
                if (s_Slots.Contains(value)) continue;
                s_Slots.Add(value);
                names.Add(SkinSlotGuide.Get(value).Name);
            }
            s_Names = names.ToArray();
        }
    }
}
