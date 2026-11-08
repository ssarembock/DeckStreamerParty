using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Fills a row of summary columns (one <see cref="RecapColumnView"/> per player, grey lines between them) from inactive
    /// templates. Used by the stream / shop summary pop-up and by the Free For All waiting panel over the shop.
    /// </summary>
    public static class RecapColumns
    {
        public static void Fill(RectTransform root, RecapColumnView template, GameObject dividerTemplate,
            List<RecapColumnView> columns, List<GameObject> dividers, IList<RecapEntry> entries)
        {
            if (root == null || template == null) return;
            int count = entries != null ? entries.Count : 0;
            while (columns.Count < count)
            {
                var column = Object.Instantiate(template, root);
                column.name = "Column" + (columns.Count + 1);
                columns.Add(column);
            }
            while (dividerTemplate != null && dividers.Count < count - 1)
            {
                var divider = Object.Instantiate(dividerTemplate, root);
                divider.name = "Divider" + (dividers.Count + 1);
                dividers.Add(divider);
            }

            // Column, line, column, line... in that order inside the layout.
            int sibling = 0;
            for (int i = 0; i < columns.Count; i++)
            {
                bool on = i < count;
                columns[i].gameObject.SetActive(on);
                if (!on) continue;
                columns[i].transform.SetSiblingIndex(sibling++);
                columns[i].Bind(entries[i]);
                if (i < count - 1 && i < dividers.Count)
                {
                    dividers[i].SetActive(true);
                    dividers[i].transform.SetSiblingIndex(sibling++);
                }
            }
            for (int i = Mathf.Max(0, count - 1); i < dividers.Count; i++) dividers[i].SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }
    }
}
