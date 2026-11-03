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

        public void Bind(RecapEntry entry)
        {
            bool art = entry.portrait != null;
            if (portrait != null)
            {
                portrait.sprite = entry.portrait;
                portrait.enabled = art;
                portrait.preserveAspect = true;
            }
            if (placeholder != null)
            {
                placeholder.gameObject.SetActive(!art);
                placeholder.color = entry.color;
            }
            if (initialsLabel != null) initialsLabel.text = entry.initials ?? "?";
            if (nameLabel != null) nameLabel.text = entry.name ?? string.Empty;
            if (streamerLabel != null)
            {
                streamerLabel.text = entry.streamer ?? string.Empty;
                streamerLabel.gameObject.SetActive(!string.IsNullOrEmpty(entry.streamer));
            }
            if (headlineLabel != null)
            {
                headlineLabel.text = LightText.Adapt(entry.headline ?? string.Empty);
                headlineLabel.gameObject.SetActive(!string.IsNullOrEmpty(entry.headline));
            }
            if (bodyLabel != null) bodyLabel.text = LightText.Adapt(entry.body ?? string.Empty);
        }
    }
}
