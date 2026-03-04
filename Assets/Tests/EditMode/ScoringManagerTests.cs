using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GolfGame.Core;
using GolfGame.Golf;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for ScoringManager statistics, best distance tracking,
    /// and CTP accumulation.
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

        // --- New CTP tests ---

        [Test]
        public void TotalCtpDistance_StartsAtZero()
        {
            Assert.AreEqual(0f, scoringManager.TotalCtpDistance);
        }

        [Test]
        public void TotalCtpDistance_AccumulatesAcrossShots()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            // Shot 1: lands at (0,0,110) -> 4 yds from pin
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 110f));
            gameManager.BallLanded();

            float dist1 = pinController.CalculateDistance(new Vector3(0f, 0f, 110f));

            // Shot 2: lands at (0,0,100) -> 14 yds from pin
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 100f));
            gameManager.BallLanded();

            float dist2 = pinController.CalculateDistance(new Vector3(0f, 0f, 100f));

            // Shot 3: lands at (2,0,112) -> ~2.8 yds from pin
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(2f, 0f, 112f));
            gameManager.BallLanded();

            float dist3 = pinController.CalculateDistance(new Vector3(2f, 0f, 112f));

            Assert.AreEqual(dist1 + dist2 + dist3, scoringManager.TotalCtpDistance, 0.01f);
        }

        [Test]
        public void OnShotRecorded_FiresWithDistanceToPin()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            float receivedDistance = -1f;
            scoringManager.OnShotRecorded += d => receivedDistance = d;

            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 110f));
            gameManager.BallLanded();

            float expectedDistance = pinController.CalculateDistance(new Vector3(0f, 0f, 110f));
            Assert.AreEqual(expectedDistance, receivedDistance, 0.01f);
        }

        [Test]
        public void OnShotRecorded_FiresOnEachShot()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            var distances = new List<float>();
            scoringManager.OnShotRecorded += d => distances.Add(d);

            for (int i = 0; i < 3; i++)
            {
                gameManager.LaunchShot();
                SimulateBallLand(new Vector3(0f, 0f, 100f + i * 5));
                gameManager.BallLanded();
            }

            Assert.AreEqual(3, distances.Count);
            // Each distance should be distinct
            Assert.AreNotEqual(distances[0], distances[1]);
            Assert.AreNotEqual(distances[1], distances[2]);
        }

        [Test]
        public void OnAllShotsComplete_FiresAfterMaxShots()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            float receivedTotal = -1f;
            scoringManager.OnAllShotsComplete += t => receivedTotal = t;

            for (int i = 0; i < GameManager.MaxShots; i++)
            {
                gameManager.LaunchShot();
                SimulateBallLand(new Vector3(0f, 0f, 110f));
                gameManager.BallLanded();
            }

            Assert.AreEqual(scoringManager.TotalCtpDistance, receivedTotal, 0.01f);
        }

        [Test]
        public void OnAllShotsComplete_DoesNotFireBeforeMaxShots()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            bool fired = false;
            scoringManager.OnAllShotsComplete += _ => fired = true;

            for (int i = 0; i < GameManager.MaxShots - 1; i++)
            {
                gameManager.LaunchShot();
                SimulateBallLand(new Vector3(0f, 0f, 110f));
                gameManager.BallLanded();
            }

            Assert.IsFalse(fired);
        }

        [Test]
        public void Reset_ClearsTotalCtpDistance()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 110f));
            gameManager.BallLanded();

            Assert.Greater(scoringManager.TotalCtpDistance, 0f);

            scoringManager.Reset();

            Assert.AreEqual(0f, scoringManager.TotalCtpDistance);
        }

        [Test]
        public void BallLandsOnPin_RecordsZeroDistance()
        {
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            float receivedDistance = -1f;
            scoringManager.OnShotRecorded += d => receivedDistance = d;

            // Land at exact pin position
            gameManager.LaunchShot();
            SimulateBallLand(new Vector3(0f, 0f, 114f));
            gameManager.BallLanded();

            Assert.AreEqual(0f, receivedDistance, 0.01f);
            Assert.AreEqual(0f, scoringManager.TotalCtpDistance, 0.01f);
        }

        [Test]
        public void VeryLargeDistance_FormatsCorrectly()
        {
            // Pure formatting test
            Assert.AreEqual("138.2", 138.2f.ToString("F1"));
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
