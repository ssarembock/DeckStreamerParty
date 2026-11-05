using TMPro;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Put by the scene builder on main menu text with no panel behind it. Menu text is pale, for dark backgrounds, so on
    /// the plain white default (no images in the Game Database) it is darkened through <see cref="ReadableOnLight"/>. Over
    /// images it keeps its colours, and the Main Menu Backgrounds darkening keeps it readable.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class MenuBackdropText : MonoBehaviour
    {
        void Awake()
        {
            if (GameDatabase.Instance.HasMainMenuBackgrounds) return;
            gameObject.AddComponent<ReadableOnLight>();
            // The title's white light sweep would wipe its letters out on white.
            if (TryGetComponent<TitleShine>(out var shine)) shine.shineColor = UIPalette.Gold;
        }
    }
}
