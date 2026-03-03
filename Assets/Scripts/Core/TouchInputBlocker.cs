using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace GolfGame.Core
{
    /// <summary>
    /// Prevents browser default touch behaviors (scroll, zoom, context menu)
    /// on the WebGL canvas. Attach to any persistent GameObject.
    /// </summary>
    public class TouchInputBlocker : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DisableBrowserTouchDefaults();
#endif

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                DisableBrowserTouchDefaults();
                Debug.Log("[TouchInputBlocker] Browser touch defaults disabled");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TouchInputBlocker] Failed to disable defaults: {e.Message}");
            }
#endif

            // Capture keyboard input in WebGL
#if UNITY_WEBGL
            WebGLInput.captureAllKeyboardInput = true;
#endif
        }
    }
}
