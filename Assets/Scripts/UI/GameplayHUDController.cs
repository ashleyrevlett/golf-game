using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;
using GolfGame.Golf;
using GolfGame.Environment;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the gameplay HUD. Shows shot counter, best distance,
    /// wind display, ready message, and per-shot statistics.
    /// </summary>
    public class GameplayHUDController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        private GameManager gameManager;
        private ScoringManager scoringManager;
        private WindSystem windSystem;

        private VisualElement root;
        private Label shotCounter;
        private Label bestDistance;
        private Label windDisplay;
        private Label readyMessage;
        private VisualElement statsPanel;
        private Label statDistance;
        private Label statCarry;
        private Label statSpeed;
        private Label statCurve;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            scoringManager = FindFirstObjectByType<ScoringManager>();
            windSystem = FindFirstObjectByType<WindSystem>();

            root = uiDocument.rootVisualElement;

            shotCounter = root.Q<Label>("shot-counter");
            bestDistance = root.Q<Label>("best-distance");
            windDisplay = root.Q<Label>("wind-display");
            readyMessage = root.Q<Label>("ready-message");
            statsPanel = root.Q("stats-panel");
            statDistance = root.Q<Label>("stat-distance");
            statCarry = root.Q<Label>("stat-carry");
            statSpeed = root.Q<Label>("stat-speed");
            statCurve = root.Q<Label>("stat-curve");

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged += HandleAppStateChanged;
                HandleAppStateChanged(AppManager.Instance.CurrentState);
            }

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged += HandleShotStateChanged;
            }

            if (scoringManager != null)
            {
                scoringManager.OnShotScored += HandleShotScored;
                scoringManager.OnBestDistanceUpdated += HandleBestDistanceUpdated;
            }

            if (windSystem != null)
            {
                windSystem.OnWindChanged += HandleWindChanged;
            }

            UpdateShotCounter(0);
            UpdateBestDistance(float.MaxValue);
        }

        private void OnDestroy()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged -= HandleAppStateChanged;
            }

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged -= HandleShotStateChanged;
            }

            if (scoringManager != null)
            {
                scoringManager.OnShotScored -= HandleShotScored;
                scoringManager.OnBestDistanceUpdated -= HandleBestDistanceUpdated;
            }

            if (windSystem != null)
            {
                windSystem.OnWindChanged -= HandleWindChanged;
            }
        }

        private void HandleAppStateChanged(AppState state)
        {
            SetVisible(state == AppState.Playing);
        }

        private void HandleShotStateChanged(ShotState state)
        {
            int shotNum = gameManager != null ? gameManager.CurrentShot : 0;
            UpdateShotCounter(shotNum);

            bool isReady = state == ShotState.Ready;
            if (readyMessage != null)
            {
                readyMessage.style.display = isReady ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Hide stats panel when starting a new shot
            if (state == ShotState.Flying && statsPanel != null)
            {
                statsPanel.style.display = DisplayStyle.None;
            }
        }

        private void HandleShotScored(ShotResult result)
        {
            if (statsPanel != null)
            {
                statsPanel.style.display = DisplayStyle.Flex;
            }

            if (statDistance != null)
            {
                statDistance.text = $"Distance: {result.DistanceToPin:F1}m from pin";
            }

            if (statCarry != null)
            {
                statCarry.text = $"Carry: {result.CarryDistance:F1}m";
            }

            if (statSpeed != null)
            {
                statSpeed.text = $"Speed: {result.BallSpeed:F1} m/s";
            }

            if (statCurve != null)
            {
                statCurve.text = $"Curve: {result.LateralDeviation:F1}m";
            }
        }

        private void HandleBestDistanceUpdated(float distance)
        {
            UpdateBestDistance(distance);
        }

        private void HandleWindChanged(Vector3 wind)
        {
            if (windDisplay != null)
            {
                float speed = wind.magnitude;
                float dir = Mathf.Atan2(wind.x, wind.z) * Mathf.Rad2Deg;
                windDisplay.text = $"Wind: {speed:F1} m/s {GetCardinalDirection(dir)}";
            }
        }

        private void UpdateShotCounter(int shotNum)
        {
            if (shotCounter != null)
            {
                shotCounter.text = $"Shot {shotNum}/{GameManager.MaxShots}";
            }
        }

        private void UpdateBestDistance(float distance)
        {
            if (bestDistance != null)
            {
                bestDistance.text = distance >= float.MaxValue / 2f
                    ? "Best: --"
                    : $"Best: {distance:F1}m";
            }
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static string GetCardinalDirection(float degrees)
        {
            // Normalize to 0-360
            degrees = ((degrees % 360f) + 360f) % 360f;

            if (degrees < 22.5f || degrees >= 337.5f) return "N";
            if (degrees < 67.5f) return "NE";
            if (degrees < 112.5f) return "E";
            if (degrees < 157.5f) return "SE";
            if (degrees < 202.5f) return "S";
            if (degrees < 247.5f) return "SW";
            if (degrees < 292.5f) return "W";
            return "NW";
        }
    }
}
