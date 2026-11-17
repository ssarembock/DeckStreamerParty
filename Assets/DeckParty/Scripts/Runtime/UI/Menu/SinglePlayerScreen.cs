using System.Collections.Generic;
using System.Text;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The character set screen: pick a set (its shop shows on the right), then Start. The streamers are picked after
    /// Start. Used by Puzzles, Endless, Versus the AI and Couch Competitive (the mode line says which).
    /// </summary>
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
        [Tooltip("Stream setups are no longer picked (2026-11-17): scenes built before still have these, hidden.")]
        [SerializeField] internal OptionSelector venueSelector;
        [SerializeField] internal Image venuePreview;
        [SerializeField] internal TMP_Text venueDescription;
        [SerializeField] internal Button startButton;
        [SerializeField] internal Button backButton;

        MainMenuController m_Menu;
        readonly List<ScenarioItemView> m_Items = new List<ScenarioItemView>();
        readonly List<GuestRowView> m_Rows = new List<GuestRowView>();
        int m_Selected = -1;

        public void Init(MainMenuController menu) => m_Menu = menu;

        /// <summary>Puzzles (the usual character sets), Endless, Versus the AI or Couch Competitive (picked on a mode screen).</summary>
        public SinglePlayerMode Mode { get; set; }

        void Awake()
        {
            startButton.onClick.AddListener(StartSeason);
            backButton.onClick.AddListener(() => m_Menu.ShowCharacterSetsBack());
            HideStreamSetup();
            PadDefaultFocus.Mark(startButton, 1);
            Loc.Changed += () =>
            {
                if (IsVisible) OnShow();
            };
        }

        /// <summary>The stream setup picker of older scenes (its preview and description) stays hidden.</summary>
        void HideStreamSetup()
        {
            if (venueSelector != null) venueSelector.gameObject.SetActive(false);
            if (venuePreview != null) venuePreview.gameObject.SetActive(false);
            if (venueDescription != null) venueDescription.gameObject.SetActive(false);
        }

        protected override void OnShow()
        {
            BuildScenarioList();
            HideStreamSetup();
            Select(m_Selected >= 0 ? m_Selected : AppSettings.LastScenario);
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
            if (Mode == SinglePlayerMode.Endless)
            {
                // Endless: no win, so no cleared marks or streaks; the longest run instead.
                progress.Append($"<color=#3DDBD9>{Loc.Get("single.endless_mode")}</color>    ");
                progress.Append(ProgressStore.BestEndlessRun > 0 ? Loc.Get("modes.endless_best", "count", ProgressStore.BestEndlessRun) : Loc.Get("modes.endless_none"));
            }
            else if (Mode == SinglePlayerMode.VersusAi)
            {
                // The rival's streamer is picked after Start, with yours.
                progress.Append($"<color=#FF4F8B>{Loc.Get("modes.versus")}</color>    ");
                progress.Append(Loc.Get("single.versus_record", "wins", ProgressStore.VersusWins, "losses", ProgressStore.VersusLosses));
            }
            else if (Mode == SinglePlayerMode.Couch)
            {
                // Two players on this computer: both streamers are picked after Start.
                progress.Append($"<color=#6BE38B>{Loc.Get("modes.couch")}</color>    ");
                progress.Append(Loc.Get("single.couch_line"));
            }
            else if (scenario.IsPreset) progress.Append(ProgressStore.IsCleared(scenario.Id) ? $"<color=#6BE38B>{Loc.Get("single.cleared")}</color>" : $"<color=#B9AED6>{Loc.Get("single.not_cleared")}</color>");
            else if (scenario.IsRandom)
                progress.Append(Loc.Get("single.streak", ("streak", $"<color=#3DDBD9>{ProgressStore.RandomStreak}</color>"), ("goal", ProgressStore.CherryStreak), ("best", ProgressStore.BestRandomStreak)));
            else progress.Append($"<color=#B9AED6>{Loc.Get("single.sandbox")}</color>");
            progress.Append("    ").Append(Loc.Get(Mode == SinglePlayerMode.Endless ? "single.endless_streams" : "single.nights", "count", GameRules.DefaultNights));
            // The streamer is picked last, after Start (like a fighting game), so it isn't shown here.
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
        }

        void StartSeason()
        {
            var launch = new SinglePlayerLaunch
            {
                ScenarioId = m_Selected,
                Seed = (uint)Random.Range(1, int.MaxValue),
                // Stream setups aren't picked any more: the game uses the streamer's own setup, or the default.
                VenueChoice = AppSettings.VenueAuto,
                // The streamers are picked next (after Start), last thing before the season.
                Mode = Mode == SinglePlayerMode.Endless ? GameMode.Endless : GameMode.Classic,
                VersusAi = Mode == SinglePlayerMode.VersusAi,
                Couch = Mode == SinglePlayerMode.Couch,
            };
            AppSettings.LastScenario = m_Selected;

            // Versus the AI and couch play aren't saved, so an unfinished solo season stays where it is.
            if (launch.VersusAi || launch.Couch)
            {
                m_Menu.PickStreamersAndStart(launch);
                return;
            }

            if (SaveSystem.HasSave && SaveSystem.Describe() != null)
            {
                ModalDialog.Show(Loc.Get("single.replace_title"), Loc.Get("single.replace_body"),
                    () => m_Menu.PickStreamersAndStart(launch), Loc.Get("single.start_new"));
                return;
            }
            m_Menu.PickStreamersAndStart(launch);
        }
    }
}
