using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeckParty.UI
{
    /// <summary>
    /// Runs the gamepad layer for the whole game: created once at start-up and kept across scenes. Every frame, before any
    /// screen reads input, it updates <see cref="PadInput"/> and the selector (<see cref="PadNavigator"/>), and it hides the
    /// mouse cursor while a pad is in use.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class PadRunner : MonoBehaviour
    {
        static PadRunner s_Instance;
        PadFocusFrame m_Frame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (s_Instance != null) return;
            var go = new GameObject("PadRunner");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<PadRunner>();
        }

        void Awake()
        {
            m_Frame = PadFocusFrame.Create(transform);
            SceneManager.sceneLoaded += OnSceneLoaded;
            PadInput.ActiveChanged += OnActiveChanged;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PadInput.ActiveChanged -= OnActiveChanged;
            if (s_Instance == this) s_Instance = null;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PadNavigator.Release();
            PadNavigator.DefaultProvider = null;
            PadNavigator.Paused = false;
        }

        static void OnActiveChanged(bool active)
        {
            if (!active) PadNavigator.Release();
            else PadNavigator.RequestRefocus();
            if (PadLook.OrDefault.hideCursor) Cursor.visible = !active;
            else if (!Cursor.visible) Cursor.visible = true;
        }

        void Update()
        {
            PadInput.Tick();
            PadNavigator.Tick();
        }
    }
}
