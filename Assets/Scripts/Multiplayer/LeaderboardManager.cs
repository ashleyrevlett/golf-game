using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GolfGame.Core;
using GolfGame.Environment;

namespace GolfGame.Multiplayer
{
    /// <summary>
    /// Orchestrates leaderboard score submission and polling.
    /// Subscribes to ScoringManager events and exposes leaderboard data for UI.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        private ScoringManager scoringManager;
        private GameManager gameManager;

        [Header("Settings")]
        [SerializeField] private float pollInterval = 5f;
        [SerializeField] private int leaderboardSize = 10;

        private IAuthService authService;
        private ILeaderboardService leaderboardService;
        private string playerId;
        private bool isPolling;

        // Retry queue for failed score posts
        private readonly Queue<(string playerId, float distance)> retryQueue
            = new Queue<(string, float)>();
        private const float RetryInterval = 10f;
        private const int MaxRetryQueueSize = 10;
        private bool isRetrying;

        /// <summary>
        /// Current leaderboard entries.
        /// </summary>
        public LeaderboardEntry[] CurrentEntries { get; private set; } = Array.Empty<LeaderboardEntry>();

        /// <summary>
        /// Current player's rank (1-based). -1 if not ranked.
        /// </summary>
        public int PlayerRank { get; private set; } = -1;

        /// <summary>
        /// Fires when leaderboard data updates.
        /// Payload: (entries array, player rank).
        /// </summary>
        public event Action<LeaderboardEntry[], int> OnLeaderboardUpdated;

        private async void Start()
        {
            try
            {
                scoringManager = FindFirstObjectByType<ScoringManager>();
                gameManager = FindFirstObjectByType<GameManager>();

                authService = ServiceLocator.Get<IAuthService>();
                leaderboardService = ServiceLocator.Get<ILeaderboardService>();

                if (authService == null)
                {
                    Debug.LogWarning("[LeaderboardManager] No IAuthService registered");
                    return;
                }

                if (leaderboardService == null)
                {
                    Debug.LogWarning("[LeaderboardManager] No ILeaderboardService registered");
                    return;
                }

                playerId = authService.PlayerId;

                if (scoringManager != null)
                {
                    scoringManager.OnBestDistanceUpdated += HandleBestDistanceUpdated;
                }

                if (gameManager != null)
                {
                    gameManager.OnShotStateChanged += HandleShotStateChanged;
                    gameManager.OnGameOver += HandleGameOver;
                }

                // Initial poll
                await PollLeaderboardAsync();
                if (this == null) return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardManager] Start failed: {ex}");
            }
        }

        private void OnDestroy()
        {
            if (scoringManager != null)
            {
                scoringManager.OnBestDistanceUpdated -= HandleBestDistanceUpdated;
            }

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged -= HandleShotStateChanged;
                gameManager.OnGameOver -= HandleGameOver;
            }
        }

        private System.Collections.IEnumerator PollLoop()
        {
            while (isPolling && leaderboardService != null)
            {
                yield return new WaitForSeconds(pollInterval);
                if (isPolling) _ = PollLeaderboardAsync();
            }
        }

        private System.Collections.IEnumerator RetryLoop()
        {
            isRetrying = true;
            while (retryQueue.Count > 0)
            {
                yield return new WaitForSeconds(RetryInterval);
                if (retryQueue.Count > 0) _ = ProcessRetryQueueAsync();
            }
            isRetrying = false;
        }

        private void HandleShotStateChanged(ShotState state)
        {
            bool wasPolling = isPolling;
            isPolling = state == ShotState.Ready || state == ShotState.Landed;
            if (isPolling && !wasPolling)
                StartCoroutine(PollLoop());
        }

        private void StartRetryIfNeeded()
        {
            if (isRetrying) return;
            StartCoroutine(RetryLoop());
        }

        private async void HandleBestDistanceUpdated(float distance)
        {
            try
            {
                if (leaderboardService == null) return;
                await PostScoreWithRetryAsync(playerId, distance);
                if (this == null) return;
                await PollLeaderboardAsync();
                if (this == null) return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardManager] HandleBestDistanceUpdated failed: {ex}");
            }
        }

        private async void HandleGameOver(int shots)
        {
            try
            {
                isPolling = false;
                await PollLeaderboardAsync();
                if (this == null) return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardManager] HandleGameOver failed: {ex}");
            }
        }

        internal async Task PostScoreWithRetryAsync(string id, float dist)
        {
            try
            {
                await leaderboardService.PostScoreAsync(id, dist);
            }
            catch (Exception ex)
            {
                if (retryQueue.Count >= MaxRetryQueueSize)
                {
                    Debug.LogWarning($"[LeaderboardManager] Retry queue full ({MaxRetryQueueSize} entries) — dropping score submission for player {id}");
                    return;
                }
                Debug.LogWarning($"[LeaderboardManager] Post failed, queuing retry: {ex.Message}");
                retryQueue.Enqueue((id, dist));
                StartRetryIfNeeded();
            }
        }

        internal async Task ProcessRetryQueueAsync()
        {
            if (retryQueue.Count == 0) return;
            var (id, dist) = retryQueue.Peek();
            try
            {
                await leaderboardService.PostScoreAsync(id, dist);
                retryQueue.Dequeue(); // only dequeue on success
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LeaderboardManager] Retry failed: {ex.Message}");
            }
        }

        internal async Task PollLeaderboardAsync()
        {
            if (leaderboardService == null) return;
            try
            {
                CurrentEntries = await leaderboardService.GetLeaderboardAsync(leaderboardSize);
                PlayerRank = await leaderboardService.GetPlayerRankAsync(playerId);
            }
            catch (Exception ex)
            {
                // Stale cache: CurrentEntries and PlayerRank keep their last values
                Debug.LogWarning($"[LeaderboardManager] Poll failed, using cached data: {ex.Message}");
            }
            OnLeaderboardUpdated?.Invoke(CurrentEntries, PlayerRank);
        }

        /// <summary>
        /// Number of items in the retry queue. Exposed for testing.
        /// </summary>
        internal int RetryQueueCount => retryQueue.Count;

        /// <summary>
        /// Whether the retry loop coroutine is currently active. Exposed for testing.
        /// </summary>
        internal bool IsRetrying => isRetrying;
    }
}
