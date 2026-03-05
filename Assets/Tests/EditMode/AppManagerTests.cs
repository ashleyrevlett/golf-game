using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Core;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for AppManager state transitions and events.
    /// Note: Scene loading cannot be tested in EditMode — see PlayMode tests.
    /// </summary>
    public class AppManagerTests
    {
        private GameObject appManagerObj;
        private AppManager appManager;

        [SetUp]
        public void SetUp()
        {
            // Suppress DontDestroyOnLoad / scene-load errors in edit mode
            LogAssert.ignoreFailingMessages = true;

            // Clean up any existing instance
            if (AppManager.Instance != null)
            {
                Object.DestroyImmediate(AppManager.Instance.gameObject);
            }

            appManagerObj = new GameObject("AppManager");
            appManager = appManagerObj.AddComponent<AppManager>();
            // Awake doesn't auto-fire in edit mode — invoke manually
            appManager.SendMessage("Awake");
        }

        [TearDown]
        public void TearDown()
        {
            if (appManagerObj != null)
            {
                Object.DestroyImmediate(appManagerObj);
            }
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Singleton_SetsInstance()
        {
            Assert.AreEqual(appManager, AppManager.Instance);
        }

        [Test]
        public void Singleton_DestroysSecondInstance()
        {
            var secondObj = new GameObject("AppManager2");
            var second = secondObj.AddComponent<AppManager>();
            // Invoke Awake — detects existing Instance and calls Destroy (suppressed)
            second.SendMessage("Awake");

            // Second instance should not replace first
            Assert.AreEqual(appManager, AppManager.Instance);
            Object.DestroyImmediate(secondObj);
        }

        [Test]
        public void SetState_ChangesCurrentState()
        {
            appManager.SetState(AppState.Instructions);
            Assert.AreEqual(AppState.Instructions, appManager.CurrentState);
        }

        [Test]
        public void SetState_FiresOnAppStateChanged()
        {
            var receivedStates = new List<AppState>();
            appManager.OnAppStateChanged += state => receivedStates.Add(state);

            appManager.SetState(AppState.Instructions);

            Assert.AreEqual(1, receivedStates.Count);
            Assert.AreEqual(AppState.Instructions, receivedStates[0]);
        }

        [Test]
        public void SetState_SameState_DoesNotFireEvent()
        {
            // First set to Instructions
            appManager.SetState(AppState.Instructions);

            var eventCount = 0;
            appManager.OnAppStateChanged += _ => eventCount++;

            // Set to same state again
            appManager.SetState(AppState.Instructions);

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void SetState_MultipleTransitions_FireAllEvents()
        {
            var receivedStates = new List<AppState>();
            appManager.OnAppStateChanged += state => receivedStates.Add(state);

            appManager.SetState(AppState.Instructions);
            appManager.SetState(AppState.GameOver);

            Assert.AreEqual(2, receivedStates.Count);
            Assert.AreEqual(AppState.Instructions, receivedStates[0]);
            Assert.AreEqual(AppState.GameOver, receivedStates[1]);
        }

        [Test]
        public void StartGame_TransitionsToInstructions()
        {
            appManager.StartGame();
            Assert.AreEqual(AppState.Instructions, appManager.CurrentState);
        }

        [Test]
        public void EndGame_TransitionsToGameOver()
        {
            appManager.SetState(AppState.Playing);
            appManager.EndGame();
            Assert.AreEqual(AppState.GameOver, appManager.CurrentState);
        }

        [Test]
        public void ReturnToTitle_TransitionsToTitle()
        {
            appManager.SetState(AppState.Instructions);
            appManager.ReturnToTitle();
            Assert.AreEqual(AppState.Title, appManager.CurrentState);
        }

        [Test]
        public void ShowLeaderboard_TransitionsToLeaderboard()
        {
            appManager.SetState(AppState.GameOver);
            appManager.ShowLeaderboard();
            Assert.AreEqual(AppState.Leaderboard, appManager.CurrentState);
        }
    }
}
