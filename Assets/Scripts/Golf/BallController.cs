using System;
using UnityEngine;
using GolfGame.Core;

namespace GolfGame.Golf
{
    public class BallController : MonoBehaviour
    {
        [Header("Launch Settings")]
        [SerializeField] private float launchForce = 15f;  // Newtons*seconds impulse
        [SerializeField] private float loftAngle = 25f;

        private GameManager gameManager;
        private Rigidbody rb;
        private bool isFlying;
        private float flightTimer;
        private Vector3 initialTeePosition;

        public event Action<Vector3> OnBallLanded;
        public event Action<Vector3, float> OnBallBounced;
        public event Action OnBallLaunched;
        public bool IsFlying => isFlying;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

            rb.mass = 0.046f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
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
            var loftRotation = Quaternion.AngleAxis(loftAngle, rightAxis);
            var launchDir = loftRotation * aimed;

            float force = launchForce * shot.PowerNormalized;

            flightTimer = 0f;
            isFlying = true;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(launchDir.normalized * force, ForceMode.Impulse);
            OnBallLaunched?.Invoke();

            Debug.Log($"[BallController] Launched: force={force:F1} dir={launchDir}");
        }

        private void FixedUpdate()
        {
            if (!isFlying) return;

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
