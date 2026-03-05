using NUnit.Framework;
using UnityEngine;
using GolfGame.Multiplayer;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for PlayerPrefsBestScoreService persistence.
    /// </summary>
    public class PlayerPrefsBestScoreServiceTests
    {
        private const string Key = "ctp_best_score";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(Key);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(Key);
        }

        [Test]
        public void GetBestScoreAsync_ReturnsMaxValue_WhenNoPreviousSave()
        {
            var service = new PlayerPrefsBestScoreService();
            var task = service.GetBestScoreAsync();
            Assert.AreEqual(float.MaxValue, task.Result);
        }

        [Test]
        public void SaveAndLoad_RoundTrips()
        {
            var service = new PlayerPrefsBestScoreService();
            service.SaveBestScoreAsync(42.7f);
            var result = service.GetBestScoreAsync().Result;
            Assert.AreEqual(42.7f, result, 0.01f);
        }

        [Test]
        public void SaveBestScore_OverwritesPrevious()
        {
            var service = new PlayerPrefsBestScoreService();
            service.SaveBestScoreAsync(50.0f);
            service.SaveBestScoreAsync(30.0f);
            var result = service.GetBestScoreAsync().Result;
            Assert.AreEqual(30.0f, result, 0.01f);
        }
    }
}
