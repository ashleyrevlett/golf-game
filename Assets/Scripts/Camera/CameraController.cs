using UnityEngine;
using GolfGame.Core;
using GolfGame.Golf;

using Unity.Cinemachine;

namespace GolfGame.Camera
{
    /// <summary>
    /// Manages Cinemachine camera switching based on shot state.
    /// Adjusts camera priorities to trigger CinemachineBrain blending.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        private const int ActivePriority = 10;
        private const int InactivePriority = 0;

        private GameManager gameManager;
        private BallController ballController;

        private CinemachineCamera teeCamera;
        private CinemachineCamera flightCamera;
        private CinemachineCamera landingCamera;

        [Header("Config")]
        [SerializeField] private CameraConfig config;

        private ActiveCamera currentCamera;

        /// <summary>
        /// Which camera is currently active.
        /// </summary>
        public ActiveCamera CurrentCamera => currentCamera;

        private void Start()
        {
            gameManager = FindFirstObjectByType<GameManager>();
            ballController = FindFirstObjectByType<BallController>();

            teeCamera = GameObject.FindWithTag("TeeCamera")?.GetComponent<CinemachineCamera>();
            if (teeCamera == null)
                Debug.LogWarning("[CameraController] TeeCamera tag lookup returned null — camera transitions will be disabled");

            flightCamera = GameObject.FindWithTag("FlightCamera")?.GetComponent<CinemachineCamera>();
            if (flightCamera == null)
                Debug.LogWarning("[CameraController] FlightCamera tag lookup returned null — camera transitions will be disabled");

            landingCamera = GameObject.FindWithTag("LandingCamera")?.GetComponent<CinemachineCamera>();
            if (landingCamera == null)
                Debug.LogWarning("[CameraController] LandingCamera tag lookup returned null — camera transitions will be disabled");

            if (teeCamera == null)
                Debug.LogWarning("[CameraController] TeeCamera tag not found — check scene tag assignment");
            if (flightCamera == null)
                Debug.LogWarning("[CameraController] FlightCamera tag not found — check scene tag assignment");
            if (landingCamera == null)
                Debug.LogWarning("[CameraController] LandingCamera tag not found — check scene tag assignment");

            if (gameManager != null)
            {
                gameManager.OnShotStateChanged += HandleShotStateChanged;
            }

            if (ballController != null)
            {
                ballController.OnBallLanded += HandleBallLanded;
            }

            ActivateTeeCamera();
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnShotStateChanged -= HandleShotStateChanged;
            }

            if (ballController != null)
            {
                ballController.OnBallLanded -= HandleBallLanded;
            }
        }

        private void HandleShotStateChanged(ShotState newState)
        {
            switch (newState)
            {
                case ShotState.Ready:
                    ActivateTeeCamera();
                    break;
                case ShotState.Flying:
                    ActivateFlightCamera();
                    break;
                case ShotState.Landed:
                    ActivateLandingCamera();
                    break;
            }
        }

        private void HandleBallLanded(Vector3 landPosition)
        {
            // Update landing camera to look at where ball stopped
            if (landingCamera != null)
            {
                landingCamera.Follow = ballController.transform;
                landingCamera.LookAt = ballController.transform;
            }
        }

        /// <summary>
        /// Activate the tee camera (behind ball, looking down course).
        /// </summary>
        public void ActivateTeeCamera()
        {
            currentCamera = ActiveCamera.Tee;
            SetCameraPriorities(ActivePriority, InactivePriority, InactivePriority);

            Debug.Log("[CameraController] Tee camera active");
        }

        /// <summary>
        /// Activate the flight camera (tracking ball).
        /// </summary>
        public void ActivateFlightCamera()
        {
            currentCamera = ActiveCamera.Flight;
            SetCameraPriorities(InactivePriority, ActivePriority, InactivePriority);

            if (ballController != null)
            {
                if (flightCamera != null)
                {
                    flightCamera.Follow = ballController.transform;
                    flightCamera.LookAt = ballController.transform;
                }
                // Pre-wire landing camera so it's ready for the transition
                if (landingCamera != null)
                {
                    landingCamera.Follow = ballController.transform;
                    landingCamera.LookAt = ballController.transform;
                }
            }

            Debug.Log("[CameraController] Flight camera active");
        }

        /// <summary>
        /// Activate the landing camera (close-up on ball).
        /// </summary>
        public void ActivateLandingCamera()
        {
            currentCamera = ActiveCamera.Landing;
            SetCameraPriorities(InactivePriority, InactivePriority, ActivePriority);

            Debug.Log("[CameraController] Landing camera active");
        }

        private void SetCameraPriorities(int tee, int flight, int landing)
        {
            if (teeCamera != null) teeCamera.Priority = tee;
            if (flightCamera != null) flightCamera.Priority = flight;
            if (landingCamera != null) landingCamera.Priority = landing;
        }

        /// <summary>
        /// Get the priority of a specific camera. Used for testing.
        /// </summary>
        public int GetCameraPriority(ActiveCamera camera)
        {
            switch (camera)
            {
                case ActiveCamera.Tee: return teeCamera != null ? (int)teeCamera.Priority : 0;
                case ActiveCamera.Flight: return flightCamera != null ? (int)flightCamera.Priority : 0;
                case ActiveCamera.Landing: return landingCamera != null ? (int)landingCamera.Priority : 0;
            }
            return 0;
        }
    }

    /// <summary>
    /// Enum identifying which camera is currently active.
    /// </summary>
    public enum ActiveCamera
    {
        Tee,
        Flight,
        Landing
    }
}
