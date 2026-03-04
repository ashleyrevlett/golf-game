using UnityEngine;
using GolfGame.Core;
using GolfGame.Golf;
using GolfGame.Environment;

namespace GolfGame.Audio
{
    /// <summary>
    /// Controls ambient audio: wind ambience and crowd reactions.
    /// Wind volume scales with wind speed. Crowd triggers on close pin shots.
    /// </summary>
    public class AmbientAudioController : MonoBehaviour
    {
        private WindSystem windSystem;
        private ScoringManager scoringManager;
        private GameManager gameManager;

        private AudioSource windSource;

        private void Start()
        {
            windSystem = FindFirstObjectByType<WindSystem>();
            scoringManager = FindFirstObjectByType<ScoringManager>();
            gameManager = FindFirstObjectByType<GameManager>();

            if (windSystem != null)
            {
                windSystem.OnWindChanged += HandleWindChanged;
            }

            if (scoringManager != null)
            {
                scoringManager.OnShotScored += HandleShotScored;
            }

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged += HandleShotStateChanged;
                gameManager.OnGameOver += HandleGameOver;
            }
        }

        private void OnDestroy()
        {
            if (windSystem != null)
            {
                windSystem.OnWindChanged -= HandleWindChanged;
            }

            if (scoringManager != null)
            {
                scoringManager.OnShotScored -= HandleShotScored;
            }

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged -= HandleShotStateChanged;
                gameManager.OnGameOver -= HandleGameOver;
            }

            StopWind();
        }

        private void HandleShotStateChanged(ShotState state)
        {
            if (state == ShotState.Ready && windSource == null)
            {
                StartWind();
            }
        }

        private void HandleWindChanged(Vector3 wind)
        {
            if (windSource != null)
            {
                // Scale volume by wind speed (0-8 m/s range)
                float normalizedSpeed = Mathf.Clamp01(wind.magnitude / 8f);
                float ambientVol = GetAmbientVolume();
                windSource.volume = normalizedSpeed * ambientVol;
            }
        }

        private void HandleShotScored(ShotResult result)
        {
            if (AudioManager.Instance == null) return;
            var config = AudioManager.Instance.Config;
            if (config == null) return;

            if (result.DistanceToPin <= config.CrowdReactionDistanceThreshold)
            {
                AudioManager.Instance.PlaySFX(config.CrowdReaction, 0.8f);
            }
        }

        private void HandleGameOver(int shots, bool isNewBest)
        {
            StopWind();
        }

        private void StartWind()
        {
            if (AudioManager.Instance == null) return;
            var config = AudioManager.Instance.Config;
            if (config == null) return;

            windSource = AudioManager.Instance.PlayLoop(config.WindAmbience, 0.3f);
        }

        private void StopWind()
        {
            if (windSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.StopSource(windSource);
                windSource = null;
            }
        }

        private float GetAmbientVolume()
        {
            if (AudioManager.Instance == null) return 0.5f;
            var config = AudioManager.Instance.Config;
            return config != null ? config.AmbientVolume : 0.5f;
        }
    }
}
