using NUnit.Framework;
using UnityEngine;
using GolfGame.UI;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for PowerMeterController color interpolation logic.
    /// </summary>
    public class PowerMeterControllerTests
    {
        [Test]
        public void GetMeterColor_AtZero_IsGreen()
        {
            Color color = PowerMeterController.GetMeterColor(0f);
            // Green channel should dominate
            Assert.Greater(color.g, color.r,
                "At value 0, green channel should exceed red");
            Assert.AreEqual(0.3f, color.r, 0.01f);
            Assert.AreEqual(0.8f, color.g, 0.01f);
        }

        [Test]
        public void GetMeterColor_AtHalf_IsYellow()
        {
            Color color = PowerMeterController.GetMeterColor(0.5f);
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(0.84f, color.g, 0.01f);
        }

        [Test]
        public void GetMeterColor_AtOne_IsRed()
        {
            Color color = PowerMeterController.GetMeterColor(1f);
            // Red channel should dominate
            Assert.Greater(color.r, color.g,
                "At value 1, red channel should exceed green");
            Assert.AreEqual(0.85f, color.r, 0.01f);
            Assert.AreEqual(0.26f, color.g, 0.01f);
        }

        [Test]
        public void GetMeterColor_AtQuarter_IsBetweenGreenAndYellow()
        {
            Color color = PowerMeterController.GetMeterColor(0.25f);
            // Should be midpoint between green and yellow
            float expectedR = Mathf.Lerp(0.3f, 1f, 0.5f);
            float expectedG = Mathf.Lerp(0.8f, 0.84f, 0.5f);
            Assert.AreEqual(expectedR, color.r, 0.01f);
            Assert.AreEqual(expectedG, color.g, 0.01f);
        }

        [Test]
        public void GetMeterColor_AtThreeQuarters_IsBetweenYellowAndRed()
        {
            Color color = PowerMeterController.GetMeterColor(0.75f);
            // Should be midpoint between yellow and red
            float expectedR = Mathf.Lerp(1f, 0.85f, 0.5f);
            float expectedG = Mathf.Lerp(0.84f, 0.26f, 0.5f);
            Assert.AreEqual(expectedR, color.r, 0.01f);
            Assert.AreEqual(expectedG, color.g, 0.01f);
        }

        [Test]
        public void GetMeterColor_MonotonicallyIncreasesRed()
        {
            Color low = PowerMeterController.GetMeterColor(0.1f);
            Color mid = PowerMeterController.GetMeterColor(0.5f);
            Color high = PowerMeterController.GetMeterColor(0.9f);

            Assert.Less(low.r, mid.r, "Red should increase from low to mid");
            // mid to high: yellow (1.0) to red (0.85) — red decreases slightly
            // but green drops significantly, making it "more red" visually
            Assert.Greater(high.r, high.g, "At high values, red should exceed green");
        }
    }
}
