using DeckParty.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.EditorTools
{
    /// <summary>Builds Assets/DeckParty/Scenes/Game.unity: HUD, house, party controls, planning/shop, overlays.</summary>
    internal static class GameSceneBuilder
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

            var canvas = SceneCommon.CreateCanvas("GameCanvas", 0);
            var root = EditorUI.Rect(canvas, "Root").Stretch();

            var hud = BuildHud(root);

            var mainArea = EditorUI.Rect(root, "MainArea").Box(Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-456, -130));
            var partyRoot = EditorUI.Rect(mainArea, "Party").Stretch();
            var house = BuildHouse(partyRoot, prefabs);
            var targeting = BuildTargetingBanner(partyRoot);
            var controls = BuildPartyControls(partyRoot, prefabs);
            var planning = BuildPlanning(mainArea, prefabs);
            var raid = BuildRaidBlame(mainArea, prefabs);

            var side = EditorUI.Rect(root, "SidePanel").Box(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-440, 16), new Vector2(-16, -130));
            EditorUI.VLayout(side, 12, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, true, false);
            var opponents = BuildOpponents(side, prefabs);
            var log = BuildLog(side, prefabs);
            var chat = BuildChat(side, prefabs);

            var overlay = SceneCommon.CreateCanvas("OverlayCanvas", 100);
            var effects = BuildEffects(overlay, root);
            var banner = BuildBanner(overlay);
            var picker = BuildPicker(overlay, prefabs);
            var rolodex = BuildRolodex(overlay, prefabs);
            var pause = BuildPauseMenu(overlay);
            var gameOver = BuildGameOver(overlay, prefabs);
            SceneCommon.CreateOverlays(overlay);

            var controllerGo = new GameObject("GameController");
            var controller = controllerGo.AddComponent<GameController>();
            controller.hud = hud;
            controller.partyRoot = partyRoot.gameObject;
            controller.house = house;
            controller.partyControls = controls;
            controller.planning = planning;
            controller.raidBlame = raid;
            controller.targetingBanner = targeting;
            controller.log = log;
            controller.opponents = opponents;
            controller.chat = chat;
            controller.picker = picker;
            controller.rolodex = rolodex;
            controller.gameOver = gameOver;
            controller.pauseMenu = pause;
            controller.banner = banner;
            controller.effects = effects;
            controller.background = background;

            EditorSceneManager.SaveScene(scene, DeckPartyPaths.GameScene);
        }

        // ------------------------------------------------------------------ HUD

        static HudView BuildHud(RectTransform root)
        {
            var bar = EditorUI.Panel(root, "TopBar", UIPalette.PanelDeep, SkinSlot.TopBar);
            bar.rectTransform.TopBand(14, 102, 16, 16);
            EditorUI.HLayout(bar, 22, new RectOffset(28, 20, 10, 10), TextAnchor.MiddleLeft, true, true, false, true);

            var info = EditorUI.Rect(bar.transform, "Info");
            EditorUI.Layout(info, 290, -1, 0);
            EditorUI.VLayout(info, 0, EditorUI.Pad(0), TextAnchor.MiddleLeft, true, true, true, false);
            var scenario = EditorUI.Text(info, "Scenario", "Scenario", 20, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
            EditorUI.Layout(scenario, -1, 28);
            var night = EditorUI.Text(info, "Night", "Night 1/25", 40, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            EditorUI.Layout(night, -1, 50);

            var pop = Stat(bar.transform, "Pop", "GIFTED SUBS", EditorUI.Sparkle, UIPalette.Gold, SkinSlot.IconPop, 190);
            var cash = Stat(bar.transform, "Cash", "CASH", EditorUI.Coin, UIPalette.Green, SkinSlot.IconCash, 170);
            var capacity = Stat(bar.transform, "House", "SUB CAP", EditorUI.Door, UIPalette.Teal, SkinSlot.IconDoor, 190);

            var trouble = Meter(bar.transform, "Trouble", EditorUI.Cross, UIPalette.Border, SkinSlot.IconTrouble, 3, 230, out var troublePips, out var troubleLabel);
            var stars = Meter(bar.transform, "Stars", EditorUI.Star, UIPalette.Border, SkinSlot.IconStar, 4, 240, out var starPips, out var starsLabel);

            var spacer = EditorUI.Rect(bar.transform, "Spacer");
            EditorUI.Layout(spacer, -1, -1, 1);

            var rolodex = EditorUI.Button(bar.transform, "Rolodex", "Squad [R]", ButtonStyle.Normal, 24);
            EditorUI.Layout(rolodex, 190, 64, 0);
            var menu = EditorUI.Button(bar.transform, "Menu", "Menu", ButtonStyle.Normal, 24);
            EditorUI.Layout(menu, 130, 64, 0);

            var view = bar.gameObject.AddComponent<HudView>();
            view.scenarioLabel = scenario;
            view.nightLabel = night;
            view.popLabel = pop;
            view.cashLabel = cash;
            view.capacityLabel = capacity;
            view.troubleLabel = troubleLabel;
            view.troublePips = troublePips;
            view.starsLabel = starsLabel;
            view.starPips = starPips;
            view.troubleTooltip = EditorUI.Tooltip(trouble);
            view.starsTooltip = EditorUI.Tooltip(stars);
            view.rolodexButton = rolodex;
            view.menuButton = menu;
            return view;
        }

        static TextMeshProUGUI Stat(Transform parent, string name, string caption, Sprite icon, Color color, SkinSlot slot, float width)
        {
            var block = EditorUI.Rect(parent, name);
            EditorUI.Layout(block, width, -1, 0);
            EditorUI.HLayout(block, 10, EditorUI.Pad(0), TextAnchor.MiddleLeft, true, true, false, false);

            var image = EditorUI.Icon(block, "Icon", icon, color, slot, 46);
            EditorUI.Layout(image, 46, 46, 0);

            var texts = EditorUI.Rect(block, "Texts");
            EditorUI.Layout(texts, -1, 80, 1);
            EditorUI.VLayout(texts, 0, EditorUI.Pad(0), TextAnchor.MiddleLeft, true, true, true, false);
            var label = EditorUI.Text(texts, "Caption", caption, 16, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, true, false, false);
            EditorUI.Layout(label, -1, 22);
            var value = EditorUI.Text(texts, "Value", "0", 40, color, TextAlignmentOptions.MidlineLeft, true, true, false);
            EditorUI.Layout(value, -1, 48);
            EditorUI.Tooltip(block, name == "Pop" ? "Gifted Subs: spend them to recruit new guests." : name == "Cash" ? "Cash: spend it to raise your Sub Cap." : "Spaces in use / your Sub Cap.");
            var hit = block.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            return value;
        }

        static RectTransform Meter(Transform parent, string name, Sprite icon, Color color, SkinSlot slot, int count, float width,
            out Image[] pips, out TextMeshProUGUI label)
        {
            var block = EditorUI.Rect(parent, name);
            EditorUI.Layout(block, width, -1, 0);
            EditorUI.VLayout(block, 4, EditorUI.Pad(0, 6), TextAnchor.MiddleLeft, true, true, true, false);
            var hit = block.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);

            label = EditorUI.Text(block, "Label", name, 22, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, false, false);
            EditorUI.Layout(label, -1, 30);
            var row = EditorUI.Rect(block, "Pips");
            EditorUI.Layout(row, -1, 40);
            EditorUI.HLayout(row, 8, EditorUI.Pad(0), TextAnchor.MiddleLeft, false, false, false, false);
            pips = new Image[count];
            for (int i = 0; i < count; i++)
            {
                pips[i] = EditorUI.Icon(row, "Pip" + (i + 1), icon, color, slot, 38);
            }
            return block;
        }

        // ------------------------------------------------------------------ house + controls

        static HouseView BuildHouse(RectTransform partyRoot, UIPrefabs prefabs)
        {
            var header = EditorUI.Text(partyRoot, "Header", "The Party", 30, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            header.rectTransform.TopBand(0, 44, 12, 12);
            var hint = EditorUI.Text(partyRoot, "Hint", string.Empty, 24, UIPalette.Orange, TextAlignmentOptions.MidlineLeft, false, false, false);
            hint.rectTransform.TopBand(44, 34, 12, 12);

            var area = EditorUI.Rect(partyRoot, "HouseArea").Box(Vector2.zero, Vector2.one, new Vector2(10, 200), new Vector2(-10, -84));
            var gridRect = EditorUI.Rect(area, "Grid").Stretch();
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(GuestCardView.DesignWidth, GuestCardView.DesignHeight);
            grid.spacing = new Vector2(16, 16);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            var view = partyRoot.gameObject.AddComponent<HouseView>();
            view.area = area;
            view.grid = grid;
            view.cardPrefab = prefabs.GuestCard;
            view.headerLabel = header;
            view.hintLabel = hint;
            return view;
        }

        static TargetingBannerView BuildTargetingBanner(RectTransform partyRoot)
        {
            var host = EditorUI.Rect(partyRoot, "Targeting").TopBand(84, 72, 160, 160);
            var panel = EditorUI.Panel(host, "Root", UIPalette.Purple.WithAlpha(0.95f), SkinSlot.PanelAlt);
            panel.rectTransform.Stretch();
            var label = EditorUI.Text(panel.transform, "Label", "Choose a target", 24, UIPalette.Text, TextAlignmentOptions.MidlineLeft);
            label.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(24, 4), new Vector2(-170, -4));
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = 24;
            var cancel = EditorUI.Button(panel.transform, "Cancel", "Cancel", ButtonStyle.Ghost, 24);
            cancel.GetComponent<RectTransform>().Place(new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(140, 52), new Vector2(1, 0.5f));
            var confirm = EditorUI.Button(panel.transform, "Confirm", "Confirm", ButtonStyle.Gold, 22, out var confirmLabel);
            confirm.GetComponent<RectTransform>().Place(new Vector2(1, 0.5f), new Vector2(-164, 0), new Vector2(170, 52), new Vector2(1, 0.5f));
            confirmLabel.enableAutoSizing = true;
            confirmLabel.fontSizeMin = 14;
            confirmLabel.fontSizeMax = 22;
            confirm.gameObject.SetActive(false);

            var view = host.gameObject.AddComponent<TargetingBannerView>();
            view.root = panel.gameObject;
            view.label = label;
            view.cancelButton = cancel;
            view.confirmButton = confirm;
            return view;
        }

        static PartyControlsView BuildPartyControls(RectTransform partyRoot, UIPrefabs prefabs)
        {
            var bar = EditorUI.Panel(partyRoot, "Controls", UIPalette.PanelDeep.WithAlpha(0.9f));
            bar.rectTransform.BottomBand(0, 186);

            var door = EditorUI.Button(bar.transform, "Door", "Open the Door", ButtonStyle.Primary, 36, out var doorLabel);
            door.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(-255, 16), new Vector2(480, 126));
            doorLabel.rectTransform.TopBand(16, 52, 16, 16);
            var doorSub = EditorUI.Text(door.transform, "Sub", string.Empty, 20, UIPalette.Text.WithAlpha(0.9f), TextAlignmentOptions.Center, false, false, false);
            doorSub.rectTransform.BottomBand(14, 34, 12, 12);
            var doorTip = EditorUI.Tooltip(door);

            var end = EditorUI.Button(bar.transform, "EndParty", "End Party", ButtonStyle.Gold, 36, out var endLabel);
            end.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(255, 16), new Vector2(480, 126));
            endLabel.rectTransform.TopBand(16, 52, 16, 16);
            var endSub = EditorUI.Text(end.transform, "Sub", string.Empty, 20, UIPalette.Night, TextAlignmentOptions.Center, true, false, false);
            endSub.rectTransform.BottomBand(14, 34, 12, 12);
            EditorUI.Tooltip(end, "Bank the Subs and Cash of everyone inside as Gifted Subs and $.");

            var risk = EditorUI.Text(bar.transform, "Risk", string.Empty, 22, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            risk.rectTransform.BottomBand(6, 32, 20, 20);

            // Peek decision panel floats above the controls.
            var peek = EditorUI.Panel(bar.transform, "Peek", UIPalette.PanelDeep);
            peek.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, 12), new Vector2(680, 350), new Vector2(0.5f, 0));
            var peekTitle = EditorUI.Text(peek.transform, "Title", "Waiting at the door...", 28, UIPalette.Teal, TextAlignmentOptions.MidlineLeft, true, true, false);
            peekTitle.rectTransform.TopBand(14, 40, 28, 28);
            var cardGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.GuestCard.gameObject, peek.transform);
            var peekCard = cardGo.GetComponent<GuestCardView>();
            ((RectTransform)cardGo.transform).Place(new Vector2(0, 0), new Vector2(28, 22), new Vector2(GuestCardView.DesignWidth, GuestCardView.DesignHeight), new Vector2(0, 0));
            var admit = EditorUI.Button(peek.transform, "Admit", "Let them in", ButtonStyle.Primary, 30);
            admit.GetComponent<RectTransform>().Place(new Vector2(1, 0.5f), new Vector2(-30, 30), new Vector2(360, 96), new Vector2(1, 0.5f));
            var reject = EditorUI.Button(peek.transform, "Reject", "Turn them away", ButtonStyle.Danger, 30);
            reject.GetComponent<RectTransform>().Place(new Vector2(1, 0.5f), new Vector2(-30, -86), new Vector2(360, 96), new Vector2(1, 0.5f));
            peek.gameObject.SetActive(false);

            var view = bar.gameObject.AddComponent<PartyControlsView>();
            view.doorButton = door;
            view.doorLabel = doorLabel;
            view.doorSubLabel = doorSub;
            view.doorTooltip = doorTip;
            view.endButton = end;
            view.endButtonImage = end.GetComponent<Image>();
            view.endLabel = endLabel;
            view.endSubLabel = endSub;
            view.riskLabel = risk;
            view.peekPanel = peek.gameObject;
            view.peekTitle = peekTitle;
            view.peekCard = peekCard;
            view.admitButton = admit;
            view.rejectButton = reject;
            return view;
        }

        // ------------------------------------------------------------------ planning

        static PlanningView BuildPlanning(RectTransform mainArea, UIPrefabs prefabs)
        {
            var root = EditorUI.Rect(mainArea, "Planning").Stretch();

            var left = EditorUI.Panel(root, "Summary");
            left.rectTransform.Box(new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(600, 0));
            EditorUI.VLayout(left, 14, EditorUI.Pad(28, 24), TextAnchor.UpperCenter, true, true, true, false);

            var result = EditorUI.Panel(left.transform, "Result", UIPalette.PanelAlt, SkinSlot.PanelAlt);
            EditorUI.Layout(result, -1, 250);
            var accent = EditorUI.Image(result.transform, "Accent", UIPalette.Gold, EditorUI.RoundedSmall);
            accent.rectTransform.Box(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 12), new Vector2(10, -12));
            var resultTitle = EditorUI.Text(result.transform, "Title", "Party success!", 34, UIPalette.Gold, TextAlignmentOptions.MidlineLeft, true, true, false);
            resultTitle.rectTransform.TopBand(14, 48, 30, 16);
            resultTitle.enableAutoSizing = true;
            resultTitle.fontSizeMin = 22;
            resultTitle.fontSizeMax = 34;
            var resultDetail = EditorUI.Text(result.transform, "Detail", string.Empty, 23, UIPalette.Text, TextAlignmentOptions.TopLeft);
            resultDetail.rectTransform.TopBand(64, 72, 30, 16);
            var breakdown = EditorUI.Text(result.transform, "Breakdown", string.Empty, 18, UIPalette.TextDim, TextAlignmentOptions.TopLeft);
            breakdown.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(30, 10), new Vector2(-16, -140));
            breakdown.enableAutoSizing = true;
            breakdown.fontSizeMin = 12;
            breakdown.fontSizeMax = 18;

            var nightLabel = EditorUI.Text(left.transform, "Night", "Night 1 is over.", 22, UIPalette.Text, TextAlignmentOptions.MidlineLeft);
            EditorUI.Layout(nightLabel, -1, 34);
            var wallet = EditorUI.Text(left.transform, "Wallet", "You have 0 Gifted Subs and 0 $", 28, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, false, false);
            EditorUI.Layout(wallet, -1, 44);

            var expand = EditorUI.Button(left.transform, "Expand", "Raise Sub Cap", ButtonStyle.Secondary, 25, out var expandLabel);
            EditorUI.Layout(expand, -1, 84);
            var expandTip = EditorUI.Tooltip(expand);
            var rolodex = EditorUI.Button(left.transform, "Rolodex", "View Squad", ButtonStyle.Normal, 25, out var rolodexLabel);
            EditorUI.Layout(rolodex, -1, 68);

            var spacer = EditorUI.Rect(left.transform, "Spacer");
            EditorUI.Layout(spacer, -1, 10, -1, 1);

            var waiting = EditorUI.Text(left.transform, "Waiting", string.Empty, 21, UIPalette.TextDim, TextAlignmentOptions.Center);
            EditorUI.Layout(waiting, -1, 60);
            var next = EditorUI.Button(left.transform, "NextNight", "Start Night 2", ButtonStyle.Primary, 34, out var nextLabel);
            EditorUI.Layout(next, -1, 110);

            var shop = EditorUI.Panel(root, "Shop");
            shop.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(620, 0), Vector2.zero);
            var shopTitle = EditorUI.Text(shop.transform, "Header", "Recruit Guests", 38, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            shopTitle.rectTransform.TopBand(18, 52, 30, 30);
            var shopHint = EditorUI.Text(shop.transform, "Hint", "Spend Gifted Subs to add guests to your Squad for every future party. Hover a guest for details.", 21, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft);
            shopHint.rectTransform.TopBand(70, 34, 30, 30);
            EditorUI.Scroll(shop.transform, "List", out var shopList, 10, EditorUI.Pad(6)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -114));

            var view = root.gameObject.AddComponent<PlanningView>();
            view.resultAccent = accent;
            view.resultTitle = resultTitle;
            view.resultDetail = resultDetail;
            view.breakdownLabel = breakdown;
            view.nightLabel = nightLabel;
            view.expandButton = expand;
            view.expandLabel = expandLabel;
            view.expandTooltip = expandTip;
            view.rolodexButton = rolodex;
            view.rolodexLabel = rolodexLabel;
            view.nextNightButton = next;
            view.nextNightLabel = nextLabel;
            view.waitingLabel = waiting;
            view.walletLabel = wallet;
            view.shopList = shopList;
            view.shopRowPrefab = prefabs.GuestRow;
            root.gameObject.SetActive(false);
            return view;
        }

        static RaidBlameView BuildRaidBlame(RectTransform mainArea, UIPrefabs prefabs)
        {
            var panel = EditorUI.Panel(mainArea, "RaidBlame", UIPalette.PanelDeep.WithAlpha(0.96f));
            panel.rectTransform.Stretch();
            var title = EditorUI.Text(panel.transform, "Title", "SWATTED!", 80, UIPalette.Red, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(26, 100, 40, 40);
            var message = EditorUI.Text(panel.transform, "Message", string.Empty, 28, UIPalette.Text, TextAlignmentOptions.Center);
            message.rectTransform.TopBand(128, 90, 60, 60);
            EditorUI.Scroll(panel.transform, "Suspects", out var grid, 18, EditorUI.Pad(10), GuestCardView.CellSize(0.8f)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(40, 30), new Vector2(-40, -232));

            var view = panel.gameObject.AddComponent<RaidBlameView>();
            view.titleLabel = title;
            view.messageLabel = message;
            view.grid = grid;
            view.cardPrefab = prefabs.GuestCard;
            panel.gameObject.SetActive(false);
            return view;
        }

        // ------------------------------------------------------------------ side panel

        static OpponentsView BuildOpponents(RectTransform side, UIPrefabs prefabs)
        {
            var panel = EditorUI.Panel(side, "Players");
            EditorUI.Layout(panel, -1, 340);
            var title = EditorUI.Text(panel.transform, "Header", "Players", 26, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            title.rectTransform.TopBand(12, 36, 20, 20);
            EditorUI.Scroll(panel.transform, "List", out var list, 8, EditorUI.Pad(2)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -54));

            var view = panel.gameObject.AddComponent<OpponentsView>();
            view.titleLabel = title;
            view.list = list;
            view.rowPrefab = prefabs.PlayerRow;
            return view;
        }

        static EventLogView BuildLog(RectTransform side, UIPrefabs prefabs)
        {
            var panel = EditorUI.Panel(side, "Log");
            EditorUI.Layout(panel, -1, 200, -1, 1);
            var title = EditorUI.Text(panel.transform, "Header", "Party Log", 26, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            title.rectTransform.TopBand(12, 36, 20, 20);
            var log = SceneCommon.LogList(panel.transform, "Lines", prefabs.TextLine, 150);
            log.GetComponent<RectTransform>().Box(Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -54));
            return log;
        }

        static ChatView BuildChat(RectTransform side, UIPrefabs prefabs)
        {
            var panel = EditorUI.Panel(side, "Chat");
            EditorUI.Layout(panel, -1, 290);
            var title = EditorUI.Text(panel.transform, "Header", "Chat", 26, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            title.rectTransform.TopBand(12, 36, 20, 20);
            var chat = SceneCommon.Chat(panel.rectTransform, 54, prefabs.TextLine);
            // GameController toggles the whole chat panel, so put the view on the panel's object.
            var host = panel.gameObject.AddComponent<ChatView>();
            host.log = chat.log;
            host.input = chat.input;
            host.sendButton = chat.sendButton;
            Object.DestroyImmediate(chat);
            return host;
        }

        // ------------------------------------------------------------------ overlays

        static EffectsView BuildEffects(RectTransform overlay, RectTransform shakeTarget)
        {
            var host = EditorUI.Rect(overlay, "Effects").Stretch();
            var flash = EditorUI.Image(host, "Flash", new Color(1, 1, 1, 0));
            flash.rectTransform.Stretch();
            var particles = EditorUI.Rect(host, "Particles").Stretch();
            var view = host.gameObject.AddComponent<EffectsView>();
            view.flash = flash;
            view.shakeTarget = shakeTarget;
            view.particleRoot = particles;
            return view;
        }

        static BannerView BuildBanner(RectTransform overlay)
        {
            var host = EditorUI.Rect(overlay, "Banner").Stretch();
            var group = host.gameObject.AddComponent<CanvasGroup>();
            var panel = EditorUI.Panel(host, "Panel", UIPalette.PanelDeep, SkinSlot.Panel, false);
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1300, 240));
            var title = EditorUI.Text(panel.transform, "Title", "NIGHT 1", 110, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(14, 140, 20, 20);
            title.enableAutoSizing = true;
            title.fontSizeMin = 50;
            title.fontSizeMax = 110;
            var subtitle = EditorUI.Text(panel.transform, "Subtitle", string.Empty, 34, UIPalette.Text, TextAlignmentOptions.Center, false, false, false);
            subtitle.rectTransform.TopBand(158, 56, 30, 30);

            var view = host.gameObject.AddComponent<BannerView>();
            view.group = group;
            view.panel = panel.rectTransform;
            view.background = panel;
            view.titleLabel = title;
            view.subtitleLabel = subtitle;
            return view;
        }

        static GuestPickerView BuildPicker(RectTransform overlay, UIPrefabs prefabs)
        {
            var host = EditorUI.ModalShell(overlay, "GuestPicker", new Vector2(1500, 860), out var root, out var panel);
            var title = EditorUI.Text(panel, "Title", "Choose a guest", 40, UIPalette.Text, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(26, 54, 40, 40);
            var hint = EditorUI.Text(panel, "Hint", string.Empty, 24, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            hint.rectTransform.TopBand(82, 34, 40, 40);
            EditorUI.Scroll(panel, "Guests", out var grid, 16, EditorUI.Pad(10), GuestCardView.CellSize(0.72f)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(30, 112), new Vector2(-30, -128));
            var cancel = EditorUI.Button(panel, "Cancel", "Cancel  [Esc]", ButtonStyle.Ghost, 28);
            cancel.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(300, 72), new Vector2(0.5f, 0));

            var view = host.gameObject.AddComponent<GuestPickerView>();
            view.root = root;
            view.titleLabel = title;
            view.hintLabel = hint;
            view.grid = grid;
            view.cardPrefab = prefabs.GuestCard;
            view.cancelButton = cancel;
            return view;
        }

        static RolodexView BuildRolodex(RectTransform overlay, UIPrefabs prefabs)
        {
            var host = EditorUI.ModalShell(overlay, "Rolodex", new Vector2(1640, 920), out var root, out var panel);
            var title = EditorUI.Text(panel, "Title", "Your Squad", 42, UIPalette.Text, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(24, 56, 40, 40);
            var summary = EditorUI.Text(panel, "Summary", string.Empty, 22, UIPalette.Text, TextAlignmentOptions.Center);
            summary.rectTransform.TopBand(82, 76, 60, 60);
            EditorUI.Scroll(panel, "Guests", out var grid, 14, EditorUI.Pad(10), GuestCardView.CellSize(0.62f)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(30, 112), new Vector2(-30, -166));
            var close = EditorUI.Button(panel, "Close", "Close  [R]", ButtonStyle.Primary, 28);
            close.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(300, 72), new Vector2(0.5f, 0));

            var view = host.gameObject.AddComponent<RolodexView>();
            view.root = root;
            view.titleLabel = title;
            view.summaryLabel = summary;
            view.grid = grid;
            view.cardPrefab = prefabs.GuestCard;
            view.closeButton = close;
            return view;
        }

        static PauseMenuView BuildPauseMenu(RectTransform overlay)
        {
            var host = EditorUI.ModalShell(overlay, "PauseMenu", new Vector2(1180, 820), out var root, out var panel);
            var title = EditorUI.Text(panel, "Title", "Paused", 50, UIPalette.Text, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(26, 64, 40, 40);

            var left = EditorUI.Rect(panel, "Settings").Box(new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(44, 140), new Vector2(-16, -116));
            EditorUI.VLayout(left, 14, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, true, false);
            var venue = EditorUI.Selector(left, "Venue", "Venue");
            EditorUI.Layout(venue, -1, 100);
            var master = MainMenuSceneBuilder.SliderRow(left, "Master", "Master", 170);
            var music = MainMenuSceneBuilder.SliderRow(left, "Music", "Music", 170);
            var sfx = MainMenuSceneBuilder.SliderRow(left, "Sfx", "Effects", 170);
            var fast = EditorUI.Toggle(left, "FastAnimations", "Fast animations");
            EditorUI.Layout(fast, -1, 60);

            var right = EditorUI.Panel(panel, "Rules", UIPalette.PanelAlt, SkinSlot.PanelAlt);
            right.rectTransform.Box(new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(16, 140), new Vector2(-44, -116));
            var rulesHeader = EditorUI.Text(right.transform, "Header", "Quick rules", 26, UIPalette.Gold, TextAlignmentOptions.MidlineLeft, true, true, false);
            rulesHeader.rectTransform.TopBand(16, 36, 24, 24);
            var rules = EditorUI.Text(right.transform, "Text", string.Empty, 21, UIPalette.Text, TextAlignmentOptions.TopLeft);
            rules.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(24, 18), new Vector2(-24, -60));
            rules.enableAutoSizing = true;
            rules.fontSizeMin = 14;
            rules.fontSizeMax = 21;

            var resume = EditorUI.Button(panel, "Resume", "Resume  [Esc]", ButtonStyle.Primary, 30);
            resume.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(-200, 34), new Vector2(360, 84), new Vector2(0.5f, 0));
            var quit = EditorUI.Button(panel, "Quit", "Save & Quit to Menu", ButtonStyle.Danger, 28, out var quitLabel);
            quit.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(200, 34), new Vector2(360, 84), new Vector2(0.5f, 0));

            var view = host.gameObject.AddComponent<PauseMenuView>();
            view.root = root;
            view.titleLabel = title;
            view.resumeButton = resume;
            view.venueSelector = venue;
            view.masterSlider = master;
            view.musicSlider = music;
            view.sfxSlider = sfx;
            view.fastAnimationsToggle = fast;
            view.rulesText = rules;
            view.quitButton = quit;
            view.quitLabel = quitLabel;
            return view;
        }

        static GameOverView BuildGameOver(RectTransform overlay, UIPrefabs prefabs)
        {
            var host = EditorUI.ModalShell(overlay, "GameOver", new Vector2(1300, 900), out var root, out var panel);
            var title = EditorUI.Text(panel, "Title", "VICTORY!", 100, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(26, 124, 40, 40);
            title.enableAutoSizing = true;
            title.fontSizeMin = 48;
            title.fontSizeMax = 100;
            var subtitle = EditorUI.Text(panel, "Subtitle", string.Empty, 30, UIPalette.Text, TextAlignmentOptions.Center);
            subtitle.rectTransform.TopBand(152, 84, 60, 60);
            var stats = EditorUI.Text(panel, "Stats", string.Empty, 22, UIPalette.TextDim, TextAlignmentOptions.Center);
            stats.rectTransform.TopBand(240, 70, 60, 60);

            var ranking = EditorUI.Rect(panel, "Ranking").Box(Vector2.zero, Vector2.one, new Vector2(90, 186), new Vector2(-90, -320));
            var rankingTitle = EditorUI.Text(ranking, "Header", "Standings", 26, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            rankingTitle.rectTransform.TopBand(0, 36);
            EditorUI.Scroll(ranking, "List", out var rankingList, 8, EditorUI.Pad(2)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -44));

            var waiting = EditorUI.Text(panel, "Waiting", string.Empty, 22, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            waiting.rectTransform.BottomBand(128, 40, 60, 60);
            var primary = EditorUI.Button(panel, "Primary", "Play Again", ButtonStyle.Primary, 30, out var primaryLabel);
            primary.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(-200, 36), new Vector2(360, 84), new Vector2(0.5f, 0));
            var secondary = EditorUI.Button(panel, "Secondary", "Main Menu", ButtonStyle.Normal, 30, out var secondaryLabel);
            secondary.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(200, 36), new Vector2(360, 84), new Vector2(0.5f, 0));

            var view = host.gameObject.AddComponent<GameOverView>();
            view.root = root;
            view.titleLabel = title;
            view.subtitleLabel = subtitle;
            view.statsLabel = stats;
            view.rankingGroup = ranking.gameObject;
            view.rankingList = rankingList;
            view.rowPrefab = prefabs.PlayerRow;
            view.waitingLabel = waiting;
            view.primaryButton = primary;
            view.primaryLabel = primaryLabel;
            view.secondaryButton = secondary;
            view.secondaryLabel = secondaryLabel;
            return view;
        }
    }
}
