using NUnit.Framework;
using GolfGame.Core;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for SceneLoader constants and architectural invariants.
    /// </summary>
    public class SceneLoaderTests
    {
        [Test]
        public void MainMenuScene_MatchesActualSceneFile()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("MainMenu t:Scene");
            Assert.IsTrue(guids.Length > 0,
                $"Scene file for '{SceneLoader.MainMenuScene}' not found in AssetDatabase");
        }

        [Test]
        public void GameplayScene_MatchesActualSceneFile()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("Gameplay t:Scene");
            Assert.IsTrue(guids.Length > 0,
                $"Scene file for '{SceneLoader.GameplayScene}' not found in AssetDatabase");
        }

        [Test]
        public void SceneConstants_AreNotEmpty()
        {
            Assert.IsNotEmpty(SceneLoader.MainMenuScene);
            Assert.IsNotEmpty(SceneLoader.GameplayScene);
        }
    }
}
