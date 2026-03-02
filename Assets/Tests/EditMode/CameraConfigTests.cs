using NUnit.Framework;
using UnityEngine;
using GolfGame.Camera;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for CameraConfig ScriptableObject default values.
    /// </summary>
    public class CameraConfigTests
    {
        private CameraConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<CameraConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultTeeOffset_IsNonZero()
        {
            Assert.AreNotEqual(Vector3.zero, config.TeeOffset);
        }

        [Test]
        public void DefaultTeeFov_IsPositive()
        {
            Assert.Greater(config.TeeFov, 0f);
        }

        [Test]
        public void DefaultFlightOffset_IsNonZero()
        {
            Assert.AreNotEqual(Vector3.zero, config.FlightOffset);
        }

        [Test]
        public void DefaultFlightFov_IsPositive()
        {
            Assert.Greater(config.FlightFov, 0f);
        }

        [Test]
        public void DefaultFlightDamping_IsPositive()
        {
            Assert.Greater(config.FlightDamping, 0f);
        }

        [Test]
        public void DefaultFlightLookahead_IsPositive()
        {
            Assert.Greater(config.FlightLookahead, 0f);
        }

        [Test]
        public void DefaultLandingOffset_IsNonZero()
        {
            Assert.AreNotEqual(Vector3.zero, config.LandingOffset);
        }

        [Test]
        public void DefaultLandingFov_IsPositive()
        {
            Assert.Greater(config.LandingFov, 0f);
        }

        [Test]
        public void DefaultTeeToFlightBlend_IsPositive()
        {
            Assert.Greater(config.TeeToFlightBlend, 0f);
        }

        [Test]
        public void DefaultFlightToLandingBlend_IsPositive()
        {
            Assert.Greater(config.FlightToLandingBlend, 0f);
        }

        [Test]
        public void FlightFov_IsNarrowerThanTeeFov()
        {
            Assert.Less(config.FlightFov, config.TeeFov);
        }

        [Test]
        public void LandingFov_IsNarrowerThanFlightFov()
        {
            Assert.Less(config.LandingFov, config.FlightFov);
        }
    }
}
