using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Golf;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// Tests for BallController component behavior.
    /// Note: Physics simulation requires PlayMode for full integration.
    /// These tests verify component setup and method contracts.
    /// </summary>
    public class BallControllerTests
    {
        private GameObject ballObj;
        private BallController ballController;
        private Rigidbody rb;

        [SetUp]
        public void SetUp()
        {
            // Suppress warnings from Resources.Load / physics config in edit mode
            LogAssert.ignoreFailingMessages = true;

            ballObj = new GameObject("Ball");
            var collider = ballObj.AddComponent<SphereCollider>();
            rb = ballObj.AddComponent<Rigidbody>();
            ballController = ballObj.AddComponent<BallController>();
            // Awake doesn't auto-fire in edit mode — invoke to set internal rb reference
            ballController.SendMessage("Awake");
        }

        [TearDown]
        public void TearDown()
        {
            if (ballObj != null)
            {
                Object.DestroyImmediate(ballObj);
            }
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Launch_SetsNonZeroVelocity()
        {
            var shot = new ShotParameters
            {
                PowerNormalized = 1f,
                AimAngleDegrees = 0f,
                BackspinRpm = 3000f,
                SidespinRpm = 0f
            };

            ballController.Launch(shot);

            Assert.Greater(rb.linearVelocity.magnitude, 0f,
                "Ball should have velocity after launch");
        }

        [Test]
        public void Launch_WithZeroPower_MinimalVelocity()
        {
            var shot = new ShotParameters
            {
                PowerNormalized = 0f,
                AimAngleDegrees = 0f,
                BackspinRpm = 0f,
                SidespinRpm = 0f
            };

            ballController.Launch(shot);

            Assert.AreEqual(0f, rb.linearVelocity.magnitude, 0.1f,
                "Zero power should result in near-zero velocity");
        }

        [Test]
        public void Launch_SetsIsFlying()
        {
            var shot = new ShotParameters { PowerNormalized = 0.5f };
            ballController.Launch(shot);

            Assert.IsTrue(ballController.IsFlying);
        }

        [Test]
        public void Launch_NullParameters_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ballController.Launch(null));
            Assert.IsFalse(ballController.IsFlying);
        }

        [Test]
        public void ResetToTee_RestoresPosition()
        {
            ballObj.transform.position = new Vector3(100f, 50f, 200f);
            ballController.ResetToTee();

            Assert.AreEqual(Vector3.zero, ballObj.transform.position,
                "Ball should return to initial position");
        }

        [Test]
        public void ResetToTee_ZerosVelocity()
        {
            rb.linearVelocity = new Vector3(10f, 10f, 10f);
            ballController.ResetToTee();

            Assert.AreEqual(0f, rb.linearVelocity.magnitude, 0.001f);
        }

        [Test]
        public void ResetToTee_SetsNotFlying()
        {
            var shot = new ShotParameters { PowerNormalized = 0.5f };
            ballController.Launch(shot);

            ballController.ResetToTee();

            Assert.IsFalse(ballController.IsFlying);
        }

        [Test]
        public void SetTeePosition_AffectsReset()
        {
            var newTee = new Vector3(10f, 0f, 20f);
            ballController.SetTeePosition(newTee);
            ballController.ResetToTee();

            Assert.AreEqual(newTee, ballObj.transform.position);
        }

        [Test]
        public void OnBallLanded_EventExists()
        {
            // Verify event can be subscribed to without error
            bool fired = false;
            ballController.OnBallLanded += _ => fired = true;
            ballController.OnBallLanded -= _ => fired = true;
            Assert.IsFalse(fired);
        }

        [Test]
        public void OnBallBounced_EventExists()
        {
            bool fired = false;
            ballController.OnBallBounced += (_, _) => fired = true;
            ballController.OnBallBounced -= (_, _) => fired = true;
            Assert.IsFalse(fired);
        }

        [Test]
        public void Launch_WhileAlreadyFlying_DoesNothing()
        {
            var shot = new ShotParameters { PowerNormalized = 0.5f };
            ballController.Launch(shot);
            var velocity1 = rb.linearVelocity;

            // Second launch should be ignored
            var shot2 = new ShotParameters { PowerNormalized = 1f };
            ballController.Launch(shot2);
            var velocity2 = rb.linearVelocity;

            Assert.AreEqual(velocity1, velocity2,
                "Second launch while flying should be ignored");
        }
    }
}
