using System.Threading.Tasks;

namespace GolfGame.Multiplayer
{
    /// <summary>
    /// Authentication service interface.
    /// Implementations provide player token and info.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Get the current player's authentication token.
        /// </summary>
        Task<string> GetPlayerTokenAsync();

        /// <summary>
        /// Get the current player's information.
        /// </summary>
        Task<PlayerInfo> GetPlayerInfoAsync();

        /// <summary>
        /// Whether the player is currently signed in.
        /// Synchronous getter for cached state.
        /// </summary>
        bool IsSignedIn { get; }

        /// <summary>
        /// The current player's ID.
        /// Synchronous getter for cached state.
        /// </summary>
        string PlayerId { get; }
    }
}
