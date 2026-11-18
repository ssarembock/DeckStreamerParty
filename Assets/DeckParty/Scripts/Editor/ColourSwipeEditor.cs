using DeckParty.UI;
using UnityEditor;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// The Colour Swipe's inspector: only the settings that matter (the UI Graphic's Material and Raycast fields are hidden:
    /// the swipe makes its own material and never takes clicks). Changes show live in the prefab editor.
    /// </summary>
    [CustomEditor(typeof(ColourSwipe)), CanEditMultipleObjects]
    internal sealed class ColourSwipeEditor : Editor
    {
        static readonly string[] s_Hidden =
        {
            "m_Script", "m_Material", "m_RaycastTarget", "m_RaycastPadding", "m_OnCullStateChanged", "m_Maskable",
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox(
                "A swipe of colour across this object's rect: a band that crosses it (a shine), or a wipe that fills it, holds and " +
                "fades. Make the object cover the card (or the part to swipe) above the card's content. Show When picks the cards " +
                "it's on (Viewer Has Action: only viewers with an action); Play Mode picks when it plays. While you edit the prefab " +
                "it loops so you can see it (turn on Always Refresh in the Scene view to watch it move). Colour (below) tints the " +
                "whole effect.",
                MessageType.None);
            DrawPropertiesExcluding(serializedObject, s_Hidden);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
