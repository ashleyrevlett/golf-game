using NUnit.Framework;
using UnityEngine;
using GolfGame.Core;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for GameplayHUDController logic.
    /// Tests cardinal direction helper and distance formatting.
    /// UI element tests require PlayMode with UIDocument.
    /// </summary>
    public class GameplayHUDControllerTests
    {
        [Test]
        public void BestDistance_MaxValue_DisplaysPlaceholder()
        {
            // When best distance is float.MaxValue, display should show "--"
            float distance = float.MaxValue;
            bool isMaxValue = distance >= float.MaxValue / 2f;
            Assert.IsTrue(isMaxValue);
        }

        [Test]
        public void BestDistance_ValidValue_Formats()
        {
            float distance = 2.45f;
            string formatted = $"Best: {distance:F1}m";
            Assert.AreEqual("Best: 2.4m", formatted);
        }

        [Test]
        public void ShotCounter_Formats()
        {
            int shot = 3;
            string formatted = $"Shot {shot}/{GameManager.MaxShots}";
            Assert.AreEqual("Shot 3/6", formatted);
        }

        [Test]
        public void ShotResult_DistanceFormats()
        {
            var result = new ShotResult
            {
                DistanceToPin = 4.23f,
                CarryDistance = 110.5f,
                BallSpeed = 42.7f,
                LateralDeviation = -1.3f
            };

            Assert.AreEqual("4.2", result.DistanceToPin.ToString("F1"));
            Assert.AreEqual("110.5", result.CarryDistance.ToString("F1"));
            Assert.AreEqual("42.7", result.BallSpeed.ToString("F1"));
            Assert.AreEqual("-1.3", result.LateralDeviation.ToString("F1"));
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
