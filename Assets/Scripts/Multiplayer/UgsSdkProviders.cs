namespace GolfGame.Multiplayer
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Unity.Services.Authentication;
    using Unity.Services.CloudCode;
    using Unity.Services.Leaderboards;

    /// <summary>
    /// Raw leaderboard score DTO decoupled from UGS SDK response types.
    /// Rank is 0-based (matching UGS convention); services convert to 1-based.
    /// </summary>
    public struct RawLeaderboardScore
    {
        public int Rank;
        public string PlayerId;
        public string PlayerName;
        public double Score;
    }

    public interface IUgsAuthProvider
    {
        bool IsSignedIn { get; }
        string PlayerId { get; }
        string AccessToken { get; }
        Task SignInAnonymouslyAsync();
        Task UpdatePlayerNameAsync(string name);
    }

    public interface IUgsCloudCodeProvider
    {
        Task<T> CallEndpointAsync<T>(string function, Dictionary<string, object> args);
    }

    public interface IUgsLeaderboardsProvider
    {
        Task<List<RawLeaderboardScore>> GetScoresAsync(string leaderboardId, int limit);
        Task<RawLeaderboardScore> GetPlayerScoreAsync(string leaderboardId);
    }

    internal class DefaultUgsAuthProvider : IUgsAuthProvider
    {
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
        public string PlayerId => AuthenticationService.Instance.PlayerId;
        public string AccessToken => AuthenticationService.Instance.AccessToken;

        public Task SignInAnonymouslyAsync()
        {
            return AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        public Task UpdatePlayerNameAsync(string name)
        {
            return AuthenticationService.Instance.UpdatePlayerNameAsync(name);
        }
    }

    internal class DefaultUgsCloudCodeProvider : IUgsCloudCodeProvider
    {
        public Task<T> CallEndpointAsync<T>(string function, Dictionary<string, object> args)
        {
            return CloudCodeService.Instance.CallEndpointAsync<T>(function, args);
        }
    }

    internal class DefaultUgsLeaderboardsProvider : IUgsLeaderboardsProvider
    {
        public async Task<List<RawLeaderboardScore>> GetScoresAsync(string leaderboardId, int limit)
        {
            var response = await LeaderboardsService.Instance
                .GetScoresAsync(leaderboardId, new GetScoresOptions { Limit = limit });

            var scores = new List<RawLeaderboardScore>(response.Results.Count);
            for (int i = 0; i < response.Results.Count; i++)
            {
                var r = response.Results[i];
                scores.Add(new RawLeaderboardScore
                {
                    Rank = r.Rank,
                    PlayerId = r.PlayerId,
                    PlayerName = r.PlayerName,
                    Score = r.Score
                });
            }
            return scores;
        }

        public async Task<RawLeaderboardScore> GetPlayerScoreAsync(string leaderboardId)
        {
            var entry = await LeaderboardsService.Instance
                .GetPlayerScoreAsync(leaderboardId);

            return new RawLeaderboardScore
            {
                Rank = entry.Rank,
                PlayerId = entry.PlayerId,
                PlayerName = entry.PlayerName,
                Score = entry.Score
            };
        }
    }
}
