namespace GolfGame.Multiplayer
{
    using System.Threading.Tasks;
    using Unity.Services.Authentication;
    using UnityEngine;

    /// <summary>
    /// UGS Authentication wrapper implementing IAuthService.
    /// Uses anonymous sign-in for MVP.
    /// </summary>
    public class UgsAuthService : IAuthService
    {
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
        public string PlayerId => AuthenticationService.Instance.PlayerId;

        public async Task<string> GetPlayerTokenAsync()
        {
            try
            {
                if (!IsSignedIn) await SignInAsync();
                return AuthenticationService.Instance.AccessToken;
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
            return new PlayerInfo
            {
                PlayerId = AuthenticationService.Instance.PlayerId,
                DisplayName = $"Player_{AuthenticationService.Instance.PlayerId[..6]}",
                Token = AuthenticationService.Instance.AccessToken
            };
        }

        public async Task SignInAsync()
        {
            if (IsSignedIn) return;
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[UgsAuth] Signed in: {PlayerId}");
        }
    }
}
