using NUnit.Framework;
using System.IO;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for SettingsController.
    /// </summary>
    public class SettingsControllerTests
    {
        [Test]
        public void OnVolumeChanged_DoesNotSetAudioListenerVolumeDirectly()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("SettingsController t:MonoScript");
            Assert.IsTrue(guids.Length > 0, "SettingsController script not found in AssetDatabase");

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var fullPath = Path.GetFullPath(path);
            var source = File.ReadAllText(fullPath);

            Assert.IsFalse(
                source.Contains("AudioListener.volume"),
                "Volume changes must go through AudioManager.SetMasterVolume, not AudioListener.volume directly");
        }
    }
}
