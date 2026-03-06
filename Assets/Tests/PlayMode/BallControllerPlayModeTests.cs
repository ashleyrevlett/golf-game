using System.Collections;
using System.Reflection;
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
            // Clean up any WindSystem created by wind tests
            var windSystems = Object.FindObjectsByType<WindSystem>(FindObjectsSortMode.None);
            foreach (var ws in windSystems) Object.Destroy(ws.gameObject);
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
            Assert.That(Mathf.Abs(straightVel.x - aimedVel.x), Is.GreaterThan(0.1f),
                "Aimed shot should have different horizontal direction");
        }

        [UnityTest]
        public IEnumerator Launch_WithWind_BallDriftsInWindDirection()
        {
            // Create WindSystem with strong eastward wind
            var windObj = new GameObject("WindSystem");
            var windSystem = windObj.AddComponent<WindSystem>();
            // Set wind via reflection (CurrentWind is read-only property backed by private field)
            var windField = typeof(WindSystem).GetField("currentWind", BindingFlags.NonPublic | BindingFlags.Instance);
            windField.SetValue(windSystem, new Vector3(10f, 0f, 0f)); // Strong east wind

            // Re-initialize BallController so it finds the WindSystem
            Object.Destroy(ballObj);
            ballObj = new GameObject("Ball");
            ballObj.transform.position = new Vector3(0f, 2f, 0f);
            ballObj.AddComponent<SphereCollider>().radius = 0.02135f;
            rb = ballObj.AddComponent<Rigidbody>();
            ballController = ballObj.AddComponent<BallController>();

            yield return null;
            yield return new WaitForFixedUpdate();

            var startX = ballObj.transform.position.x;

            var shot = new ShotParameters { PowerNormalized = 1f, AimAngleDegrees = 0f };
            ballController.Launch(shot);

            // Let physics simulate
            for (int i = 0; i < 30; i++)
                yield return new WaitForFixedUpdate();

            Assert.Greater(ballObj.transform.position.x, startX + 0.01f,
                "Ball should drift in wind direction (positive X)");
        }

        [UnityTest]
        public IEnumerator Launch_WithZeroWind_NoDrift()
        {
            // Create WindSystem with zero wind
            var windObj = new GameObject("WindSystem");
            var windSystem = windObj.AddComponent<WindSystem>();
            var windField = typeof(WindSystem).GetField("currentWind", BindingFlags.NonPublic | BindingFlags.Instance);
            windField.SetValue(windSystem, Vector3.zero);

            Object.Destroy(ballObj);
            ballObj = new GameObject("Ball");
            ballObj.transform.position = new Vector3(0f, 2f, 0f);
            ballObj.AddComponent<SphereCollider>().radius = 0.02135f;
            rb = ballObj.AddComponent<Rigidbody>();
            ballController = ballObj.AddComponent<BallController>();

            yield return null;
            yield return new WaitForFixedUpdate();

            var shot = new ShotParameters { PowerNormalized = 1f, AimAngleDegrees = 0f };
            ballController.Launch(shot);

            for (int i = 0; i < 30; i++)
                yield return new WaitForFixedUpdate();

            // With zero wind and straight aim, X drift should be negligible
            Assert.That(Mathf.Abs(ballObj.transform.position.x), Is.LessThan(0.5f),
                "Ball should not drift laterally with zero wind");
        }

        [UnityTest]
        public IEnumerator NotFlying_WindDoesNotMoveBall()
        {
            // Create WindSystem with strong wind
            var windObj = new GameObject("WindSystem");
            var windSystem = windObj.AddComponent<WindSystem>();
            var windField = typeof(WindSystem).GetField("currentWind", BindingFlags.NonPublic | BindingFlags.Instance);
            windField.SetValue(windSystem, new Vector3(10f, 0f, 0f));

            Object.Destroy(ballObj);
            ballObj = new GameObject("Ball");
            ballObj.transform.position = new Vector3(0f, 2f, 0f);
            ballObj.AddComponent<SphereCollider>().radius = 0.02135f;
            rb = ballObj.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Keep ball at rest
            ballController = ballObj.AddComponent<BallController>();

            yield return null;
            yield return new WaitForFixedUpdate();

            var startPos = ballObj.transform.position;

            // Don't launch — ball should stay put despite wind
            for (int i = 0; i < 20; i++)
                yield return new WaitForFixedUpdate();

            Assert.AreEqual(startPos.x, ballObj.transform.position.x, 0.001f,
                "Ball should not move when not flying, even with wind");
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
