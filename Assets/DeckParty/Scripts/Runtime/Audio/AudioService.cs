using UnityEngine;

namespace DeckParty
{
    /// <summary>Persistent music + SFX player. Created on demand; no scene setup required.</summary>
    public sealed class AudioService : MonoBehaviour
    {
        static AudioService s_Instance;

        AudioSource m_Music;
        AudioSource m_Sfx;
        float m_LastHoverTime;
        AudioClip m_TargetMusic;
        float m_MusicFade = 1f;

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
            m_Music.volume = AppSettings.MusicVolume * m_MusicFade;
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

        void PlayInternal(Sfx sfx, float volumeScale)
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
            m_Sfx.PlayOneShot(clip, volume * volumeScale * AppSettings.SfxVolume);
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
