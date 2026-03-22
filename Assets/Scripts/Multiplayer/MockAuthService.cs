using System.Threading.Tasks;
using UnityEngine;

namespace GolfGame.Multiplayer
{
    /// <summary>
    /// Mock authentication service returning hardcoded player data.
    /// Replace with real API implementation for production.
    /// </summary>
    public class MockAuthService : IAuthService
    {
        private PlayerInfo playerInfo;

        public bool IsSignedIn => true;
        public string PlayerId => playerInfo.PlayerId;

        public MockAuthService()
        {
            var nickname = PlayerPrefs.GetString("nickname", "");
            playerInfo = new PlayerInfo
            {
                PlayerId = "player_local",
                DisplayName = string.IsNullOrEmpty(nickname) ? "You" : nickname,
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

        public Task<string> GetPlayerTokenAsync()
        {
            return Task.FromResult(playerInfo.Token);
        }

        public Task<PlayerInfo> GetPlayerInfoAsync()
        {
            return Task.FromResult(playerInfo);
        }

        public Task UpdateDisplayNameAsync(string displayName)
        {
            playerInfo = new PlayerInfo
            {
                PlayerId = playerInfo.PlayerId,
                DisplayName = displayName,
                Token = playerInfo.Token
            };
            return Task.CompletedTask;
        }
    }
}
