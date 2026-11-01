using System.Collections.Generic;
using System.Text;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    public sealed class SinglePlayerScreen : UIScreen
    {
        [SerializeField] internal RectTransform scenarioList;
        [SerializeField] internal ScenarioItemView scenarioItemPrefab;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text taglineLabel;
        [SerializeField] internal TMP_Text descriptionLabel;
        [SerializeField] internal TMP_Text progressLabel;
        [SerializeField] internal TMP_Text shopHeader;
        [SerializeField] internal RectTransform shopList;
        [SerializeField] internal GuestRowView guestRowPrefab;
        [SerializeField] internal OptionSelector venueSelector;
        [SerializeField] internal Image venuePreview;
        [SerializeField] internal TMP_Text venueDescription;
        [SerializeField] internal Button startButton;
        [SerializeField] internal Button backButton;

        MainMenuController m_Menu;
        readonly List<ScenarioItemView> m_Items = new List<ScenarioItemView>();
        readonly List<GuestRowView> m_Rows = new List<GuestRowView>();
        List<(string id, string label)> m_VenueChoices;
        int m_Selected = -1;

        public void Init(MainMenuController menu) => m_Menu = menu;

        /// <summary>The streamer picked on the select screen before this one.</summary>
        public byte Streamer { get; set; }

        void Awake()
        {
            startButton.onClick.AddListener(StartSeason);
            backButton.onClick.AddListener(() => m_Menu.ShowStreamerSelect(StreamerSelectMode.SinglePlayer));
            venueSelector.Changed += OnVenueChanged;
            Loc.Changed += () =>
            {
                if (IsVisible) OnShow();
            };
        }

        protected override void OnShow()
        {
            BuildScenarioList();

            m_VenueChoices = GameDatabase.Instance.VenueChoices();
            var labels = new List<string>();
            int venueIndex = 0;
            for (int i = 0; i < m_VenueChoices.Count; i++)
            {
                labels.Add(m_VenueChoices[i].label);
                if (m_VenueChoices[i].id == AppSettings.VenueId) venueIndex = i;
            }
            venueSelector.SetTitle(Loc.Get("pause.setup"));
            venueSelector.SetOptions(labels, venueIndex);

            Select(m_Selected >= 0 ? m_Selected : AppSettings.LastScenario);
        }

        protected override void OnHide() => m_Menu.PreviewVenue(null);

        void BuildScenarioList()
        {
            if (m_Items.Count == 0)
            {
                foreach (var scenario in ScenarioCatalog.All)
                {
                    var item = Instantiate(scenarioItemPrefab, scenarioList);
                    item.gameObject.SetActive(true);
                    item.Clicked += view => Select(view.ScenarioId);
                    m_Items.Add(item);
                }
            }

            for (int i = 0; i < m_Items.Count; i++)
            {
                var scenario = ScenarioCatalog.All[i];
                string extra = scenario.IsRandom ? Loc.Get("single.streak_short", "count", ProgressStore.RandomStreak) : null;
                m_Items[i].Bind(scenario, ProgressStore.IsCleared(scenario.Id), extra);
            }
        }

        void Select(int scenarioId)
        {
            var scenario = ScenarioCatalog.Get(scenarioId);
            m_Selected = scenario.Id;
            foreach (var item in m_Items) item.SetSelected(item.ScenarioId == m_Selected);

            titleLabel.text = ContentText.ScenarioName(scenario.Id);
            taglineLabel.text = ContentText.ScenarioTagline(scenario.Id);
            descriptionLabel.text = ContentText.ScenarioDescription(scenario.Id);

            var progress = new StringBuilder();
            if (scenario.IsPreset) progress.Append(ProgressStore.IsCleared(scenario.Id) ? $"<color=#6BE38B>{Loc.Get("single.cleared")}</color>" : $"<color=#B9AED6>{Loc.Get("single.not_cleared")}</color>");
            else if (scenario.IsRandom)
                progress.Append(Loc.Get("single.streak", ("streak", $"<color=#3DDBD9>{ProgressStore.RandomStreak}</color>"), ("goal", ProgressStore.CherryStreak), ("best", ProgressStore.BestRandomStreak)));
            else progress.Append($"<color=#B9AED6>{Loc.Get("single.sandbox")}</color>");
            progress.Append("    ").Append(Loc.Get("single.nights", "count", GameRules.DefaultNights));
            if (Streamer != (byte)StreamerType.None && Streamer != StreamerCatalog.RandomPick)
                progress.Append("    ").Append(Loc.Get("audience.streamer", "streamer", StreamerVisuals.NameTag((StreamerType)Streamer), "power", StreamerVisuals.PowerName((StreamerType)Streamer)));
            progressLabel.text = progress.ToString();

            foreach (var row in m_Rows) row.gameObject.SetActive(false);
            if (scenario.IsRandom)
            {
                shopHeader.text = Loc.Get("single.random_set");
            }
            else
            {
                var shop = ScenarioCatalog.BuildShop(scenario.Id, 1);
                shopHeader.text = Loc.Get("single.set_header", "count", shop.Count);
                for (int i = 0; i < shop.Count; i++)
                {
                    if (i >= m_Rows.Count) m_Rows.Add(Instantiate(guestRowPrefab, shopList));
                    var row = m_Rows[i];
                    row.gameObject.SetActive(true);
                    row.Bind(shop[i], false);
                }
            }

            RefreshVenuePreview();
        }

        void OnVenueChanged(int index)
        {
            if (m_VenueChoices == null || index < 0 || index >= m_VenueChoices.Count) return;
            AppSettings.VenueId = m_VenueChoices[index].id;
            RefreshVenuePreview();
        }

        void RefreshVenuePreview()
        {
            if (m_VenueChoices == null) return;
            var choice = m_VenueChoices[Mathf.Clamp(venueSelector.Index, 0, m_VenueChoices.Count - 1)].id;
            var preview = PartyEngine.CreateGame(m_Selected, 1, GameRules.DefaultNights, PreviewStreamer);
            var venue = GameDatabase.Instance.ResolveVenue(choice, preview);
            if (venue == null) return;

            var sprite = venue.Preview;
            venuePreview.sprite = sprite;
            venuePreview.color = sprite != null ? Color.white : venue.skyBottom;

            string note = choice == AppSettings.VenueEvolving ? " " + Loc.Get("single.evolving_note") : string.Empty;
            venueDescription.text = $"<b>{GameDatabase.VenueName(venue)}</b>{note}\n<color=#B9AED6>{ContentText.VenueDescription(venue)}</color>";
            m_Menu.PreviewVenue(venue);
        }

        StreamerType PreviewStreamer => Streamer == StreamerCatalog.RandomPick || !StreamerCatalog.IsValid((StreamerType)Streamer) ? StreamerType.None : (StreamerType)Streamer;

        void StartSeason()
        {
            var launch = new SinglePlayerLaunch
            {
                ScenarioId = m_Selected,
                Seed = (uint)Random.Range(1, int.MaxValue),
                VenueChoice = AppSettings.VenueId,
                Streamer = Streamer,
            };
            AppSettings.LastScenario = m_Selected;

            if (SaveSystem.HasSave && SaveSystem.Describe() != null)
            {
                ModalDialog.Show(Loc.Get("single.replace_title"), Loc.Get("single.replace_body"),
                    () => SceneFlow.StartSinglePlayer(launch), Loc.Get("single.start_new"));
                return;
            }
            SceneFlow.StartSinglePlayer(launch);
        }
    }
}
