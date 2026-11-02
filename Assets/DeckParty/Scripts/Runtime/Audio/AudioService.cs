using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Persistent music + SFX player. Created on demand; no scene setup required.
    /// Audio juice (FX Library > audioJuice): music ducks under big moments and goes muffled while a menu is open.
    /// Musical hover (FX Library > musicalHover): hover ticks rise in pitch across a row of cards.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        static AudioService s_Instance;

        AudioSource m_Music;
        AudioSource m_Sfx;
        float m_LastHoverTime;
        AudioClip m_TargetMusic;
        float m_MusicFade = 1f;
        AudioSource m_Pitched;
        AudioLowPassFilter m_LowPass;
        float m_Duck = 1f;
        float m_DuckUntil;
        bool m_Muffled;

        static AudioService Instance
        {
            get
            {
                if (s_Instance == null && Application.isPlaying)
                {
                    var go = new GameObject("[AudioService]");
                    DontDestroyOnLoad(go);
                    s_Instance = go.AddComponent<AudioService>();
                }
                return s_Instance;
            }
        }

        void Awake()
        {
            m_Music = gameObject.AddComponent<AudioSource>();
            m_Music.loop = true;
            m_Music.playOnAwake = false;
            m_Sfx = gameObject.AddComponent<AudioSource>();
            m_Sfx.playOnAwake = false;
            m_Pitched = gameObject.AddComponent<AudioSource>();
            m_Pitched.playOnAwake = false;
            m_LowPass = gameObject.AddComponent<AudioLowPassFilter>();
            m_LowPass.cutoffFrequency = 22000f;
            AppSettings.Changed += ApplyVolumes;
            ApplyVolumes();
        }

        void OnDestroy()
        {
            AppSettings.Changed -= ApplyVolumes;
            if (s_Instance == this) s_Instance = null;
        }

        void ApplyVolumes()
        {
            if (m_Music != null) m_Music.volume = AppSettings.MusicVolume * m_MusicFade;
        }

        void Update()
        {
            // Simple fade-out / swap / fade-in between music tracks.
            if (m_Music.clip != m_TargetMusic)
            {
                m_MusicFade = Mathf.MoveTowards(m_MusicFade, 0f, Time.unscaledDeltaTime * 2f);
                if (m_MusicFade <= 0f || !m_Music.isPlaying)
                {
                    m_Music.clip = m_TargetMusic;
                    if (m_TargetMusic != null) m_Music.Play();
                    else m_Music.Stop();
                }
            }
            else if (m_MusicFade < 1f)
            {
                m_MusicFade = Mathf.MoveTowards(m_MusicFade, 1f, Time.unscaledDeltaTime * 1.5f);
            }

            bool juice = FxLibrary.Current.audioJuice;
            float duckTarget = juice && Time.unscaledTime < m_DuckUntil ? 0.35f : 1f;
            m_Duck = Mathf.MoveTowards(m_Duck, duckTarget, Time.unscaledDeltaTime * (duckTarget < m_Duck ? 6f : 1.2f));
            m_Music.volume = AppSettings.MusicVolume * m_MusicFade * m_Duck;

            // The low-pass filter sits on this object, so it muffles everything it plays: only engage it under a menu.
            float cutoff = juice && m_Muffled ? 900f : 22000f;
            m_LowPass.enabled = cutoff < 21000f || m_LowPass.cutoffFrequency < 21000f;
            m_LowPass.cutoffFrequency = Mathf.MoveTowards(m_LowPass.cutoffFrequency, cutoff, Time.unscaledDeltaTime * 40000f);
        }

        /// <summary>Ducks the music for a moment so a big sound (DEPLATFORMED!, a Diamond, a win) lands.</summary>
        public static void Duck(float seconds = 1.2f)
        {
            var service = Instance;
            if (service != null) service.m_DuckUntil = Mathf.Max(service.m_DuckUntil, Time.unscaledTime + seconds);
        }

        /// <summary>Muffles the music while a menu is open (audio juice).</summary>
        public static void SetMuffled(bool muffled)
        {
            var service = Instance;
            if (service != null) service.m_Muffled = muffled;
        }

        /// <summary>A hover tick whose pitch climbs with the card's position in its row (musical hover).</summary>
        public static void PlayHoverTick(int index)
        {
            var service = Instance;
            if (service == null || Time.unscaledTime - service.m_LastHoverTime < 0.05f) return;
            service.m_LastHoverTime = Time.unscaledTime;
            if (!FxLibrary.Current.musicalHover)
            {
                service.PlayInternal(Sfx.Hover, 0.8f);
                return;
            }
            // A pentatonic step per card, wrapping every two octaves.
            int[] steps = { 0, 2, 4, 7, 9 };
            int i = Mathf.Abs(index) % 10;
            int semitones = steps[i % 5] + 12 * (i / 5);
            service.m_Pitched.pitch = Mathf.Pow(2f, semitones / 12f);
            service.PlayOn(service.m_Pitched, Sfx.Hover, 0.7f);
        }

        /// <summary>Plays a sound at a pitch (rising scoring ticks).</summary>
        public static void PlayPitched(Sfx sfx, float pitch, float volumeScale = 1f)
        {
            var service = Instance;
            if (service == null) return;
            service.m_Pitched.pitch = Mathf.Clamp(pitch, 0.5f, 3f);
            service.PlayOn(service.m_Pitched, sfx, volumeScale);
        }

        public static void Play(Sfx sfx, float volumeScale = 1f)
        {
            var service = Instance;
            if (service != null) service.PlayInternal(sfx, volumeScale);
        }

        public static void PlayHover()
        {
            var service = Instance;
            if (service == null || Time.unscaledTime - service.m_LastHoverTime < 0.06f) return;
            service.m_LastHoverTime = Time.unscaledTime;
            service.PlayInternal(Sfx.Hover, 1f);
        }

        /// <summary>Crossfades to a music clip. Null fades to silence.</summary>
        public static void PlayMusic(AudioClip clip)
        {
            var service = Instance;
            if (service != null) service.m_TargetMusic = clip;
        }

        void PlayInternal(Sfx sfx, float volumeScale) => PlayOn(m_Sfx, sfx, volumeScale);

        void PlayOn(AudioSource source, Sfx sfx, float volumeScale)
        {
            AudioClip clip = null;
            float volume = 1f;

            var library = GameDatabase.Instance.audio;
            if (library != null)
            {
                var entry = library.Get(sfx);
                if (entry != null && entry.clip != null)
                {
                    clip = entry.clip;
                    volume = entry.volume;
                }
            }

            if (clip == null) clip = ProceduralAudio.Get(sfx);
            source.PlayOneShot(clip, volume * volumeScale * AppSettings.SfxVolume);
        }

        public static AudioClip MenuMusic()
        {
            var library = GameDatabase.Instance.audio;
            return library != null ? library.menuMusic : null;
        }

        public static AudioClip PartyMusic(VenueDefinition venue)
        {
            if (venue != null && venue.music != null) return venue.music;
            var library = GameDatabase.Instance.audio;
            return library != null ? library.partyMusic : null;
        }

        public static AudioClip PlanningMusic()
        {
            var library = GameDatabase.Instance.audio;
            return library != null && library.planningMusic != null ? library.planningMusic : null;
        }
    }
}
