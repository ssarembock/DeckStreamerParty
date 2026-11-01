using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    public sealed class HowToPlayScreen : UIScreen
    {
        [SerializeField] internal Button rulesTab;
        [SerializeField] internal Button guestsTab;
        [SerializeField] internal Image rulesTabImage;
        [SerializeField] internal Image guestsTabImage;
        [SerializeField] internal GameObject rulesPanel;
        [SerializeField] internal GameObject guestsPanel;
        [SerializeField] internal TMP_Text rulesText;
        [SerializeField] internal RectTransform guestList;
        [SerializeField] internal GuestRowView guestRowPrefab;
        [SerializeField] internal Button backButton;

        MainMenuController m_Menu;
        readonly List<GuestRowView> m_Rows = new List<GuestRowView>();

        public void Init(MainMenuController menu) => m_Menu = menu;

        void Awake()
        {
            rulesTab.onClick.AddListener(() => ShowTab(true));
            guestsTab.onClick.AddListener(() => ShowTab(false));
            backButton.onClick.AddListener(() => m_Menu.ShowMain());
            Loc.Changed += () =>
            {
                if (IsVisible) OnShow();
            };
        }

        protected override void OnShow()
        {
            rulesText.text = RulesText.Full;
            if (m_Rows.Count == 0)
            {
                foreach (var info in GuestCatalog.All)
                {
                    var row = Instantiate(guestRowPrefab, guestList);
                    row.gameObject.SetActive(true);
                    row.Bind(info.Type, false);
                    m_Rows.Add(row);
                }
            }
            else
            {
                foreach (var row in m_Rows) row.Bind(row.Type, false);
            }
            ShowTab(true);
        }

        void ShowTab(bool rules)
        {
            rulesPanel.SetActive(rules);
            guestsPanel.SetActive(!rules);
            if (rulesTabImage != null) rulesTabImage.color = rules ? UIPalette.Pink : UIPalette.PanelAlt;
            if (guestsTabImage != null) guestsTabImage.color = rules ? UIPalette.PanelAlt : UIPalette.Pink;
        }
    }
}
