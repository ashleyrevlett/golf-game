using UnityEngine;
using UnityEngine.UIElements;

namespace GolfGame.UI
{
    /// <summary>
    /// Applies Screen.safeArea padding to a UIDocument's root VisualElement.
    /// Handles notches, punch-holes, and rounded corners on mobile devices.
    /// Attach to any GameObject with a UIDocument component.
    /// </summary>
    public class SafeAreaHandler : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Rect lastSafeArea;

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            // Re-check if safe area changed (orientation change, etc.)
            if (Screen.safeArea != lastSafeArea)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            if (uiDocument == null) return;

            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            var safeArea = Screen.safeArea;
            lastSafeArea = safeArea;

            // If safe area equals full screen, no padding needed
            if (safeArea.x == 0 && safeArea.y == 0 &&
                Mathf.Approximately(safeArea.width, Screen.width) &&
                Mathf.Approximately(safeArea.height, Screen.height))
            {
                root.style.paddingLeft = 0;
                root.style.paddingRight = 0;
                root.style.paddingTop = 0;
                root.style.paddingBottom = 0;
                return;
            }

            // Calculate insets in pixels
            float left = safeArea.x;
            float right = Screen.width - (safeArea.x + safeArea.width);
            float top = Screen.height - (safeArea.y + safeArea.height);
            float bottom = safeArea.y;

            root.style.paddingLeft = left;
            root.style.paddingRight = right;
            root.style.paddingTop = top;
            root.style.paddingBottom = bottom;
        }
    }
}
