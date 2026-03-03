using UnityEngine;

namespace GolfGame.Multiplayer
{
    /// <summary>
    /// One-time initialization: registers services and performs
    /// platform-specific setup. Must execute before AppManager.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class Bootstrap : MonoBehaviour
    {
        private static bool initialized;

        private void Awake()
        {
            if (initialized) return;
            initialized = true;

            RegisterServices();
            ConfigurePlatform();

            Debug.Log("[Bootstrap] Initialization complete");
        }

        private static void RegisterServices()
        {
            ServiceLocator.Register<IAuthService>(new MockAuthService());
            ServiceLocator.Register<ILeaderboardService>(new MockLeaderboardService());

            Debug.Log("[Bootstrap] Mock services registered");
        }

        private static void ConfigurePlatform()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Lock framerate to device refresh rate
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 1;
#endif

            // Physics settings for mobile performance
            Physics.autoSyncTransforms = false;
            Time.fixedDeltaTime = 0.02f; // 50Hz physics
        }

        /// <summary>
        /// Reset for testing. Clears the initialized flag.
        /// </summary>
        public static void ResetForTesting()
        {
            initialized = false;
            ServiceLocator.Clear();
        }
    }
}
