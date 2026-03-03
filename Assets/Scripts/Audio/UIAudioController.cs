using UnityEngine;

namespace GolfGame.Audio
{
    /// <summary>
    /// Provides static methods for UI sound effects.
    /// Called by UI controllers on button clicks and transitions.
    /// </summary>
    public class UIAudioController : MonoBehaviour
    {
        /// <summary>
        /// Play a button click sound.
        /// </summary>
        public static void PlayClick()
        {
            if (AudioManager.Instance == null) return;
            var config = AudioManager.Instance.Config;
            if (config == null) return;

            AudioManager.Instance.PlaySFX(config.ButtonClick, 0.5f);
        }

        /// <summary>
        /// Play the score reveal sound (game over screen).
        /// </summary>
        public static void PlayScoreReveal()
        {
            if (AudioManager.Instance == null) return;
            var config = AudioManager.Instance.Config;
            if (config == null) return;

            AudioManager.Instance.PlaySFX(config.ScoreReveal, 0.7f);
        }
    }
}
