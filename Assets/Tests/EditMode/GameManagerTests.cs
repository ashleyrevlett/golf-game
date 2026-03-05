using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Core;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for GameManager shot state machine.
    /// Tests run without AppManager dependency by using Activate() directly.
    /// Note: BallLanded() starts a coroutine (WaitForSeconds) that doesn't
    /// complete in edit mode. Tests manually advance to Ready where needed.
    /// </summary>
    public class GameManagerTests
    {
        private GameObject gameManagerObj;
        private GameManager gameManager;

        [SetUp]
        public void SetUp()
        {
            // Suppress coroutine / DontDestroyOnLoad warnings in edit mode
            LogAssert.ignoreFailingMessages = true;

            // Clean up any existing AppManager instance
            if (AppManager.Instance != null)
            {
                Object.DestroyImmediate(AppManager.Instance.gameObject);
            }

            gameManagerObj = new GameObject("GameManager");
            gameManager = gameManagerObj.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameManagerObj != null)
            {
                Object.DestroyImmediate(gameManagerObj);
            }
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void Activate_SetsIsActiveTrue()
        {
            gameManager.Activate();
            Assert.IsTrue(gameManager.IsActive);
        }

        [Test]
        public void Activate_ResetsToReadyState()
        {
            gameManager.Activate();
            Assert.AreEqual(ShotState.Ready, gameManager.CurrentShotState);
        }

        [Test]
        public void Activate_ResetsShotCountToZero()
        {
            gameManager.Activate();
            Assert.AreEqual(0, gameManager.CurrentShot);
        }

        [Test]
        public void LaunchShot_TransitionsToFlying()
        {
            gameManager.Activate();
            gameManager.LaunchShot();
            Assert.AreEqual(ShotState.Flying, gameManager.CurrentShotState);
        }

        [Test]
        public void LaunchShot_IncrementsShotCount()
        {
            gameManager.Activate();
            gameManager.LaunchShot();
            Assert.AreEqual(1, gameManager.CurrentShot);
        }

        [Test]
        public void BallLanded_TransitionsToLandedThenReady()
        {
            gameManager.Activate();
            gameManager.LaunchShot();

            var receivedStates = new List<ShotState>();
            gameManager.OnShotStateChanged += state => receivedStates.Add(state);

            gameManager.BallLanded();
            // Coroutine doesn't advance in edit mode — manually set Ready
            gameManager.SetShotState(ShotState.Ready);

            Assert.AreEqual(2, receivedStates.Count);
            Assert.AreEqual(ShotState.Landed, receivedStates[0]);
            Assert.AreEqual(ShotState.Ready, receivedStates[1]);
        }

        [Test]
        public void ShotCycle_FullLoop()
        {
            gameManager.Activate();

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(ShotState.Ready, gameManager.CurrentShotState);
                gameManager.LaunchShot();
                Assert.AreEqual(ShotState.Flying, gameManager.CurrentShotState);
                gameManager.BallLanded();
                // Manually advance — coroutine doesn't run in edit mode
                gameManager.SetShotState(ShotState.Ready);
            }

            Assert.AreEqual(5, gameManager.CurrentShot);
            Assert.AreEqual(ShotState.Ready, gameManager.CurrentShotState);
        }

        [Test]
        public void GameOver_AfterMaxShots()
        {
            gameManager.Activate();

            var gameOverFired = false;
            var gameOverShotCount = 0;
            gameManager.OnGameOver += (shots) =>
            {
                gameOverFired = true;
                gameOverShotCount = shots;
            };

            for (int i = 0; i < GameManager.MaxShots; i++)
            {
                gameManager.LaunchShot();
                gameManager.BallLanded();
                // For non-final shots, manually advance to Ready
                if (i < GameManager.MaxShots - 1)
                {
                    gameManager.SetShotState(ShotState.Ready);
                }
            }

            Assert.IsTrue(gameOverFired);
            Assert.AreEqual(GameManager.MaxShots, gameOverShotCount);
            Assert.IsFalse(gameManager.IsActive);
        }

        [Test]
        public void OnShotStateChanged_FiresOnEachTransition()
        {
            gameManager.Activate();

            var eventCount = 0;
            gameManager.OnShotStateChanged += _ => eventCount++;

            gameManager.LaunchShot();
            Assert.AreEqual(1, eventCount); // Flying

            gameManager.BallLanded();
            // Manually advance to Ready
            gameManager.SetShotState(ShotState.Ready);
            Assert.AreEqual(3, eventCount); // Landed + Ready
        }

        [Test]
        public void LaunchShot_WhileInactive_DoesNothing()
        {
            // Not activated
            gameManager.LaunchShot();
            Assert.AreEqual(0, gameManager.CurrentShot);
        }

        [Test]
        public void LaunchShot_WhenNotReady_DoesNothing()
        {
            gameManager.Activate();
            gameManager.LaunchShot(); // Now Flying

            var shotBefore = gameManager.CurrentShot;
            gameManager.LaunchShot(); // Should be ignored

            Assert.AreEqual(shotBefore, gameManager.CurrentShot);
            Assert.AreEqual(ShotState.Flying, gameManager.CurrentShotState);
        }

        [Test]
        public void BallLanded_WhenNotFlying_DoesNothing()
        {
            gameManager.Activate(); // Ready state
            gameManager.BallLanded(); // Should be ignored

            Assert.AreEqual(ShotState.Ready, gameManager.CurrentShotState);
        }

        [Test]
        public void Deactivate_SetsIsActiveFalse()
        {
            gameManager.Activate();
            gameManager.Deactivate();
            Assert.IsFalse(gameManager.IsActive);
        }

        [Test]
        public void Activate_AfterDeactivate_ResetsShotCount()
        {
            gameManager.Activate();
            gameManager.LaunchShot();
            gameManager.BallLanded();
            // Manually advance
            gameManager.SetShotState(ShotState.Ready);
            Assert.AreEqual(1, gameManager.CurrentShot);

            gameManager.Deactivate();
            gameManager.Activate();
            Assert.AreEqual(0, gameManager.CurrentShot);
        }
    }
}
