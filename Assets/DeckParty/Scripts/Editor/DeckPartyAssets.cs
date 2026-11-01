using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DeckParty.Core;
using DeckParty.Networking;
using DeckParty.UI;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.EditorTools
{
    /// <summary>The UI prefabs every scene builder needs.</summary>
    internal sealed class UIPrefabs
    {
        public GuestCardView GuestCard;
        public GuestRowView GuestRow;
        public ScenarioItemView ScenarioItem;
        public PlayerRowView PlayerRow;
        public TMP_Text TextLine;
    }

    /// <summary>
    /// Card layout (200x290 design size). Change these to resize the portrait area, then run
    /// Deck Party > Setup > Rebuild Scenes and UI Prefabs. The old prefabs are backed up first.
    /// </summary>
    internal static class CardLayout
    {
        public const float PortraitTop = 10f;
        public const float PortraitHeight = 178f;
        public const float PlaceholderSize = 140f;
        public const float NameTop = 192f;
        public const float StatsTop = 221f;
        public const float BadgeRowTop = 152f;
        public const float ShopRowHeight = 140f;
        public const float ShopPortrait = 116f;
    }

    /// <summary>Creates folders, data assets, the network session prefab and the UI prefabs.</summary>
    internal static class DeckPartyAssets
    {
        public const string PrefabBackupFolder = "Assets/DeckParty/Prefabs/Backup";
        // ------------------------------------------------------------------ folders

        public static void EnsureFolders()
        {
            foreach (var path in new[]
                     {
                         DeckPartyPaths.Data, DeckPartyPaths.Venues, DeckPartyPaths.Resources, DeckPartyPaths.NetworkResources,
                         DeckPartyPaths.Prefabs, DeckPartyPaths.Scenes, DeckPartyPaths.ArtCharacters, DeckPartyPaths.ArtStreamers, DeckPartyPaths.ArtBackgrounds,
                         DeckPartyPaths.ArtUI, DeckPartyPaths.Audio + "/Music", DeckPartyPaths.Audio + "/SFX", PlaceholderArt.Folder,
                     })
            {
                EnsureFolder(path);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ------------------------------------------------------------------ data

        public static GameDatabase EnsureDataAssets()
        {
            var venues = new List<VenueDefinition>();
            bool migrated = false;
            foreach (var preset in VenuePresets.All)
            {
                string path = $"{DeckPartyPaths.Venues}/Venue_{preset.Id}.asset";
                var venue = AssetDatabase.LoadAssetAtPath<VenueDefinition>(path);

                // Venues from before the streaming re-theme are renamed in place, keeping any art already assigned.
                string legacyPath = $"{DeckPartyPaths.Venues}/Venue_{preset.LegacyId}.asset";
                if (venue == null && !string.IsNullOrEmpty(preset.LegacyId) && AssetDatabase.LoadAssetAtPath<VenueDefinition>(legacyPath) != null)
                {
                    var error = AssetDatabase.RenameAsset(legacyPath, $"Venue_{preset.Id}");
                    if (string.IsNullOrEmpty(error))
                    {
                        venue = AssetDatabase.LoadAssetAtPath<VenueDefinition>(path);
                        VenuePresets.Apply(venue, preset);
                        EditorUtility.SetDirty(venue);
                        migrated = true;
                        Debug.Log($"[DeckParty] Venue '{preset.LegacyId}' is now '{preset.Id}' ({preset.Name}).");
                    }
                    else
                    {
                        Debug.LogWarning($"[DeckParty] Couldn't rename {legacyPath}: {error}");
                    }
                }

                if (venue == null)
                {
                    venue = ScriptableObject.CreateInstance<VenueDefinition>();
                    VenuePresets.Apply(venue, preset);
                    AssetDatabase.CreateAsset(venue, path);
                }
                venues.Add(venue);
            }

            var skin = LoadOrCreate<UISkin>(DeckPartyPaths.Skin);
            skin.EnsureAllSlots();
            AssignDefaultSkin(skin);
            EditorUtility.SetDirty(skin);

            var audio = LoadOrCreate<AudioLibrary>(DeckPartyPaths.AudioLibrary);
            audio.EnsureAllSlots();
            EditorUtility.SetDirty(audio);

            var db = LoadOrCreate<GameDatabase>(DeckPartyPaths.Database);
            int reset = db.EnsureAllGuests();
            if (reset > 0)
                Debug.LogWarning($"[DeckParty] The viewer roster changed: {reset} portrait/animation setting(s) belonged to removed characters and were cleared. Re-assign them with Deck Party > Select Game Database.");

            // One stream setup per streamer, linked in the database (art you assign is kept).
            db.EnsureAllStreamers();
            foreach (var preset in VenuePresets.StreamerStages)
            {
                string path = $"{DeckPartyPaths.Venues}/Venue_{preset.Id}.asset";
                var venue = AssetDatabase.LoadAssetAtPath<VenueDefinition>(path);
                if (venue == null)
                {
                    venue = ScriptableObject.CreateInstance<VenueDefinition>();
                    VenuePresets.Apply(venue, preset);
                    AssetDatabase.CreateAsset(venue, path);
                }
                venues.Add(venue);
            }
            foreach (var art in db.streamers)
                if (art.venue == null) art.venue = venues.Find(v => v.id == VenuePresets.StreamerStageId(art.type));

            // Built-in venues first (in preset order), then any venues you made yourself.
            var custom = db.venues.FindAll(v => v != null && !venues.Contains(v));
            db.venues.Clear();
            db.venues.AddRange(venues);
            db.venues.AddRange(custom);

            if (migrated) db.evolvingVenue.Clear();
            db.evolvingVenue.RemoveAll(stage => stage == null || stage.venue == null);
            if (db.evolvingVenue.Count == 0)
            {
                foreach (var stage in VenuePresets.Evolving)
                {
                    var venue = venues.Find(v => v.id == stage.venueId);
                    if (venue != null) db.evolvingVenue.Add(new EvolvingStage { minCapacity = stage.minCapacity, venue = venue });
                }
            }

            if (db.skin == null) db.skin = skin;
            if (db.audio == null) db.audio = audio;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            GameDatabase.ResetCache();
            return db;
        }

        static void AssignDefaultSkin(UISkin skin)
        {
            foreach (var entry in skin.entries)
            {
                if (entry == null || entry.sprite != null) continue;
                switch (entry.slot)
                {
                    case SkinSlot.Panel:
                    case SkinSlot.PanelAlt:
                    case SkinSlot.Button:
                    case SkinSlot.ButtonPrimary:
                    case SkinSlot.ButtonSecondary:
                    case SkinSlot.ButtonDanger:
                    case SkinSlot.CardFrame:
                    case SkinSlot.CardFrameStar:
                    case SkinSlot.InputField:
                    case SkinSlot.TopBar:
                    case SkinSlot.Tooltip:
                        Set(entry, EditorUI.Rounded, Image.Type.Sliced);
                        break;
                    case SkinSlot.Badge:
                    case SkinSlot.ScrollHandle:
                        Set(entry, EditorUI.RoundedSmall, Image.Type.Sliced);
                        break;
                    case SkinSlot.SlotEmpty:
                        Set(entry, EditorUI.Frame, Image.Type.Sliced);
                        break;
                    case SkinSlot.Circle: Set(entry, EditorUI.Circle, Image.Type.Simple); break;
                    case SkinSlot.Glow: Set(entry, EditorUI.Glow, Image.Type.Simple); break;
                    case SkinSlot.IconPop: Set(entry, EditorUI.Sparkle, Image.Type.Simple); break;
                    case SkinSlot.IconCash: Set(entry, EditorUI.Coin, Image.Type.Simple); break;
                    case SkinSlot.IconTrouble: Set(entry, EditorUI.Cross, Image.Type.Simple); break;
                    case SkinSlot.IconStar: Set(entry, EditorUI.Star, Image.Type.Simple); break;
                    case SkinSlot.IconCancel: Set(entry, EditorUI.Shield, Image.Type.Simple); break;
                    case SkinSlot.IconDoor: Set(entry, EditorUI.Door, Image.Type.Simple); break;
                    case SkinSlot.IconCrown: Set(entry, EditorUI.Crown, Image.Type.Simple); break;
                    case SkinSlot.IconCheck: Set(entry, EditorUI.Check, Image.Type.Simple); break;
                }
            }
        }

        static void Set(SkinEntry entry, Sprite sprite, Image.Type type)
        {
            entry.sprite = sprite;
            entry.imageType = type;
            entry.pixelsPerUnitMultiplier = 1f;
        }

        // ------------------------------------------------------------------ network

        public static GameObject EnsureNetworkPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(DeckPartyPaths.NetworkSessionPrefab);
            if (existing != null && existing.GetComponent<DeckPartyNetSession>() != null)
            {
                RefreshNetworkHash(existing);
                return existing;
            }

            var go = new GameObject("DeckPartyNetSession");
            var networkObject = go.AddComponent<NetworkObject>();
            networkObject.SynchronizeTransform = false;
            networkObject.ActiveSceneSynchronization = false;
            // Every peer moves the session into DontDestroyOnLoad itself, so Netcode doesn't need to sync scene moves.
            networkObject.SceneMigrationSynchronization = false;
            networkObject.DontDestroyWithOwner = true;
            go.AddComponent<DeckPartyNetSession>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, DeckPartyPaths.NetworkSessionPrefab);
            Object.DestroyImmediate(go);
            RefreshNetworkHash(prefab);
            return prefab;
        }

        /// <summary>
        /// Netcode identifies network prefabs by a GlobalObjectIdHash generated in NetworkObject.OnValidate.
        /// Prefabs created from code may not have run it yet, so run it now and persist the result.
        /// </summary>
        static void RefreshNetworkHash(GameObject prefab)
        {
            var networkObject = prefab.GetComponent<NetworkObject>();
            if (networkObject == null) return;
            var validate = typeof(NetworkObject).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (validate != null)
            {
                try
                {
                    validate.Invoke(networkObject, null);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[DeckParty] Could not refresh NetworkObject hash automatically ({e.Message}). Re-saving the prefab will fix it.");
                }
            }
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ UI prefabs

        public static void EnsureUIPrefabs(bool overwrite)
        {
            Ensure<GuestCardView>(DeckPartyPaths.GuestCardPrefab, BuildGuestCard, overwrite);
            Ensure<GuestRowView>(DeckPartyPaths.GuestRowPrefab, BuildGuestRow, overwrite);
            Ensure<ScenarioItemView>(DeckPartyPaths.ScenarioItemPrefab, BuildScenarioItem, overwrite);
            Ensure<PlayerRowView>(DeckPartyPaths.PlayerRowPrefab, BuildPlayerRow, overwrite);
            Ensure<TextMeshProUGUI>(DeckPartyPaths.TextLinePrefab, BuildTextLine, overwrite);
        }

        /// <summary>
        /// Loads the UI prefabs fresh. Scene builders call this right after creating their scene: opening a scene
        /// unloads assets nothing references, so references held from before would point at destroyed objects.
        /// </summary>
        public static UIPrefabs LoadUIPrefabs()
        {
            return new UIPrefabs
            {
                GuestCard = LoadPrefab<GuestCardView>(DeckPartyPaths.GuestCardPrefab),
                GuestRow = LoadPrefab<GuestRowView>(DeckPartyPaths.GuestRowPrefab),
                ScenarioItem = LoadPrefab<ScenarioItemView>(DeckPartyPaths.ScenarioItemPrefab),
                PlayerRow = LoadPrefab<PlayerRowView>(DeckPartyPaths.PlayerRowPrefab),
                TextLine = LoadPrefab<TextMeshProUGUI>(DeckPartyPaths.TextLinePrefab),
            };
        }

        static T LoadPrefab<T>(string path) where T : Component
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var component = go != null ? go.GetComponent<T>() : null;
            if (component == null)
                throw new System.InvalidOperationException($"UI prefab missing or broken: {path}. Run Deck Party > Setup > Rebuild Scenes and UI Prefabs.");
            return component;
        }

        static void Ensure<T>(string path, System.Func<GameObject> build, bool overwrite) where T : Component
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!overwrite && existing != null && existing.GetComponent<T>() != null) return;
            if (existing != null) BackupPrefab(path);

            var go = build();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        /// <summary>Copies a prefab to Prefabs/Backup (dated) before it's regenerated, so hand edits can be recovered.</summary>
        static void BackupPrefab(string path)
        {
            EnsureFolder(PrefabBackupFolder);
            string target = $"{PrefabBackupFolder}/{Path.GetFileNameWithoutExtension(path)}_{System.DateTime.Now:yyyyMMdd_HHmmss}.prefab";
            if (!AssetDatabase.CopyAsset(path, target)) Debug.LogWarning($"[DeckParty] Couldn't back up {path}.");
        }

        static GameObject BuildGuestCard()
        {
            var root = EditorUI.Rect(null, "GuestCard");
            root.sizeDelta = new Vector2(GuestCardView.DesignWidth, GuestCardView.DesignHeight);

            var visual = EditorUI.Rect(root, "Visual").Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(GuestCardView.DesignWidth, GuestCardView.DesignHeight));
            var anim = EditorUI.Rect(visual, "Anim").Stretch();

            var highlight = EditorUI.Image(anim, "Highlight", UIPalette.Gold, EditorUI.Frame);
            highlight.rectTransform.Stretch(-9, -9, -9, -9);
            highlight.enabled = false;

            var frame = EditorUI.Image(anim, "Frame", UIPalette.PanelAlt, EditorUI.Rounded, SkinSlot.CardFrame, true);
            frame.rectTransform.Stretch();

            var content = EditorUI.Rect(anim, "Content").Stretch();

            var portraitBg = EditorUI.Image(content, "PortraitBg", UIPalette.PanelDeep, EditorUI.Rounded);
            portraitBg.rectTransform.TopBand(CardLayout.PortraitTop, CardLayout.PortraitHeight, 10, 10);
            var placeholder = EditorUI.Image(portraitBg.transform, "Placeholder", UIPalette.Teal, EditorUI.Circle, SkinSlot.Circle);
            placeholder.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardLayout.PlaceholderSize, CardLayout.PlaceholderSize));
            var initials = EditorUI.Text(placeholder.transform, "Initials", "Gu", 56, UIPalette.Night, TextAlignmentOptions.Center, true, true, false);
            initials.rectTransform.Stretch();
            var portrait = EditorUI.Image(portraitBg.transform, "Portrait", Color.white);
            portrait.rectTransform.Stretch(3, 3, 3, 3);
            portrait.preserveAspect = true;
            portrait.enabled = false;
            // Animated characters (prefab / controller in the Game Database) replace the sprite; SSU status effects sit on top.
            var characterPortrait = portrait.gameObject.AddComponent<CharacterPortrait>();
            characterPortrait.image = portrait;
            portrait.gameObject.AddComponent<CardFx>();

            var star = EditorUI.Icon(content, "StarBadge", EditorUI.Star, UIPalette.Gold, SkinSlot.IconStar, 46);
            star.rectTransform.Place(new Vector2(0, 1), new Vector2(4, -4), new Vector2(46, 46), new Vector2(0, 1));
            var trouble = EditorUI.Icon(content, "TroubleBadge", EditorUI.Cross, UIPalette.Red, SkinSlot.IconTrouble, 40);
            trouble.rectTransform.Place(new Vector2(1, 1), new Vector2(-6, -6), new Vector2(40, 40), new Vector2(1, 1));
            var calm = EditorUI.Icon(content, "CancelBadge", EditorUI.Shield, UIPalette.Teal, SkinSlot.IconCancel, 40);
            calm.rectTransform.Place(new Vector2(1, 1), new Vector2(-6, -50), new Vector2(40, 40), new Vector2(1, 1));

            var bonus = EditorUI.Image(content, "BonusBadge", UIPalette.Pink, EditorUI.RoundedSmall, SkinSlot.Badge);
            bonus.rectTransform.Place(new Vector2(1, 1), new Vector2(-8, -CardLayout.BadgeRowTop), new Vector2(64, 34), new Vector2(1, 1));
            var bonusLabel = EditorUI.Text(bonus.transform, "Label", "+1", 21, UIPalette.Text, TextAlignmentOptions.Center, true, false, false);
            bonusLabel.rectTransform.Stretch();

            var count = EditorUI.Image(content, "CountBadge", UIPalette.Purple, EditorUI.RoundedSmall, SkinSlot.Badge);
            count.rectTransform.Place(new Vector2(0, 1), new Vector2(8, -CardLayout.BadgeRowTop), new Vector2(60, 34), new Vector2(0, 1));
            var countLabel = EditorUI.Text(count.transform, "Label", "x2", 21, UIPalette.Text, TextAlignmentOptions.Center, true, false, false);
            countLabel.rectTransform.Stretch();
            count.gameObject.SetActive(false);

            var nameLabel = EditorUI.Text(content, "Name", "-", 22, UIPalette.Text, TextAlignmentOptions.Center, true, false, false);
            nameLabel.rectTransform.TopBand(CardLayout.NameTop, 28, 6, 6);
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 14;
            nameLabel.fontSizeMax = 22;

            var stats = EditorUI.Text(content, "Stats", "-", 19, UIPalette.Text, TextAlignmentOptions.Center, false, false, false);
            stats.rectTransform.TopBand(CardLayout.StatsTop, 24, 4, 4);
            stats.enableAutoSizing = true;
            stats.fontSizeMin = 11;
            stats.fontSizeMax = 19;

            var ability = EditorUI.Button(content, "Ability", "-", ButtonStyle.Secondary, 18, out var abilityLabel);
            ability.GetComponent<RectTransform>().Place(new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(184, 38), new Vector2(0.5f, 0));
            abilityLabel.enableAutoSizing = true;
            abilityLabel.fontSizeMin = 12;
            abilityLabel.fontSizeMax = 18;
            var abilityTooltip = EditorUI.Tooltip(ability);

            var empty = EditorUI.Rect(anim, "Empty").Stretch();
            var emptyFrame = EditorUI.Image(empty, "Frame", UIPalette.Border.WithAlpha(0.7f), EditorUI.Frame, SkinSlot.SlotEmpty);
            emptyFrame.rectTransform.Stretch(6, 6, 6, 6);
            var emptyLabel = EditorUI.Text(empty, "Label", "-", 22, UIPalette.TextDim.WithAlpha(0.6f), TextAlignmentOptions.Center, false, false, false);
            emptyLabel.rectTransform.Stretch();
            empty.gameObject.SetActive(false);

            var dim = EditorUI.Image(anim, "Dim", new Color(0.03f, 0.02f, 0.06f, 0.62f), EditorUI.Rounded);
            dim.rectTransform.Stretch();
            dim.enabled = false;

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.None;
            EditorUI.NoNavigation(button);
            var feedback = root.gameObject.AddComponent<ButtonFeedback>();
            feedback.hoverScale = 1.03f;
            feedback.hoverSound = false;
            var tooltip = root.gameObject.AddComponent<TooltipTrigger>();

            var view = root.gameObject.AddComponent<GuestCardView>();
            view.button = button;
            view.visual = visual;
            view.animRoot = anim;
            view.frame = frame;
            view.highlight = highlight;
            view.content = content.gameObject;
            view.emptyState = empty.gameObject;
            view.portrait = portrait;
            view.placeholder = placeholder;
            view.initialsLabel = initials;
            view.nameLabel = nameLabel;
            view.statsLabel = stats;
            view.starBadge = star.gameObject;
            view.troubleBadge = trouble.gameObject;
            view.troubleIcon = trouble;
            view.cancelBadge = calm.gameObject;
            view.bonusBadge = bonus.gameObject;
            view.bonusLabel = bonusLabel;
            view.countBadge = count.gameObject;
            view.countLabel = countLabel;
            view.abilityButton = ability;
            view.abilityLabel = abilityLabel;
            view.abilityTooltip = abilityTooltip;
            view.dim = dim;
            view.tooltip = tooltip;
            view.characterPortrait = characterPortrait;
            return root.gameObject;
        }

        static GameObject BuildGuestRow()
        {
            var bg = EditorUI.Image(null, "GuestRow", UIPalette.PanelAlt, EditorUI.Rounded, SkinSlot.PanelAlt, true);
            bg.rectTransform.sizeDelta = new Vector2(900, CardLayout.ShopRowHeight);
            EditorUI.Layout(bg, -1, CardLayout.ShopRowHeight);

            var portraitBg = EditorUI.Image(bg.transform, "PortraitBg", UIPalette.PanelDeep, EditorUI.Rounded);
            portraitBg.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(CardLayout.ShopPortrait, CardLayout.ShopPortrait), new Vector2(0, 0.5f));
            var placeholder = EditorUI.Image(portraitBg.transform, "Placeholder", UIPalette.Teal, EditorUI.Circle, SkinSlot.Circle);
            placeholder.rectTransform.Stretch(8, 8, 8, 8);
            var initials = EditorUI.Text(placeholder.transform, "Initials", "Gu", 34, UIPalette.Night, TextAlignmentOptions.Center, true, true, false);
            initials.rectTransform.Stretch();
            var portrait = EditorUI.Image(portraitBg.transform, "Portrait", Color.white);
            portrait.rectTransform.Stretch(3, 3, 3, 3);
            portrait.preserveAspect = true;
            portrait.enabled = false;
            portrait.gameObject.AddComponent<CharacterPortrait>().image = portrait;

            var star = EditorUI.Icon(bg.transform, "StarTag", EditorUI.Star, UIPalette.Gold, SkinSlot.IconStar, 34);
            star.rectTransform.Place(new Vector2(0, 1), new Vector2(6, -6), new Vector2(34, 34), new Vector2(0, 1));

            float textLeft = CardLayout.ShopPortrait + 28f;
            var name = EditorUI.Text(bg.transform, "Name", "-", 26, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, false, false);
            name.rectTransform.Box(new Vector2(0, 1), new Vector2(1, 1), new Vector2(textLeft, -42), new Vector2(-250, -8));
            var stats = EditorUI.Text(bg.transform, "Stats", "-", 20, UIPalette.Text, TextAlignmentOptions.MidlineLeft, false, false, false);
            stats.rectTransform.Box(new Vector2(0, 1), new Vector2(1, 1), new Vector2(textLeft, -68), new Vector2(-250, -42));
            var description = EditorUI.Text(bg.transform, "Description", "-", 19, UIPalette.TextDim, TextAlignmentOptions.TopLeft);
            description.rectTransform.Box(new Vector2(0, 0), new Vector2(1, 1), new Vector2(textLeft, 6), new Vector2(-250, -70));
            description.enableAutoSizing = true;
            description.fontSizeMin = 14;
            description.fontSizeMax = 19;

            var cost = EditorUI.Text(bg.transform, "Cost", "-", 26, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            cost.enableAutoSizing = true;
            cost.fontSizeMin = 14;
            cost.fontSizeMax = 26;
            cost.rectTransform.Place(new Vector2(1, 1), new Vector2(-122, -10), new Vector2(220, 34), new Vector2(0.5f, 1));
            var owned = EditorUI.Text(bg.transform, "Owned", "-", 18, UIPalette.TextDim, TextAlignmentOptions.Center, false, false, false);
            owned.rectTransform.Place(new Vector2(1, 1), new Vector2(-122, -44), new Vector2(220, 24), new Vector2(0.5f, 1));
            var buy = EditorUI.Button(bg.transform, "Buy", "-", ButtonStyle.Primary, 24, out var buyLabel);
            buy.GetComponent<RectTransform>().Place(new Vector2(1, 0), new Vector2(-122, 10), new Vector2(200, 48), new Vector2(0.5f, 0));
            var buyTooltip = EditorUI.Tooltip(buy);
            var tooltip = EditorUI.Tooltip(bg);

            var view = bg.gameObject.AddComponent<GuestRowView>();
            view.background = bg;
            view.portrait = portrait;
            view.placeholder = placeholder;
            view.initialsLabel = initials;
            view.nameLabel = name;
            view.tagsLabel = null;
            view.descriptionLabel = description;
            view.statsLabel = stats;
            view.costLabel = cost;
            view.ownedLabel = owned;
            view.starTag = star.gameObject;
            view.buyButton = buy;
            view.buyLabel = buyLabel;
            view.buyTooltip = buyTooltip;
            view.tooltip = tooltip;
            return bg.gameObject;
        }

        static GameObject BuildScenarioItem()
        {
            var bg = EditorUI.Image(null, "ScenarioItem", UIPalette.Panel, EditorUI.Rounded, SkinSlot.PanelAlt, true);
            bg.rectTransform.sizeDelta = new Vector2(520, 100);
            EditorUI.Layout(bg, -1, 100);

            var outline = EditorUI.Image(bg.transform, "Selected", UIPalette.Pink, EditorUI.Frame);
            outline.rectTransform.Stretch(-4, -4, -4, -4);
            outline.gameObject.SetActive(false);

            var title = EditorUI.Text(bg.transform, "Title", "-", 30, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, true, false);
            title.rectTransform.Box(new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -54), new Vector2(-70, -10));
            var subtitle = EditorUI.Text(bg.transform, "Subtitle", "-", 20, UIPalette.TextDim, TextAlignmentOptions.MidlineLeft, false, false, false);
            subtitle.rectTransform.Box(new Vector2(0, 0), new Vector2(1, 1), new Vector2(24, 8), new Vector2(-70, -54));
            subtitle.overflowMode = TextOverflowModes.Ellipsis;

            var check = EditorUI.Icon(bg.transform, "Cleared", EditorUI.Check, UIPalette.Green, SkinSlot.IconCheck, 42);
            check.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(42, 42), new Vector2(1, 0.5f));

            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            EditorUI.NoNavigation(button);
            var colors = button.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.2f, 1f);
            button.colors = colors;
            bg.gameObject.AddComponent<ButtonFeedback>().hoverScale = 1.02f;

            var view = bg.gameObject.AddComponent<ScenarioItemView>();
            view.button = button;
            view.background = bg;
            view.titleLabel = title;
            view.subtitleLabel = subtitle;
            view.clearedIcon = check.gameObject;
            view.selectedOutline = outline.gameObject;
            return bg.gameObject;
        }

        static GameObject BuildPlayerRow()
        {
            var bg = EditorUI.Image(null, "PlayerRow", UIPalette.PanelAlt, EditorUI.Rounded, SkinSlot.PanelAlt, true);
            // Tall enough for the presence line (what the player is doing right now) under the status.
            bg.rectTransform.sizeDelta = new Vector2(500, 100);
            EditorUI.Layout(bg, -1, 100);

            var rank = EditorUI.Text(bg.transform, "Rank", "#1", 28, UIPalette.Gold, TextAlignmentOptions.Center, true, true, false);
            rank.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(6, 0), new Vector2(56, 56), new Vector2(0, 0.5f));
            var swatch = EditorUI.Image(bg.transform, "Swatch", UIPalette.Pink, EditorUI.Circle, SkinSlot.Circle);
            swatch.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(30, 30), new Vector2(0, 0.5f));

            // Streamer portrait next to the colour swatch (hidden for players without a streamer).
            var streamerFrame = EditorUI.Rect(bg.transform, "Streamer").Place(new Vector2(0, 0.5f), new Vector2(104, 0), new Vector2(48, 48), new Vector2(0, 0.5f));
            var streamerPlaceholder = EditorUI.Image(streamerFrame, "Placeholder", UIPalette.Purple, EditorUI.Circle);
            streamerPlaceholder.rectTransform.Stretch();
            var streamerInitials = EditorUI.Text(streamerPlaceholder.transform, "Initials", "?", 20, Color.white, TextAlignmentOptions.Center, true, true, false);
            streamerInitials.rectTransform.Stretch();
            var streamerArt = EditorUI.Image(streamerFrame, "Art", Color.white);
            streamerArt.rectTransform.Stretch();
            streamerArt.preserveAspect = true;
            streamerFrame.gameObject.SetActive(false);

            var name = EditorUI.Text(bg.transform, "Name", "-", 26, UIPalette.Text, TextAlignmentOptions.MidlineLeft, true, false, false);
            name.rectTransform.Box(new Vector2(0, 1), new Vector2(1, 1), new Vector2(160, -40), new Vector2(-92, -6));
            name.overflowMode = TextOverflowModes.Ellipsis;
            var status = EditorUI.Text(bg.transform, "Status", "-", 16, UIPalette.TextDim, TextAlignmentOptions.TopLeft);
            status.rectTransform.Box(new Vector2(0, 0), new Vector2(1, 1), new Vector2(160, 4), new Vector2(-92, -40));
            status.enableAutoSizing = true;
            status.fontSizeMin = 11;
            status.fontSizeMax = 16;

            var host = EditorUI.Icon(bg.transform, "HostIcon", EditorUI.Crown, UIPalette.Gold, SkinSlot.IconCrown, 32);
            host.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-50, 0), new Vector2(32, 32), new Vector2(1, 0.5f));
            var ready = EditorUI.Icon(bg.transform, "ReadyIcon", EditorUI.Check, UIPalette.Green, SkinSlot.IconCheck, 34);
            ready.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(34, 34), new Vector2(1, 0.5f));

            var view = bg.gameObject.AddComponent<PlayerRowView>();
            view.background = bg;
            view.colorSwatch = swatch;
            view.nameLabel = name;
            view.statusLabel = status;
            view.rankLabel = rank;
            view.readyIcon = ready.gameObject;
            view.hostIcon = host.gameObject;
            view.streamerPortrait = streamerArt;
            view.streamerPlaceholder = streamerPlaceholder;
            view.streamerInitials = streamerInitials;
            return bg.gameObject;
        }

        static GameObject BuildTextLine()
        {
            var text = EditorUI.Text(null, "TextLine", "Line", 20, UIPalette.Text, TextAlignmentOptions.TopLeft);
            text.rectTransform.sizeDelta = new Vector2(380, 28);
            text.lineSpacing = -6f;
            return text.gameObject;
        }
    }
}
