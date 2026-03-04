using System;
using UnityEngine;

namespace GolfGame.Core
{
    /// <summary>
    /// Gameplay-level state machine controlling the shot loop.
    /// Lives in the Gameplay scene. Only active when AppManager is in Playing state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public const int MaxShots = 6;

        [Header("Debug")]
#if UNITY_EDITOR
        [SerializeField] private bool autoActivate;
#endif

        private ShotState currentShotState;
        private int currentShot;
        private bool isActive;

        /// <summary>
        /// Current shot state in the gameplay loop.
        /// </summary>
        public ShotState CurrentShotState => currentShotState;

        /// <summary>
        /// Current shot number (1-based). 0 means no shots taken yet.
        /// </summary>
        public int CurrentShot => currentShot;

        /// <summary>
        /// Whether the GameManager is actively processing the shot loop.
        /// </summary>
        public bool IsActive => isActive;

        /// <summary>
        /// Fires when the shot state changes. Payload is the new state.
        /// </summary>
        public event Action<ShotState> OnShotStateChanged;

        /// <summary>
        /// Fires when the game ends. Payload: (shotCount, isNewBest).
        /// isNewBest is always false for now — scoring is in M4.
        /// </summary>
        public event Action<int, bool> OnGameOver;

        private void Start()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged += HandleAppStateChanged;

                if (AppManager.Instance.CurrentState == AppState.Playing)
                {
                    Activate();
                }
            }
            else
            {
                // Auto-activate when no AppManager (WebGL direct load, debug builds)
                Debug.Log("[GameManager] Auto-activating without AppManager");
                Activate();
            }
        }

        private void OnDestroy()
        {
            if (AppManager.Instance != null)
            {
                AppManager.Instance.OnAppStateChanged -= HandleAppStateChanged;
            }
        }

        private void HandleAppStateChanged(AppState newState)
        {
            if (newState == AppState.Playing)
            {
                Activate();
            }
            else if (isActive)
            {
                Deactivate();
            }
        }

        /// <summary>
        /// Activate the gameplay loop. Resets state.
        /// </summary>
        public void Activate()
        {
            isActive = true;
            currentShot = 0;
            SetShotState(ShotState.Ready);
        }

        /// <summary>
        /// Deactivate the gameplay loop.
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
        }

        /// <summary>
        /// Transition to a new shot state.
        /// </summary>
        public void SetShotState(ShotState newState)
        {
            if (!isActive)
            {
                Debug.LogWarning("[GameManager] Cannot change shot state while inactive");
                return;
            }

            var previousState = currentShotState;
            currentShotState = newState;

            Debug.Log($"[GameManager] Shot {currentShot}: {previousState} -> {newState}");
            OnShotStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Called when a shot is launched. Transitions to Flying.
        /// </summary>
        public void LaunchShot()
        {
            if (!isActive || currentShotState != ShotState.Ready)
            {
                Debug.LogWarning("[GameManager] Cannot launch shot in current state");
                return;
            }

            currentShot++;
            SetShotState(ShotState.Flying);
        }

        /// <summary>
        /// Called when the ball has landed and stopped. Transitions to Landed,
        /// then either back to Ready or to GameOver.
        /// </summary>
        public void BallLanded()
        {
            if (!isActive || currentShotState != ShotState.Flying)
            {
                Debug.LogWarning("[GameManager] Ball cannot land in current state");
                return;
            }

            SetShotState(ShotState.Landed);

            if (currentShot >= MaxShots)
            {
                EndGame();
            }
            else
            {
                SetShotState(ShotState.Ready);
            }
        }

        private void EndGame()
        {
            isActive = false;
            Debug.Log($"[GameManager] Game over after {currentShot} shots");
            OnGameOver?.Invoke(currentShot, false);

            if (AppManager.Instance != null)
            {
                AppManager.Instance.EndGame();
            }
        }
    }
}
