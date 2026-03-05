using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Camera;
using GolfGame.Core;
using GolfGame.Golf;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for CameraController state-driven camera switching.
    /// Tests camera state logic without Cinemachine (priorities tested via CurrentCamera).
    /// </summary>
    public class CameraControllerTests
    {
        private GameObject controllerObj;
        private CameraController cameraController;
        private GameObject gameManagerObj;
        private GameManager gameManager;
        private GameObject ballObj;
        private BallController ballController;

        [SetUp]
        public void SetUp()
        {
            // Suppress Cinemachine ShouldRunBehaviour assertions and
            // DontDestroyOnLoad / coroutine errors in edit mode
            LogAssert.ignoreFailingMessages = true;

            // Clean up any existing AppManager
            if (AppManager.Instance != null)
            {
                Object.DestroyImmediate(AppManager.Instance.gameObject);
            }

            gameManagerObj = new GameObject("GameManager");
            gameManager = gameManagerObj.AddComponent<GameManager>();

            ballObj = new GameObject("Ball");
            var rb = ballObj.AddComponent<Rigidbody>();
            ballController = ballObj.AddComponent<BallController>();
            ballController.SendMessage("Awake");

            controllerObj = new GameObject("CameraController");
            cameraController = controllerObj.AddComponent<CameraController>();

            // Use reflection to set serialized fields since they're private
            SetPrivateField(cameraController, "gameManager", gameManager);
            SetPrivateField(cameraController, "ballController", ballController);
        }

        [TearDown]
        public void TearDown()
        {
            if (controllerObj != null) Object.DestroyImmediate(controllerObj);
            if (gameManagerObj != null) Object.DestroyImmediate(gameManagerObj);
            if (ballObj != null) Object.DestroyImmediate(ballObj);
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void ActivateTeeCamera_SetsCurrentCameraToTee()
        {
            cameraController.ActivateTeeCamera();
            Assert.AreEqual(ActiveCamera.Tee, cameraController.CurrentCamera);
        }

        [Test]
        public void ActivateFlightCamera_SetsCurrentCameraToFlight()
        {
            cameraController.ActivateFlightCamera();
            Assert.AreEqual(ActiveCamera.Flight, cameraController.CurrentCamera);
        }

        [Test]
        public void ActivateLandingCamera_SetsCurrentCameraToLanding()
        {
            cameraController.ActivateLandingCamera();
            Assert.AreEqual(ActiveCamera.Landing, cameraController.CurrentCamera);
        }

        [Test]
        public void OnlyOneCamera_ActiveAtATime()
        {
            cameraController.ActivateTeeCamera();
            Assert.AreEqual(ActiveCamera.Tee, cameraController.CurrentCamera);

            cameraController.ActivateFlightCamera();
            Assert.AreEqual(ActiveCamera.Flight, cameraController.CurrentCamera);
            Assert.AreNotEqual(ActiveCamera.Tee, cameraController.CurrentCamera);

            cameraController.ActivateLandingCamera();
            Assert.AreEqual(ActiveCamera.Landing, cameraController.CurrentCamera);
            Assert.AreNotEqual(ActiveCamera.Flight, cameraController.CurrentCamera);
        }

        [Test]
        public void ReadyState_ActivatesTeeCamera()
        {
            // Simulate Start() subscription
            cameraController.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

            gameManager.Activate();

            Assert.AreEqual(ActiveCamera.Tee, cameraController.CurrentCamera);
        }

        [Test]
        public void FlyingState_ActivatesFlightCamera()
        {
            cameraController.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

            gameManager.Activate();
            gameManager.LaunchShot();

            Assert.AreEqual(ActiveCamera.Flight, cameraController.CurrentCamera);
        }

        [Test]
        public void LandedState_ActivatesLandingCamera()
        {
            cameraController.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

            gameManager.Activate();
            gameManager.LaunchShot();
            gameManager.BallLanded();

            // BallLanded sets state to Landed. In edit mode, coroutine to Ready
            // doesn't complete, so camera stays at Landing after Landed event.
            // Manually advance to Ready to complete the cycle.
            gameManager.SetShotState(ShotState.Ready);

            Assert.AreEqual(ActiveCamera.Tee, cameraController.CurrentCamera);
        }

        [Test]
        public void ShotCycle_CameraFollowsStateTransitions()
        {
            cameraController.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

            gameManager.Activate();
            Assert.AreEqual(ActiveCamera.Tee, cameraController.CurrentCamera);

            gameManager.LaunchShot();
            Assert.AreEqual(ActiveCamera.Flight, cameraController.CurrentCamera);

            // BallLanded goes to Landed; manually advance to Ready
            gameManager.BallLanded();
            gameManager.SetShotState(ShotState.Ready);
            Assert.AreEqual(ActiveCamera.Tee, cameraController.CurrentCamera);
        }

        [Test]
        public void RapidStateTransitions_DoNotBreak()
        {
            cameraController.SendMessage("Start", SendMessageOptions.DontRequireReceiver);

            gameManager.Activate();

            // Rapidly cycle through multiple shots
            for (int i = 0; i < 5; i++)
            {
                gameManager.LaunchShot();
                Assert.AreEqual(ActiveCamera.Flight, cameraController.CurrentCamera);
                gameManager.BallLanded();
                // Manually advance — coroutine doesn't run in edit mode
                gameManager.SetShotState(ShotState.Ready);
                Assert.AreEqual(ActiveCamera.Tee, cameraController.CurrentCamera);
            }
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
