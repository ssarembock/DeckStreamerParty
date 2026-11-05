using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The background behind every main menu screen: plain white until images are added in the Game Database
    /// (Main Menu Backgrounds), then those images in list order, cover-fit, fading from one to the next.
    /// </summary>
    public sealed class MainMenuBackgroundView : MonoBehaviour
    {
        const float FadeSeconds = 1f;

        [SerializeField] internal Image current;
        [SerializeField] internal AspectRatioFitter currentFitter;
        [SerializeField] internal Image next;
        [SerializeField] internal AspectRatioFitter nextFitter;
        [SerializeField] internal Image dim;

        readonly List<Sprite> m_Images = new List<Sprite>();
        float m_Seconds;
        float m_NextAt;
        int m_Index;

        void Start()
        {
            var db = GameDatabase.Instance;
            foreach (var sprite in db.mainMenuBackgrounds)
                if (sprite != null) m_Images.Add(sprite);

            bool hasImages = m_Images.Count > 0;
            current.enabled = hasImages;
            next.enabled = false;
            dim.enabled = hasImages;
            if (!hasImages) return;

            Show(current, currentFitter, m_Images[0]);
            dim.color = dim.color.WithAlpha(db.mainMenuBackgroundDim);
            m_Seconds = db.mainMenuBackgroundSeconds;
            m_NextAt = Time.unscaledTime + m_Seconds;
        }

        void Update()
        {
            if (m_Images.Count < 2) return;

            if (!next.enabled)
            {
                if (Time.unscaledTime < m_NextAt) return;
                m_Index = (m_Index + 1) % m_Images.Count;
                Show(next, nextFitter, m_Images[m_Index]);
                next.color = Color.white.WithAlpha(0f);
                next.enabled = true;
                return;
            }

            var color = next.color;
            color.a = Mathf.MoveTowards(color.a, 1f, Time.unscaledDeltaTime / FadeSeconds);
            next.color = color;
            if (color.a < 1f) return;

            // Fully faded in: it becomes the current image.
            Show(current, currentFitter, m_Images[m_Index]);
            next.enabled = false;
            m_NextAt = Time.unscaledTime + m_Seconds;
        }

        static void Show(Image image, AspectRatioFitter fitter, Sprite sprite)
        {
            image.sprite = sprite;
            var rect = sprite.rect;
            fitter.aspectRatio = rect.height > 0 ? rect.width / rect.height : 16f / 9f;
        }
    }
}
