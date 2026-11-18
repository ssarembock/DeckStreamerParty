using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Gives a button its own gamepad button (Add Viewer, End the Stream, Raise Sub Cap, Go Live...): pressing it presses
    /// the button, as long as the button could be clicked right now. While a pad is in use the bound button's glyph shows
    /// on a corner of the button (UI Skin > Controller sets its size, corner and art) when the player's Show controller
    /// shortcuts setting says so (Hold: while LB is held, Always, Never). Settings > Controller changes both.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class PadShortcut : MonoBehaviour
    {
        [Tooltip("The pad action that presses this button (Settings > Controller maps actions to pad buttons).")]
        public PadAction action;

        Selectable m_Selectable;
        RectTransform m_Prompt;
        Image m_PromptImage;
        TMP_Text m_PromptLabel;
        PadButton m_ShownButton = (PadButton)255;

        /// <summary>Adds (or retargets) the shortcut on <paramref name="target"/>'s button.</summary>
        public static PadShortcut Bind(Component target, PadAction action)
        {
            if (target == null || !target.TryGetComponent<Selectable>(out _)) return null;
            if (!target.TryGetComponent<PadShortcut>(out var shortcut)) shortcut = target.gameObject.AddComponent<PadShortcut>();
            shortcut.action = action;
            shortcut.m_ShownButton = (PadButton)255;
            return shortcut;
        }

        void Awake() => m_Selectable = GetComponent<Selectable>();

        void OnEnable()
        {
            PadBindings.Changed += Redraw;
            UISkin.SkinChanged += Redraw;
        }

        void OnDisable()
        {
            PadBindings.Changed -= Redraw;
            UISkin.SkinChanged -= Redraw;
            if (m_Prompt != null) m_Prompt.gameObject.SetActive(false);
        }

        void Redraw() => m_ShownButton = (PadButton)255;

        /// <summary>
        /// The player's Show controller shortcuts setting (Settings > Controller): Always, Never, or Hold = only while the
        /// Show Shortcuts pad button (LB by default) is held. Only the prompts follow it; the shortcuts always work.
        /// </summary>
        public static bool PromptsWanted
        {
            get
            {
                switch (AppSettings.PadPrompts)
                {
                    case PadPromptMode.Always: return true;
                    case PadPromptMode.Never: return false;
                    default: return PadInput.Held(PadAction.ShowShortcuts);
                }
            }
        }

        void Update()
        {
            if (!PadInput.Active || m_Selectable == null) return;
            if (!PadInput.Pressed(action) || InputHelper.IsTyping) return;
            if (!m_Selectable.IsInteractable() || !PadNavigator.Reachable(m_Selectable)) return;
            PadInput.Consume(action);
            if (m_Selectable is Button button) button.onClick.Invoke();
            else if (m_Selectable is Toggle toggle) toggle.isOn = !toggle.isOn;
            Fx.Play(FxCue.Purchase, transform, 0.6f);
        }

        void LateUpdate()
        {
            var look = PadLook.OrDefault;
            var button = PadBindings.Get(action);
            bool show = PadInput.Active && look.showPrompts && PromptsWanted && button != PadButton.None && m_Selectable != null && m_Selectable.IsActive();
            if (!show)
            {
                if (m_Prompt != null && m_Prompt.gameObject.activeSelf) m_Prompt.gameObject.SetActive(false);
                return;
            }
            EnsurePrompt();
            if (!m_Prompt.gameObject.activeSelf) m_Prompt.gameObject.SetActive(true);
            if (button != m_ShownButton) ApplyGlyph(button);
            Place(look);
            // Dim the prompt with the button when it can't be pressed.
            var colour = m_PromptImage.color;
            float alpha = m_Selectable.IsInteractable() ? 1f : 0.45f;
            if (!Mathf.Approximately(colour.a, alpha)) m_PromptImage.color = new Color(colour.r, colour.g, colour.b, alpha);
        }

        void EnsurePrompt()
        {
            if (m_Prompt != null) return;
            var go = new GameObject("PadPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            m_Prompt = (RectTransform)go.transform;
            m_Prompt.SetParent(transform, false);
            m_Prompt.SetAsLastSibling();
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            m_PromptImage = go.GetComponent<Image>();
            m_PromptImage.raycastTarget = false;
            m_PromptImage.preserveAspect = true;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(m_Prompt, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            m_PromptLabel = labelGo.GetComponent<TextMeshProUGUI>();
            var font = GetComponentInChildren<TMP_Text>(true);
            if (font != null && font != m_PromptLabel) m_PromptLabel.font = font.font;
            m_PromptLabel.alignment = TextAlignmentOptions.Center;
            m_PromptLabel.fontStyle = FontStyles.Bold;
            m_PromptLabel.color = Color.white;
            m_PromptLabel.raycastTarget = false;
            m_PromptLabel.enableAutoSizing = true;
            m_PromptLabel.fontSizeMin = 8f;
            m_PromptLabel.fontSizeMax = 24f;
            m_PromptLabel.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>The bound button's art from the UI Skin, or a coloured circle with its name.</summary>
        void ApplyGlyph(PadButton button)
        {
            m_ShownButton = button;
            var entry = GameDatabase.Instance != null && GameDatabase.Instance.skin != null ? GameDatabase.Instance.skin.Get(PadBindings.GlyphSlot(button)) : null;
            if (entry != null && entry.sprite != null)
            {
                m_PromptImage.sprite = entry.sprite;
                m_PromptImage.type = Image.Type.Simple;
                m_PromptImage.color = entry.overrideColor ? entry.color : Color.white;
                m_PromptLabel.gameObject.SetActive(false);
                return;
            }
            m_PromptImage.sprite = RuntimeSprites.Circle;
            m_PromptImage.type = Image.Type.Simple;
            m_PromptImage.color = entry != null && entry.overrideColor ? entry.color : PadBindings.PlaceholderColour(button);
            m_PromptLabel.gameObject.SetActive(true);
            m_PromptLabel.text = PadBindings.ShortLabel(button);
        }

        void Place(PadLook look)
        {
            float size = Mathf.Max(12f, look.promptSize);
            bool right = look.promptCorner == PadPromptCorner.TopRight || look.promptCorner == PadPromptCorner.BottomRight;
            bool top = look.promptCorner == PadPromptCorner.TopRight || look.promptCorner == PadPromptCorner.TopLeft;
            var anchor = new Vector2(right ? 1f : 0f, top ? 1f : 0f);
            if (m_Prompt.anchorMin != anchor)
            {
                m_Prompt.anchorMin = m_Prompt.anchorMax = anchor;
                m_Prompt.pivot = new Vector2(0.5f, 0.5f);
            }
            float inset = size * 0.5f - look.promptOverhang;
            var position = new Vector2(right ? -inset : inset, top ? -inset : inset);
            if (m_Prompt.anchoredPosition != position) m_Prompt.anchoredPosition = position;
            var sizeDelta = new Vector2(size, size);
            if (m_Prompt.sizeDelta != sizeDelta) m_Prompt.sizeDelta = sizeDelta;
        }
    }
}
