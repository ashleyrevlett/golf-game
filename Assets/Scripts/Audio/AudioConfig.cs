using UnityEngine;

namespace GolfGame.Audio
{
    /// <summary>
    /// ScriptableObject holding audio clip references and volume settings.
    /// All clip fields are nullable — game runs silently without assets.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Golf/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Ball SFX")]
        [SerializeField] private AudioClip ballHit;
        [SerializeField] private AudioClip ballBounce;
        [SerializeField] private AudioClip ballBounceRough;
        [SerializeField] private AudioClip ballRoll;
        [SerializeField] private AudioClip ballStop;

        [Header("Environment")]
        [SerializeField] private AudioClip windAmbience;
        [SerializeField] private AudioClip crowdReaction;

        [Header("UI")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip scoreReveal;

        [Header("Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private float ambientVolume = 0.5f;
        [SerializeField] private float crowdReactionDistanceThreshold = 3f;

        public AudioClip BallHit => ballHit;
        public AudioClip BallBounce => ballBounce;
        public AudioClip BallBounceRough => ballBounceRough;
        public AudioClip BallRoll => ballRoll;
        public AudioClip BallStop => ballStop;
        public AudioClip WindAmbience => windAmbience;
        public AudioClip CrowdReaction => crowdReaction;
        public AudioClip ButtonClick => buttonClick;
        public AudioClip ScoreReveal => scoreReveal;
        public float MasterVolume => masterVolume;
        public float SfxVolume => sfxVolume;
        public float AmbientVolume => ambientVolume;
        public float CrowdReactionDistanceThreshold => crowdReactionDistanceThreshold;
    }
}
