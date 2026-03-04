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
        [Header("References")]
        [SerializeField] private ScoringManager scoringManager;
        [SerializeField] private GameManager gameManager;

        [Header("Settings")]
        [SerializeField] private float pollInterval = 5f;
        [SerializeField] private int leaderboardSize = 10;

        private IAuthService authService;
        private ILeaderboardService leaderboardService;
        private string playerId;
        private float pollTimer;
        private bool isPolling;

        // Retry queue for failed score posts
        private readonly Queue<(string playerId, float distance)> retryQueue
            = new Queue<(string, float)>();
        private float retryTimer;
        private const float RetryInterval = 10f;

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

        private void Update()
        {
            // Existing poll logic (now calls async)
            if (isPolling && leaderboardService != null)
            {
                pollTimer += Time.deltaTime;
                if (pollTimer >= pollInterval)
                {
                    pollTimer = 0f;
                    _ = PollLeaderboardAsync();
                }
            }

            // Retry failed posts
            if (retryQueue.Count > 0)
            {
                retryTimer += Time.deltaTime;
                if (retryTimer >= RetryInterval)
                {
                    retryTimer = 0f;
                    _ = ProcessRetryQueueAsync();
                }
            }
        }

        private void HandleShotStateChanged(ShotState state)
        {
            isPolling = state == ShotState.Ready || state == ShotState.Landed;
        }

        private async void HandleBestDistanceUpdated(float distance)
        {
            if (leaderboardService == null) return;
            await PostScoreWithRetryAsync(playerId, distance);
            await PollLeaderboardAsync();
        }

        private async void HandleGameOver(int shots, bool isNewBest)
        {
            isPolling = false;
            await PollLeaderboardAsync();
        }

        internal async Task PostScoreWithRetryAsync(string id, float dist)
        {
            try
            {
                await leaderboardService.PostScoreAsync(id, dist);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LeaderboardManager] Post failed, queuing retry: {ex.Message}");
                retryQueue.Enqueue((id, dist));
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
    }
}
