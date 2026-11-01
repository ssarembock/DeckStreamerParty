using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// What the passive players see of the active player's intent, in the active player's colour: an outline and name tag
    /// on whatever they point at (cards, shop rows, buttons), the targets they've picked, an arrow from the acting viewer
    /// to the hovered target, and (optional) their mouse pointer. Lives on a full-screen RectTransform above the board.
    /// </summary>
    public sealed class PresenceView : MonoBehaviour
    {
        sealed class Marker
        {
            public RectTransform Rect;
            public Image Fill;
            public Outline Border;
            public RectTransform Target;
        }

        RectTransform m_Root;
        readonly List<Marker> m_Markers = new List<Marker>();
        RectTransform m_Tag;
        TMP_Text m_TagLabel;
        RectTransform m_TagTarget;
        Image m_Arrow;
        RectTransform m_ArrowFrom;
        RectTransform m_ArrowTo;
        RectTransform m_Cursor;
        Image m_CursorImage;
        RectTransform m_Board;
        Vector2 m_CursorTarget;
        bool m_CursorVisible;
        Color m_Color = Color.white;
        bool m_Built;

        void Build()
        {
            if (m_Built) return;
            m_Built = true;
            m_Root = (RectTransform)transform;

            m_Arrow = UiFactory.Image("TargetArrow", m_Root, Color.white, RuntimeSprites.Arrow);
            m_Arrow.rectTransform.pivot = new Vector2(0f, 0.5f);
            m_Arrow.gameObject.SetActive(false);

            m_Tag = UiFactory.Rect("NameTag", m_Root);
            m_Tag.sizeDelta = new Vector2(260f, 34f);
            var tagBg = UiFactory.Image("Background", m_Tag, Color.black.WithAlpha(0.75f), RuntimeSprites.Rounded, true);
            UiFactory.Stretch(tagBg.rectTransform);
            m_TagLabel = UiFactory.Text("Label", m_Tag, 20f, Color.white);
            UiFactory.Stretch(m_TagLabel.rectTransform, 4f);
            m_Tag.gameObject.SetActive(false);

            m_Cursor = UiFactory.Rect("GhostCursor", m_Root);
            m_Cursor.sizeDelta = new Vector2(26f, 26f);
            m_CursorImage = UiFactory.Image("Pointer", m_Cursor, Color.white, RuntimeSprites.Circle);
            UiFactory.Stretch(m_CursorImage.rectTransform);
            m_Cursor.gameObject.SetActive(false);
        }

        public void Clear()
        {
            Build();
            foreach (var marker in m_Markers) marker.Rect.gameObject.SetActive(false);
            m_Tag.gameObject.SetActive(false);
            m_Arrow.gameObject.SetActive(false);
            m_Cursor.gameObject.SetActive(false);
            m_TagTarget = null;
            m_ArrowFrom = m_ArrowTo = null;
            m_CursorVisible = false;
        }

        /// <param name="card">Finds the card showing a viewer id (chat, picker).</param>
        /// <param name="shopRow">Finds the shop row for a viewer type.</param>
        /// <param name="button">Finds a control (Add Viewer, End the Stream...).</param>
        /// <param name="board">The area the ghost cursor's normalized position maps onto.</param>
        public void Render(PresenceMessage msg, Color color, string playerName, Func<int, RectTransform> card,
            Func<GuestType, RectTransform> shopRow, Func<PresenceButton, RectTransform> button, RectTransform board)
        {
            Clear();
            if (!FxLibrary.Current.presenceSignals) return;
            m_Color = color;
            m_Board = board;
            int used = 0;

            RectTransform hovered = null;
            switch (msg.Kind)
            {
                case PresenceKind.HoverCard:
                    hovered = card(msg.A);
                    break;
                case PresenceKind.HoverShop:
                    hovered = shopRow((GuestType)msg.A);
                    break;
                case PresenceKind.HoverButton:
                    hovered = button((PresenceButton)msg.A);
                    break;
                case PresenceKind.Targeting:
                case PresenceKind.Picking:
                {
                    var actor = msg.A >= 0 ? card(msg.A) : null;
                    if (actor != null) Mark(actor, used++, 0.18f);
                    foreach (var pick in new[] { msg.Pick0, msg.Pick1, msg.Pick2 })
                    {
                        var picked = pick >= 0 ? card(pick) : null;
                        if (picked != null) Mark(picked, used++, 0.45f);
                    }
                    hovered = msg.B >= 0 ? card(msg.B) : null;
                    if (actor != null && hovered != null && actor != hovered)
                    {
                        m_ArrowFrom = actor;
                        m_ArrowTo = hovered;
                        m_Arrow.color = color.WithAlpha(0.85f);
                        m_Arrow.gameObject.SetActive(true);
                    }
                    break;
                }
                case PresenceKind.Choosing:
                case PresenceKind.ChoosingRival:
                    hovered = msg.A >= 0 ? card(msg.A) : button(PresenceButton.Power);
                    break;
            }

            if (hovered != null)
            {
                Mark(hovered, used++, 0.28f);
                m_TagTarget = hovered;
                m_TagLabel.text = GuestVisuals.Colored(playerName, color);
                m_Tag.gameObject.SetActive(true);
            }

            if (FxLibrary.Current.ghostCursor && msg.CursorX >= 0 && msg.CursorY >= 0 && board != null)
            {
                m_CursorTarget = new Vector2(msg.CursorX / 1000f, msg.CursorY / 1000f);
                if (!m_CursorVisible) m_Cursor.anchoredPosition = BoardToLocal(m_CursorTarget);
                m_CursorVisible = true;
                m_CursorImage.color = color.WithAlpha(0.9f);
                m_Cursor.gameObject.SetActive(true);
            }
        }

        void Mark(RectTransform target, int index, float fillAlpha)
        {
            while (m_Markers.Count <= index)
            {
                var rect = UiFactory.Rect("Marker", m_Root);
                rect.SetAsFirstSibling();
                var fill = UiFactory.Image("Fill", rect, Color.white, RuntimeSprites.Rounded, true);
                UiFactory.Stretch(fill.rectTransform);
                var border = fill.gameObject.AddComponent<Outline>();
                border.effectDistance = new Vector2(4f, -4f);
                m_Markers.Add(new Marker { Rect = rect, Fill = fill, Border = border });
            }
            var marker = m_Markers[index];
            marker.Target = target;
            marker.Fill.color = m_Color.WithAlpha(fillAlpha);
            marker.Border.effectColor = m_Color;
            marker.Rect.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            if (!m_Built) return;
            foreach (var marker in m_Markers)
            {
                if (!marker.Rect.gameObject.activeSelf) continue;
                if (marker.Target == null || !marker.Target.gameObject.activeInHierarchy)
                {
                    marker.Rect.gameObject.SetActive(false);
                    continue;
                }
                Cover(marker.Rect, marker.Target, 8f);
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 5f);
                marker.Border.effectColor = m_Color.WithAlpha(pulse);
            }

            if (m_TagTarget != null && m_TagTarget.gameObject.activeInHierarchy)
            {
                var corners = new Vector3[4];
                m_TagTarget.GetWorldCorners(corners);
                var top = (corners[1] + corners[2]) * 0.5f;
                m_Tag.anchoredPosition = (Vector2)m_Root.InverseTransformPoint(top) + new Vector2(0f, 26f);
            }
            else if (m_Tag.gameObject.activeSelf) m_Tag.gameObject.SetActive(false);

            if (m_ArrowFrom != null && m_ArrowTo != null && m_Arrow.gameObject.activeSelf)
            {
                Vector2 a = m_Root.InverseTransformPoint(m_ArrowFrom.position);
                Vector2 b = m_Root.InverseTransformPoint(m_ArrowTo.position);
                var delta = b - a;
                var rect = m_Arrow.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = a;
                rect.sizeDelta = new Vector2(delta.magnitude, 28f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }

            if (m_CursorVisible && m_Board != null)
                m_Cursor.anchoredPosition = Vector2.Lerp(m_Cursor.anchoredPosition, BoardToLocal(m_CursorTarget), 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        }

        Vector2 BoardToLocal(Vector2 normalized)
        {
            var corners = new Vector3[4];
            m_Board.GetWorldCorners(corners);
            var world = Vector3.Lerp(Vector3.Lerp(corners[0], corners[3], normalized.x), Vector3.Lerp(corners[1], corners[2], normalized.x), normalized.y);
            return m_Root.InverseTransformPoint(world);
        }

        void Cover(RectTransform rect, RectTransform target, float pad)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 min = m_Root.InverseTransformPoint(corners[0]);
            Vector2 max = m_Root.InverseTransformPoint(corners[2]);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (min + max) * 0.5f;
            rect.sizeDelta = (max - min) + Vector2.one * pad * 2f;
        }

        /// <summary>Turns a screen point into the board's 0-1000 space for the ghost cursor (negative when outside).</summary>
        public static void BoardCursor(RectTransform board, Vector2 screenPoint, out short x, out short y)
        {
            x = y = -1;
            if (board == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(board, screenPoint, null, out var local)) return;
            var r = board.rect;
            float nx = (local.x - r.xMin) / Mathf.Max(1f, r.width);
            float ny = (local.y - r.yMin) / Mathf.Max(1f, r.height);
            if (nx < 0f || nx > 1f || ny < 0f || ny > 1f) return;
            x = (short)Mathf.RoundToInt(nx * 1000f);
            y = (short)Mathf.RoundToInt(ny * 1000f);
        }
    }
}
