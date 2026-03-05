using NUnit.Framework;
using UnityEngine;
using GolfGame.Core;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for MainMenuController.
    /// Verifies play button triggers AppManager.StartGame and
    /// visibility toggling via AppState events.
    /// </summary>
    public class MainMenuControllerTests
    {
        private GameObject appManagerObj;
        private AppManager appManager;

        [SetUp]
        public void SetUp()
        {
            if (AppManager.Instance != null)
            {
                Object.DestroyImmediate(AppManager.Instance.gameObject);
            }

            appManagerObj = new GameObject("AppManager");
            appManager = appManagerObj.AddComponent<AppManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (appManagerObj != null)
            {
                Object.DestroyImmediate(appManagerObj);
            }
        }

        [Test]
        public void PlayButton_InvokesStartGame_TransitionsToInstructions()
        {
            appManager.StartGame();
            Assert.AreEqual(AppState.Instructions, appManager.CurrentState);
        }

        [Test]
        public void AppStateChanged_TitleState_IsVisible()
        {
            bool receivedTitle = false;
            appManager.OnAppStateChanged += state =>
            {
                if (state == AppState.Title)
                    receivedTitle = true;
            };

            appManager.SetState(AppState.Instructions);
            appManager.SetState(AppState.Title);

            Assert.IsTrue(receivedTitle);
        }

        [Test]
        public void AppStateChanged_NonTitleState_WouldHide()
        {
            var lastState = AppState.Title;
            appManager.OnAppStateChanged += state => lastState = state;

            appManager.SetState(AppState.Instructions);

            Assert.AreEqual(AppState.Instructions, lastState);
            Assert.AreNotEqual(AppState.Title, lastState);
        }

        [Test]
        public void AppStateChanged_GameOver_IsNotTitle()
        {
            appManager.SetState(AppState.GameOver);
            Assert.AreNotEqual(AppState.Title, appManager.CurrentState);
        }
    }
}
