using System;
using UnityEngine;
using GolfGame.Core;

namespace GolfGame.Golf
{
    /// <summary>
    /// Handles touch/mouse input for aiming and power control.
    /// Only active when GameManager is in ShotState.Ready.
    /// </summary>
    public class ShotInput : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxAimAngle = 45f;
        [SerializeField] private float aimSensitivity = 0.5f;
        [SerializeField] private float chargeSpeed = 0.5f;
        [SerializeField] private float defaultBackspinRpm = 3000f;

        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BallController ballController;
        [SerializeField] private LineRenderer aimLine;

        private enum InputState { Idle, Aiming, Charging }

        private InputState inputState;
        private float currentAimAngle;
        private float currentPower;
        private Vector2 pointerStartPosition;
        private bool isActive;

        /// <summary>
        /// Fires when a shot is ready with parameters. Consumed by BallController.
        /// </summary>
        public event Action<ShotParameters> OnShotReady;

        /// <summary>
        /// Current aim angle in degrees.
        /// </summary>
        public float CurrentAimAngle => currentAimAngle;

        /// <summary>
        /// Current power level (0-1).
        /// </summary>
        public float CurrentPower => currentPower;

        /// <summary>
        /// Whether input is currently active.
        /// </summary>
        public bool IsActive => isActive;

        private void Start()
        {
            if (gameManager != null)
            {
                gameManager.OnShotStateChanged += HandleShotStateChanged;
            }

            SetInputActive(false);
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnShotStateChanged -= HandleShotStateChanged;
            }
        }

        private void HandleShotStateChanged(ShotState newState)
        {
            SetInputActive(newState == ShotState.Ready);
        }

        private void SetInputActive(bool active)
        {
            isActive = active;
            inputState = InputState.Idle;
            currentPower = 0f;

            if (aimLine != null)
            {
                aimLine.enabled = active;
            }

            if (active)
            {
                currentAimAngle = 0f;
                UpdateAimLine();
            }
        }

        private void Update()
        {
            if (!isActive) return;

            switch (inputState)
            {
                case InputState.Idle:
                    HandleIdleInput();
                    break;
                case InputState.Aiming:
                    HandleAimingInput();
                    break;
                case InputState.Charging:
                    HandleChargingInput();
                    break;
            }

            UpdateAimLine();
        }

        private void HandleIdleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                pointerStartPosition = Input.mousePosition;
                inputState = InputState.Aiming;
            }
        }

        private void HandleAimingInput()
        {
            if (!Input.GetMouseButton(0))
            {
                // Released without charging — start charging on next press
                inputState = InputState.Charging;
                currentPower = 0f;
                return;
            }

            // Horizontal drag to aim
            float deltaX = Input.mousePosition.x - pointerStartPosition.x;
            float screenNormalized = deltaX / Screen.width;
            currentAimAngle = Mathf.Clamp(
                screenNormalized * maxAimAngle / aimSensitivity,
                -maxAimAngle,
                maxAimAngle);
        }

        private void HandleChargingInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Start charging
                currentPower = 0f;
            }

            if (Input.GetMouseButton(0))
            {
                // Charge power
                currentPower = Mathf.Clamp01(currentPower + chargeSpeed * Time.deltaTime);
            }

            if (Input.GetMouseButtonUp(0) && currentPower > 0f)
            {
                // Release — fire shot
                FireShot();
            }
        }

        private void FireShot()
        {
            var parameters = new ShotParameters
            {
                PowerNormalized = currentPower,
                AimAngleDegrees = currentAimAngle,
                BackspinRpm = defaultBackspinRpm,
                SidespinRpm = 0f
            };

            inputState = InputState.Idle;
            SetInputActive(false);

            OnShotReady?.Invoke(parameters);

            // Launch the ball and notify GameManager
            if (ballController != null)
            {
                ballController.Launch(parameters);
            }

            if (gameManager != null)
            {
                gameManager.LaunchShot();
            }
        }

        private void UpdateAimLine()
        {
            if (aimLine == null || !isActive) return;

            var ballPos = ballController != null ? ballController.transform.position : transform.position;
            var aimRotation = Quaternion.AngleAxis(currentAimAngle, Vector3.up);
            var aimDirection = aimRotation * Vector3.forward;

            aimLine.positionCount = 2;
            aimLine.SetPosition(0, ballPos);
            aimLine.SetPosition(1, ballPos + aimDirection * 2f);
        }
    }
}
