using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the instructions screen. Shows on AppState.Instructions.
    /// Displays shot mechanic steps and game objective, with a PLAY button
    /// to proceed to gameplay.
    /// </summary>
    public class InstructionsController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement root;
        private VisualElement instructionsRoot;
        private VisualElement instructionsPanel;
        private Button playButton;

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

            instructionsRoot = root.Q("instructions-root");
            instructionsPanel = root.Q("instructions-panel");
            playButton = root.Q<Button>("play-button");

            playButton?.RegisterCallback<ClickEvent>(OnPlayClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged += HandleAppStateChanged;
                HandleAppStateChanged(AppManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged -= HandleAppStateChanged;
            }
        }

        private void HandleAppStateChanged(AppState state)
        {
            if (state == AppState.Instructions)
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
            if (instructionsRoot != null)
            {
                instructionsRoot.schedule.Execute(() =>
                {
                    if (instructionsRoot != null)
                    {
                        instructionsRoot.style.opacity = 1f;
                    }
                    if (instructionsPanel != null)
                    {
                        instructionsPanel.style.opacity = 1f;
                        instructionsPanel.style.scale = new Scale(Vector2.one);
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
                if (instructionsRoot != null)
                {
                    instructionsRoot.style.opacity = 0f;
                }
                if (instructionsPanel != null)
                {
                    instructionsPanel.style.opacity = 0f;
                    instructionsPanel.style.scale = new Scale(new Vector2(0.9f, 0.9f));
                }
            }
        }

        private void OnPlayClicked(ClickEvent evt)
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.ProceedToGameplay();
            }
        }
    }
}
