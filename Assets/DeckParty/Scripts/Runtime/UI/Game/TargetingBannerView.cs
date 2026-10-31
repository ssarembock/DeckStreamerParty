using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>"Choose a guest" prompt shown while an action is picking its targets.</summary>
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
        Vector2 m_LabelOffsetMax;

        void Awake()
        {
            cancelButton.onClick.AddListener(() => CancelClicked?.Invoke());
            if (confirmButton == null) confirmButton = CreateConfirmButton();
            confirmButton.onClick.AddListener(() => ConfirmClicked?.Invoke());
            m_ConfirmLabel = confirmButton.GetComponentInChildren<TMP_Text>(true);
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
                text.text = "Confirm";
                text.color = UIPalette.Night;
            }
            return go.GetComponent<Button>();
        }

        public void Show(string text)
        {
            label.text = text + "  <color=#B9AED6>[Esc] cancels</color>";
            if (!root.activeSelf)
            {
                root.SetActive(true);
                UITween.PopIn(this, root.transform, 0.18f, 0f, 0.9f);
            }
        }

        /// <summary>Shows the Confirm button for actions that take a variable number of picks.</summary>
        public void SetConfirm(bool visible, bool interactable = true, string text = "Confirm")
        {
            if (confirmButton == null) return;
            confirmButton.gameObject.SetActive(visible);
            confirmButton.interactable = interactable;
            if (m_ConfirmLabel != null) m_ConfirmLabel.text = text;
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
