using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GolfGame.Editor
{
    public static class PauseMenuSetup
    {
        [MenuItem("GolfGame/Setup Pause Menu")]
        public static void SetupPauseMenu()
        {
            // Check if PauseMenu already exists
            var existing = GameObject.Find("PauseMenu");
            if (existing != null)
            {
                Debug.LogWarning("[PauseMenuSetup] PauseMenu already exists in scene");
                return;
            }

            // Load assets
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Screens/PauseMenu.uxml");
            if (uxml == null)
            {
                Debug.LogError("[PauseMenuSetup] PauseMenu.uxml not found");
                return;
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/Settings/DefaultPanelSettings.asset");

            // Create GameObject
            var go = new GameObject("PauseMenu");
            var doc = go.AddComponent<UIDocument>();
            doc.visualTreeAsset = uxml;
            doc.sortingOrder = 10;
            if (panelSettings != null)
            {
                doc.panelSettings = panelSettings;
            }

            go.AddComponent<UI.PauseMenuController>();

            EditorUtility.SetDirty(go);
            Debug.Log("[PauseMenuSetup] PauseMenu created. Save the scene to persist.");
        }
    }
}
