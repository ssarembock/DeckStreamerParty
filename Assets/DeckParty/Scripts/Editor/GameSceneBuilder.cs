using DeckParty.Core;
using DeckParty.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Builds Assets/DeckParty/Scenes/Game.unity in the V5 "streaming website" look: a light page with a white rounded menu bar
    /// floating at the top (the animated platform logo, then stream, Clout, Cash, Sub Cap, risk and Plays as icons and numbers in
    /// sections, then the Audience and Menu icon buttons). During a stream the page shows the Stream Chat window (black border,
    /// a title, round viewer avatars and a footer picture), the Add Viewer and End the Stream buttons under it, and beside it a
    /// channel column (the facecam with the LIVE badge, and the Community Beats panel). The shop is two white cards: the
    /// streamer (active, Raise Sub Cap, Audience, Go Live) and the viewer grid. The side panel on the left (players, emotes, optional
    /// Activity log) and the overlays (presence, anxious streamer, turn timer, link lines, endings, round summary) follow.
    /// Static captions are localization keys (see EditorLoc); labels the views fill at runtime use plain placeholders.
    /// </summary>
    internal static class GameSceneBuilder
    {
        /// <summary>
        /// The site's menu bar (1080p pixels): a plain rectangle along the top edge of the screen, the full width of the screen,
        /// with a border along its bottom edge.
        /// </summary>
        const float HeaderHeight = 128f;
        /// <summary>The Audience button: as wide as a counter (its icon and the number of viewers still in the queue).</summary>
        const float AudienceButtonWidth = 150f;
        /// <summary>Space between the top of the screen and the menu bar (none: the bar starts at the top edge).</summary>
        const float BarTop = 0f;
        /// <summary>Space between the screen's sides and the menu bar's outer items (the logo, the Menu button).</summary>
        const float BarPadding = 24f;
        /// <summary>Space between the menu bar and the panels under it.</summary>
        const float BelowBar = 16f;
        /// <summary>Where the panels under the menu bar start, from the top of the screen.</summary>
        const float ContentTop = BarTop + HeaderHeight + BelowBar;
        /// <summary>Page margin and the gap between cards.</summary>
        const float Gap = 16f;
        /// <summary>Border thickness of the site's panels (pure black).</summary>
        const float PanelBorder = 4.5f;
        /// <summary>How far the grey dividers inside a panel stop short of its left and right edges.</summary>
        const float DividerInset = 20f;
        /// <summary>Thickness of the grey dividers: horizontal ones (under titles) and vertical ones (menu bar sections, summary columns).</summary>
        const float HDivider = 4f;
        const float VDivider = 3f;
        /// <summary>The Stream Chat window's title band (it shows the Deplatform Risk pips).</summary>
        const float ChatHeaderHeight = 176f;
        /// <summary>The Deplatform Risk pips in the chat's title band (they replaced the Stream Chat title icon and left the menu bar).</summary>
        const float RiskPipSize = 92f;
        const float RiskPipGap = 20f;
        /// <summary>
        /// The band above the buttons kept for the near-deplatform warning (no picture any more). It was 224 px with the footer
        /// picture; of the 74 px it gave up, 16 went to the button bases (ButtonsBand) and 58 to the viewers.
        /// </summary>
        const float FooterHeight = 150f;
        /// <summary>The near-deplatform warning's panel (StreamerAnxietyView caps its width to the chat window).</summary>
        static readonly Vector2 AnxietySize = new Vector2(1480f, 140f);
        /// <summary>
        /// The band along the bottom of the Stream Chat window that holds the Add Viewer and End the Stream buttons and the
        /// rounded bases under them. The window reaches the bottom of the stream, level with the facecam, and the buttons sit
        /// inside its border (where the row under the window used to be).
        /// </summary>
        const float ButtonsBand = 128f;
        /// <summary>The two main buttons: 480 = the old 320 + 50%.</summary>
        const float MainButtonWidth = 480f;
        const float MainButtonHeight = 88f;
        /// <summary>
        /// The rounded rectangle under each main button (UI Skin: Add Viewer base, End the Stream base): as wide as the button,
        /// <see cref="ButtonBaseOverlap"/> px of it under the button's bottom edge and the rest showing below.
        /// </summary>
        const float ButtonBaseHeight = 40f;
        const float ButtonBaseBottom = 10f;
        const float ButtonBaseOverlap = 14f;
        /// <summary>
        /// The channel column right of the chat: Community Beats on top, the facecam square at the bottom (the usual streamer
        /// layout: the camera sits in the bottom-right corner). 370 = the old 336 px facecam + 10%.
        /// </summary>
        const float ChannelWidth = 370f;
        /// <summary>Space between the chat window's border and the avatars, left and right.</summary>
        const float ChatSidePadding = 44f;
        /// <summary>The Community Beats panel's title band (just the icon, then a grey line).</summary>
        const float BeatsHeaderHeight = 136f;
        /// <summary>The Community Beats title icon (no words).</summary>
        const float BeatsTitleIcon = 120f;
        /// <summary>Community Beats chips (two columns in the channel column).</summary>
        const float BeatChipWidth = 167f;
        const float BeatChipHeight = 70f;
        /// <summary>The community icon on each chip.</summary>
        const float BeatIconSize = 62f;
        const float BeatChipGap = 6f;

        public static void Build()
        {
            // Everything in the website look gets its colours from UI Skin > Site Colours (buttons keep their own).
            using var siteColours = EditorUI.SiteColourScope();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Opening a scene unloads unreferenced assets, so load art and prefabs only after it.
            EditorUI.LoadArt();
            var prefabs = DeckPartyAssets.LoadUIPrefabs();

            SceneCommon.CreateCamera();
            SceneCommon.CreateEventSystem();

            var backgroundCanvas = SceneCommon.CreateCanvas("BackgroundCanvas", -10, false);
            var background = SceneCommon.CreateBackground(backgroundCanvas);
            BuildSitePage(backgroundCanvas);

            var canvas = SceneCommon.CreateCanvas("GameCanvas", 0);
            // The content frame caps the UI at 21:9 and centres it; the page fills the rest of the screen.
            var root = EditorUI.Rect(canvas, "Root").Stretch();
            var frame = root.gameObject.AddComponent<ContentFrame>();

            // The menu bar spans the whole screen (outside the 21:9 content frame), along the top edge.
            var hud = BuildHud(canvas);

            var mainArea = EditorUI.Rect(root, "MainArea").Box(Vector2.zero, Vector2.one, new Vector2(456, Gap), new Vector2(-Gap, -ContentTop));
            var partyRoot = EditorUI.Rect(mainArea, "Party").Stretch();
            var house = BuildStream(partyRoot, prefabs, out var links, out var viewerEmojis, out var controls, out var beats, out var facecam,
                out var streamOverlay, out var anxiety, out var riskMeter, out var riskPips, out var riskTooltip);
            // The Deplatform Risk pips live in the Stream Chat's title band now; the menu bar's view still lights them.
            hud.troubleMeter = riskMeter;
            hud.troublePips = riskPips;
            hud.troubleTooltip = riskTooltip;
            var targeting = BuildTargetingBanner(partyRoot);
            var planning = BuildPlanning(mainArea, prefabs);
            var raid = BuildRaidBlame(mainArea, prefabs);

            // Players and emotes (multiplayer) and the optional Activity log, on the left of the Stream Chat. With none of them
            // showing (single player, log off) ContentFrame gives the stream the whole width.
            var side = EditorUI.Rect(root, "SidePanel").Box(new Vector2(0, 0), new Vector2(0, 1), new Vector2(Gap, Gap), new Vector2(440, -ContentTop));
            EditorUI.VLayout(side, 12, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, true, false);
            var opponents = BuildOpponents(side, prefabs);
            var emotes = BuildEmoteBar(side);
            var feed = BuildFeed(side, prefabs, out var log, out var chat);
            // Keeps the panels at the top.
            var spacer = EditorUI.Rect(side, "Spacer");
            EditorUI.Layout(spacer, -1, 0, -1, 1);
            // Multiplayer: the shop summary at the bottom of the column (fades after 10 seconds).
            var shopRecap = BuildShopRecapPanel(side);
            frame.sidePanel = side;
            frame.sideOnLeft = true;
            frame.mainArea = mainArea;

            var overlay = SceneCommon.CreateCanvas("OverlayCanvas", 100);
            var presence = BuildPresence(overlay);
            var turnTimer = BuildTurnTimer(overlay);
            var effects = BuildEffects(overlay, root);
            emotes.floatArea = effects.particleRoot;
            var banner = BuildBanner(overlay);
            var splash = BuildOutcomeSplash(overlay);
            var picker = BuildPicker(overlay, prefabs);
            var rolodex = BuildRolodex(overlay, prefabs);
            var roundSummary = BuildRoundSummary(overlay);
            var recap = BuildRecap(overlay);
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
            // The 9-sliced chat bubble and its tail are gone: the Stream Chat window replaces them.
            controller.chatBubble = null;
            controller.viewerEmojis = viewerEmojis;
            controller.communityBeats = beats;
            controller.outcomeSplash = splash;
            controller.roundSummary = roundSummary;
            controller.recap = recap;
            controller.shopRecapPanel = shopRecap;
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

            // Every text gets an id and an entry in UI Skin > Text Colours (colours you set there are kept).
            TextColourRegistry.TagScene(scene);
            EditorSceneManager.SaveScene(scene, DeckPartyPaths.GameScene);
        }

        // ------------------------------------------------------------------ the page

        /// <summary>
        /// The website behind the game: a pale page over the (now hidden) stream setup, spanning the whole screen (the UI
        /// itself is capped at 21:9). The menu bar is its own floating panel (see <see cref="BuildHud"/>).
        /// </summary>
        static void BuildSitePage(RectTransform canvas)
        {
            var page = EditorUI.Image(canvas, "SitePage", SitePalette.Page);
            page.rectTransform.Stretch();
        }

        // ------------------------------------------------------------------ the menu bar (HUD)

        /// <summary>
        /// The menu bar: a plain white rectangle along the top edge of the screen, the full width of the screen, with a black
        /// border along its bottom edge (both from UI Skin > Site Colours > Menu bar). The platform logo and the stream count sit
        /// on the left; Clout, Cash and Trophies (one icon and "in chat / needed to win") are centred on the screen; Audience
        /// (with the number of viewers still in the queue) and Menu sit on the right. Every item sits in a rounded frame of the same
        /// height, centred on the bar. Sections are split by short grey lines. The Deplatform Risk pips are in the Stream Chat's
        /// title band (see <see cref="BuildStream"/>).
        /// </summary>
        static HudView BuildHud(RectTransform canvas)
        {
            var border = EditorUI.Image(canvas, "TopBar", SitePalette.Outline, null, null, true);
            // Under the panels (as before, when it was the first thing inside Root).
            border.rectTransform.SetSiblingIndex(0);
            EditorUI.TagArea(border, SiteArea.MenuBar);
            border.rectTransform.TopBand(BarTop, HeaderHeight, 0, 0);
            var fill = EditorUI.Image(border.transform, "Fill", SitePalette.Surface, null);
            // The border shows only along the bottom edge (the other three edges are the screen's).
            fill.rectTransform.Stretch(0, 0, 0, PanelBorder);
            EditorUI.TagSite(border, SiteColourRole.PanelBorder);
            EditorUI.TagSite(fill, SiteColourRole.PanelFill);
            var bar = fill.rectTransform;

            // Left: the animated platform logo (PlatformLogoView builds it from Game Database > Platforms) and the stream count.
            var left = BarGroup(bar, "Left", 0f, BarPadding);
            var logo = EditorUI.Rect(left, "PlatformLogo");
            EditorUI.Layout(logo, 196, -1, 0);
            logo.gameObject.AddComponent<PlatformLogoView>();
            Separator(left, "Sep1");
            var night = Stat(left, "Night", EditorUI.Night, SkinSlot.IconNight, SkinSlot.FrameNight, SitePalette.Text, 190, out var nightIcon, out var nightTip);

            // Centre of the screen: Clout, Cash and Trophies (the Sub Cap count is gone: the chat's seats show it).
            var centre = BarGroup(bar, "Centre", 0.5f, 0f);
            var pop = Stat(centre, "Pop", EditorUI.Sparkle, SkinSlot.IconPop, SkinSlot.FramePop, UIPalette.Gold, 170, out var popIcon, out _);
            Separator(centre, "Sep2");
            var cash = Stat(centre, "Cash", EditorUI.Coin, SkinSlot.IconCash, SkinSlot.FrameCash, UIPalette.Green, 162, out var cashIcon, out _);
            Separator(centre, "Sep3");
            var stars = Stat(centre, "Stars", EditorUI.Star, SkinSlot.IconStar, SkinSlot.FrameStars, UIPalette.Gold, 158, out var starsIcon, out var starsTip);

            // Right: HudView writes their tooltips (Audience [R], Menu [Esc]) in the current language.
            var right = BarGroup(bar, "Right", 1f, -BarPadding);
            // Audience is wide, like the counters: its icon and the number of viewers still in the queue (HudView writes it).
            var rolodex = FramedBarButton(right, "Rolodex", EditorUI.Audience, SkinSlot.IconAudience, SkinSlot.FrameAudience, BarIconSize,
                AudienceButtonWidth, out var rolodexIcon);
            rolodexIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(BarFramePadding, 0), new Vector2(BarIconSize, BarIconSize), new Vector2(0, 0.5f));
            var queue = EditorUI.Text(rolodex.transform, "Count", "0", 38, SitePalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            queue.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(BarFramePadding + BarIconSize + 10, 6), new Vector2(-BarFramePadding, -6));
            var menu = FramedBarButton(right, "Menu", EditorUI.Menu, SkinSlot.IconMenu, SkinSlot.FrameMenu, 42, BarFrameSize, out _);
            // Pinned, so the group's layout doesn't stretch them to its height. Centred on the bar like the counters' frames, so
            // all the frames line up top and bottom (the risky-queue count that sat under Audience is gone).
            var rolodexSlot = KeepSquare(rolodex);
            Separator(right, "Sep4").SetSiblingIndex(rolodexSlot.GetSiblingIndex() + 1);
            KeepSquare(menu);

            var view = bar.gameObject.AddComponent<HudView>();
            view.scenarioLabel = null;
            view.nightLabel = night;
            view.nightTooltip = nightTip;
            view.nightIcon = nightIcon;
            view.capacityIcon = null;
            view.popLabel = pop;
            view.popIcon = popIcon;
            view.cashLabel = cash;
            view.cashIcon = cashIcon;
            view.capacityLabel = null;
            view.troubleLabel = null;
            // The risk pips, their block and tooltip are set by Build (they're in the Stream Chat's title band).
            view.troublePips = null;
            view.troubleMeter = null;
            view.troubleTooltip = null;
            // Trophies: one icon and "in chat / needed to win" instead of a pip per Trophy.
            view.starsLabel = stars;
            view.starsIcon = starsIcon;
            view.starPips = null;
            view.starsMeter = (RectTransform)stars.transform.parent;
            view.starsTooltip = starsTip;
            view.rolodexButton = rolodex;
            view.queueLabel = queue;
            view.menuButton = menu;
            return view;
        }

        /// <summary>
        /// A row of menu-bar sections that sizes itself to what's in it, pinned to the bar at <paramref name="anchorX"/> (0 left,
        /// 0.5 centre, 1 right) and nudged by <paramref name="offsetX"/>, so the middle group is centred on the screen whatever
        /// the side groups hold.
        /// </summary>
        static RectTransform BarGroup(RectTransform bar, string name, float anchorX, float offsetX)
        {
            var group = EditorUI.Rect(bar, name);
            group.anchorMin = new Vector2(anchorX, 0f);
            group.anchorMax = new Vector2(anchorX, 1f);
            group.pivot = new Vector2(anchorX, 0.5f);
            group.sizeDelta = new Vector2(0f, -16f);
            group.anchoredPosition = new Vector2(offsetX, 0f);
            EditorUI.HLayout(group, 22, EditorUI.Pad(0), TextAnchor.MiddleCenter, true, true, false, true);
            EditorUI.Fit(group, true, false);
            return group;
        }

        /// <summary>Wraps a fixed-size button so a stretching layout group keeps it square and centred. Returns the holder.</summary>
        static RectTransform KeepSquare(Button button)
        {
            var rect = (RectTransform)button.transform;
            var holder = EditorUI.Rect(rect.parent, button.name + "Slot");
            holder.SetSiblingIndex(rect.GetSiblingIndex());
            EditorUI.Layout(holder, rect.sizeDelta.x, -1, 0);
            var element = button.GetComponent<LayoutElement>();
            if (element != null) Object.DestroyImmediate(element);
            rect.SetParent(holder, false);
            rect.Place(new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta);
            return holder;
        }

        /// <summary>
        /// A short vertical grey line between two menu-bar sections. It stops short of the bar's top and bottom edges.
        /// </summary>
        static RectTransform Separator(Transform bar, string name)
        {
            var slot = EditorUI.Rect(bar, name);
            EditorUI.Layout(slot, VDivider, -1, 0);
            var line = EditorUI.Line(slot, "Line", SitePalette.Divider);
            line.rectTransform.Stretch(0, 16, 0, 16);
            return slot;
        }

        /// <summary>
        /// The height of the rounded frames in the menu bar: each stat's frame wraps its icon and number, and the Audience and
        /// Menu buttons are this size square.
        /// </summary>
        const float BarFrameSize = 72f;
        const float BarIconSize = 52f;
        /// <summary>Space between a stat's frame and its icon (left) and number (right).</summary>
        const float BarFramePadding = 16f;

        /// <summary>
        /// A menu-bar stat: an icon and a number (no caption) inside one rounded frame; hovering it says what it is. The frame's
        /// art and colour come from its own UI Skin entry (<paramref name="frameSlot"/>). <paramref name="width"/> is the frame's
        /// width (the number shrinks to fit if it ever runs out of room).
        /// </summary>
        static TextMeshProUGUI Stat(Transform parent, string name, Sprite icon, SkinSlot slot, SkinSlot frameSlot, Color color, float width,
            out Image iconImage, out TooltipTrigger tooltip)
        {
            var block = EditorUI.Rect(parent, name);
            EditorUI.Layout(block, width, -1, 0);
            EditorUI.HLayout(block, 10, new RectOffset((int)BarFramePadding, (int)BarFramePadding, 0, 0), TextAnchor.MiddleLeft, true, true, false, false);

            // The frame spans the whole stat, centred on the bar at the buttons' height; drawn first, so it's behind the icon
            // and the number. It stays out of the row's layout.
            var frame = BarFrame(block, frameSlot);
            frame.rectTransform.Box(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -BarFrameSize * 0.5f), new Vector2(0, BarFrameSize * 0.5f));
            EditorUI.Layout(frame, -1, -1, -1, -1).ignoreLayout = true;

            iconImage = EditorUI.Icon(block, "Icon", icon, color, slot, BarIconSize);
            EditorUI.Layout(iconImage, BarIconSize, BarIconSize, 0);

            var value = EditorUI.Text(block, "Value", "0", 38, SitePalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            EditorUI.Layout(value, -1, BarIconSize, 1);
            // HudView fills the tooltip text in the current language.
            tooltip = EditorUI.Tooltip(block);
            var hit = block.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            return value;
        }

        /// <summary>
        /// The rounded rectangle around a menu-bar stat (its icon and number) or button: by default the hollow placeholder outline
        /// in the panel border colour; your own art and colour go in its UI Skin entry. Drawn behind what it frames, so filled
        /// art works too.
        /// </summary>
        static Image BarFrame(Transform parent, SkinSlot slot)
        {
            var frame = EditorUI.SkinShape(parent, "Frame", EditorUI.Frame, SitePalette.Outline, slot, EditorUI.MenuFramePixelsPerUnit);
            frame.rectTransform.sizeDelta = new Vector2(BarFrameSize, BarFrameSize);
            return frame;
        }

        /// <summary>
        /// The Audience and Menu buttons: white, with their border drawn by a frame entry in the UI Skin (like the frames round
        /// the other menu-bar icons) instead of the button's own black edge.
        /// </summary>
        static Button FramedBarButton(Transform parent, string name, Sprite icon, SkinSlot iconSlot, SkinSlot frameSlot, float iconSize,
            float width, out Image iconImage)
        {
            var size = new Vector2(width, BarFrameSize);
            var button = EditorUI.IconButton(parent, name, icon, iconSlot, EditorUI.SiteButton.Light, size, iconSize, out iconImage);
            // The button's own image was its black border: it stays as the (invisible) click area, the white fill inside it
            // stays, and the frame goes between the fill and the icon.
            var edge = button.GetComponent<Image>();
            edge.color = new Color(0, 0, 0, 0);
            var frame = EditorUI.SkinShape(button.transform, "Frame", EditorUI.Frame, SitePalette.Outline, frameSlot, EditorUI.MenuFramePixelsPerUnit);
            frame.rectTransform.Stretch();
            frame.transform.SetSiblingIndex(iconImage.transform.GetSiblingIndex());
            return button;
        }

        /// <summary>A row of pips (the Deplatform Risk pips): lit ones show the count, the tooltip explains.</summary>
        static RectTransform Meter(Transform parent, string name, Sprite icon, SkinSlot slot, int count, float pip, float gap, out Image[] pips)
        {
            var block = EditorUI.Rect(parent, name);
            block.sizeDelta = new Vector2(count * pip + (count - 1) * gap, pip);
            EditorUI.Layout(block, block.sizeDelta.x, -1, 0);
            EditorUI.HLayout(block, gap, EditorUI.Pad(0), TextAnchor.MiddleCenter, false, false, false, false);
            var hit = block.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            pips = new Image[count];
            for (int i = 0; i < count; i++) pips[i] = EditorUI.Icon(block, "Pip" + (i + 1), icon, SitePalette.Off, slot, pip);
            return block;
        }

        // ------------------------------------------------------------------ the stream

        /// <summary>
        /// The stream: the Stream Chat window (left) and the channel column (right: Community Beats, then the facecam).
        /// The chat is a white, slightly rounded window with a black border reaching the bottom of the stream, level with the
        /// facecam: the Deplatform Risk pips centred in its title band over a hairline, the viewers as round avatars, a footer
        /// band for the near-deplatform warning, and the Add Viewer and End the Stream buttons (on their rounded bases) in a
        /// band along its bottom, inside the border.
        /// </summary>
        static HouseView BuildStream(RectTransform partyRoot, UIPrefabs prefabs, out LinkLinesView links,
            out ViewerEmojiView emojiView, out PartyControlsView controlsView, out CommunityBeatsView beatsView, out FacecamView facecam,
            out StreamOverlayView overlay, out StreamerAnxietyView anxiety, out RectTransform riskMeter, out Image[] riskPips,
            out TooltipTrigger riskTooltip)
        {
            // The chat window runs down to the bottom of the stream (level with the facecam); the buttons are inside it.
            var chatRoot = EditorUI.Rect(partyRoot, "StreamChat").Box(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-(ChannelWidth + Gap), 0));
            var inner = EditorUI.SitePanel(chatRoot, "Window", SitePalette.Surface, SitePalette.Outline, PanelBorder, true, SiteArea.StreamChat);
            inner.transform.parent.GetComponent<RectTransform>().Stretch();

            // The title band shows the Deplatform Risk pips (they light up as risk builds; hover for the details). HudView
            // colours them. They replaced the Stream Chat title icon and moved here from the menu bar.
            riskMeter = Meter(inner.transform, "Risk", EditorUI.Cross, SkinSlot.IconTrouble, GameRules.RaidTroubleThreshold, RiskPipSize, RiskPipGap, out riskPips);
            riskMeter.Place(new Vector2(0.5f, 1), new Vector2(0, -ChatHeaderHeight * 0.5f), riskMeter.sizeDelta, new Vector2(0.5f, 0.5f));
            riskTooltip = EditorUI.Tooltip(riskMeter);
            var titleLine = EditorUI.Line(inner.transform, "TitleLine", SitePalette.Divider);
            // Inset from both sides of the window.
            titleLine.rectTransform.TopBand(ChatHeaderHeight, HDivider, DividerInset, DividerInset);

            // More room between the window's border and the avatars on both sides.
            var area = EditorUI.Rect(inner.transform, "HouseArea").Box(Vector2.zero, Vector2.one, new Vector2(ChatSidePadding, ButtonsBand + FooterHeight + 8), new Vector2(-ChatSidePadding, -(ChatHeaderHeight + 14)));

            // Link lines (pairs and chains of viewers) draw under the avatars.
            var linksRect = EditorUI.Rect(area, "LinkLines").Stretch();
            links = linksRect.gameObject.AddComponent<LinkLinesView>();

            // Avatars shrink to fit down to HouseView.MinCardScale, then the chat scrolls.
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
            grid.cellSize = prefabs.ChatAvatar.DesignSize;
            grid.spacing = new Vector2(16, 16);
            grid.padding = EditorUI.Pad(6);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GameRules.HouseColumns;
            EditorUI.Fit(gridRect, false, true);
            scroll.viewport = viewport;
            scroll.content = gridRect;

            // The footer band above the buttons, under the viewers: it holds the near-deplatform warning (the footer picture is
            // gone, and the band is lower so the viewers get more room).
            var footer = EditorUI.Rect(inner.transform, "Footer").BottomBand(ButtonsBand, FooterHeight, 0, 0);
            // Twice the old 740 px width (capped to the chat window by StreamerAnxietyView); its portrait fits its height.
            var anxietyRect = EditorUI.Rect(footer, "AnxiousStreamer").Place(new Vector2(0.5f, 0.5f), Vector2.zero, AnxietySize);
            anxiety = anxietyRect.gameObject.AddComponent<StreamerAnxietyView>();

            controlsView = BuildChatButtons(inner.transform, prefabs);

            var view = partyRoot.gameObject.AddComponent<HouseView>();
            view.area = area;
            view.grid = grid;
            view.scroll = scroll;
            view.cardPrefab = prefabs.ChatAvatar;
            view.hintLabel = null;
            view.avoid = null;

            // The channel column: Community Beats on top, the facecam in the bottom-right corner (like a streamer's camera).
            var column = EditorUI.Rect(partyRoot, "Channel").Box(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-ChannelWidth, 0), Vector2.zero);
            beatsView = BuildCommunityBeats(column);
            facecam = BuildFacecam(column, out overlay);

            // Viewer emojis float above the avatars (and above the window's edge).
            var emojiLayer = EditorUI.Rect(partyRoot, "ViewerEmojis").Stretch();
            emojiView = emojiLayer.gameObject.AddComponent<ViewerEmojiView>();
            emojiView.house = view;
            emojiView.layer = emojiLayer;
            return view;
        }

        /// <summary>
        /// The band along the bottom of the Stream Chat window, inside its border: Add Viewer (always green) and End the Stream
        /// (always red) as wide icon buttons in the middle. The faces of the next viewers (when an effect reveals them) sit just
        /// above it at the left, and the waiting-viewer panel floats above it in the middle. Every word is in a hover tooltip.
        /// (The risky-queue count that sat under the menu bar's Audience button was removed.)
        /// </summary>
        static PartyControlsView BuildChatButtons(Transform window, UIPrefabs prefabs)
        {
            var row = EditorUI.Rect(window, "ChatButtons").BottomBand(0, ButtonsBand, 0, 0);
            var input = row.gameObject.AddComponent<Image>();
            input.color = new Color(0, 0, 0, 0);
            input.raycastTarget = false;

            // The two main buttons, wide and fanned out from the centre (a 40 px gap between them). Add Viewer is always green and
            // End the Stream always red, whatever the platform.
            const float buttonGap = 40f;
            const float buttonX = MainButtonWidth * 0.5f + buttonGap * 0.5f;
            var size = new Vector2(MainButtonWidth, MainButtonHeight);
            // Each button sits on a rounded base as wide as it (UI Skin: Add Viewer base / End the Stream base), overlapping
            // the base's top edge. The bases come first so the buttons draw over them.
            const float buttonBottom = ButtonBaseBottom + ButtonBaseHeight - ButtonBaseOverlap;
            var doorBase = ButtonBase(row, "DoorBase", SkinSlot.AddViewerBase, Shade(SitePalette.Go), -buttonX);
            var endBase = ButtonBase(row, "EndPartyBase", SkinSlot.EndStreamBase, Shade(SitePalette.Stop), buttonX);
            var door = EditorUI.IconButton(row, "Door", EditorUI.AddViewer, SkinSlot.IconAddViewer, EditorUI.SiteButton.Go, size, 52, out _);
            ((RectTransform)door.transform).Place(new Vector2(0.5f, 0), new Vector2(-buttonX, buttonBottom), size, new Vector2(0.5f, 0));
            var end = EditorUI.IconButton(row, "EndParty", EditorUI.EndStream, SkinSlot.IconEndStream, EditorUI.SiteButton.Stop, size, 44, out _);
            ((RectTransform)end.transform).Place(new Vector2(0.5f, 0), new Vector2(buttonX, buttonBottom), size, new Vector2(0.5f, 0));

            // Who's next, when an effect reveals it (X-Ray, Side Eye Chloe): their faces (names on hover), just above the buttons
            // at the left (the wide buttons leave no room beside them).
            const int faces = 3;
            const float faceSize = 48f;
            var queue = EditorUI.Rect(window, "NextUp").Place(new Vector2(0, 0), new Vector2(ChatSidePadding, ButtonsBand + 8),
                new Vector2(faces * faceSize + (faces - 1) * 6, faceSize), new Vector2(0, 0));
            EditorUI.HLayout(queue, 6, EditorUI.Pad(0), TextAnchor.MiddleLeft, false, false, false, false);
            var backs = new Image[faces];
            var arts = new Image[faces];
            var tips = new TooltipTrigger[faces];
            for (int i = 0; i < faces; i++)
            {
                backs[i] = EditorUI.Image(queue, "Face" + (i + 1), SitePalette.Border, EditorUI.Circle, null, true);
                backs[i].rectTransform.sizeDelta = new Vector2(faceSize, faceSize);
                arts[i] = EditorUI.Image(backs[i].transform, "Art", Color.white);
                arts[i].rectTransform.Stretch(4, 4, 4, 4);
                arts[i].preserveAspect = true;
                tips[i] = EditorUI.Tooltip(backs[i]);
            }
            queue.gameObject.SetActive(false);

            // Waiting-viewer panel: a white card over the bottom of the chat (just above the buttons) with the viewer's avatar,
            // Approve and Deny.
            var peekFill = EditorUI.SitePanel(window, "Peek", SitePalette.Surface, SitePalette.Outline, PanelBorder, true, SiteArea.WaitingViewer);
            var peek = (RectTransform)peekFill.transform.parent;
            peek.Place(new Vector2(0.5f, 0), new Vector2(0, ButtonsBand + 14), new Vector2(560, 250), new Vector2(0.5f, 0));
            var cardGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.ChatAvatar.gameObject, peekFill.transform);
            var peekCard = cardGo.GetComponent<GuestCardView>();
            ((RectTransform)cardGo.transform).Place(new Vector2(0, 0.5f), new Vector2(26, 0), prefabs.ChatAvatar.DesignSize, new Vector2(0, 0.5f));
            // PartyControlsView writes their tooltips (Approve / Deny) in the current language.
            var admit = EditorUI.IconButton(peekFill.transform, "Admit", EditorUI.Check, SkinSlot.IconCheck, EditorUI.SiteButton.Accent, new Vector2(132, 92), 48, out _);
            ((RectTransform)admit.transform).Place(new Vector2(1, 0.5f), new Vector2(-174, 0), new Vector2(132, 92), new Vector2(1, 0.5f));
            var reject = EditorUI.IconButton(peekFill.transform, "Reject", EditorUI.Close, SkinSlot.IconClose, EditorUI.SiteButton.Light, new Vector2(132, 92), 44, out _);
            ((RectTransform)reject.transform).Place(new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(132, 92), new Vector2(1, 0.5f));
            peek.gameObject.SetActive(false);

            var view = input.gameObject.AddComponent<PartyControlsView>();
            view.doorButton = door;
            view.doorLabel = null;
            view.doorSubLabel = null;
            view.doorTooltip = door.GetComponent<TooltipTrigger>();
            view.endButton = end;
            view.endButtonImage = end.GetComponent<Image>();
            view.endLabel = null;
            // No projected-take pill any more: ending's take is in the End the Stream tooltip.
            view.endSubLabel = null;
            // The risky-queue count under the Audience button was removed (2026-11-10).
            view.riskLabel = null;
            view.riskBadge = null;
            view.doorBase = doorBase;
            view.endBase = endBase;
            view.queueRoot = queue.gameObject;
            view.queueBacks = backs;
            view.queueFaces = arts;
            view.queueTips = tips;
            view.peekPanel = peek.gameObject;
            view.peekTitle = null;
            view.peekCard = peekCard;
            view.admitButton = admit;
            view.rejectButton = reject;
            return view;
        }

        /// <summary>
        /// The rounded rectangle under a main button, as wide as the button and centred under it at <paramref name="x"/>. Its art
        /// and colour come from its UI Skin entry (built-in: the small rounded rectangle in a darker shade of the button). It fades
        /// with its button while the button can't be pressed (PartyControlsView).
        /// </summary>
        static CanvasGroup ButtonBase(Transform row, string name, SkinSlot slot, Color color, float x)
        {
            var image = EditorUI.SkinShape(row, name, EditorUI.RoundedSmall, color, slot);
            image.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(x, ButtonBaseBottom), new Vector2(MainButtonWidth, ButtonBaseHeight),
                new Vector2(0.5f, 0));
            var group = image.gameObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        /// <summary>A darker shade of a button colour (the built-in colour of the bases under the main buttons).</summary>
        static Color Shade(Color color) => Color.Lerp(color, Color.black, 0.35f);

        /// <summary>
        /// Community Beats: a white panel under the facecam with a chip per community (icon, live score, current bonus; hover
        /// for what it does and how it scales), two to a row, plus a red chip while rival Doomsdayers curse the stream.
        /// </summary>
        static CommunityBeatsView BuildCommunityBeats(Transform column)
        {
            // Above the facecam (which takes the bottom ChannelWidth px of the column).
            var fill = EditorUI.SitePanel(column, "CommunityPanel", SitePalette.Surface, SitePalette.Outline, PanelBorder, true, SiteArea.CommunityBeats);
            ((RectTransform)fill.transform.parent).Box(Vector2.zero, Vector2.one, new Vector2(0, ChannelWidth + Gap), Vector2.zero);

            // Title band: the community icon and "Community Beats", then a light grey line across the panel.
            // The title is just the icon, centred (its own UI Skin slot, IconCommunityBeats, separate from the Audience
            // button's icon); hovering it says "Community Beats" in the current language.
            var header = EditorUI.Rect(fill.transform, "Header").TopBand(0, BeatsHeaderHeight, 16, 16);
            var headerIcon = EditorUI.Icon(header, "Icon", EditorUI.Audience, SitePalette.Text, SkinSlot.IconCommunityBeats, BeatsTitleIcon);
            headerIcon.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BeatsTitleIcon, BeatsTitleIcon));
            headerIcon.raycastTarget = true;
            EditorUI.Tooltip(headerIcon).textKey = "beats.title";
            var headerLine = EditorUI.Line(fill.transform, "HeaderLine", SitePalette.Divider);
            headerLine.rectTransform.TopBand(BeatsHeaderHeight, HDivider, DividerInset, DividerInset);

            var row = EditorUI.Rect(fill.transform, "CommunityBeats").Stretch(10, BeatsHeaderHeight + 12, 10, 12);
            var layout = row.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(BeatChipWidth, BeatChipHeight);
            layout.spacing = new Vector2(BeatChipGap, BeatChipGap);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;

            // Rival Doomsdayers warning (hidden unless this stream is cursed).
            var cursed = EditorUI.Image(row, "Cursed", SitePalette.Danger, EditorUI.RoundedSmall, null, true);
            cursed.type = Image.Type.Sliced;
            var cursedLabel = EditorUI.Text(cursed.transform, "Label", "-", 24, Color.white, TextAlignmentOptions.Center, true, false, false);
            cursedLabel.rectTransform.Stretch(6, 4, 6, 4);
            var cursedTip = EditorUI.Tooltip(cursed);
            cursed.gameObject.SetActive(false);

            var chips = new CommunityBeatsView.Chip[Communities.All.Length];
            for (int i = 0; i < Communities.All.Length; i++)
            {
                var community = Communities.All[i];
                // No background or border per entry (CommunityBeatsView keeps it clear): the clear image only takes the hover.
                var background = EditorUI.Image(row, "Beat_" + community, new Color(0, 0, 0, 0), EditorUI.RoundedSmall, null, true);
                background.type = Image.Type.Sliced;
                var group = background.gameObject.AddComponent<CanvasGroup>();

                // At the maximum score: a sunburst and a gold halo behind the icon (CommunityMaxFx), centred on it.
                var maxRoot = EditorUI.Rect(background.transform, "MaxFx").Place(new Vector2(0, 0.5f), new Vector2(7 + BeatIconSize * 0.5f, 0), Vector2.zero);
                var rays = EditorUI.Image(maxRoot, "Rays", Color.white, null);
                rays.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BeatIconSize * 2.3f, BeatIconSize * 2.3f));
                rays.enabled = false;
                var halo = EditorUI.Image(maxRoot, "Halo", Color.white, EditorUI.Glow);
                halo.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BeatIconSize * 1.85f, BeatIconSize * 1.85f));
                halo.enabled = false;

                // Icon on the left, the score on top, the current bonus underneath.
                var icon = EditorUI.Image(background.transform, "Icon", Color.white, EditorUI.Circle);
                icon.preserveAspect = true;
                icon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(7, 0), new Vector2(BeatIconSize, BeatIconSize), new Vector2(0, 0.5f));

                var maxFx = background.gameObject.AddComponent<CommunityMaxFx>();
                maxFx.icon = icon;
                maxFx.halo = halo;
                maxFx.rays = rays;
                maxFx.punchTarget = background.rectTransform;

                var points = EditorUI.Text(background.transform, "Points", "0", 24, SitePalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
                points.rectTransform.Box(new Vector2(0, 0.45f), Vector2.one, new Vector2(BeatIconSize + 14, 0), new Vector2(-6, -4));
                var effect = EditorUI.Text(background.transform, "Effect", "-", 16, SitePalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
                effect.rectTransform.Box(Vector2.zero, new Vector2(1, 0.45f), new Vector2(BeatIconSize + 14, 4), new Vector2(-6, 0));
                effect.fontSizeMin = 10;

                chips[i] = new CommunityBeatsView.Chip
                {
                    community = community,
                    root = background.rectTransform,
                    group = group,
                    background = background,
                    icon = icon,
                    pointsLabel = points,
                    effectLabel = effect,
                    tooltip = EditorUI.Tooltip(background),
                    maxFx = maxFx,
                };
            }

            var view = row.gameObject.AddComponent<CommunityBeatsView>();
            view.chips = chips;
            view.cursed = cursed.gameObject;
            view.cursedLabel = cursedLabel;
            view.cursedTooltip = cursedTip;
            view.avoid = null;
            view.siteStyle = true;
            return view;
        }

        // ------------------------------------------------------------------ polish overlays

        /// <summary>The LIVE badge and the stream timer, top-left inside the facecam.</summary>
        static StreamOverlayView BuildStreamOverlay(Transform parent)
        {
            var rect = EditorUI.Rect(parent, "StreamOverlay").Place(new Vector2(0, 1), new Vector2(12, -12), new Vector2(210, 34), new Vector2(0, 1));
            return rect.gameObject.AddComponent<StreamOverlayView>();
        }

        static PresenceView BuildPresence(RectTransform overlay)
        {
            var rect = EditorUI.Rect(overlay, "Presence").Stretch();
            return rect.gameObject.AddComponent<PresenceView>();
        }

        static TurnTimerView BuildTurnTimer(RectTransform overlay)
        {
            // Top-right of the Stream Chat window, in its title band (left of the channel column).
            var rect = EditorUI.Rect(overlay, "TurnTimer").Place(new Vector2(1, 1), new Vector2(-(ChannelWidth + Gap * 3), -(ContentTop + 4)), new Vector2(150, 54), new Vector2(1, 1));
            return rect.gameObject.AddComponent<TurnTimerView>();
        }

        /// <summary>The "pick a viewer" prompt: a white notice with a black border over the top of the chat, with Confirm and Cancel icon buttons.</summary>
        static TargetingBannerView BuildTargetingBanner(RectTransform partyRoot)
        {
            var host = EditorUI.Rect(partyRoot, "Targeting").TopBand(ChatHeaderHeight + 10, 76, 40, ChannelWidth + Gap + 40);
            // Root = the black border (ViewBarView copies its sprite); the white fill sits inside it.
            var panel = EditorUI.Image(host, "Root", SitePalette.Outline, EditorUI.RoundedSmall, null, true);
            panel.type = Image.Type.Sliced;
            panel.rectTransform.Stretch();
            EditorUI.TagArea(panel, SiteArea.PopUps);
            var panelFill = EditorUI.Image(panel.transform, "Fill", SitePalette.Surface, EditorUI.RoundedSmall);
            panelFill.type = Image.Type.Sliced;
            panelFill.rectTransform.Stretch(EditorUI.ModalBorder, EditorUI.ModalBorder, EditorUI.ModalBorder, EditorUI.ModalBorder);
            EditorUI.TagSite(panel, SiteColourRole.PanelBorder);
            EditorUI.TagSite(panelFill, SiteColourRole.PanelFill);
            var label = EditorUI.Readable(EditorUI.Text(panel.transform, "Label", "-", 24, SitePalette.Text, TextAlignmentOptions.MidlineLeft));
            label.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(24, 4), new Vector2(-(2 * 72 + 12 + 24), -4));
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = 24;
            var cancel = EditorUI.IconButton(panel.transform, "Cancel", EditorUI.Close, SkinSlot.IconClose, EditorUI.SiteButton.Light, new Vector2(72, 58), 32, out _);
            ((RectTransform)cancel.transform).Place(new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(72, 58), new Vector2(1, 0.5f));
            var confirm = EditorUI.IconButton(panel.transform, "Confirm", EditorUI.Check, SkinSlot.IconCheck, EditorUI.SiteButton.Accent, new Vector2(72, 58), 34, out _);
            ((RectTransform)confirm.transform).Place(new Vector2(1, 0.5f), new Vector2(-94, 0), new Vector2(72, 58), new Vector2(1, 0.5f));
            confirm.gameObject.SetActive(false);

            var view = host.gameObject.AddComponent<TargetingBannerView>();
            view.root = panel.gameObject;
            view.label = label;
            view.cancelButton = cancel;
            view.confirmButton = confirm;
            return view;
        }

        // ------------------------------------------------------------------ the shop

        /// <summary>
        /// The shop in the website look: one white card holding everything you can do between streams. Along the top, under
        /// the title, a row with the player's streamer (their active as an icon button with its price), Raise Sub Cap (+1 and
        /// the price), Remove a Viewer (-1 and the price), the Audience (and how many), what you are waiting for and the big
        /// Go Live / Ready button; under it, the grid of viewers to recruit. Words are in the hover tooltips.
        /// </summary>
        static PlanningView BuildPlanning(RectTransform mainArea, UIPrefabs prefabs)
        {
            const float toolbarTop = 84f;
            const float toolbarHeight = 124f;
            const float buttonHeight = 96f;
            var root = EditorUI.Rect(mainArea, "Planning").Stretch();

            var shop = EditorUI.SitePanel(root, "Shop", SitePalette.Surface, SitePalette.Outline, PanelBorder, true, SiteArea.Shop);
            ((RectTransform)shop.transform.parent).Stretch();
            // PlanningView writes the title (it names the player when you are watching someone else's shop).
            var shopTitle = EditorUI.Text(shop.transform, "Header", "-", 36, SitePalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            shopTitle.rectTransform.TopBand(16, 52, 28, 28);

            // Everything that costs something (and the Ready button) in one row under the title.
            var toolbar = EditorUI.Rect(shop.transform, "Toolbar").TopBand(toolbarTop, toolbarHeight, 24, 24);
            EditorUI.HLayout(toolbar, 18, EditorUI.Pad(0), TextAnchor.MiddleLeft, true, false, false, false);

            var power = BuildStreamerPower(toolbar);
            ToolbarSeparator(toolbar, buttonHeight);
            var expand = WideIconButton(toolbar, "Expand", EditorUI.Door, SkinSlot.IconDoor, 210, buttonHeight, out var expandLabel);
            var expandTip = expand.GetComponent<TooltipTrigger>();
            var remove = WideIconButton(toolbar, "RemoveViewer", EditorUI.RemoveViewer, SkinSlot.IconRemoveViewer, 210, buttonHeight, out var removeLabel);
            var removeTip = remove.GetComponent<TooltipTrigger>();
            var rolodex = WideIconButton(toolbar, "Rolodex", EditorUI.Audience, SkinSlot.IconAudience, 170, buttonHeight, out var rolodexLabel);
            var rolodexTip = rolodex.GetComponent<TooltipTrigger>();

            var spacer = EditorUI.Rect(toolbar, "Spacer");
            EditorUI.Layout(spacer, -1, 10, 1);

            var waiting = EditorUI.Text(toolbar, "Waiting", string.Empty, 20, SitePalette.Text, TextAlignmentOptions.MidlineRight);
            EditorUI.Layout(waiting, 300, buttonHeight, 0);
            waiting.rectTransform.sizeDelta = new Vector2(300, buttonHeight);
            var next = EditorUI.IconButton(toolbar, "NextNight", EditorUI.GoLive, SkinSlot.IconGoLive, EditorUI.SiteButton.Accent, new Vector2(240, buttonHeight + 8), 62, out var goLiveIcon);
            EditorUI.Layout(next, 240, buttonHeight + 8, 0);
            var readyIcon = EditorUI.Icon(next.transform, "ReadyIcon", EditorUI.Check, SitePalette.Surface, SkinSlot.IconCheck, 58);
            readyIcon.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58, 58));
            readyIcon.gameObject.AddComponent<SiteAccent>().role = SiteAccent.Role.Content;
            readyIcon.gameObject.SetActive(false);

            var titleLine = EditorUI.Line(shop.transform, "TitleLine", SitePalette.Divider);
            titleLine.rectTransform.TopBand(toolbarTop + toolbarHeight + 10, HDivider, DividerInset, DividerInset);
            var scroll = EditorUI.Scroll(shop.transform, "Grid", out var shopContent, 16, EditorUI.Pad(8), new Vector2(220, 260));
            scroll.GetComponent<RectTransform>().Box(Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -(toolbarTop + toolbarHeight + 22)));
            var grid = shopContent.GetComponent<GridLayoutGroup>();
            grid.childAlignment = TextAnchor.UpperLeft;
            // Each tile has a pile of card backs peeking out straight above it (up to 21 px). They no longer stick out to the
            // left, so the full 38 px between tiles shows as space and the grid needs no extra room on its left.
            grid.spacing = new Vector2(38, 36);
            grid.padding = new RectOffset(20, 20, 30, 12);
            LightScrollbar(scroll);
            var gate = BuildShopGate(shop.transform);

            var view = root.gameObject.AddComponent<PlanningView>();
            view.headerLabel = shopTitle;
            view.expandButton = expand;
            view.expandLabel = expandLabel;
            view.expandTooltip = expandTip;
            view.removeButton = remove;
            view.removeLabel = removeLabel;
            view.removeTooltip = removeTip;
            view.rolodexButton = rolodex;
            view.rolodexLabel = rolodexLabel;
            view.rolodexTooltip = rolodexTip;
            view.nextNightButton = next;
            view.nextNightLabel = null;
            view.nextNightTooltip = next.GetComponent<TooltipTrigger>();
            view.nextNightIcon = goLiveIcon.gameObject;
            view.nextReadyIcon = readyIcon.gameObject;
            view.waitingLabel = waiting;
            view.shopViewport = scroll.viewport != null ? scroll.viewport : (RectTransform)shopContent.parent;
            view.shopGrid = grid;
            view.shopItemPrefab = prefabs.ShopItem;
            view.power = power;
            view.gate = gate;
            // Narrower, playing-card shaped tiles (about 5:7).
            view.minTileWidth = 160f;
            view.maxTileWidth = 196f;
            view.tileAspect = 1.4f;
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>A short vertical grey line between two groups of the shop's toolbar.</summary>
        static void ToolbarSeparator(Transform toolbar, float height)
        {
            var line = EditorUI.Line(toolbar, "Separator", SitePalette.Divider);
            line.rectTransform.sizeDelta = new Vector2(VDivider, height * 0.7f);
            EditorUI.Layout(line, VDivider, height * 0.7f, 0);
        }

        /// <summary>A light button with an icon on the left and a short number label (the words in its tooltip).</summary>
        static Button WideIconButton(Transform parent, string name, Sprite icon, SkinSlot slot, float width, float height, out TextMeshProUGUI label)
        {
            // A button: its label keeps the button's own colours whatever the shop's Site Colours are.
            using var plain = EditorUI.PlainColourScope();
            var button = EditorUI.IconButton(parent, name, icon, slot, EditorUI.SiteButton.Light, new Vector2(width, height), 46, out var iconImage);
            EditorUI.Layout(button, width, height, 0);
            iconImage.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(46, 46), new Vector2(0, 0.5f));
            label = EditorUI.Text(button.transform, "Label", "-", 28, SitePalette.Text, TextAlignmentOptions.MidlineLeft, true, false, false);
            label.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(78, 4), new Vector2(-12, -4));
            return button;
        }

        /// <summary>The shop grid's scrollbar in light greys.</summary>
        static void LightScrollbar(ScrollRect scroll)
        {
            if (scroll.verticalScrollbar == null) return;
            if (scroll.verticalScrollbar.TryGetComponent<Image>(out var track))
            {
                track.color = SitePalette.SurfaceAlt;
                EditorUI.TagSite(track, SiteColourRole.SoftFill);
            }
            if (scroll.verticalScrollbar.handleRect != null && scroll.verticalScrollbar.handleRect.TryGetComponent<Image>(out var handle)) handle.color = SitePalette.Border;
        }

        /// <summary>
        /// Free For All: the panel over the shop until it opens for everyone. After your stream: "Waiting for other streamers"
        /// and who is still streaming; once every stream is over, every player's stream summary (a column each, like the
        /// summary pop-up) and Ready for the Shop; once everyone is ready, a big 3-2-1. ShopGateView writes every text.
        /// </summary>
        static ShopGateView BuildShopGate(Transform shop)
        {
            var host = EditorUI.Rect(shop, "ShopGate").Stretch();
            // A white card with a black border over the shop (black text; the 3-2-1 stays gold with a black outline).
            var panelFill = EditorUI.SitePanel(host, "Root", SitePalette.Surface, SitePalette.Outline, EditorUI.ModalBorder, true, SiteArea.PopUps);
            var panel = (Image)panelFill.transform.parent.GetComponent<Image>();
            panel.rectTransform.Stretch(10, 10, 10, 10);

            var title = EditorUI.Readable(EditorUI.Text(panel.transform, "Title", "-", 46, SitePalette.Text, TextAlignmentOptions.Center, true, true, false));
            title.rectTransform.TopBand(34, 64, 40, 40);
            var body = EditorUI.Readable(EditorUI.Text(panel.transform, "Body", "-", 24, SitePalette.Text, TextAlignmentOptions.Top));
            body.rectTransform.TopBand(104, 70, 90, 90);

            // Every player's stream summary, once all streams are over (one column each, grey lines between them).
            var columns = EditorUI.Rect(panel.transform, "StreamSummary").Box(Vector2.zero, Vector2.one, new Vector2(40, 226), new Vector2(-40, -186));
            EditorUI.HLayout(columns, 0, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, false, true);
            var columnTemplate = RecapColumnTemplate(columns, 248f, 96f, out var divider);
            columns.gameObject.SetActive(false);

            var countdown = EditorUI.Text(panel.transform, "Countdown", "3", 220, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            countdown.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(420, 280));
            countdown.outlineWidth = 0.25f;
            countdown.outlineColor = new Color32(0, 0, 0, 255);
            countdown.gameObject.SetActive(false);

            var ready = EditorUI.SiteTextButton(panel.transform, "Ready", "-", EditorUI.SiteButton.Accent, 34, out var readyLabel);
            ready.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 104), new Vector2(560, 104), new Vector2(0.5f, 0));

            var notice = EditorUI.Readable(EditorUI.Text(panel.transform, "Notice", "-", 22, SitePalette.Warning, TextAlignmentOptions.Center));
            notice.rectTransform.BottomBand(24, 70, 90, 90);

            var view = host.gameObject.AddComponent<ShopGateView>();
            view.root = panel.gameObject;
            view.titleLabel = title;
            view.bodyLabel = body;
            view.countdownLabel = countdown;
            view.readyButton = ready;
            view.readyLabel = readyLabel;
            view.noticeLabel = notice;
            view.columnsRoot = columns;
            view.columnTemplate = columnTemplate;
            view.dividerTemplate = divider;
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// One summary column (inactive template, cloned once per player): the streamer's round avatar, the player's name and
        /// streamer, the headline and the summary lines. Also makes the grey line template that goes between two columns.
        /// </summary>
        static RecapColumnView RecapColumnTemplate(RectTransform columns, float columnWidth, float avatarSize, out GameObject divider)
        {
            var column = EditorUI.Rect(columns, "ColumnTemplate");
            EditorUI.Layout(column, columnWidth, -1, 0, 1);
            EditorUI.VLayout(column, 4, new RectOffset(14, 14, 8, 8), TextAnchor.UpperCenter, true, true, true, false);

            var avatarRow = EditorUI.Rect(column, "Avatar");
            EditorUI.Layout(avatarRow, -1, avatarSize + 8);
            var avatar = EditorUI.Rect(avatarRow, "Frame").Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(avatarSize, avatarSize));
            var mask = EditorUI.Image(avatar, "Mask", SitePalette.SurfaceAlt, EditorUI.Circle);
            mask.rectTransform.Stretch(4, 4, 4, 4);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var placeholder = EditorUI.Image(mask.transform, "Placeholder", UIPalette.Purple, EditorUI.Circle);
            placeholder.rectTransform.Stretch();
            var initials = EditorUI.Text(placeholder.transform, "Initials", "?", 40, Color.white, TextAlignmentOptions.Center, true, true, false);
            initials.rectTransform.Stretch();
            var portrait = EditorUI.Image(mask.transform, "Art", Color.white);
            portrait.rectTransform.Stretch(2, 2, 2, 2);
            portrait.preserveAspect = true;
            portrait.enabled = false;
            var ring = EditorUI.Image(avatar, "Ring", SitePalette.Outline, EditorUI.Ring, SkinSlot.AvatarRing);
            ring.rectTransform.Stretch();

            var name = EditorUI.Readable(EditorUI.Text(column, "Name", "-", 24, SitePalette.Text, TextAlignmentOptions.Center, true, true, false));
            EditorUI.Layout(name, -1, 32);
            var streamer = EditorUI.Text(column, "Streamer", "-", 18, SitePalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            EditorUI.Layout(streamer, -1, 24);
            var headline = EditorUI.Text(column, "Headline", "-", 22, SitePalette.Text, TextAlignmentOptions.Center, true, true, false);
            EditorUI.Layout(headline, -1, 34);
            var bodyLine = EditorUI.Line(column, "Line", SitePalette.Divider);
            EditorUI.Layout(bodyLine, -1, HDivider);
            var body = EditorUI.Text(column, "Body", "-", 21, SitePalette.Text, TextAlignmentOptions.Top);
            EditorUI.Layout(body, -1, 160, -1, 1);
            body.fontSizeMin = 13;
            body.lineSpacing = 8f;

            var columnView = column.gameObject.AddComponent<RecapColumnView>();
            columnView.portrait = portrait;
            columnView.placeholder = placeholder;
            columnView.initialsLabel = initials;
            columnView.ring = ring;
            columnView.nameLabel = name;
            columnView.streamerLabel = streamer;
            columnView.headlineLabel = headline;
            columnView.bodyLabel = body;
            column.gameObject.SetActive(false);

            // The light grey line between two players' columns.
            var line = EditorUI.Line(columns, "DividerTemplate", SitePalette.Divider);
            EditorUI.Layout(line, VDivider, -1, 0, 1);
            line.gameObject.SetActive(false);
            divider = line.gameObject;
            return columnView;
        }

        /// <summary>
        /// The streamer in the shop's toolbar: their portrait in a ring (hover for name, power, what it does and its status),
        /// the active as an icon button with its price beside it, a small badge with the cooldown and a tick while it's armed.
        /// </summary>
        static StreamerPowerView BuildStreamerPower(Transform parent)
        {
            const float portraitSize = 108f;
            var panel = EditorUI.Rect(parent, "StreamerPower");
            panel.sizeDelta = new Vector2(330, 116);
            EditorUI.Layout(panel, 330, 116, 0);
            var hit = panel.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var info = EditorUI.Tooltip(panel);

            var content = EditorUI.Rect(panel, "Content").Stretch();
            var frame = EditorUI.Rect(content, "Portrait").Place(new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(portraitSize, portraitSize), new Vector2(0, 0.5f));
            var maskImage = EditorUI.Image(frame, "Mask", SitePalette.SurfaceAlt, EditorUI.Circle);
            maskImage.rectTransform.Stretch(5, 5, 5, 5);
            maskImage.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var placeholder = EditorUI.Image(maskImage.transform, "Placeholder", UIPalette.Purple, EditorUI.Circle);
            placeholder.rectTransform.Stretch();
            var initials = EditorUI.Text(placeholder.transform, "Initials", "?", 40, Color.white, TextAlignmentOptions.Center, true, true, false);
            initials.rectTransform.Stretch();
            var portrait = EditorUI.Image(maskImage.transform, "Art", Color.white);
            portrait.rectTransform.Stretch(3, 3, 3, 3);
            portrait.preserveAspect = true;
            portrait.enabled = false;
            var ring = EditorUI.Image(frame, "Ring", SitePalette.Outline, EditorUI.Ring, SkinSlot.AvatarRing);
            ring.rectTransform.Stretch();

            var cooldownLabel = EditorUI.Pill(frame, "Cooldown", 20, out var cooldown);
            cooldown.rectTransform.Place(new Vector2(1, 1), new Vector2(8, 4), new Vector2(46, 34), new Vector2(1, 1));
            cooldown.gameObject.SetActive(false);
            var armed = EditorUI.Image(frame, "Armed", UIPalette.Green, EditorUI.Circle);
            armed.rectTransform.Place(new Vector2(1, 0), new Vector2(4, 2), new Vector2(40, 40), new Vector2(1, 0));
            var armedTick = EditorUI.Icon(armed.transform, "Tick", EditorUI.Check, Color.white, SkinSlot.IconCheck, 24);
            armedTick.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24));
            armed.gameObject.SetActive(false);

            var use = EditorUI.IconButton(content, "Use", EditorUI.Action, SkinSlot.IconAction, EditorUI.SiteButton.Accent, new Vector2(196, 76), 34, out var useIcon);
            ((RectTransform)use.transform).Place(new Vector2(1, 0.5f), new Vector2(-2, 0), new Vector2(196, 76), new Vector2(1, 0.5f));
            useIcon.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(34, 34), new Vector2(0, 0.5f));
            var useLabel = EditorUI.Text(use.transform, "Label", "-", 26, Color.white, TextAlignmentOptions.Center, true, false, false);
            useLabel.rectTransform.Box(Vector2.zero, Vector2.one, new Vector2(58, 4), new Vector2(-10, -4));
            useLabel.gameObject.AddComponent<SiteAccent>().role = SiteAccent.Role.Content;
            // Over the streamer's own tooltip: it comes back when the pointer leaves the button.
            use.GetComponent<TooltipTrigger>().overlay = true;

            var empty = EditorUI.Text(panel, "Empty", "power.none", 20, SitePalette.TextFaint, TextAlignmentOptions.Center, false, false, false);
            empty.rectTransform.Stretch(portraitSize + 12, 10, 6, 10);

            var view = panel.gameObject.AddComponent<StreamerPowerView>();
            view.portrait = portrait;
            view.placeholder = placeholder;
            view.initialsLabel = initials;
            view.nameLabel = null;
            view.kindLabel = null;
            view.powerLabel = null;
            view.descriptionLabel = null;
            view.statusLabel = null;
            view.useButton = use;
            view.useLabel = useLabel;
            view.useTooltip = use.GetComponent<TooltipTrigger>();
            view.emptyState = empty.gameObject;
            view.content = content.gameObject;
            view.infoTooltip = info;
            view.cooldownBadge = cooldown.gameObject;
            view.cooldownLabel = cooldownLabel;
            view.armedBadge = armed.gameObject;
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

        /// <summary>
        /// A white card with a strong black border whose own Image is the border (so layout elements and views can sit on it
        /// directly): the white fill is its first child, and everything added afterwards draws on top.
        /// </summary>
        static Image BorderedCard(Transform parent, string name)
        {
            var border = EditorUI.Image(parent, name, SitePalette.Outline, EditorUI.RoundedSmall, null, true);
            border.type = Image.Type.Sliced;
            // The side panel's cards (Players, emotes, shop recap) share the Side panel colours in UI Skin > Site Colours.
            EditorUI.TagArea(border, SiteArea.SidePanel);
            var fill = EditorUI.Image(border.transform, "Fill", SitePalette.Surface, EditorUI.RoundedSmall);
            fill.type = Image.Type.Sliced;
            fill.rectTransform.Stretch(PanelBorder, PanelBorder, PanelBorder, PanelBorder);
            EditorUI.RoundPanel(border, fill, PanelBorder);
            EditorUI.TagSite(border, SiteColourRole.PanelBorder);
            EditorUI.TagSite(fill, SiteColourRole.PanelFill);
            return border;
        }

        static OpponentsView BuildOpponents(RectTransform side, UIPrefabs prefabs)
        {
            // A white card like the rest of the site (the player rows inside keep their dark cards).
            // It grows with the number of players (no scrolling): the card's height is its rows' height.
            var panel = BorderedCard(side, "Players");
            panel.transform.GetChild(0).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            EditorUI.VLayout(panel, 8, new RectOffset(14, 14, 12, 14), TextAnchor.UpperCenter, true, true, true, false);
            var title = EditorUI.Text(panel.transform, "Header", "-", 26, SitePalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            EditorUI.Layout(title, -1, 36);
            var list = EditorUI.Rect(panel.transform, "List");
            EditorUI.VLayout(list, 8, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, true, false);

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
            var panel = BorderedCard(side, GameController.EmoteBarName);
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

        /// <summary>
        /// Multiplayer: the shop summary at the bottom of the left column (under the players): a white card with a title and
        /// one block per player. It shows after each shop and fades away after 10 seconds (ShopRecapPanelView).
        /// </summary>
        static ShopRecapPanelView BuildShopRecapPanel(RectTransform side)
        {
            var panel = BorderedCard(side, "ShopRecap");
            panel.transform.GetChild(0).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            EditorUI.VLayout(panel, 8, new RectOffset(18, 18, 14, 16), TextAnchor.UpperLeft, true, true, true, false);
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = true;
            var title = EditorUI.Readable(EditorUI.Text(panel.transform, "Title", "recap.shop_title", 24, SitePalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false));
            EditorUI.Layout(title, -1, 34);
            var line = EditorUI.Line(panel.transform, "TitleLine", SitePalette.Divider);
            EditorUI.Layout(line, -1, HDivider);
            var body = EditorUI.Readable(EditorUI.Text(panel.transform, "Body", string.Empty, 19, SitePalette.Text, TextAlignmentOptions.TopLeft));
            body.enableAutoSizing = false;
            body.fontSize = 19;
            body.lineSpacing = 4f;

            var view = panel.gameObject.AddComponent<ShopRecapPanelView>();
            view.group = group;
            view.titleLabel = title;
            view.bodyLabel = body;
            view.visibleSeconds = 10f;
            panel.gameObject.SetActive(false);
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

        /// <summary>
        /// The facecam: the video square at the top of the channel column, with the streamer, the REC dot and the LIVE badge
        /// with the stream timer. No name plate: hovering the video names the streamer (and the player).
        /// </summary>
        static FacecamView BuildFacecam(RectTransform column, out StreamOverlayView overlay)
        {
            // Bottom of the channel column: the streamer camera sits in the bottom-right corner of the page.
            var row = EditorUI.Rect(column, "Facecam").Place(new Vector2(0.5f, 0), Vector2.zero, new Vector2(ChannelWidth, ChannelWidth), new Vector2(0.5f, 0));
            var hit = row.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            var tooltip = EditorUI.Tooltip(row);

            var frame = EditorUI.Rect(row, "Frame").Stretch();
            var border = EditorUI.Image(frame, "Border", SitePalette.Outline, EditorUI.RoundedSmall);
            EditorUI.TagSite(border, SiteColourRole.PanelBorder);
            border.type = Image.Type.Sliced;
            border.rectTransform.Stretch();
            var screen = EditorUI.Image(frame, "Screen", UIPalette.Night, EditorUI.RoundedSmall);
            screen.type = Image.Type.Sliced;
            screen.rectTransform.Stretch(5, 5, 5, 5);
            EditorUI.RoundPanel(border, screen, 5);
            // A rounded mask (matching the screen's soft corners) so the streamer never pokes out at the corners.
            var maskShape = EditorUI.Image(screen.transform, "Mask", Color.white, EditorUI.PanelSprite);
            maskShape.type = Image.Type.Sliced;
            maskShape.pixelsPerUnitMultiplier = EditorUI.PanelRadius / Mathf.Max(1f, EditorUI.PanelRadius - 9f);
            maskShape.raycastTarget = false;
            maskShape.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var mask = maskShape.rectTransform.Stretch(4, 4, 4, 4);
            var content = EditorUI.Rect(mask, "Content").Stretch();

            var glow = EditorUI.Image(content, "Glow", UIPalette.Purple.WithAlpha(0.28f), EditorUI.Circle);
            glow.rectTransform.Box(new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            var placeholderArea = EditorUI.Rect(content, "PlaceholderArea").Box(new Vector2(0.2f, 0.26f), new Vector2(0.8f, 0.86f), Vector2.zero, Vector2.zero);
            var placeholder = EditorUI.Image(placeholderArea, "Placeholder", UIPalette.Purple, EditorUI.Circle);
            var fitter = placeholder.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            var initials = EditorUI.Text(placeholder.transform, "Initials", "?", 72, Color.white, TextAlignmentOptions.Center, true, true, false);
            initials.rectTransform.Stretch();

            // The streamer stands on the bottom edge (pivot at the feet, so idle breathing grows upward).
            var art = EditorUI.Image(content, "Art", Color.white);
            art.rectTransform.Box(new Vector2(0.03f, 0f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
            art.rectTransform.pivot = new Vector2(0.5f, 0f);
            art.preserveAspect = true;
            art.enabled = false;
            var portrait = art.gameObject.AddComponent<CharacterPortrait>();
            portrait.image = art;

            var rec = EditorUI.Image(screen.transform, "RecDot", UIPalette.Red, EditorUI.Circle);
            rec.rectTransform.Place(new Vector2(1, 1), new Vector2(-14, -14), new Vector2(16, 16), new Vector2(1, 1));
            overlay = BuildStreamOverlay(screen.transform);

            var view = row.gameObject.AddComponent<FacecamView>();
            view.frame = frame;
            view.border = border;
            view.content = content;
            view.glow = glow;
            view.portrait = portrait;
            view.placeholder = placeholder;
            view.initialsLabel = initials;
            view.nameLabel = null;
            view.recDot = rec;
            view.tooltip = tooltip;
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
            // A white card with a black border; the callout's colour is the stripe along the top (and the title, darkened to read on white).
            var fill = EditorUI.SitePanel(host, "Panel", SitePalette.Surface, SitePalette.Outline, EditorUI.ModalBorder, false, SiteArea.PopUps);
            var panel = fill.transform.parent.GetComponent<Image>();
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(1100, 200));
            var accent = EditorUI.Image(fill.transform, "Accent", UIPalette.Gold, null);
            accent.rectTransform.TopBand(0, 10, 0, 0);
            var title = EditorUI.Text(panel.transform, "Title", "-", 80, SitePalette.Text, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(22, 108, 20, 20);
            title.enableAutoSizing = true;
            title.fontSizeMin = 40;
            title.fontSizeMax = 80;
            var subtitle = EditorUI.Readable(EditorUI.Text(panel.transform, "Subtitle", string.Empty, 30, SitePalette.Text, TextAlignmentOptions.Center, false, false, false));
            subtitle.rectTransform.TopBand(132, 50, 30, 30);

            var view = host.gameObject.AddComponent<BannerView>();
            view.group = group;
            view.panel = panel.rectTransform;
            view.background = fill;
            view.titleLabel = title;
            view.subtitleLabel = subtitle;
            view.accent = accent;
            view.siteStyle = true;
            return view;
        }

        /// <summary>The full-screen Deplatform / Server Down image (Game Database > Stream Endings), with its caption.</summary>
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
            var host = EditorUI.ModalShell(overlay, "GuestPicker", new Vector2(1500, 860), out var root, out var panel, true);
            var title = EditorUI.Readable(EditorUI.Text(panel, "Title", "-", 40, SitePalette.Text, TextAlignmentOptions.Center, true, true, false));
            title.rectTransform.TopBand(26, 54, 40, 40);
            var hint = EditorUI.Readable(EditorUI.Text(panel, "Hint", string.Empty, 24, SitePalette.TextDim, TextAlignmentOptions.Center, false, false, false));
            hint.rectTransform.TopBand(82, 34, 40, 40);
            var pickerScroll = EditorUI.Scroll(panel, "Guests", out var grid, 16, EditorUI.Pad(10), GuestCardView.CellSize(0.72f));
            pickerScroll.GetComponent<RectTransform>().Box(Vector2.zero, Vector2.one, new Vector2(30, 112), new Vector2(-30, -128));
            LightScrollbar(pickerScroll);
            var cancel = EditorUI.SiteTextButton(panel, "Cancel", "Cancel  [Esc]", EditorUI.SiteButton.Light, 26);
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
            var host = EditorUI.ModalShell(overlay, "Rolodex", new Vector2(1640, 920), out var root, out var panel, true);
            var title = EditorUI.Readable(EditorUI.Text(panel, "Title", "-", 42, SitePalette.Text, TextAlignmentOptions.Center, true, true, false));
            title.rectTransform.TopBand(24, 56, 40, 40);
            var summary = EditorUI.Readable(EditorUI.Text(panel, "Summary", string.Empty, 22, SitePalette.Text, TextAlignmentOptions.Center));
            summary.rectTransform.TopBand(82, 76, 60, 60);
            var line = EditorUI.Line(panel, "SummaryLine", SitePalette.Divider);
            line.rectTransform.TopBand(160, HDivider, 40, 40);
            const float communitiesWidth = 400f;
            var rolodexScroll = EditorUI.Scroll(panel, "Guests", out var grid, 14, EditorUI.Pad(10), GuestCardView.CellSize(0.62f));
            rolodexScroll.GetComponent<RectTransform>().Box(Vector2.zero, Vector2.one, new Vector2(30, 112), new Vector2(-(communitiesWidth + 54), -166));
            LightScrollbar(rolodexScroll);

            // The communities in this Audience, in a column on the right (hover an icon for its rule).
            var columnLine = EditorUI.Line(panel, "CommunitiesLine", SitePalette.Divider);
            columnLine.rectTransform.Box(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-(communitiesWidth + 40), 112), new Vector2(-(communitiesWidth + 40) + VDivider, -176));
            var communitiesTitle = EditorUI.Readable(EditorUI.Text(panel, "CommunitiesTitle", "audience.communities_title", 26, SitePalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false));
            communitiesTitle.rectTransform.Box(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-(communitiesWidth + 20), -214), new Vector2(-30, -174));
            var communitiesScroll = EditorUI.Scroll(panel, "Communities", out var communitiesContent, 0, EditorUI.Pad(4));
            communitiesScroll.GetComponent<RectTransform>().Box(new Vector2(1, 0), new Vector2(1, 1), new Vector2(-(communitiesWidth + 20), 112), new Vector2(-30, -222));
            LightScrollbar(communitiesScroll);
            var communities = EditorUI.Readable(EditorUI.Text(communitiesContent, "List", string.Empty, 22, SitePalette.Text, TextAlignmentOptions.TopLeft));
            communities.enableAutoSizing = false;
            communities.fontSize = 22;
            communities.raycastTarget = true;
            var close = EditorUI.SiteTextButton(panel, "Close", "common.close_r", EditorUI.SiteButton.Dark, 26);
            close.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(300, 72), new Vector2(0.5f, 0));

            var view = host.gameObject.AddComponent<RolodexView>();
            view.communitiesLabel = communities;
            view.root = root;
            view.titleLabel = title;
            view.summaryLabel = summary;
            view.grid = grid;
            view.cardPrefab = prefabs.GuestCard;
            view.closeButton = close;
            return view;
        }

        /// <summary>The round summary (Free For All): one scrolling text with every player's round. RoundSummaryView writes it.</summary>
        static RoundSummaryView BuildRoundSummary(RectTransform overlay)
        {
            var host = EditorUI.ModalShell(overlay, "RoundSummary", new Vector2(1200, 880), out var root, out var panel, true);
            var title = EditorUI.Readable(EditorUI.Text(panel, "Title", "-", 40, SitePalette.Text, TextAlignmentOptions.Center, true, true, false));
            title.rectTransform.TopBand(24, 58, 40, 40);
            var titleLine = EditorUI.Line(panel, "TitleLine", SitePalette.Divider);
            titleLine.rectTransform.TopBand(88, HDivider, 40, 40);
            var scroll = EditorUI.Scroll(panel, "Lines", out var content, 0, EditorUI.Pad(8));
            scroll.GetComponent<RectTransform>().Box(Vector2.zero, Vector2.one, new Vector2(40, 116), new Vector2(-40, -96));
            LightScrollbar(scroll);
            var body = EditorUI.Readable(EditorUI.Text(content, "Body", string.Empty, 24, SitePalette.Text, TextAlignmentOptions.TopLeft));
            body.enableAutoSizing = false;
            body.fontSize = 24;
            var close = EditorUI.SiteTextButton(panel, "Close", "common.close", EditorUI.SiteButton.Dark, 26);
            close.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(300, 72), new Vector2(0.5f, 0));

            var view = host.gameObject.AddComponent<RoundSummaryView>();
            view.root = root;
            view.titleLabel = title;
            view.bodyLabel = body;
            view.scroll = scroll;
            view.closeButton = close;
            return view;
        }

        /// <summary>
        /// The stream / shop summary pop-up (UIFixes): a white card with a black border, a short title ("Great stream!") and a
        /// subtitle (which stream), a grey line, then one column per player with their streamer's round avatar, their name and
        /// streamer, the result and the key numbers, light grey lines between the columns, and Continue. RecapView fills it.
        /// </summary>
        static RecapView BuildRecap(RectTransform overlay)
        {
            const float columnWidth = 248f;
            const float avatarSize = 108f;
            var host = EditorUI.ModalShell(overlay, "StreamRecap", new Vector2(900, 660), out var root, out var panel, true);

            var title = EditorUI.Text(panel, "Title", "-", 44, SitePalette.Text, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.TopBand(20, 56, 30, 30);
            title.fontSizeMin = 26;
            var subtitle = EditorUI.Text(panel, "Subtitle", string.Empty, 22, SitePalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            subtitle.rectTransform.TopBand(78, 30, 30, 30);
            var titleLine = EditorUI.Line(panel, "TitleLine", SitePalette.Divider);
            titleLine.rectTransform.TopBand(122, HDivider, 30, 30);

            var columns = EditorUI.Rect(panel, "Columns").Box(Vector2.zero, Vector2.one, new Vector2(30, 108), new Vector2(-30, -136));
            EditorUI.HLayout(columns, 0, EditorUI.Pad(0), TextAnchor.UpperCenter, true, true, false, true);

            // The column template (inactive; RecapView clones one per player) and the grey line between columns.
            var columnView = RecapColumnTemplate(columns, columnWidth, avatarSize, out var divider);

            var close = EditorUI.SiteTextButton(panel, "Continue", "recap.continue", EditorUI.SiteButton.Dark, 26);
            close.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(320, 66), new Vector2(0.5f, 0));

            var view = host.gameObject.AddComponent<RecapView>();
            view.root = root;
            view.panel = panel;
            view.titleLabel = title;
            view.subtitleLabel = subtitle;
            view.columnsRoot = columns;
            view.columnTemplate = columnView;
            view.dividerTemplate = divider;
            view.closeButton = close;
            view.columnWidth = columnWidth + 2;
            view.panelPadding = 60f;
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
