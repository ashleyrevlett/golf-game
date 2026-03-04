using System;
using UnityEngine;
using GolfGame.Core;

namespace GolfGame.Golf
{
    /// <summary>
    /// Controls ball physics during flight, bounce, and roll.
    /// Attach to a GameObject with Rigidbody and SphereCollider.
    /// </summary>
    public class BallController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private BallPhysicsConfig config;

        private WindSystem windSystem;
        private GameManager gameManager;
        private Transform teePosition;

        private Rigidbody rb;
        private bool isFlying;
        private bool isGrounded;
        private float currentBackspinRpm;
        private float currentSidespinRpm;
        private int stopFrameCount;
        private float flightTimer;
        private Vector3 initialTeePosition;

        /// <summary>
        /// Fires when ball has come to rest. Payload is final position.
        /// </summary>
        public event Action<Vector3> OnBallLanded;

        /// <summary>
        /// Fires on ground collision. Payload: (position, velocity magnitude).
        /// </summary>
        public event Action<Vector3, float> OnBallBounced;

        /// <summary>
        /// Whether the ball is currently in flight/motion.
        /// </summary>
        public bool IsFlying => isFlying;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            ConfigureRigidbody();
        }

        private void Start()
        {
            windSystem = FindFirstObjectByType<WindSystem>();
            gameManager = FindFirstObjectByType<GameManager>();
            teePosition = GameObject.Find("TeeBox")?.transform;

            initialTeePosition = teePosition != null ? teePosition.position : transform.position;
            ResetToTee();
        }

        private void ConfigureRigidbody()
        {
            if (config != null)
            {
                rb.mass = config.BallMass;
            }
            else
            {
                rb.mass = 0.046f;
            }

            rb.useGravity = true;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        /// <summary>
        /// Launch the ball with the given shot parameters.
        /// </summary>
        public void Launch(ShotParameters shot)
        {
            if (shot == null || isFlying) return;

            float maxPower = config != null ? config.MaxPowerMph : 150f;
            float mphToForce = config != null ? config.MphToForceMultiplier : 0.6f;
            float loftAngle = config != null ? config.DefaultLoftAngle : 30f;

            var launchVelocity = BallPhysics.CalculateLaunchVelocity(
                shot, maxPower, mphToForce, loftAngle, transform.forward);

            currentBackspinRpm = shot.BackspinRpm;
            currentSidespinRpm = shot.SidespinRpm;
            stopFrameCount = 0;
            flightTimer = 0f;
            isFlying = true;
            isGrounded = false;

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(launchVelocity, ForceMode.VelocityChange);
        }

        private void FixedUpdate()
        {
            if (!isFlying) return;

            float dt = Time.fixedDeltaTime;
            flightTimer += dt;

            // Timeout failsafe
            float timeout = config != null ? config.FlightTimeout : 30f;
            if (flightTimer >= timeout)
            {
                Debug.LogWarning("[BallController] Flight timeout — forcing stop");
                ForceStop();
                return;
            }

            ApplyAerodynamics(dt);
            CheckStopCondition();
        }

        private void ApplyAerodynamics(float dt)
        {
            if (isGrounded) return;

            var velocity = rb.linearVelocity;

            float dragCoeff = config != null ? config.DragCoefficient : 0.25f;
            float magnusCoeff = config != null ? config.MagnusCoefficient : 0.0001f;
            float spinDecay = config != null ? config.SpinDecayAir : 0.98f;

            // Drag
            var dragForce = BallPhysics.CalculateDragForce(velocity, dragCoeff);
            rb.AddForce(dragForce * dt, ForceMode.VelocityChange);

            // Magnus force (spin effects)
            var magnusForce = BallPhysics.CalculateMagnusForce(
                velocity, currentBackspinRpm, currentSidespinRpm, magnusCoeff);
            rb.AddForce(magnusForce * dt, ForceMode.VelocityChange);

            // Wind
            if (windSystem != null)
            {
                rb.AddForce(windSystem.CurrentWind * dt, ForceMode.VelocityChange);
            }

            // Spin decay
            var decayed = BallPhysics.DecaySpin(currentBackspinRpm, currentSidespinRpm, spinDecay);
            currentBackspinRpm = decayed.backspin;
            currentSidespinRpm = decayed.sidespin;
        }

        private void CheckStopCondition()
        {
            float velThreshold = config != null ? config.StopVelocityThreshold : 0.1f;
            float angThreshold = config != null ? config.StopAngularThreshold : 0.05f;
            int requiredFrames = config != null ? config.StopConsecutiveFrames : 10;

            bool isSlow = rb.linearVelocity.magnitude < velThreshold
                          && rb.angularVelocity.magnitude < angThreshold;

            if (isSlow && isGrounded)
            {
                stopFrameCount++;
                if (stopFrameCount >= requiredFrames)
                {
                    BallStopped();
                }
            }
            else
            {
                stopFrameCount = 0;
            }
        }

        private void BallStopped()
        {
            isFlying = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            var landPosition = transform.position;
            Debug.Log($"[BallController] Ball stopped at {landPosition}");

            OnBallLanded?.Invoke(landPosition);

            if (gameManager != null)
            {
                gameManager.BallLanded();
            }
        }

        private void ForceStop()
        {
            BallStopped();
        }

        /// <summary>
        /// Reset ball to tee position with zero velocity.
        /// </summary>
        public void ResetToTee()
        {
            isFlying = false;
            isGrounded = false;
            stopFrameCount = 0;
            flightTimer = 0f;
            currentBackspinRpm = 0f;
            currentSidespinRpm = 0f;

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            transform.position = initialTeePosition;
        }

        /// <summary>
        /// Set the tee position for resets.
        /// </summary>
        public void SetTeePosition(Vector3 position)
        {
            initialTeePosition = position;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isFlying) return;

            isGrounded = true;

            float spinDecayBounce = config != null ? config.SpinDecayBounce : 0.6f;
            var decayed = BallPhysics.DecaySpin(currentBackspinRpm, currentSidespinRpm, spinDecayBounce);
            currentBackspinRpm = decayed.backspin;
            currentSidespinRpm = decayed.sidespin;

            OnBallBounced?.Invoke(transform.position, rb.linearVelocity.magnitude);
        }

        private void OnCollisionExit(Collision collision)
        {
            // Ball left the ground (bounced up)
        }
    }
}
