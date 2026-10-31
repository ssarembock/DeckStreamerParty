using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Multiplayer strip above the board: whose party is on screen, whose turn it is, and a button to jump between
    /// boards. Built at runtime from existing widgets, so Game scenes generated before it existed get it too.
    /// </summary>
    public sealed class ViewBarView : MonoBehaviour
    {
        const float Height = 52f;
        const float Gap = 8f;
        const float ButtonWidth = 250f;

        Image m_Background;
        TMP_Text m_Label;
        Button m_Button;
        TMP_Text m_ButtonLabel;
        Action m_OnClick;

        /// <summary>Docks a new bar to the top edge of <paramref name="board"/> and shrinks the board to make room.</summary>
        public static ViewBarView Create(RectTransform board, Button buttonTemplate, TMP_Text textTemplate, Image panelTemplate)
        {
            if (board == null || !(board.parent is RectTransform parent) || buttonTemplate == null || textTemplate == null) return null;

            var go = new GameObject("ViewBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.SetSiblingIndex(board.GetSiblingIndex() + 1);
            rect.anchorMin = new Vector2(board.anchorMin.x, board.anchorMax.y);
            rect.anchorMax = board.anchorMax;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(board.offsetMin.x, board.offsetMax.y - Height);
            rect.offsetMax = board.offsetMax;
            board.offsetMax = new Vector2(board.offsetMax.x, board.offsetMax.y - Height - Gap);

            var view = go.AddComponent<ViewBarView>();
            view.m_Background = go.GetComponent<Image>();
            if (panelTemplate != null)
            {
                view.m_Background.sprite = panelTemplate.sprite;
                view.m_Background.type = panelTemplate.type;
                view.m_Background.pixelsPerUnitMultiplier = panelTemplate.pixelsPerUnitMultiplier;
            }
            view.m_Background.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 4f);
            labelRect.offsetMax = new Vector2(-20f, -4f);
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.font = textTemplate.font;
            label.fontSize = 24f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 24f;
            label.color = UIPalette.Text;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.richText = true;
            label.raycastTarget = false;
            view.m_Label = label;

            var buttonGo = Instantiate(buttonTemplate.gameObject, rect);
            buttonGo.name = "SwitchBoard";
            buttonGo.SetActive(true);
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-6f, 0f);
            buttonRect.sizeDelta = new Vector2(ButtonWidth, Height - 10f);
            view.m_Button = buttonGo.GetComponent<Button>();
            view.m_Button.onClick.RemoveAllListeners();
            view.m_Button.onClick.AddListener(() => view.m_OnClick?.Invoke());
            view.m_ButtonLabel = buttonGo.GetComponentInChildren<TMP_Text>(true);
            if (view.m_ButtonLabel != null)
            {
                view.m_ButtonLabel.enableAutoSizing = true;
                view.m_ButtonLabel.fontSizeMin = 14f;
                view.m_ButtonLabel.fontSizeMax = 22f;
            }

            view.Set(string.Empty, UIPalette.Teal, null, null);
            return view;
        }

        /// <param name="buttonText">Null hides the button.</param>
        public void Set(string text, Color accent, string buttonText, Action onClick)
        {
            m_Label.text = text;
            m_Background.color = Color.Lerp(UIPalette.PanelDeep, accent, 0.22f).WithAlpha(0.95f);

            bool showButton = !string.IsNullOrEmpty(buttonText);
            m_Button.gameObject.SetActive(showButton);
            if (showButton && m_ButtonLabel != null) m_ButtonLabel.text = buttonText;
            m_OnClick = onClick;
            m_Label.rectTransform.offsetMax = new Vector2(showButton ? -(ButtonWidth + 18f) : -20f, -4f);
        }
    }
}
