using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Core;

namespace GolfGame.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for AppManager lifecycle behavior.
    /// Verifies DontDestroyOnLoad singleton and state transitions
    /// that require runtime execution.
    /// </summary>
    public class AppManagerPlayModeTests
    {
        private GameObject managerObj;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;

            // Ensure no leftover instance
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
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator Singleton_SetsInstanceOnAwake()
        {
            managerObj = new GameObject("AppManager");
            var appManager = managerObj.AddComponent<AppManager>();

            yield return null;

            Assert.AreEqual(appManager, AppManager.Instance,
                "AppManager.Instance should be set after Awake");
        }

        [UnityTest]
        public IEnumerator Singleton_DestroysDuplicate()
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
        public IEnumerator SetState_FiresOnAppStateChanged()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;

            AppState receivedState = AppState.Title;
            bool eventFired = false;
            AppManager.Instance.OnAppStateChanged += state =>
            {
                receivedState = state;
                eventFired = true;
            };

            AppManager.Instance.SetState(AppState.Instructions);

            Assert.IsTrue(eventFired, "OnAppStateChanged should fire on state change");
            Assert.AreEqual(AppState.Instructions, receivedState);
            Assert.AreEqual(AppState.Instructions, AppManager.Instance.CurrentState);
        }

        [UnityTest]
        public IEnumerator SetState_SameState_DoesNotFireEvent()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;

            // Start fires SetState(Title) during Start, so CurrentState is Title
            int fireCount = 0;
            AppManager.Instance.OnAppStateChanged += _ => fireCount++;

            AppManager.Instance.SetState(AppState.Title); // same as current

            Assert.AreEqual(0, fireCount,
                "Setting same state should not fire event");
        }

        [UnityTest]
        public IEnumerator StartGame_TransitionsToInstructions()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;

            AppManager.Instance.StartGame();

            Assert.AreEqual(AppState.Instructions, AppManager.Instance.CurrentState);
        }

        [UnityTest]
        public IEnumerator EndGame_TransitionsToGameOver()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;

            // Set to Playing first, then end
            AppManager.Instance.SetState(AppState.Playing);
            AppManager.Instance.EndGame();

            Assert.AreEqual(AppState.GameOver, AppManager.Instance.CurrentState);
        }

        [UnityTest]
        public IEnumerator ReturnToTitle_TransitionsToTitle()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;

            AppManager.Instance.SetState(AppState.Instructions);
            AppManager.Instance.ReturnToTitle();

            Assert.AreEqual(AppState.Title, AppManager.Instance.CurrentState);
        }

        [UnityTest]
        public IEnumerator ShowLeaderboard_TransitionsToLeaderboard()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;

            AppManager.Instance.ShowLeaderboard();

            Assert.AreEqual(AppState.Leaderboard, AppManager.Instance.CurrentState);
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
        public IEnumerator StateTransitions_FireMultipleEvents()
        {
            managerObj = new GameObject("AppManager");
            managerObj.AddComponent<AppManager>();

            yield return null;

            var states = new System.Collections.Generic.List<AppState>();
            AppManager.Instance.OnAppStateChanged += s => states.Add(s);

            AppManager.Instance.SetState(AppState.Instructions);
            AppManager.Instance.SetState(AppState.Playing);
            AppManager.Instance.SetState(AppState.GameOver);

            Assert.AreEqual(3, states.Count);
            Assert.AreEqual(AppState.Instructions, states[0]);
            Assert.AreEqual(AppState.Playing, states[1]);
            Assert.AreEqual(AppState.GameOver, states[2]);
        }
    }

}
