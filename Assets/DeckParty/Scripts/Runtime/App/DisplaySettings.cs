using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>One selectable screen resolution, grouped by aspect ratio.</summary>
    public readonly struct DisplayResolution
    {
        public readonly int Width;
        public readonly int Height;
        /// <summary>"16:9", "16:10", "21:9" or "32:9".</summary>
        public readonly string Aspect;

        public DisplayResolution(int width, int height, string aspect)
        {
            Width = width;
            Height = height;
            Aspect = aspect;
        }

        public string Label => $"{Width} x {Height}  ({Aspect})";
    }

    /// <summary>
    /// Window mode and resolution. Fullscreen is borderless (alt-tab friendly). Unity remembers the last mode and size
    /// itself, so nothing needs re-applying on launch. F11 toggles fullscreen anywhere; Alt+Enter is handled by Unity
    /// itself (Player Settings > Allow Fullscreen Switch).
    /// </summary>
    public static class DisplaySettings
    {
        /// <summary>The typical PC resolutions offered, by aspect ratio. Sizes the monitor can't show are hidden.</summary>
        public static readonly DisplayResolution[] Presets =
        {
            new DisplayResolution(1280, 720, "16:9"),
            new DisplayResolution(1366, 768, "16:9"),
            new DisplayResolution(1600, 900, "16:9"),
            new DisplayResolution(1920, 1080, "16:9"),
            new DisplayResolution(2560, 1440, "16:9"),
            new DisplayResolution(3840, 2160, "16:9"),
            new DisplayResolution(1280, 800, "16:10"),
            new DisplayResolution(1440, 900, "16:10"),
            new DisplayResolution(1680, 1050, "16:10"),
            new DisplayResolution(1920, 1200, "16:10"),
            new DisplayResolution(2560, 1600, "16:10"),
            new DisplayResolution(2560, 1080, "21:9"),
            new DisplayResolution(3440, 1440, "21:9"),
            new DisplayResolution(3840, 1600, "21:9"),
            new DisplayResolution(5120, 1440, "32:9"),
        };

        public static bool IsFullscreen => Screen.fullScreenMode != FullScreenMode.Windowed;

        /// <summary>The monitor's native size (what fullscreen can reach).</summary>
        public static Vector2Int MonitorSize
        {
            get
            {
                var display = Screen.mainWindowDisplayInfo;
                if (display.width > 0 && display.height > 0) return new Vector2Int(display.width, display.height);
                var current = Screen.currentResolution;
                return new Vector2Int(current.width, current.height);
            }
        }

        /// <summary>Resolutions that fit the monitor. In windowed mode, sizes as big as the desktop are hidden (they'd overflow the taskbar).</summary>
        public static List<DisplayResolution> Available(bool fullscreen)
        {
            var monitor = MonitorSize;
            var result = new List<DisplayResolution>();
            foreach (var r in Presets)
            {
                bool fits = fullscreen ? r.Width <= monitor.x && r.Height <= monitor.y : r.Width < monitor.x && r.Height < monitor.y;
                if (fits) result.Add(r);
            }
            if (result.Count == 0) result.Add(new DisplayResolution(Mathf.Max(640, Screen.width), Mathf.Max(360, Screen.height), "-"));
            return result;
        }

        /// <summary>The index in <paramref name="list"/> closest to the current window size.</summary>
        public static int ClosestIndex(List<DisplayResolution> list)
        {
            int best = 0;
            long bestScore = long.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                long dx = list[i].Width - Screen.width, dy = list[i].Height - Screen.height;
                long score = dx * dx + dy * dy;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }

        public static void Apply(bool fullscreen, int width, int height)
        {
            Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        /// <summary>F11: fullscreen at the monitor's size, or back to a window that fits the desktop.</summary>
        public static void ToggleFullscreen()
        {
            if (IsFullscreen)
            {
                var windowed = Available(false);
                var pick = windowed[windowed.Count - 1];
                Apply(false, pick.Width, pick.Height);
            }
            else
            {
                var monitor = MonitorSize;
                Apply(true, monitor.x, monitor.y);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallHotkeys()
        {
            if (Application.isEditor) return; // the editor's Game view owns the window
            var go = new GameObject("DisplayHotkeys");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<DisplayHotkeys>();
        }
    }

    /// <summary>F11 toggles fullscreen anywhere in the game.</summary>
    sealed class DisplayHotkeys : MonoBehaviour
    {
        void Update()
        {
            if (!UI.InputHelper.IsTyping && UI.InputHelper.Pressed(UI.HotKey.F11)) DisplaySettings.ToggleFullscreen();
        }
    }
}
