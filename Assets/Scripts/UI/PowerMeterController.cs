using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;
using GolfGame.Golf;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the 3-click power meter UI. Subscribes to ShotInput events
    /// and updates the visual bar, marker, and labels.
    /// </summary>
    public class PowerMeterController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private ShotInput shotInput;
        private VisualElement root;
        private VisualElement meterContainer;
        private VisualElement meterFill;
        private VisualElement meterMarker;
        private VisualElement accuracyZone;
        private VisualElement accuracyMarker;
        private Label meterLabel;
        private Label powerReadout;
        private Label accuracyFeedback;
        private IVisualElementScheduledItem feedbackHideSchedule;
        private float lastAccuracyValue;
        private bool wasInAccuracyPhase;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            shotInput = FindFirstObjectByType<ShotInput>();

            root = uiDocument.rootVisualElement;
            meterContainer = root.Q("power-meter-container");
            meterFill = root.Q("power-meter-fill");
            meterMarker = root.Q("power-meter-marker");
            accuracyZone = root.Q("accuracy-zone");
            accuracyMarker = root.Q("accuracy-marker");
            meterLabel = root.Q<Label>("power-meter-label");
            powerReadout = root.Q<Label>("power-readout");
            accuracyFeedback = root.Q<Label>("accuracy-feedback");

            if (shotInput != null)
            {
                shotInput.OnMeterPhaseChanged += HandlePhaseChanged;
                shotInput.OnMeterValueChanged += HandleMeterValueChanged;
                shotInput.OnAccuracyValueChanged += HandleAccuracyValueChanged;
                shotInput.OnPowerLocked += HandlePowerLocked;
            }

            SetMeterVisible(false);
        }

        private void OnDestroy()
        {
            if (shotInput != null)
            {
                shotInput.OnMeterPhaseChanged -= HandlePhaseChanged;
                shotInput.OnMeterValueChanged -= HandleMeterValueChanged;
                shotInput.OnAccuracyValueChanged -= HandleAccuracyValueChanged;
                shotInput.OnPowerLocked -= HandlePowerLocked;
            }
        }

        private void HandlePhaseChanged(ShotInput.MeterPhase phase)
        {
            switch (phase)
            {
                case ShotInput.MeterPhase.Idle:
                    if (wasInAccuracyPhase)
                    {
                        ShowAccuracyFeedback(lastAccuracyValue);
                        wasInAccuracyPhase = false;
                    }
                    SetMeterVisible(false);
                    break;
                case ShotInput.MeterPhase.Power:
                    SetMeterVisible(true);
                    ShowPowerMode();
                    break;
                case ShotInput.MeterPhase.Accuracy:
                    wasInAccuracyPhase = true;
                    ShowAccuracyMode();
                    break;
            }
        }

        private void HandleMeterValueChanged(float value)
        {
            if (meterFill != null)
            {
                meterFill.style.width = Length.Percent(value * 100f);
            }

            if (meterMarker != null)
            {
                meterMarker.style.left = Length.Percent(value * 100f);
            }

            if (powerReadout != null)
            {
                powerReadout.text = $"{Mathf.RoundToInt(value * 100)}%";
            }

            // Color transition: green -> yellow -> red
            if (meterFill != null)
            {
                meterFill.style.backgroundColor = GetMeterColor(value);
            }
        }

        private void HandleAccuracyValueChanged(float value)
        {
            lastAccuracyValue = value;

            if (accuracyMarker != null)
            {
                // Map -1..1 to 0%..100%
                float pct = (value + 1f) * 0.5f * 100f;
                accuracyMarker.style.left = Length.Percent(pct);
            }
        }

        private void HandlePowerLocked(float power)
        {
            if (powerReadout != null)
            {
                powerReadout.text = $"{Mathf.RoundToInt(power * 100)}%";
            }
        }

        private void ShowPowerMode()
        {
            if (meterLabel != null)
            {
                meterLabel.text = "POWER";
            }

            if (accuracyZone != null)
            {
                accuracyZone.style.display = DisplayStyle.None;
            }

            if (accuracyMarker != null)
            {
                accuracyMarker.style.display = DisplayStyle.None;
            }

            if (meterFill != null)
            {
                meterFill.style.display = DisplayStyle.Flex;
                meterFill.style.width = Length.Percent(0f);
            }

            if (meterMarker != null)
            {
                meterMarker.style.display = DisplayStyle.Flex;
            }
        }

        private void ShowAccuracyMode()
        {
            if (meterLabel != null)
            {
                meterLabel.text = "ACCURACY";
            }

            // Hide power fill, show accuracy zone + marker
            if (meterFill != null)
            {
                meterFill.style.display = DisplayStyle.None;
            }

            if (meterMarker != null)
            {
                meterMarker.style.display = DisplayStyle.None;
            }

            if (accuracyZone != null)
            {
                accuracyZone.style.display = DisplayStyle.Flex;
            }

            if (accuracyMarker != null)
            {
                accuracyMarker.style.display = DisplayStyle.Flex;
                accuracyMarker.style.left = Length.Percent(50f);
            }
        }

        private void ShowAccuracyFeedback(float accuracyValue)
        {
            if (accuracyFeedback == null) return;

            var (label, color) = GetAccuracyRating(accuracyValue);

            accuracyFeedback.text = label;
            accuracyFeedback.style.color = color;
            accuracyFeedback.style.display = DisplayStyle.Flex;
            accuracyFeedback.style.opacity = 1f;

            // Cancel any pending hide
            feedbackHideSchedule?.Pause();

            // Fade out after 1.5 seconds
            feedbackHideSchedule = accuracyFeedback.schedule.Execute(() =>
            {
                accuracyFeedback.style.opacity = 0f;
                // Hide after fade transition completes
                accuracyFeedback.schedule.Execute(() =>
                {
                    accuracyFeedback.style.display = DisplayStyle.None;
                }).StartingIn(300);
            }).StartingIn(1500);
        }

        /// <summary>
        /// Rate the accuracy lock quality. Value is -1 to 1 (0 = perfect center).
        /// </summary>
        public static (string label, Color color) GetAccuracyRating(float accuracyValue)
        {
            float absValue = Mathf.Abs(accuracyValue);

            if (absValue <= 0.15f)
                return ("GREAT!", new Color(0.3f, 0.8f, 0.3f));

            if (absValue <= 0.45f)
                return ("OK", new Color(1f, 0.84f, 0f));

            return ("MISS", new Color(0.85f, 0.26f, 0.21f));
        }

        /// <summary>
        /// Get the meter bar color for a given power value (0-1).
        /// Interpolates green (0) -> yellow (0.5) -> red (1).
        /// </summary>
        public static Color GetMeterColor(float value)
        {
            if (value < 0.5f)
            {
                return Color.Lerp(
                    new Color(0.3f, 0.8f, 0.3f),
                    new Color(1f, 0.84f, 0f),
                    value * 2f);
            }

            return Color.Lerp(
                new Color(1f, 0.84f, 0f),
                new Color(0.85f, 0.26f, 0.21f),
                (value - 0.5f) * 2f);
        }

        private void SetMeterVisible(bool visible)
        {
            if (meterContainer != null)
            {
                meterContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
