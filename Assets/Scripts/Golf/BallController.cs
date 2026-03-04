using System;
using UnityEngine;
using GolfGame.Core;

namespace GolfGame.Golf
{
    public class BallController : MonoBehaviour
    {
        [Header("Launch Settings")]
        [SerializeField] private float launchSpeed = 30f;
        [SerializeField] private float loftAngle = 25f;

        private GameManager gameManager;
        private Rigidbody rb;
        private bool isFlying;
        private Vector3 initialTeePosition;

        public event Action<Vector3> OnBallLanded;
        public event Action<Vector3, float> OnBallBounced;
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
            initialTeePosition = transform.position;
            ResetToTee();
        }

        public void Launch(ShotParameters shot)
        {
            if (shot == null || isFlying) return;

            // Aim direction (horizontal)
            var aimRotation = Quaternion.AngleAxis(shot.AimAngleDegrees, Vector3.up);
            var aimed = aimRotation * Vector3.forward;

            // Apply loft
            var loftRotation = Quaternion.AngleAxis(-loftAngle, Vector3.Cross(aimed, Vector3.up).normalized);
            var launchDir = loftRotation * aimed;

            float speed = launchSpeed * shot.PowerNormalized;
            var launchVelocity = launchDir.normalized * speed;

            isFlying = true;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(launchVelocity, ForceMode.VelocityChange);

            Debug.Log($"[BallController] Launched: speed={speed:F1} dir={launchDir} vel={launchVelocity}");
        }

        private void FixedUpdate()
        {
            if (!isFlying) return;

            // Ball has settled: slow + on ground
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

        private void OnCollisionEnter(Collision collision)
        {
            if (!isFlying) return;
            OnBallBounced?.Invoke(transform.position, rb.linearVelocity.magnitude);
        }
    }
}
