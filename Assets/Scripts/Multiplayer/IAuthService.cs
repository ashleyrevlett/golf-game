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
        string GetPlayerToken();

        /// <summary>
        /// Get the current player's information.
        /// </summary>
        PlayerInfo GetPlayerInfo();
    }
}
