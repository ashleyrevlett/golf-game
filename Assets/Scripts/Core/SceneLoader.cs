using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GolfGame.Core
{
    /// <summary>
    /// Static utility for async scene loading.
    /// </summary>
    public static class SceneLoader
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameplayScene = "Gameplay";

        /// <summary>
        /// Fires during scene load with progress value (0-1).
        /// </summary>
        public static event Action<float> OnLoadProgress;

        /// <summary>
        /// Fires when scene load completes.
        /// </summary>
        public static event Action<string> OnSceneLoaded;

        /// <summary>
        /// Load a scene asynchronously by name.
        /// </summary>
        public static async Task LoadSceneAsync(string sceneName)
        {
            // Skip reload if already in this scene
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                Debug.Log($"[SceneLoader] Already in scene '{sceneName}', skipping reload.");
                OnLoadProgress?.Invoke(1f);
                OnSceneLoaded?.Invoke(sceneName);
                return;
            }

            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load scene: {sceneName}");
                return;
            }

            while (!operation.isDone)
            {
                OnLoadProgress?.Invoke(operation.progress);
                await Task.Yield();
            }

            OnLoadProgress?.Invoke(1f);
            OnSceneLoaded?.Invoke(sceneName);
        }
    }
}
