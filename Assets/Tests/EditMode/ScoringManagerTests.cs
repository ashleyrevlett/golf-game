using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GolfGame.Core;
using GolfGame.Golf;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for ScoringManager statistics and best distance tracking.
    /// </summary>
    public class ScoringManagerTests
    {
        private GameObject scoringObj;
        private ScoringManager scoringManager;
        private GameObject gameManagerObj;
        private GameManager gameManager;
        private GameObject ballObj;
        private BallController ballController;
        private GameObject pinObj;
        private PinController pinController;

        [SetUp]
        public void SetUp()
        {
            if (AppManager.Instance != null)
            {
                Object.DestroyImmediate(AppManager.Instance.gameObject);
            }

            gameManagerObj = new GameObject("GameManager");
            gameManager = gameManagerObj.AddComponent<GameManager>();

            ballObj = new GameObject("Ball");
            ballObj.AddComponent<Rigidbody>();
            ballController = ballObj.AddComponent<BallController>();

            pinObj = new GameObject("Pin");
            pinObj.transform.position = new Vector3(0f, 0f, 114f);
            pinController = pinObj.AddComponent<PinController>();

            scoringObj = new GameObject("ScoringManager");
            scoringManager = scoringObj.AddComponent<ScoringManager>();

            SetPrivateField(scoringManager, "gameManager", gameManager);
            SetPrivateField(scoringManager, "ballController", ballController);
            SetPrivateField(scoringManager, "pinController", pinController);
        }

        [TearDown]
        public void TearDown()
        {
            if (scoringObj != null) Object.DestroyImmediate(scoringObj);
            if (gameManagerObj != null) Object.DestroyImmediate(gameManagerObj);
            if (ballObj != null) Object.DestroyImmediate(ballObj);
            if (pinObj != null) Object.DestroyImmediate(pinObj);
        }

        [Test]
        public void InitialBestDistance_IsMaxValue()
        {
            Assert.AreEqual(float.MaxValue, scoringManager.BestDistance);
        }

        [Test]
        public void BestDistance_UpdatesOnCloserShot()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            // Simulate shot landing at (0,0,110) -> 4m from pin
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 110f));
            gameManager.BallLanded();

            Assert.AreEqual(4f, scoringManager.BestDistance, 0.1f);

            // Closer shot at (0,0,113) -> 1m from pin
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 113f));
            gameManager.BallLanded();

            Assert.AreEqual(1f, scoringManager.BestDistance, 0.1f);
        }

        [Test]
        public void BestDistance_DoesNotUpdateOnFartherShot()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            // Close shot: 2m from pin
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 112f));
            gameManager.BallLanded();

            float bestAfterFirst = scoringManager.BestDistance;

            // Farther shot: 10m from pin
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 104f));
            gameManager.BallLanded();

            Assert.AreEqual(bestAfterFirst, scoringManager.BestDistance, 0.001f);
        }

        [Test]
        public void ShotResult_RecordsCorrectShotNumber()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 100f));
            gameManager.BallLanded();

            Assert.AreEqual(1, scoringManager.Results.Count);
            Assert.AreEqual(1, scoringManager.Results[0].ShotNumber);
        }

        [Test]
        public void OnShotScored_FiresWithCorrectData()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            ShotResult? receivedResult = null;
            scoringManager.OnShotScored += result => receivedResult = result;

            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 100f));
            gameManager.BallLanded();

            Assert.IsNotNull(receivedResult);
            Assert.AreEqual(14f, receivedResult.Value.DistanceToPin, 0.1f);
        }

        [Test]
        public void OnBestDistanceUpdated_FiresOnImprovement()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            var updates = new List<float>();
            scoringManager.OnBestDistanceUpdated += d => updates.Add(d);

            // First shot — always improves from MaxValue
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 100f));
            gameManager.BallLanded();

            Assert.AreEqual(1, updates.Count);

            // Second shot — closer
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 113f));
            gameManager.BallLanded();

            Assert.AreEqual(2, updates.Count);
        }

        [Test]
        public void OnBestDistanceUpdated_DoesNotFireWhenNoImprovement()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            var updates = new List<float>();
            scoringManager.OnBestDistanceUpdated += d => updates.Add(d);

            // First shot
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 113f));
            gameManager.BallLanded();

            Assert.AreEqual(1, updates.Count);

            // Farther shot
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 50f));
            gameManager.BallLanded();

            Assert.AreEqual(1, updates.Count); // no new update
        }

        [Test]
        public void AllShotsRecorded_InResultsList()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            for (int i = 0; i < 3; i++)
            {
                gameManager.LaunchShot();
                SimulateBallLand(new Vector3(0f, 0f, 100f + i));
                gameManager.BallLanded();
            }

            Assert.AreEqual(3, scoringManager.Results.Count);
        }

        [Test]
        public void Reset_ClearsBestDistanceAndResults()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 110f));
            gameManager.BallLanded();

            Assert.AreEqual(1, scoringManager.Results.Count);

            scoringManager.Reset();

            Assert.AreEqual(float.MaxValue, scoringManager.BestDistance);
            Assert.AreEqual(0, scoringManager.Results.Count);
        }

        [Test]
        public void NoPinController_GracefulHandling()
        {
            // Create scoring manager without pin
            var noPinObj = new GameObject("NoPinScoring");
            var noPinScoring = noPinObj.AddComponent<ScoringManager>();
            SetPrivateField(noPinScoring, "gameManager", gameManager);
            SetPrivateField(noPinScoring, "ballController", ballController);
            // pinController intentionally null

            noPinScoring.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            Assert.DoesNotThrow(() =>
            {
                gameManager.LaunchShot();
                SimulateBallLand(new Vector3(0f, 0f, 100f));
                gameManager.BallLanded();
            });

            // Distance should be 0 when no pin
            Assert.AreEqual(0f, noPinScoring.Results[0].DistanceToPin);

            Object.DestroyImmediate(noPinObj);
        }

        private void SimulateBallLand(Vector3 position)
        {
            ballObj.transform.position = position;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }
    }
}
