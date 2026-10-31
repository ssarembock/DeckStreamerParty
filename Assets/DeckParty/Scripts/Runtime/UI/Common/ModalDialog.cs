using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Confirm / message / multiple-choice dialog. One per scene; call the static <see cref="Show"/> or <see cref="ShowChoice"/>.</summary>
    public sealed class ModalDialog : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text messageLabel;
        [SerializeField] internal Button confirmButton;
        [SerializeField] internal TMP_Text confirmLabel;
        [SerializeField] internal Button cancelButton;
        [SerializeField] internal TMP_Text cancelLabel;

        static ModalDialog s_Active;
        Action m_OnConfirm;
        Action m_OnCancel;
        Action<int> m_OnChoice;

        // Extra option buttons for ShowChoice, cloned from the confirm button on demand.
        readonly List<Button> m_ChoiceButtons = new List<Button>();

        public static bool IsOpen => s_Active != null && s_Active.root.activeSelf;

        void Awake()
        {
            s_Active = this;
            root.SetActive(false);
            confirmButton.onClick.AddListener(() => Choose(0));
            cancelButton.onClick.AddListener(() => Close(false));
        }

        void OnDestroy()
        {
            if (s_Active == this) s_Active = null;
        }

        /// <summary>Shows a dialog. Pass a null <paramref name="cancel"/> label for a single-button message.</summary>
        public static void Show(string title, string message, Action onConfirm, string confirm = "OK", string cancel = "Cancel", Action onCancel = null)
        {
            if (s_Active == null)
            {
                Debug.LogWarning($"[DeckParty] No ModalDialog in scene; auto-confirming '{title}'.");
                onConfirm?.Invoke();
                return;
            }
            s_Active.Open(title, message, onConfirm, confirm, cancel, onCancel);
        }

        /// <summary>
        /// Shows one button per option. <paramref name="enabled"/> (optional) greys out options that can't be picked.
        /// <paramref name="onCancel"/> runs if the player backs out.
        /// </summary>
        public static void ShowChoice(string title, string message, IReadOnlyList<string> options, Action<int> onChoice,
            Action onCancel = null, IReadOnlyList<bool> enabled = null)
        {
            if (s_Active == null || options == null || options.Count == 0)
            {
                Debug.LogWarning($"[DeckParty] No ModalDialog in scene; picking the first option for '{title}'.");
                onChoice?.Invoke(0);
                return;
            }
            s_Active.OpenChoice(title, message, options, onChoice, onCancel, enabled);
        }

        public static void CloseActive()
        {
            if (IsOpen) s_Active.Close(false);
        }

        void Open(string title, string message, Action onConfirm, string confirm, string cancel, Action onCancel)
        {
            HideChoices();
            m_OnConfirm = onConfirm;
            m_OnCancel = onCancel;
            m_OnChoice = null;
            titleLabel.text = title;
            messageLabel.text = message;
            confirmLabel.text = confirm;
            confirmButton.interactable = true;
            cancelButton.gameObject.SetActive(!string.IsNullOrEmpty(cancel));
            if (!string.IsNullOrEmpty(cancel)) cancelLabel.text = cancel;
            Present();
        }

        void OpenChoice(string title, string message, IReadOnlyList<string> options, Action<int> onChoice, Action onCancel, IReadOnlyList<bool> enabled)
        {
            HideChoices();
            m_OnConfirm = null;
            m_OnCancel = onCancel;
            m_OnChoice = onChoice;
            titleLabel.text = title;
            messageLabel.text = message;

            for (int i = 0; i < options.Count; i++)
            {
                var button = OptionButton(i);
                button.gameObject.SetActive(true);
                button.interactable = enabled == null || i >= enabled.Count || enabled[i];
                var text = i == 0 ? confirmLabel : button.GetComponentInChildren<TMP_Text>(true);
                if (text != null)
                {
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 14;
                    text.text = options[i];
                }
            }

            cancelButton.gameObject.SetActive(true);
            cancelLabel.text = "Cancel";
            cancelButton.transform.SetAsFirstSibling();
            Present();
        }

        Button OptionButton(int index)
        {
            if (index == 0) return confirmButton;
            while (m_ChoiceButtons.Count < index)
            {
                var go = Instantiate(confirmButton.gameObject, confirmButton.transform.parent);
                go.name = "Option" + (m_ChoiceButtons.Count + 2);
                var button = go.GetComponent<Button>();
                int choice = m_ChoiceButtons.Count + 1;
                button.onClick.AddListener(() => Choose(choice));
                m_ChoiceButtons.Add(button);
            }
            return m_ChoiceButtons[index - 1];
        }

        void HideChoices()
        {
            foreach (var button in m_ChoiceButtons) button.gameObject.SetActive(false);
            if (confirmLabel != null) confirmLabel.enableAutoSizing = false;
        }

        void Present()
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            UITween.PopIn(this, root.transform, 0.22f, 0f, 0.85f);
        }

        void Choose(int index)
        {
            if (m_OnChoice == null)
            {
                Close(true);
                return;
            }
            var callback = m_OnChoice;
            m_OnChoice = null;
            m_OnCancel = null;
            root.SetActive(false);
            HideChoices();
            callback(index);
        }

        void Close(bool confirmed)
        {
            root.SetActive(false);
            HideChoices();
            var callback = confirmed ? m_OnConfirm : m_OnCancel;
            m_OnConfirm = null;
            m_OnCancel = null;
            m_OnChoice = null;
            callback?.Invoke();
        }
    }
}
