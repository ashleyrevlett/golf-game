using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace GolfGame.Core
{
    public static class HapticsManager
    {
        private const string PlayerPrefsKey = "HapticsEnabled";
        private const float PowerThreshold = 0.7f;
        private const int LowPowerDurationMs = 50;
        private static readonly int[] HighPowerPattern = { 80, 30, 50 };

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void TriggerHaptic(int durationMs);

        [DllImport("__Internal")]
        private static extern void TriggerHapticPattern(int[] pattern, int length);
#endif

        private static bool isEnabled;

        public static bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                PlayerPrefs.SetInt(PlayerPrefsKey, value ? 1 : 0);
            }
        }

        static HapticsManager()
        {
            isEnabled = PlayerPrefs.GetInt(PlayerPrefsKey, 1) == 1;
        }

        public static void TriggerShotHaptic(float powerNormalized)
        {
            if (!isEnabled) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (powerNormalized < PowerThreshold)
            {
                TriggerHaptic(LowPowerDurationMs);
            }
            else
            {
                TriggerHapticPattern(HighPowerPattern, HighPowerPattern.Length);
            }
#endif
        }
    }
}
