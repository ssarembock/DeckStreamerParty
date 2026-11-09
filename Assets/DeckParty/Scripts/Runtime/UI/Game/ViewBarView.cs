using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Multiplayer strip above the board: whose party is on screen, whose turn it is, and a button to jump between
    /// boards. Built at runtime, so Game scenes generated before it existed get it too. Light, like the rest of the site.
    /// </summary>
    public sealed class ViewBarView : MonoBehaviour
    {
        const float Height = 52f;
        const float Gap = 8f;
        const float ButtonWidth = 250f;

        Image m_Background;
        RectTransform m_Board;
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
            view.m_Board = board;
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
            label.color = SiteColours.Get(SiteColourRole.Text);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.richText = true;
            label.raycastTarget = false;
            view.m_Label = label;

            // The button is built here (the HUD's buttons are icon-only now, so they can't serve as a text-button template).
            var buttonGo = new GameObject("SwitchBoard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.SetParent(rect, false);
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-6f, 0f);
            buttonRect.sizeDelta = new Vector2(ButtonWidth, Height - 10f);
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.sprite = RuntimeSprites.Rounded;
            buttonImage.type = Image.Type.Sliced;
            buttonImage.color = SitePalette.Outline;
            view.m_Button = buttonGo.GetComponent<Button>();
            view.m_Button.targetGraphic = buttonImage;
            var navigation = view.m_Button.navigation;
            navigation.mode = Navigation.Mode.None;
            view.m_Button.navigation = navigation;
            view.m_Button.onClick.AddListener(() => view.m_OnClick?.Invoke());
            buttonGo.AddComponent<ButtonFeedback>();

            var buttonLabelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var buttonLabelRect = (RectTransform)buttonLabelGo.transform;
            buttonLabelRect.SetParent(buttonRect, false);
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = new Vector2(10f, 2f);
            buttonLabelRect.offsetMax = new Vector2(-10f, -2f);
            view.m_ButtonLabel = buttonLabelGo.GetComponent<TextMeshProUGUI>();
            view.m_ButtonLabel.font = textTemplate.font;
            view.m_ButtonLabel.color = Color.white;
            view.m_ButtonLabel.alignment = TextAlignmentOptions.Center;
            view.m_ButtonLabel.fontStyle = FontStyles.Bold;
            view.m_ButtonLabel.raycastTarget = false;
            view.m_ButtonLabel.enableAutoSizing = true;
            view.m_ButtonLabel.fontSizeMin = 14f;
            view.m_ButtonLabel.fontSizeMax = 22f;

            view.Set(string.Empty, UIPalette.Teal, null, null);
            return view;
        }

        /// <summary>Follows the board's width (the content frame widens it when the side panel is empty).</summary>
        void LateUpdate()
        {
            if (m_Board == null) return;
            var rect = (RectTransform)transform;
            if (Mathf.Approximately(rect.offsetMin.x, m_Board.offsetMin.x) && Mathf.Approximately(rect.offsetMax.x, m_Board.offsetMax.x)) return;
            rect.offsetMin = new Vector2(m_Board.offsetMin.x, rect.offsetMin.y);
            rect.offsetMax = new Vector2(m_Board.offsetMax.x, rect.offsetMax.y);
        }

        /// <param name="buttonText">Null hides the button.</param>
        public void Set(string text, Color accent, string buttonText, Action onClick)
        {
            m_Label.text = text;
            // A light notice strip in the site's look, tinted with the player's colour.
            m_Background.color = Color.Lerp(SiteColours.Get(SiteColourRole.PanelFill, transform), accent, 0.18f);
            m_Label.color = SiteColours.Get(SiteColourRole.Text, transform);

            bool showButton = !string.IsNullOrEmpty(buttonText);
            m_Button.gameObject.SetActive(showButton);
            if (showButton && m_ButtonLabel != null) m_ButtonLabel.text = buttonText;
            m_OnClick = onClick;
            m_Label.rectTransform.offsetMax = new Vector2(showButton ? -(ButtonWidth + 18f) : -20f, -4f);
        }
    }
}
