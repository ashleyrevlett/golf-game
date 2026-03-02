using UnityEngine;

namespace GolfGame.Golf
{
    /// <summary>
    /// ScriptableObject holding all tunable ball physics parameters.
    /// Change values without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "BallPhysicsConfig", menuName = "Golf/Ball Physics Config")]
    public class BallPhysicsConfig : ScriptableObject
    {
        [Header("Launch")]
        [SerializeField] private float defaultLoftAngle = 30f;
        [SerializeField] private float maxPowerMph = 150f;
        [SerializeField] private float mphToForceMultiplier = 0.6f;

        [Header("Aerodynamics")]
        [SerializeField] private float dragCoefficient = 0.25f;
        [SerializeField] private float magnusCoefficient = 0.0001f;
        [SerializeField] private float spinDecayAir = 0.98f;
        [SerializeField] private float spinDecayBounce = 0.6f;

        [Header("Ground")]
        [SerializeField] private float bounceRestitution = 0.5f;
        [SerializeField] private float friction = 0.4f;
        [SerializeField] private float rollingFriction = 0.1f;
        [SerializeField] private float stopVelocityThreshold = 0.1f;
        [SerializeField] private float stopAngularThreshold = 0.05f;
        [SerializeField] private int stopConsecutiveFrames = 10;
        [SerializeField] private float flightTimeout = 30f;

        [Header("Wind")]
        [SerializeField] private float windMinSpeed;
        [SerializeField] private float windMaxSpeed = 8f;

        [Header("Ball")]
        [SerializeField] private float ballMass = 0.046f;
        [SerializeField] private float ballRadius = 0.02135f;

        public float DefaultLoftAngle => defaultLoftAngle;
        public float MaxPowerMph => maxPowerMph;
        public float MphToForceMultiplier => mphToForceMultiplier;
        public float DragCoefficient => dragCoefficient;
        public float MagnusCoefficient => magnusCoefficient;
        public float SpinDecayAir => spinDecayAir;
        public float SpinDecayBounce => spinDecayBounce;
        public float BounceRestitution => bounceRestitution;
        public float Friction => friction;
        public float RollingFriction => rollingFriction;
        public float StopVelocityThreshold => stopVelocityThreshold;
        public float StopAngularThreshold => stopAngularThreshold;
        public int StopConsecutiveFrames => stopConsecutiveFrames;
        public float FlightTimeout => flightTimeout;
        public float WindMinSpeed => windMinSpeed;
        public float WindMaxSpeed => windMaxSpeed;
        public float BallMass => ballMass;
        public float BallRadius => ballRadius;
    }
}
