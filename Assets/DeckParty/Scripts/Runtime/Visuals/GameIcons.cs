using System;
using System.Collections.Generic;
using DeckParty.Core;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

namespace DeckParty
{
    /// <summary>The key resources. Shown as icons everywhere; hovering one names it.</summary>
    public enum ResourceIcon
    {
        /// <summary>Subs a viewer brings in, banked as Gifted Subs.</summary>
        Subs,
        Cash,
        /// <summary>Deplatform Risk.</summary>
        Risk,
        /// <summary>Plays (were Diamonds): the viewers that win the game, four in one stream.</summary>
        Plays,
    }

    /// <summary>
    /// Resource and community icons, as Image sprites and as inline TextMeshPro sprites.
    /// <para>
    /// Strings write icons as tokens: <c>{icon:subs}</c>, <c>{icon:cash}</c>, <c>{icon:risk}</c> or <c>{icon:CoreFanbase}</c> (any
    /// <see cref="Community"/> name). <see cref="Loc"/> expands them into <c>&lt;sprite="dp_icons" name="cash"&gt;</c>, and the
    /// sprite asset behind that tag is built here at runtime from your art: resources from the UI Skin (IconPop, IconCash,
    /// IconTrouble), communities from the Game Database (Stream Art > Community Icons). Until art is assigned, tinted
    /// placeholders stand in.
    /// </para>
    /// </summary>
    public static class GameIcons
    {
        /// <summary>The sprite asset name used in tags (resolved through <see cref="TMP_Text.OnSpriteAssetRequest"/>).</summary>
        public const string AssetName = "dp_icons";

        const string SubsId = "subs";
        const string CashId = "cash";
        const string RiskId = "risk";
        const string PlaysId = "plays";

        static TMP_SpriteAsset s_Asset;
        static bool s_Hooked;
        static Sprite[] s_CommunityPlaceholders;

        public static string Id(ResourceIcon icon) =>
            icon == ResourceIcon.Cash ? CashId : icon == ResourceIcon.Risk ? RiskId : icon == ResourceIcon.Plays ? PlaysId : SubsId;

        public static string Id(Community community) => community.ToString();

        // ------------------------------------------------------------------ sprites and colours

        public static Sprite Sprite(ResourceIcon icon)
        {
            var skin = GameDatabase.Instance.skin;
            var sprite = skin != null ? skin.GetSprite(SlotOf(icon)) : null;
            return sprite != null ? sprite : RuntimeSprites.Circle;
        }

        public static Sprite Sprite(Community community)
        {
            var sprite = GameDatabase.Instance.GetCommunityIcon(community);
            return sprite != null ? sprite : CommunityPlaceholder(community);
        }

        /// <summary>
        /// The skin slot's colour when Override Color is ticked (as for any coloured art, so it also wins over the placeholder
        /// name check below); otherwise the resource colour while a white placeholder shape stands in, and white for your art.
        /// </summary>
        public static Color Tint(ResourceIcon icon)
        {
            var entry = GameDatabase.Instance.skin != null ? GameDatabase.Instance.skin.Get(SlotOf(icon)) : null;
            if (entry != null && entry.sprite != null && entry.overrideColor) return entry.color;
            return IsPlaceholder(Sprite(icon)) ? ResourceColor(icon) : Color.white;
        }

        public static Color ResourceColor(ResourceIcon icon) =>
            icon == ResourceIcon.Cash ? UIPalette.Green : icon == ResourceIcon.Risk ? UIPalette.Red : UIPalette.Gold;

        /// <summary>True when the UI Skin slot holds your own art (not the generated white placeholder shape).</summary>
        public static bool HasArt(ResourceIcon icon) => !IsPlaceholder(Sprite(icon));

        /// <summary>
        /// The colour for an icon Image showing any UI Skin slot: the slot's colour when Override Color is ticked, white for your
        /// own art (so its colours show), otherwise <paramref name="placeholderTint"/> for the generated white shape.
        /// </summary>
        public static Color SlotTint(SkinSlot slot, Color placeholderTint)
        {
            var skin = GameDatabase.Instance.skin;
            var entry = skin != null ? skin.Get(slot) : null;
            if (entry == null || entry.sprite == null) return placeholderTint;
            if (entry.overrideColor) return entry.color;
            return IsGeneratedShape(entry.sprite) ? placeholderTint : Color.white;
        }

        /// <summary>The generated placeholder PNGs (Art/Placeholders): single sprites named icon_* or ui_* after their file.</summary>
        static bool IsGeneratedShape(Sprite sprite)
        {
            string name = sprite.name;
            return sprite.texture != null && sprite.texture.name == name && (name.StartsWith("icon_") || name.StartsWith("ui_"));
        }

        /// <summary>
        /// The colour for an icon Image that shows a resource as on/off (HUD pips): your art keeps its colours (dimmed when off),
        /// a placeholder shape takes <paramref name="onColor"/> or <paramref name="offColor"/>.
        /// </summary>
        public static Color PipColor(ResourceIcon icon, bool on, Color onColor, Color offColor)
        {
            if (!HasArt(icon)) return on ? onColor : offColor;
            var tint = Tint(icon);
            return on ? tint : new Color(tint.r * 0.6f, tint.g * 0.6f, tint.b * 0.6f, 0.28f);
        }

        public static Color CommunityColor(Community community)
        {
            switch (community)
            {
                case Community.CoreFanbase: return UIPalette.Pink;
                case Community.Staff: return UIPalette.Blue;
                case Community.Content: return UIPalette.Purple;
                case Community.Techspertise: return UIPalette.Teal;
                case Community.FinanceBros: return UIPalette.Green;
                case Community.Trolls: return UIPalette.Orange;
                case Community.Boomer: return new Color(0.80f, 0.64f, 0.40f);
                case Community.Millennial: return new Color(0.98f, 0.66f, 0.76f);
                case Community.Zoomer: return new Color(0.80f, 0.96f, 0.22f);
                case Community.ArmchairActivists: return new Color(0.62f, 0.62f, 0.76f);
                case Community.Clout: return new Color(1.00f, 0.88f, 0.35f);
                case Community.MemeticsDivision: return new Color(1.00f, 0.45f, 0.95f);
                case Community.Bots: return new Color(0.66f, 0.78f, 0.88f);
                case Community.GenZ: return new Color(0.80f, 0.96f, 0.22f);
                case Community.Gatekeepers: return new Color(0.92f, 0.44f, 0.30f);
                case Community.HypeMen: return new Color(1.00f, 0.80f, 0.25f);
                case Community.Mods: return UIPalette.Teal;
                case Community.FullStream: return new Color(0.36f, 0.74f, 1.00f);
                case Community.Marketers: return UIPalette.Green;
                case Community.Saboteurs: return new Color(0.78f, 0.20f, 0.32f);
                case Community.ShopControl: return new Color(0.56f, 0.46f, 0.96f);
                case Community.Voyeurs: return new Color(0.95f, 0.55f, 0.85f);
                default: return UIPalette.Red;
            }
        }

        static SkinSlot SlotOf(ResourceIcon icon) =>
            icon == ResourceIcon.Cash ? SkinSlot.IconCash : icon == ResourceIcon.Risk ? SkinSlot.IconTrouble
            : icon == ResourceIcon.Plays ? SkinSlot.IconStar : SkinSlot.IconPop;

        /// <summary>The white placeholder shapes Build Everything puts in the UI Skin (icon_pop, icon_cash, icon_trouble, icon_star).</summary>
        static bool IsPlaceholder(Sprite sprite)
        {
            if (sprite == null || sprite == RuntimeSprites.Circle) return true;
            string name = sprite.name;
            return name == "icon_pop" || name == "icon_cash" || name == "icon_trouble" || name == "icon_star";
        }

        // ------------------------------------------------------------------ text

        /// <summary>The inline sprite tag for a resource.</summary>
        public static string Tag(ResourceIcon icon) => Tag(Id(icon), Tint(icon));

        public static string Tag(Community community) => Tag(Id(community), Color.white);

        /// <summary>A resource amount for prices and wallets: the icon, then the number ("[icon] 5").</summary>
        public static string Amount(ResourceIcon icon, int value) => Tag(icon) + " " + value;

        /// <summary>Expands an icon token's name ("cash", "subs", "risk" or a community name) into its tag, or null if unknown.</summary>
        public static string TagFor(string id)
        {
            switch (id)
            {
                case SubsId:
                case "gifted_subs": return Tag(ResourceIcon.Subs);
                case CashId: return Tag(ResourceIcon.Cash);
                case RiskId: return Tag(ResourceIcon.Risk);
                case PlaysId:
                case "play":
                case "diamond":
                case "diamonds": return Tag(ResourceIcon.Plays);
            }
            return Enum.TryParse(id, true, out Community community) && Enum.IsDefined(typeof(Community), community) ? Tag(community) : null;
        }

        static string Tag(string id, Color tint)
        {
            Hook();
            string color = tint == Color.white ? string.Empty : " color=" + UIPalette.Tag(tint);
            return $"<sprite=\"{AssetName}\" name=\"{id}\"{color}>";
        }

        /// <summary>What hovering an icon says (resource name and use, or the community and how it scales), or null.</summary>
        public static string Tooltip(string id, PlayerState state = null)
        {
            switch (id)
            {
                case SubsId: return Loc.Get("hud.tip.gifted_subs");
                case CashId: return Loc.Get("hud.tip.cash");
                case RiskId: return Loc.Get("hud.tip.risk_term", "max", GameRules.RaidTroubleThreshold);
                case PlaysId: return Loc.Get("hud.tip.plays_term", "goal", GameRules.DiamondsToWin);
            }
            return Enum.TryParse(id, false, out Community community) && Enum.IsDefined(typeof(Community), community)
                ? CommunityVisuals.Tooltip(community, state)
                : null;
        }

        // ------------------------------------------------------------------ the TextMeshPro sprite asset

        static void Hook()
        {
            if (s_Hooked) return;
            s_Hooked = true;
            TMP_Text.OnSpriteAssetRequest += OnSpriteAssetRequest;
        }

        static TMP_SpriteAsset OnSpriteAssetRequest(int hashCode, string assetName) => assetName == AssetName ? Asset : null;

        /// <summary>Built once per session from the current art (TextMeshPro caches it by name).</summary>
        static TMP_SpriteAsset Asset
        {
            get
            {
                if (s_Asset == null)
                {
                    try { s_Asset = BuildAsset(); }
                    catch (Exception e) { Debug.LogException(e); }
                }
                return s_Asset;
            }
        }

        static TMP_SpriteAsset BuildAsset()
        {
            var entries = new List<(string id, Sprite sprite)>
            {
                (SubsId, Sprite(ResourceIcon.Subs)),
                (CashId, Sprite(ResourceIcon.Cash)),
                (RiskId, Sprite(ResourceIcon.Risk)),
                (PlaysId, Sprite(ResourceIcon.Plays)),
            };
            foreach (var community in Communities.Every) entries.Add((Id(community), Sprite(community)));

            // One sprite asset per texture (sheets and placeholders can differ); the first holds the rest as fallbacks.
            var byTexture = new Dictionary<Texture2D, List<(string id, Sprite sprite)>>();
            var order = new List<Texture2D>();
            foreach (var entry in entries)
            {
                if (entry.sprite == null || entry.sprite.texture == null) continue;
                var texture = entry.sprite.texture;
                if (!byTexture.TryGetValue(texture, out var list))
                {
                    byTexture[texture] = list = new List<(string, Sprite)>();
                    order.Add(texture);
                }
                list.Add(entry);
            }
            if (order.Count == 0) return null;

            TMP_SpriteAsset primary = null;
            foreach (var texture in order)
            {
                var asset = CreateAsset(texture, byTexture[texture]);
                if (primary == null) primary = asset;
                else primary.fallbackSpriteAssets.Add(asset);
            }
            return primary;
        }

        static TMP_SpriteAsset CreateAsset(Texture2D texture, List<(string id, Sprite sprite)> sprites)
        {
            var asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = AssetName;
            asset.hideFlags = HideFlags.DontSave;
            // A versioned asset with no legacy sprite list, so TextMeshPro never tries to "upgrade" (and clear) it.
            JsonUtility.FromJsonOverwrite("{\"m_Version\":\"1.1.0\"}", asset);
            asset.spriteInfoList = new List<TMP_Sprite>();
            asset.fallbackSpriteAssets = new List<TMP_SpriteAsset>();
            asset.spriteSheet = texture;

            uint index = 0;
            foreach (var (id, sprite) in sprites)
            {
                Rect rect;
                // Tightly packed atlas sprites have no texture rectangle; their sprite rect is the next best thing.
                try { rect = sprite.textureRect; }
                catch (Exception) { rect = sprite.rect; }
                // Scaled to the font's ascent by TextMeshPro; sit slightly below the baseline and a touch larger than letters.
                var metrics = new GlyphMetrics(rect.width, rect.height, 0f, rect.height * 0.84f, rect.width * 1.06f);
                var glyph = new TMP_SpriteGlyph(index, metrics, new GlyphRect(Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y),
                    Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height)), 1.12f, 0, sprite);
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = id });
                index++;
            }

            // Lookups are built before the material is set: TextMeshPro only "upgrades" (clears) unversioned assets that
            // have a material when it rebuilds them, and with the lookups built it never needs to again.
            asset.UpdateLookupTables();
            asset.material = new Material(SpriteShader()) { name = AssetName, hideFlags = HideFlags.DontSave, mainTexture = texture };
            return asset;
        }

        /// <summary>TextMeshPro's sprite shader, taken from its default sprite asset so it's always in the build.</summary>
        static Shader SpriteShader()
        {
            Shader shader = null;
            try
            {
                var fallback = TMP_Settings.defaultSpriteAsset;
                if (fallback != null && fallback.material != null) shader = fallback.material.shader;
            }
            catch (Exception)
            {
                // TMP Settings missing: fall through to the lookup.
            }
            if (shader == null) shader = Shader.Find("TextMeshPro/Sprite");
            if (shader == null) shader = Shader.Find("UI/Default");
            return shader;
        }

        // ------------------------------------------------------------------ community placeholders

        /// <summary>A coloured badge with a symbol per community, all on one small texture, until you assign icons.</summary>
        static Sprite CommunityPlaceholder(Community community)
        {
            if (s_CommunityPlaceholders == null || s_CommunityPlaceholders[0] == null) s_CommunityPlaceholders = BuildCommunityPlaceholders();
            return s_CommunityPlaceholders[(int)community];
        }

        static Sprite[] BuildCommunityPlaceholders()
        {
            const int cell = 64;
            int count = Communities.Count;
            var texture = new Texture2D(cell * count, cell, TextureFormat.RGBA32, false)
            {
                name = "dp_community_placeholders",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[cell * count * cell];
            for (int c = 0; c < count; c++)
            {
                var community = (Community)c;
                Color badge = CommunityColor(community);
                Color ring = Color.Lerp(badge, Color.black, 0.35f);
                for (int py = 0; py < cell; py++)
                {
                    for (int px = 0; px < cell; px++)
                    {
                        // 2x2 supersampling for soft edges.
                        Color sum = Color.clear;
                        for (int sy = 0; sy < 2; sy++)
                        {
                            for (int sx = 0; sx < 2; sx++)
                            {
                                float x = (px + 0.25f + sx * 0.5f) / cell * 2f - 1f;
                                float y = (py + 0.25f + sy * 0.5f) / cell * 2f - 1f;
                                float r = Mathf.Sqrt(x * x + y * y);
                                Color sample = Color.clear;
                                if (r <= 0.96f) sample = r >= 0.84f ? ring : badge;
                                if (r < 0.84f && Symbol(community, x / 0.78f, y / 0.78f)) sample = Color.white;
                                sum += sample;
                            }
                        }
                        pixels[py * cell * count + c * cell + px] = sum * 0.25f;
                    }
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            var sprites = new Sprite[count];
            for (int c = 0; c < count; c++)
            {
                sprites[c] = UnityEngine.Sprite.Create(texture, new Rect(c * cell, 0, cell, cell), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                sprites[c].name = "dp_community_" + (Community)c;
                sprites[c].hideFlags = HideFlags.DontSave;
            }
            return sprites;
        }

        /// <summary>The white symbol on each placeholder badge, in a -1..1 square (y up).</summary>
        static bool Symbol(Community community, float x, float y)
        {
            switch (community)
            {
                case Community.CoreFanbase:
                {
                    // Heart.
                    float hx = x * 1.15f, hy = y * 1.15f + 0.25f;
                    float a = hx * hx + hy * hy - 1f;
                    return a * a * a - hx * hx * hy * hy * hy <= 0f;
                }
                case Community.Staff:
                {
                    // Briefcase: body and handle.
                    bool body = Mathf.Abs(x) < 0.62f && y > -0.48f && y < 0.22f;
                    bool handle = Mathf.Abs(x) < 0.26f && y >= 0.22f && y < 0.46f && !(Mathf.Abs(x) < 0.14f && y < 0.36f);
                    return body || handle;
                }
                case Community.Content:
                    // Play button.
                    return InTriangle(x, y, -0.32f, 0.5f, -0.32f, -0.5f, 0.52f, 0f);
                case Community.Techspertise:
                {
                    // Gear with a hole.
                    float r = Mathf.Sqrt(x * x + y * y);
                    float angle = Mathf.Atan2(y, x);
                    float outer = Mathf.Cos(angle * 8f) > 0.1f ? 0.62f : 0.48f;
                    return r < outer && r > 0.2f;
                }
                case Community.FinanceBros:
                {
                    // Arrow up.
                    bool shaft = Mathf.Abs(x) < 0.14f && y > -0.55f && y < 0.12f;
                    bool head = y >= 0.05f && y < 0.6f && Mathf.Abs(x) < (0.6f - y) * 1.05f;
                    return shaft || head;
                }
                case Community.Trolls:
                    // Lightning bolt.
                    return InTriangle(x, y, 0.22f, 0.62f, -0.36f, -0.04f, 0.1f, -0.04f) ||
                           InTriangle(x, y, -0.1f, 0.06f, 0.36f, 0.06f, -0.22f, -0.62f);
                case Community.Boomer:
                {
                    // Newspaper: a page with three lines of print.
                    bool page = Mathf.Abs(x) < 0.55f && Mathf.Abs(y) < 0.5f;
                    bool line = Mathf.Abs(x) < 0.4f && (Mathf.Abs(y - 0.24f) < 0.06f || Mathf.Abs(y) < 0.06f || Mathf.Abs(y + 0.24f) < 0.06f);
                    return page && !line;
                }
                case Community.Millennial:
                {
                    // Avocado: an egg shape with a round stone.
                    float ax = x / 0.48f, ay = (y + 0.08f) / (y > -0.08f ? 0.66f : 0.5f);
                    bool fruit = ax * ax + ay * ay < 1f;
                    float sx = x, sy = y + 0.18f;
                    bool stone = sx * sx + sy * sy < 0.2f * 0.2f;
                    return fruit && !stone;
                }
                case Community.GenZ:
                case Community.Zoomer:
                {
                    // Phone: a tall rounded slab with a dark screen.
                    bool body = Mathf.Abs(x) < 0.34f && Mathf.Abs(y) < 0.6f;
                    bool screen = Mathf.Abs(x) < 0.24f && y > -0.42f && y < 0.48f;
                    return body && !screen;
                }
                case Community.ArmchairActivists:
                {
                    // Armchair: back, seat, arms and feet.
                    bool back = Mathf.Abs(x) < 0.4f && y > -0.05f && y < 0.5f;
                    bool seat = Mathf.Abs(x) < 0.4f && y > -0.3f && y <= -0.05f;
                    bool arm = Mathf.Abs(x) >= 0.4f && Mathf.Abs(x) < 0.62f && y > -0.3f && y < 0.18f;
                    bool foot = Mathf.Abs(Mathf.Abs(x) - 0.48f) < 0.07f && y > -0.5f && y <= -0.3f;
                    bool gap = Mathf.Abs(x) < 0.4f && Mathf.Abs(y + 0.07f) < 0.03f;
                    return (back || seat || arm || foot) && !gap;
                }
                case Community.Clout:
                {
                    // Five-pointed star.
                    float r = Mathf.Sqrt(x * x + y * y);
                    const float sector = Mathf.PI * 2f / 5f;
                    float phase = Mathf.Repeat(Mathf.Atan2(x, y), sector) / sector;
                    float tip = Mathf.Abs(phase - 0.5f) * 2f;
                    return r < Mathf.Lerp(0.27f, 0.66f, tip * tip);
                }
                case Community.MemeticsDivision:
                {
                    // Repost loop: a ring with a gap and an arrowhead (the stream scores again).
                    float r = Mathf.Sqrt(x * x + y * y);
                    float angle = Mathf.Atan2(y, x);
                    bool ring = r > 0.3f && r < 0.52f && !(angle > 0.15f && angle < 1.05f);
                    bool head = InTriangle(x, y, 0.18f, 0.3f, 0.68f, 0.3f, 0.43f, 0.02f);
                    return ring || head;
                }
                case Community.Bots:
                {
                    // Robot head: square face, two eyes, a mouth slot and an antenna.
                    bool face = Mathf.Abs(x) < 0.48f && y > -0.42f && y < 0.28f;
                    float ex = Mathf.Abs(x) - 0.2f, ey = y + 0.02f;
                    bool eye = ex * ex + ey * ey < 0.1f * 0.1f;
                    bool mouth = Mathf.Abs(x) < 0.22f && Mathf.Abs(y + 0.26f) < 0.045f;
                    bool antenna = Mathf.Abs(x) < 0.05f && y >= 0.28f && y < 0.46f;
                    float by = y - 0.52f;
                    bool ball = x * x + by * by < 0.09f * 0.09f;
                    return (face && !eye && !mouth) || antenna || ball;
                }
                case Community.Gatekeepers:
                {
                    // Padlock: a body with a keyhole and a shackle.
                    bool body = Mathf.Abs(x) < 0.46f && y > -0.55f && y < 0.08f;
                    bool hole = (x * x + (y + 0.16f) * (y + 0.16f) < 0.09f * 0.09f) || (Mathf.Abs(x) < 0.04f && y > -0.38f && y < -0.16f);
                    float sy = y - 0.08f;
                    float r = Mathf.Sqrt(x * x + sy * sy);
                    bool shackle = sy >= 0f && r > 0.2f && r < 0.33f;
                    return (body && !hole) || shackle;
                }
                case Community.HypeMen:
                    // Megaphone: a cone and a handle.
                    return InTriangle(x, y, -0.3f, 0.12f, 0.55f, 0.55f, 0.55f, -0.55f) && x < 0.55f
                        || (Mathf.Abs(y) < 0.16f && x > -0.55f && x <= -0.3f)
                        || (Mathf.Abs(x + 0.12f) < 0.07f && y < -0.1f && y > -0.5f);
                case Community.Mods:
                {
                    // Shield: flat top, pointed bottom, with a tick cut out.
                    bool top = Mathf.Abs(x) < 0.5f && y > -0.05f && y < 0.55f;
                    bool point = y <= -0.05f && y > -0.62f && Mathf.Abs(x) < 0.5f * (y + 0.62f) / 0.57f;
                    bool tick = Mathf.Abs((y - 0.02f) + (x + 0.12f)) < 0.07f && x > -0.3f && x < -0.1f
                             || Mathf.Abs((y + 0.08f) - (x + 0.08f) * 1.3f) < 0.07f && x >= -0.12f && x < 0.3f;
                    return (top || point) && !tick;
                }
                case Community.FullStream:
                    // Signal bars, all full.
                    return (y > -0.5f && y < -0.1f && Mathf.Abs(x + 0.4f) < 0.11f)
                        || (y > -0.5f && y < 0.15f && Mathf.Abs(x) < 0.11f)
                        || (y > -0.5f && y < 0.5f && Mathf.Abs(x - 0.4f) < 0.11f);
                case Community.Marketers:
                {
                    // Target: a ring and a bullseye.
                    float r = Mathf.Sqrt(x * x + y * y);
                    return (r > 0.38f && r < 0.56f) || r < 0.2f;
                }
                case Community.Saboteurs:
                {
                    // Bomb: a round body and a fuse.
                    float bx = x + 0.08f, by = y + 0.1f;
                    bool body = bx * bx + by * by < 0.42f * 0.42f;
                    bool fuse = Mathf.Abs((y - 0.32f) - (x - 0.2f)) < 0.07f && x > 0.12f && x < 0.42f;
                    float sx = x - 0.48f, sy = y - 0.52f;
                    bool spark = sx * sx + sy * sy < 0.1f * 0.1f;
                    return body || fuse || spark;
                }
                case Community.ShopControl:
                {
                    // Shopping bag: a body with a handle.
                    bool bag = Mathf.Abs(x) < 0.46f && y > -0.55f && y < 0.18f;
                    float hy = y - 0.18f;
                    float r = Mathf.Sqrt(x * x + hy * hy);
                    bool handle = hy >= 0f && r > 0.18f && r < 0.3f;
                    return bag || handle;
                }
                case Community.Voyeurs:
                {
                    // Eye: an almond with a pupil.
                    float ex = x / 0.66f, ey = y / 0.36f;
                    bool almond = Mathf.Abs(ey) < 1f - ex * ex && Mathf.Abs(ex) < 1f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    bool iris = r < 0.24f;
                    bool pupil = r < 0.1f;
                    return (almond && !iris) || pupil;
                }
                default:
                {
                    // Skull: a round head and jaw with two eyes.
                    float hx = x, hy = y - 0.1f;
                    bool head = hx * hx + hy * hy < 0.5f * 0.5f;
                    bool jaw = Mathf.Abs(x) < 0.28f && y > -0.55f && y <= -0.2f;
                    float ex = Mathf.Abs(x) - 0.19f, ey = y - 0.08f;
                    bool eye = ex * ex + ey * ey < 0.13f * 0.13f;
                    bool gap = Mathf.Abs(x) < 0.03f && y < -0.3f;
                    return (head || jaw) && !eye && !gap;
                }
            }
        }

        static bool InTriangle(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
            float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
            float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
            bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }
    }
}
