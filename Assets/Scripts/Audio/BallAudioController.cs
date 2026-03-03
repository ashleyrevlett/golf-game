using UnityEngine;
using GolfGame.Core;
using GolfGame.Golf;

namespace GolfGame.Audio
{
    /// <summary>
    /// Plays ball sound effects: hit, bounce, roll, and stop.
    /// Subscribes to BallController and GameManager events.
    /// </summary>
    public class BallAudioController : MonoBehaviour
    {
        [SerializeField] private BallController ballController;
        [SerializeField] private GameManager gameManager;

        private AudioSource rollSource;
        private float lastLaunchPower;

        private void Start()
        {
            if (gameManager != null)
            {
                gameManager.OnShotStateChanged += HandleShotStateChanged;
            }

            if (ballController != null)
            {
                ballController.OnBallBounced += HandleBallBounced;
                ballController.OnBallLanded += HandleBallLanded;
            }
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnShotStateChanged -= HandleShotStateChanged;
            }

            if (ballController != null)
            {
                ballController.OnBallBounced -= HandleBallBounced;
                ballController.OnBallLanded -= HandleBallLanded;
            }
        }

        private void HandleShotStateChanged(ShotState state)
        {
            if (state == ShotState.Flying)
            {
                PlayHitSound();
            }
        }

        private void HandleBallBounced(Vector3 position, float speed)
        {
            PlayBounceSound(speed);
        }

        private void HandleBallLanded(Vector3 position)
        {
            StopRollSound();
            PlayStopSound();
        }

        private void PlayHitSound()
        {
            if (AudioManager.Instance == null) return;
            var config = AudioManager.Instance.Config;
            if (config == null) return;

            // Pitch scales slightly with power (0.8 to 1.2 range)
            float pitch = Mathf.Lerp(0.8f, 1.2f, lastLaunchPower);
            AudioManager.Instance.PlaySFX(config.BallHit, 1f, pitch);
        }

        private void PlayBounceSound(float speed)
        {
            if (AudioManager.Instance == null) return;
            var config = AudioManager.Instance.Config;
            if (config == null) return;

            // Volume scales with impact speed
            float volume = Mathf.Clamp01(speed / 20f);
            AudioManager.Instance.PlaySFX(config.BallBounce, volume);

            // Start roll loop if not already playing
            if (rollSource == null || !rollSource.isPlaying)
            {
                rollSource = AudioManager.Instance.PlayLoop(config.BallRoll, 0.3f);
            }
        }

        private void PlayStopSound()
        {
            if (AudioManager.Instance == null) return;
            var config = AudioManager.Instance.Config;
            if (config == null) return;

            AudioManager.Instance.PlaySFX(config.BallStop, 0.5f);
        }

        private void StopRollSound()
        {
            if (rollSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopSource(rollSource);
                rollSource = null;
            }
        }

        /// <summary>
        /// Set the launch power for pitch scaling. Called before launch.
        /// </summary>
        public void SetLaunchPower(float normalizedPower)
        {
            lastLaunchPower = Mathf.Clamp01(normalizedPower);
        }
    }
}
