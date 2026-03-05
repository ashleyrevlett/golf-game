using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Core;

namespace GolfGame.Tests.PlayMode
{
    /// <summary>
    /// PlayMode tests for GameManager state transitions using coroutines.
    /// Verifies runtime behavior that EditMode cannot test (WaitForSeconds, coroutine flow).
    /// </summary>
    public class GameManagerPlayModeTests
    {
        private GameObject managerObj;
        private GameManager gameManager;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            managerObj = new GameObject("GameManager");
            gameManager = managerObj.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (managerObj != null)
            {
                Object.Destroy(managerObj);
            }
            // Clean up any AppManager that might have been created
            if (AppManager.Instance != null)
            {
                Object.Destroy(AppManager.Instance.gameObject);
            }
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator Activate_SetsReadyState()
        {
            yield return null; // let Start run

            gameManager.Activate();

            Assert.IsTrue(gameManager.IsActive);
            Assert.AreEqual(ShotState.Ready, gameManager.CurrentShotState);
            Assert.AreEqual(0, gameManager.CurrentShot);
        }

        [UnityTest]
        public IEnumerator LaunchShot_TransitionsToFlying()
        {
            yield return null;

            gameManager.Activate();
            gameManager.LaunchShot();

            Assert.AreEqual(ShotState.Flying, gameManager.CurrentShotState);
            Assert.AreEqual(1, gameManager.CurrentShot);
        }

        [UnityTest]
        public IEnumerator BallLanded_TransitionsToLanded()
        {
            yield return null;

            gameManager.Activate();
            gameManager.LaunchShot();
            gameManager.BallLanded();

            Assert.AreEqual(ShotState.Landed, gameManager.CurrentShotState);
        }

        [UnityTest]
        public IEnumerator BallLanded_CoroutineResetsToReady()
        {
            yield return null;

            gameManager.Activate();
            gameManager.LaunchShot();

            bool resetFired = false;
            gameManager.OnResetToTee += () => resetFired = true;

            gameManager.BallLanded();
            Assert.AreEqual(ShotState.Landed, gameManager.CurrentShotState);

            // ResetAfterDelay uses 3 second delay
            yield return new WaitForSeconds(3.2f);

            Assert.IsTrue(resetFired, "OnResetToTee should have fired after delay");
            Assert.AreEqual(ShotState.Ready, gameManager.CurrentShotState);
        }

        [UnityTest]
        public IEnumerator OnShotStateChanged_FiresOnTransitions()
        {
            yield return null;

            gameManager.Activate();

            var states = new System.Collections.Generic.List<ShotState>();
            gameManager.OnShotStateChanged += s => states.Add(s);

            gameManager.LaunchShot();
            gameManager.BallLanded();

            Assert.AreEqual(2, states.Count);
            Assert.AreEqual(ShotState.Flying, states[0]);
            Assert.AreEqual(ShotState.Landed, states[1]);
        }

        [UnityTest]
        public IEnumerator MaxShots_TriggersGameOver()
        {
            yield return null;

            gameManager.Activate();

            bool gameOverFired = false;
            int gameOverShots = 0;
            gameManager.OnGameOver += shots =>
            {
                gameOverFired = true;
                gameOverShots = shots;
            };

            // Exhaust all shots
            for (int i = 0; i < GameManager.MaxShots; i++)
            {
                gameManager.SetShotState(ShotState.Ready);
                gameManager.LaunchShot();

                if (i < GameManager.MaxShots - 1)
                {
                    // Not the last shot -- BallLanded starts coroutine
                    gameManager.BallLanded();
                    // Wait for coroutine to reset state
                    yield return new WaitForSeconds(3.2f);
                }
            }

            // Last shot landing should trigger game over
            gameManager.BallLanded();
            yield return null;

            Assert.IsTrue(gameOverFired, "OnGameOver should fire after max shots");
            Assert.AreEqual(GameManager.MaxShots, gameOverShots);
            Assert.IsFalse(gameManager.IsActive, "GameManager should be inactive after game over");
        }

        [UnityTest]
        public IEnumerator LaunchShot_WhileNotReady_IsIgnored()
        {
            yield return null;

            gameManager.Activate();
            gameManager.LaunchShot(); // now Flying
            int shotsBefore = gameManager.CurrentShot;

            gameManager.LaunchShot(); // should be ignored (not Ready)

            Assert.AreEqual(shotsBefore, gameManager.CurrentShot);
            Assert.AreEqual(ShotState.Flying, gameManager.CurrentShotState);
        }

        [UnityTest]
        public IEnumerator BallLanded_WhileNotFlying_IsIgnored()
        {
            yield return null;

            gameManager.Activate();
            // State is Ready, not Flying

            gameManager.BallLanded(); // should be ignored

            Assert.AreEqual(ShotState.Ready, gameManager.CurrentShotState);
        }

        [UnityTest]
        public IEnumerator Deactivate_PreventsStateChanges()
        {
            yield return null;

            gameManager.Activate();
            gameManager.Deactivate();

            gameManager.SetShotState(ShotState.Flying);

            // State should not change when inactive
            Assert.AreEqual(ShotState.Ready, gameManager.CurrentShotState);
        }
    }
}
