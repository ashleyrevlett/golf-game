using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;
using GolfGame.Environment;
using GolfGame.Multiplayer;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the game over screen. Shows total CTP score,
    /// best score comparison, and provides play again and menu buttons.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        private ScoringManager scoringManager;

        private VisualElement root;
        private VisualElement gameoverPanel;
        private Label finalScore;
        private Label bestScore;
        private Button playAgainButton;
        private Button menuButton;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            scoringManager = FindFirstObjectByType<ScoringManager>();

            root = uiDocument.rootVisualElement;

            var gameoverRoot = root.Q("gameover-root");
            gameoverPanel = root.Q("gameover-panel");
            finalScore = root.Q<Label>("final-score");
            bestScore = root.Q<Label>("best-score");
            playAgainButton = root.Q<Button>("play-again-button");
            menuButton = root.Q<Button>("menu-button");

            playAgainButton?.RegisterCallback<ClickEvent>(OnPlayAgainClicked);
            menuButton?.RegisterCallback<ClickEvent>(OnMenuClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged += HandleAppStateChanged;
                HandleAppStateChanged(AppManager.Instance.CurrentState);
            }

            if (scoringManager != null)
            {
                scoringManager.OnAllShotsComplete += HandleAllShotsComplete;
            }
        }

        private void OnDestroy()
        {
            playAgainButton?.UnregisterCallback<ClickEvent>(OnPlayAgainClicked);
            menuButton?.UnregisterCallback<ClickEvent>(OnMenuClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged -= HandleAppStateChanged;
            }

            if (scoringManager != null)
            {
                scoringManager.OnAllShotsComplete -= HandleAllShotsComplete;
            }
        }

        private void HandleAppStateChanged(AppState state)
        {
            if (state != AppState.GameOver)
            {
                SetVisible(false);
            }
        }

        private void HandleAllShotsComplete(float totalCtp)
        {
            // Delay showing game over to let player see ball landing + popup
            StartCoroutine(ShowGameOverAfterDelay(totalCtp, 1.5f));
        }

        private IEnumerator ShowGameOverAfterDelay(float totalCtp, float delay)
        {
            yield return new WaitForSeconds(delay);
            UpdateFinalScore(totalCtp);
            SetVisible(true);

            // Trigger fade-in transitions
            var gameoverRoot = root.Q("gameover-root");
            if (gameoverRoot != null)
            {
                gameoverRoot.style.opacity = 1f;
            }
            if (gameoverPanel != null)
            {
                gameoverPanel.style.opacity = 1f;
                gameoverPanel.style.scale = new Scale(Vector2.one);
            }
        }

        private async void UpdateFinalScore(float totalCtp)
        {
            if (finalScore != null)
            {
                finalScore.text = FormatFinalScore(totalCtp);
            }

            if (bestScore == null) return;

            try
            {
                var bestScoreService = ServiceLocator.Get<IBestScoreService>();
                if (bestScoreService == null)
                {
                    bestScore.style.display = DisplayStyle.None;
                    return;
                }

                float previousBest = await bestScoreService.GetBestScoreAsync();
                bool isNewBest = totalCtp < previousBest;

                if (isNewBest)
                {
                    await bestScoreService.SaveBestScoreAsync(totalCtp);
                    bestScore.text = FormatBestScoreLabel(totalCtp, true);
                    bestScore.style.display = DisplayStyle.Flex;
                }
                else if (previousBest < float.MaxValue / 2f)
                {
                    bestScore.text = FormatBestScoreLabel(previousBest, false);
                    bestScore.style.display = DisplayStyle.Flex;
                }
                else
                {
                    // No best exists yet
                    bestScore.style.display = DisplayStyle.None;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameOverController] Failed to load best score: {ex.Message}");
                bestScore.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Format total CTP distance for the game over screen.
        /// </summary>
        public static string FormatFinalScore(float totalCtp)
        {
            return $"{totalCtp:F1} yds";
        }

        /// <summary>
        /// Format the best score label, with optional "NEW!" prefix.
        /// </summary>
        public static string FormatBestScoreLabel(float bestCtp, bool isNewBest)
        {
            if (isNewBest)
            {
                return $"NEW! BEST: {bestCtp:F1} yds";
            }
            return $"BEST: {bestCtp:F1} yds";
        }

        /// <summary>
        /// Determine if a score is a new best compared to a previous best.
        /// </summary>
        public static bool IsNewBestScore(float current, float previousBest)
        {
            return current < previousBest;
        }

        /// <summary>
        /// Determine if a historical best score exists.
        /// </summary>
        public static bool HasBestScore(float bestValue)
        {
            return bestValue < float.MaxValue / 2f;
        }

        private void OnPlayAgainClicked(ClickEvent evt)
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.StartGame();
            }
        }

        private void OnMenuClicked(ClickEvent evt)
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.ReturnToTitle();
            }
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!visible)
            {
                // Reset overlay/panel for next show
                var gameoverRoot = root?.Q("gameover-root");
                if (gameoverRoot != null)
                {
                    gameoverRoot.style.opacity = 0f;
                }
                if (gameoverPanel != null)
                {
                    gameoverPanel.style.opacity = 0f;
                    gameoverPanel.style.scale = new Scale(new Vector2(0.9f, 0.9f));
                }
            }
        }
    }
}
