namespace GolfGame.Multiplayer
{
    /// <summary>
    /// Mock authentication service returning hardcoded player data.
    /// Replace with real API implementation for production.
    /// </summary>
    public class MockAuthService : IAuthService
    {
        private readonly PlayerInfo playerInfo;

        public MockAuthService()
        {
            playerInfo = new PlayerInfo
            {
                PlayerId = "player_local",
                DisplayName = "You",
                Token = "mock-token-12345"
            };
        }

        public MockAuthService(string playerId, string displayName)
        {
            playerInfo = new PlayerInfo
            {
                PlayerId = playerId,
                DisplayName = displayName,
                Token = $"mock-token-{playerId}"
            };
        }

        public string GetPlayerToken()
        {
            return playerInfo.Token;
        }

        public PlayerInfo GetPlayerInfo()
        {
            return playerInfo;
        }
    }
}
