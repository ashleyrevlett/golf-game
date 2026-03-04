using System.Threading.Tasks;
using UnityEngine;

namespace GolfGame.Multiplayer
{
    /// <summary>
    /// PlayerPrefs-backed best score persistence.
    /// Stores CTP total distance; lower is better.
    /// </summary>
    public class PlayerPrefsBestScoreService : IBestScoreService
    {
        private const string Key = "ctp_best_score";

        public Task<float> GetBestScoreAsync()
        {
            float val = PlayerPrefs.GetFloat(Key, float.MaxValue);
            return Task.FromResult(val);
        }

        public Task SaveBestScoreAsync(float score)
        {
            PlayerPrefs.SetFloat(Key, score);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }
    }
}
