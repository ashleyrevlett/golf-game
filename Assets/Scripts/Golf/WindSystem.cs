using System;
using UnityEngine;
using GolfGame.Core;

namespace GolfGame.Golf
{
    /// <summary>
    /// Generates random wind per shot and provides wind vector during flight.
    /// Subscribes to GameManager shot state changes.
    /// </summary>
    public class WindSystem : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private BallPhysicsConfig config;

        [Header("References")]
        [SerializeField] private GameManager gameManager;

        private Vector3 currentWind;

        /// <summary>
        /// Current wind vector (horizontal only, y=0).
        /// </summary>
        public Vector3 CurrentWind => currentWind;

        /// <summary>
        /// Current wind speed in m/s.
        /// </summary>
        public float WindSpeed => currentWind.magnitude;

        /// <summary>
        /// Wind direction in degrees (0=north, 90=east).
        /// </summary>
        public float WindDirectionDegrees
        {
            get
            {
                if (currentWind.sqrMagnitude < 0.001f) return 0f;
                return Mathf.Atan2(currentWind.x, currentWind.z) * Mathf.Rad2Deg;
            }
        }

        /// <summary>
        /// Fires when wind changes. Payload is the new wind vector.
        /// </summary>
        public event Action<Vector3> OnWindChanged;

        private void Start()
        {
            if (gameManager != null)
            {
                gameManager.OnShotStateChanged += HandleShotStateChanged;
            }

            GenerateNewWind();
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnShotStateChanged -= HandleShotStateChanged;
            }
        }

        private void HandleShotStateChanged(ShotState newState)
        {
            if (newState == ShotState.Ready)
            {
                GenerateNewWind();
            }
        }

        /// <summary>
        /// Generate a new random wind vector within configured range.
        /// </summary>
        public void GenerateNewWind()
        {
            float minSpeed = config != null ? config.WindMinSpeed : 0f;
            float maxSpeed = config != null ? config.WindMaxSpeed : 8f;

            float speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
            float angleDegrees = UnityEngine.Random.Range(0f, 360f);
            float angleRadians = angleDegrees * Mathf.Deg2Rad;

            currentWind = new Vector3(
                Mathf.Sin(angleRadians) * speed,
                0f,
                Mathf.Cos(angleRadians) * speed
            );

            OnWindChanged?.Invoke(currentWind);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (currentWind.sqrMagnitude > 0.001f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position + Vector3.up * 2f, currentWind);
            }
        }
#endif
    }
}
