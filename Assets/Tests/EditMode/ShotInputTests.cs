using NUnit.Framework;
using UnityEngine;
using GolfGame.Golf;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for ShotInput meter phases and event firing.
    /// </summary>
    public class ShotInputTests
    {
        private GameObject shotInputObj;
        private ShotInput shotInput;

        [SetUp]
        public void SetUp()
        {
            shotInputObj = new GameObject("ShotInput");
            shotInput = shotInputObj.AddComponent<ShotInput>();
        }

        [TearDown]
        public void TearDown()
        {
            if (shotInputObj != null)
            {
                Object.DestroyImmediate(shotInputObj);
            }
        }

        [Test]
        public void InitialPhase_IsIdle()
        {
            Assert.AreEqual(ShotInput.MeterPhase.Idle, shotInput.CurrentPhase);
        }

        [Test]
        public void InitialPower_IsZero()
        {
            Assert.AreEqual(0f, shotInput.CurrentPower);
        }

        [Test]
        public void InitialAimAngle_IsZero()
        {
            Assert.AreEqual(0f, shotInput.CurrentAimAngle);
        }

        [Test]
        public void IsActive_DefaultsFalse()
        {
            Assert.IsFalse(shotInput.IsActive);
        }

        [Test]
        public void OnShotReady_EventExists()
        {
            // Verify event can be subscribed without error
            bool fired = false;
            shotInput.OnShotReady += _ => fired = true;
            shotInput.OnShotReady -= _ => {};
            Assert.IsFalse(fired);
        }

        [Test]
        public void OnMeterPhaseChanged_EventExists()
        {
            bool fired = false;
            shotInput.OnMeterPhaseChanged += _ => fired = true;
            shotInput.OnMeterPhaseChanged -= _ => {};
            Assert.IsFalse(fired);
        }

        [Test]
        public void OnMeterValueChanged_EventExists()
        {
            bool fired = false;
            shotInput.OnMeterValueChanged += _ => fired = true;
            shotInput.OnMeterValueChanged -= _ => {};
            Assert.IsFalse(fired);
        }

        [Test]
        public void OnAccuracyValueChanged_EventExists()
        {
            bool fired = false;
            shotInput.OnAccuracyValueChanged += _ => fired = true;
            shotInput.OnAccuracyValueChanged -= _ => {};
            Assert.IsFalse(fired);
        }

        [Test]
        public void OnPowerLocked_EventExists()
        {
            bool fired = false;
            shotInput.OnPowerLocked += _ => fired = true;
            shotInput.OnPowerLocked -= _ => {};
            Assert.IsFalse(fired);
        }

        [Test]
        public void MeterPhaseEnum_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)ShotInput.MeterPhase.Idle);
            Assert.AreEqual(1, (int)ShotInput.MeterPhase.Power);
            Assert.AreEqual(2, (int)ShotInput.MeterPhase.Accuracy);
        }
    }
}
