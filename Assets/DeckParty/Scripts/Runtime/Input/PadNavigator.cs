using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The gamepad selector. It moves between everything a mouse could click right now (menu buttons, shop cards, viewers
    /// in chat, dialog buttons): a button counts when a click at its centre would land on it, so things under an open pop-up
    /// or hidden away are skipped without any screen having to say so. The D-pad / left stick moves it to the nearest thing
    /// in that direction, Confirm presses it (and the viewer's or button's hover pop-up shows while it's highlighted), and
    /// Back closes an open dialog. Screens with their own pad controls (the streamer select) switch it off with
    /// <see cref="PadSelfNavigation"/>.
    /// </summary>
    public static class PadNavigator
    {
        static Selectable s_Focus;
        static GameObject s_Hovered;
        static bool s_Refocus = true;
        static Vector2 s_LastPoint = new Vector2(float.NaN, float.NaN);
        static readonly List<RaycastResult> s_Hits = new List<RaycastResult>();
        static readonly List<(float score, Selectable selectable)> s_Candidates = new List<(float, Selectable)>();
        static readonly Vector3[] s_Corners = new Vector3[4];
        static PointerEventData s_Pointer;
        static EventSystem s_PointerSystem;
        static int s_SelfNavigation;

        /// <summary>The highlighted thing (null when nothing is, or the pad isn't in use).</summary>
        public static Selectable Focus => s_Focus != null && s_Focus.isActiveAndEnabled ? s_Focus : null;

        /// <summary>Where the highlighted thing is on screen (pixels), for the frame and the tooltip.</summary>
        public static Rect FocusScreenRect { get; private set; }

        /// <summary>The selector is showing (pad in use, something highlighted, not switched off or asleep).</summary>
        public static bool Showing => PadInput.Active && !Suppressed && !Paused && !Dormant && Focus != null;

        /// <summary>
        /// Asleep: nothing is highlighted or pressed until the player presses a direction, which wakes it on the screen's
        /// default thing. Each stream starts like this (the stream's buttons have their own pad buttons); the shop and the
        /// menus show the selector straight away.
        /// </summary>
        public static bool Dormant { get; private set; }

        /// <summary>Hides the selector until the next direction press (see <see cref="Dormant"/>).</summary>
        public static void Sleep()
        {
            if (Dormant) return;
            Dormant = true;
            ClearHover();
            s_Focus = null;
            s_Refocus = true;
        }

        /// <summary>Shows the selector again on the screen's default thing.</summary>
        public static void Wake()
        {
            if (!Dormant) return;
            Dormant = false;
            s_Refocus = true;
        }

        /// <summary>A screen that drives the pad itself is open (<see cref="PadSelfNavigation"/>).</summary>
        public static bool Suppressed => s_SelfNavigation > 0;

        /// <summary>Set by the game screen while the scoring sequence or a summary is up (they take Confirm and Back).</summary>
        public static bool Paused { get; set; }

        /// <summary>
        /// Optional: the game screen's idea of where the selector should start when nothing is highlighted (the first viewer
        /// with an action in a stream, the first card in the shop). Null falls back to the screen's default.
        /// </summary>
        public static Func<Selectable> DefaultProvider;

        /// <summary>The screen changed: highlight its default thing next frame.</summary>
        public static void RequestRefocus() => s_Refocus = true;

        /// <summary>Highlights <paramref name="selectable"/> (when the pad is in use).</summary>
        public static void SetFocus(Selectable selectable)
        {
            if (selectable == null) return;
            // Mouse in use: remember it for when the pad is picked up, without a fake hover.
            if (!PadInput.Active)
            {
                s_Focus = selectable;
                s_Refocus = false;
                return;
            }
            MoveTo(selectable);
            s_Refocus = false;
        }

        internal static void AddSelfNavigation() => s_SelfNavigation++;
        internal static void RemoveSelfNavigation() => s_SelfNavigation = Mathf.Max(0, s_SelfNavigation - 1);

        /// <summary>Called by <see cref="PadRunner"/> every frame, after <see cref="PadInput"/>.</summary>
        internal static void Tick()
        {
            var system = EventSystem.current;
            if (system == null)
            {
                Release();
                return;
            }
            // The selector replaces uGUI's own pad navigation (which would move and press a second, hidden selection).
            if (system.sendNavigationEvents) system.sendNavigationEvents = false;

            if (!PadInput.Active || Suppressed || Paused)
            {
                ClearHover();
                return;
            }

            // Back closes an open dialog anywhere (it counts as Cancel).
            if (ModalDialog.IsOpen && PadInput.Pressed(PadAction.Back))
            {
                PadInput.Consume(PadAction.Back);
                ModalDialog.CloseActive();
                s_Refocus = true;
                return;
            }

            // Asleep: the first direction press shows the selector on the default thing (it doesn't move yet).
            if (Dormant)
            {
                if (PadInput.Navigate == Vector2Int.zero) return;
                Dormant = false;
                s_Refocus = false;
                MoveTo(PickDefault());
                if (s_Focus != null) AudioService.PlayPitched(Sfx.Click, 1.3f, 0.35f);
                return;
            }

            if (s_Refocus || !Usable(s_Focus))
            {
                s_Refocus = false;
                MoveTo(PickDefault());
            }

            var direction = PadInput.Navigate;
            if (direction != Vector2Int.zero)
            {
                if (s_Focus is Slider slider && direction.x != 0 && slider.direction <= Slider.Direction.RightToLeft)
                    Nudge(slider, direction.x);
                else if (s_Focus == null) MoveTo(PickDefault());
                else
                {
                    var next = FindNext(s_Focus, direction);
                    if (next != null)
                    {
                        MoveTo(next);
                        AudioService.PlayPitched(Sfx.Click, 1.3f, 0.35f);
                    }
                }
            }

            ScrollWithStick();

            if (s_Focus != null && PadInput.Pressed(PadAction.Confirm) && Press(s_Focus)) PadInput.Consume(PadAction.Confirm);

            if (s_Focus != null)
            {
                FocusScreenRect = ScreenRect((RectTransform)s_Focus.transform);
                s_LastPoint = FocusScreenRect.center;
            }
        }

        // ------------------------------------------------------------------ what counts

        /// <summary>The selector can land on it: <see cref="Reachable"/> and not marked <see cref="PadSelectorSkip"/>.</summary>
        internal static bool Usable(Selectable selectable) =>
            Reachable(selectable) && !selectable.TryGetComponent<PadSelectorSkip>(out _);

        /// <summary>
        /// Active, not a scrollbar, and clickable at its centre (or inside a scroll view that is). Pad shortcuts use this too,
        /// so a button the selector skips still works from its own pad button.
        /// </summary>
        internal static bool Reachable(Selectable selectable)
        {
            if (selectable == null || !selectable.isActiveAndEnabled || !selectable.gameObject.activeInHierarchy) return false;
            if (selectable is Scrollbar) return false;
            var rect = selectable.transform as RectTransform;
            if (rect == null) return false;
            var screen = ScreenRect(rect);
            if (screen.width < 2f || screen.height < 2f) return false;

            // Inside a scroll view, a row scrolled out of sight still counts while the scroll view itself is on top.
            var scroll = selectable.GetComponentInParent<ScrollRect>();
            if (scroll != null && scroll.viewport != null && scroll.content != null && selectable.transform.IsChildOf(scroll.content))
            {
                var view = ScreenRect(scroll.viewport);
                if (!view.Contains(screen.center)) return HitsInside(view.center, scroll.viewport);
            }
            return HitsInside(screen.center, rect);
        }

        /// <summary>A click at <paramref name="screenPoint"/> lands on <paramref name="target"/> (or something inside it).</summary>
        static bool HitsInside(Vector2 screenPoint, Transform target)
        {
            var system = EventSystem.current;
            if (system == null) return false;
            if (s_Pointer == null || s_PointerSystem != system)
            {
                s_Pointer = new PointerEventData(system);
                s_PointerSystem = system;
            }
            s_Pointer.position = screenPoint;
            s_Hits.Clear();
            system.RaycastAll(s_Pointer, s_Hits);
            if (s_Hits.Count == 0) return false;
            var top = s_Hits[0].gameObject.transform;
            return top == target || top.IsChildOf(target);
        }

        // ------------------------------------------------------------------ moving

        /// <summary>The nearest usable thing in <paramref name="direction"/>: closest along it, with sideways distance counting double.</summary>
        static Selectable FindNext(Selectable from, Vector2Int direction)
        {
            var origin = ScreenRect((RectTransform)from.transform);
            var dir = new Vector2(direction.x, direction.y);
            s_Candidates.Clear();
            var all = Selectable.allSelectablesArray;
            for (int i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null || candidate == from || !candidate.IsActive() || candidate is Scrollbar) continue;
                var rect = ScreenRect((RectTransform)candidate.transform);
                var offset = rect.center - origin.center;
                float along = Vector2.Dot(offset, dir);
                if (along <= 2f) continue;
                // Sideways distance: zero while the two overlap across the direction of travel (same row or column).
                float across = direction.x != 0
                    ? Gap(origin.yMin, origin.yMax, rect.yMin, rect.yMax)
                    : Gap(origin.xMin, origin.xMax, rect.xMin, rect.xMax);
                s_Candidates.Add((along + across * 2.5f, candidate));
            }
            s_Candidates.Sort((a, b) => a.score.CompareTo(b.score));
            foreach (var (_, candidate) in s_Candidates)
                if (Usable(candidate)) return candidate;
            return null;
        }

        static float Gap(float aMin, float aMax, float bMin, float bMax) =>
            bMax < aMin ? aMin - bMax : bMin > aMax ? bMin - aMax : 0f;

        /// <summary>
        /// Where the selector starts: a screen's <see cref="PadDefaultFocus"/>, the game screen's suggestion, the thing
        /// nearest to where the selector last was, or the top-left thing on screen.
        /// </summary>
        static Selectable PickDefault()
        {
            Selectable best = null;
            int bestPriority = int.MinValue;
            foreach (var marker in PadDefaultFocus.Active)
            {
                if (marker == null || marker.priority < bestPriority) continue;
                var selectable = marker.Target;
                if (!Usable(selectable)) continue;
                best = selectable;
                bestPriority = marker.priority;
            }
            if (best != null) return best;

            if (DefaultProvider != null)
            {
                try
                {
                    var suggested = DefaultProvider();
                    if (Usable(suggested)) return suggested;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            float bestScore = float.MaxValue;
            bool haveLast = !float.IsNaN(s_LastPoint.x);
            var all = Selectable.allSelectablesArray;
            for (int i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null || !candidate.IsActive() || candidate is Scrollbar || candidate is TMP_InputField) continue;
                var rect = ScreenRect((RectTransform)candidate.transform);
                // Near where the selector last was; otherwise reading order (top first, then left).
                float score = haveLast ? (rect.center - s_LastPoint).sqrMagnitude : -rect.yMax * 4f + rect.xMin;
                if (score >= bestScore || !Usable(candidate)) continue;
                bestScore = score;
                best = candidate;
            }
            return best;
        }

        static void MoveTo(Selectable next)
        {
            if (next == s_Focus) return;
            s_Focus = next;
            if (next == null) return;
            ScrollIntoView(next);
            FocusScreenRect = ScreenRect((RectTransform)next.transform);
            s_LastPoint = FocusScreenRect.center;
            Hover(next.gameObject);
        }

        /// <summary>Confirm on the highlighted thing: what a click does. False when it can't be pressed right now.</summary>
        static bool Press(Selectable selectable)
        {
            if (selectable == null || !selectable.IsInteractable())
            {
                AudioService.Play(Sfx.Error);
                return false;
            }
            var system = EventSystem.current;
            bool handled = ExecuteEvents.Execute(selectable.gameObject, new BaseEventData(system), ExecuteEvents.submitHandler);
            // Buttons that only listen for clicks (no submit handler).
            if (!handled) handled = ExecuteEvents.Execute(selectable.gameObject, PointerAt(selectable), ExecuteEvents.pointerClickHandler);
            // A press can change the screen (a new page, a closed dialog): look again next frame.
            if (!Usable(selectable)) s_Refocus = true;
            return handled;
        }

        static void Nudge(Slider slider, int sign)
        {
            if (!slider.IsInteractable()) return;
            float step = slider.wholeNumbers ? 1f : (slider.maxValue - slider.minValue) * 0.1f;
            if (slider.direction == Slider.Direction.RightToLeft) sign = -sign;
            slider.value = Mathf.Clamp(slider.value + step * sign, slider.minValue, slider.maxValue);
        }

        // ------------------------------------------------------------------ hover (tooltips, card lift)

        static void Hover(GameObject target)
        {
            if (target == s_Hovered) return;
            ClearHover();
            s_Hovered = target;
            if (target != null) ExecuteEvents.Execute(target, PointerAt(target.GetComponent<Selectable>()), ExecuteEvents.pointerEnterHandler);
        }

        static void ClearHover()
        {
            if (s_Hovered == null) return;
            var old = s_Hovered;
            s_Hovered = null;
            if (old != null) ExecuteEvents.Execute(old, PointerAt(null), ExecuteEvents.pointerExitHandler);
        }

        static PointerEventData PointerAt(Selectable selectable)
        {
            var data = new PointerEventData(EventSystem.current);
            if (selectable != null) data.position = ScreenRect((RectTransform)selectable.transform).center;
            return data;
        }

        /// <summary>The pad stopped being used, or the scene changed: drop the highlight and its hover.</summary>
        internal static void Release()
        {
            ClearHover();
            s_Focus = null;
            s_Refocus = true;
        }

        // ------------------------------------------------------------------ scrolling

        /// <summary>Scrolls the nearest scroll view so the highlighted thing is fully in sight.</summary>
        static void ScrollIntoView(Selectable selectable)
        {
            var scroll = selectable != null ? selectable.GetComponentInParent<ScrollRect>() : null;
            if (scroll == null || scroll.viewport == null || scroll.content == null || !selectable.transform.IsChildOf(scroll.content)) return;
            var view = ScreenRect(scroll.viewport);
            var item = ScreenRect((RectTransform)selectable.transform);
            var content = ScreenRect(scroll.content);
            if (scroll.vertical && content.height > view.height + 1f)
            {
                float shift = item.yMax > view.yMax ? item.yMax - view.yMax : item.yMin < view.yMin ? item.yMin - view.yMin : 0f;
                if (shift != 0f)
                    scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + (shift + Mathf.Sign(shift) * 12f) / (content.height - view.height));
            }
            if (scroll.horizontal && content.width > view.width + 1f)
            {
                float shift = item.xMax > view.xMax ? item.xMax - view.xMax : item.xMin < view.xMin ? item.xMin - view.xMin : 0f;
                if (shift != 0f)
                    scroll.horizontalNormalizedPosition = Mathf.Clamp01(scroll.horizontalNormalizedPosition + (shift + Mathf.Sign(shift) * 12f) / (content.width - view.width));
            }
        }

        /// <summary>The right stick scrolls the scroll view the highlighted thing is in (long lists, How to Play).</summary>
        static void ScrollWithStick()
        {
            var amount = PadInput.Scroll;
            if (amount.sqrMagnitude < 0.04f) return;
            var scroll = s_Focus != null ? s_Focus.GetComponentInParent<ScrollRect>() : null;
            if (scroll == null || !scroll.vertical || scroll.content == null || scroll.viewport == null) return;
            float overflow = scroll.content.rect.height - scroll.viewport.rect.height;
            if (overflow <= 1f) return;
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + amount.y * 1400f * Time.unscaledDeltaTime / overflow);
        }

        // ------------------------------------------------------------------ geometry

        /// <summary>A rect transform's area on screen, in pixels (any canvas render mode).</summary>
        public static Rect ScreenRect(RectTransform rect)
        {
            if (rect == null) return default;
            rect.GetWorldCorners(s_Corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = null;
            if (canvas != null)
            {
                var root = canvas.rootCanvas;
                if (root.renderMode != RenderMode.ScreenSpaceOverlay) camera = root.worldCamera;
            }
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, s_Corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
