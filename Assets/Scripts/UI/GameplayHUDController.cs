using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;
using GolfGame.Golf;
using GolfGame.Environment;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the gameplay HUD. Shows CTP running total score,
    /// shots remaining, and wind display in separated pods.
    /// </summary>
    public class GameplayHUDController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        private GameManager gameManager;
        private ScoringManager scoringManager;
        private WindSystem windSystem;

        private VisualElement root;
        private VisualElement pauseButtonPod;
        private Label scoreValue;
        private Label shotsValue;
        private Label windDisplay;

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

            pauseButtonPod = root.Q("pause-button-pod");
            scoreValue = root.Q<Label>("score-value");
            shotsValue = root.Q<Label>("shots-value");
            windDisplay = root.Q<Label>("wind-display");

            pauseButtonPod?.RegisterCallback<ClickEvent>(OnPauseClicked);

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
                scoringManager.OnShotRecorded += HandleShotRecorded;
            }

            if (windSystem != null)
            {
                windSystem.OnWindChanged += HandleWindChanged;
            }

            UpdateScoreDisplay(0f);
            UpdateShotsRemaining(GameManager.MaxShots);
        }

        private void OnDestroy()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged -= HandleAppStateChanged;
            }

            pauseButtonPod?.UnregisterCallback<ClickEvent>(OnPauseClicked);

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged -= HandleShotStateChanged;
            }

            if (scoringManager != null)
            {
                scoringManager.OnShotRecorded -= HandleShotRecorded;
            }

            if (windSystem != null)
            {
                windSystem.OnWindChanged -= HandleWindChanged;
            }
        }

        private void HandleAppStateChanged(AppState state)
        {
            SetVisible(state == AppState.Playing || state == AppState.Paused);
        }

        private void OnPauseClicked(ClickEvent evt)
        {
            AppManager.Instance?.PauseGame();
        }

        private void HandleShotStateChanged(ShotState state)
        {
            int shotsRemaining = gameManager != null
                ? GameManager.MaxShots - gameManager.CurrentShot
                : 0;
            UpdateShotsRemaining(shotsRemaining);
        }

        private void HandleShotRecorded(float distanceToPin)
        {
            if (scoringManager != null)
            {
                UpdateScoreDisplay(scoringManager.TotalCtpDistance);
            }

            // Punch animation on score update
            if (scoreValue != null)
            {
                scoreValue.AddToClassList("score-punch");
                StartCoroutine(RemoveClassAfterDelay(scoreValue, "score-punch", 0.15f));
            }

            // Also punch shots value
            if (shotsValue != null)
            {
                shotsValue.AddToClassList("score-punch");
                StartCoroutine(RemoveClassAfterDelay(shotsValue, "score-punch", 0.15f));
            }
        }

        private IEnumerator RemoveClassAfterDelay(VisualElement element, string className, float delay)
        {
            yield return new WaitForSeconds(delay);
            element.RemoveFromClassList(className);
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

        private void UpdateScoreDisplay(float totalCtp)
        {
            if (scoreValue != null)
            {
                scoreValue.text = totalCtp.ToString("F1");
            }
        }

        private void UpdateShotsRemaining(int remaining)
        {
            if (shotsValue != null)
            {
                shotsValue.text = remaining.ToString();
                if (remaining <= 0)
                {
                    shotsValue.AddToClassList("hud-value-warning");
                }
                else
                {
                    shotsValue.RemoveFromClassList("hud-value-warning");
                }
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
