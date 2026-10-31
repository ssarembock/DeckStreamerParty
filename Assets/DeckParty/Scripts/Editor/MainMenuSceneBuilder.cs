using DeckParty.UI;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.EditorTools
{
    /// <summary>Builds Assets/DeckParty/Scenes/MainMenu.unity: title, single player, multiplayer, lobby, settings, how to play.</summary>
    internal static class MainMenuSceneBuilder
    {
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Opening a scene unloads unreferenced assets, so load art and prefabs only after it.
            EditorUI.LoadArt();
            var prefabs = DeckPartyAssets.LoadUIPrefabs();

            SceneCommon.CreateCamera();
            SceneCommon.CreateEventSystem();

            var backgroundCanvas = SceneCommon.CreateCanvas("BackgroundCanvas", -10, false);
            var background = SceneCommon.CreateBackground(backgroundCanvas);

            var canvas = SceneCommon.CreateCanvas("MenuCanvas", 0);
            var screens = EditorUI.Rect(canvas, "Screens").Stretch();

            var main = BuildMainScreen(screens);
            var single = BuildSinglePlayerScreen(screens, prefabs);
            var multi = BuildMultiplayerScreen(screens);
            var lobby = BuildLobbyScreen(screens, prefabs);
            var settings = BuildSettingsScreen(screens);
            var howTo = BuildHowToPlayScreen(screens, prefabs);

            var overlay = SceneCommon.CreateCanvas("OverlayCanvas", 100);
            SceneCommon.CreateOverlays(overlay);

            var controllerGo = new GameObject("MainMenuController");
            var controller = controllerGo.AddComponent<MainMenuController>();
            controller.mainScreen = main;
            controller.singlePlayerScreen = single;
            controller.multiplayerScreen = multi;
            controller.lobbyScreen = lobby;
            controller.settingsScreen = settings;
            controller.howToPlayScreen = howTo;
            controller.background = background;

            EditorSceneManager.SaveScene(scene, DeckPartyPaths.MainMenuScene);
        }

        // ------------------------------------------------------------------ main

        static MainMenuScreen BuildMainScreen(Transform parent)
        {
            var screen = SceneCommon.Screen(parent, "MainScreen");

            var titleGroup = EditorUI.Rect(screen, "TitleGroup").Place(new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1500, 210), new Vector2(0.5f, 1));
            var shadow = EditorUI.Text(titleGroup, "TitleShadow", "DECK PARTY", 156, UIPalette.Pink.WithAlpha(0.85f), TextAlignmentOptions.Center, true, true, false);
            shadow.rectTransform.Stretch(0, 8, -8, -8);
            var title = EditorUI.Text(titleGroup, "Title", "DECK PARTY", 156, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.Stretch();

            var logo = EditorUI.Image(screen, "Logo", Color.white, null, SkinSlot.Logo);
            logo.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(900, 280), new Vector2(0.5f, 1));
            logo.preserveAspect = true;
            logo.gameObject.SetActive(false);

            var tagline = EditorUI.Text(screen, "Tagline", "Go live, build your Squad and get 4 Diamonds to one party.", 32, UIPalette.Text, TextAlignmentOptions.Center, false, false, false);
            tagline.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -292), new Vector2(1500, 50), new Vector2(0.5f, 1));

            var column = EditorUI.Panel(screen, "Buttons", UIPalette.Panel.WithAlpha(0.75f));
            column.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(640, 600));
            EditorUI.VLayout(column, 16, EditorUI.Pad(36, 32), TextAnchor.UpperCenter, true, true, true, false);

            var continueButton = EditorUI.Button(column.transform, "Continue", "Continue", ButtonStyle.Gold, 30, out var continueLabel);
            EditorUI.Layout(continueButton, -1, 86);
            var singleButton = EditorUI.Button(column.transform, "SinglePlayer", "Single Player", ButtonStyle.Primary, 34);
            EditorUI.Layout(singleButton, -1, 86);
            var multiButton = EditorUI.Button(column.transform, "Multiplayer", "Multiplayer", ButtonStyle.Secondary, 34);
            EditorUI.Layout(multiButton, -1, 86);
            var howButton = EditorUI.Button(column.transform, "HowToPlay", "How to Play", ButtonStyle.Normal, 30);
            EditorUI.Layout(howButton, -1, 72);
            var settingsButton = EditorUI.Button(column.transform, "Settings", "Settings", ButtonStyle.Normal, 30);
            EditorUI.Layout(settingsButton, -1, 72);
            var quitButton = EditorUI.Button(column.transform, "Quit", "Quit", ButtonStyle.Ghost, 28);
            EditorUI.Layout(quitButton, -1, 66);

            var progress = EditorUI.Text(screen, "Progress", string.Empty, 24, UIPalette.Text, TextAlignmentOptions.Center, false, false, false);
            progress.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1700, 40), new Vector2(0.5f, 0));
            var version = EditorUI.Text(screen, "Version", "v1.0", 20, UIPalette.TextDim, TextAlignmentOptions.BottomRight, false, false, false);
            version.rectTransform.Place(new Vector2(1, 0), new Vector2(-24, 16), new Vector2(300, 30), new Vector2(1, 0));

            var view = screen.gameObject.AddComponent<MainMenuScreen>();
            view.titleLabel = title;
            view.logoImage = logo;
            view.titleGroup = titleGroup.gameObject;
            view.taglineLabel = tagline;
            view.continueButton = continueButton;
            view.continueLabel = continueLabel;
            view.singlePlayerButton = singleButton;
            view.multiplayerButton = multiButton;
            view.howToPlayButton = howButton;
            view.settingsButton = settingsButton;
            view.quitButton = quitButton;
            view.progressLabel = progress;
            view.versionLabel = version;
            return view;
        }

        // ------------------------------------------------------------------ single player

        static SinglePlayerScreen BuildSinglePlayerScreen(Transform parent, UIPrefabs prefabs)
        {
            var screen = SceneCommon.Screen(parent, "SinglePlayerScreen");
            SceneCommon.Header(screen, "Single Player", "Pick a character set and a venue, then start your season.");

            var left = EditorUI.Panel(screen, "Scenarios");
            left.rectTransform.Box(new Vector2(0, 0), new Vector2(0, 1), new Vector2(60, 150), new Vector2(640, -170));
            var leftTitle = EditorUI.Text(left.transform, "Header", "Character Sets", 30, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            leftTitle.rectTransform.TopBand(18, 40, 26, 26);
            EditorUI.Scroll(left.transform, "List", out var scenarioList, 10, EditorUI.Pad(6)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -70));

            var right = EditorUI.Panel(screen, "Details");
            right.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(680, 150), new Vector2(-60, -170));
            var title = EditorUI.Text(right.transform, "Title", "Character Set", 52, UIPalette.Gold, TextAlignmentOptions.MidlineLeft, true, true, false);
            title.rectTransform.TopBand(20, 64, 32, 32);
            var tagline = EditorUI.Text(right.transform, "Tagline", "Tagline", 28, UIPalette.Teal, TextAlignmentOptions.MidlineLeft, false, false, false);
            tagline.rectTransform.TopBand(86, 38, 32, 32);
            var description = EditorUI.Text(right.transform, "Description", "Description", 25, UIPalette.Text, TextAlignmentOptions.TopLeft);
            description.rectTransform.TopBand(130, 70, 32, 32);
            var progress = EditorUI.Text(right.transform, "Progress", string.Empty, 23, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
            progress.rectTransform.TopBand(204, 34, 32, 32);
            var shopHeader = EditorUI.Text(right.transform, "ShopHeader", "Character set", 23, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, false, false);
            shopHeader.rectTransform.TopBand(244, 34, 32, 32);
            EditorUI.Scroll(right.transform, "Shop", out var shopList, 10, EditorUI.Pad(6)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(20, 18), new Vector2(-20, -284));

            var back = SceneCommon.BackButton(screen);

            var venueSelector = EditorUI.Selector(screen, "VenueSelector", "Venue");
            venueSelector.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(320, 24), new Vector2(520, 100), new Vector2(0, 0));
            var preview = EditorUI.Image(screen, "VenuePreview", UIPalette.PanelAlt, EditorUI.Rounded);
            preview.rectTransform.Place(new Vector2(0, 0), new Vector2(862, 30), new Vector2(156, 88), new Vector2(0, 0));
            preview.preserveAspect = false;
            var venueDescription = EditorUI.Text(screen, "VenueDescription", string.Empty, 21, UIPalette.Text, TextAlignmentOptions.MidlineLeft);
            venueDescription.rectTransform.Place(new Vector2(0, 0), new Vector2(1036, 26), new Vector2(460, 96), new Vector2(0, 0));

            var start = EditorUI.Button(screen, "Start", "Start Season", ButtonStyle.Primary, 34);
            start.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-60, 34), new Vector2(340, 88), new Vector2(1, 0));

            var view = screen.gameObject.AddComponent<SinglePlayerScreen>();
            view.scenarioList = scenarioList;
            view.scenarioItemPrefab = prefabs.ScenarioItem;
            view.titleLabel = title;
            view.taglineLabel = tagline;
            view.descriptionLabel = description;
            view.progressLabel = progress;
            view.shopHeader = shopHeader;
            view.shopList = shopList;
            view.guestRowPrefab = prefabs.GuestRow;
            view.venueSelector = venueSelector;
            view.venuePreview = preview;
            view.venueDescription = venueDescription;
            view.startButton = start;
            view.backButton = back;
            return view;
        }

        // ------------------------------------------------------------------ multiplayer

        static MultiplayerScreen BuildMultiplayerScreen(Transform parent)
        {
            var screen = SceneCommon.Screen(parent, "MultiplayerScreen");
            SceneCommon.Header(screen, "Multiplayer", "Up to 6 players. Same character set, separate parties. First 4-Diamond party wins.");

            var nameRow = EditorUI.Rect(screen, "NameRow").Place(new Vector2(0.5f, 1), new Vector2(0, -186), new Vector2(820, 70), new Vector2(0.5f, 1));
            EditorUI.HLayout(nameRow, 16, EditorUI.Pad(0), TextAnchor.MiddleCenter, true, true, false, true);
            var nameLabel = EditorUI.Text(nameRow, "Label", "Your name", 28, UIPalette.Text, TextAlignmentOptions.MidlineRight, true, false, false);
            EditorUI.Layout(nameLabel, 220, -1, 0);
            var nameInput = EditorUI.Input(nameRow, "NameInput", "Player name", 28, 16);
            EditorUI.Layout(nameInput, -1, 70, 1);

            // Online card
            var online = EditorUI.Panel(screen, "OnlineCard");
            online.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(-410, -40), new Vector2(780, 540));
            EditorUI.VLayout(online, 16, EditorUI.Pad(36, 30), TextAnchor.UpperCenter, true, true, true, false);
            var onlineTitle = EditorUI.Text(online.transform, "Title", "Online (Unity Relay)", 38, UIPalette.Teal, TextAlignmentOptions.MidlineLeft, true, true, false);
            EditorUI.Layout(onlineTitle, -1, 52);
            var onlineNote = EditorUI.Text(online.transform, "Note", "Play over the internet with a join code.", 22, UIPalette.TextDim, TextAlignmentOptions.TopLeft);
            EditorUI.Layout(onlineNote, -1, 62);
            var hostOnline = EditorUI.Button(online.transform, "HostOnline", "Host Online Party", ButtonStyle.Primary, 32);
            EditorUI.Layout(hostOnline, -1, 88);
            var orOnline = EditorUI.Text(online.transform, "Or", "- or join a friend's party -", 22, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            EditorUI.Layout(orOnline, -1, 34);
            var codeRow = EditorUI.Rect(online.transform, "CodeRow");
            EditorUI.Layout(codeRow, -1, 76);
            EditorUI.HLayout(codeRow, 12, EditorUI.Pad(0), TextAnchor.MiddleCenter, true, true, false, true);
            var codeInput = EditorUI.Input(codeRow, "CodeInput", "Join code", 30, 12);
            EditorUI.Layout(codeInput, -1, 76, 1);
            var joinOnline = EditorUI.Button(codeRow, "JoinOnline", "Join", ButtonStyle.Secondary, 30);
            EditorUI.Layout(joinOnline, 200, 76, 0);

            // LAN card
            var lan = EditorUI.Panel(screen, "LanCard");
            lan.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(410, -40), new Vector2(780, 540));
            EditorUI.VLayout(lan, 16, EditorUI.Pad(36, 30), TextAnchor.UpperCenter, true, true, true, false);
            var lanTitle = EditorUI.Text(lan.transform, "Title", "Local Network / Direct IP", 38, UIPalette.Orange, TextAlignmentOptions.MidlineLeft, true, true, false);
            EditorUI.Layout(lanTitle, -1, 52);
            var lanNote = EditorUI.Text(lan.transform, "Note", "Same Wi-Fi / LAN, or a forwarded port.", 22, UIPalette.TextDim, TextAlignmentOptions.TopLeft);
            EditorUI.Layout(lanNote, -1, 62);
            var hostLan = EditorUI.Button(lan.transform, "HostLan", "Host on this Network", ButtonStyle.Primary, 32);
            EditorUI.Layout(hostLan, -1, 88);
            var orLan = EditorUI.Text(lan.transform, "Or", "- or join by address -", 22, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            EditorUI.Layout(orLan, -1, 34);
            var ipRow = EditorUI.Rect(lan.transform, "IpRow");
            EditorUI.Layout(ipRow, -1, 76);
            EditorUI.HLayout(ipRow, 12, EditorUI.Pad(0), TextAnchor.MiddleCenter, true, true, false, true);
            var ipInput = EditorUI.Input(ipRow, "IpInput", "Host IP (e.g. 192.168.1.20)", 26, 64);
            EditorUI.Layout(ipInput, -1, 76, 1);
            var portInput = EditorUI.Input(ipRow, "PortInput", "Port", 26, 5);
            portInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            EditorUI.Layout(portInput, 130, 76, 0);
            var joinLan = EditorUI.Button(ipRow, "JoinLan", "Join", ButtonStyle.Secondary, 30);
            EditorUI.Layout(joinLan, 150, 76, 0);

            var status = EditorUI.Text(screen, "Status", string.Empty, 26, UIPalette.Text, TextAlignmentOptions.Center);
            status.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(1500, 70), new Vector2(0.5f, 0));
            var back = SceneCommon.BackButton(screen);

            var view = screen.gameObject.AddComponent<MultiplayerScreen>();
            view.nameInput = nameInput;
            view.hostOnlineButton = hostOnline;
            view.codeInput = codeInput;
            view.joinOnlineButton = joinOnline;
            view.onlineNote = onlineNote;
            view.ipInput = ipInput;
            view.portInput = portInput;
            view.hostLanButton = hostLan;
            view.joinLanButton = joinLan;
            view.lanNote = lanNote;
            view.statusLabel = status;
            view.backButton = back;
            return view;
        }

        // ------------------------------------------------------------------ lobby

        static LobbyScreen BuildLobbyScreen(Transform parent, UIPrefabs prefabs)
        {
            var screen = SceneCommon.Screen(parent, "LobbyScreen");
            var title = SceneCommon.Header(screen, "Party Lobby", null);

            var codePanel = EditorUI.Panel(screen, "CodePanel");
            codePanel.rectTransform.Place(new Vector2(1, 1), new Vector2(-60, -30), new Vector2(680, 150), new Vector2(1, 1));
            var codeHint = EditorUI.Text(codePanel.transform, "Hint", "Share this join code with your friends.", 21, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
            codeHint.rectTransform.TopBand(14, 30, 24, 24);
            var code = EditorUI.Text(codePanel.transform, "Code", "------", 54, UIPalette.Gold, TextAlignmentOptions.MidlineLeft, true, true, false);
            code.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(24, 14), new Vector2(-190, -48));
            code.enableAutoSizing = true;
            code.fontSizeMin = 24;
            code.fontSizeMax = 54;
            var copy = EditorUI.Button(codePanel.transform, "Copy", "Copy", ButtonStyle.Normal, 26);
            copy.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-20, 22), new Vector2(150, 64), new Vector2(1, 0));

            // Players
            var players = EditorUI.Panel(screen, "Players");
            players.rectTransform.Box(new Vector2(0, 0), new Vector2(0, 1), new Vector2(60, 150), new Vector2(620, -200));
            var playerCount = EditorUI.Text(players.transform, "Header", "Players", 30, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            playerCount.rectTransform.TopBand(18, 40, 24, 24);
            var list = EditorUI.Rect(players.transform, "List").Box(Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -70));
            EditorUI.VLayout(list, 10, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, true, false);

            // Settings
            var settings = EditorUI.Panel(screen, "Settings");
            settings.rectTransform.Box(new Vector2(0, 0), new Vector2(0, 1), new Vector2(660, 150), new Vector2(1260, -200));
            EditorUI.VLayout(settings, 12, EditorUI.Pad(28, 22), TextAnchor.UpperCenter, true, true, true, false);
            var settingsTitle = EditorUI.Text(settings.transform, "Header", "Match Settings", 30, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            EditorUI.Layout(settingsTitle, -1, 40);
            var scenario = EditorUI.Selector(settings.transform, "Scenario", "Character Set");
            EditorUI.Layout(scenario, -1, 96);
            var mode = EditorUI.Selector(settings.transform, "Mode", "Mode");
            EditorUI.Layout(mode, -1, 96);
            var nights = EditorUI.Selector(settings.transform, "Nights", "Season length");
            EditorUI.Layout(nights, -1, 96);
            var summary = EditorUI.Text(settings.transform, "Summary", string.Empty, 19, UIPalette.TextDim, TextAlignmentOptions.TopLeft);
            EditorUI.Layout(summary, -1, 110, -1, 1);
            summary.enableAutoSizing = true;
            summary.fontSizeMin = 13;
            summary.fontSizeMax = 19;
            var venue = EditorUI.Selector(settings.transform, "Venue", "Your venue");
            EditorUI.Layout(venue, -1, 96);

            // Chat
            var chatPanel = EditorUI.Panel(screen, "Chat");
            chatPanel.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(1300, 150), new Vector2(-60, -200));
            var chatTitle = EditorUI.Text(chatPanel.transform, "Header", "Chat", 30, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            chatTitle.rectTransform.TopBand(18, 40, 24, 24);
            var chat = SceneCommon.Chat(chatPanel.rectTransform, 70, prefabs.TextLine);

            // Bottom bar
            var leave = EditorUI.Button(screen, "Leave", "Leave", ButtonStyle.Ghost, 28);
            leave.GetComponent<RectTransform>().Place(new Vector2(0, 0), new Vector2(60, 40), new Vector2(240, 84), new Vector2(0, 0));
            var status = EditorUI.Text(screen, "Status", string.Empty, 26, UIPalette.Text, TextAlignmentOptions.Center);
            status.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(-40, 44), new Vector2(900, 76), new Vector2(0.5f, 0));
            var ready = EditorUI.Button(screen, "Ready", "Ready!", ButtonStyle.Secondary, 32, out var readyLabel);
            ready.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-60, 40), new Vector2(340, 88), new Vector2(1, 0));
            var start = EditorUI.Button(screen, "Start", "Start Season", ButtonStyle.Primary, 32);
            start.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-60, 40), new Vector2(340, 88), new Vector2(1, 0));

            var view = screen.gameObject.AddComponent<LobbyScreen>();
            view.titleLabel = title;
            view.codeLabel = code;
            view.codeHintLabel = codeHint;
            view.copyCodeButton = copy;
            view.playerList = list;
            view.playerRowPrefab = prefabs.PlayerRow;
            view.playerCountLabel = playerCount;
            view.scenarioSelector = scenario;
            view.modeSelector = mode;
            view.nightsSelector = nights;
            view.venueSelector = venue;
            view.settingsSummary = summary;
            view.readyButton = ready;
            view.readyLabel = readyLabel;
            view.startButton = start;
            view.leaveButton = leave;
            view.statusLabel = status;
            view.chat = chat;
            return view;
        }

        // ------------------------------------------------------------------ settings

        static SettingsScreen BuildSettingsScreen(Transform parent)
        {
            var screen = SceneCommon.Screen(parent, "SettingsScreen");
            SceneCommon.Header(screen, "Settings", null);

            var panel = EditorUI.Panel(screen, "Panel");
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1040, 720));
            EditorUI.VLayout(panel, 18, EditorUI.Pad(50, 40), TextAnchor.UpperCenter, true, true, true, false);

            EditorUI.LabelledRow(panel.transform, "NameRow", "Player name", 70, out var nameSlot);
            var nameInput = EditorUI.Input(nameSlot, "NameInput", "Player name", 28, 16);
            nameInput.GetComponent<RectTransform>().Stretch(0, 2, 0, 2);

            var master = SliderRow(panel.transform, "Master", "Master volume");
            var music = SliderRow(panel.transform, "Music", "Music volume");
            var sfx = SliderRow(panel.transform, "Sfx", "Sound effects");

            var fullscreen = EditorUI.Toggle(panel.transform, "Fullscreen", "Fullscreen");
            EditorUI.Layout(fullscreen, -1, 62);
            var fast = EditorUI.Toggle(panel.transform, "FastAnimations", "Fast animations");
            EditorUI.Layout(fast, -1, 62);

            var spacer = EditorUI.Rect(panel.transform, "Spacer");
            EditorUI.Layout(spacer, -1, 10);
            var reset = EditorUI.Button(panel.transform, "ResetProgress", "Reset Progress", ButtonStyle.Danger, 28);
            EditorUI.Layout(reset, -1, 72);

            var back = SceneCommon.BackButton(screen);

            var view = screen.gameObject.AddComponent<SettingsScreen>();
            view.nameInput = nameInput;
            view.masterSlider = master;
            view.musicSlider = music;
            view.sfxSlider = sfx;
            view.fullscreenToggle = fullscreen;
            view.fastAnimationsToggle = fast;
            view.resetProgressButton = reset;
            view.backButton = back;
            return view;
        }

        internal static Slider SliderRow(Transform parent, string name, string label, float labelWidth = 300)
        {
            EditorUI.LabelledRow(parent, name + "Row", label, 58, out var slot, labelWidth);
            var slider = EditorUI.Slider(slot, name + "Slider");
            slider.GetComponent<RectTransform>().Stretch(0, 16, 0, 16);
            return slider;
        }

        // ------------------------------------------------------------------ how to play

        static HowToPlayScreen BuildHowToPlayScreen(Transform parent, UIPrefabs prefabs)
        {
            var screen = SceneCommon.Screen(parent, "HowToPlayScreen");
            SceneCommon.Header(screen, "How to Play", null);

            var tabs = EditorUI.Rect(screen, "Tabs").Place(new Vector2(0, 1), new Vector2(80, -136), new Vector2(560, 64), new Vector2(0, 1));
            EditorUI.HLayout(tabs, 12, EditorUI.Pad(0), TextAnchor.MiddleLeft, true, true, true, true);
            var rulesTab = EditorUI.Button(tabs, "RulesTab", "Rules", ButtonStyle.Primary, 28);
            var guestsTab = EditorUI.Button(tabs, "GuestsTab", "All 46 Guests", ButtonStyle.Normal, 28);

            var content = EditorUI.Panel(screen, "Content");
            content.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(80, 140), new Vector2(-80, -220));

            var rulesPanel = EditorUI.Rect(content.transform, "Rules").Stretch(20, 18, 20, 18);
            EditorUI.Scroll(rulesPanel, "Scroll", out var rulesContent, 0, EditorUI.Pad(16, 10)).GetComponent<RectTransform>().Stretch();
            var rulesText = EditorUI.Text(rulesContent, "Text", string.Empty, 25, UIPalette.Text, TextAlignmentOptions.TopLeft);

            var guestsPanel = EditorUI.Rect(content.transform, "Guests").Stretch(20, 18, 20, 18);
            EditorUI.Scroll(guestsPanel, "Scroll", out var guestList, 10, EditorUI.Pad(6)).GetComponent<RectTransform>().Stretch();
            guestsPanel.gameObject.SetActive(false);

            var back = SceneCommon.BackButton(screen);

            var view = screen.gameObject.AddComponent<HowToPlayScreen>();
            view.rulesTab = rulesTab;
            view.guestsTab = guestsTab;
            view.rulesTabImage = rulesTab.GetComponent<Image>();
            view.guestsTabImage = guestsTab.GetComponent<Image>();
            view.rulesPanel = rulesPanel.gameObject;
            view.guestsPanel = guestsPanel.gameObject;
            view.rulesText = rulesText;
            view.guestList = guestList;
            view.guestRowPrefab = prefabs.GuestRow;
            view.backButton = back;
            return view;
        }
    }
}
