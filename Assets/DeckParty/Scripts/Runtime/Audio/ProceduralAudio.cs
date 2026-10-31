using System.Collections.Generic;
using UnityEngine;

namespace DeckParty
{
    /// <summary>Tiny synthesizer that generates placeholder sound effects so the game has feedback before real audio is added.</summary>
    public static class ProceduralAudio
    {
        const int Rate = 44100;

        enum Wave { Sine, Square, Triangle }

        static readonly Dictionary<Sfx, AudioClip> s_Cache = new Dictionary<Sfx, AudioClip>();

        public static AudioClip Get(Sfx sfx)
        {
            if (s_Cache.TryGetValue(sfx, out var clip) && clip != null) return clip;
            clip = Clip(sfx.ToString(), Build(sfx));
            s_Cache[sfx] = clip;
            return clip;
        }

        static float[] Build(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Click: return Tone(1200, 0.04f, 0.18f, 900, Wave.Square, 8);
                case Sfx.Hover: return Tone(1800, 0.025f, 0.05f, -1, Wave.Sine, 10);
                case Sfx.DoorKnock:
                {
                    var knock = Mix(Noise(0.05f, 0.25f, 30), Tone(170, 0.07f, 0.5f, 110, Wave.Sine, 18));
                    return Concat(knock, Silence(0.08f), knock);
                }
                case Sfx.GuestArrive: return Concat(Tone(660, 0.07f, 0.22f, -1, Wave.Triangle, 5), Tone(880, 0.1f, 0.22f, -1, Wave.Triangle, 5));
                case Sfx.StarArrive:
                    return Concat(Tone(784, 0.07f, 0.22f, -1, Wave.Triangle, 4), Tone(988, 0.07f, 0.22f, -1, Wave.Triangle, 4),
                        Tone(1175, 0.07f, 0.22f, -1, Wave.Triangle, 4), Tone(1568, 0.25f, 0.25f, -1, Wave.Triangle, 3));
                case Sfx.Boot: return Tone(520, 0.25f, 0.22f, 120, Wave.Square, 4);
                case Sfx.Ability: return Concat(Tone(520, 0.05f, 0.22f, 700, Wave.Sine, 3), Tone(880, 0.13f, 0.22f, 1250, Wave.Sine, 4));
                case Sfx.Coin: return Concat(Tone(988, 0.06f, 0.18f, -1, Wave.Square, 6), Tone(1319, 0.18f, 0.18f, -1, Wave.Square, 5));
                case Sfx.Purchase:
                    return Mix(Concat(Tone(988, 0.06f, 0.16f, -1, Wave.Square, 6), Tone(1319, 0.18f, 0.16f, -1, Wave.Square, 5)),
                        Tone(660, 0.3f, 0.12f, -1, Wave.Sine, 3));
                case Sfx.Expand:
                    return Concat(Tone(330, 0.09f, 0.22f, -1, Wave.Square, 4), Tone(440, 0.09f, 0.22f, -1, Wave.Square, 4),
                        Tone(554, 0.09f, 0.22f, -1, Wave.Square, 4), Tone(659, 0.25f, 0.22f, -1, Wave.Square, 3));
                case Sfx.PartySuccess:
                    return Concat(Tone(523, 0.09f, 0.25f, -1, Wave.Triangle, 3), Tone(659, 0.09f, 0.25f, -1, Wave.Triangle, 3),
                        Tone(784, 0.09f, 0.25f, -1, Wave.Triangle, 3), Tone(1047, 0.35f, 0.27f, -1, Wave.Triangle, 2));
                case Sfx.Raid:
                {
                    var up = Tone(700, 0.22f, 0.2f, 960, Wave.Square, 0.4f);
                    var down = Tone(960, 0.22f, 0.2f, 700, Wave.Square, 0.4f);
                    return Concat(up, down, up, down, up, down);
                }
                case Sfx.FireMarshal:
                    return Concat(Tone(220, 0.28f, 0.25f, -1, Wave.Square, 1), Silence(0.05f), Tone(220, 0.28f, 0.25f, -1, Wave.Square, 1),
                        Silence(0.05f), Tone(196, 0.45f, 0.25f, -1, Wave.Square, 1.5f));
                case Sfx.Victory:
                    return Concat(Tone(523, 0.1f, 0.25f, -1, Wave.Triangle, 2), Tone(659, 0.1f, 0.25f, -1, Wave.Triangle, 2),
                        Tone(784, 0.1f, 0.25f, -1, Wave.Triangle, 2), Tone(1047, 0.16f, 0.27f, -1, Wave.Triangle, 2),
                        Tone(784, 0.1f, 0.25f, -1, Wave.Triangle, 2), Tone(1047, 0.6f, 0.3f, -1, Wave.Triangle, 1.5f));
                case Sfx.Defeat:
                    return Concat(Tone(392, 0.25f, 0.22f, -1, Wave.Triangle, 2), Tone(370, 0.25f, 0.22f, -1, Wave.Triangle, 2),
                        Tone(349, 0.25f, 0.22f, -1, Wave.Triangle, 2), Tone(330, 0.7f, 0.24f, -1, Wave.Triangle, 1.5f));
                case Sfx.Error: return Concat(Tone(220, 0.08f, 0.22f, -1, Wave.Square, 6), Silence(0.03f), Tone(180, 0.12f, 0.22f, -1, Wave.Square, 6));
                case Sfx.Notify: return Concat(Tone(880, 0.06f, 0.16f, -1, Wave.Sine, 4), Tone(1320, 0.14f, 0.16f, -1, Wave.Sine, 4));
                case Sfx.Peek: return Tone(400, 0.2f, 0.2f, 900, Wave.Sine, 3);
                default: return Tone(800, 0.05f, 0.15f, -1, Wave.Sine, 6);
            }
        }

        static float[] Tone(float freq, float duration, float volume, float freqEnd, Wave wave, float decay)
        {
            int count = Mathf.Max(1, (int)(duration * Rate));
            var data = new float[count];
            double phase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                float progress = i / (float)count;
                float f = freqEnd > 0 ? Mathf.Lerp(freq, freqEnd, progress) : freq;
                phase += f / Rate;
                float p = (float)(phase - System.Math.Floor(phase));
                float sample;
                switch (wave)
                {
                    case Wave.Square: sample = p < 0.5f ? 0.6f : -0.6f; break;
                    case Wave.Triangle: sample = 1f - 4f * Mathf.Abs(p - 0.5f); break;
                    default: sample = Mathf.Sin(p * Mathf.PI * 2f); break;
                }
                float attack = Mathf.Clamp01(t / 0.004f);
                float release = Mathf.Clamp01((count - i) / (Rate * 0.006f));
                data[i] = sample * volume * attack * release * Mathf.Exp(-decay * progress);
            }
            return data;
        }

        static float[] Noise(float duration, float volume, float decay)
        {
            int count = Mathf.Max(1, (int)(duration * Rate));
            var data = new float[count];
            var rng = new System.Random(1234);
            float last = 0f;
            for (int i = 0; i < count; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, white, 0.25f);
                data[i] = last * volume * Mathf.Exp(-decay * (i / (float)count));
            }
            return data;
        }

        static float[] Silence(float duration) => new float[Mathf.Max(1, (int)(duration * Rate))];

        static float[] Concat(params float[][] parts)
        {
            int total = 0;
            foreach (var p in parts) total += p.Length;
            var result = new float[total];
            int offset = 0;
            foreach (var p in parts)
            {
                System.Array.Copy(p, 0, result, offset, p.Length);
                offset += p.Length;
            }
            return result;
        }

        static float[] Mix(params float[][] parts)
        {
            int length = 0;
            foreach (var p in parts) length = Mathf.Max(length, p.Length);
            var result = new float[length];
            foreach (var p in parts)
                for (int i = 0; i < p.Length; i++) result[i] += p[i];
            return result;
        }

        static AudioClip Clip(string name, float[] data)
        {
            var clip = AudioClip.Create("dp_" + name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            clip.hideFlags = HideFlags.DontSave;
            return clip;
        }
    }
}
