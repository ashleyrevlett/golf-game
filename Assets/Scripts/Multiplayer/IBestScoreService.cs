using System.Threading.Tasks;

namespace GolfGame.Multiplayer
{
    /// <summary>
    /// Abstraction for persisting and retrieving the player's best CTP score.
    /// Returns float.MaxValue when no previous score exists.
    /// </summary>
    public interface IBestScoreService
    {
        Task<float> GetBestScoreAsync();
        Task SaveBestScoreAsync(float score);
    }
}
