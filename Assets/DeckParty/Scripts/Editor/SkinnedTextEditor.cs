using DeckParty.UI;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// The Skinned Text component (on every text of the game): shows the text's UI Skin > Text Colours entry and lets you set
    /// its colour right here (it's saved in the UI Skin, so it survives rebuilds), with a Show in UI Skin button.
    /// </summary>
    [CustomEditor(typeof(SkinnedText)), CanEditMultipleObjects]
    internal sealed class SkinnedTextEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("header"), new GUIContent("Header Font",
                "Uses the UI Skin's Header Font (titles and big numbers) instead of the Body Font."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colourOnly"), new GUIContent("Keep Own Font",
                "Only the colour comes from the UI Skin; this text keeps the font it has."));
            serializedObject.ApplyModifiedProperties();

            if (targets.Length > 1) return;
            var text = (SkinnedText)target;
            EditorGUILayout.Space(4);
            if (string.IsNullOrEmpty(text.ColourId))
            {
                EditorGUILayout.HelpBox("This text has no Text Colours entry. Texts of the stream and the shop get one when the scenes are " +
                                        "rebuilt (Deck Party > Setup > Rebuild Scenes and UI Prefabs).", MessageType.None);
                return;
            }

            var skin = GameDatabase.Instance.skin;
            var entry = skin != null ? skin.GetText(text.ColourId) : null;
            if (entry == null)
            {
                EditorGUILayout.HelpBox($"Not listed in UI Skin > Text Colours yet (id {text.ColourId}). Run Deck Party > Setup > Rebuild " +
                                        "Scenes and UI Prefabs.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Text colour (UI Skin)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{entry.group}: {entry.label}", EditorStyles.wordWrappedLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    bool on = EditorGUILayout.ToggleLeft(new GUIContent("Own colour", "Ticked: the colour beside it. Unticked: the game's colour."),
                        entry.overrideColor, GUILayout.Width(100));
                    if (EditorGUI.EndChangeCheck()) Set(skin, entry, on, entry.color);
                    EditorGUI.BeginChangeCheck();
                    var picked = EditorGUILayout.ColorField(GUIContent.none, entry.color, true, false, false);
                    if (EditorGUI.EndChangeCheck()) Set(skin, entry, true, picked);
                }
                EditorGUILayout.LabelField("Saved in the UI Skin, so it stays when you rebuild. Prefab texts change every copy.",
                    EditorStyles.miniLabel);
                if (GUILayout.Button(new GUIContent("Show in UI Skin", "Open the UI Skin's Text Colours on this text.")))
                    UISkinEditor.FocusText(entry.id);
            }
        }

        static void Set(UISkin skin, TextColourEntry entry, bool on, Color colour)
        {
            Undo.RecordObject(skin, "Text Colour");
            entry.overrideColor = on;
            entry.color = new Color(colour.r, colour.g, colour.b, 1f);
            EditorUtility.SetDirty(skin);
            skin.NotifyChanged();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
