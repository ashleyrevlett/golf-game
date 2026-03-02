using System;
using System.Collections.Generic;
using UnityEngine;
using GolfGame.Core;
using GolfGame.Golf;

namespace GolfGame.Environment
{
    /// <summary>
    /// Tracks shot statistics and best distance across the 6-shot game.
    /// Subscribes to BallController.OnBallLanded and BallController.OnBallBounced.
    /// </summary>
    public class ScoringManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BallController ballController;
        [SerializeField] private PinController pinController;

        private readonly List<ShotResult> results = new List<ShotResult>();
        private float bestDistance = float.MaxValue;
        private Vector3 teePosition;
        private Vector3 firstBouncePosition;
        private bool hasRecordedBounce;
        private float launchSpeed;

        /// <summary>
        /// Best distance to pin across all shots this game.
        /// float.MaxValue if no shots taken.
        /// </summary>
        public float BestDistance => bestDistance;

        /// <summary>
        /// All completed shot results.
        /// </summary>
        public IReadOnlyList<ShotResult> Results => results;

        /// <summary>
        /// Fires when a shot lands and is scored. Payload is the shot result.
        /// </summary>
        public event Action<ShotResult> OnShotScored;

        /// <summary>
        /// Fires when best distance improves. Payload is the new best distance.
        /// </summary>
        public event Action<float> OnBestDistanceUpdated;

        /// <summary>
        /// Fires when the game completes. Payload: (all results, best distance).
        /// </summary>
        public event Action<IReadOnlyList<ShotResult>, float> OnGameComplete;

        private void Start()
        {
            if (ballController != null)
            {
                ballController.OnBallLanded += HandleBallLanded;
                ballController.OnBallBounced += HandleBallBounced;
                teePosition = ballController.transform.position;
            }

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged += HandleShotStateChanged;
                gameManager.OnGameOver += HandleGameOver;
            }
        }

        private void OnDestroy()
        {
            if (ballController != null)
            {
                ballController.OnBallLanded -= HandleBallLanded;
                ballController.OnBallBounced -= HandleBallBounced;
            }

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged -= HandleShotStateChanged;
                gameManager.OnGameOver -= HandleGameOver;
            }
        }

        /// <summary>
        /// Reset scoring state for a new game.
        /// </summary>
        public void Reset()
        {
            results.Clear();
            bestDistance = float.MaxValue;
            hasRecordedBounce = false;
            launchSpeed = 0f;
        }

        private void HandleShotStateChanged(ShotState newState)
        {
            if (newState == ShotState.Flying)
            {
                // Record launch data
                if (ballController != null)
                {
                    teePosition = ballController.transform.position;
                }
                hasRecordedBounce = false;
                launchSpeed = 0f;
            }
        }

        private void HandleBallBounced(Vector3 position, float speed)
        {
            if (!hasRecordedBounce)
            {
                firstBouncePosition = position;
                hasRecordedBounce = true;
            }

            // Record launch speed from first bounce impact
            if (launchSpeed <= 0f)
            {
                launchSpeed = speed;
            }
        }

        private void HandleBallLanded(Vector3 landPosition)
        {
            float distanceToPin = 0f;
            if (pinController != null)
            {
                distanceToPin = pinController.CalculateDistance(landPosition);
            }

            float carryDistance = hasRecordedBounce
                ? FlatDistance(teePosition, firstBouncePosition)
                : FlatDistance(teePosition, landPosition);

            float totalDistance = FlatDistance(teePosition, landPosition);
            float lateralDeviation = landPosition.x - teePosition.x;

            int shotNumber = gameManager != null ? gameManager.CurrentShot : results.Count + 1;

            var result = new ShotResult
            {
                ShotNumber = shotNumber,
                DistanceToPin = distanceToPin,
                CarryDistance = carryDistance,
                TotalDistance = totalDistance,
                LateralDeviation = lateralDeviation,
                BallSpeed = launchSpeed
            };

            results.Add(result);
            OnShotScored?.Invoke(result);

            if (distanceToPin < bestDistance)
            {
                bestDistance = distanceToPin;
                OnBestDistanceUpdated?.Invoke(bestDistance);
            }
        }

        private void HandleGameOver(int shots, bool isNewBest)
        {
            OnGameComplete?.Invoke(results, bestDistance);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            var flatA = new Vector3(a.x, 0f, a.z);
            var flatB = new Vector3(b.x, 0f, b.z);
            return Vector3.Distance(flatA, flatB);
        }
    }
}
