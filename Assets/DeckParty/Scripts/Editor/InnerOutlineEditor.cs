using DeckParty.UI;
using UnityEditor;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// The Inner Outline's inspector: only the settings that matter (the UI Graphic's Material and Raycast fields are
    /// hidden: the outline makes its own material and never takes clicks). Changes show live in the prefab editor.
    /// </summary>
    [CustomEditor(typeof(InnerOutline)), CanEditMultipleObjects]
    internal sealed class InnerOutlineEditor : Editor
    {
        static readonly string[] s_Hidden =
        {
            "m_Script", "m_Material", "m_RaycastTarget", "m_RaycastPadding", "m_OnCullStateChanged", "m_Maskable",
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "An inner outline along this object's edge, with rounded corners and an optional glow and pulse. Make the object " +
                "cover the card (or its frame) and sit above the card's content. On a shop card set to \"Viewer Has Action\", " +
                "it lights up only on viewers that have an action. Sizes are in card pixels. Colour (below) tints the whole effect.",
                MessageType.None);
            DrawPropertiesExcluding(serializedObject, s_Hidden);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
