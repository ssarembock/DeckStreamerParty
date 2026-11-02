using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DeckParty.UI
{
    /// <summary>
    /// Names the resource and community icons that sit inside text: hover one and the tooltip says what it is (above any
    /// tooltip the text's card or button already shows). Added automatically next to the <see cref="TooltipView"/>.
    /// </summary>
    public sealed class InlineIconTooltips : MonoBehaviour
    {
        /// <summary>How many of the topmost UI hits are searched for text.</summary>
        const int HitsToSearch = 3;

        readonly List<RaycastResult> m_Hits = new List<RaycastResult>();
        readonly List<TMP_Text> m_Texts = new List<TMP_Text>();
        PointerEventData m_Pointer;
        Vector2 m_LastPosition = new Vector2(-9999f, -9999f);
        float m_NextCheck;
        string m_ShownId;

        void OnDisable()
        {
            TooltipView.ClearOverride(this);
            m_ShownId = null;
        }

        void Update()
        {
            var events = EventSystem.current;
            if (events == null) return;

            var position = InputHelper.PointerPosition;
            bool moved = (position - m_LastPosition).sqrMagnitude > 0.25f;
            if (!moved && Time.unscaledTime < m_NextCheck) return;
            m_LastPosition = position;
            m_NextCheck = Time.unscaledTime + 0.2f;

            string id = FindIcon(events, position);
            if (id == m_ShownId) return;
            m_ShownId = id;

            string tip = id != null ? GameIcons.Tooltip(id, CommunityVisuals.LiveState) : null;
            if (tip != null) TooltipView.ShowOverride(tip, this);
            else TooltipView.ClearOverride(this);
        }

        string FindIcon(EventSystem events, Vector2 position)
        {
            if (m_Pointer == null) m_Pointer = new PointerEventData(events);
            m_Pointer.position = position;
            m_Hits.Clear();
            events.RaycastAll(m_Pointer, m_Hits);

            for (int h = 0; h < m_Hits.Count && h < HitsToSearch; h++)
            {
                var hit = m_Hits[h].gameObject;
                if (hit == null) continue;
                m_Texts.Clear();
                hit.GetComponentsInChildren(false, m_Texts);
                foreach (var text in m_Texts)
                {
                    var info = text.textInfo;
                    if (info == null || info.spriteCount == 0) continue;
                    var camera = CameraFor(text);
                    if (!RectTransformUtility.RectangleContainsScreenPoint(text.rectTransform, position, camera)) continue;

                    int index = TMP_TextUtilities.FindIntersectingCharacter(text, position, camera, true);
                    if (index < 0 || index >= info.characterCount) continue;
                    var character = info.characterInfo[index];
                    if (character.elementType != TMP_TextElementType.Sprite) continue;
                    if (character.textElement is TMP_SpriteCharacter sprite && sprite.textAsset != null && sprite.textAsset.name == GameIcons.AssetName)
                        return sprite.name;
                }
            }
            return null;
        }

        static Camera CameraFor(TMP_Text text)
        {
            var canvas = text.canvas;
            if (canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.rootCanvas.worldCamera;
        }
    }
}
