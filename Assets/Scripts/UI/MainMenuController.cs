using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the main menu screen. Shows on Title state.
    /// Play button starts the game, Settings button shows settings panel.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private SettingsController settingsController;

        private VisualElement root;
        private Button playButton;
        private Button settingsButton;

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

            playButton = root.Q<Button>("play-button");
            settingsButton = root.Q<Button>("settings-button");

            playButton?.RegisterCallback<ClickEvent>(OnPlayClicked);
            settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged += HandleAppStateChanged;
                HandleAppStateChanged(AppManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);
            settingsButton?.UnregisterCallback<ClickEvent>(OnSettingsClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged -= HandleAppStateChanged;
            }
        }

        private void HandleAppStateChanged(AppState state)
        {
            SetVisible(state == AppState.Title);
        }

        private void OnPlayClicked(ClickEvent evt)
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.StartGame();
            }
        }

        private void OnSettingsClicked(ClickEvent evt)
        {
            SetVisible(false);
            if (settingsController != null)
            {
                settingsController.Show();
            }
        }

        /// <summary>
        /// Show the main menu panel.
        /// </summary>
        public void Show()
        {
            SetVisible(true);
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
