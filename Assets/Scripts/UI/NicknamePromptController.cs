using System;
using UnityEngine;
using UnityEngine.UIElements;
using GolfGame.Core;
using GolfGame.Multiplayer;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the nickname prompt overlay shown on first launch.
    /// Collects a display name, persists it in PlayerPrefs, and
    /// calls UpdateDisplayNameAsync on the auth service.
    /// </summary>
    public class NicknamePromptController : MonoBehaviour
    {
        private const string NicknameKey = "nickname";
        private const string PromptedKey = "nickname_prompted";
        private const int MaxLength = 20;

        [SerializeField] private UIDocument uiDocument;

        private VisualElement root;
        private VisualElement overlay;
        private TextField nicknameField;
        private Label charCounter;
        private Button saveButton;
        private Button skipButton;

        /// <summary>
        /// Fired when the prompt is dismissed (save or skip).
        /// </summary>
        public event Action OnPromptDismissed;

        /// <summary>
        /// Whether a nickname prompt is needed (first launch, not yet prompted).
        /// </summary>
        public static bool NeedsPrompt =>
            PlayerPrefs.GetInt(PromptedKey, 0) == 0;

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
            overlay = root.Q("nickname-overlay");
            nicknameField = root.Q<TextField>("nickname-field");
            charCounter = root.Q<Label>("char-counter");
            saveButton = root.Q<Button>("save-button");
            skipButton = root.Q<Button>("skip-button");

            nicknameField?.RegisterValueChangedCallback(OnNicknameChanged);
            saveButton?.RegisterCallback<ClickEvent>(OnSaveClicked);
            skipButton?.RegisterCallback<ClickEvent>(OnSkipClicked);

            // Start hidden
            SetVisible(false);
        }

        private void OnDestroy()
        {
            nicknameField?.UnregisterValueChangedCallback(OnNicknameChanged);
            saveButton?.UnregisterCallback<ClickEvent>(OnSaveClicked);
            skipButton?.UnregisterCallback<ClickEvent>(OnSkipClicked);
        }

        /// <summary>
        /// Show the nickname prompt with a fade-in.
        /// </summary>
        public void Show()
        {
            SetVisible(true);

            // Reset field
            if (nicknameField != null)
            {
                nicknameField.value = "";
            }
            UpdateCharCounter(0);

            // Trigger fade-in
            if (overlay != null)
            {
                overlay.schedule.Execute(() =>
                {
                    overlay.style.opacity = 1f;
                }).StartingIn(16);
            }
        }

        private void OnNicknameChanged(ChangeEvent<string> evt)
        {
            UpdateCharCounter(evt.newValue?.Length ?? 0);
        }

        private void UpdateCharCounter(int length)
        {
            if (charCounter == null) return;
            charCounter.text = $"{length} / {MaxLength}";

            if (length >= MaxLength)
            {
                charCounter.style.color = new StyleColor(new Color(244f / 255f, 67f / 255f, 54f / 255f));
            }
            else
            {
                charCounter.style.color = StyleKeyword.Null;
            }
        }

        private void OnSaveClicked(ClickEvent evt)
        {
            var nickname = nicknameField?.value?.Trim() ?? "";
            if (string.IsNullOrEmpty(nickname))
            {
                // Empty save treated as skip
                Dismiss("");
                return;
            }
            Dismiss(nickname);
        }

        private void OnSkipClicked(ClickEvent evt)
        {
            Dismiss("");
        }

        private void Dismiss(string nickname)
        {
            // Persist
            PlayerPrefs.SetInt(PromptedKey, 1);
            if (!string.IsNullOrEmpty(nickname))
            {
                PlayerPrefs.SetString(NicknameKey, nickname);
                _ = UpdateDisplayNameAsync(nickname);
            }
            PlayerPrefs.Save();

            // Fade out then hide
            if (overlay != null)
            {
                overlay.style.opacity = 0f;
                overlay.schedule.Execute(() =>
                {
                    SetVisible(false);
                    OnPromptDismissed?.Invoke();
                }).StartingIn(300);
            }
            else
            {
                SetVisible(false);
                OnPromptDismissed?.Invoke();
            }
        }

        private async System.Threading.Tasks.Task UpdateDisplayNameAsync(string nickname)
        {
            try
            {
                var authService = ServiceLocator.Get<IAuthService>();
                if (authService != null)
                {
                    await authService.UpdateDisplayNameAsync(nickname);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NicknamePrompt] Failed to update display name: {ex.Message}");
            }
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!visible && overlay != null)
            {
                overlay.style.opacity = 0f;
            }
        }
    }
}
