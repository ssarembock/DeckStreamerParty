using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The picture centred along the bottom of the Stream Chat window (Game Database > Stream Art > Chat Footer Image). With
    /// no picture assigned nothing shows. The near-deplatform warning appears on top of it.
    /// </summary>
    public sealed class ChatFooterView : MonoBehaviour
    {
        [SerializeField] internal Image image;

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (image == null) return;
            var sprite = GameDatabase.Instance.chatFooterImage;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = sprite != null;
        }
    }
}
