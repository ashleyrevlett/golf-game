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
        private MeterOscillator powerMeter = new MeterOscillator(0f, 1f);
        private MeterOscillator accuracyMeter = new MeterOscillator(-1f, 1f);
        private float lockedPower;
        private bool isActive;

        [SerializeField] private float tapThresholdPx = 10f;
        private Vector2 touchStartPosition;

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

        /// <summary>
        /// Fires when aim angle changes. Payload is angle in degrees (-45 to 45).
        /// </summary>
        public event Action<float> OnAimAngleChanged;

        /// <summary>Current aim angle in degrees.</summary>
        public float CurrentAimAngle => currentAimAngle;

        /// <summary>Current power meter value (0-1).</summary>
        public float CurrentPower => currentPhase == MeterPhase.Power ? powerMeter.Value : lockedPower;

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
            powerMeter.Reset();
            accuracyMeter.Reset();
            lockedPower = 0f;

            if (aimLine != null)
            {
                aimLine.enabled = active;
            }

            if (active)
            {
                UpdateAimLine();
            }

            OnMeterPhaseChanged?.Invoke(currentPhase);
            OnMeterValueChanged?.Invoke(0f);
            OnAccuracyValueChanged?.Invoke(0f);
        }

        private bool WasActionPressed()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                float displacement = Vector2.Distance(
                    Touchscreen.current.primaryTouch.position.ReadValue(),
                    touchStartPosition);
                if (displacement < tapThresholdPx)
                    return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            return false;
        }

        private void UpdateAimInput()
        {
            float aimDelta = 0f;
            float aimSpeed = maxAimAngle * aimSensitivity;

            // Keyboard: arrow keys
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.isPressed)
                    aimDelta -= aimSpeed * Time.deltaTime;
                if (Keyboard.current.rightArrowKey.isPressed)
                    aimDelta += aimSpeed * Time.deltaTime;
            }

            // Touch: horizontal drag
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                float touchDelta = Touchscreen.current.primaryTouch.delta.x.ReadValue();
                aimDelta += touchDelta * aimSensitivity * 0.1f;
            }

            // Mouse: horizontal drag while right button held
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                float mouseDelta = Mouse.current.delta.x.ReadValue();
                aimDelta += mouseDelta * aimSensitivity * 0.1f;
            }

            if (aimDelta != 0f)
            {
                currentAimAngle = Mathf.Clamp(currentAimAngle + aimDelta, -maxAimAngle, maxAimAngle);
                OnAimAngleChanged?.Invoke(currentAimAngle);
            }
        }

        private void UpdateTouchTracking()
        {
            if (Touchscreen.current == null) return;
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                touchStartPosition = touch.position.ReadValue();
            }
        }

        private void Update()
        {
            if (!isActive) return;

            UpdateTouchTracking();

            // Aim adjustment only during Idle phase (before meter starts)
            if (currentPhase == MeterPhase.Idle)
            {
                UpdateAimInput();
            }

            bool actionPressed = WasActionPressed();

            switch (currentPhase)
            {
                case MeterPhase.Idle:
                    HandleIdlePhase(actionPressed);
                    break;
                case MeterPhase.Power:
                    UpdatePowerMeter();
                    if (actionPressed) LockPower();
                    break;
                case MeterPhase.Accuracy:
                    UpdateAccuracyMeter();
                    if (actionPressed) LockAccuracyAndFire();
                    break;
            }

            UpdateAimLine();
        }

        private void HandleIdlePhase(bool spacePressed)
        {
            if (spacePressed)
            {
                currentPhase = MeterPhase.Power;
                powerMeter.Reset();
                OnMeterPhaseChanged?.Invoke(currentPhase);
            }
        }

        private void UpdatePowerMeter()
        {
            powerMeter.Tick(meterSpeed, Time.deltaTime);
            OnMeterValueChanged?.Invoke(powerMeter.Value);
        }

        private void LockPower()
        {
            lockedPower = powerMeter.Value;
            currentPhase = MeterPhase.Accuracy;
            accuracyMeter.Reset();

            OnPowerLocked?.Invoke(lockedPower);
            OnMeterPhaseChanged?.Invoke(currentPhase);
        }

        private void UpdateAccuracyMeter()
        {
            accuracyMeter.Tick(accuracySpeed, Time.deltaTime);
            OnAccuracyValueChanged?.Invoke(accuracyMeter.Value);
        }

        private void LockAccuracyAndFire()
        {
            float aimDeviation = accuracyMeter.Value * maxAccuracyDeviation;

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
