using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    public enum ToastKind
    {
        Info,
        Success,
        Warning,
        Error,
    }

    /// <summary>Short notification banner at the top of the screen. Call <see cref="Show"/> from anywhere.</summary>
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] internal CanvasGroup group;
        [SerializeField] internal TMP_Text label;
        [SerializeField] internal Image accent;

        static ToastView s_Active;
        readonly Queue<(string message, ToastKind kind)> m_Queue = new Queue<(string, ToastKind)>();
        Coroutine m_Routine;

        void Awake()
        {
            s_Active = this;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void OnDestroy()
        {
            if (s_Active == this) s_Active = null;
        }

        public static void Show(string message, ToastKind kind = ToastKind.Info)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (s_Active == null)
            {
                Debug.Log($"[DeckParty] {message}");
                return;
            }
            s_Active.Enqueue(message, kind);
        }

        void Enqueue(string message, ToastKind kind)
        {
            while (m_Queue.Count >= 4) m_Queue.Dequeue();
            m_Queue.Enqueue((message, kind));
            if (m_Routine == null) m_Routine = StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            while (m_Queue.Count > 0)
            {
                var (message, kind) = m_Queue.Dequeue();
                label.text = message;
                if (accent != null) accent.color = ColorFor(kind);

                yield return UITween.Run(0.15f, t => group.alpha = t);
                float hold = Mathf.Clamp(1.6f + message.Length * 0.025f, 1.8f, 4f);
                float end = Time.unscaledTime + (m_Queue.Count > 0 ? hold * 0.6f : hold);
                while (Time.unscaledTime < end) yield return null;
                yield return UITween.Run(0.2f, t => group.alpha = 1f - t);
            }
            group.alpha = 0f;
            m_Routine = null;
        }

        static Color ColorFor(ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Success: return UIPalette.Green;
                case ToastKind.Warning: return UIPalette.Orange;
                case ToastKind.Error: return UIPalette.Red;
                default: return UIPalette.Teal;
            }
        }
    }
}
