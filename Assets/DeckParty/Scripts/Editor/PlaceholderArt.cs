using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Generates the default UI shapes and icons as real PNG sprites (Assets/DeckParty/Art/Placeholders) so every
    /// scene looks finished out of the box. Replace them through the UI Skin asset with your sprite sheet.
    /// </summary>
    internal static class PlaceholderArt
    {
        public const string Folder = "Assets/DeckParty/Art/Placeholders";

        public const string RoundedName = "ui_rounded";
        public const string RoundedSmallName = "ui_rounded_small";
        public const string FrameName = "ui_frame";
        public const string CircleName = "ui_circle";
        public const string GlowName = "ui_glow";
        public const string StarName = "icon_star";
        public const string CrossName = "icon_trouble";
        public const string ShieldName = "icon_calm";
        public const string SparkleName = "icon_pop";
        public const string CoinName = "icon_cash";
        public const string DoorName = "icon_door";
        public const string CrownName = "icon_host";
        public const string CheckName = "icon_check";
        // V5 website look: icon buttons and the chat avatar ring.
        public const string AddViewerName = "icon_add_viewer";
        public const string EndStreamName = "icon_end_stream";
        public const string AudienceName = "icon_audience";
        public const string MenuName = "icon_menu";
        public const string ActionName = "icon_action";
        public const string GoLiveName = "icon_go_live";
        public const string CloseName = "icon_close";
        public const string NightName = "icon_night";
        /// <summary>A speech bubble with three dots: the Stream Chat title.</summary>
        public const string ChatName = "icon_chat";
        public const string TimerName = "icon_timer";
        public const string MuteName = "icon_mute";
        public const string RemoveViewerName = "icon_remove_viewer";
        public const string RingName = "ui_ring";

        public static string PathOf(string name) => $"{Folder}/{name}.png";

        public static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(name));

        public static void GenerateAll(bool overwrite)
        {
            Directory.CreateDirectory(Folder);

            Write(RoundedName, 96, 96, p => SdRoundBox(p, new Vector2(46, 46), 24), new Vector4(30, 30, 30, 30), overwrite);
            Write(RoundedSmallName, 48, 48, p => SdRoundBox(p, new Vector2(22, 22), 11), new Vector4(14, 14, 14, 14), overwrite);
            Write(FrameName, 96, 96, p => Mathf.Abs(SdRoundBox(p, new Vector2(43, 43), 24)) - 3.5f, new Vector4(30, 30, 30, 30), overwrite);
            Write(CircleName, 128, 128, p => p.magnitude - 62f, Vector4.zero, overwrite);
            WriteAlpha(GlowName, 128, 128, p =>
            {
                float t = Mathf.Clamp01(1f - p.magnitude / 63f);
                return t * t;
            }, overwrite);

            // Diamonds (formerly Stars) keep the icon_star file name so existing UI Skin references still work.
            Write(StarName, 128, 128, p => SdPolygon(p, new[]
            {
                new Vector2(-38, 34), new Vector2(38, 34), new Vector2(60, 8), new Vector2(0, -58), new Vector2(-60, 8),
            }), Vector4.zero, overwrite);
            Write(SparkleName, 128, 128, p => SdPolygon(p, StarPoints(4, 60f, 18f, 90f)), Vector4.zero, overwrite);
            Write(CrossName, 128, 128, p =>
            {
                var a = Rotate(p, 45f);
                var b = Rotate(p, -45f);
                return Mathf.Min(SdRoundBox(a, new Vector2(54, 13), 6), SdRoundBox(b, new Vector2(54, 13), 6));
            }, Vector4.zero, overwrite);
            Write(ShieldName, 128, 128, p => SdPolygon(p, new[]
            {
                new Vector2(-50, 50), new Vector2(0, 60), new Vector2(50, 50), new Vector2(48, 0),
                new Vector2(30, -36), new Vector2(0, -60), new Vector2(-30, -36), new Vector2(-48, 0),
            }) - 2f, Vector4.zero, overwrite);
            Write(CoinName, 128, 128, p => Mathf.Min(Mathf.Abs(p.magnitude - 52f) - 8f, p.magnitude - 34f), Vector4.zero, overwrite);
            Write(DoorName, 128, 128, p =>
            {
                float frame = Mathf.Abs(SdRoundBox(p - new Vector2(0, -4), new Vector2(36, 54), 8)) - 6f;
                float knob = (p - new Vector2(18, -6)).magnitude - 7f;
                return Mathf.Min(frame, knob);
            }, Vector4.zero, overwrite);
            Write(CrownName, 128, 128, p => SdPolygon(p, new[]
            {
                new Vector2(-56, -40), new Vector2(56, -40), new Vector2(60, 40), new Vector2(30, 6),
                new Vector2(0, 52), new Vector2(-30, 6), new Vector2(-60, 40),
            }), Vector4.zero, overwrite);
            Write(CheckName, 128, 128, p =>
            {
                float a = SdSegment(p, new Vector2(-44, 2), new Vector2(-12, -32));
                float b = SdSegment(p, new Vector2(-12, -32), new Vector2(48, 38));
                return Mathf.Min(a, b) - 11f;
            }, Vector4.zero, overwrite);

            GenerateSiteIcons(overwrite);
            AssetDatabase.Refresh();
        }

        /// <summary>The website look's button icons (white, tinted by the UI) and the avatar ring.</summary>
        static void GenerateSiteIcons(bool overwrite)
        {
            // A person with a plus: add a viewer to chat.
            Write(AddViewerName, 128, 128, p =>
            {
                float head = (p - new Vector2(-16, 24)).magnitude - 19f;
                float body = SdRoundBox(p - new Vector2(-16, -36), new Vector2(34, 24), 22);
                float plusH = SdRoundBox(p - new Vector2(38, 20), new Vector2(20, 6), 3);
                float plusV = SdRoundBox(p - new Vector2(38, 20), new Vector2(6, 20), 3);
                return Mathf.Min(Mathf.Min(head, body), Mathf.Min(plusH, plusV));
            }, Vector4.zero, overwrite);
            // A person with a minus: remove a viewer from your Audience (shop).
            Write(RemoveViewerName, 128, 128, p =>
            {
                float head = (p - new Vector2(-16, 24)).magnitude - 19f;
                float body = SdRoundBox(p - new Vector2(-16, -36), new Vector2(34, 24), 22);
                float minus = SdRoundBox(p - new Vector2(38, 20), new Vector2(20, 6), 3);
                return Mathf.Min(Mathf.Min(head, body), minus);
            }, Vector4.zero, overwrite);
            // A stop square: end the stream.
            Write(EndStreamName, 128, 128, p => SdRoundBox(p, new Vector2(40, 40), 10), Vector4.zero, overwrite);
            // Two people: the Audience.
            Write(AudienceName, 128, 128, p =>
            {
                float backHead = (p - new Vector2(22, 26)).magnitude - 16f;
                float backBody = SdRoundBox(p - new Vector2(22, -22), new Vector2(28, 22), 20);
                float frontHead = (p - new Vector2(-18, 18)).magnitude - 18f;
                float frontBody = SdRoundBox(p - new Vector2(-18, -36), new Vector2(32, 24), 22);
                float front = Mathf.Min(frontHead, frontBody);
                // A gap around the front person so the two read apart.
                float back = Mathf.Max(Mathf.Min(backHead, backBody), -(front - 6f));
                return Mathf.Min(front, back);
            }, Vector4.zero, overwrite);
            // Three bars: the menu.
            Write(MenuName, 128, 128, p => Mathf.Min(SdRoundBox(p - new Vector2(0, 30), new Vector2(46, 7), 7),
                Mathf.Min(SdRoundBox(p, new Vector2(46, 7), 7), SdRoundBox(p - new Vector2(0, -30), new Vector2(46, 7), 7))), Vector4.zero, overwrite);
            // A lightning bolt: use a viewer's action / the streamer's active.
            Write(ActionName, 128, 128, p => SdPolygon(p, new[]
            {
                new Vector2(14, 60), new Vector2(-34, -6), new Vector2(-4, -6), new Vector2(-16, -60), new Vector2(34, 8), new Vector2(4, 8),
            }) - 2f, Vector4.zero, overwrite);
            // A play triangle: go live (start the next night).
            Write(GoLiveName, 128, 128, p => SdPolygon(p, new[] { new Vector2(-30, 46), new Vector2(-30, -46), new Vector2(48, 0) }) - 5f, Vector4.zero, overwrite);
            // A thin X: cancel / deny.
            Write(CloseName, 128, 128, p =>
            {
                var a = Rotate(p, 45f);
                var b = Rotate(p, -45f);
                return Mathf.Min(SdRoundBox(a, new Vector2(48, 8), 8), SdRoundBox(b, new Vector2(48, 8), 8));
            }, Vector4.zero, overwrite);
            // A crescent moon: the night counter.
            Write(NightName, 128, 128, p => Mathf.Max(p.magnitude - 48f, -((p - new Vector2(22, 14)).magnitude - 40f)), Vector4.zero, overwrite);
            // An hourglass: the stream and shop timers.
            Write(TimerName, 128, 128, p =>
            {
                float caps = Mathf.Min(SdRoundBox(p - new Vector2(0, 50), new Vector2(38, 7), 4), SdRoundBox(p - new Vector2(0, -50), new Vector2(38, 7), 4));
                // Two triangles meeting at a narrow neck.
                float halfWidth = 6f + Mathf.Abs(p.y) * 0.62f;
                float glass = Mathf.Max(Mathf.Abs(p.x) - halfWidth, Mathf.Abs(p.y) - 44f);
                return Mathf.Min(caps, glass);
            }, Vector4.zero, overwrite);
            // A speaker with a slash: mute a player's emotes.
            Write(MuteName, 128, 128, p =>
            {
                float box = SdRoundBox(p - new Vector2(-34, 0), new Vector2(12, 16), 3);
                float coneHalf = 16f + (p.x + 22f) * 0.9f;
                float cone = Mathf.Max(Mathf.Max(Mathf.Abs(p.y) - coneHalf, -(p.x + 22f)), p.x - 10f);
                float speaker = Mathf.Min(box, cone);
                // The slash, from top-right to bottom-left.
                var d = new Vector2(0.7071f, 0.7071f);
                float along = Vector2.Dot(p, d);
                float across = Mathf.Abs(p.x * d.y - p.y * d.x);
                float slash = Mathf.Max(across - 7f, Mathf.Abs(along) - 56f);
                return Mathf.Min(Mathf.Max(speaker, -(across - 13f)), slash);
            }, Vector4.zero, overwrite);
            // A speech bubble with a tail and three dots: the Stream Chat title.
            Write(ChatName, 128, 128, p =>
            {
                float bubble = SdRoundBox(p - new Vector2(0, 10), new Vector2(52, 36), 18);
                // The tail: a small slanted box under the bottom-left of the bubble.
                var q = p - new Vector2(-24, -32);
                q = new Vector2(q.x * 0.8f + q.y * 0.6f, -q.x * 0.6f + q.y * 0.8f);
                float tail = SdRoundBox(q, new Vector2(9, 14), 3);
                float shape = Mathf.Min(bubble, tail);
                float dots = Mathf.Min((p - new Vector2(-22, 10)).magnitude, Mathf.Min((p - new Vector2(0, 10)).magnitude, (p - new Vector2(22, 10)).magnitude)) - 7f;
                return Mathf.Max(shape, -dots);
            }, Vector4.zero, overwrite);
            // A ring: the round chat avatars' border.
            Write(RingName, 128, 128, p => Mathf.Abs(p.magnitude - 58f) - 4.5f, Vector4.zero, overwrite);
        }

        // ------------------------------------------------------------------ rasterising

        static void Write(string name, int width, int height, Func<Vector2, float> sdf, Vector4 border, bool overwrite)
        {
            WriteAlpha(name, width, height, p => Mathf.Clamp01(0.5f - sdf(p)), overwrite, border);
        }

        static void WriteAlpha(string name, int width, int height, Func<Vector2, float> alpha, bool overwrite, Vector4 border = default)
        {
            string path = PathOf(name);
            if (!overwrite && File.Exists(path)) return;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            var centre = new Vector2(width * 0.5f, height * 0.5f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Supersample 2x2 for smooth edges.
                    float a = 0f;
                    for (int sy = 0; sy < 2; sy++)
                    for (int sx = 0; sx < 2; sx++)
                    {
                        var p = new Vector2(x + 0.25f + sx * 0.5f, y + 0.25f + sy * 0.5f) - centre;
                        a += Mathf.Clamp01(alpha(p));
                    }
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 0.25f * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------ signed distance helpers (pixel units)

        static float SdRoundBox(Vector2 p, Vector2 halfSize, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize + new Vector2(radius, radius);
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        static float SdPolygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y;
                bool c2 = p.y < v[j].y;
                bool c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        static Vector2[] StarPoints(int points, float outer, float inner, float startDegrees)
        {
            var result = new Vector2[points * 2];
            for (int i = 0; i < result.Length; i++)
            {
                float angle = (startDegrees + i * 180f / points) * Mathf.Deg2Rad;
                float r = i % 2 == 0 ? outer : inner;
                result[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }
            return result;
        }

        static Vector2 Rotate(Vector2 p, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);
        }
    }
}
