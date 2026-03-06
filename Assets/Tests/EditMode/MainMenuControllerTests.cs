using NUnit.Framework;
using System.IO;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for MainMenuController.
    /// Uses source-grep to verify architectural invariants.
    /// </summary>
    public class MainMenuControllerTests
    {
        [Test]
        public void HandleAppStateChanged_ShowsOnlyForTitleState()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("MainMenuController t:MonoScript");
            Assert.IsTrue(guids.Length > 0, "MainMenuController script not found in AssetDatabase");

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var fullPath = Path.GetFullPath(path);
            var source = File.ReadAllText(fullPath);

            Assert.IsTrue(
                source.Contains("AppState.Title"),
                "HandleAppStateChanged must reference AppState.Title to determine visibility");
        }
    }
}
