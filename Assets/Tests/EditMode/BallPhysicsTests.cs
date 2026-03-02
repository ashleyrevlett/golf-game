using NUnit.Framework;
using UnityEngine;
using GolfGame.Golf;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// Tests for BallPhysics static math utilities and ShotParameters.
    /// </summary>
    public class BallPhysicsTests
    {
        // --- ShotParameters ---

        [Test]
        public void ShotParameters_PowerMph_AtHalf_ReturnsHalfMax()
        {
            var shot = new ShotParameters { PowerNormalized = 0.5f };
            Assert.AreEqual(75f, shot.PowerMph(150f), 0.001f);
        }

        [Test]
        public void ShotParameters_PowerMph_AtZero_ReturnsZero()
        {
            var shot = new ShotParameters { PowerNormalized = 0f };
            Assert.AreEqual(0f, shot.PowerMph(150f), 0.001f);
        }

        [Test]
        public void ShotParameters_PowerMph_AtMax_ReturnsMax()
        {
            var shot = new ShotParameters { PowerNormalized = 1f };
            Assert.AreEqual(150f, shot.PowerMph(150f), 0.001f);
        }

        // --- Launch Velocity ---

        [Test]
        public void LaunchVelocity_ZeroPower_ReturnsZero()
        {
            var shot = new ShotParameters { PowerNormalized = 0f, AimAngleDegrees = 0f };
            var velocity = BallPhysics.CalculateLaunchVelocity(
                shot, 150f, 0.6f, 30f, Vector3.forward);

            Assert.AreEqual(0f, velocity.magnitude, 0.001f);
        }

        [Test]
        public void LaunchVelocity_FullPower_HasExpectedMagnitude()
        {
            var shot = new ShotParameters { PowerNormalized = 1f, AimAngleDegrees = 0f };
            var velocity = BallPhysics.CalculateLaunchVelocity(
                shot, 150f, 0.6f, 30f, Vector3.forward);

            // 150 mph * 0.6 = 90 units/s
            Assert.AreEqual(90f, velocity.magnitude, 0.1f);
        }

        [Test]
        public void LaunchVelocity_HasUpwardComponent_FromLoft()
        {
            var shot = new ShotParameters { PowerNormalized = 1f, AimAngleDegrees = 0f };
            var velocity = BallPhysics.CalculateLaunchVelocity(
                shot, 150f, 0.6f, 30f, Vector3.forward);

            Assert.Greater(velocity.y, 0f, "Loft angle should create upward velocity");
        }

        [Test]
        public void LaunchVelocity_AimAngle_AffectsHorizontalDirection()
        {
            var shotCenter = new ShotParameters { PowerNormalized = 1f, AimAngleDegrees = 0f };
            var shotRight = new ShotParameters { PowerNormalized = 1f, AimAngleDegrees = 20f };

            var velCenter = BallPhysics.CalculateLaunchVelocity(
                shotCenter, 150f, 0.6f, 30f, Vector3.forward);
            var velRight = BallPhysics.CalculateLaunchVelocity(
                shotRight, 150f, 0.6f, 30f, Vector3.forward);

            Assert.Greater(velRight.x, velCenter.x,
                "Positive aim angle should shift velocity rightward");
        }

        // --- Drag Force ---

        [Test]
        public void DragForce_OpposesVelocity()
        {
            var velocity = new Vector3(10f, 5f, 10f);
            var drag = BallPhysics.CalculateDragForce(velocity, 0.25f);

            float dot = Vector3.Dot(drag, velocity);
            Assert.Less(dot, 0f, "Drag should oppose velocity direction");
        }

        [Test]
        public void DragForce_ScalesWithSpeedSquared()
        {
            var velocity1 = new Vector3(0f, 0f, 10f);
            var velocity2 = new Vector3(0f, 0f, 20f);

            float drag1 = BallPhysics.CalculateDragForce(velocity1, 0.25f).magnitude;
            float drag2 = BallPhysics.CalculateDragForce(velocity2, 0.25f).magnitude;

            // Double speed = 4x drag
            Assert.AreEqual(drag1 * 4f, drag2, 0.1f);
        }

        [Test]
        public void DragForce_ZeroVelocity_ReturnsZero()
        {
            var drag = BallPhysics.CalculateDragForce(Vector3.zero, 0.25f);
            Assert.AreEqual(0f, drag.magnitude, 0.001f);
        }

        // --- Magnus Force ---

        [Test]
        public void MagnusForce_Backspin_CreatesLift()
        {
            var velocity = new Vector3(0f, 0f, 20f); // moving forward
            var magnus = BallPhysics.CalculateMagnusForce(velocity, 3000f, 0f, 0.0001f);

            Assert.Greater(magnus.y, 0f, "Backspin should create upward lift");
        }

        [Test]
        public void MagnusForce_Sidespin_CreatesLateralForce()
        {
            var velocity = new Vector3(0f, 0f, 20f); // moving forward
            var magnus = BallPhysics.CalculateMagnusForce(velocity, 0f, 3000f, 0.0001f);

            Assert.AreNotEqual(0f, magnus.x,
                "Sidespin should create lateral force");
        }

        [Test]
        public void MagnusForce_PerpendicularToVelocity()
        {
            var velocity = new Vector3(5f, 5f, 20f);
            var magnus = BallPhysics.CalculateMagnusForce(velocity, 3000f, 1000f, 0.0001f);

            if (magnus.magnitude > 0.001f)
            {
                float dot = Vector3.Dot(magnus.normalized, velocity.normalized);
                Assert.AreEqual(0f, dot, 0.2f,
                    "Magnus force should be roughly perpendicular to velocity");
            }
        }

        [Test]
        public void MagnusForce_ZeroVelocity_ReturnsZero()
        {
            var magnus = BallPhysics.CalculateMagnusForce(Vector3.zero, 3000f, 1000f, 0.0001f);
            Assert.AreEqual(0f, magnus.magnitude, 0.001f);
        }

        // --- Spin Decay ---

        [Test]
        public void SpinDecay_ReducesSpin()
        {
            var (backspin, sidespin) = BallPhysics.DecaySpin(3000f, 1000f, 0.98f);
            Assert.Less(backspin, 3000f);
            Assert.Less(sidespin, 1000f);
        }

        [Test]
        public void SpinDecay_CorrectFactor()
        {
            var (backspin, sidespin) = BallPhysics.DecaySpin(3000f, 1000f, 0.5f);
            Assert.AreEqual(1500f, backspin, 0.001f);
            Assert.AreEqual(500f, sidespin, 0.001f);
        }

        [Test]
        public void SpinDecay_ZeroSpin_StaysZero()
        {
            var (backspin, sidespin) = BallPhysics.DecaySpin(0f, 0f, 0.98f);
            Assert.AreEqual(0f, backspin, 0.001f);
            Assert.AreEqual(0f, sidespin, 0.001f);
        }
    }
}
