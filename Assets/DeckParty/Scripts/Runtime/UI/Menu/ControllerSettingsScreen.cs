using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Settings > Controller: one row per gamepad action with the button it's on. Press a row (click it, or highlight it and
    /// press Confirm), then press the pad button you want. Actions that can happen at the same time swap buttons instead of
    /// sharing one, and Reset puts the original mapping back. Saved on this PC.
    /// </summary>
    public sealed class ControllerSettingsScreen : UIScreen
    {
        [Tooltip("Where the rows go (a vertical list).")]
        [SerializeField] internal RectTransform list;
        [Tooltip("One action row, kept hidden: a Label text and a Binding button holding a Glyph image and a Text.")]
        [SerializeField] internal GameObject rowTemplate;
        [Tooltip("A section title row, kept hidden: one text.")]
        [SerializeField] internal TMP_Text headerTemplate;
        [SerializeField] internal TMP_Text statusLabel;
        [SerializeField] internal Button resetButton;
        [SerializeField] internal Button backButton;

        /// <summary>Seconds to wait for a pad button before giving up.</summary>
        const float ListenSeconds = 8f;

        sealed class Row
        {
            public PadAction action;
            public TMP_Text label;
            public Button binding;
            public Image glyph;
            public TMP_Text glyphLabel;
            public TMP_Text bindingText;
        }

        MainMenuController m_Menu;
        readonly List<Row> m_Rows = new List<Row>();
        readonly List<TMP_Text> m_Headers = new List<TMP_Text>();
        int m_Listening = -1;
        float m_ListenUntil;
        bool m_Built;

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            if (rowTemplate != null) rowTemplate.SetActive(false);
            if (headerTemplate != null) headerTemplate.gameObject.SetActive(false);
            resetButton.onClick.AddListener(ResetBindings);
            backButton.onClick.AddListener(() => m_Menu.ShowSettings());
            Loc.Changed += () =>
            {
                if (IsVisible) Refresh();
            };
        }

        protected override void OnShow()
        {
            Build();
            StopListening();
            Refresh();
            if (m_Rows.Count > 0) PadNavigator.SetFocus(m_Rows[0].binding);
        }

        protected override void OnHide() => StopListening();

        void Build()
        {
            if (m_Built || rowTemplate == null) return;
            m_Built = true;
            var context = (PadContext)(-1);
            foreach (var action in PadBindings.All)
            {
                var actionContext = PadBindings.ContextOf(action);
                if (actionContext != context && headerTemplate != null)
                {
                    context = actionContext;
                    var header = Instantiate(headerTemplate, list);
                    header.gameObject.SetActive(true);
                    header.name = "Header " + context;
                    m_Headers.Add(header);
                }

                var go = Instantiate(rowTemplate, list);
                go.SetActive(true);
                go.name = "Row " + action;
                var row = new Row
                {
                    action = action,
                    label = Find<TMP_Text>(go.transform, "Label"),
                    binding = Find<Button>(go.transform, "Binding"),
                };
                if (row.binding != null)
                {
                    row.glyph = Find<Image>(row.binding.transform, "Glyph");
                    row.glyphLabel = row.glyph != null ? row.glyph.GetComponentInChildren<TMP_Text>(true) : null;
                    row.bindingText = Find<TMP_Text>(row.binding.transform, "Text");
                    int index = m_Rows.Count;
                    row.binding.onClick.AddListener(() => StartListening(index));
                }
                m_Rows.Add(row);
            }
        }

        static T Find<T>(Transform root, string name) where T : Component
        {
            foreach (var t in root.GetComponentsInChildren<T>(true))
                if (t.name == name) return t;
            return null;
        }

        void Refresh()
        {
            int header = 0;
            var context = (PadContext)(-1);
            foreach (var action in PadBindings.All)
            {
                var actionContext = PadBindings.ContextOf(action);
                if (actionContext == context || header >= m_Headers.Count) continue;
                context = actionContext;
                m_Headers[header++].text = Loc.Get("controller.section." + actionContext);
            }

            for (int i = 0; i < m_Rows.Count; i++)
            {
                var row = m_Rows[i];
                var button = PadBindings.Get(row.action);
                if (row.label != null) row.label.text = Loc.Get(PadBindings.ActionKey(row.action));
                bool listening = i == m_Listening;
                if (row.bindingText != null)
                    row.bindingText.text = listening ? $"<color=#FFC94A>{Loc.Get("controller.press_button")}</color>" : ButtonName(button);
                ApplyGlyph(row, listening ? PadButton.None : button);
            }

            if (statusLabel != null)
            {
                statusLabel.text = m_Listening >= 0
                    ? Loc.Get("controller.listening", "action", Loc.Get(PadBindings.ActionKey(m_Rows[m_Listening].action)), "seconds", Mathf.CeilToInt(m_ListenUntil - Time.unscaledTime))
                    : PadInput.Connected ? Loc.Get("controller.connected") : Loc.Get("controller.not_connected");
            }
        }

        /// <summary>The button's name in this language ("A", "Left bumper"...).</summary>
        static string ButtonName(PadButton button) => button == PadButton.None ? Loc.Get("controller.unbound") : Loc.Get("controller.button." + button);

        static void ApplyGlyph(Row row, PadButton button)
        {
            if (row.glyph == null) return;
            row.glyph.gameObject.SetActive(button != PadButton.None);
            if (button == PadButton.None) return;
            var entry = GameDatabase.Instance != null && GameDatabase.Instance.skin != null ? GameDatabase.Instance.skin.Get(PadBindings.GlyphSlot(button)) : null;
            bool art = entry != null && entry.sprite != null;
            row.glyph.sprite = art ? entry.sprite : RuntimeSprites.Circle;
            row.glyph.color = entry != null && entry.overrideColor ? entry.color : art ? Color.white : PadBindings.PlaceholderColour(button);
            row.glyph.preserveAspect = true;
            if (row.glyphLabel != null)
            {
                row.glyphLabel.gameObject.SetActive(!art);
                row.glyphLabel.text = PadBindings.ShortLabel(button);
            }
        }

        // ------------------------------------------------------------------ listening for a button

        void StartListening(int index)
        {
            if (index < 0 || index >= m_Rows.Count) return;
            if (m_Listening < 0) PadNavigator.AddSelfNavigation();
            m_Listening = index;
            m_ListenUntil = Time.unscaledTime + ListenSeconds;
            Refresh();
        }

        /// <summary>Waiting for a pad button (Esc on the keyboard cancels; every pad button is a valid answer).</summary>
        public bool Listening => m_Listening >= 0;

        public void CancelListening() => StopListening();

        void StopListening()
        {
            if (m_Listening < 0) return;
            m_Listening = -1;
            PadNavigator.RemoveSelfNavigation();
            if (IsVisible) Refresh();
        }

        void Update()
        {
            if (!IsVisible) return;
            if (m_Listening < 0)
            {
                // Keep the "controller connected" line current.
                if (statusLabel != null && Time.frameCount % 30 == 0) Refresh();
                return;
            }

            var pressed = PadInput.AnyButtonPressed();
            if (pressed != PadButton.None)
            {
                var row = m_Rows[m_Listening];
                PadBindings.Set(row.action, pressed);
                AudioService.Play(Sfx.Click);
                int done = m_Listening;
                StopListening();
                // Keep the highlight on the row just changed.
                PadNavigator.SetFocus(m_Rows[done].binding);
                // The press that set the binding must not also press something else this frame.
                foreach (var action in PadBindings.All) PadInput.Consume(action);
                return;
            }
            if (Time.unscaledTime >= m_ListenUntil)
            {
                StopListening();
                return;
            }
            if (Time.frameCount % 10 == 0) Refresh();
        }

        void ResetBindings()
        {
            ModalDialog.Show(Loc.Get("controller.reset_title"), Loc.Get("controller.reset_body"), () =>
            {
                PadBindings.ResetAll();
                Refresh();
                ToastView.Show(Loc.Get("controller.reset_done"), ToastKind.Success);
            }, Loc.Get("controller.reset_confirm"));
        }
    }
}
