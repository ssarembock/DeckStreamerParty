using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Colours a graphic with the site accent (<see cref="SiteTheme"/>, the same on every platform): Fill for accent buttons and badges,
    /// Content for the icon on an accent button, Line for accent text and outlines on white. Re-applies when the streamer on
    /// screen changes platform.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class SiteAccent : MonoBehaviour
    {
        public enum Role
        {
            Fill,
            Content,
            Line,
            Soft,
        }

        [SerializeField] internal Role role;
        [Tooltip("Multiplies the alpha of the accent.")]
        [SerializeField, Range(0f, 1f)] internal float alpha = 1f;

        Graphic m_Graphic;

        public Role Kind
        {
            get => role;
            set
            {
                role = value;
                Apply();
            }
        }

        void OnEnable()
        {
            SiteTheme.Changed += Apply;
            Apply();
        }

        void OnDisable() => SiteTheme.Changed -= Apply;

        public void Apply()
        {
            if (m_Graphic == null) m_Graphic = GetComponent<Graphic>();
            if (m_Graphic == null) return;
            Color color;
            switch (role)
            {
                case Role.Content:
                    // An icon whose UI Skin slot has Override Color ticked (coloured art) keeps the skin's colour.
                    color = TryGetComponent<SkinnedImage>(out var skinned) && SkinOverrides(skinned.Slot)
                        ? GameIcons.SlotTint(skinned.Slot, SiteTheme.AccentContent)
                        : SiteTheme.AccentContent;
                    break;
                case Role.Line: color = SiteTheme.AccentOnWhite; break;
                case Role.Soft: color = SiteTheme.AccentSoft; break;
                default: color = SiteTheme.Accent; break;
            }
            m_Graphic.color = color.WithAlpha(alpha);
        }

        static bool SkinOverrides(SkinSlot slot)
        {
            var skin = GameDatabase.Instance.skin;
            var entry = skin != null ? skin.Get(slot) : null;
            return entry != null && entry.sprite != null && entry.overrideColor;
        }
    }
}
