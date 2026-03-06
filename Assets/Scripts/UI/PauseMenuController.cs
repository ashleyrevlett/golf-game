using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the pause menu overlay. Shows on AppState.Paused.
    /// Provides Resume, Settings, and Quit buttons.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement root;
        private VisualElement pauseRoot;
        private VisualElement pausePanel;
        private Button resumeButton;
        private Button settingsButton;
        private Button quitButton;
        private SettingsController settingsController;

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

            pauseRoot = root.Q("pause-root");
            pausePanel = root.Q("pause-panel");
            resumeButton = root.Q<Button>("resume-button");
            settingsButton = root.Q<Button>("settings-button");
            quitButton = root.Q<Button>("quit-button");

            resumeButton?.RegisterCallback<ClickEvent>(OnResumeClicked);
            settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClicked);
            quitButton?.RegisterCallback<ClickEvent>(OnQuitClicked);

            settingsController = FindFirstObjectByType<SettingsController>();

            // Hide SETTINGS button when no SettingsController in this scene
            if (settingsController == null && settingsButton != null)
            {
                settingsButton.style.display = DisplayStyle.None;
            }

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged += HandleAppStateChanged;
                HandleAppStateChanged(AppManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            resumeButton?.UnregisterCallback<ClickEvent>(OnResumeClicked);
            settingsButton?.UnregisterCallback<ClickEvent>(OnSettingsClicked);
            quitButton?.UnregisterCallback<ClickEvent>(OnQuitClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged -= HandleAppStateChanged;
            }
        }

        private void HandleAppStateChanged(AppState state)
        {
            if (state == AppState.Paused)
            {
                Show();
            }
            else
            {
                SetVisible(false);
            }
        }

        private void Show()
        {
            SetVisible(true);

            // Trigger fade-in on next frame so the transition plays
            if (pauseRoot != null)
            {
                pauseRoot.schedule.Execute(() =>
                {
                    if (pauseRoot != null)
                    {
                        pauseRoot.style.opacity = 1f;
                    }
                    if (pausePanel != null)
                    {
                        pausePanel.style.opacity = 1f;
                        pausePanel.style.scale = new Scale(Vector2.one);
                    }
                });
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
                // Reset for next show
                if (pauseRoot != null)
                {
                    pauseRoot.style.opacity = 0f;
                }
                if (pausePanel != null)
                {
                    pausePanel.style.opacity = 0f;
                    pausePanel.style.scale = new Scale(new Vector2(0.9f, 0.9f));
                }
            }
        }

        private void OnResumeClicked(ClickEvent evt)
        {
            AppManager.Instance?.ResumeGame();
        }

        private void OnSettingsClicked(ClickEvent evt)
        {
            if (settingsController == null) return;

            SetVisible(false);
            settingsController.ShowWithBackAction(() =>
            {
                settingsController.Hide();
                Show();
            });
        }

        private void OnQuitClicked(ClickEvent evt)
        {
            AppManager.Instance?.ReturnToTitle();
        }
    }
}
