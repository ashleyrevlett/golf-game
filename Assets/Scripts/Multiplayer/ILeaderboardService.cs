using System.Threading.Tasks;

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
        Task PostScoreAsync(string playerId, float distance);

        /// <summary>
        /// Get the top N leaderboard entries, sorted by distance ascending.
        /// </summary>
        Task<LeaderboardEntry[]> GetLeaderboardAsync(int count);

        /// <summary>
        /// Get a player's current rank (1-based). Returns -1 if not found.
        /// </summary>
        Task<int> GetPlayerRankAsync(string playerId);
    }
}
