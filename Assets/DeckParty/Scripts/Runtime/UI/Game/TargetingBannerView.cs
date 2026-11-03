using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// "Choose a viewer" prompt shown while an action is picking its targets. Cancel and Confirm are icon buttons in the
    /// website look (their words and hotkeys in the tooltip); older scenes with text buttons still work.
    /// </summary>
    public sealed class TargetingBannerView : MonoBehaviour
    {
        [SerializeField] internal GameObject root;
        [SerializeField] internal TMP_Text label;
        [SerializeField] internal Button cancelButton;
        [Tooltip("Optional. Created from the Cancel button at runtime when left empty.")]
        [SerializeField] internal Button confirmButton;

        public event Action CancelClicked;
        public event Action ConfirmClicked;

        TMP_Text m_ConfirmLabel;
        TooltipTrigger m_ConfirmTooltip;
        bool m_IconButtons;
        Vector2 m_LabelOffsetMax;

        void Awake()
        {
            cancelButton.onClick.AddListener(() => CancelClicked?.Invoke());
            if (confirmButton == null) confirmButton = CreateConfirmButton();
            confirmButton.onClick.AddListener(() => ConfirmClicked?.Invoke());
            m_ConfirmLabel = confirmButton.GetComponentInChildren<TMP_Text>(true);
            confirmButton.TryGetComponent(out m_ConfirmTooltip);
            m_IconButtons = m_ConfirmTooltip != null && m_ConfirmLabel == null;
            if (cancelButton.TryGetComponent<TooltipTrigger>(out var cancelTip)) cancelTip.Text = Loc.Get("common.cancel") + "  [Esc]";
            m_LabelOffsetMax = label.rectTransform.offsetMax;
            confirmButton.gameObject.SetActive(false);
            root.SetActive(false);
        }

        Button CreateConfirmButton()
        {
            var go = Instantiate(cancelButton.gameObject, cancelButton.transform.parent);
            go.name = "Confirm";
            var rect = (RectTransform)go.transform;
            var cancelRect = (RectTransform)cancelButton.transform;
            rect.anchoredPosition = cancelRect.anchoredPosition - new Vector2(cancelRect.sizeDelta.x + 12f, 0f);
            if (go.TryGetComponent<Image>(out var image)) image.color = UIPalette.Gold;
            var text = go.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = Loc.Get("common.confirm");
                text.color = UIPalette.Night;
            }
            return go.GetComponent<Button>();
        }

        public void Show(string text)
        {
            // Icon buttons say "[Esc]" on hover; the old text buttons need the reminder in the prompt.
            label.text = m_IconButtons ? text : text + $"  <color=#B9AED6>{Loc.Get("target.esc_cancels")}</color>";
            if (!root.activeSelf)
            {
                root.SetActive(true);
                UITween.PopIn(this, root.transform, 0.18f, 0f, 0.9f);
            }
        }

        /// <summary>Shows the Confirm button for actions that take a variable number of picks.</summary>
        public void SetConfirm(bool visible, bool interactable = true, string text = null)
        {
            if (confirmButton == null) return;
            confirmButton.gameObject.SetActive(visible);
            confirmButton.interactable = interactable;
            if (m_ConfirmLabel != null) m_ConfirmLabel.text = text ?? Loc.Get("common.confirm");
            if (m_ConfirmTooltip != null) m_ConfirmTooltip.Text = (text ?? Loc.Get("common.confirm")) + "  [Space]";
            if (m_IconButtons) return;
            var rect = (RectTransform)confirmButton.transform;
            label.rectTransform.offsetMax = visible ? new Vector2(m_LabelOffsetMax.x - rect.sizeDelta.x - 12f, m_LabelOffsetMax.y) : m_LabelOffsetMax;
        }

        public void Hide()
        {
            SetConfirm(false);
            root.SetActive(false);
        }
    }
}
