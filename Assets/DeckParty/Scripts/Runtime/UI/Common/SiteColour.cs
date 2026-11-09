using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Colours this graphic from the UI Skin's Site Colours by its role (panel inside, border, divider, text...), using the
    /// colours of the named panel it sits in (the nearest <see cref="SiteAreaTag"/> above it). Follows the skin live, in the
    /// editor and while playing. The scene builders add it to the website look's panels, lines and text.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public sealed class SiteColour : MonoBehaviour
    {
        [Tooltip("Which Site Colours entry this graphic takes (UI Skin > Site Colours).")]
        [SerializeField] internal SiteColourRole role;
        [Tooltip("Multiplies the colour's alpha (for see-through lines and fills).")]
        [SerializeField, Range(0f, 1f)] internal float alpha = 1f;

        Graphic m_Graphic;
        SiteArea m_Area;
        bool m_Resolved;

        public SiteColourRole Role
        {
            get => role;
            set
            {
                role = value;
                Apply();
            }
        }

        /// <summary>The named panel this graphic sits in.</summary>
        public SiteArea Area
        {
            get
            {
                Resolve();
                return m_Area;
            }
        }

        void OnEnable()
        {
            UISkin.SkinChanged += Apply;
            m_Resolved = false;
            Apply();
        }

        void OnDisable() => UISkin.SkinChanged -= Apply;

        void OnTransformParentChanged()
        {
            m_Resolved = false;
            if (isActiveAndEnabled) Apply();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            m_Resolved = false;
            // Not during OnValidate itself (it also runs while the scene is loading).
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Apply();
            };
        }
#endif

        /// <summary>Looks up the panel again and re-applies (after a Site Area Tag above it changed).</summary>
        public void Refresh()
        {
            m_Resolved = false;
            Apply();
        }

        public void Apply()
        {
            if (this == null) return;
            if (m_Graphic == null && !TryGetComponent(out m_Graphic)) return;
            Resolve();
            var color = SiteColours.Get(role, m_Area);
            color.a *= alpha;
            m_Graphic.color = color;
        }

        void Resolve()
        {
            if (m_Resolved) return;
            m_Area = SiteColours.AreaOf(transform);
            m_Resolved = true;
        }
    }
}
