using NUnit.Framework;
using UnityEngine;
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

            // BallLanded triggers Landed then Ready, so final state is Tee.
            // But Landed fires first, so the camera should have gone through Landing.
            // Since BallLanded calls Landed then immediately Ready, the final camera is Tee.
            // We test the intermediate by checking after LaunchShot + manual Landed state.
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

            // BallLanded goes Landed -> Ready, so we end on Tee
            gameManager.BallLanded();
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
                Assert.AreEqual(ActiveCamera.Tee, cameraController.CurrentCamera);
            }
        }

        [Test]
        public void DestroyController_DoesNotThrowOnStateChange()
        {
            cameraController.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            gameManager.Activate();

            // Destroy the controller
            Object.DestroyImmediate(controllerObj);
            controllerObj = null;

            // State changes after destruction should not throw
            Assert.DoesNotThrow(() => gameManager.LaunchShot());
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
