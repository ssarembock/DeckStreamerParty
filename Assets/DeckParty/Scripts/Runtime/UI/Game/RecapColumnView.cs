using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>What one column of a <see cref="RecapView"/> shows: a player's streamer, their name and their quick summary.</summary>
    public sealed class RecapEntry
    {
        public Sprite portrait;
        public Color color = Color.white;
        public string initials;
        /// <summary>The player's name (bold, under the avatar).</summary>
        public string name;
        /// <summary>The streamer they play (smaller, under the name), so two players with the same streamer can be told apart.</summary>
        public string streamer;
        /// <summary>The headline of the column (the stream's result), already coloured.</summary>
        public string headline;
        /// <summary>The summary lines (icons and numbers), already formatted.</summary>
        public string body;

        /// <summary>Teams (2026-11-18): a second streamer shown beside the first (the team's other player).</summary>
        public bool hasSecond;
        public Sprite portrait2;
        public Color color2 = Color.white;
        public string initials2;
    }

    /// <summary>One player's column in the stream / shop summary pop-up: the streamer avatar, name, streamer and summary.</summary>
    public sealed class RecapColumnView : MonoBehaviour
    {
        [SerializeField] internal Image portrait;
        [SerializeField] internal Image placeholder;
        [SerializeField] internal TMP_Text initialsLabel;
        [SerializeField] internal Image ring;
        [SerializeField] internal TMP_Text nameLabel;
        [SerializeField] internal TMP_Text streamerLabel;
        [SerializeField] internal TMP_Text headlineLabel;
        [SerializeField] internal TMP_Text bodyLabel;

        // Teams: a copy of the avatar frame for the second streamer, made the first time it's needed (no Rebuild needed).
        RectTransform m_Frame;
        Vector2 m_FramePosition;
        RectTransform m_Frame2;
        Image m_Portrait2;
        Image m_Placeholder2;
        TMP_Text m_Initials2;

        public void Bind(RecapEntry entry)
        {
            ShowAvatar(portrait, placeholder, initialsLabel, entry.portrait, entry.color, entry.initials);
            ArrangeAvatars(entry.hasSecond);
            if (entry.hasSecond && m_Frame2 != null) ShowAvatar(m_Portrait2, m_Placeholder2, m_Initials2, entry.portrait2, entry.color2, entry.initials2);
            if (nameLabel != null) nameLabel.text = entry.name ?? string.Empty;
            if (streamerLabel != null)
            {
                streamerLabel.text = entry.streamer ?? string.Empty;
                streamerLabel.gameObject.SetActive(!string.IsNullOrEmpty(entry.streamer));
            }
            if (headlineLabel != null)
            {
                headlineLabel.text = LightText.AdaptOn(headlineLabel.transform, entry.headline ?? string.Empty);
                headlineLabel.gameObject.SetActive(!string.IsNullOrEmpty(entry.headline));
            }
            if (bodyLabel != null) bodyLabel.text = LightText.AdaptOn(bodyLabel.transform, entry.body ?? string.Empty);
        }

        static void ShowAvatar(Image art, Image fallback, TMP_Text initials, Sprite sprite, Color color, string letters)
        {
            bool has = sprite != null;
            if (art != null)
            {
                art.sprite = sprite;
                art.enabled = has;
                art.preserveAspect = true;
            }
            if (fallback != null)
            {
                fallback.gameObject.SetActive(!has);
                fallback.color = color;
            }
            if (initials != null) initials.text = letters ?? "?";
        }

        /// <summary>
        /// One avatar in the middle, or (teams) two overlapping a little, side by side. The frame is the avatar's ring holder
        /// (Frame > Mask > Art in the built column); the copy is made from it once.
        /// </summary>
        void ArrangeAvatars(bool two)
        {
            if (m_Frame == null)
            {
                var mask = portrait != null ? portrait.transform.parent : null;
                m_Frame = mask != null ? mask.parent as RectTransform : null;
                if (m_Frame == null) return;
                m_FramePosition = m_Frame.anchoredPosition;
            }
            if (two && m_Frame2 == null)
            {
                m_Frame2 = Instantiate(m_Frame, m_Frame.parent);
                m_Frame2.name = m_Frame.name + "2";
                foreach (var image in m_Frame2.GetComponentsInChildren<Image>(true))
                {
                    if (image.name == portrait.name) m_Portrait2 = image;
                    else if (placeholder != null && image.name == placeholder.name) m_Placeholder2 = image;
                }
                m_Initials2 = m_Frame2.GetComponentInChildren<TMP_Text>(true);
            }
            float shift = two ? m_Frame.rect.width * 0.45f : 0f;
            m_Frame.anchoredPosition = m_FramePosition - new Vector2(shift, 0f);
            if (m_Frame2 == null) return;
            m_Frame2.gameObject.SetActive(two);
            m_Frame2.anchoredPosition = m_FramePosition + new Vector2(shift, 0f);
        }
    }
}
