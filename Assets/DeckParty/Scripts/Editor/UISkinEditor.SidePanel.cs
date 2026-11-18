using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// UI Skin > Players and emotes (2026-11-18): the colours inside multiplayer's left column, the player rows and the emote
    /// buttons (UISkin.sidePanel, <see cref="SidePanelLook"/>). The cards around them are Site Colours > Players and Emotes;
    /// the rows' texts are Text Colours > Player rows. Applied live while playing.
    /// </summary>
    internal sealed partial class UISkinEditor
    {
        const string SidePanelHelp =
            "The colours inside the left column in multiplayer: each player's row in the Players card, and the emote buttons. " +
            "The cards themselves (inside, border, header) are Site Colours > Players and Emotes above. The texts in the rows " +
            "(name, status, rank) are Text Colours > Player rows. Changes show straight away, also while playing. The lobby's " +
            "player rows keep the menus' dark look.";

        bool m_ShowSidePanel;

        void DrawSidePanel()
        {
            var look = serializedObject.FindProperty("sidePanel");
            if (look == null) return;
            m_ShowSidePanel = EditorGUILayout.Foldout(m_ShowSidePanel, "Players and emotes (multiplayer's left column)", true, EditorStyles.foldoutHeader);
            if (!m_ShowSidePanel) return;
            EditorGUILayout.HelpBox(SidePanelHelp, MessageType.None);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Player rows", EditorStyles.boldLabel);
            Field(look, "rowColour", "Row", "The background of each player's row.");
            Field(look, "ownRowTint", "Your row: lean to your colour", "How far your own row leans towards your player colour (0 = like the others).");
            Field(look, "ownRowColour", "Your row: fixed colour", "Tick to give your own row this colour instead of the lean.");
            Field(look, "watchedRowTint", "On screen: lean to their colour", "How far the row of the stream on screen leans towards that player's colour.");
            Field(look, "watchedRowColour", "On screen: fixed colour", "Tick to give the row of the stream on screen this colour instead.");
            Field(look, "watchedOutline", "On screen: outline", "Outline the row of the stream on screen.");
            Field(look, "watchedOutlineColour", "Outline: fixed colour", "Tick for a fixed outline colour (unticked: that player's colour).");
            Field(look, "rowHoverTint", "Hover tint", "Multiplies the row's colour while the pointer is over it (white = no change).");
            Field(look, "rowPressedTint", "Pressed tint", "Multiplies the row's colour while it's pressed (white = no change).");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Emote buttons", EditorStyles.boldLabel);
            Field(look, "emoteColour", "Button", "The background of each emote button.");
            Field(look, "emoteHoverTint", "Hover tint", "Multiplies the button's colour while the pointer is over it (white = no change).");
            Field(look, "emotePressedTint", "Pressed tint", "Multiplies the button's colour while it's pressed (white = no change).");
            Field(look, "lockedEmoteAlpha", "Locked: opacity", "How see-through a locked emote is (its button and picture).");
            Field(look, "emoteArtTint", "Picture tint", "Multiplies the emote pictures (white = their own colours).");
            Field(look, "emoteTextColour", "Text emote colour", "The colour of text emotes (shown until an emote has a picture).");
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }

            EditorGUILayout.Space(2);
            if (GUILayout.Button("Reset Players and emotes to the built-in look"))
            {
                var skin = (UISkin)target;
                Undo.RecordObject(skin, "Reset Players and emotes");
                skin.sidePanel = new SidePanelLook();
                EditorUtility.SetDirty(skin);
                serializedObject.Update();
                skin.NotifyChanged();
            }
        }
    }
}
