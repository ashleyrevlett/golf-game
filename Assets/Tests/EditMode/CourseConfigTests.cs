using NUnit.Framework;
using UnityEngine;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for CourseConfig ScriptableObject defaults.
    /// </summary>
    public class CourseConfigTests
    {
        private CourseConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<CourseConfig>();
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
        public void DefaultCourseLength_Is125Yards()
        {
            Assert.Greater(config.CourseLength, 100f);
        }

        [Test]
        public void DefaultFairwayWidth_IsPositive()
        {
            Assert.Greater(config.FairwayWidth, 0f);
        }

        [Test]
        public void DefaultGreenRadius_IsPositive()
        {
            Assert.Greater(config.GreenRadius, 0f);
        }

        [Test]
        public void DefaultPinHeight_IsPositive()
        {
            Assert.Greater(config.PinHeight, 0f);
        }

        [Test]
        public void DefaultRoughWidth_IsPositive()
        {
            Assert.Greater(config.RoughWidth, 0f);
        }

        [Test]
        public void DefaultTeeBoxSize_IsPositive()
        {
            Assert.Greater(config.TeeBoxSize, 0f);
        }

        [Test]
        public void DefaultObMarkerSpacing_IsPositive()
        {
            Assert.Greater(config.ObMarkerSpacing, 0f);
        }
    }
}
