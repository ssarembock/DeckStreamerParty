using System;
using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeckParty.UI
{
    /// <summary>
    /// The fake live chat (nice-to-have N3): a Twitch-style feed that reacts to what happens on the board with meme spam
    /// (keys "fakechat.*", localized). Purely cosmetic. Fills this RectTransform.
    /// </summary>
    public sealed class FakeChatView : MonoBehaviour
    {
        const int MaxLines = 9;
        static readonly string[] s_Names =
        {
            "xX_Sniper_Xx", "PogFrog", "chatGPTea", "lurker42", "MemeLord", "kappa_kid", "NoScopeNana", "copium_dealer",
            "W_enjoyer", "ratio_bot", "ZoomerZed", "BrainrotBob", "sigma_grind", "TouchGrass", "modsAsleep", "LUL_hunter",
        };
        static readonly Color[] s_NameColors =
        {
            new Color32(255, 79, 139, 255), new Color32(61, 219, 217, 255), new Color32(255, 201, 74, 255), new Color32(167, 123, 255, 255),
            new Color32(107, 227, 139, 255), new Color32(255, 154, 61, 255), new Color32(90, 168, 255, 255),
        };

        TMP_Text m_Text;
        readonly Queue<string> m_Lines = new Queue<string>();
        float m_NextAmbient;
        bool m_Built;

        void Build()
        {
            if (m_Built) return;
            m_Built = true;
            var bg = UiFactory.Image("Background", transform, Color.black.WithAlpha(0.45f), RuntimeSprites.Rounded, true);
            UiFactory.Stretch(bg.rectTransform);
            m_Text = UiFactory.Text("Lines", transform, 18f, Color.white, TextAlignmentOptions.BottomLeft);
            m_Text.enableAutoSizing = false;
            m_Text.overflowMode = TextOverflowModes.Masking;
            UiFactory.Stretch(m_Text.rectTransform, 10f);
            m_NextAmbient = Time.unscaledTime + 2f;
        }

        void Awake() => Build();

        void Update()
        {
            bool on = FxLibrary.Current.fakeChat && AppSettings.FakeChat;
            if (m_Text != null) m_Text.enabled = on;
            if (!on) return;
            if (Time.unscaledTime >= m_NextAmbient)
            {
                m_NextAmbient = Time.unscaledTime + UnityEngine.Random.Range(2.5f, 6f);
                Say("ambient");
            }
        }

        /// <summary>Chat reacts to a game event (a few lines, slightly staggered).</summary>
        public void React(GameEventType type)
        {
            string pool;
            switch (type)
            {
                case GameEventType.GuestArrived: pool = "join"; break;
                case GameEventType.PoliceRaid: pool = "swatted"; break;
                case GameEventType.FireMarshal: pool = "crashed"; break;
                case GameEventType.PartyEnded: pool = "ended"; break;
                case GameEventType.Victory: pool = "victory"; break;
                case GameEventType.Defeat: pool = "defeat"; break;
                case GameEventType.AbilityUsed: pool = "action"; break;
                case GameEventType.GuestRemoved: pool = "kick"; break;
                case GameEventType.TroubleAdded: pool = "risk"; break;
                case GameEventType.GuestPurchased: pool = "buy"; break;
                case GameEventType.PowerUsed: pool = "power"; break;
                default: return;
            }
            int count = type == GameEventType.PoliceRaid || type == GameEventType.Victory ? 4 : 1 + UnityEngine.Random.Range(0, 2);
            for (int i = 0; i < count; i++) Say(pool);
        }

        void Say(string pool)
        {
            Build();
            if (!(FxLibrary.Current.fakeChat && AppSettings.FakeChat)) return;
            // Each pool has keys fakechat.<pool>.0 ... until one is missing.
            int options = 0;
            while (options < 12 && Loc.Has($"fakechat.{pool}.{options}")) options++;
            if (options == 0) return;
            string line = Loc.Get($"fakechat.{pool}.{UnityEngine.Random.Range(0, options)}");
            int who = UnityEngine.Random.Range(0, s_Names.Length);
            m_Lines.Enqueue($"<b>{GuestVisuals.Colored(s_Names[who], s_NameColors[who % s_NameColors.Length])}</b>: {line}");
            while (m_Lines.Count > MaxLines) m_Lines.Dequeue();
            m_Text.text = string.Join("\n", m_Lines);
        }
    }
}
