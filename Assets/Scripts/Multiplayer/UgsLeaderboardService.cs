namespace GolfGame.Multiplayer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// UGS Leaderboards + Cloud Code wrapper implementing ILeaderboardService.
    /// Reads go directly to UGS Leaderboards API.
    /// Writes go through Cloud Code for server-side validation.
    /// </summary>
    public class UgsLeaderboardService : ILeaderboardService
    {
        private const string LeaderboardId = "closest-to-pin";
        private const string CloudCodeFunction = "validate-and-post-score";

        private readonly IUgsCloudCodeProvider _cloudCode;
        private readonly IUgsLeaderboardsProvider _leaderboards;

        public UgsLeaderboardService(IUgsCloudCodeProvider cloudCode, IUgsLeaderboardsProvider leaderboards)
        {
            _cloudCode = cloudCode;
            _leaderboards = leaderboards;
        }

        public async Task PostScoreAsync(string playerId, float distance)
        {
            var args = new Dictionary<string, object>
            {
                { "distance", distance }
            };
            // Cloud Code validates and writes; throws on rejection
            var result = await _cloudCode.CallEndpointAsync<ScorePostResult>(
                CloudCodeFunction, args);

            if (!result.success)
            {
                throw new InvalidOperationException(
                    $"Score rejected: {result.reason}");
            }

            Debug.Log($"[UgsLeaderboard] Score posted: {distance:F2}m");
        }

        public async Task<LeaderboardEntry[]> GetLeaderboardAsync(int count)
        {
            var scores = await _leaderboards.GetScoresAsync(LeaderboardId, count);

            var entries = new LeaderboardEntry[scores.Count];
            for (int i = 0; i < scores.Count; i++)
            {
                var r = scores[i];
                entries[i] = new LeaderboardEntry
                {
                    Rank = r.Rank + 1, // UGS is 0-based, we use 1-based
                    PlayerId = r.PlayerId,
                    DisplayName = r.PlayerName ?? $"Player_{r.PlayerId[..6]}",
                    Distance = (float)r.Score
                };
            }
            return entries;
        }

        public async Task<int> GetPlayerRankAsync(string playerId)
        {
            try
            {
                var entry = await _leaderboards.GetPlayerScoreAsync(LeaderboardId);
                return entry.Rank + 1; // 0-based to 1-based
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UgsLeaderboard] Rank lookup failed: {ex.Message}");
                return -1;
            }
        }
    }

    /// <summary>
    /// Deserialized response from Cloud Code validate-and-post-score.
    /// Field names are lowercase to match the JSON keys returned by the Cloud Code JS script.
    /// </summary>
    [Serializable]
    internal struct ScorePostResult
    {
        public bool success;
        public string reason;
    }
}
