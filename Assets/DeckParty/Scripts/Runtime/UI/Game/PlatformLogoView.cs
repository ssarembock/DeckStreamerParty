using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The animated platform logo at the top-left of the stream and shop. Each platform has its own little loop, made from
    /// separate sprites moved with code-driven squash and stretch (the same technique as Feel's Squash and Stretch feedback,
    /// but synced across several parts):
    /// <list type="bullet">
    /// <item>Cough: the logo coughs twice (squash, lean and spring back); money flies out of its mouth sideways and fades fast,
    /// and three cough lines blink on for each cough.</item>
    /// <item>Lookout: the balloon looks left, back to centre, right, back to centre; the pupils slide across the lenses while the
    /// body leans and squashes toward the look, and the balloon bobs.</item>
    /// <item>Stomp: the boot drops onto the emoticon, squashes it flat (the impact effect flashes), holds, the effect hides, the
    /// boot lifts and the emoticon springs back.</item>
    /// <item>Swipe: the hand sweeps a full circle and repaints the face behind it (a radial fill), black face to white and back;
    /// the hand switches colour after each sweep.</item>
    /// <item>Post and Tunes: a hop with anticipation, stretch and a springy landing, then a breather.</item>
    /// </list>
    /// Art, placement and timing come from Game Database > Platforms; empty sprite slots use built-in shapes.
    /// </summary>
    public sealed class PlatformLogoView : MonoBehaviour
    {
        [Tooltip("Follow the platform of the streamer on screen. Untick to always show Preview (for testing a logo).")]
        [SerializeField] internal bool followTheme = true;
        [SerializeField] internal StreamPlatform preview = StreamPlatform.Lookout;

        struct Bill
        {
            public RectTransform Rect;
            public Image Image;
            public float Age;
            public Vector2 Start;
            public Vector2 Velocity;
            public float Spin;
            public bool Alive;
        }

        const int MaxBills = 8;

        RectTransform m_Logo;
        StreamPlatform m_Platform;
        PlatformArt m_Art;
        bool m_Built;
        float m_Size;
        float m_Clock;
        float m_Time;

        // Shared: the main body (logo / balloon / emoticon / clock face).
        RectTransform m_Body;
        Image m_BodyImage;

        // Cough
        readonly Bill[] m_Bills = new Bill[MaxBills];
        RectTransform m_Lines;
        int m_LastCough = -1;

        // Lookout
        RectTransform m_PupilLeft;
        RectTransform m_PupilRight;
        float m_LastLook;

        // Stomp
        RectTransform m_Boot;
        RectTransform m_Impact;

        // Swipe
        Image m_FaceBase;
        Image m_FaceReveal;
        RectTransform m_Hand;
        Image m_HandImage;
        Image m_HandCap;
        bool m_LightIsBase;
        bool m_Sweeping = true;

        static Sprite s_FaceLight;
        static Sprite s_FaceDark;

        void OnEnable()
        {
            SiteTheme.Changed += Rebuild;
            Rebuild();
        }

        void OnDisable() => SiteTheme.Changed -= Rebuild;

        /// <summary>Rebuilds the logo for the current platform (called when the streamer on screen changes platform).</summary>
        public void Rebuild()
        {
            var platform = followTheme ? SiteTheme.Platform : preview;
            var art = GameDatabase.Instance.GetPlatformArt(platform) ?? PlatformArt.CreateDefault(platform);
            if (m_Built && platform == m_Platform && art == m_Art) return;
            m_Platform = platform;
            m_Art = art;
            m_Built = true;

            if (m_Logo != null) Destroy(m_Logo.gameObject);
            m_Logo = UiFactory.Rect("Logo", transform);
            m_Size = Mathf.Max(32f, art.size);
            m_Logo.anchorMin = m_Logo.anchorMax = new Vector2(0f, 0.5f);
            m_Logo.pivot = new Vector2(0f, 0.5f);
            m_Logo.sizeDelta = new Vector2(m_Size, m_Size);
            m_Logo.anchoredPosition = Vector2.zero;

            m_Clock = 0f;
            m_BodyScale = 1f;
            m_LastCough = -1;
            m_LastLook = 0f;
            m_LightIsBase = false;
            m_Sweeping = true;
            for (int i = 0; i < m_Bills.Length; i++) m_Bills[i] = default;
            m_Lines = m_Boot = m_Impact = m_PupilLeft = m_PupilRight = m_Hand = null;
            m_FaceBase = m_FaceReveal = m_HandImage = m_HandCap = null;

            switch (platform)
            {
                case StreamPlatform.Cough: BuildCough(); break;
                case StreamPlatform.Lookout: BuildLookout(); break;
                case StreamPlatform.Stomp: BuildStomp(); break;
                case StreamPlatform.Swipe: BuildSwipe(); break;
                default: BuildGeneric(); break;
            }
        }

        void Update()
        {
            if (!m_Built || m_Logo == null) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            m_Time += dt;
            m_Clock += dt * Mathf.Max(0.05f, m_Art.speed);
            switch (m_Platform)
            {
                case StreamPlatform.Cough: AnimateCough(dt); break;
                case StreamPlatform.Lookout: AnimateLookout(dt); break;
                case StreamPlatform.Stomp: AnimateStomp(); break;
                case StreamPlatform.Swipe: AnimateSwipe(); break;
                default: AnimateGeneric(); break;
            }
        }

        // ================================================================== Cough

        const float CoughGap = 0.5f;
        const float CoughLength = 0.45f;

        void BuildCough()
        {
            var art = m_Art;
            m_Body = Body(art.logo, 0.86f, out m_BodyImage);
            if (art.logo == null)
            {
                m_BodyImage.sprite = RuntimeSprites.Rounded;
                m_BodyImage.type = Image.Type.Sliced;
                m_BodyImage.color = art.accent.WithAlpha(1f);
                // Placeholder face: two eyes and a mouth on the right, where the money comes out.
                Part("EyeL", m_Body, RuntimeSprites.Circle, Color.white, new Vector2(0.1f, 0.1f) * m_Size, new Vector2(-0.12f, 0.12f) * m_Size);
                Part("EyeR", m_Body, RuntimeSprites.Circle, Color.white, new Vector2(0.1f, 0.1f) * m_Size, new Vector2(0.1f, 0.12f) * m_Size);
                Part("Mouth", m_Body, RuntimeSprites.Circle, UIPalette.Hex(0x18181B), new Vector2(0.12f, 0.09f) * m_Size, new Vector2(0.22f, -0.1f) * m_Size);
            }

            var mouth = Point(art.pointA);
            m_Lines = UiFactory.Rect("CoughLines", m_Logo);
            m_Lines.anchorMin = m_Lines.anchorMax = new Vector2(0.5f, 0.5f);
            m_Lines.sizeDelta = new Vector2(0.34f, 0.34f) * m_Size;
            m_Lines.anchoredPosition = mouth + new Vector2(0.2f * m_Size, 0f);
            if (art.partB != null)
            {
                var lines = UiFactory.Image("Lines", m_Lines, Color.white, art.partB);
                lines.preserveAspect = true;
                UiFactory.Stretch(lines.rectTransform);
            }
            else
            {
                // Three short strokes fanning out from the mouth.
                for (int i = 0; i < 3; i++)
                {
                    float angle = 28f - i * 28f;
                    var stroke = UiFactory.Image("Line" + (i + 1), m_Lines, UIPalette.Hex(0x18181B), RuntimeSprites.Square);
                    var rect = stroke.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.sizeDelta = new Vector2(0.17f * m_Size, Mathf.Max(2f, 0.035f * m_Size));
                    rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                    rect.anchoredPosition = new Vector2(0.02f * m_Size, 0f);
                }
            }
            m_Lines.gameObject.SetActive(false);

            for (int i = 0; i < MaxBills; i++)
            {
                var sprite = art.partA != null ? art.partA : RuntimeSprites.Rounded;
                var bill = UiFactory.Image("Money" + (i + 1), m_Logo, art.partA != null ? Color.white : UIPalette.Hex(0x2FB463), sprite);
                bill.preserveAspect = art.partA != null;
                if (art.partA == null) bill.type = Image.Type.Sliced;
                var rect = bill.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                float w = art.partSize * m_Size;
                rect.sizeDelta = art.partA != null ? new Vector2(w, w) : new Vector2(w, w * 0.55f);
                bill.gameObject.SetActive(false);
                m_Bills[i] = new Bill { Rect = rect, Image = bill };
            }
        }

        void AnimateCough(float dt)
        {
            float loop = CoughGap + CoughLength + Mathf.Max(0f, m_Art.rest);
            if (m_Clock >= loop)
            {
                m_Clock -= loop;
                m_LastCough = -1;
            }
            float t = m_Clock;

            // Two coughs: the second a little weaker.
            float sx = 1f, sy = 1f, rot = 0f;
            for (int c = 0; c < 2; c++)
            {
                float local = t - c * CoughGap;
                if (local < 0f || local > CoughLength) continue;
                CoughPose(local, c == 0 ? 1f : 0.75f, out sx, out sy, out rot);
                // The cough itself: money and lines at the moment of release.
                if (local >= 0.1f && m_LastCough < c)
                {
                    m_LastCough = c;
                    SpawnBills(c == 0 ? 3 : 2);
                }
                if (m_Lines != null) m_Lines.gameObject.SetActive(local >= 0.1f && local < 0.3f);
            }
            if (t > CoughGap + CoughLength && m_Lines != null) m_Lines.gameObject.SetActive(false);

            float breath = 1f + 0.012f * Mathf.Sin(m_Time * 2.2f);
            m_Body.localScale = new Vector3(sx / breath, sy * breath, 1f);
            m_Body.localRotation = Quaternion.Euler(0f, 0f, rot);
            UpdateBills(dt);
        }

        /// <summary>One cough: breathe in (stretch up, lean back), bark (squash, lean forward), spring back.</summary>
        static void CoughPose(float t, float strength, out float sx, out float sy, out float rot)
        {
            if (t < 0.1f)
            {
                float k = EaseOut(t / 0.1f);
                sx = Mathf.Lerp(1f, 0.95f, k);
                sy = Mathf.Lerp(1f, 1.07f, k);
                rot = Mathf.Lerp(0f, 4f, k);
            }
            else if (t < 0.18f)
            {
                float k = EaseOut((t - 0.1f) / 0.08f);
                sx = Mathf.Lerp(0.95f, 1.16f, k);
                sy = Mathf.Lerp(1.07f, 0.84f, k);
                rot = Mathf.Lerp(4f, -8f, k);
            }
            else
            {
                float k = (t - 0.18f) / (CoughLength - 0.18f);
                float e = Mathf.Exp(-5f * k) * Mathf.Cos(11f * k);
                sx = 1f + 0.16f * e;
                sy = 1f - 0.16f * e;
                rot = -8f * e;
            }
            sx = 1f + (sx - 1f) * strength;
            sy = 1f + (sy - 1f) * strength;
            rot *= strength;
        }

        void SpawnBills(int count)
        {
            var mouth = Point(m_Art.pointA);
            float distance = Mathf.Max(0.1f, m_Art.travel) * m_Size;
            for (int n = 0, i = 0; i < m_Bills.Length && n < count; i++)
            {
                if (m_Bills[i].Alive) continue;
                n++;
                var bill = m_Bills[i];
                bill.Alive = true;
                bill.Age = 0f;
                bill.Start = mouth;
                // Mostly sideways (away from the logo), a little up or down.
                float spread = (n - (count + 1) * 0.5f) * 0.22f + Random.Range(-0.06f, 0.06f);
                bill.Velocity = new Vector2(distance / 0.5f, spread * distance / 0.5f);
                bill.Spin = Random.Range(-260f, 260f);
                bill.Rect.anchoredPosition = mouth;
                bill.Rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));
                bill.Rect.gameObject.SetActive(true);
                m_Bills[i] = bill;
            }
        }

        void UpdateBills(float dt)
        {
            const float life = 0.55f;
            for (int i = 0; i < m_Bills.Length; i++)
            {
                var bill = m_Bills[i];
                if (!bill.Alive) continue;
                bill.Age += dt * Mathf.Max(0.05f, m_Art.speed);
                float k = bill.Age / life;
                if (k >= 1f)
                {
                    bill.Alive = false;
                    bill.Rect.gameObject.SetActive(false);
                    m_Bills[i] = bill;
                    continue;
                }
                // Fast out, slowing down, fading quickly.
                float travel = 1f - (1f - k) * (1f - k);
                bill.Rect.anchoredPosition = bill.Start + bill.Velocity * (travel * life * 0.9f);
                bill.Rect.localRotation = Quaternion.Euler(0f, 0f, bill.Rect.localEulerAngles.z + bill.Spin * dt);
                float pop = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(k * 5f));
                bill.Rect.localScale = new Vector3(pop, pop, 1f);
                var c = bill.Image.color;
                c.a = 1f - Mathf.Clamp01((k - 0.25f) / 0.75f);
                bill.Image.color = c;
                m_Bills[i] = bill;
            }
        }

        // ================================================================== Lookout

        const float LookMove = 0.28f;
        const float LookHold = 0.7f;

        void BuildLookout()
        {
            var art = m_Art;
            m_Body = Body(art.logo, 1f, out m_BodyImage);
            if (art.logo == null)
            {
                // Placeholder: a balloon on a string with binoculars across it.
                m_BodyImage.enabled = false;
                Part("String", m_Body, RuntimeSprites.Square, UIPalette.Hex(0x53535F), new Vector2(0.025f, 0.24f) * m_Size, new Vector2(0f, -0.36f) * m_Size);
                Part("Balloon", m_Body, RuntimeSprites.Circle, art.accent.WithAlpha(1f), new Vector2(0.74f, 0.74f) * m_Size, new Vector2(0f, 0.1f) * m_Size);
                var middle = Vector2.Lerp(Point(art.pointA), Point(art.pointB), 0.5f);
                Part("Bridge", m_Body, RuntimeSprites.Square, UIPalette.Hex(0x18181B), new Vector2(0.16f, 0.06f) * m_Size, InBody(middle));
                foreach (var point in new[] { art.pointA, art.pointB })
                {
                    float lens = art.partSize * 2.3f * m_Size;
                    Part("LensRim", m_Body, RuntimeSprites.Circle, UIPalette.Hex(0x18181B), new Vector2(lens * 1.25f, lens * 1.25f), InBody(Point(point)));
                    Part("Lens", m_Body, RuntimeSprites.Circle, Color.white, new Vector2(lens, lens), InBody(Point(point)));
                }
            }

            float pupil = art.partSize * m_Size;
            var pupilSprite = art.partA != null ? art.partA : RuntimeSprites.Circle;
            var pupilColor = art.partA != null ? Color.white : UIPalette.Hex(0x0E0E10);
            m_PupilLeft = Part("PupilL", m_Body, pupilSprite, pupilColor, new Vector2(pupil, pupil), InBody(Point(art.pointA)));
            m_PupilRight = Part("PupilR", m_Body, pupilSprite, pupilColor, new Vector2(pupil, pupil), InBody(Point(art.pointB)));
            if (art.partA != null)
            {
                m_PupilLeft.GetComponent<Image>().preserveAspect = true;
                m_PupilRight.GetComponent<Image>().preserveAspect = true;
            }
        }

        /// <summary>Where the body holder's pivot (the box's bottom-centre) sits, in pixels from the box centre.</summary>
        Vector2 BodyOrigin() => new Vector2(0f, -0.5f * m_Size);

        /// <summary>-1 looking left, 0 centre, 1 right: left, back, right, back, holding at each stop.</summary>
        static float LookAt(float t, float hold, out float loop)
        {
            loop = 4f * LookMove + 2f * LookHold + 2f * hold;
            t = Mathf.Repeat(t, loop);
            if (t < LookMove) return -EaseInOut(t / LookMove);
            t -= LookMove;
            if (t < LookHold) return -1f;
            t -= LookHold;
            if (t < LookMove) return -1f + EaseInOut(t / LookMove);
            t -= LookMove;
            if (t < hold) return 0f;
            t -= hold;
            if (t < LookMove) return EaseInOut(t / LookMove);
            t -= LookMove;
            if (t < LookHold) return 1f;
            t -= LookHold;
            if (t < LookMove) return 1f - EaseInOut(t / LookMove);
            return 0f;
        }

        void AnimateLookout(float dt)
        {
            float look = LookAt(m_Clock, Mathf.Max(0.05f, m_Art.rest), out float loop);
            if (m_Clock >= loop) m_Clock -= loop;

            // The body follows the eyes: leans toward the look, squashes a little on the turn.
            float speed = dt > 0f ? Mathf.Clamp01(Mathf.Abs(look - m_LastLook) / dt * LookMove * 0.5f) : 0f;
            m_LastLook = look;
            float sx = 1f + 0.05f * Mathf.Abs(look) + 0.04f * speed;
            float sy = 1f - 0.035f * Mathf.Abs(look) - 0.05f * speed;
            float bob = Mathf.Sin(m_Time * 2.1f) * 0.025f * m_Size;
            m_Body.anchoredPosition = BodyOrigin() + new Vector2(look * 0.02f * m_Size, bob);
            m_Body.localScale = new Vector3(sx, sy, 1f);
            m_Body.localRotation = Quaternion.Euler(0f, 0f, -look * 5f);

            float shift = look * m_Art.travel * m_Size;
            if (m_PupilLeft != null) m_PupilLeft.anchoredPosition = InBody(Point(m_Art.pointA)) + new Vector2(shift, 0f);
            if (m_PupilRight != null) m_PupilRight.anchoredPosition = InBody(Point(m_Art.pointB)) + new Vector2(shift, 0f);
        }

        // ================================================================== Stomp

        const float EmoticonSize = 0.4f;
        const float StompDrop = 0.22f;
        const float StompHold = 0.95f;
        const float StompLift = 0.3f;

        void BuildStomp()
        {
            var art = m_Art;
            float emo = EmoticonSize * m_Size;
            m_Body = UiFactory.Rect("Emoticon", m_Logo);
            m_Body.anchorMin = m_Body.anchorMax = new Vector2(0.5f, 0.5f);
            m_Body.pivot = new Vector2(0.5f, 0f);
            m_Body.sizeDelta = new Vector2(emo, emo);
            m_Body.anchoredPosition = BodyOrigin();
            if (art.logo != null)
            {
                m_BodyImage = UiFactory.Image("Art", m_Body, Color.white, art.logo);
                m_BodyImage.preserveAspect = true;
                UiFactory.Stretch(m_BodyImage.rectTransform);
            }
            else
            {
                m_BodyImage = UiFactory.Image("Face", m_Body, UIPalette.Hex(0xFFD43B), RuntimeSprites.Circle);
                UiFactory.Stretch(m_BodyImage.rectTransform);
                Part("EyeL", m_BodyImage.rectTransform, RuntimeSprites.Circle, UIPalette.Hex(0x18181B), new Vector2(0.15f, 0.15f) * emo, new Vector2(-0.17f, 0.1f) * emo);
                Part("EyeR", m_BodyImage.rectTransform, RuntimeSprites.Circle, UIPalette.Hex(0x18181B), new Vector2(0.15f, 0.15f) * emo, new Vector2(0.17f, 0.1f) * emo);
                Part("Mouth", m_BodyImage.rectTransform, RuntimeSprites.Square, UIPalette.Hex(0x18181B), new Vector2(0.34f, 0.07f) * emo, new Vector2(0f, -0.2f) * emo);
            }

            float bootSize = art.partSize * m_Size;
            m_Boot = UiFactory.Rect("Boot", m_Logo);
            m_Boot.anchorMin = m_Boot.anchorMax = new Vector2(0.5f, 0.5f);
            m_Boot.pivot = new Vector2(0.5f, 0f);
            m_Boot.sizeDelta = new Vector2(bootSize, bootSize);
            if (art.partA != null)
            {
                var boot = UiFactory.Image("Art", m_Boot, Color.white, art.partA);
                boot.preserveAspect = true;
                UiFactory.Stretch(boot.rectTransform);
            }
            else
            {
                // Placeholder boot: a shaft and a wide sole, toe to the right.
                var dark = UIPalette.Hex(0x2B2B30);
                var shaft = UiFactory.Image("Shaft", m_Boot, dark, RuntimeSprites.Square);
                UiFactory.Place(shaft.rectTransform, new Vector2(0.18f, 0.22f), new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero);
                var sole = UiFactory.Image("Sole", m_Boot, dark, RuntimeSprites.Rounded, true);
                UiFactory.Place(sole.rectTransform, new Vector2(0.08f, 0f), new Vector2(0.98f, 0.34f), Vector2.zero, Vector2.zero);
                var cuff = UiFactory.Image("Cuff", m_Boot, art.accent.WithAlpha(1f), RuntimeSprites.Square);
                UiFactory.Place(cuff.rectTransform, new Vector2(0.14f, 0.86f), new Vector2(0.62f, 1f), Vector2.zero, Vector2.zero);
            }

            m_Impact = UiFactory.Rect("Impact", m_Logo);
            m_Impact.anchorMin = m_Impact.anchorMax = new Vector2(0.5f, 0.5f);
            m_Impact.sizeDelta = new Vector2(0.9f, 0.5f) * m_Size;
            m_Impact.anchoredPosition = Point(art.pointA);
            if (art.partB != null)
            {
                var impact = UiFactory.Image("Art", m_Impact, Color.white, art.partB);
                impact.preserveAspect = true;
                UiFactory.Stretch(impact.rectTransform);
            }
            else
            {
                // Placeholder impact: strokes bursting out to both sides.
                for (int i = 0; i < 6; i++)
                {
                    bool right = i >= 3;
                    float angle = (right ? 0f : 180f) + (i % 3 - 1) * 30f;
                    var stroke = UiFactory.Image("Burst" + (i + 1), m_Impact, UIPalette.Hex(0xFF8A00), RuntimeSprites.Square);
                    var rect = stroke.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.sizeDelta = new Vector2(0.14f * m_Size, Mathf.Max(2f, 0.035f * m_Size));
                    rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                    rect.anchoredPosition = new Vector2(right ? 0.24f : -0.24f, 0f) * m_Size;
                }
            }
            m_Impact.gameObject.SetActive(false);
        }

        void AnimateStomp()
        {
            float loop = StompHold + StompLift + 0.1f + Mathf.Max(0f, m_Art.rest);
            if (m_Clock >= loop) m_Clock -= loop;
            float t = m_Clock;

            float emo = EmoticonSize * m_Size;
            float lift = m_Art.travel * m_Size;
            float floor = BodyOrigin().y;
            float contact = floor + emo;

            // Emoticon squash: flattened on impact, held, then springs back (with overshoot) as the boot leaves.
            float squash = 1f;
            if (t >= StompDrop && t < StompDrop + 0.08f) squash = Mathf.Lerp(1f, 0.52f, EaseOut((t - StompDrop) / 0.08f));
            else if (t >= StompDrop + 0.08f && t < StompHold) squash = 0.52f;
            else if (t >= StompHold && t < StompHold + 0.45f)
            {
                float k = (t - StompHold) / 0.45f;
                squash = 1f - 0.48f * Mathf.Exp(-4.5f * k) * Mathf.Cos(9f * k);
            }
            float widen = 1f + (1f - squash) * 0.65f;
            m_Body.localScale = new Vector3(widen, squash, 1f);
            float top = floor + emo * squash;

            // Boot: drops (accelerating), rides the squashed face, lifts (decelerating).
            float bootY;
            if (t < StompDrop) bootY = Mathf.Lerp(contact + lift, contact, t / StompDrop * (t / StompDrop));
            else if (t < StompHold) bootY = top;
            else if (t < StompHold + StompLift) bootY = Mathf.Lerp(top, contact + lift, EaseOut((t - StompHold) / StompLift));
            else bootY = contact + lift;
            bootY = Mathf.Max(bootY, top);
            m_Boot.anchoredPosition = new Vector2(0.04f * m_Size, bootY);
            // A small squash of the boot itself on impact.
            float bootSquash = t >= StompDrop && t < StompDrop + 0.16f ? 1f - 0.1f * Mathf.Sin((t - StompDrop) / 0.16f * Mathf.PI) : 1f;
            m_Boot.localScale = new Vector3(2f - bootSquash, bootSquash, 1f);

            // The impact effect shows on contact and hides before the boot lifts.
            if (m_Impact != null)
            {
                bool show = t >= StompDrop && t < StompDrop + 0.3f;
                m_Impact.gameObject.SetActive(show);
                if (show)
                {
                    float k = (t - StompDrop) / 0.3f;
                    float pop = Mathf.Lerp(0.7f, 1.1f, EaseOut(Mathf.Clamp01(k * 3f)));
                    m_Impact.localScale = new Vector3(pop, pop, 1f);
                }
            }
        }

        // ================================================================== Swipe

        const float SweepTime = 1.6f;

        void BuildSwipe()
        {
            var art = m_Art;
            var light = art.logo != null ? art.logo : FaceLight;
            var dark = art.partA != null ? art.partA : FaceDark;

            m_Body = UiFactory.Rect("Clock", m_Logo);
            UiFactory.Stretch(m_Body);
            m_FaceBase = UiFactory.Image("FaceBase", m_Body, Color.white, dark);
            m_FaceBase.preserveAspect = true;
            UiFactory.Stretch(m_FaceBase.rectTransform);
            m_FaceReveal = UiFactory.Image("FaceReveal", m_Body, Color.white, light);
            m_FaceReveal.preserveAspect = true;
            m_FaceReveal.type = Image.Type.Filled;
            m_FaceReveal.fillMethod = Image.FillMethod.Radial360;
            m_FaceReveal.fillOrigin = (int)Image.Origin360.Top;
            m_FaceReveal.fillClockwise = true;
            m_FaceReveal.fillAmount = 0f;
            UiFactory.Stretch(m_FaceReveal.rectTransform);

            m_HandImage = UiFactory.Image("Hand", m_Body, Color.white, RuntimeSprites.Square);
            m_HandImage.preserveAspect = false;
            m_Hand = m_HandImage.rectTransform;
            m_Hand.anchorMin = m_Hand.anchorMax = new Vector2(0.5f, 0.5f);
            m_Hand.pivot = new Vector2(0.5f, 0f);
            m_Hand.anchoredPosition = Vector2.zero;
            float length = Mathf.Max(0.05f, art.partSize) * m_Size;
            m_Hand.sizeDelta = new Vector2(Mathf.Max(3f, (art.partB != null ? 0.16f : 0.05f) * m_Size), length);
            m_HandCap = UiFactory.Image("Cap", m_Body, Color.white, RuntimeSprites.Circle);
            m_HandCap.rectTransform.anchorMin = m_HandCap.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            m_HandCap.rectTransform.sizeDelta = new Vector2(0.1f, 0.1f) * m_Size;
            if (art.partB != null) m_HandCap.enabled = false;

            m_LightIsBase = false;
            ApplySwipeFaces();
        }

        /// <summary>Base = the face before the sweep, reveal = the face the hand paints; the hand takes the base face's marker colour.</summary>
        void ApplySwipeFaces()
        {
            var art = m_Art;
            var light = art.logo != null ? art.logo : FaceLight;
            var dark = art.partA != null ? art.partA : FaceDark;
            m_FaceBase.sprite = m_LightIsBase ? light : dark;
            m_FaceReveal.sprite = m_LightIsBase ? dark : light;
            m_FaceReveal.fillAmount = 0f;

            // On a dark face the markers are white, so the hand is white; on a light face it's black.
            bool whiteHand = !m_LightIsBase;
            Sprite sprite;
            Color tint;
            if (art.partB != null && art.partC != null)
            {
                sprite = whiteHand ? art.partC : art.partB;
                tint = Color.white;
            }
            else
            {
                // One hand sprite (drawn white) or the placeholder bar: tinted.
                sprite = art.partB != null ? art.partB : RuntimeSprites.Square;
                tint = whiteHand ? Color.white : UIPalette.Hex(0x0E0E10);
            }
            m_HandImage.sprite = sprite;
            m_HandImage.preserveAspect = art.partB != null;
            m_HandImage.color = tint;
            if (m_HandCap != null) m_HandCap.color = tint;
        }

        void AnimateSwipe()
        {
            float sweep = SweepTime;
            float hold = Mathf.Max(0f, m_Art.rest);
            if (m_Sweeping)
            {
                float k = Mathf.Clamp01(m_Clock / sweep);
                // Mostly steady like a clock, easing in and out a touch.
                float p = Mathf.Lerp(k, k * k * (3f - 2f * k), 0.35f);
                m_FaceReveal.fillAmount = p;
                m_Hand.localRotation = Quaternion.Euler(0f, 0f, -360f * p);
                if (m_Clock >= sweep)
                {
                    // The swept face becomes the base, and the hand switches colour.
                    m_LightIsBase = !m_LightIsBase;
                    ApplySwipeFaces();
                    m_Hand.localRotation = Quaternion.identity;
                    m_Sweeping = false;
                    m_Clock = 0f;
                }
            }
            else if (m_Clock >= hold)
            {
                m_Sweeping = true;
                m_Clock = 0f;
            }

            // A small tick of the whole clock at the top of each sweep.
            float pulse = m_Sweeping && m_Clock < 0.12f ? 1f + 0.05f * Mathf.Sin(m_Clock / 0.12f * Mathf.PI) : 1f;
            m_Body.localScale = new Vector3(pulse, pulse, 1f);
        }

        static Sprite FaceLight => s_FaceLight != null ? s_FaceLight : (s_FaceLight = ClockFace("dp_clock_light", Color.white, UIPalette.Hex(0x0E0E10)));
        static Sprite FaceDark => s_FaceDark != null ? s_FaceDark : (s_FaceDark = ClockFace("dp_clock_dark", UIPalette.Hex(0x0E0E10), Color.white));

        /// <summary>A placeholder clock face: a disc with a rim and twelve hour markers.</summary>
        static Sprite ClockFace(string name, Color face, Color marks)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    Color sum = Color.clear;
                    for (int s = 0; s < 4; s++)
                    {
                        float x = (px + 0.25f + (s & 1) * 0.5f) / size * 2f - 1f;
                        float y = (py + 0.25f + (s >> 1) * 0.5f) / size * 2f - 1f;
                        float r = Mathf.Sqrt(x * x + y * y);
                        Color c = Color.clear;
                        if (r <= 0.97f)
                        {
                            c = r >= 0.87f ? marks : face;
                            // Hour markers: twelve short strokes inside the rim (longer at 12, 3, 6, 9).
                            float angle = Mathf.Atan2(x, y) / (Mathf.PI * 2f) * 12f;
                            float nearest = Mathf.Round(angle);
                            bool major = ((int)nearest % 3 + 3) % 3 == 0;
                            float width = Mathf.Abs(angle - nearest) * r * Mathf.PI * 2f / 12f;
                            if (r < 0.84f && r > (major ? 0.62f : 0.7f) && width < 0.035f) c = marks;
                        }
                        sum += c;
                    }
                    pixels[py * size + px] = sum * 0.25f;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        // ================================================================== Post / Tunes

        void BuildGeneric()
        {
            var art = m_Art;
            m_Body = Body(art.logo, 0.82f, out m_BodyImage);
            if (art.logo != null) return;

            var accent = art.accent.WithAlpha(1f);
            var content = art.accentContent.WithAlpha(1f);
            if (m_Platform == StreamPlatform.Tunes)
            {
                // A disc with three sound bars.
                m_BodyImage.sprite = RuntimeSprites.Circle;
                m_BodyImage.color = accent;
                for (int i = 0; i < 3; i++)
                {
                    float w = (0.5f - i * 0.1f) * m_Size;
                    Part("Bar" + (i + 1), m_Body, RuntimeSprites.Rounded, content, new Vector2(w, 0.07f * m_Size), new Vector2(0f, (0.14f - i * 0.14f) * m_Size)).GetComponent<Image>().type = Image.Type.Sliced;
                }
            }
            else
            {
                // A rounded square with a speech bubble.
                m_BodyImage.sprite = RuntimeSprites.Rounded;
                m_BodyImage.type = Image.Type.Sliced;
                m_BodyImage.color = accent;
                var bubble = Part("Bubble", m_Body, RuntimeSprites.Rounded, content, new Vector2(0.5f, 0.34f) * m_Size, new Vector2(0f, 0.04f) * m_Size);
                bubble.GetComponent<Image>().type = Image.Type.Sliced;
                Part("Tail", m_Body, RuntimeSprites.Square, content, new Vector2(0.1f, 0.1f) * m_Size, new Vector2(-0.12f, -0.15f) * m_Size).localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        void AnimateGeneric()
        {
            const float hop = 0.8f;
            float loop = hop + Mathf.Max(0.2f, m_Art.rest);
            if (m_Clock >= loop) m_Clock -= loop;
            float t = m_Clock;

            float sx = 1f, sy = 1f, y = 0f;
            if (t < 0.12f)
            {
                float k = EaseOut(t / 0.12f);
                sx = Mathf.Lerp(1f, 1.1f, k);
                sy = Mathf.Lerp(1f, 0.88f, k);
            }
            else if (t < 0.32f)
            {
                float k = EaseOut((t - 0.12f) / 0.2f);
                sx = Mathf.Lerp(1.1f, 0.92f, k);
                sy = Mathf.Lerp(0.88f, 1.1f, k);
                y = 0.12f * m_Size * k;
            }
            else if (t < 0.5f)
            {
                float k = (t - 0.32f) / 0.18f;
                sx = Mathf.Lerp(0.92f, 1f, k);
                sy = Mathf.Lerp(1.1f, 1f, k);
                y = 0.12f * m_Size * (1f - k * k);
            }
            else if (t < hop)
            {
                float k = (t - 0.5f) / (hop - 0.5f);
                float e = Mathf.Exp(-5f * k) * Mathf.Cos(12f * k);
                sx = 1f + 0.1f * e;
                sy = 1f - 0.1f * e;
            }
            else
            {
                float breath = 1f + 0.012f * Mathf.Sin(m_Time * 2f);
                sx = 1f / breath;
                sy = breath;
            }
            m_Body.anchoredPosition = BodyOrigin() + new Vector2(0f, y);
            m_Body.localScale = new Vector3(sx, sy, 1f);
        }

        // ================================================================== helpers

        /// <summary>
        /// The main sprite in a holder that pivots at the box's bottom-centre, so squashes sit on the ground. Parts added to the
        /// holder are positioned from its centre (<see cref="InBody"/> converts box positions).
        /// </summary>
        RectTransform Body(Sprite sprite, float scale, out Image image)
        {
            m_BodyScale = scale;
            var rect = UiFactory.Rect("Body", m_Logo);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(m_Size, m_Size) * scale;
            rect.anchoredPosition = BodyOrigin();
            image = UiFactory.Image("Art", rect, Color.white, sprite);
            image.preserveAspect = sprite != null;
            UiFactory.Stretch(image.rectTransform);
            return rect;
        }

        float m_BodyScale = 1f;

        /// <summary>A box position (pixels from the box centre) as a position inside the body holder (pixels from its centre).</summary>
        Vector2 InBody(Vector2 boxPosition) => boxPosition - new Vector2(0f, -0.5f * m_Size + 0.5f * m_Size * m_BodyScale);

        /// <summary>A part centred at <paramref name="position"/> (pixels from the parent's centre).</summary>
        static RectTransform Part(string name, Transform parent, Sprite sprite, Color color, Vector2 size, Vector2 position)
        {
            var image = UiFactory.Image(name, parent, color, sprite);
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        /// <summary>A point in the logo box (0..1 each way) in pixels from the box centre.</summary>
        Vector2 Point(Vector2 fraction) => (fraction - new Vector2(0.5f, 0.5f)) * m_Size;

        static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        static float EaseInOut(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
