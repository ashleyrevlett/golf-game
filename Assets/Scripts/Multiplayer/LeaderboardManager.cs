using System;
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
        private PlayerInfo playerInfo;
        private float pollTimer;
        private bool isPolling;

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

        private void Start()
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

            playerInfo = authService.GetPlayerInfo();

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
            PollLeaderboard();
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
            if (!isPolling || leaderboardService == null) return;

            pollTimer += Time.deltaTime;
            if (pollTimer >= pollInterval)
            {
                pollTimer = 0f;
                PollLeaderboard();
            }
        }

        private void HandleShotStateChanged(ShotState state)
        {
            isPolling = state == ShotState.Ready || state == ShotState.Landed;
        }

        private void HandleBestDistanceUpdated(float distance)
        {
            if (leaderboardService == null) return;

            leaderboardService.PostScore(playerInfo.PlayerId, distance);
            PollLeaderboard();
        }

        private void HandleGameOver(int shots, bool isNewBest)
        {
            isPolling = false;
            PollLeaderboard();
        }

        private void PollLeaderboard()
        {
            if (leaderboardService == null) return;

            CurrentEntries = leaderboardService.GetLeaderboard(leaderboardSize);
            PlayerRank = leaderboardService.GetPlayerRank(playerInfo.PlayerId);

            OnLeaderboardUpdated?.Invoke(CurrentEntries, PlayerRank);
        }
    }
}
