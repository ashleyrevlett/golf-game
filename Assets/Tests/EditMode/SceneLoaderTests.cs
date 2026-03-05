using NUnit.Framework;
using GolfGame.Core;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for SceneLoader constants and event wiring.
    /// Async scene loading itself requires PlayMode; these tests verify
    /// the static API surface and constants.
    /// </summary>
    public class SceneLoaderTests
    {
        [Test]
        public void MainMenuScene_ConstantMatchesExpected()
        {
            Assert.AreEqual("MainMenu", SceneLoader.MainMenuScene);
        }

        [Test]
        public void GameplayScene_ConstantMatchesExpected()
        {
            Assert.AreEqual("Gameplay", SceneLoader.GameplayScene);
        }

        [Test]
        public void OnLoadProgress_CanSubscribeAndUnsubscribe()
        {
            // Verify event subscription does not throw
            bool fired = false;
            System.Action<float> handler = _ => fired = true;

            Assert.DoesNotThrow(() => SceneLoader.OnLoadProgress += handler);
            Assert.DoesNotThrow(() => SceneLoader.OnLoadProgress -= handler);
            Assert.IsFalse(fired, "Handler should not fire from subscribe/unsubscribe alone");
        }

        [Test]
        public void OnSceneLoaded_CanSubscribeAndUnsubscribe()
        {
            string loadedScene = null;
            System.Action<string> handler = s => loadedScene = s;

            Assert.DoesNotThrow(() => SceneLoader.OnSceneLoaded += handler);
            Assert.DoesNotThrow(() => SceneLoader.OnSceneLoaded -= handler);
            Assert.IsNull(loadedScene, "Handler should not fire from subscribe/unsubscribe alone");
        }
    }
}
