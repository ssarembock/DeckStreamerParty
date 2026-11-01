using DeckParty.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Builds Assets/DeckParty/Scenes/Game.unity: HUD, the stream (a stage strip on top and the chat bubble the viewers sit
    /// in), stream controls, shop (streamer card, Sub Cap, Audience and the viewer grid), side panel (players, emotes,
    /// optional Activity log, facecam) and overlays (presence, anxious streamer, turn timer, stream overlay, link lines,
    /// Platform Ban / Server Down images).
    /// Static captions are localization keys (see EditorLoc); labels the views fill at runtime use plain placeholders.
    /// </summary>
    internal static class GameSceneBuilder
    {
        /// <summary>Height of the strip above the chat bubble, where the stream setup's computer shows (1080p pixels).</summary>
        const float StageHeight = 190f;
        /// <summary>Height of the stream controls along the bottom.</summary>
        const float ControlsHeight = 186f;
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
            // The content frame caps the UI at 21:9 and centres it; the stream setup fills the rest of the screen.
            var root = EditorUI.Rect(canvas, "Root").Stretch();
            var frame = root.gameObject.AddComponent<ContentFrame>();

            var hud = BuildHud(root);

            var mainArea = EditorUI.Rect(root, "MainArea").Box(Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-456, -130));
            var partyRoot = EditorUI.Rect(mainArea, "Party").Stretch();
            var house = BuildHouse(partyRoot, prefabs, out var links, out var bubble, out var viewerEmojis);
            var targeting = BuildTargetingBanner(partyRoot);
            var controls = BuildPartyControls(partyRoot, prefabs);
            var streamOverlay = BuildStreamOverlay(partyRoot);
            var planning = BuildPlanning(mainArea, prefabs);
            var raid = BuildRaidBlame(mainArea, prefabs);

            // Players and emotes (multiplayer), the optional Activity log, then the facecam in the bottom-right corner.
            var side = EditorUI.Rect(root, "SidePanel").Box(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-440, 16), new Vector2(-16, -130));
            EditorUI.VLayout(side, 12, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, true, false);
            var opponents = BuildOpponents(side, prefabs);
            var emotes = BuildEmoteBar(side);
            var feed = BuildFeed(side, prefabs, out var log, out var chat);
            // Keeps the facecam at the bottom while the Activity log is hidden.
            var spacer = EditorUI.Rect(side, "Spacer");
            EditorUI.Layout(spacer, -1, 0, -1, 1);
            var facecam = BuildFacecam(side);
            frame.sidePanel = side;
            frame.mainArea = mainArea;

            var overlay = SceneCommon.CreateCanvas("OverlayCanvas", 100);
            var presence = BuildPresence(overlay);
            var anxiety = BuildAnxiety(overlay);
            var turnTimer = BuildTurnTimer(overlay);
            var effects = BuildEffects(overlay, root);
            emotes.floatArea = effects.particleRoot;
            var banner = BuildBanner(overlay);
            var splash = BuildOutcomeSplash(overlay);
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
            controller.feed = feed;
            controller.facecam = facecam;
            controller.chatBubble = bubble;
            controller.viewerEmojis = viewerEmojis;
            controller.outcomeSplash = splash;
            controller.picker = picker;
            controller.rolodex = rolodex;
            controller.gameOver = gameOver;
            controller.pauseMenu = pause;
            controller.banner = banner;
            controller.effects = effects;
            controller.background = background;
            controller.anxiety = anxiety;
            controller.presence = presence;
            controller.emotes = emotes;
            controller.overlay = streamOverlay;
            controller.turnTimer = turnTimer;
            controller.links = links;

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
            var scenario = EditorUI.Text(info, "Scenario", "-", 20, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
            EditorUI.Layout(scenario, -1, 28);
            var night = EditorUI.Text(info, "Night", "Night 1/25", 40, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            EditorUI.Layout(night, -1, 50);

            var pop = Stat(bar.transform, "Pop", "term.gifted_subs", EditorUI.Sparkle, UIPalette.Gold, SkinSlot.IconPop, 190);
            var cash = Stat(bar.transform, "Cash", "term.cash", EditorUI.Coin, UIPalette.Green, SkinSlot.IconCash, 170);
            var capacity = Stat(bar.transform, "House", "term.sub_cap", EditorUI.Door, UIPalette.Teal, SkinSlot.IconDoor, 190);

            var trouble = Meter(bar.transform, "Trouble", EditorUI.Cross, UIPalette.Border, SkinSlot.IconTrouble, 3, 230, out var troublePips, out var troubleLabel);
            var stars = Meter(bar.transform, "Stars", EditorUI.Star, UIPalette.Border, SkinSlot.IconStar, 4, 240, out var starPips, out var starsLabel);

            var spacer = EditorUI.Rect(bar.transform, "Spacer");
            EditorUI.Layout(spacer, -1, -1, 1);

            var rolodex = EditorUI.Button(bar.transform, "Rolodex", "Audience [R]", ButtonStyle.Normal, 24);
            EditorUI.Layout(rolodex, 190, 64, 0);
            var menu = EditorUI.Button(bar.transform, "Menu", "hud.menu", ButtonStyle.Normal, 24);
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
            label.fontStyle |= FontStyles.UpperCase;
            EditorUI.Layout(label, -1, 22);
            var value = EditorUI.Text(texts, "Value", "0", 40, color, TextAlignmentOptions.MidlineLeft, true, true, false);
            EditorUI.Layout(value, -1, 48);
            // HudView fills the tooltip text in the current language.
            EditorUI.Tooltip(block);
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

            label = EditorUI.Text(block, "Label", "-", 22, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, false, false);
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

        /// <summary>
        /// The stream: a stage strip along the top (the stream setup's optional header image; otherwise the background shows
        /// through, computer and all), and below it the chat bubble the viewers sit in. The bubble's tail sits on its top
        /// edge, pointing up at the computer (ChatBubbleView places it from the stream setup's Chat Tail Position).
        /// </summary>
        static HouseView BuildHouse(RectTransform partyRoot, UIPrefabs prefabs, out LinkLinesView links, out ChatBubbleView bubbleView, out ViewerEmojiView emojiView)
        {
            var stage = EditorUI.Rect(partyRoot, "Stage").TopBand(0, StageHeight);
            var headerImage = EditorUI.Image(stage, "HeaderImage", Color.white);
            headerImage.rectTransform.Stretch();
            headerImage.preserveAspect = true;
            headerImage.enabled = false;

            var bubble = EditorUI.Image(partyRoot, "ChatBubble", new Color(0.13f, 0.10f, 0.22f, 0.86f), EditorUI.Rounded);
            bubble.type = Image.Type.Sliced;
            bubble.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(2, ControlsHeight + 10), new Vector2(-2, -StageHeight));
            // The tail draws over the bubble's edge and under the cards (ChatBubbleView sizes and places it).
            var tail = EditorUI.Image(bubble.transform, "Tail", new Color(0.13f, 0.10f, 0.22f, 0.86f));
            tail.preserveAspect = true;

            var header = EditorUI.Text(bubble.transform, "Header", "-", 28, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            header.rectTransform.TopBand(14, 40, 30, 30);
            var hint = EditorUI.Text(bubble.transform, "Hint", string.Empty, 22, UIPalette.Orange, TextAlignmentOptions.MidlineLeft, false, false, false);
            hint.rectTransform.TopBand(54, 30, 30, 30);

            var area = EditorUI.Rect(bubble.transform, "HouseArea").Box(Vector2.zero, Vector2.one, new Vector2(18, 16), new Vector2(-18, -88));

            // Link lines (Overly Attached Girlfriend, Nyan Cat chains) draw under the cards.
            var linksRect = EditorUI.Rect(area, "LinkLines").Stretch();
            links = linksRect.gameObject.AddComponent<LinkLinesView>();

            // Cards shrink to fit down to HouseView.MinCardScale, then the chat scrolls.
            var scrollRoot = EditorUI.Rect(area, "Scroll").Stretch();
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            var viewport = EditorUI.Rect(scrollRoot, "Viewport").Stretch();
            viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var gridRect = EditorUI.Rect(viewport, "Grid");
            gridRect.anchorMin = new Vector2(0, 0.5f);
            gridRect.anchorMax = new Vector2(1, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.offsetMin = gridRect.offsetMax = Vector2.zero;
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(GuestCardView.DesignWidth, GuestCardView.DesignHeight);
            grid.spacing = new Vector2(16, 16);
            grid.padding = EditorUI.Pad(6);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            EditorUI.Fit(gridRect, false, true);
            scroll.viewport = viewport;
            scroll.content = gridRect;

            bubbleView = bubble.gameObject.AddComponent<ChatBubbleView>();
            bubbleView.bubble = bubble;
            bubbleView.tail = tail;
            bubbleView.header = headerImage;

            var view = partyRoot.gameObject.AddComponent<HouseView>();
            view.area = area;
            view.grid = grid;
            view.scroll = scroll;
            view.cardPrefab = prefabs.GuestCard;
            view.headerLabel = header;
            view.hintLabel = hint;

            // Viewer emojis float above the cards (and above the bubble's edge).
            var emojiLayer = EditorUI.Rect(partyRoot, "ViewerEmojis").Stretch();
            emojiView = emojiLayer.gameObject.AddComponent<ViewerEmojiView>();
            emojiView.house = view;
            emojiView.layer = emojiLayer;
            return view;
        }

        // ------------------------------------------------------------------ polish overlays

        static StreamOverlayView BuildStreamOverlay(RectTransform partyRoot)
        {
            var rect = EditorUI.Rect(partyRoot, "StreamOverlay").Place(new Vector2(1, 1), new Vector2(-12, -4), new Vector2(220, 38), new Vector2(1, 1));
            return rect.gameObject.AddComponent<StreamOverlayView>();
        }

        static PresenceView BuildPresence(RectTransform overlay)
        {
            var rect = EditorUI.Rect(overlay, "Presence").Stretch();
            return rect.gameObject.AddComponent<PresenceView>();
        }

        static StreamerAnxietyView BuildAnxiety(RectTransform overlay)
        {
            var rect = EditorUI.Rect(overlay, "AnxiousStreamer").Place(new Vector2(0.5f, 1), new Vector2(-220, -136), new Vector2(660, 124), new Vector2(0.5f, 1));
            return rect.gameObject.AddComponent<StreamerAnxietyView>();
        }

        static TurnTimerView BuildTurnTimer(RectTransform overlay)
        {
            var rect = EditorUI.Rect(overlay, "TurnTimer").Place(new Vector2(1, 1), new Vector2(-480, -140), new Vector2(300, 48), new Vector2(1, 1));
            return rect.gameObject.AddComponent<TurnTimerView>();
        }

        static TargetingBannerView BuildTargetingBanner(RectTransform partyRoot)
        {
            var host = EditorUI.Rect(partyRoot, "Targeting").TopBand(84, 72, 160, 160);
            var panel = EditorUI.Panel(host, "Root", UIPalette.Purple.WithAlpha(0.95f), SkinSlot.PanelAlt);
            panel.rectTransform.Stretch();
            var label = EditorUI.Text(panel.transform, "Label", "-", 24, UIPalette.Text, TextAlignmentOptions.MidlineLeft);
            label.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(24, 4), new Vector2(-170, -4));
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = 24;
            var cancel = EditorUI.Button(panel.transform, "Cancel", "common.cancel", ButtonStyle.Ghost, 24);
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
            bar.rectTransform.BottomBand(0, ControlsHeight);

            var door = EditorUI.Button(bar.transform, "Door", "Add Viewer to Chat", ButtonStyle.Primary, 36, out var doorLabel);
            door.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(-255, 16), new Vector2(480, 126));
            doorLabel.rectTransform.TopBand(16, 52, 16, 16);
            var doorSub = EditorUI.Text(door.transform, "Sub", string.Empty, 20, UIPalette.Text.WithAlpha(0.9f), TextAlignmentOptions.Center, false, false, false);
            doorSub.rectTransform.BottomBand(14, 34, 12, 12);
            var doorTip = EditorUI.Tooltip(door);

            var end = EditorUI.Button(bar.transform, "EndParty", "End the Stream", ButtonStyle.Gold, 36, out var endLabel);
            end.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0.5f), new Vector2(255, 16), new Vector2(480, 126));
            endLabel.rectTransform.TopBand(16, 52, 16, 16);
            var endSub = EditorUI.Text(end.transform, "Sub", string.Empty, 20, UIPalette.Night, TextAlignmentOptions.Center, true, false, false);
            endSub.rectTransform.BottomBand(14, 34, 12, 12);
            EditorUI.Tooltip(end);

            var risk = EditorUI.Text(bar.transform, "Risk", string.Empty, 22, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            risk.rectTransform.BottomBand(6, 32, 20, 20);

            // Peek decision panel floats above the controls.
            var peek = EditorUI.Panel(bar.transform, "Peek", UIPalette.PanelDeep);
            peek.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, 12), new Vector2(680, 384), new Vector2(0.5f, 0));
            var peekTitle = EditorUI.Text(peek.transform, "Title", "-", 28, UIPalette.Teal, TextAlignmentOptions.MidlineLeft, true, true, false);
            peekTitle.rectTransform.TopBand(14, 40, 28, 28);
            var cardGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.GuestCard.gameObject, peek.transform);
            var peekCard = cardGo.GetComponent<GuestCardView>();
            ((RectTransform)cardGo.transform).Place(new Vector2(0, 0), new Vector2(28, 22), new Vector2(GuestCardView.DesignWidth, GuestCardView.DesignHeight), new Vector2(0, 0));
            var admit = EditorUI.Button(peek.transform, "Admit", "controls.approve", ButtonStyle.Primary, 30);
            admit.GetComponent<RectTransform>().Place(new Vector2(1, 0.5f), new Vector2(-30, 30), new Vector2(360, 96), new Vector2(1, 0.5f));
            var reject = EditorUI.Button(peek.transform, "Reject", "controls.deny", ButtonStyle.Danger, 30);
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

        /// <summary>
        /// The shop: the left column holds the player's streamer card (with its active), Raise Sub Cap, the Audience button
        /// and the next-night button; the rest is a grid of viewers to recruit (name, picture, group icon, price, copies left).
        /// </summary>
        static PlanningView BuildPlanning(RectTransform mainArea, UIPrefabs prefabs)
        {
            const float columnWidth = 470f;
            var root = EditorUI.Rect(mainArea, "Planning").Stretch();

            var left = EditorUI.Panel(root, "Streamer");
            left.rectTransform.Box(new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(columnWidth, 0));
            EditorUI.VLayout(left, 16, EditorUI.Pad(24, 24), TextAnchor.UpperCenter, true, true, true, false);

            var power = BuildStreamerPower(left.transform);

            var expand = EditorUI.Button(left.transform, "Expand", "Raise Sub Cap", ButtonStyle.Secondary, 25, out var expandLabel);
            EditorUI.Layout(expand, -1, 80);
            var expandTip = EditorUI.Tooltip(expand);
            var rolodex = EditorUI.Button(left.transform, "Rolodex", "View Audience", ButtonStyle.Normal, 25, out var rolodexLabel);
            EditorUI.Layout(rolodex, -1, 72);

            var spacer = EditorUI.Rect(left.transform, "Spacer");
            EditorUI.Layout(spacer, -1, 10, -1, 1);

            var waiting = EditorUI.Text(left.transform, "Waiting", string.Empty, 21, UIPalette.TextDim, TextAlignmentOptions.Center);
            EditorUI.Layout(waiting, -1, 60);
            var next = EditorUI.Button(left.transform, "NextNight", "-", ButtonStyle.Primary, 34, out var nextLabel);
            EditorUI.Layout(next, -1, 100);

            var shop = EditorUI.Panel(root, "Shop");
            shop.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(columnWidth + 20, 0), Vector2.zero);
            // PlanningView writes the header (it names the player when you're watching someone else's shop).
            var shopTitle = EditorUI.Text(shop.transform, "Header", "-", 38, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            shopTitle.rectTransform.TopBand(18, 52, 30, 30);
            var shopHint = EditorUI.Text(shop.transform, "Hint", "shop.hint", 20, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft);
            shopHint.rectTransform.TopBand(70, 32, 30, 30);
            var scroll = EditorUI.Scroll(shop.transform, "Grid", out var shopContent, 16, EditorUI.Pad(8), new Vector2(220, 300));
            scroll.GetComponent<RectTransform>().Box(Vector2.zero, Vector2.one, new Vector2(20, 20), new Vector2(-20, -112));
            var grid = shopContent.GetComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.UpperLeft;

            var view = root.gameObject.AddComponent<PlanningView>();
            view.headerLabel = shopTitle;
            view.expandButton = expand;
            view.expandLabel = expandLabel;
            view.expandTooltip = expandTip;
            view.rolodexButton = rolodex;
            view.rolodexLabel = rolodexLabel;
            view.nextNightButton = next;
            view.nextNightLabel = nextLabel;
            view.waitingLabel = waiting;
            view.shopViewport = scroll.viewport != null ? scroll.viewport : (RectTransform)shopContent.parent;
            view.shopGrid = grid;
            view.shopItemPrefab = prefabs.ShopItem;
            view.power = power;
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>The streamer power slot in the shop column: portrait, power name, description, status and Use button.</summary>
        static StreamerPowerView BuildStreamerPower(Transform parent)
        {
            var panel = EditorUI.Panel(parent, "StreamerPower", UIPalette.PanelAlt, SkinSlot.PanelAlt);
            EditorUI.Layout(panel, -1, 400);

            // Portrait on top, then name, power and description, with the active's status and Use button along the bottom.
            var content = EditorUI.Rect(panel.transform, "Content").Stretch();
            var frame = EditorUI.Rect(content, "Portrait").Place(new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(170, 170), new Vector2(0.5f, 1));
            var placeholder = EditorUI.Image(frame, "Placeholder", UIPalette.Purple, EditorUI.Circle);
            placeholder.rectTransform.Stretch();
            var initials = EditorUI.Text(placeholder.transform, "Initials", "?", 40, Color.white, TextAlignmentOptions.Center, true, true, false);
            initials.rectTransform.Stretch();
            var portrait = EditorUI.Image(frame, "Art", Color.white);
            portrait.rectTransform.Stretch();
            portrait.preserveAspect = true;
            portrait.enabled = false;

            var name = EditorUI.Text(content, "Name", "-", 28, UIPalette.Text, TextAlignmentOptions.Center, true, true, false);
            name.rectTransform.TopBand(190, 34, 16, 16);
            var kind = EditorUI.Text(content, "Kind", "-", 16, UIPalette.Gold, TextAlignmentOptions.Center, true, false, false);
            kind.rectTransform.TopBand(224, 22, 16, 16);
            var powerName = EditorUI.Text(content, "Power", "-", 22, UIPalette.Pink, TextAlignmentOptions.Center, true, false, false);
            powerName.rectTransform.TopBand(248, 28, 16, 16);
            var description = EditorUI.Text(content, "Description", "-", 17, UIPalette.Text, TextAlignmentOptions.Top);
            description.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(18, 70), new Vector2(-18, -278));
            var status = EditorUI.Text(content, "Status", "-", 15, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
            status.rectTransform.BottomBand(12, 46, 16, 214);
            var use = EditorUI.Button(content, "Use", "-", ButtonStyle.Gold, 20, out var useLabel);
            use.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-14, 12), new Vector2(190, 48), new Vector2(1, 0));
            var useTip = EditorUI.Tooltip(use);

            var empty = EditorUI.Text(panel.transform, "Empty", "power.none", 22, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            empty.rectTransform.Stretch(20, 10, 20, 10);

            var view = panel.gameObject.AddComponent<StreamerPowerView>();
            view.portrait = portrait;
            view.placeholder = placeholder;
            view.initialsLabel = initials;
            view.nameLabel = name;
            view.kindLabel = kind;
            view.powerLabel = powerName;
            view.descriptionLabel = description;
            view.statusLabel = status;
            view.useButton = use;
            view.useLabel = useLabel;
            view.useTooltip = useTip;
            view.emptyState = empty.gameObject;
            view.content = content.gameObject;
            return view;
        }

        static RaidBlameView BuildRaidBlame(RectTransform mainArea, UIPrefabs prefabs)
        {
            var panel = EditorUI.Panel(mainArea, "RaidBlame", UIPalette.PanelDeep.WithAlpha(0.96f));
            panel.rectTransform.Stretch();
            var title = EditorUI.Text(panel.transform, "Title", "-", 80, UIPalette.Red, TextAlignmentOptions.Center, true, true, false);
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
            EditorUI.Layout(panel, -1, 280, -1, -1, -1, 200);
            var title = EditorUI.Text(panel.transform, "Header", "-", 26, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            title.rectTransform.TopBand(12, 36, 20, 20);
            EditorUI.Scroll(panel.transform, "List", out var list, 8, EditorUI.Pad(2)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -54));

            var view = panel.gameObject.AddComponent<OpponentsView>();
            view.titleLabel = title;
            view.list = list;
            view.rowPrefab = prefabs.PlayerRow;
            return view;
        }

        /// <summary>
        /// Multiplayer emotes: the sprite buttons from Game Database > Multiplayer Emotes. Pressing one pops the emote up from
        /// your avatar in the Players panel on everyone's screen.
        /// </summary>
        static EmoteView BuildEmoteBar(RectTransform side)
        {
            var panel = EditorUI.Panel(side, GameController.EmoteBarName);
            EditorUI.Layout(panel, -1, 200, -1, -1, -1, 130);
            var buttons = EditorUI.Rect(panel.transform, "Buttons").Stretch(12, 12, 12, 12);
            var emotes = buttons.gameObject.AddComponent<EmoteView>();
            emotes.buttonSize = 56f;
            panel.gameObject.SetActive(false);
            return emotes;
        }

        /// <summary>
        /// One panel with two tabs: Activity (the activity log, off until switched on in the menu) and Chat (multiplayer text
        /// chat, off while FX Library > Text Chat is). With both off the panel hides.
        /// </summary>
        static SideTabsView BuildFeed(RectTransform side, UIPrefabs prefabs, out EventLogView log, out ChatView chat)
        {
            var panel = EditorUI.Panel(side, "Feed");
            EditorUI.Layout(panel, -1, 200, -1, 1000, -1, 100);

            var tabs = EditorUI.Rect(panel.transform, "Tabs").TopBand(10, 46, 12, 12);
            EditorUI.HLayout(tabs, 8, EditorUI.Pad(0), TextAnchor.MiddleLeft, true, true, false, true);
            var activityTab = Tab(tabs, "ActivityTab", "tab.activity", out var activityLabel, out var activityUnderline);
            var chatTab = Tab(tabs, "ChatTab", "tab.chat", out var chatLabel, out var chatUnderline);

            var activityPage = EditorUI.Rect(panel.transform, "Activity").Box(Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -64));
            log = SceneCommon.LogList(activityPage, "Lines", prefabs.TextLine, 150);
            log.GetComponent<RectTransform>().Stretch();

            var chatPage = EditorUI.Rect(panel.transform, "Chat").Box(Vector2.zero, Vector2.one, new Vector2(12, 12), new Vector2(-12, -64));
            chat = SceneCommon.Chat(chatPage, 0, prefabs.TextLine);
            chat.GetComponent<RectTransform>().Stretch();
            chatPage.gameObject.SetActive(false);

            panel.gameObject.SetActive(false);
            var view = panel.gameObject.AddComponent<SideTabsView>();
            view.activityTab = activityTab;
            view.activityLabel = activityLabel;
            view.activityUnderline = activityUnderline;
            view.activityPage = activityPage.gameObject;
            view.activityLog = log;
            view.chatTab = chatTab;
            view.chatLabel = chatLabel;
            view.chatUnderline = chatUnderline;
            view.chatPage = chatPage.gameObject;
            view.chatLog = chat.log;
            return view;
        }

        /// <summary>A tab button. SideTabsView writes the caption (with the unread count), so it gets no LocalizedLabel.</summary>
        static Button Tab(RectTransform parent, string name, string captionKey, out TextMeshProUGUI label, out GameObject underline)
        {
            var button = EditorUI.Button(parent, name, "-", ButtonStyle.Normal, 22, out label);
            EditorUI.Layout(button, -1, 46, 1);
            label.text = EditorLoc.Get(captionKey);
            var line = EditorUI.Image(button.transform, "Underline", UIPalette.Pink, EditorUI.RoundedSmall);
            line.rectTransform.BottomBand(0, 4, 14, 14);
            underline = line.gameObject;
            return button;
        }

        /// <summary>The facecam along the bottom: the streamer's square (FacecamView keeps it square and sizes the row).</summary>
        static FacecamView BuildFacecam(RectTransform side)
        {
            var row = EditorUI.Rect(side, "Facecam");
            var element = EditorUI.Layout(row, -1, 424, -1, 0, -1, 424);

            var frame = EditorUI.Rect(row, "Frame").Place(new Vector2(1, 0), new Vector2(-212, 212), new Vector2(424, 424), new Vector2(0.5f, 0.5f));
            var border = EditorUI.Image(frame, "Border", UIPalette.Border, EditorUI.Rounded);
            border.rectTransform.Stretch();
            var screen = EditorUI.Image(frame, "Screen", UIPalette.Night, EditorUI.Rounded);
            screen.rectTransform.Stretch(6, 6, 6, 6);
            var mask = EditorUI.Rect(screen.transform, "Mask").Stretch(4, 4, 4, 4);
            mask.gameObject.AddComponent<RectMask2D>();
            var content = EditorUI.Rect(mask, "Content").Stretch();

            var glow = EditorUI.Image(content, "Glow", UIPalette.Purple.WithAlpha(0.28f), EditorUI.Circle);
            glow.rectTransform.Box(new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            var placeholderArea = EditorUI.Rect(content, "PlaceholderArea").Box(new Vector2(0.2f, 0.26f), new Vector2(0.8f, 0.86f), Vector2.zero, Vector2.zero);
            var placeholder = EditorUI.Image(placeholderArea, "Placeholder", UIPalette.Purple, EditorUI.Circle);
            var fitter = placeholder.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            var initials = EditorUI.Text(placeholder.transform, "Initials", "?", 96, Color.white, TextAlignmentOptions.Center, true, true, false);
            initials.rectTransform.Stretch();

            // The streamer stands on the bottom edge (pivot at the feet, so idle breathing grows upward).
            var art = EditorUI.Image(content, "Art", Color.white);
            art.rectTransform.Box(new Vector2(0.03f, 0f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
            art.rectTransform.pivot = new Vector2(0.5f, 0f);
            art.preserveAspect = true;
            art.enabled = false;
            var portrait = art.gameObject.AddComponent<CharacterPortrait>();
            portrait.image = art;

            var plate = EditorUI.Image(screen.transform, "NamePlate", UIPalette.PanelDeep.WithAlpha(0.88f), EditorUI.RoundedSmall);
            plate.rectTransform.BottomBand(12, 60, 12, 12);
            var name = EditorUI.Text(plate.transform, "Name", "-", 26, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            name.rectTransform.Stretch(14, 4, 14, 4);

            var rec = EditorUI.Image(screen.transform, "RecDot", UIPalette.Red, EditorUI.Circle);
            rec.rectTransform.Place(new Vector2(1, 1), new Vector2(-16, -16), new Vector2(18, 18), new Vector2(1, 1));

            var view = row.gameObject.AddComponent<FacecamView>();
            view.frame = frame;
            view.border = border;
            view.content = content;
            view.glow = glow;
            view.portrait = portrait;
            view.placeholder = placeholder;
            view.initialsLabel = initials;
            view.nameLabel = name;
            view.recDot = rec;
            view.layoutElement = element;
            return view;
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
            var title = EditorUI.Text(panel.transform, "Title", "-", 110, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
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

        /// <summary>The full-screen Platform Ban / Server Down image (Game Database > Stream Endings), with its caption.</summary>
        static OutcomeSplashView BuildOutcomeSplash(RectTransform overlay)
        {
            var host = EditorUI.Rect(overlay, "StreamEnding").Stretch();
            var root = EditorUI.Rect(host, "Root").Stretch();
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var dim = EditorUI.Image(root, "Dim", new Color(0.02f, 0.01f, 0.05f, 0.62f), null, null, true);
            dim.rectTransform.Stretch();
            var art = EditorUI.Image(root, "Art", Color.white);
            art.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1200, 640));
            art.preserveAspect = true;
            var caption = EditorUI.Text(root, "Caption", "-", 64, UIPalette.Red, TextAlignmentOptions.Center, true, true, false);
            caption.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(1400, 150));
            caption.outlineWidth = 0.2f;
            caption.outlineColor = new Color32(0, 0, 0, 230);

            var view = host.gameObject.AddComponent<OutcomeSplashView>();
            view.root = root.gameObject;
            view.group = group;
            view.art = art;
            view.caption = caption;
            root.gameObject.SetActive(false);
            return view;
        }

        static GuestPickerView BuildPicker(RectTransform overlay, UIPrefabs prefabs)
        {
            var host = EditorUI.ModalShell(overlay, "GuestPicker", new Vector2(1500, 860), out var root, out var panel);
            var title = EditorUI.Text(panel, "Title", "-", 40, UIPalette.Text, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(26, 54, 40, 40);
            var hint = EditorUI.Text(panel, "Hint", string.Empty, 24, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            hint.rectTransform.TopBand(82, 34, 40, 40);
            EditorUI.Scroll(panel, "Guests", out var grid, 16, EditorUI.Pad(10), GuestCardView.CellSize(0.72f)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(30, 112), new Vector2(-30, -128));
            var cancel = EditorUI.Button(panel, "Cancel", "Cancel  [Esc]", ButtonStyle.Ghost, 28);
            cancel.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(340, 72), new Vector2(0.5f, 0));

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
            var title = EditorUI.Text(panel, "Title", "-", 42, UIPalette.Text, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(24, 56, 40, 40);
            var summary = EditorUI.Text(panel, "Summary", string.Empty, 22, UIPalette.Text, TextAlignmentOptions.Center);
            summary.rectTransform.TopBand(82, 76, 60, 60);
            EditorUI.Scroll(panel, "Guests", out var grid, 14, EditorUI.Pad(10), GuestCardView.CellSize(0.62f)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, new Vector2(30, 112), new Vector2(-30, -166));
            var close = EditorUI.Button(panel, "Close", "common.close_r", ButtonStyle.Primary, 28);
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
            // Three columns: stream setup + audio | display & language | quick rules.
            var host = EditorUI.ModalShell(overlay, "PauseMenu", new Vector2(1720, 900), out var root, out var panel);
            var title = EditorUI.Text(panel, "Title", "-", 50, UIPalette.Text, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(26, 64, 40, 40);

            var left = EditorUI.Rect(panel, "Settings").Box(new Vector2(0, 0), new Vector2(0.34f, 1), new Vector2(44, 140), new Vector2(-16, -116));
            EditorUI.VLayout(left, 14, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, true, false);
            var venue = EditorUI.Selector(left, "Venue", "pause.setup");
            EditorUI.Layout(venue, -1, 100);
            var master = MainMenuSceneBuilder.SliderRow(left, "Master", "settings.master", 200);
            var music = MainMenuSceneBuilder.SliderRow(left, "Music", "settings.music", 200);
            var sfx = MainMenuSceneBuilder.SliderRow(left, "Sfx", "settings.sfx", 200);
            var fast = EditorUI.Toggle(left, "FastAnimations", "settings.fast_animations");
            EditorUI.Layout(fast, -1, 60);

            var middle = EditorUI.Rect(panel, "DisplayColumn").Box(new Vector2(0.34f, 0), new Vector2(0.68f, 1), new Vector2(16, 140), new Vector2(-16, -116));
            EditorUI.VLayout(middle, 8, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, true, false);
            var display = SceneCommon.DisplayBlock(middle, 220f);

            var right = EditorUI.Panel(panel, "Rules", UIPalette.PanelAlt, SkinSlot.PanelAlt);
            right.rectTransform.Box(new Vector2(0.68f, 0), new Vector2(1, 1), new Vector2(16, 140), new Vector2(-44, -116));
            var rulesHeader = EditorUI.Text(right.transform, "Header", "pause.rules", 26, UIPalette.Gold, TextAlignmentOptions.MidlineLeft, true, true, false);
            rulesHeader.rectTransform.TopBand(16, 36, 24, 24);
            var rules = EditorUI.Text(right.transform, "Text", string.Empty, 21, UIPalette.Text, TextAlignmentOptions.TopLeft);
            rules.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(24, 18), new Vector2(-24, -60));
            rules.enableAutoSizing = true;
            rules.fontSizeMin = 14;
            rules.fontSizeMax = 21;

            var resume = EditorUI.Button(panel, "Resume", "pause.resume_esc", ButtonStyle.Primary, 30);
            resume.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(-200, 34), new Vector2(360, 84), new Vector2(0.5f, 0));
            var quit = EditorUI.Button(panel, "Quit", "-", ButtonStyle.Danger, 28, out var quitLabel);
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
            view.display = display;
            view.rulesText = rules;
            view.quitButton = quit;
            view.quitLabel = quitLabel;
            return view;
        }

        static GameOverView BuildGameOver(RectTransform overlay, UIPrefabs prefabs)
        {
            var host = EditorUI.ModalShell(overlay, "GameOver", new Vector2(1300, 900), out var root, out var panel);
            var title = EditorUI.Text(panel, "Title", "-", 100, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(26, 124, 40, 40);
            title.enableAutoSizing = true;
            title.fontSizeMin = 48;
            title.fontSizeMax = 100;
            var subtitle = EditorUI.Text(panel, "Subtitle", string.Empty, 30, UIPalette.Text, TextAlignmentOptions.Center);
            subtitle.rectTransform.TopBand(152, 84, 60, 60);
            var stats = EditorUI.Text(panel, "Stats", string.Empty, 22, UIPalette.TextDim, TextAlignmentOptions.Center);
            stats.rectTransform.TopBand(240, 70, 60, 60);

            var ranking = EditorUI.Rect(panel, "Ranking").Box(Vector2.zero, Vector2.one, new Vector2(90, 186), new Vector2(-90, -320));
            var rankingTitle = EditorUI.Text(ranking, "Header", "over.standings", 26, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            rankingTitle.rectTransform.TopBand(0, 36);
            EditorUI.Scroll(ranking, "List", out var rankingList, 8, EditorUI.Pad(2)).GetComponent<RectTransform>()
                .Box(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -44));

            var waiting = EditorUI.Text(panel, "Waiting", string.Empty, 22, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            waiting.rectTransform.BottomBand(128, 40, 60, 60);
            var primary = EditorUI.Button(panel, "Primary", "-", ButtonStyle.Primary, 30, out var primaryLabel);
            primary.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(-200, 36), new Vector2(360, 84), new Vector2(0.5f, 0));
            var secondary = EditorUI.Button(panel, "Secondary", "-", ButtonStyle.Normal, 30, out var secondaryLabel);
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
