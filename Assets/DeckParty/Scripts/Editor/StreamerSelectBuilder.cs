using DeckParty.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Builds the SELECT YOUR STREAMER screen, a fighting-game character select parody: big title and a mock round timer
    /// up top, your streamer on the left (name, power, a random quote), VS in the middle, your opponent on the right,
    /// and the roster grid along the bottom. Layout numbers are for the 1920x1080 reference canvas.
    /// </summary>
    internal static class StreamerSelectBuilder
    {
        public static StreamerSelectScreen Build(Transform parent)
        {
            var screen = SceneCommon.Screen(parent, "StreamerSelectScreen");

            // Diagonal colour slabs behind each fighter, fighting-game style.
            var leftSlab = EditorUI.Image(screen, "P1Slab", UIPalette.Pink.WithAlpha(0.22f), EditorUI.Rounded);
            leftSlab.rectTransform.Box(new Vector2(0, 0.28f), new Vector2(0.5f, 0.88f), new Vector2(-120, 0), new Vector2(-60, 0));
            leftSlab.rectTransform.localRotation = Quaternion.Euler(0, 0, 4f);
            var rightSlab = EditorUI.Image(screen, "P2Slab", UIPalette.Teal.WithAlpha(0.18f), EditorUI.Rounded);
            rightSlab.rectTransform.Box(new Vector2(0.5f, 0.28f), new Vector2(1, 0.88f), new Vector2(60, 0), new Vector2(120, 0));
            rightSlab.rectTransform.localRotation = Quaternion.Euler(0, 0, -4f);

            var titleShadow = EditorUI.Text(screen, "TitleShadow", "select.title", 92, UIPalette.Pink.WithAlpha(0.8f), TextAlignmentOptions.Center, true, true, false);
            titleShadow.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(6, -24), new Vector2(1500, 110), new Vector2(0.5f, 1));
            titleShadow.fontStyle |= FontStyles.Italic;
            var title = EditorUI.Text(screen, "Title", "select.title", 92, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            title.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(1500, 110), new Vector2(0.5f, 1));
            title.fontStyle |= FontStyles.Italic;
            var subtitle = EditorUI.Text(screen, "Subtitle", "-", 26, UIPalette.Text, TextAlignmentOptions.Center, false, false, false);
            subtitle.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -128), new Vector2(1400, 40), new Vector2(0.5f, 1));

            var timerBox = EditorUI.Panel(screen, "Timer", UIPalette.PanelDeep, SkinSlot.Badge, false);
            timerBox.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -176), new Vector2(150, 96), new Vector2(0.5f, 1));
            var timer = EditorUI.Text(timerBox.transform, "Label", "99", 72, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            timer.rectTransform.Stretch();

            // --- Player 1 (left)
            var p1 = EditorUI.Rect(screen, "Player1").Box(new Vector2(0, 0.3f), new Vector2(0.5f, 0.86f), new Vector2(60, 0), new Vector2(-150, 0));
            var p1Portrait = EditorUI.Rect(p1, "Portrait").Box(new Vector2(0, 0), new Vector2(0.42f, 1), Vector2.zero, Vector2.zero);
            var p1Placeholder = EditorUI.Image(p1Portrait, "Placeholder", UIPalette.Purple, EditorUI.Circle);
            p1Placeholder.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 300));
            var p1Initials = EditorUI.Text(p1Placeholder.transform, "Initials", "?", 120, Color.white, TextAlignmentOptions.Center, true, true, false);
            p1Initials.rectTransform.Stretch();
            var p1Art = EditorUI.Image(p1Portrait, "Art", Color.white);
            p1Art.rectTransform.Stretch();
            p1Art.preserveAspect = true;
            p1Art.enabled = false;

            var info = EditorUI.Rect(p1, "Info").Box(new Vector2(0.44f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            EditorUI.VLayout(info, 4, EditorUI.Pad(0), TextAnchor.UpperLeft, true, true, true, false);
            var p1Name = EditorUI.Text(info, "Name", "-", 58, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            p1Name.fontStyle |= FontStyles.Italic;
            EditorUI.Layout(p1Name, -1, 70);
            var p1Title = EditorUI.Text(info, "TitleLine", "-", 24, UIPalette.Teal, TextAlignmentOptions.MidlineLeft, false, false, false);
            EditorUI.Layout(p1Title, -1, 32);
            var p1Kind = EditorUI.Text(info, "Kind", "-", 18, UIPalette.Gold, TextAlignmentOptions.MidlineLeft, true, false, false);
            EditorUI.Layout(p1Kind, -1, 26);
            var p1Power = EditorUI.Text(info, "Power", "-", 28, UIPalette.Pink, TextAlignmentOptions.MidlineLeft, true, false, false);
            EditorUI.Layout(p1Power, -1, 36);
            var p1Description = EditorUI.Text(info, "Description", "-", 22, UIPalette.Text, TextAlignmentOptions.TopLeft);
            EditorUI.Layout(p1Description, -1, 84);
            var p1Cost = EditorUI.Text(info, "Cost", "-", 18, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
            EditorUI.Layout(p1Cost, -1, 26);
            var p1Warning = EditorUI.Text(info, "Warning", string.Empty, 18, UIPalette.Orange, TextAlignmentOptions.MidlineLeft, false, false, false);
            EditorUI.Layout(p1Warning, -1, 24);

            // A random line from the streamer's quotes in the Game Database (up to three lines; long ones shrink to fit).
            var p1Quote = EditorUI.Text(info, "Quote", "-", 22, UIPalette.TextDim, TextAlignmentOptions.TopLeft);
            EditorUI.Layout(p1Quote, -1, 96);
            p1Quote.fontSizeMin = 14;

            // --- VS
            var vs = EditorUI.Text(screen, "VS", "select.vs", 150, UIPalette.Red, TextAlignmentOptions.Center, true, true, false);
            vs.rectTransform.Place(new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(300, 180));
            vs.fontStyle |= FontStyles.Italic;
            vs.outlineWidth = 0.25f;
            vs.outlineColor = new Color32(255, 220, 80, 255);

            // --- Opponent (right)
            var p2 = EditorUI.Rect(screen, "Player2").Box(new Vector2(0.5f, 0.3f), new Vector2(1, 0.86f), new Vector2(150, 0), new Vector2(-60, 0));
            var p2Placeholder = EditorUI.Image(p2, "Placeholder", UIPalette.Gold, EditorUI.Circle);
            p2Placeholder.rectTransform.Place(new Vector2(0.72f, 0.58f), Vector2.zero, new Vector2(280, 280));
            var p2Initials = EditorUI.Text(p2Placeholder.transform, "Initials", "?", 120, UIPalette.Night, TextAlignmentOptions.Center, true, true, false);
            p2Initials.rectTransform.Stretch();
            var p2Name = EditorUI.Text(p2, "Name", "-", 50, UIPalette.Text, TextAlignmentOptions.MidlineRight, true, true, false);
            p2Name.fontStyle |= FontStyles.Italic;
            p2Name.rectTransform.Box(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 40), new Vector2(0, 110));
            var p2Sub = EditorUI.Text(p2, "Sub", "-", 22, UIPalette.TextDim, TextAlignmentOptions.MidlineRight, false, false, false);
            p2Sub.rectTransform.Box(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 40));

            // --- Roster grid (2 rows of 7)
            var rosterPanel = EditorUI.Panel(screen, "Roster", UIPalette.PanelDeep.WithAlpha(0.88f));
            rosterPanel.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 118), new Vector2(1300, 300), new Vector2(0.5f, 0));
            var grid = EditorUI.Rect(rosterPanel.transform, "Grid").Stretch(20, 16, 20, 16);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(166, 128);
            layout.spacing = new Vector2(10, 12);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 7;

            var tile = BuildTile(screen);

            var hint = EditorUI.Text(screen, "Hint", "select.hint", 20, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            hint.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0, 84), new Vector2(1300, 30), new Vector2(0.5f, 0));

            var back = SceneCommon.BackButton(screen);
            var confirm = EditorUI.Button(screen, "Confirm", "select.confirm", ButtonStyle.Gold, 38, out var confirmLabel);
            confirm.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-60, 34), new Vector2(340, 96), new Vector2(1, 0));

            var flash = EditorUI.Image(screen, "Flash", Color.white.WithAlpha(0f));
            flash.rectTransform.Stretch();
            flash.gameObject.SetActive(false);
            var announcer = EditorUI.Text(screen, "Announcer", "-", 110, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            announcer.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1800, 220));
            announcer.fontStyle |= FontStyles.Italic;
            announcer.outlineWidth = 0.3f;
            announcer.outlineColor = new Color32(0, 0, 0, 230);
            announcer.gameObject.SetActive(false);

            var view = screen.gameObject.AddComponent<StreamerSelectScreen>();
            view.titleLabel = title;
            view.subtitleLabel = subtitle;
            view.timerLabel = timer;
            view.hintLabel = hint;
            view.grid = grid;
            view.tilePrefab = tile;
            view.p1Root = p1;
            view.p1Portrait = p1Art;
            view.p1Placeholder = p1Placeholder;
            view.p1Initials = p1Initials;
            view.p1Name = p1Name;
            view.p1Title = p1Title;
            view.p1Kind = p1Kind;
            view.p1Power = p1Power;
            view.p1Description = p1Description;
            view.p1Cost = p1Cost;
            view.p1Quote = p1Quote;
            view.p1Warning = p1Warning;
            view.p2Root = p2;
            view.p2Placeholder = p2Placeholder;
            view.p2Initials = p2Initials;
            view.p2Name = p2Name;
            view.p2Sub = p2Sub;
            view.vsLabel = vs;
            view.confirmButton = confirm;
            view.confirmLabel = confirmLabel;
            view.backButton = back;
            view.announcerLabel = announcer;
            view.flash = flash;
            return view;
        }

        /// <summary>The roster tile template (kept inactive in the scene; the screen clones it per streamer).</summary>
        static StreamerTileView BuildTile(Transform parent)
        {
            var frame = EditorUI.Image(parent, "TileTemplate", UIPalette.Border, EditorUI.Rounded, SkinSlot.CardFrame, true);
            frame.rectTransform.sizeDelta = new Vector2(166, 128);
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            EditorUI.NoNavigation(button);

            var anim = EditorUI.Rect(frame.transform, "Anim").Stretch();
            var inner = EditorUI.Image(anim, "Inner", UIPalette.PanelDeep, EditorUI.Rounded);
            inner.rectTransform.Stretch(5, 5, 5, 5);
            var placeholder = EditorUI.Image(anim, "Placeholder", UIPalette.Purple, EditorUI.Circle);
            placeholder.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(76, 76), new Vector2(0.5f, 1));
            var initials = EditorUI.Text(placeholder.transform, "Initials", "?", 32, Color.white, TextAlignmentOptions.Center, true, true, false);
            initials.rectTransform.Stretch();
            var portrait = EditorUI.Image(anim, "Art", Color.white);
            portrait.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(110, 86), new Vector2(0.5f, 1));
            portrait.preserveAspect = true;
            portrait.enabled = false;
            var name = EditorUI.Text(anim, "Name", "-", 16, UIPalette.Text, TextAlignmentOptions.Center, true, false, false);
            name.rectTransform.BottomBand(6, 28, 6, 6);

            var cursor = EditorUI.Image(frame.transform, "Cursor", UIPalette.Gold, EditorUI.RoundedSmall);
            cursor.rectTransform.Place(new Vector2(0, 1), new Vector2(-6, 10), new Vector2(48, 26), new Vector2(0, 1));
            var cursorLabel = EditorUI.Text(cursor.transform, "Label", "P1", 16, UIPalette.Night, TextAlignmentOptions.Center, true, true, false);
            cursorLabel.rectTransform.Stretch();
            cursor.gameObject.SetActive(false);

            var view = frame.gameObject.AddComponent<StreamerTileView>();
            view.button = button;
            view.frame = frame;
            view.portrait = portrait;
            view.placeholder = placeholder;
            view.initialsLabel = initials;
            view.nameLabel = name;
            view.cursor = cursor.gameObject;
            view.cursorLabel = cursorLabel;
            view.animRoot = anim;
            frame.gameObject.SetActive(false);
            return view;
        }
    }
}
