using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>A full-screen menu page that fades in and out.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        CanvasGroup m_Group;

        protected CanvasGroup Group => m_Group != null ? m_Group : (m_Group = GetComponent<CanvasGroup>());

        public bool IsVisible { get; private set; }

        public void Show()
        {
            gameObject.SetActive(true);
            IsVisible = true;
            Group.alpha = 0f;
            Group.interactable = true;
            Group.blocksRaycasts = true;
            OnShow();
            UITween.Fade(this, Group, 1f, 0.2f);
        }

        public void Hide()
        {
            if (IsVisible) OnHide();
            IsVisible = false;
            gameObject.SetActive(false);
        }

        /// <summary>Loading another scene destroys a visible screen without Hide(); give it a chance to clean up.</summary>
        protected virtual void OnDestroy()
        {
            if (!IsVisible) return;
            IsVisible = false;
            OnUnloaded();
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        /// <summary>
        /// The screen was destroyed while visible (a scene load). Drop subscriptions to long-lived objects here, such as
        /// the network session, which survives scene loads. Other UI in the scene may already be gone.
        /// </summary>
        protected virtual void OnUnloaded() { }
    }
}
