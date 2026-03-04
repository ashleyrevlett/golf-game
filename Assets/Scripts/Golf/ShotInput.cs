using System;
using UnityEngine;
using UnityEngine.InputSystem;
using GolfGame.Core;

namespace GolfGame.Golf
{
    /// <summary>
    /// Handles keyboard input for the 3-click power meter shot system.
    /// Click 1: start power oscillation. Click 2: lock power, start accuracy.
    /// Click 3: lock accuracy offset and fire.
    /// Only active when GameManager is in ShotState.Ready.
    /// </summary>
    public class ShotInput : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float maxAimAngle = 45f;
        [SerializeField] private float aimSensitivity = 0.5f;
        [SerializeField] private float defaultBackspinRpm = 3000f;

        [Header("Power Meter")]
        [SerializeField] private float meterSpeed = 1.2f;
        [SerializeField] private float accuracySpeed = 2.0f;
        [SerializeField] private float maxAccuracyDeviation = 8f;

        private GameManager gameManager;
        private BallController ballController;
        private LineRenderer aimLine;

        /// <summary>
        /// Shot input phases for the 3-click meter.
        /// </summary>
        public enum MeterPhase { Idle, Power, Accuracy }

        private MeterPhase currentPhase;
        private float currentAimAngle;
        private float meterValue;
        private float lockedPower;
        private float accuracyValue;
        private bool isActive;
        private bool meterRising;
        private bool accuracyRising;

        /// <summary>
        /// Fires when a shot is ready with parameters. Consumed by BallController.
        /// </summary>
        public event Action<ShotParameters> OnShotReady;

        /// <summary>
        /// Fires when the meter phase changes. UI subscribes to this.
        /// </summary>
        public event Action<MeterPhase> OnMeterPhaseChanged;

        /// <summary>
        /// Fires every frame with updated meter value (0-1). UI subscribes.
        /// </summary>
        public event Action<float> OnMeterValueChanged;

        /// <summary>
        /// Fires every frame with updated accuracy value (-1 to 1). UI subscribes.
        /// </summary>
        public event Action<float> OnAccuracyValueChanged;

        /// <summary>
        /// Fires when power is locked (click 2). Payload is locked power 0-1.
        /// </summary>
        public event Action<float> OnPowerLocked;

        /// <summary>Current aim angle in degrees.</summary>
        public float CurrentAimAngle => currentAimAngle;

        /// <summary>Current power meter value (0-1).</summary>
        public float CurrentPower => currentPhase == MeterPhase.Power ? meterValue : lockedPower;

        /// <summary>Whether input is currently active.</summary>
        public bool IsActive => isActive;

        /// <summary>Current meter phase.</summary>
        public MeterPhase CurrentPhase => currentPhase;

        private void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            ballController = FindFirstObjectByType<BallController>();
            aimLine = GetComponent<LineRenderer>();

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged += HandleShotStateChanged;
            }

            if (ballController == null)
                Debug.LogWarning($"[ShotInput] BallController NOT FOUND");

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
            currentPhase = MeterPhase.Idle;
            meterValue = 0f;
            lockedPower = 0f;
            accuracyValue = 0f;
            meterRising = true;
            accuracyRising = true;

            if (aimLine != null)
            {
                aimLine.enabled = active;
            }

            if (active)
            {
                currentAimAngle = 0f;
                UpdateAimLine();
            }

            OnMeterPhaseChanged?.Invoke(currentPhase);
            OnMeterValueChanged?.Invoke(0f);
            OnAccuracyValueChanged?.Invoke(0f);
        }

        private void Update()
        {
            if (!isActive) return;

            bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

            switch (currentPhase)
            {
                case MeterPhase.Idle:
                    HandleIdlePhase(spacePressed);
                    break;
                case MeterPhase.Power:
                    UpdatePowerMeter();
                    if (spacePressed) LockPower();
                    break;
                case MeterPhase.Accuracy:
                    UpdateAccuracyMeter();
                    if (spacePressed) LockAccuracyAndFire();
                    break;
            }

            UpdateAimLine();
        }

        private void HandleIdlePhase(bool spacePressed)
        {
            if (spacePressed)
            {
                currentPhase = MeterPhase.Power;
                meterValue = 0f;
                meterRising = true;
                OnMeterPhaseChanged?.Invoke(currentPhase);
            }
        }

        private void UpdatePowerMeter()
        {
            // Oscillate 0 -> 1 -> 0 -> 1 ...
            float delta = meterSpeed * Time.deltaTime;
            if (meterRising)
            {
                meterValue += delta;
                if (meterValue >= 1f)
                {
                    meterValue = 1f;
                    meterRising = false;
                }
            }
            else
            {
                meterValue -= delta;
                if (meterValue <= 0f)
                {
                    meterValue = 0f;
                    meterRising = true;
                }
            }

            OnMeterValueChanged?.Invoke(meterValue);
        }

        private void LockPower()
        {
            lockedPower = meterValue;
            currentPhase = MeterPhase.Accuracy;
            accuracyValue = 0f;
            accuracyRising = true;

            OnPowerLocked?.Invoke(lockedPower);
            OnMeterPhaseChanged?.Invoke(currentPhase);
        }

        private void UpdateAccuracyMeter()
        {
            // Oscillate -1 -> 1 -> -1 ...
            float delta = accuracySpeed * Time.deltaTime;
            if (accuracyRising)
            {
                accuracyValue += delta;
                if (accuracyValue >= 1f)
                {
                    accuracyValue = 1f;
                    accuracyRising = false;
                }
            }
            else
            {
                accuracyValue -= delta;
                if (accuracyValue <= -1f)
                {
                    accuracyValue = -1f;
                    accuracyRising = true;
                }
            }

            OnAccuracyValueChanged?.Invoke(accuracyValue);
        }

        private void LockAccuracyAndFire()
        {
            float aimDeviation = accuracyValue * maxAccuracyDeviation;

            var parameters = new ShotParameters
            {
                PowerNormalized = lockedPower,
                AimAngleDegrees = currentAimAngle + aimDeviation,
                BackspinRpm = defaultBackspinRpm,
                SidespinRpm = 0f
            };

            currentPhase = MeterPhase.Idle;
            SetInputActive(false);

            OnShotReady?.Invoke(parameters);

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
