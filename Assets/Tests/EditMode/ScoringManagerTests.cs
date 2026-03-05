using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Core;
using GolfGame.Golf;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for ScoringManager statistics, best distance tracking,
    /// and CTP accumulation.
    ///
    /// In edit mode, BallController.OnBallLanded never fires automatically
    /// (no physics simulation). Tests invoke it via reflection after positioning
    /// the ball. GameManager coroutines don't run, so state is advanced manually.
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
            // Suppress ShotPopup / Cinemachine / coroutine errors in edit mode
            LogAssert.ignoreFailingMessages = true;

            if (AppManager.Instance != null)
            {
                Object.DestroyImmediate(AppManager.Instance.gameObject);
            }

            gameManagerObj = new GameObject("GameManager");
            gameManager = gameManagerObj.AddComponent<GameManager>();

            ballObj = new GameObject("Ball");
            ballObj.AddComponent<Rigidbody>();
            ballController = ballObj.AddComponent<BallController>();
            ballController.SendMessage("Awake");

            pinObj = new GameObject("Pin");
            pinObj.transform.position = new Vector3(0f, 0f, 114f);
            pinController = pinObj.AddComponent<PinController>();

            scoringObj = new GameObject("ScoringManager");
            scoringManager = scoringObj.AddComponent<ScoringManager>();

            // Invoke Start to subscribe to events via FindFirstObjectByType
            scoringManager.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up ShotPopup objects leaked from HandleBallLanded
            foreach (var popup in Object.FindObjectsByType<ShotPopup>(FindObjectsSortMode.None))
                Object.DestroyImmediate(popup.gameObject);

            if (scoringObj != null) Object.DestroyImmediate(scoringObj);
            if (gameManagerObj != null) Object.DestroyImmediate(gameManagerObj);
            if (ballObj != null) Object.DestroyImmediate(ballObj);
            if (pinObj != null) Object.DestroyImmediate(pinObj);
            LogAssert.ignoreFailingMessages = false;
        }

        /// <summary>
        /// Simulate a ball landing: move the ball, fire BallController.OnBallLanded
        /// event via reflection (physics doesn't run in edit mode).
        /// </summary>
        private void SimulateBallLandAndFire(Vector3 position)
        {
            ballObj.transform.position = position;
            InvokeEvent(ballController, "OnBallLanded", position);
        }

        /// <summary>
        /// Complete a full shot cycle: launch, land, score, advance to Ready.
        /// </summary>
        private void CompleteShotCycle(Vector3 landPosition, bool isFinal = false)
        {
            gameManager.LaunchShot();
            SimulateBallLandAndFire(landPosition);
            gameManager.BallLanded();
            // Coroutine doesn't run in edit mode — manually advance for non-final shots
            if (!isFinal)
            {
                gameManager.SetShotState(ShotState.Ready);
            }
        }

        [Test]
        public void InitialBestDistance_IsMaxValue()
        {
            Assert.AreEqual(float.MaxValue, scoringManager.BestDistance);
        }

        [Test]
        public void BestDistance_UpdatesOnCloserShot()
        {
            gameManager.Activate();

            // Simulate shot landing at (0,0,110) -> 4m from pin
            CompleteShotCycle(new Vector3(0f, 0f, 110f));
            Assert.AreEqual(4f, scoringManager.BestDistance, 0.1f);

            // Closer shot at (0,0,113) -> 1m from pin
            CompleteShotCycle(new Vector3(0f, 0f, 113f));
            Assert.AreEqual(1f, scoringManager.BestDistance, 0.1f);
        }

        [Test]
        public void BestDistance_DoesNotUpdateOnFartherShot()
        {
            gameManager.Activate();

            // Close shot: 2m from pin
            CompleteShotCycle(new Vector3(0f, 0f, 112f));
            float bestAfterFirst = scoringManager.BestDistance;

            // Farther shot: 10m from pin
            CompleteShotCycle(new Vector3(0f, 0f, 104f));
            Assert.AreEqual(bestAfterFirst, scoringManager.BestDistance, 0.001f);
        }

        [Test]
        public void ShotResult_RecordsCorrectShotNumber()
        {
            gameManager.Activate();
            CompleteShotCycle(new Vector3(0f, 0f, 100f));

            Assert.AreEqual(1, scoringManager.Results.Count);
            Assert.AreEqual(1, scoringManager.Results[0].ShotNumber);
        }

        [Test]
        public void OnShotScored_FiresWithCorrectData()
        {
            gameManager.Activate();

            ShotResult? receivedResult = null;
            scoringManager.OnShotScored += result => receivedResult = result;

            CompleteShotCycle(new Vector3(0f, 0f, 100f));

            Assert.IsNotNull(receivedResult);
            Assert.AreEqual(14f, receivedResult.Value.DistanceToPin, 0.1f);
        }

        [Test]
        public void OnBestDistanceUpdated_FiresOnImprovement()
        {
            gameManager.Activate();

            var updates = new List<float>();
            scoringManager.OnBestDistanceUpdated += d => updates.Add(d);

            // First shot — always improves from MaxValue
            CompleteShotCycle(new Vector3(0f, 0f, 100f));
            Assert.AreEqual(1, updates.Count);

            // Second shot — closer
            CompleteShotCycle(new Vector3(0f, 0f, 113f));
            Assert.AreEqual(2, updates.Count);
        }

        [Test]
        public void OnBestDistanceUpdated_DoesNotFireWhenNoImprovement()
        {
            gameManager.Activate();

            var updates = new List<float>();
            scoringManager.OnBestDistanceUpdated += d => updates.Add(d);

            // First shot
            CompleteShotCycle(new Vector3(0f, 0f, 113f));
            Assert.AreEqual(1, updates.Count);

            // Farther shot
            CompleteShotCycle(new Vector3(0f, 0f, 50f));
            Assert.AreEqual(1, updates.Count); // no new update
        }

        [Test]
        public void AllShotsRecorded_InResultsList()
        {
            gameManager.Activate();

            for (int i = 0; i < 3; i++)
            {
                CompleteShotCycle(new Vector3(0f, 0f, 100f + i));
            }

            Assert.AreEqual(3, scoringManager.Results.Count);
        }

        [Test]
        public void Reset_ClearsBestDistanceAndResults()
        {
            gameManager.Activate();
            CompleteShotCycle(new Vector3(0f, 0f, 110f));

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

            // Start subscribes to ballController via FindFirstObjectByType
            noPinScoring.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            // Override: remove pinController (Start found our test pin)
            SetPrivateField(noPinScoring, "pinController", null);

            gameManager.Activate();

            Assert.DoesNotThrow(() =>
            {
                gameManager.LaunchShot();
                SimulateBallLandAndFire(new Vector3(0f, 0f, 100f));
                gameManager.BallLanded();
            });

            // Distance should be 0 when no pin
            Assert.AreEqual(0f, noPinScoring.Results[0].DistanceToPin);

            Object.DestroyImmediate(noPinObj);
        }

        // --- CTP tests ---

        [Test]
        public void TotalCtpDistance_StartsAtZero()
        {
            Assert.AreEqual(0f, scoringManager.TotalCtpDistance);
        }

        [Test]
        public void TotalCtpDistance_AccumulatesAcrossShots()
        {
            gameManager.Activate();

            // Shot 1: lands at (0,0,110) -> 4 yds from pin
            CompleteShotCycle(new Vector3(0f, 0f, 110f));
            float dist1 = pinController.CalculateDistance(new Vector3(0f, 0f, 110f));

            // Shot 2: lands at (0,0,100) -> 14 yds from pin
            CompleteShotCycle(new Vector3(0f, 0f, 100f));
            float dist2 = pinController.CalculateDistance(new Vector3(0f, 0f, 100f));

            // Shot 3: lands at (2,0,112) -> ~2.8 yds from pin
            CompleteShotCycle(new Vector3(2f, 0f, 112f));
            float dist3 = pinController.CalculateDistance(new Vector3(2f, 0f, 112f));

            Assert.AreEqual(dist1 + dist2 + dist3, scoringManager.TotalCtpDistance, 0.01f);
        }

        [Test]
        public void OnShotRecorded_FiresWithDistanceToPin()
        {
            gameManager.Activate();

            float receivedDistance = -1f;
            scoringManager.OnShotRecorded += d => receivedDistance = d;

            CompleteShotCycle(new Vector3(0f, 0f, 110f));

            float expectedDistance = pinController.CalculateDistance(new Vector3(0f, 0f, 110f));
            Assert.AreEqual(expectedDistance, receivedDistance, 0.01f);
        }

        [Test]
        public void OnShotRecorded_FiresOnEachShot()
        {
            gameManager.Activate();

            var distances = new List<float>();
            scoringManager.OnShotRecorded += d => distances.Add(d);

            for (int i = 0; i < 3; i++)
            {
                CompleteShotCycle(new Vector3(0f, 0f, 100f + i * 5));
            }

            Assert.AreEqual(3, distances.Count);
            // Each distance should be distinct
            Assert.AreNotEqual(distances[0], distances[1]);
            Assert.AreNotEqual(distances[1], distances[2]);
        }

        [Test]
        public void OnAllShotsComplete_FiresAfterMaxShots()
        {
            gameManager.Activate();

            float receivedTotal = -1f;
            scoringManager.OnAllShotsComplete += t => receivedTotal = t;

            for (int i = 0; i < GameManager.MaxShots; i++)
            {
                bool isFinal = i == GameManager.MaxShots - 1;
                CompleteShotCycle(new Vector3(0f, 0f, 110f), isFinal);
            }

            Assert.AreEqual(scoringManager.TotalCtpDistance, receivedTotal, 0.01f);
        }

        [Test]
        public void OnAllShotsComplete_DoesNotFireBeforeMaxShots()
        {
            gameManager.Activate();

            bool fired = false;
            scoringManager.OnAllShotsComplete += _ => fired = true;

            for (int i = 0; i < GameManager.MaxShots - 1; i++)
            {
                CompleteShotCycle(new Vector3(0f, 0f, 110f));
            }

            Assert.IsFalse(fired);
        }

        [Test]
        public void Reset_ClearsTotalCtpDistance()
        {
            gameManager.Activate();
            CompleteShotCycle(new Vector3(0f, 0f, 110f));

            Assert.Greater(scoringManager.TotalCtpDistance, 0f);

            scoringManager.Reset();

            Assert.AreEqual(0f, scoringManager.TotalCtpDistance);
        }

        [Test]
        public void BallLandsOnPin_RecordsZeroDistance()
        {
            gameManager.Activate();

            float receivedDistance = -1f;
            scoringManager.OnShotRecorded += d => receivedDistance = d;

            // Land at exact pin position
            CompleteShotCycle(new Vector3(0f, 0f, 114f));

            Assert.AreEqual(0f, receivedDistance, 0.01f);
            Assert.AreEqual(0f, scoringManager.TotalCtpDistance, 0.01f);
        }

        /// <summary>
        /// Invoke a C# event's backing delegate via reflection.
        /// </summary>
        private static void InvokeEvent<T>(object target, string eventName, T arg)
        {
            var field = target.GetType().GetField(eventName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(target) is System.Action<T> handler)
                handler.Invoke(arg);
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
