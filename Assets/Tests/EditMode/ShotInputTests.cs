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
    }
}
