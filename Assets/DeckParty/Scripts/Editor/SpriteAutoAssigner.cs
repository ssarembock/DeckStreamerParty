using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DeckParty.Core;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Hooks art up to the game:
    ///  - by name: guests ("Doge", "guest_this_is_fine_dog", "tungtung"), streamers ("streamer_bro_jogan", or just
    ///    "brojogan" inside Art/Streamers), venues ("bg_bedroom", "bg_bedroom_fg", "bg_bedroom_thumb", "bg_stage_fate",
    ///    or just "bedroom" inside Art/Backgrounds) and UI skin slots ("ui_panel", "icon_star");
    ///  - by order: assign a character sheet's sprites to the guests in roster order.
    /// Runs automatically whenever images are imported into Assets/DeckParty/Art (see <see cref="DeckPartyArtImporter"/>).
    /// </summary>
    public static class SpriteAutoAssigner
    {
        static readonly string[] s_GuestPrefixes = { "guest", "character", "char", "portrait", "dp" };
        static readonly string[] s_StreamerPrefixes = { "streamer" };
        static readonly string[] s_VenuePrefixes = { "background", "bg", "venue" };
        static readonly string[] s_SkinPrefixes = { "ui", "skin" };

        /// <summary>Spellings of the streamer names that don't normalise to the enum name.</summary>
        static readonly (string alias, StreamerType type)[] s_StreamerAliases =
        {
            ("ethicalfkboi", StreamerType.EthicalFboi), ("ethicalfuckboi", StreamerType.EthicalFboi), ("ethicalfboy", StreamerType.EthicalFboi),
            ("softhardbody", StreamerType.SoftHardBodyMan), ("abomination", StreamerType.StrategicAbomination),
            ("manim", StreamerType.ManimMakethMan), ("radiologist", StreamerType.RudeRadiologist), ("jogan", StreamerType.BroJogan),
        };

        /// <summary>Short names people are likely to use for files, on top of the full names and enum names.</summary>
        static readonly (string alias, GuestType type)[] s_Aliases =
        {
            ("troll", GuestType.IneffectiveTroll), ("botfarm", GuestType.IneffectiveTroll), ("producer", GuestType.Producer),
            ("admin", GuestType.ChatAdmin), ("mod", GuestType.ChatAdmin), ("moderator", GuestType.ChatAdmin), ("automod", GuestType.Normie),
            ("collab", GuestType.MildlyRiskyCollaborator), ("collaborator", GuestType.MildlyRiskyCollaborator), ("juniorpr", GuestType.JuniorPrManager),
            ("hodl", GuestType.JuniorPrManager), ("diehard", GuestType.DieHardFan), ("vip", GuestType.DieHardFan), ("marketing", GuestType.MarketingManager),
            ("parasocial", GuestType.MarketingManager), ("thisisfine", GuestType.ThisIsFineDog),
            ("finedog", GuestType.ThisIsFineDog), ("grumpy", GuestType.GrumpyCat), ("nyan", GuestType.NyanCat),
            ("stonks", GuestType.StonksGuy), ("chad", GuestType.ChadVsVirgin), ("npc", GuestType.NpcWojak),
            ("keyboard", GuestType.KeyboardCat), ("bongo", GuestType.BongoCat), ("popcat", GuestType.PopCat),
            ("chungus", GuestType.BigChungus), ("knuckles", GuestType.UgandanKnuckles), ("oag", GuestType.OverlyAttachedGirlfriend),
            ("attachedgirlfriend", GuestType.OverlyAttachedGirlfriend), ("disaster", GuestType.DisasterGirl),
            ("harold", GuestType.HideThePainHarold), ("success", GuestType.SuccessKid), ("brian", GuestType.BadLuckBrian),
            ("tungtung", GuestType.TungTungTungSahur), ("sahur", GuestType.TungTungTungSahur), ("tralalero", GuestType.TralaleroTralala),
            ("bombardiro", GuestType.BombardiroCrocodilo), ("ballerina", GuestType.BallerinaCappuccina), ("skibidi", GuestType.SkibidiToilet),
            ("sigma", GuestType.SigmaMale), ("distracted", GuestType.DistractedBoyfriend), ("drake", GuestType.Drakeposting),
            ("mathlady", GuestType.ConfusedMathLady), ("chloe", GuestType.SideEyeChloe), ("sideeye", GuestType.SideEyeChloe),
            ("crying", GuestType.DistraughtCat), ("distraught", GuestType.DistraughtCat), ("doge", GuestType.DegenDog), ("degen", GuestType.DegenDog), ("baby", GuestType.DancingBaby), ("boomer", GuestType.BigBoomer),
            ("clippo", GuestType.ClippoTheClanker), ("clanker", GuestType.ClippoTheClanker), ("biggerstar", GuestType.BiggerStar),
            ("youth", GuestType.PrecociousYouth), ("starpower", GuestType.InternetStarpower), ("owner", GuestType.PlatformOwner),
            ("celebrity", GuestType.MainstreamCelebrity), ("seniormarketing", GuestType.SeniorMarketingManager), ("surprise", GuestType.Evangelist), ("evangelist", GuestType.Evangelist),
            ("influential", GuestType.InfluentialTroll), ("cybersecurity", GuestType.CybersecurityEngineer), ("cyber", GuestType.CybersecurityEngineer),
            ("sponsorship", GuestType.PaidSponsorship), ("comedian", GuestType.PromoCodeUser), ("podcast", GuestType.PromoCodeUser), ("promocode", GuestType.PromoCodeUser),
            ("accountmanager", GuestType.AccountManager), ("crypto", GuestType.CryptoEnthusiast), ("donator", GuestType.GenerousDonator),
            ("prmanager", GuestType.PrManager), ("highlyrisky", GuestType.HighlyRiskyCollaborator), ("arbitrator", GuestType.ChatArbitrator),
            ("botdetector", GuestType.BotDetector), ("orbiter", GuestType.RiskyOrbiter), ("smm", GuestType.SocialMediaManager),
            ("socialmedia", GuestType.SocialMediaManager), ("billionaire", GuestType.BillionaireBenefactor), ("techadmin", GuestType.TechAdmin),
            ("scandal", GuestType.OnlineScandal), ("gatekeeper", GuestType.Gatekeeper), ("bunny", GuestType.BingeBunny),
            ("rabbit", GuestType.BingeBunny), ("hatewatcher", GuestType.HateWatcher), ("affiliate", GuestType.Affiliate),
            ("currency", GuestType.MasterOfCurrency), ("executivepr", GuestType.ExecutivePr), ("genz", GuestType.GenZWithJob),
        };

        [MenuItem("Deck Party/Art/Auto-Assign Sprites by Name", priority = 0)]
        public static void AssignByNameMenu() => AssignByName(false);

        /// <summary>Matches every sprite under Assets/DeckParty/Art by name. Returns how many were assigned.</summary>
        public static int AssignByName(bool quiet)
        {
            if (AssetDatabase.LoadAssetAtPath<GameDatabase>(DeckPartyPaths.Database) == null)
            {
                if (!quiet) EditorUtility.DisplayDialog("Deck Party", "Run Deck Party > Setup > Build Everything first.", "OK");
                return 0;
            }
            // Brings the roster and venue ids up to date first (no-op when they already are).
            var db = DeckPartyAssets.EnsureDataAssets();

            var guestKeys = new Dictionary<string, GuestType>();
            foreach (var info in GuestCatalog.All)
            {
                guestKeys[Normalize(info.Type.ToString())] = info.Type;
                guestKeys[Normalize(info.Name)] = info.Type;
                var art = db.GetGuestArt(info.Type);
                if (art != null && !string.IsNullOrWhiteSpace(art.displayNameOverride)) guestKeys[Normalize(art.displayNameOverride)] = info.Type;
            }
            foreach (var (alias, type) in s_Aliases)
                if (!guestKeys.ContainsKey(alias)) guestKeys[alias] = type;

            int guests = 0, streamers = 0, venues = 0, skins = 0;
            var unmatched = new List<string>();

            foreach (var (sprite, path) in FindSprites(DeckPartyPaths.Art))
            {
                string key = Normalize(StripIndex(sprite.name));
                bool inBackgrounds = path.StartsWith(DeckPartyPaths.ArtBackgrounds + "/", StringComparison.OrdinalIgnoreCase);
                bool inStreamers = path.StartsWith(DeckPartyPaths.ArtStreamers + "/", StringComparison.OrdinalIgnoreCase);

                if (TryAssignSkin(db, key, sprite)) { skins++; continue; }
                if (TryAssignVenue(db, key, sprite, inBackgrounds)) { venues++; continue; }
                if (TryAssignStreamer(db, key, sprite, inStreamers)) { streamers++; continue; }

                string guestKey = StripPrefix(key, s_GuestPrefixes);
                if (guestKeys.TryGetValue(guestKey, out var guestType) || guestKeys.TryGetValue(key, out guestType))
                {
                    var art = db.GetGuestArt(guestType);
                    if (art != null)
                    {
                        if (art.portrait != sprite) guests++;
                        art.portrait = sprite;
                        continue;
                    }
                }
                unmatched.Add(sprite.name);
            }

            EditorUtility.SetDirty(db);
            if (db.skin != null) EditorUtility.SetDirty(db.skin);
            foreach (var venue in db.venues) if (venue != null) EditorUtility.SetDirty(venue);
            AssetDatabase.SaveAssets();
            GameDatabase.ResetCache();

            var report = new StringBuilder();
            report.AppendLine($"Assigned {guests} guest portrait(s), {streamers} streamer portrait(s), {venues} background image(s) and {skins} UI skin sprite(s).");
            if (unmatched.Count > 0)
            {
                report.AppendLine();
                report.AppendLine($"{unmatched.Count} sprite(s) didn't match a name (see the Console for the full list):");
                report.AppendLine(string.Join(", ", unmatched.Take(12)) + (unmatched.Count > 12 ? ", ..." : string.Empty));
                Debug.Log("[DeckParty] Unmatched sprites (rename them to a guest or venue name, see V1Instructions.md): " + string.Join(", ", unmatched));
            }

            if (quiet) Debug.Log("[DeckParty] Art auto-assign: " + report.ToString().Split('\n')[0].Trim());
            else EditorUtility.DisplayDialog("Deck Party: sprites assigned", report.ToString(), "OK");
            return guests + streamers + venues + skins;
        }

        [MenuItem("Deck Party/Art/Assign Selected Sheet to Guests in Roster Order", priority = 1)]
        public static void AssignSheetInOrder()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DeckPartyPaths.Database);
            var texture = Selection.activeObject as Texture2D;
            if (db == null || texture == null)
            {
                EditorUtility.DisplayDialog("Deck Party", "Select a sliced character sprite sheet (the Texture asset) in the Project window first.", "OK");
                return;
            }

            var path = AssetDatabase.GetAssetPath(texture);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToList();
            if (sprites.Count == 0)
            {
                EditorUtility.DisplayDialog("Deck Party", "That texture has no sprites. Set Sprite Mode to Multiple and slice it in the Sprite Editor.", "OK");
                return;
            }

            // Reading order: top-to-bottom rows, then left-to-right.
            sprites.Sort((a, b) =>
            {
                float rowA = Mathf.Round(a.rect.yMax / Mathf.Max(1f, a.rect.height));
                float rowB = Mathf.Round(b.rect.yMax / Mathf.Max(1f, b.rect.height));
                if (!Mathf.Approximately(rowA, rowB)) return rowB.CompareTo(rowA);
                return a.rect.x.CompareTo(b.rect.x);
            });

            db.EnsureAllGuests();
            // The current roster only (put-aside viewers are skipped), in id order; Deck Party > Art > Log Expected File Names lists it.
            var roster = GuestCatalog.Roster;
            int count = Mathf.Min(sprites.Count, roster.Count);
            if (count == 0) return;
            if (!EditorUtility.DisplayDialog("Assign portraits in roster order?",
                    $"{sprites.Count} sprites found. The first {count} will become the portraits of viewers #1 ({roster[0].Name}) to #{count} ({roster[count - 1].Name}), " +
                    "reading the sheet left-to-right, top-to-bottom.\n\nDeck Party > Art > Log Expected File Names prints the roster order.", "Assign", "Cancel"))
                return;

            for (int i = 0; i < count; i++)
            {
                var art = db.GetGuestArt(roster[i].Type);
                if (art != null) art.portrait = sprites[i];
            }
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            GameDatabase.ResetCache();
            Debug.Log($"[DeckParty] Assigned {count} portraits from {texture.name}.");
        }

        [MenuItem("Deck Party/Art/Clear Guest Portraits", priority = 20)]
        public static void ClearPortraits()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DeckPartyPaths.Database);
            if (db == null || !EditorUtility.DisplayDialog("Clear portraits?", "Remove every guest portrait from the database (placeholders will show)?", "Clear", "Cancel")) return;
            foreach (var art in db.guests) if (art != null) art.portrait = null;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            GameDatabase.ResetCache();
        }

        [MenuItem("Deck Party/Art/Log Expected File Names", priority = 21)]
        public static void LogExpectedNames()
        {
            var sb = new StringBuilder("[DeckParty] File names the auto-assigner recognises (case, spaces, - and _ don't matter):\n\nGuests (Assets/DeckParty/Art/Characters):\n");
            var roster = GuestCatalog.Roster;
            for (int i = 0; i < roster.Count; i++)
                sb.AppendLine($"  {i + 1,2}. {roster[i].Name}  ->  {FileName(roster[i].Name)}.png");
            sb.AppendLine($"  Bot (the Bots community's token)  ->  {FileName(GuestCatalog.Get(GuestType.Bot).Name)}.png");
            sb.AppendLine("\nStreamers (Assets/DeckParty/Art/Characters or Art/Streamers):");
            foreach (var type in StreamerCatalog.Roster) sb.AppendLine($"  {type}  ->  streamer_{FileName(SplitWords(type.ToString()))}.png");
            sb.AppendLine("\nBackgrounds (Assets/DeckParty/Art/Backgrounds):");
            foreach (var preset in VenuePresets.AllWithStages()) sb.AppendLine($"  {preset.Name}  ->  bg_{preset.Id}.png  (optional: bg_{preset.Id}_fg.png, bg_{preset.Id}_thumb.png)");
            Debug.Log(sb.ToString());
        }

        static string FileName(string name) => Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');

        static string SplitWords(string pascal) => Regex.Replace(pascal, "(?<=[a-z])(?=[A-Z])", " ");

        // ------------------------------------------------------------------ matching

        static bool TryAssignSkin(GameDatabase db, string key, Sprite sprite)
        {
            if (db.skin == null) return false;
            string stripped = StripPrefix(key, s_SkinPrefixes);
            foreach (SkinSlot slot in Enum.GetValues(typeof(SkinSlot)))
            {
                string slotKey = Normalize(slot.ToString());
                if (stripped != slotKey && key != slotKey) continue;
                // Only treat it as a skin sprite when it was clearly named for the UI.
                if (stripped == key && !slotKey.StartsWith("icon")) continue;

                var entry = db.skin.Get(slot);
                if (entry == null) return false;
                entry.sprite = sprite;
                entry.imageType = sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
                return true;
            }
            return false;
        }

        static bool TryAssignVenue(GameDatabase db, string key, Sprite sprite, bool inBackgroundsFolder)
        {
            string stripped = StripPrefix(key, s_VenuePrefixes);
            if (stripped == key && !inBackgroundsFolder) return false;

            foreach (var venue in db.venues)
            {
                if (venue == null) continue;
                string id = Normalize(venue.id);
                string name = Normalize(venue.displayName);
                if (stripped == id || stripped == name) { venue.background = sprite; return true; }
                if (stripped == id + "fg" || stripped == id + "foreground") { venue.foreground = sprite; return true; }
                if (stripped == id + "thumb" || stripped == id + "thumbnail" || stripped == id + "preview") { venue.thumbnail = sprite; return true; }
            }
            return false;
        }

        static bool TryAssignStreamer(GameDatabase db, string key, Sprite sprite, bool inStreamersFolder)
        {
            string stripped = StripPrefix(key, s_StreamerPrefixes);
            if (stripped == key && !inStreamersFolder) return false;

            foreach (var type in StreamerCatalog.Roster)
            {
                bool match = stripped == Normalize(type.ToString());
                foreach (var (alias, aliasType) in s_StreamerAliases)
                    match |= aliasType == type && stripped == alias;
                if (!match) continue;

                var art = db.GetStreamerArt(type);
                if (art == null) return false;
                art.portrait = sprite;
                return true;
            }
            return false;
        }

        static List<(Sprite sprite, string path)> FindSprites(string folder)
        {
            var result = new List<(Sprite, string)>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (path.Contains("/Placeholders/")) continue;
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) result.Add((sprite, path));
            }
            return result;
        }

        static string Normalize(string value) => new string((value ?? string.Empty).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        /// <summary>Removes Unity's automatic slice suffix ("Sheet_12") so the base name can match.</summary>
        static string StripIndex(string name) => Regex.Replace(name ?? string.Empty, @"[_\- ]\d+$", string.Empty);

        static string StripPrefix(string key, string[] prefixes)
        {
            foreach (var prefix in prefixes)
                if (key.StartsWith(prefix) && key.Length > prefix.Length) return key.Substring(prefix.Length);
            return key;
        }
    }
}
