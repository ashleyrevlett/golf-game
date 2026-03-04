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

            teeCamera = GameObject.Find("TeeCamera")?.GetComponent<CinemachineCamera>();
            flightCamera = GameObject.Find("FlightCamera")?.GetComponent<CinemachineCamera>();
            landingCamera = GameObject.Find("LandingCamera")?.GetComponent<CinemachineCamera>();

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

            if (flightCamera != null && ballController != null)
            {
                flightCamera.Follow = ballController.transform;
                flightCamera.LookAt = ballController.transform;
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

        private void LateUpdate()
        {
            // Keep all cameras at least 1m above ground
            EnforceMinHeight(teeCamera);
            EnforceMinHeight(flightCamera);
            EnforceMinHeight(landingCamera);
        }

        private void EnforceMinHeight(CinemachineCamera cam)
        {
            if (cam == null) return;

            var t = cam.transform;
            float groundY = 0f;

            // Raycast down to find actual ground
            if (Physics.Raycast(t.position, Vector3.down, out RaycastHit hit, 100f))
            {
                groundY = hit.point.y;
            }

            float minY = groundY + 1f;
            if (t.position.y < minY)
            {
                t.position = new Vector3(t.position.x, minY, t.position.z);
            }
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
