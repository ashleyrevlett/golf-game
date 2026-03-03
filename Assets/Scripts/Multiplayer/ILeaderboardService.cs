namespace GolfGame.Multiplayer
{
    /// <summary>
    /// Leaderboard service interface.
    /// Implementations handle score posting and retrieval.
    /// </summary>
    public interface ILeaderboardService
    {
        /// <summary>
        /// Post or update a player's best score.
        /// </summary>
        void PostScore(string playerId, float distance);

        /// <summary>
        /// Get the top N leaderboard entries, sorted by distance ascending.
        /// </summary>
        LeaderboardEntry[] GetLeaderboard(int count);

        /// <summary>
        /// Get a player's current rank (1-based). Returns -1 if not found.
        /// </summary>
        int GetPlayerRank(string playerId);
    }
}
