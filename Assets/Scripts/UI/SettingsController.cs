using UnityEngine;
using UnityEngine.UIElements;

namespace GolfGame.UI
{
    /// <summary>
    /// Controls the settings screen. Manages volume and quality settings
    /// via PlayerPrefs. Self-managed visibility (not driven by AppState).
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        private const string VolumeKey = "SoundVolume";
        private const string QualityKey = "HighQuality";

        [SerializeField] private UIDocument uiDocument;
        private MainMenuController mainMenuController;

        private VisualElement root;
        private Slider volumeSlider;
        private Toggle qualityToggle;
        private Button backButton;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void Start()
        {
            mainMenuController = FindFirstObjectByType<MainMenuController>();

            root = uiDocument.rootVisualElement;

            volumeSlider = root.Q<Slider>("volume-slider");
            qualityToggle = root.Q<Toggle>("quality-toggle");
            backButton = root.Q<Button>("back-button");

            // Load saved settings
            if (volumeSlider != null)
            {
                volumeSlider.value = PlayerPrefs.GetFloat(VolumeKey, 1f);
                volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
            }

            if (qualityToggle != null)
            {
                qualityToggle.value = PlayerPrefs.GetInt(QualityKey, 1) == 1;
                qualityToggle.RegisterValueChangedCallback(OnQualityChanged);
            }

            backButton?.RegisterCallback<ClickEvent>(OnBackClicked);

            // Start hidden
            SetVisible(false);
        }

        private void OnDestroy()
        {
            volumeSlider?.UnregisterValueChangedCallback(OnVolumeChanged);
            qualityToggle?.UnregisterValueChangedCallback(OnQualityChanged);
            backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
        }

        /// <summary>
        /// Show the settings panel.
        /// </summary>
        public void Show()
        {
            SetVisible(true);
        }

        private void OnVolumeChanged(ChangeEvent<float> evt)
        {
            PlayerPrefs.SetFloat(VolumeKey, evt.newValue);
            AudioListener.volume = evt.newValue;
        }

        private void OnQualityChanged(ChangeEvent<bool> evt)
        {
            PlayerPrefs.SetInt(QualityKey, evt.newValue ? 1 : 0);
            QualitySettings.SetQualityLevel(evt.newValue ? 1 : 0);
        }

        private void OnBackClicked(ClickEvent evt)
        {
            SetVisible(false);
            if (mainMenuController != null)
            {
                mainMenuController.Show();
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
