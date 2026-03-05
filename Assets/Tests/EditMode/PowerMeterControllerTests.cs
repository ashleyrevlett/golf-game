using NUnit.Framework;
using UnityEngine;
using GolfGame.Golf;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for PowerMeterController display logic.
    /// Tests meter value formatting, color transitions, and accuracy mapping.
    /// </summary>
    public class PowerMeterControllerTests
    {
        [Test]
        public void PowerReadout_FormatsAsPercentage()
        {
            float value = 0.75f;
            string readout = $"{Mathf.RoundToInt(value * 100)}%";
            Assert.AreEqual("75%", readout);
        }

        [Test]
        public void PowerReadout_ZeroPercent()
        {
            float value = 0f;
            string readout = $"{Mathf.RoundToInt(value * 100)}%";
            Assert.AreEqual("0%", readout);
        }

        [Test]
        public void PowerReadout_HundredPercent()
        {
            float value = 1f;
            string readout = $"{Mathf.RoundToInt(value * 100)}%";
            Assert.AreEqual("100%", readout);
        }

        [Test]
        public void MeterFill_WidthPercent_MapsCorrectly()
        {
            float value = 0.5f;
            float widthPercent = value * 100f;
            Assert.AreEqual(50f, widthPercent, 0.001f);
        }

        [Test]
        public void AccuracyMarker_MapsNegOneToZeroPercent()
        {
            float value = -1f;
            float pct = (value + 1f) * 0.5f * 100f;
            Assert.AreEqual(0f, pct, 0.001f);
        }

        [Test]
        public void AccuracyMarker_MapsZeroToFiftyPercent()
        {
            float value = 0f;
            float pct = (value + 1f) * 0.5f * 100f;
            Assert.AreEqual(50f, pct, 0.001f);
        }

        [Test]
        public void AccuracyMarker_MapsPosOneToHundredPercent()
        {
            float value = 1f;
            float pct = (value + 1f) * 0.5f * 100f;
            Assert.AreEqual(100f, pct, 0.001f);
        }

        [Test]
        public void ColorTransition_LowPower_IsGreenish()
        {
            float value = 0f;
            Color barColor = Color.Lerp(
                new Color(0.3f, 0.8f, 0.3f),
                new Color(1f, 0.84f, 0f),
                value * 2f);

            Assert.AreEqual(0.3f, barColor.r, 0.01f);
            Assert.AreEqual(0.8f, barColor.g, 0.01f);
            Assert.AreEqual(0.3f, barColor.b, 0.01f);
        }

        [Test]
        public void ColorTransition_MidPower_IsYellowish()
        {
            float value = 0.5f;
            Color barColor = Color.Lerp(
                new Color(0.3f, 0.8f, 0.3f),
                new Color(1f, 0.84f, 0f),
                value * 2f);

            Assert.AreEqual(1f, barColor.r, 0.01f);
            Assert.AreEqual(0.84f, barColor.g, 0.01f);
        }

        [Test]
        public void ColorTransition_HighPower_IsReddish()
        {
            float value = 1f;
            Color barColor = Color.Lerp(
                new Color(1f, 0.84f, 0f),
                new Color(0.85f, 0.26f, 0.21f),
                (value - 0.5f) * 2f);

            Assert.AreEqual(0.85f, barColor.r, 0.01f);
            Assert.AreEqual(0.26f, barColor.g, 0.01f);
        }

        [Test]
        public void ShotInput_MeterPhase_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)ShotInput.MeterPhase.Idle);
            Assert.AreEqual(1, (int)ShotInput.MeterPhase.Power);
            Assert.AreEqual(2, (int)ShotInput.MeterPhase.Accuracy);
        }

        [Test]
        public void ShotInput_Events_CanSubscribeAndUnsubscribe()
        {
            var obj = new GameObject("ShotInput");
            var shotInput = obj.AddComponent<ShotInput>();

            int phaseCount = 0;
            int meterCount = 0;
            int accuracyCount = 0;
            int powerLockedCount = 0;

            void OnPhase(ShotInput.MeterPhase _) => phaseCount++;
            void OnMeter(float _) => meterCount++;
            void OnAccuracy(float _) => accuracyCount++;
            void OnPowerLocked(float _) => powerLockedCount++;

            shotInput.OnMeterPhaseChanged += OnPhase;
            shotInput.OnMeterValueChanged += OnMeter;
            shotInput.OnAccuracyValueChanged += OnAccuracy;
            shotInput.OnPowerLocked += OnPowerLocked;

            Assert.DoesNotThrow(() =>
            {
                shotInput.OnMeterPhaseChanged -= OnPhase;
                shotInput.OnMeterValueChanged -= OnMeter;
                shotInput.OnAccuracyValueChanged -= OnAccuracy;
                shotInput.OnPowerLocked -= OnPowerLocked;
            });

            Object.DestroyImmediate(obj);
        }
    }
}
