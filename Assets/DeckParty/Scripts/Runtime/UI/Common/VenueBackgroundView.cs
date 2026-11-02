using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// Draws the current venue. With a background sprite assigned it shows your art (cover-fit); otherwise it
    /// builds a procedural placeholder scene from the venue's palette and style. Party lights animate on top
    /// and react to how full the party is, and flash red/blue during a police raid.
    /// </summary>
    public sealed class VenueBackgroundView : MonoBehaviour
    {
        [SerializeField] internal RawImage gradient;
        [SerializeField] internal Image art;
        [SerializeField] internal AspectRatioFitter artFitter;
        [SerializeField] internal RectTransform decorRoot;
        [SerializeField] internal RectTransform lightsRoot;
        [SerializeField] internal Image foreground;
        [SerializeField] internal Image tint;

        struct Bulb
        {
            public Image Image;
            public Color Color;
            public float Phase;
        }

        struct Spot
        {
            public RectTransform Rect;
            public Image Image;
            public Color Color;
            public Vector2 Anchor;
            public float Speed;
            public float Phase;
        }

        readonly List<Bulb> m_Bulbs = new List<Bulb>();
        readonly List<Spot> m_Spots = new List<Spot>();
        VenueDefinition m_Venue;
        Texture2D m_GradientTexture;
        float m_Energy = 0.35f;
        float m_DisplayEnergy = 0.35f;
        float m_PoliceUntil;

        public VenueDefinition Current => m_Venue;

        // Skin overrides for the light shapes, falling back to the generated ones.
        static Sprite GlowSprite => SkinSprite(SkinSlot.Glow) ?? RuntimeSprites.Glow;
        static Sprite CircleSprite => SkinSprite(SkinSlot.Circle) ?? RuntimeSprites.Circle;

        static Sprite SkinSprite(SkinSlot slot)
        {
            var skin = GameDatabase.Instance.skin;
            if (skin == null) return null;
            var sprite = skin.GetSprite(slot);
            return sprite != null ? sprite : null;
        }

        void OnDestroy()
        {
            if (m_GradientTexture != null) Destroy(m_GradientTexture);
        }

        public void Apply(VenueDefinition venue, bool force = false)
        {
            if (venue == null || (venue == m_Venue && !force)) return;
            m_Venue = venue;
            Rebuild();
        }

        /// <summary>0..1 party intensity (how full the house is).</summary>
        public void SetEnergy(float energy) => m_Energy = Mathf.Clamp01(energy);

        public void PoliceLights(float seconds = 2.6f) => m_PoliceUntil = Time.unscaledTime + seconds;

        float m_Risk;
        float m_DisplayRisk;

        /// <summary>Reactive background (FX Library > reactiveBackground): 0 calm, 1 one Deplatform Risk from being deplatformed (lights drift red and blue).</summary>
        public void SetRisk(float risk) => m_Risk = FxLibrary.Current.reactiveBackground ? Mathf.Clamp01(risk) : 0f;

        void Rebuild()
        {
            Clear(decorRoot);
            Clear(lightsRoot);
            m_Bulbs.Clear();
            m_Spots.Clear();

            if (m_GradientTexture != null) Destroy(m_GradientTexture);
            m_GradientTexture = RuntimeSprites.Gradient(m_Venue.skyTop, m_Venue.skyBottom);
            gradient.texture = m_GradientTexture;
            gradient.color = Color.white;

            bool hasArt = m_Venue.background != null;
            art.enabled = hasArt;
            if (hasArt)
            {
                art.sprite = m_Venue.background;
                art.preserveAspect = false;
                var rect = m_Venue.background.rect;
                artFitter.aspectRatio = rect.height > 0 ? rect.width / rect.height : 16f / 9f;
            }

            decorRoot.gameObject.SetActive(!hasArt);
            if (!hasArt) BuildDecor();

            foreground.enabled = m_Venue.foreground != null;
            foreground.sprite = m_Venue.foreground;

            lightsRoot.gameObject.SetActive(m_Venue.partyLights);
            if (m_Venue.partyLights) BuildLights(!hasArt);

            if (tint != null) tint.color = new Color(0, 0, 0, 0);
        }

        static void Clear(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
        }

        // ------------------------------------------------------------------ procedural scenery

        Image Add(RectTransform parent, string name, Color color, Vector2 min, Vector2 max, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Adds a circle of the given diameter (in canvas units) centred on an anchor point.</summary>
        Image Dot(RectTransform parent, string name, Color color, Vector2 anchor, float diameter, Sprite sprite = null)
        {
            var image = Add(parent, name, color, anchor, anchor, sprite != null ? sprite : CircleSprite);
            image.rectTransform.sizeDelta = new Vector2(diameter, diameter);
            return image;
        }

        void BuildDecor()
        {
            var v = m_Venue;
            var rng = new System.Random(v.id != null ? v.id.GetHashCode() : 7);
            const float floorTop = 0.24f;
            bool indoor = v.style == VenueStyle.Apartment || v.style == VenueStyle.Mansion || v.style == VenueStyle.Haunted || v.style == VenueStyle.Disco;

            switch (v.style)
            {
                case VenueStyle.Space:
                    for (int i = 0; i < 90; i++)
                    {
                        var p = new Vector2((float)rng.NextDouble(), floorTop + (float)rng.NextDouble() * (1f - floorTop));
                        Dot(decorRoot, "Star", new Color(1, 1, 1, 0.25f + (float)rng.NextDouble() * 0.6f), p, 2f + (float)rng.NextDouble() * 5f);
                    }
                    Dot(decorRoot, "Planet", Color.Lerp(v.accentA, v.skyBottom, 0.35f), new Vector2(0.82f, 0.7f), 300f);
                    var ring = Add(decorRoot, "Ring", v.accentB.WithAlpha(0.6f), new Vector2(0.82f, 0.7f), new Vector2(0.82f, 0.7f));
                    ring.rectTransform.sizeDelta = new Vector2(520f, 18f);
                    ring.rectTransform.localRotation = Quaternion.Euler(0, 0, -14f);
                    Dot(decorRoot, "Moonlet", v.accentB.WithAlpha(0.8f), new Vector2(0.14f, 0.82f), 70f);
                    break;

                case VenueStyle.Beach:
                    Dot(decorRoot, "SunGlow", v.accentA.WithAlpha(0.35f), new Vector2(0.5f, 0.38f), 640f, GlowSprite);
                    Dot(decorRoot, "Sun", v.accentA, new Vector2(0.5f, 0.38f), 260f);
                    Add(decorRoot, "Sea", v.wallColor, new Vector2(0f, floorTop), new Vector2(1f, 0.38f));
                    for (int i = 0; i < 6; i++)
                    {
                        float y = floorTop + 0.02f + i * 0.022f;
                        float x = (float)rng.NextDouble() * 0.7f;
                        Add(decorRoot, "Wave", new Color(1, 1, 1, 0.18f), new Vector2(x, y), new Vector2(x + 0.2f + (float)rng.NextDouble() * 0.1f, y + 0.004f));
                    }
                    break;

                case VenueStyle.Rooftop:
                    for (int i = 0; i < 60; i++)
                        Dot(decorRoot, "Star", new Color(1, 1, 1, 0.2f + (float)rng.NextDouble() * 0.4f),
                            new Vector2((float)rng.NextDouble(), 0.6f + (float)rng.NextDouble() * 0.4f), 2f + (float)rng.NextDouble() * 3f);
                    float x0 = 0f;
                    while (x0 < 1f)
                    {
                        float w = 0.04f + (float)rng.NextDouble() * 0.06f;
                        float h = 0.18f + (float)rng.NextDouble() * 0.36f;
                        var building = Add(decorRoot, "Building", Color.Lerp(v.wallColor, Color.black, 0.2f), new Vector2(x0, floorTop), new Vector2(x0 + w, floorTop + h));
                        for (int k = 0; k < 6; k++)
                        {
                            if (rng.NextDouble() < 0.4) continue;
                            var wx = (float)rng.NextDouble();
                            var wy = 0.1f + (float)rng.NextDouble() * 0.8f;
                            var window = Add(building.rectTransform, "Window", (rng.NextDouble() < 0.5 ? v.accentA : v.accentB).WithAlpha(0.55f),
                                new Vector2(wx, wy), new Vector2(wx, wy));
                            window.rectTransform.sizeDelta = new Vector2(8f, 10f);
                        }
                        x0 += w + 0.004f;
                    }
                    Add(decorRoot, "Railing", v.accentB.WithAlpha(0.55f), new Vector2(0f, floorTop + 0.06f), new Vector2(1f, floorTop + 0.066f));
                    for (int i = 0; i <= 24; i++)
                    {
                        float px = i / 24f;
                        Add(decorRoot, "Post", v.accentB.WithAlpha(0.4f), new Vector2(px, floorTop), new Vector2(px + 0.003f, floorTop + 0.066f));
                    }
                    break;
            }

            if (indoor)
            {
                Add(decorRoot, "Wall", v.wallColor, new Vector2(0f, floorTop), new Vector2(1f, 1f));
                Add(decorRoot, "WallShade", new Color(0, 0, 0, 0.18f), new Vector2(0f, 0.86f), new Vector2(1f, 1f));

                switch (v.style)
                {
                    case VenueStyle.Apartment:
                        Window(new Vector2(0.08f, 0.44f), new Vector2(0.26f, 0.8f), v, rng, 2, 2);
                        Window(new Vector2(0.74f, 0.44f), new Vector2(0.92f, 0.8f), v, rng, 2, 2);
                        Add(decorRoot, "Poster", v.accentB.WithAlpha(0.8f), new Vector2(0.44f, 0.56f), new Vector2(0.56f, 0.78f), RuntimeSprites.Rounded).type = Image.Type.Sliced;
                        Add(decorRoot, "PosterInner", v.accentA.WithAlpha(0.9f), new Vector2(0.465f, 0.6f), new Vector2(0.535f, 0.74f), RuntimeSprites.Rounded).type = Image.Type.Sliced;
                        break;
                    case VenueStyle.Mansion:
                        for (int i = 0; i < 3; i++)
                        {
                            float cx = 0.17f + i * 0.33f;
                            Window(new Vector2(cx - 0.06f, 0.38f), new Vector2(cx + 0.06f, 0.78f), v, rng, 2, 3);
                            Dot(decorRoot, "Arch", Color.Lerp(v.skyTop, v.skyBottom, 0.3f), new Vector2(cx, 0.78f), 230f * 0.6f);
                        }
                        Add(decorRoot, "ChandelierChain", v.accentA.WithAlpha(0.7f), new Vector2(0.499f, 0.86f), new Vector2(0.501f, 1f));
                        Dot(decorRoot, "ChandelierGlow", v.accentA.WithAlpha(0.35f), new Vector2(0.5f, 0.84f), 360f, GlowSprite);
                        for (int i = 0; i < 7; i++)
                            Dot(decorRoot, "Crystal", v.accentA, new Vector2(0.46f + i * 0.0133f, 0.84f - Mathf.Sin(i / 6f * Mathf.PI) * 0.03f), 20f);
                        break;
                    case VenueStyle.Haunted:
                        Window(new Vector2(0.36f, 0.4f), new Vector2(0.64f, 0.84f), v, rng, 3, 3);
                        Dot(decorRoot, "MoonGlow", v.accentA.WithAlpha(0.25f), new Vector2(0.55f, 0.72f), 300f, GlowSprite);
                        Dot(decorRoot, "Moon", new Color(0.92f, 0.96f, 0.86f, 0.95f), new Vector2(0.55f, 0.72f), 110f);
                        for (int i = 0; i < 4; i++)
                        {
                            float y = floorTop + 0.02f + i * 0.05f;
                            Add(decorRoot, "Fog", v.accentA.WithAlpha(0.06f), new Vector2(-0.1f + i * 0.05f, y), new Vector2(0.8f + i * 0.08f, y + 0.07f), GlowSprite);
                        }
                        break;
                    case VenueStyle.Disco:
                        Add(decorRoot, "BallString", new Color(1, 1, 1, 0.5f), new Vector2(0.499f, 0.82f), new Vector2(0.501f, 1f));
                        Dot(decorRoot, "BallGlow", new Color(1, 1, 1, 0.25f), new Vector2(0.5f, 0.78f), 420f, GlowSprite);
                        Dot(decorRoot, "DiscoBall", new Color(0.85f, 0.88f, 0.95f, 1f), new Vector2(0.5f, 0.78f), 150f);
                        break;
                }
            }

            // Floor
            Add(decorRoot, "Floor", v.floorColor, new Vector2(0f, 0f), new Vector2(1f, floorTop));
            Add(decorRoot, "FloorEdge", Color.Lerp(v.floorColor, Color.white, 0.18f), new Vector2(0f, floorTop - 0.006f), new Vector2(1f, floorTop));
            if (v.style == VenueStyle.Disco)
            {
                const int columns = 16;
                for (int row = 0; row < 3; row++)
                for (int col = 0; col < columns; col++)
                {
                    float w = 1f / columns;
                    float h = floorTop / 3f;
                    var color = ((row + col) % 2 == 0 ? v.accentA : v.accentB).WithAlpha(0.22f);
                    var tile = Add(decorRoot, "Tile", color, new Vector2(col * w, row * h), new Vector2((col + 1) * w, (row + 1) * h));
                    m_Bulbs.Add(new Bulb { Image = tile, Color = color, Phase = (float)rng.NextDouble() * 6f });
                }
            }
            else if (v.style == VenueStyle.Space)
            {
                for (int i = 0; i < 10; i++)
                    Add(decorRoot, "DeckStripe", v.accentA.WithAlpha(0.12f), new Vector2(i * 0.1f, 0f), new Vector2(i * 0.1f + 0.05f, floorTop - 0.01f));
            }
            else
            {
                for (int i = 0; i < 12; i++)
                    Add(decorRoot, "Plank", new Color(0, 0, 0, 0.08f), new Vector2(i / 12f, 0f), new Vector2(i / 12f + 0.002f, floorTop - 0.006f));
            }
        }

        void Window(Vector2 min, Vector2 max, VenueDefinition v, System.Random rng, int panesX, int panesY)
        {
            var frame = Add(decorRoot, "WindowFrame", Color.Lerp(v.wallColor, Color.black, 0.35f), min, max);
            var glass = Add(frame.rectTransform, "Glass", Color.Lerp(v.skyTop, v.skyBottom, 0.4f), new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f));
            for (int i = 0; i < 8; i++)
                Dot(glass.rectTransform, "Star", new Color(1, 1, 1, 0.3f + (float)rng.NextDouble() * 0.5f),
                    new Vector2((float)rng.NextDouble(), 0.3f + (float)rng.NextDouble() * 0.7f), 3f + (float)rng.NextDouble() * 3f);
            for (int x = 1; x < panesX; x++)
                Add(frame.rectTransform, "Mullion", Color.Lerp(v.wallColor, Color.black, 0.35f), new Vector2(x / (float)panesX - 0.015f, 0f), new Vector2(x / (float)panesX + 0.015f, 1f));
            for (int y = 1; y < panesY; y++)
                Add(frame.rectTransform, "Transom", Color.Lerp(v.wallColor, Color.black, 0.35f), new Vector2(0f, y / (float)panesY - 0.012f), new Vector2(1f, y / (float)panesY + 0.012f));
        }

        void BuildLights(bool stringLights)
        {
            var v = m_Venue;
            if (stringLights)
            {
                const int count = 26;
                for (int i = 0; i < count; i++)
                {
                    float x = (i + 0.5f) / count;
                    float y = 0.95f - Mathf.Sin(x * Mathf.PI) * 0.045f;
                    var color = i % 2 == 0 ? v.accentA : v.accentB;
                    var glow = Dot(lightsRoot, "BulbGlow", color.WithAlpha(0.3f), new Vector2(x, y), 64f, GlowSprite);
                    var bulb = Dot(lightsRoot, "Bulb", color, new Vector2(x, y), 16f);
                    m_Bulbs.Add(new Bulb { Image = glow, Color = color.WithAlpha(0.35f), Phase = i * 0.7f });
                    m_Bulbs.Add(new Bulb { Image = bulb, Color = color, Phase = i * 0.7f });
                }
            }

            for (int i = 0; i < 5; i++)
            {
                var color = (i % 2 == 0 ? v.accentA : v.accentB);
                var anchor = new Vector2(0.1f + i * 0.2f, 0.45f + (i % 2) * 0.15f);
                var image = Dot(lightsRoot, "Spot", color.WithAlpha(0.1f), anchor, 700f, GlowSprite);
                m_Spots.Add(new Spot { Rect = image.rectTransform, Image = image, Color = color, Anchor = anchor, Speed = 0.2f + i * 0.05f, Phase = i * 1.3f });
            }
        }

        void Update()
        {
            if (m_Venue == null) return;
            m_DisplayEnergy = Mathf.MoveTowards(m_DisplayEnergy, m_Energy, Time.unscaledDeltaTime * 0.5f);
            m_DisplayRisk = Mathf.MoveTowards(m_DisplayRisk, m_Risk, Time.unscaledDeltaTime * 0.8f);
            float time = Time.unscaledTime;
            bool police = time < m_PoliceUntil;
            bool redPhase = Mathf.Repeat(time * 4f, 2f) < 1f;
            var policeColor = redPhase ? new Color(1f, 0.15f, 0.2f) : new Color(0.2f, 0.4f, 1f);
            // Rising Deplatform Risk slowly bleeds the warning colours into the scene's own lights.
            var riskColor = Mathf.Repeat(time * 0.8f, 2f) < 1f ? new Color(1f, 0.2f, 0.25f) : new Color(0.25f, 0.4f, 1f);
            float riskBlend = AppSettings.ReduceFlashing ? m_DisplayRisk * 0.4f : m_DisplayRisk * 0.7f;

            for (int i = 0; i < m_Bulbs.Count; i++)
            {
                var bulb = m_Bulbs[i];
                if (bulb.Image == null) continue;
                float pulse = 0.55f + 0.45f * Mathf.Sin(time * (1.5f + m_DisplayEnergy * 3f) + bulb.Phase);
                var c = police ? policeColor.WithAlpha(bulb.Color.a) : Color.Lerp(bulb.Color, riskColor, riskBlend);
                c.a = bulb.Color.a * Mathf.Lerp(0.5f, 1f, pulse);
                bulb.Image.color = c;
            }

            for (int i = 0; i < m_Spots.Count; i++)
            {
                var spot = m_Spots[i];
                if (spot.Rect == null) continue;
                float t = time * spot.Speed + spot.Phase;
                var offset = new Vector2(Mathf.Sin(t) * 0.12f, Mathf.Cos(t * 0.8f) * 0.08f);
                spot.Rect.anchorMin = spot.Rect.anchorMax = spot.Anchor + offset;
                float alpha = Mathf.Lerp(0.04f, 0.2f, m_DisplayEnergy);
                spot.Image.color = (police ? policeColor : Color.Lerp(spot.Color, riskColor, riskBlend)).WithAlpha(police ? 0.28f : Mathf.Lerp(alpha, 0.24f, m_DisplayRisk));
            }

            if (tint != null)
                tint.color = police ? policeColor.WithAlpha(0.12f + 0.08f * Mathf.Sin(time * 25f))
                    : m_DisplayRisk > 0.01f ? riskColor.WithAlpha(0.05f * m_DisplayRisk) : new Color(0, 0, 0, 0);
        }
    }
}
