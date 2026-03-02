using UnityEngine;

namespace GolfGame.Camera
{
    /// <summary>
    /// ScriptableObject holding camera configuration for each shot phase.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Golf/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Tee Camera")]
        [SerializeField] private Vector3 teeOffset = new Vector3(0f, 1.5f, -3f);
        [SerializeField] private float teeFov = 60f;

        [Header("Flight Camera")]
        [SerializeField] private Vector3 flightOffset = new Vector3(2f, 3f, -5f);
        [SerializeField] private float flightFov = 50f;
        [SerializeField] private float flightDamping = 1f;
        [SerializeField] private float flightLookahead = 0.5f;

        [Header("Landing Camera")]
        [SerializeField] private Vector3 landingOffset = new Vector3(0.5f, 0.3f, -1.5f);
        [SerializeField] private float landingFov = 40f;

        [Header("Blend Durations")]
        [SerializeField] private float teeToFlightBlend = 0.5f;
        [SerializeField] private float flightToLandingBlend = 0.3f;

        public Vector3 TeeOffset => teeOffset;
        public float TeeFov => teeFov;
        public Vector3 FlightOffset => flightOffset;
        public float FlightFov => flightFov;
        public float FlightDamping => flightDamping;
        public float FlightLookahead => flightLookahead;
        public Vector3 LandingOffset => landingOffset;
        public float LandingFov => landingFov;
        public float TeeToFlightBlend => teeToFlightBlend;
        public float FlightToLandingBlend => flightToLandingBlend;
    }
}
