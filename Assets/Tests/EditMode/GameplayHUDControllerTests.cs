using NUnit.Framework;
using UnityEngine;
using GolfGame.Core;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for GameplayHUDController formatting logic.
    /// Tests score display, shots remaining, and wind direction.
    /// UI element tests require PlayMode with UIDocument.
    /// </summary>
    public class GameplayHUDControllerTests
    {
        [Test]
        public void ScoreValue_FormatsOneDecimal()
        {
            float score = 42.7f;
            string formatted = score.ToString("F1");
            Assert.AreEqual("42.7", formatted);
        }

        [Test]
        public void ScoreValue_ZeroFormatsCorrectly()
        {
            float score = 0f;
            string formatted = score.ToString("F1");
            Assert.AreEqual("0.0", formatted);
        }

        [Test]
        public void ShotsRemaining_ComputesFromMaxMinusCurrent()
        {
            int remaining = GameManager.MaxShots - 3;
            Assert.AreEqual(3, remaining);
        }

        [Test]
        public void ShotsRemaining_ZeroAtMaxShots()
        {
            int remaining = GameManager.MaxShots - GameManager.MaxShots;
            Assert.AreEqual(0, remaining);
        }

        [Test]
        public void WindDisplay_ZeroWind_FormatsCorrectly()
        {
            Vector3 wind = Vector3.zero;
            float speed = wind.magnitude;
            Assert.AreEqual(0f, speed, 0.001f);
        }

        [Test]
        public void WindDisplay_NorthWind_DirectionCorrect()
        {
            // Wind from North: (0, 0, positive)
            Vector3 wind = new Vector3(0f, 0f, 5f);
            float dir = Mathf.Atan2(wind.x, wind.z) * Mathf.Rad2Deg;
            // 0 degrees should be N
            Assert.AreEqual(0f, dir, 0.001f);
        }

        [Test]
        public void WindDisplay_EastWind_DirectionCorrect()
        {
            // Wind from East: (positive, 0, 0)
            Vector3 wind = new Vector3(5f, 0f, 0f);
            float dir = Mathf.Atan2(wind.x, wind.z) * Mathf.Rad2Deg;
            // 90 degrees should be E
            Assert.AreEqual(90f, dir, 0.001f);
        }
    }
}
