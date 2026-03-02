using System;
using UnityEngine;

namespace GolfGame.Core
{
    /// <summary>
    /// Application-level state machine controlling screen flow.
    /// Persists across scene loads via DontDestroyOnLoad.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        [Header("Debug")]
#if UNITY_EDITOR
        [SerializeField] private bool skipToPlaying;
        [SerializeField] private AppState debugStartState = AppState.Title;
#endif

        private AppState currentState;

        /// <summary>
        /// Current application state.
        /// </summary>
        public AppState CurrentState => currentState;

        /// <summary>
        /// Fires when the application state changes. Payload is the new state.
        /// </summary>
        public event Action<AppState> OnAppStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (skipToPlaying)
            {
                Debug.Log("[AppManager] Debug: skipping to Playing state");
                SetState(AppState.TransitionToGame);
                return;
            }

            if (debugStartState != AppState.Title)
            {
                Debug.Log($"[AppManager] Debug: starting at {debugStartState}");
                SetState(debugStartState);
                return;
            }
#endif
            SetState(AppState.Title);
        }

        /// <summary>
        /// Transition to a new application state.
        /// Handles scene loading for state transitions that require it.
        /// </summary>
        public void SetState(AppState newState)
        {
            if (currentState == newState) return;

            var previousState = currentState;
            currentState = newState;

            Debug.Log($"[AppManager] {previousState} -> {newState}");
            OnAppStateChanged?.Invoke(newState);

            HandleStateTransition(newState);
        }

        private async void HandleStateTransition(AppState newState)
        {
            switch (newState)
            {
                case AppState.TransitionToGame:
                    await SceneLoader.LoadSceneAsync(SceneLoader.GameplayScene);
                    SetState(AppState.Playing);
                    break;

                case AppState.Title:
                case AppState.Leaderboard:
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != SceneLoader.MainMenuScene)
                    {
                        await SceneLoader.LoadSceneAsync(SceneLoader.MainMenuScene);
                    }
                    break;
            }
        }

        /// <summary>
        /// Convenience: start a new game from any state.
        /// </summary>
        public void StartGame()
        {
            SetState(AppState.Instructions);
        }

        /// <summary>
        /// Convenience: proceed from instructions to gameplay.
        /// </summary>
        public void ProceedToGameplay()
        {
            SetState(AppState.TransitionToGame);
        }

        /// <summary>
        /// Called by GameManager when all shots are complete.
        /// </summary>
        public void EndGame()
        {
            SetState(AppState.GameOver);
        }

        /// <summary>
        /// Return to the title screen.
        /// </summary>
        public void ReturnToTitle()
        {
            SetState(AppState.Title);
        }

        /// <summary>
        /// Show the leaderboard.
        /// </summary>
        public void ShowLeaderboard()
        {
            SetState(AppState.Leaderboard);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
