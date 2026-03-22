using System;
using System.Collections;
using System.Runtime.InteropServices;
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
        private VisualElement gameoverRoot;
        private VisualElement gameoverPanel;
        private Label finalScore;
        private Label bestScore;
        private VisualElement shotResultsContainer;
        private Button playAgainButton;
        private Button menuButton;
        private Button viewLeaderboardButton;
        private Button shareButton;
        private float lastTotalCtp;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ShareScore(string text, string gameObjectName);
#endif

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

            gameoverRoot = root.Q("gameover-root");
            gameoverPanel = root.Q("gameover-panel");
            finalScore = root.Q<Label>("final-score");
            bestScore = root.Q<Label>("best-score");
            shotResultsContainer = root.Q("shot-results");
            playAgainButton = root.Q<Button>("play-again-button");
            menuButton = root.Q<Button>("menu-button");
            viewLeaderboardButton = root.Q<Button>("view-leaderboard-button");
            shareButton = root.Q<Button>("share-button");

            playAgainButton?.RegisterCallback<ClickEvent>(OnPlayAgainClicked);
            menuButton?.RegisterCallback<ClickEvent>(OnMenuClicked);
            viewLeaderboardButton?.RegisterCallback<ClickEvent>(OnViewLeaderboardClicked);
            shareButton?.RegisterCallback<ClickEvent>(OnShareClicked);

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
            viewLeaderboardButton?.UnregisterCallback<ClickEvent>(OnViewLeaderboardClicked);
            shareButton?.UnregisterCallback<ClickEvent>(OnShareClicked);

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
            lastTotalCtp = totalCtp;
            // Delay showing game over to let player see ball landing + popup
            StartCoroutine(ShowGameOverAfterDelay(totalCtp, 1.5f));
        }

        private IEnumerator ShowGameOverAfterDelay(float totalCtp, float delay)
        {
            yield return new WaitForSeconds(delay);
            UpdateFinalScore(totalCtp);
            SetVisible(true);
            BuildShotRows();
            AnimateShotRows();

            // Trigger fade-in transitions
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
            try
            {
                if (finalScore != null)
                {
                    finalScore.text = FormatFinalScore(totalCtp);
                }

                if (bestScore == null) return;

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
                    bestScore.style.display = DisplayStyle.None;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameOverController] UpdateFinalScore failed: {ex}");
                if (bestScore != null)
                {
                    bestScore.style.display = DisplayStyle.None;
                }
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

        private void OnViewLeaderboardClicked(ClickEvent evt)
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.ShowLeaderboard();
            }
        }

        public static string FormatShareText(float totalCtp)
        {
            return $"I scored {totalCtp:F1} yds in Golf Game \u2014 beat me!";
        }

        private void OnShareClicked(ClickEvent evt)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            ShareScore(FormatShareText(lastTotalCtp), gameObject.name);
#else
            Debug.Log($"[GameOverController] Share (editor): {FormatShareText(lastTotalCtp)}");
            OnShareResult("copied");
#endif
        }

        public void OnShareResult(string result)
        {
            if (shareButton == null) return;

            if (result == "shared")
                shareButton.text = "SHARED!";
            else if (result == "copied")
                shareButton.text = "COPIED!";

            if (result == "shared" || result == "copied")
            {
                shareButton.style.backgroundColor = new Color(76f / 255f, 175f / 255f, 80f / 255f);
                shareButton.schedule.Execute(() =>
                {
                    shareButton.text = "SHARE";
                    shareButton.style.backgroundColor = StyleKeyword.Null;
                }).StartingIn(2000);
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
                // Clear dynamically added shot rows (keep header at index 0)
                ClearShotRows();

                // Reset overlay/panel for next show
                if (gameoverRoot != null)
                {
                    gameoverRoot.style.opacity = 0f;
                }
                if (gameoverPanel != null)
                {
                    gameoverPanel.style.opacity = 0f;
                    gameoverPanel.style.scale = new Scale(new Vector2(0.9f, 0.9f));
                }

                if (shareButton != null)
                {
                    shareButton.text = "SHARE";
                    shareButton.style.backgroundColor = StyleKeyword.Null;
                }
            }
        }

        public static bool HasBestScore(float bestScore) { return bestScore < float.MaxValue; }

        public static bool IsNewBestScore(float newScore, float previousBest) { return newScore < previousBest; }

        /// <summary>
        /// Returns the accuracy grade for a shot based on distance to pin.
        /// A = under 5 yds, B = 5-15 yds, C = 15+ yds.
        /// </summary>
        internal static string GetShotGrade(float distanceToPin)
        {
            if (distanceToPin < 5f) return "A";
            if (distanceToPin < 15f) return "B";
            return "C";
        }

        /// <summary>
        /// Returns the USS class name for the given grade letter.
        /// </summary>
        internal static string GetGradeClass(string grade)
        {
            switch (grade)
            {
                case "A": return "shot-grade-a";
                case "B": return "shot-grade-b";
                case "C": return "shot-grade-c";
                default: return "shot-grade-c";
            }
        }

        private void BuildShotRows()
        {
            if (shotResultsContainer == null) return;

            if (scoringManager == null || scoringManager.Results.Count == 0)
            {
                shotResultsContainer.style.display = DisplayStyle.None;
                return;
            }

            shotResultsContainer.style.display = DisplayStyle.Flex;

            // Clear previous data rows (keep header at index 0)
            ClearShotRows();

            // Find best shot index (lowest DistanceToPin)
            int bestIndex = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < scoringManager.Results.Count; i++)
            {
                if (scoringManager.Results[i].DistanceToPin < bestDistance)
                {
                    bestDistance = scoringManager.Results[i].DistanceToPin;
                    bestIndex = i;
                }
            }

            // Build rows
            for (int i = 0; i < scoringManager.Results.Count; i++)
            {
                var result = scoringManager.Results[i];
                var row = new VisualElement();
                row.AddToClassList("shot-row");

                if (i == scoringManager.Results.Count - 1)
                    row.AddToClassList("shot-row-last");

                if (i == bestIndex)
                    row.AddToClassList("shot-row-best");

                var numberLabel = new Label($"{result.ShotNumber}");
                numberLabel.AddToClassList("shot-number");
                row.Add(numberLabel);

                var distanceLabel = new Label($"{result.DistanceToPin:F1} yds");
                distanceLabel.AddToClassList("shot-distance");
                row.Add(distanceLabel);

                string grade = GetShotGrade(result.DistanceToPin);
                var gradeLabel = new Label(grade);
                gradeLabel.AddToClassList("shot-grade");
                gradeLabel.AddToClassList(GetGradeClass(grade));
                row.Add(gradeLabel);

                // Set initial state for stagger animation
                row.style.opacity = 0f;
                row.style.translate = new Translate(0, 8);
                gradeLabel.style.scale = new Scale(new Vector2(0.8f, 0.8f));

                shotResultsContainer.Add(row);
            }
        }

        private void AnimateShotRows()
        {
            if (shotResultsContainer == null) return;

            // Iterate data rows (children after index 0, which is the header)
            for (int i = 1; i < shotResultsContainer.childCount; i++)
            {
                var row = shotResultsContainer[i];
                int delayMs = (i - 1) * 60;
                row.schedule.Execute(() =>
                {
                    row.style.opacity = 1f;
                    row.style.translate = new Translate(0, 0);
                    var grade = row.Q(className: "shot-grade");
                    if (grade != null)
                        grade.style.scale = new Scale(Vector2.one);
                }).StartingIn(delayMs);
            }
        }

        private void ClearShotRows()
        {
            if (shotResultsContainer == null) return;

            // Remove all children after the header (index 0)
            while (shotResultsContainer.childCount > 1)
            {
                shotResultsContainer.RemoveAt(shotResultsContainer.childCount - 1);
            }
        }
    }
}
