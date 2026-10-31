using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>Hover/press scaling and click sounds for any Button.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] internal float hoverScale = 1.04f;
        [SerializeField] internal float pressScale = 0.95f;
        [SerializeField] internal Sfx clickSound = Sfx.Click;
        [SerializeField] internal bool hoverSound = true;

        Button m_Button;
        bool m_Hovering;
        bool m_Pressing;

        void Awake()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(() => AudioService.Play(clickSound));
        }

        void OnDisable()
        {
            m_Hovering = false;
            m_Pressing = false;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float target = 1f;
            if (m_Button.IsInteractable())
                target = m_Pressing ? pressScale : m_Hovering ? hoverScale : 1f;

            var current = transform.localScale.x;
            if (Mathf.Abs(current - target) < 0.001f) return;
            transform.localScale = Vector3.one * Mathf.Lerp(current, target, Time.unscaledDeltaTime * 18f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_Hovering = true;
            if (hoverSound && m_Button.IsInteractable()) AudioService.PlayHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_Hovering = false;
            m_Pressing = false;
        }

        public void OnPointerDown(PointerEventData eventData) => m_Pressing = true;

        public void OnPointerUp(PointerEventData eventData) => m_Pressing = false;
    }
}
