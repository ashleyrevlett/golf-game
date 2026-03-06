using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Core;

namespace GolfGame.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for AppManager behavior that requires runtime execution:
    /// DontDestroyOnLoad, deferred Destroy, and frame-boundary lifecycle.
    /// State transition logic is covered by EditMode AppManagerTests.
    /// </summary>
    public class AppManagerPlayModeTests
    {
        private GameObject managerObj;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;

            if (AppManager.Instance != null)
            {
                Object.DestroyImmediate(AppManager.Instance.gameObject);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (AppManager.Instance != null)
            {
                Object.Destroy(AppManager.Instance.gameObject);
            }
            if (managerObj != null)
            {
                Object.Destroy(managerObj);
            }
            Time.timeScale = 1f;
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator Singleton_SetsInstanceAfterAwake()
        {
            managerObj = new GameObject("AppManager");
            var appManager = managerObj.AddComponent<AppManager>();

            yield return null;

            Assert.AreEqual(appManager, AppManager.Instance);
        }

        [UnityTest]
        public IEnumerator Singleton_DuplicateDestroyedAfterFrame()
        {
            managerObj = new GameObject("AppManager");
            var first = managerObj.AddComponent<AppManager>();

            yield return null;

            var secondObj = new GameObject("AppManager2");
            secondObj.AddComponent<AppManager>();

            yield return null; // Destroy is deferred to end of frame

            Assert.AreEqual(first, AppManager.Instance,
                "First instance should remain as singleton");
        }

        [UnityTest]
        public IEnumerator OnDestroy_ClearsInstance()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;
            Assert.IsNotNull(AppManager.Instance);

            Object.Destroy(managerObj);
            managerObj = null;
            yield return null;

            Assert.IsNull(AppManager.Instance,
                "Instance should be null after destroy");
        }

        [UnityTest]
        public IEnumerator PauseGame_SetsTimeScaleToZero()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;

            AppManager.Instance.SetState(AppState.Playing);
            AppManager.Instance.PauseGame();

            yield return null;

            Assert.AreEqual(0f, Time.timeScale,
                "Time.timeScale should be 0 when paused");

            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator ResumeGame_RestoresTimeScaleToOne()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;

            AppManager.Instance.SetState(AppState.Playing);
            AppManager.Instance.PauseGame();
            AppManager.Instance.ResumeGame();

            yield return null;

            Assert.AreEqual(1f, Time.timeScale,
                "Time.timeScale should be 1 after resume");
        }
    }
}
