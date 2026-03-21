using System;
using UnityEngine;
using GolfGame.Core;

namespace GolfGame.Golf
{
    public class BallController : MonoBehaviour
    {
        [Header("Physics Config")]
        [SerializeField] private BallPhysicsConfig physicsConfig;

        private GameManager gameManager;
        private WindSystem windSystem;
        private Rigidbody rb;
        private bool isFlying;
        private float flightTimer;
        private Vector3 initialTeePosition;

        public event Action<Vector3> OnBallLanded;
        public event Action<Vector3, float> OnBallBounced;
        public event Action OnBallLaunched;
        public bool IsFlying => isFlying;

        private float LaunchForce => physicsConfig != null
            ? physicsConfig.MaxPowerMph * physicsConfig.MphToForceMultiplier
            : 15f;

        private float LoftAngle => physicsConfig != null
            ? physicsConfig.DefaultLoftAngle
            : 25f;

        private void Awake()
        {
            if (physicsConfig == null)
            {
                physicsConfig = Resources.Load<BallPhysicsConfig>("BallPhysicsConfig");
                if (physicsConfig == null)
                    Debug.LogWarning("[BallController] No BallPhysicsConfig found — using defaults");
            }

            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

            rb.mass = physicsConfig != null ? physicsConfig.BallMass : 0.046f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            windSystem = FindFirstObjectByType<WindSystem>();
            if (gameManager != null)
                gameManager.OnResetToTee += ResetToTee;
            initialTeePosition = transform.position;
            ResetToTee();
        }

        public void Launch(ShotParameters shot)
        {
            if (shot == null || isFlying) return;

            // Aim direction (horizontal)
            var aimRotation = Quaternion.AngleAxis(shot.AimAngleDegrees, Vector3.up);
            var aimed = aimRotation * Vector3.forward;

            // Apply loft upward: rotate around the right axis (positive = up)
            var rightAxis = Vector3.Cross(Vector3.up, aimed).normalized;
            var loftRotation = Quaternion.AngleAxis(LoftAngle, rightAxis);
            var launchDir = loftRotation * aimed;

            float force = LaunchForce * shot.PowerNormalized;

            flightTimer = 0f;
            isFlying = true;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(launchDir.normalized * force, ForceMode.Impulse);
            OnBallLaunched?.Invoke();
            HapticsManager.TriggerShotHaptic(shot.PowerNormalized);

            Debug.Log($"[BallController] Launched: force={force:F1} dir={launchDir}");
        }

        private void FixedUpdate()
        {
            if (!isFlying) return;

            if (windSystem != null && physicsConfig != null)
            {
                Vector3 windForce = windSystem.CurrentWind * physicsConfig.WindSensitivity;
                rb.AddForce(windForce, ForceMode.Force);
            }

            flightTimer += Time.fixedDeltaTime;

            // Don't check stop until ball has had time to move (min 0.5s)
            if (flightTimer < 0.5f) return;

            bool slow = rb.linearVelocity.magnitude < 0.2f && rb.angularVelocity.magnitude < 0.1f;
            if (slow)
            {
                BallStopped();
            }
        }

        private void BallStopped()
        {
            isFlying = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            Debug.Log($"[BallController] Stopped at {transform.position}");
            OnBallLanded?.Invoke(transform.position);
            gameManager?.BallLanded();
        }

        public void ResetToTee()
        {
            isFlying = false;
            rb.isKinematic = true;
            transform.position = initialTeePosition;
        }

        public void SetTeePosition(Vector3 position)
        {
            initialTeePosition = position;
            transform.position = position;
        }

        private void OnDestroy()
        {
            if (gameManager != null)
                gameManager.OnResetToTee -= ResetToTee;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isFlying) return;
            OnBallBounced?.Invoke(transform.position, rb.linearVelocity.magnitude);
        }
    }
}
