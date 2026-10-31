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

        void Awake()
        {
            startButton.onClick.AddListener(StartSeason);
            backButton.onClick.AddListener(() => m_Menu.ShowMain());
            venueSelector.Changed += OnVenueChanged;
        }

        protected override void OnShow()
        {
            SetText("Subtitle", "Pick a character set and a venue, then start your season.");
            SetText("Scenarios/Header", "Character Sets");
            BuildScenarioList();

            m_VenueChoices = GameDatabase.Instance.VenueChoices();
            var labels = new List<string>();
            int venueIndex = 0;
            for (int i = 0; i < m_VenueChoices.Count; i++)
            {
                labels.Add(m_VenueChoices[i].label);
                if (m_VenueChoices[i].id == AppSettings.VenueId) venueIndex = i;
            }
            venueSelector.SetTitle("Venue");
            venueSelector.SetOptions(labels, venueIndex);

            Select(m_Selected >= 0 ? m_Selected : AppSettings.LastScenario);
        }

        protected override void OnHide() => m_Menu.PreviewVenue(null);

        /// <summary>Static captions are set here too, so scenes generated before a rename show the new terms.</summary>
        void SetText(string path, string text)
        {
            var child = transform.Find(path);
            if (child != null && child.TryGetComponent<TMP_Text>(out var label)) label.text = text;
        }

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
                string extra = scenario.IsRandom ? $"streak {ProgressStore.RandomStreak}" : null;
                m_Items[i].Bind(scenario, ProgressStore.IsCleared(scenario.Id), extra);
            }
        }

        void Select(int scenarioId)
        {
            var scenario = ScenarioCatalog.Get(scenarioId);
            m_Selected = scenario.Id;
            foreach (var item in m_Items) item.SetSelected(item.ScenarioId == m_Selected);

            titleLabel.text = scenario.Name;
            taglineLabel.text = scenario.Tagline;
            descriptionLabel.text = scenario.Description;

            var progress = new StringBuilder();
            if (scenario.IsPreset) progress.Append(ProgressStore.IsCleared(scenario.Id) ? "<color=#6BE38B>Cleared!</color>" : "<color=#B9AED6>Not cleared yet</color>");
            else if (scenario.IsRandom)
                progress.Append($"Win streak: <color=#3DDBD9>{ProgressStore.RandomStreak}</color>/{ProgressStore.CherryStreak} for the Cherry (best {ProgressStore.BestRandomStreak})");
            else progress.Append("<color=#B9AED6>Sandbox - progress isn't recorded.</color>");
            progress.Append($"    {GameRules.DefaultNights} nights");
            progressLabel.text = progress.ToString();

            foreach (var row in m_Rows) row.gameObject.SetActive(false);
            if (scenario.IsRandom)
            {
                shopHeader.text = $"Character set: <color=#FFC94A>11 random guests + 3 random {Terms.Diamonds}</color>, rolled when the season starts.";
            }
            else
            {
                var shop = ScenarioCatalog.BuildShop(scenario.Id, 1);
                shopHeader.text = $"Character set ({shop.Count} guests). Hover a guest for details.";
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
            var preview = PartyEngine.CreateGame(m_Selected, 1);
            var venue = GameDatabase.Instance.ResolveVenue(choice, preview);
            if (venue == null) return;

            var sprite = venue.Preview;
            venuePreview.sprite = sprite;
            venuePreview.color = sprite != null ? Color.white : venue.skyBottom;

            string note = choice == AppSettings.VenueAuto ? " (character set default)" : choice == AppSettings.VenueEvolving ? " (upgrades as your house grows)" : string.Empty;
            venueDescription.text = $"<b>{venue.displayName}</b>{note}\n<color=#B9AED6>{venue.description}</color>";
            m_Menu.PreviewVenue(venue);
        }

        void StartSeason()
        {
            var launch = new SinglePlayerLaunch
            {
                ScenarioId = m_Selected,
                Seed = (uint)Random.Range(1, int.MaxValue),
                VenueChoice = AppSettings.VenueId,
            };
            AppSettings.LastScenario = m_Selected;

            if (SaveSystem.HasSave && SaveSystem.Describe() != null)
            {
                ModalDialog.Show("Start a new season?", "You have a season in progress. Starting a new one will replace that save.",
                    () => SceneFlow.StartSinglePlayer(launch), "Start New", "Cancel");
                return;
            }
            SceneFlow.StartSinglePlayer(launch);
        }
    }
}
