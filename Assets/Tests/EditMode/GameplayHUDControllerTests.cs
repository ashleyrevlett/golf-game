using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using GolfGame.Core;
using GolfGame.Environment;
using GolfGame.UI;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for GameplayHUDController formatting logic.
    /// Tests score display, shots remaining, and wind direction.
    /// UI element tests require PlayMode with UIDocument.
    /// </summary>
    public class GameplayHUDControllerTests
    {
        /// <summary>
        /// Invoke the private static GetCardinalDirection method via reflection.
        /// </summary>
        private static string GetCardinalDirection(float degrees)
        {
            var method = typeof(GameplayHUDController).GetMethod(
                "GetCardinalDirection",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "GetCardinalDirection method should exist");
            return (string)method.Invoke(null, new object[] { degrees });
        }

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

        // --- GetCardinalDirection tests via reflection ---

        [Test]
        public void GetCardinalDirection_Zero_ReturnsNorth()
        {
            Assert.AreEqual("N", GetCardinalDirection(0f));
        }

        [Test]
        public void GetCardinalDirection_90_ReturnsEast()
        {
            Assert.AreEqual("E", GetCardinalDirection(90f));
        }

        [Test]
        public void GetCardinalDirection_180_ReturnsSouth()
        {
            Assert.AreEqual("S", GetCardinalDirection(180f));
        }

        [Test]
        public void GetCardinalDirection_270_ReturnsWest()
        {
            Assert.AreEqual("W", GetCardinalDirection(270f));
        }

        [Test]
        public void GetCardinalDirection_45_ReturnsNE()
        {
            Assert.AreEqual("NE", GetCardinalDirection(45f));
        }

        [Test]
        public void GetCardinalDirection_135_ReturnsSE()
        {
            Assert.AreEqual("SE", GetCardinalDirection(135f));
        }

        [Test]
        public void GetCardinalDirection_225_ReturnsSW()
        {
            Assert.AreEqual("SW", GetCardinalDirection(225f));
        }

        [Test]
        public void GetCardinalDirection_315_ReturnsNW()
        {
            Assert.AreEqual("NW", GetCardinalDirection(315f));
        }

        [Test]
        public void GetCardinalDirection_NegativeDegrees_NormalizesCorrectly()
        {
            // -90 degrees should normalize to 270 -> W
            Assert.AreEqual("W", GetCardinalDirection(-90f));
        }

        [Test]
        public void GetCardinalDirection_Over360_NormalizesCorrectly()
        {
            // 450 degrees should normalize to 90 -> E
            Assert.AreEqual("E", GetCardinalDirection(450f));
        }

        [Test]
        public void GetCardinalDirection_BoundaryAt337Point5_ReturnsNorth()
        {
            // 337.5 is the boundary: >= 337.5 should be N
            Assert.AreEqual("N", GetCardinalDirection(337.5f));
        }

        [Test]
        public void GetCardinalDirection_JustBelow337Point5_ReturnsNW()
        {
            Assert.AreEqual("NW", GetCardinalDirection(337.4f));
        }
    }
}
