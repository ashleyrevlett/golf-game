using System;
using System.Threading.Tasks;
using UnityEngine;
using GolfGame.Core;
#if UNITY_WEBGL && !UNITY_EDITOR
using Unity.Services.Core;
#endif

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

        private async void Awake()
        {
            if (initialized) return;
            initialized = true;

            try
            {
                await RegisterServicesAsync();
                ConfigurePlatform();
                Debug.Log("[Bootstrap] Initialization complete");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bootstrap] Initialization failed: {ex}");
            }
        }

        private static async Task RegisterServicesAsync()
        {
            // Always register mocks first as fallback
            ServiceLocator.Register<IAuthService>(new MockAuthService());
            ServiceLocator.Register<ILeaderboardService>(new MockLeaderboardService());
            ServiceLocator.Register<IBestScoreService>(new PlayerPrefsBestScoreService());

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                await UnityServices.InitializeAsync();
                var authProvider = new DefaultUgsAuthProvider();
                var authService = new UgsAuthService(authProvider);
                await authService.SignInAsync();

                var nickname = PlayerPrefs.GetString("nickname", "");
                if (!string.IsNullOrEmpty(nickname))
                {
                    await authService.UpdateDisplayNameAsync(nickname);
                }

                ServiceLocator.Register<IAuthService>(authService);
                ServiceLocator.Register<ILeaderboardService>(
                    new UgsLeaderboardService(
                        new DefaultUgsCloudCodeProvider(),
                        new DefaultUgsLeaderboardsProvider()));

                Debug.Log("[Bootstrap] UGS services registered");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Bootstrap] UGS init failed, using mocks: {ex.Message}");
                // Mocks already registered above -- game continues
            }
#else
            await Task.CompletedTask; // suppress warning in editor
            Debug.Log("[Bootstrap] Editor mode: mock services registered");
#endif
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
