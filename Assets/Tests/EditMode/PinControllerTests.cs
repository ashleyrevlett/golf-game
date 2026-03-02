using NUnit.Framework;
using UnityEngine;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for PinController distance calculation.
    /// </summary>
    public class PinControllerTests
    {
        private GameObject pinObj;
        private PinController pinController;

        [SetUp]
        public void SetUp()
        {
            pinObj = new GameObject("Pin");
            pinController = pinObj.AddComponent<PinController>();
            pinObj.transform.position = new Vector3(0f, 0f, 114f);
        }

        [TearDown]
        public void TearDown()
        {
            if (pinObj != null)
            {
                Object.DestroyImmediate(pinObj);
            }
        }

        [Test]
        public void DistanceFromPinToItself_IsZero()
        {
            float distance = pinController.CalculateDistance(new Vector3(0f, 0f, 114f));
            Assert.AreEqual(0f, distance, 0.001f);
        }

        [Test]
        public void Distance_IgnoresYComponent()
        {
            float distAtGround = pinController.CalculateDistance(new Vector3(0f, 0f, 0f));
            float distAtHeight = pinController.CalculateDistance(new Vector3(0f, 50f, 0f));
            Assert.AreEqual(distAtGround, distAtHeight, 0.001f);
        }

        [Test]
        public void Distance_IsSymmetric()
        {
            float distLeft = pinController.CalculateDistance(new Vector3(-10f, 0f, 100f));
            float distRight = pinController.CalculateDistance(new Vector3(10f, 0f, 100f));
            Assert.AreEqual(distLeft, distRight, 0.001f);
        }

        [Test]
        public void KnownDistance_IsCorrect()
        {
            // Ball at origin, pin at (0,0,114) -> distance = 114
            float distance = pinController.CalculateDistance(Vector3.zero);
            Assert.AreEqual(114f, distance, 0.001f);
        }

        [Test]
        public void PinBasePosition_HasZeroY()
        {
            pinObj.transform.position = new Vector3(5f, 2.5f, 114f);
            Assert.AreEqual(0f, pinController.PinBasePosition.y, 0.001f);
        }

        [Test]
        public void PinBasePosition_MatchesXZ()
        {
            pinObj.transform.position = new Vector3(5f, 2.5f, 114f);
            Assert.AreEqual(5f, pinController.PinBasePosition.x, 0.001f);
            Assert.AreEqual(114f, pinController.PinBasePosition.z, 0.001f);
        }
    }
}
