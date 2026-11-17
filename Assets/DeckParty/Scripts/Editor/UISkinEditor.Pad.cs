using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// UI Skin > Controller (2026-11-17): how the gamepad selector frame and the button prompts look. Their art (the frame and
    /// one glyph per pad button) is in the Controller (gamepad) entries further down. Applied live while playing.
    /// </summary>
    internal sealed partial class UISkinEditor
    {
        const string PadHelp =
            "How the gamepad selector and the button prompts look while a controller is in use. The art is in the Controller " +
            "(gamepad) entries below: the selector frame and one glyph per pad button (without art the prompts are coloured " +
            "circles with the button's name). Players change which pad button does what in Settings > Controller.";

        bool m_ShowPad;

        void DrawPad()
        {
            var pad = serializedObject.FindProperty("pad");
            if (pad == null) return;
            m_ShowPad = EditorGUILayout.Foldout(m_ShowPad, "Controller (selector frame, button prompts)", true, EditorStyles.foldoutHeader);
            if (!m_ShowPad) return;
            EditorGUILayout.HelpBox(PadHelp, MessageType.None);

            EditorGUILayout.LabelField("Selector frame", EditorStyles.boldLabel);
            Field(pad, "focusPadding", "Padding", "Space between the highlighted thing and the frame, in pixels.");
            Field(pad, "focusColour", "Colour", "Used when the Selector frame entry has no Override Colour.");
            Field(pad, "focusPulse", "Breathing", "How much the frame grows and shrinks (0 = still).");
            Field(pad, "focusPulseSpeed", "Breaths per second", null);
            Field(pad, "focusGlide", "Glide time", "Seconds the frame takes to move to the next thing (0 = jump).");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Button prompts", EditorStyles.boldLabel);
            Field(pad, "showPrompts", "Show prompts", "The bound pad button on the stream and shop buttons.");
            Field(pad, "promptSize", "Size", "In pixels.");
            Field(pad, "promptCorner", "Corner", "Which corner of the button the prompt sits on.");
            Field(pad, "promptOverhang", "Overhang", "How far the prompt hangs over the button's edge, in pixels.");

            EditorGUILayout.Space(4);
            Field(pad, "hideCursor", "Hide mouse cursor", "While the controller is in use (the cursor comes back when the mouse moves).");
        }

        static void Field(SerializedProperty parent, string name, string label, string tooltip)
        {
            var property = parent.FindPropertyRelative(name);
            if (property != null) EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
        }
    }
}
