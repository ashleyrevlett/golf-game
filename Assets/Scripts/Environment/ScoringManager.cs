using System;
using System.Collections.Generic;
using UnityEngine;
using GolfGame.Core;
using GolfGame.Golf;

namespace GolfGame.Environment
{
    /// <summary>
    /// Tracks shot statistics, CTP running total, and best distance across the 6-shot game.
    /// Subscribes to BallController.OnBallLanded and BallController.OnBallBounced.
    /// </summary>
    public class ScoringManager : MonoBehaviour
    {
        private GameManager gameManager;
        private BallController ballController;
        private PinController pinController;

        private readonly List<ShotResult> results = new List<ShotResult>();
        private float bestDistance = float.MaxValue;
        private float totalCtpDistance;
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
        /// Running total of CTP distances across all shots this game.
        /// </summary>
        public float TotalCtpDistance => totalCtpDistance;

        /// <summary>
        /// All completed shot results.
        /// </summary>
        public IReadOnlyList<ShotResult> Results => results;

        /// <summary>
        /// Fires when a shot lands and is scored. Payload is the shot result.
        /// </summary>
        public event Action<ShotResult> OnShotScored;

        /// <summary>
        /// Fires when a shot is recorded. Payload is this shot's distance to pin.
        /// </summary>
        public event Action<float> OnShotRecorded;

        /// <summary>
        /// Fires when best distance improves. Payload is the new best distance.
        /// </summary>
        public event Action<float> OnBestDistanceUpdated;

        /// <summary>
        /// Fires when the game completes. Payload: (all results, best distance).
        /// </summary>
        public event Action<IReadOnlyList<ShotResult>, float> OnGameComplete;

        /// <summary>
        /// Fires when all shots are complete. Payload is the total CTP distance.
        /// </summary>
        public event Action<float> OnAllShotsComplete;

        private void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            ballController = FindFirstObjectByType<BallController>();
            pinController = FindFirstObjectByType<PinController>();

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
            totalCtpDistance = 0f;
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
            totalCtpDistance += distanceToPin;

            OnShotScored?.Invoke(result);
            OnShotRecorded?.Invoke(distanceToPin);

            if (distanceToPin < bestDistance)
            {
                bestDistance = distanceToPin;
                OnBestDistanceUpdated?.Invoke(bestDistance);
            }

            // Spawn post-shot popup
            ShotPopup.Create(landPosition + Vector3.up * 1.5f, distanceToPin, Camera.main);
        }

        private void HandleGameOver(int shots, bool isNewBest)
        {
            OnGameComplete?.Invoke(results, bestDistance);
            OnAllShotsComplete?.Invoke(totalCtpDistance);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            var flatA = new Vector3(a.x, 0f, a.z);
            var flatB = new Vector3(b.x, 0f, b.z);
            return Vector3.Distance(flatA, flatB);
        }
    }
}
