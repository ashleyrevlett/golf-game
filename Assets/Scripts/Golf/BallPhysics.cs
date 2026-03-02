using UnityEngine;

namespace GolfGame.Golf
{
    /// <summary>
    /// Pure math utilities for ball physics calculations.
    /// Stateless — all methods are static for easy testing.
    /// </summary>
    public static class BallPhysics
    {
        /// <summary>
        /// Calculate the launch force vector from shot parameters.
        /// </summary>
        public static Vector3 CalculateLaunchVelocity(
            ShotParameters shot,
            float maxPowerMph,
            float mphToForce,
            float loftAngleDegrees,
            Vector3 forward)
        {
            float speedMps = shot.PowerMph(maxPowerMph) * mphToForce;

            // Rotate forward by aim angle (horizontal)
            var aimRotation = Quaternion.AngleAxis(shot.AimAngleDegrees, Vector3.up);
            var aimedForward = aimRotation * forward;

            // Apply loft angle (vertical)
            var loftRotation = Quaternion.AngleAxis(-loftAngleDegrees, Vector3.Cross(aimedForward, Vector3.up));
            var launchDirection = loftRotation * aimedForward;

            return launchDirection.normalized * speedMps;
        }

        /// <summary>
        /// Calculate aerodynamic drag force opposing velocity.
        /// Scales with speed squared.
        /// </summary>
        public static Vector3 CalculateDragForce(Vector3 velocity, float dragCoefficient)
        {
            float speed = velocity.magnitude;
            if (speed < 0.001f) return Vector3.zero;

            return -velocity.normalized * dragCoefficient * speed * speed;
        }

        /// <summary>
        /// Calculate Magnus force from spin and velocity.
        /// Perpendicular to both spin axis and velocity.
        /// Backspin creates lift, sidespin creates curve.
        /// </summary>
        public static Vector3 CalculateMagnusForce(
            Vector3 velocity,
            float backspinRpm,
            float sidespinRpm,
            float magnusCoefficient)
        {
            if (velocity.sqrMagnitude < 0.001f) return Vector3.zero;

            // Backspin axis is perpendicular to velocity in the horizontal plane
            var horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontalVelocity.sqrMagnitude < 0.001f) return Vector3.zero;

            var backspinAxis = Vector3.Cross(horizontalVelocity.normalized, Vector3.up);

            // Sidespin axis is vertical
            var sidespinAxis = Vector3.up;

            // Combined spin vector
            var spinVector = backspinAxis * backspinRpm + sidespinAxis * sidespinRpm;

            // Magnus force = coefficient * (spin x velocity)
            return magnusCoefficient * Vector3.Cross(spinVector, velocity);
        }

        /// <summary>
        /// Decay spin values. Returns (newBackspin, newSidespin).
        /// </summary>
        public static (float backspin, float sidespin) DecaySpin(
            float backspinRpm,
            float sidespinRpm,
            float decayFactor)
        {
            return (backspinRpm * decayFactor, sidespinRpm * decayFactor);
        }
    }
}
