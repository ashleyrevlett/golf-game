using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;
using GolfGame.Environment;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the game over screen. Shows final best distance
    /// and provides play again and menu buttons.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ScoringManager scoringManager;

        private VisualElement root;
        private Label finalDistance;
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
            root = uiDocument.rootVisualElement;

            finalDistance = root.Q<Label>("final-distance");
            playAgainButton = root.Q<Button>("play-again-button");
            menuButton = root.Q<Button>("menu-button");

            playAgainButton?.RegisterCallback<ClickEvent>(OnPlayAgainClicked);
            menuButton?.RegisterCallback<ClickEvent>(OnMenuClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged += HandleAppStateChanged;
                HandleAppStateChanged(AppManager.Instance.CurrentState);
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
        }

        private void HandleAppStateChanged(AppState state)
        {
            bool show = state == AppState.GameOver;
            SetVisible(show);

            if (show)
            {
                UpdateFinalScore();
            }
        }

        private void UpdateFinalScore()
        {
            if (finalDistance == null) return;

            if (scoringManager != null && scoringManager.BestDistance < float.MaxValue / 2f)
            {
                finalDistance.text = $"{scoringManager.BestDistance:F1}m";
            }
            else
            {
                finalDistance.text = "--";
            }
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
        }
    }
}
