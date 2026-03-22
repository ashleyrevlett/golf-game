namespace GolfGame.Multiplayer
{
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// UGS Authentication wrapper implementing IAuthService.
    /// Uses anonymous sign-in for MVP.
    /// </summary>
    public class UgsAuthService : IAuthService
    {
        private readonly IUgsAuthProvider _auth;

        public UgsAuthService(IUgsAuthProvider auth)
        {
            _auth = auth;
        }

        public bool IsSignedIn => _auth.IsSignedIn;
        public string PlayerId => _auth.PlayerId;

        public async Task<string> GetPlayerTokenAsync()
        {
            try
            {
                if (!IsSignedIn) await SignInAsync();
                return _auth.AccessToken;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UgsAuth] Failed to get player token: {ex.Message}");
                throw;
            }
        }

        public async Task<PlayerInfo> GetPlayerInfoAsync()
        {
            if (!IsSignedIn) await SignInAsync();
            var nickname = PlayerPrefs.GetString("nickname", "");
            return new PlayerInfo
            {
                PlayerId = _auth.PlayerId,
                DisplayName = string.IsNullOrEmpty(nickname) ? $"Player_{_auth.PlayerId[..6]}" : nickname,
                Token = _auth.AccessToken
            };
        }

        public async Task UpdateDisplayNameAsync(string displayName)
        {
            if (!IsSignedIn) await SignInAsync();
            await _auth.UpdatePlayerNameAsync(displayName);
            Debug.Log($"[UgsAuth] Display name updated: {displayName}");
        }

        public async Task SignInAsync()
        {
            if (IsSignedIn) return;
            await _auth.SignInAnonymouslyAsync();
            Debug.Log($"[UgsAuth] Signed in: {PlayerId}");
        }
    }
}
