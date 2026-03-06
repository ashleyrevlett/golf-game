using NUnit.Framework;
using System.IO;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for SettingsController.
    /// </summary>
    public class SettingsControllerTests
    {
        private string LoadSettingsControllerSource()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("SettingsController t:MonoScript");
            Assert.IsTrue(guids.Length > 0, "SettingsController script not found in AssetDatabase");

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var fullPath = Path.GetFullPath(path);
            return File.ReadAllText(fullPath);
        }

        [Test]
        public void OnVolumeChanged_DoesNotSetAudioListenerVolumeDirectly()
        {
            var source = LoadSettingsControllerSource();

            Assert.IsFalse(
                source.Contains("AudioListener.volume"),
                "Volume changes must go through AudioManager.SetMasterVolume, not AudioListener.volume directly");
        }

        [Test]
        public void OnVolumeChanged_RoutesVolumeViaAudioManager()
        {
            var source = LoadSettingsControllerSource();

            Assert.IsTrue(
                source.Contains("SetMasterVolume"),
                "OnVolumeChanged must route volume through AudioManager.SetMasterVolume");
        }

        [Test]
        public void OnBackClicked_ClearsActionBeforeInvoking()
        {
            var source = LoadSettingsControllerSource();

            var nullIndex = source.IndexOf("customBackAction = null");
            Assert.IsTrue(nullIndex >= 0,
                "OnBackClicked must null customBackAction before invoking");

            var invokeIndex = source.IndexOf("action.Invoke()", nullIndex);
            Assert.IsTrue(invokeIndex >= 0,
                "OnBackClicked must call action.Invoke() after clearing customBackAction");

            Assert.IsTrue(nullIndex < invokeIndex,
                "customBackAction must be set to null before action.Invoke() to prevent re-entrancy bugs");
        }

        [Test]
        public void ShowWithBackAction_WiresBackActionAndVisibility()
        {
            var source = LoadSettingsControllerSource();

            // Find the ShowWithBackAction method body
            var methodIndex = source.IndexOf("ShowWithBackAction");
            Assert.IsTrue(methodIndex >= 0,
                "SettingsController must have a ShowWithBackAction method");

            var methodBody = source.Substring(methodIndex);

            Assert.IsTrue(
                methodBody.Contains("customBackAction"),
                "ShowWithBackAction must assign customBackAction");

            Assert.IsTrue(
                methodBody.Contains("SetVisible(true)"),
                "ShowWithBackAction must call SetVisible(true) to show the panel");
        }
    }
}
