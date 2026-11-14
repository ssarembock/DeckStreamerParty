using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// An <see cref="OptionalColour"/> on one line: a tick and a colour box. Picking a colour ticks it (a greyed-out box would
    /// open the picker and then throw the colour away); unticked, the game's own colour applies.
    /// </summary>
    [CustomPropertyDrawer(typeof(OptionalColour))]
    internal sealed class OptionalColourDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var use = property.FindPropertyRelative("use");
            var colour = property.FindPropertyRelative("colour");
            EditorGUI.BeginProperty(position, label, property);
            var field = EditorGUI.PrefixLabel(position, label);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var tick = new Rect(field.x, field.y, 18f, field.height);
            var box = new Rect(field.x + 22f, field.y, Mathf.Max(40f, field.width - 74f), field.height);
            var note = new Rect(box.xMax + 4f, field.y, 48f, field.height);

            use.boolValue = EditorGUI.Toggle(tick, GUIContent.none, use.boolValue);
            EditorGUI.BeginChangeCheck();
            var picked = EditorGUI.ColorField(box, GUIContent.none, colour.colorValue, true, true, false);
            if (EditorGUI.EndChangeCheck())
            {
                colour.colorValue = picked;
                use.boolValue = true;
            }
            EditorGUI.LabelField(note, use.boolValue ? "own" : "game's", EditorStyles.miniLabel);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
