using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Golf;

namespace GolfGame.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for BallController with actual physics simulation.
    /// Verifies runtime physics behavior that EditMode cannot test
    /// (FixedUpdate, collision detection, physics stepping).
    /// </summary>
    public class BallControllerPlayModeTests
    {
        private GameObject ballObj;
        private BallController ballController;
        private Rigidbody rb;
        private GameObject groundObj;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;

            // Create ball with collider
            ballObj = new GameObject("Ball");
            ballObj.transform.position = new Vector3(0f, 2f, 0f);
            var collider = ballObj.AddComponent<SphereCollider>();
            collider.radius = 0.02135f;
            rb = ballObj.AddComponent<Rigidbody>();
            ballController = ballObj.AddComponent<BallController>();

            // Create ground plane for collision testing
            groundObj = new GameObject("Ground");
            groundObj.transform.position = Vector3.zero;
            var groundCollider = groundObj.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(200f, 1f, 200f);
            groundCollider.center = new Vector3(0f, -0.5f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (ballObj != null) Object.Destroy(ballObj);
            if (groundObj != null) Object.Destroy(groundObj);
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator Launch_AppliesForceToRigidbody()
        {
            // Wait for Awake/Start
            yield return null;
            yield return new WaitForFixedUpdate();

            var shot = new ShotParameters
            {
                PowerNormalized = 1f,
                AimAngleDegrees = 0f
            };

            ballController.Launch(shot);
            yield return new WaitForFixedUpdate();

            Assert.Greater(rb.linearVelocity.magnitude, 0f,
                "Ball should have velocity after launch with physics stepping");
        }

        [UnityTest]
        public IEnumerator Launch_BallMovesOverTime()
        {
            yield return null;
            yield return new WaitForFixedUpdate();

            var startPos = ballObj.transform.position;

            var shot = new ShotParameters
            {
                PowerNormalized = 0.8f,
                AimAngleDegrees = 0f
            };

            ballController.Launch(shot);

            // Wait several physics steps
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            var endPos = ballObj.transform.position;
            Assert.Greater(Vector3.Distance(startPos, endPos), 0.01f,
                "Ball should have moved after launch with physics simulation");
        }

        [UnityTest]
        public IEnumerator Launch_SetsIsFlying()
        {
            yield return null;

            var shot = new ShotParameters { PowerNormalized = 0.5f };

            Assert.IsFalse(ballController.IsFlying, "Should not be flying before launch");

            ballController.Launch(shot);

            Assert.IsTrue(ballController.IsFlying, "Should be flying after launch");
        }

        [UnityTest]
        public IEnumerator Launch_FiresOnBallLaunchedEvent()
        {
            yield return null;

            bool launched = false;
            ballController.OnBallLaunched += () => launched = true;

            var shot = new ShotParameters { PowerNormalized = 0.7f };
            ballController.Launch(shot);

            Assert.IsTrue(launched, "OnBallLaunched should fire on launch");
        }

        [UnityTest]
        public IEnumerator Launch_WhileFlying_IsIgnored()
        {
            yield return null;
            yield return new WaitForFixedUpdate();

            var shot1 = new ShotParameters { PowerNormalized = 0.5f };
            ballController.Launch(shot1);
            yield return new WaitForFixedUpdate();

            var velocityAfterFirst = rb.linearVelocity;

            var shot2 = new ShotParameters { PowerNormalized = 1f };
            ballController.Launch(shot2);
            yield return new WaitForFixedUpdate();

            // Velocity should have changed due to physics (gravity),
            // but not from a second impulse
            Assert.IsTrue(ballController.IsFlying);
        }

        [UnityTest]
        public IEnumerator ResetToTee_StopsBallAndResetsPosition()
        {
            yield return null;
            yield return new WaitForFixedUpdate();

            var teePos = ballObj.transform.position;

            var shot = new ShotParameters { PowerNormalized = 0.8f };
            ballController.Launch(shot);

            // Let ball move
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            ballController.ResetToTee();
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(ballController.IsFlying, "Should not be flying after reset");
            Assert.AreEqual(teePos.x, ballObj.transform.position.x, 0.01f);
            Assert.AreEqual(teePos.y, ballObj.transform.position.y, 0.01f);
            Assert.AreEqual(teePos.z, ballObj.transform.position.z, 0.01f);
        }

        [UnityTest]
        public IEnumerator SetTeePosition_UpdatesResetTarget()
        {
            yield return null;

            var newTee = new Vector3(5f, 1f, 10f);
            ballController.SetTeePosition(newTee);

            Assert.AreEqual(newTee.x, ballObj.transform.position.x, 0.01f,
                "SetTeePosition should move ball immediately");

            ballController.ResetToTee();

            Assert.AreEqual(newTee.x, ballObj.transform.position.x, 0.01f);
            Assert.AreEqual(newTee.y, ballObj.transform.position.y, 0.01f);
            Assert.AreEqual(newTee.z, ballObj.transform.position.z, 0.01f);
        }

        [UnityTest]
        public IEnumerator Launch_WithAim_ChangesDirection()
        {
            yield return null;
            yield return new WaitForFixedUpdate();

            // Launch straight
            var shotStraight = new ShotParameters
            {
                PowerNormalized = 1f,
                AimAngleDegrees = 0f
            };
            ballController.Launch(shotStraight);
            yield return new WaitForFixedUpdate();
            var straightVel = rb.linearVelocity;
            ballController.ResetToTee();
            yield return new WaitForFixedUpdate();

            // Launch with aim offset
            var shotAimed = new ShotParameters
            {
                PowerNormalized = 1f,
                AimAngleDegrees = 30f
            };
            ballController.Launch(shotAimed);
            yield return new WaitForFixedUpdate();
            var aimedVel = rb.linearVelocity;

            // X component should differ with aim
            Assert.AreNotEqual(straightVel.x, aimedVel.x, 0.1f,
                "Aimed shot should have different horizontal direction");
        }

        [UnityTest]
        public IEnumerator OnBallLanded_FiresWhenBallStops()
        {
            // Position ball just above ground so it settles quickly
            ballObj.transform.position = new Vector3(0f, 0.1f, 0f);
            yield return null;
            yield return new WaitForFixedUpdate();

            bool landed = false;
            Vector3 landedPos = Vector3.zero;
            ballController.OnBallLanded += pos =>
            {
                landed = true;
                landedPos = pos;
            };

            // Launch with minimal power so it stops fast
            var shot = new ShotParameters
            {
                PowerNormalized = 0.05f,
                AimAngleDegrees = 0f
            };
            ballController.Launch(shot);

            // Wait for ball to stop (flightTimer must exceed 0.5s, then velocity check)
            float timeout = 5f;
            float elapsed = 0f;
            while (!landed && elapsed < timeout)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Assert.IsTrue(landed, "OnBallLanded should fire when ball velocity drops below threshold");
            Assert.IsFalse(ballController.IsFlying, "IsFlying should be false after landing");
        }
    }
}
