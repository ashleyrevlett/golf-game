using UnityEngine;

namespace GolfGame.Audio
{
    /// <summary>
    /// Centralized audio control with pooled AudioSources.
    /// Handles WebGL autoplay policy via first-gesture initialization.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private AudioConfig config;

        [Header("Pool")]
        [SerializeField] private int poolSize = 8;

        private AudioSource[] pool;
        private int nextPoolIndex;
        private bool audioInitialized;

        /// <summary>
        /// Audio configuration reference.
        /// </summary>
        public AudioConfig Config => config;

        /// <summary>
        /// Number of AudioSources in the pool.
        /// </summary>
        public int PoolSize => pool != null ? pool.Length : 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreatePool();
        }

        private void Start()
        {
            float savedVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
            SetMasterVolume(savedVolume);
        }

        private void CreatePool()
        {
            pool = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var sourceObj = new GameObject($"AudioSource_{i}");
                sourceObj.transform.SetParent(transform);
                pool[i] = sourceObj.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }
        }

        /// <summary>
        /// Initialize audio on first user gesture (WebGL autoplay policy).
        /// </summary>
        public void OnFirstUserGesture()
        {
            if (audioInitialized) return;
            audioInitialized = true;

            float savedVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
            AudioListener.volume = savedVolume;

            Debug.Log("[AudioManager] Audio initialized on user gesture");
        }

        /// <summary>
        /// Set the master volume.
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = volume;
        }

        /// <summary>
        /// Play a one-shot sound effect.
        /// Returns the AudioSource used, or null if clip is null.
        /// </summary>
        public AudioSource PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return null;

            var source = GetAvailableSource();
            if (source == null) return null;

            float sfxVol = config != null ? config.SfxVolume : 0.8f;
            source.clip = clip;
            source.volume = volume * sfxVol;
            source.pitch = pitch;
            source.loop = false;
            source.Play();

            return source;
        }

        /// <summary>
        /// Play a looping sound. Returns the AudioSource for stop control.
        /// Returns null if clip is null.
        /// </summary>
        public AudioSource PlayLoop(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return null;

            var source = GetAvailableSource();
            if (source == null) return null;

            float ambientVol = config != null ? config.AmbientVolume : 0.5f;
            source.clip = clip;
            source.volume = volume * ambientVol;
            source.pitch = 1f;
            source.loop = true;
            source.Play();

            return source;
        }

        /// <summary>
        /// Stop a specific AudioSource.
        /// </summary>
        public void StopSource(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }
        }

        private AudioSource GetAvailableSource()
        {
            if (pool == null || pool.Length == 0) return null;

            // Find a non-playing source
            for (int i = 0; i < pool.Length; i++)
            {
                int idx = (nextPoolIndex + i) % pool.Length;
                if (!pool[idx].isPlaying)
                {
                    nextPoolIndex = (idx + 1) % pool.Length;
                    return pool[idx];
                }
            }

            // All playing — steal the oldest
            var stolen = pool[nextPoolIndex];
            stolen.Stop();
            nextPoolIndex = (nextPoolIndex + 1) % pool.Length;
            return stolen;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
